namespace JobRadar.Application.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>Par de tokens: access (curto, JWT) + refresh (longo, opaco e rotativo).</summary>
public record AuthResponse(string Token, string RefreshToken, string Name, string Email);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt,
    bool HasPassword,
    bool HasGoogleAccount
);
