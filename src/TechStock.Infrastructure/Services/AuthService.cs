using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TechStock.Application.DTOs.Auth;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TechStock.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(UserManager<AppUser> userManager, AppDbContext db, IConfiguration config)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new BusinessException("Invalid credentials.");

        if (!user.IsActive)
            throw new BusinessException("Account is disabled.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new BusinessException("Invalid credentials.");

        var token = GenerateJwt(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token as a security stamp extension (simple approach: store in a separate table if needed)
        // For simplicity, using a claims-based approach — refresh token stored as a user claim
        await _userManager.RemoveClaimsAsync(user,
            (await _userManager.GetClaimsAsync(user)).Where(c => c.Type == "refresh_token").ToList());
        await _userManager.AddClaimAsync(user, new Claim("refresh_token", refreshToken));
        await _userManager.AddClaimAsync(user, new Claim("refresh_token_expiry",
            DateTime.UtcNow.AddDays(GetRefreshExpiryDays()).ToString("O")));

        return new AuthResponse(token, refreshToken, MapToUserDto(user));
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken)
    {
        // Find user by refresh token claim
        var claims = await _db.UserClaims
            .Where(c => c.ClaimType == "refresh_token" && c.ClaimValue == refreshToken)
            .FirstOrDefaultAsync() ?? throw new BusinessException("Invalid or expired refresh token.");

        var user = await _userManager.FindByIdAsync(claims.UserId.ToString())
            ?? throw new BusinessException("User not found.");

        var expiryClaim = (await _userManager.GetClaimsAsync(user))
            .FirstOrDefault(c => c.Type == "refresh_token_expiry");

        if (expiryClaim == null || DateTime.Parse(expiryClaim.Value) < DateTime.UtcNow)
            throw new BusinessException("Refresh token has expired.");

        var newToken = GenerateJwt(user);
        var newRefresh = GenerateRefreshToken();

        await _userManager.RemoveClaimsAsync(user,
            (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "refresh_token" || c.Type == "refresh_token_expiry").ToList());
        await _userManager.AddClaimAsync(user, new Claim("refresh_token", newRefresh));
        await _userManager.AddClaimAsync(user, new Claim("refresh_token_expiry",
            DateTime.UtcNow.AddDays(GetRefreshExpiryDays()).ToString("O")));

        return new TokenResponse(newToken, newRefresh);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var claims = await _db.UserClaims
            .Where(c => c.ClaimType == "refresh_token" && c.ClaimValue == refreshToken)
            .FirstOrDefaultAsync();

        if (claims == null) return;

        var user = await _userManager.FindByIdAsync(claims.UserId.ToString());
        if (user == null) return;

        await _userManager.RemoveClaimsAsync(user,
            (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "refresh_token" || c.Type == "refresh_token_expiry").ToList());
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");
        return MapToUserDto(user);
    }

    private string GenerateJwt(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private int GetRefreshExpiryDays() => int.Parse(_config["Jwt:RefreshExpiryDays"] ?? "7");

    private static UserDto MapToUserDto(AppUser user) =>
        new(user.Id, user.Email!, user.FullName, user.Role.ToString());
}
