namespace TechStock.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record AuthResponse(string Token, string RefreshToken, UserDto User);

public record TokenResponse(string Token, string RefreshToken);

public record UserDto(Guid Id, string Email, string FullName, string Role);
