using JobRadar.Application.Abstractions;
using JobRadar.Application.Common;
using JobRadar.Application.DTOs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;

namespace JobRadar.Application.UseCases.Auth;

/// <summary>Emite o par access + refresh e persiste o refresh (hash) com validade de 30 dias.</summary>
public static class AuthSession
{
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public static async Task<AuthResponse> IssueAsync(
        User user,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        var accessToken = tokenService.GenerateToken(user);
        var rawRefreshToken = tokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            TokenHasher.Hash(rawRefreshToken),
            DateTime.UtcNow.Add(RefreshTokenLifetime));

        await refreshTokenRepository.AddAsync(refreshToken);

        return new AuthResponse(accessToken, rawRefreshToken, user.Name, user.Email);
    }
}
