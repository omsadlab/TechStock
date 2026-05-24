using TechStock.Application.DTOs.Auth;

namespace TechStock.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
}
