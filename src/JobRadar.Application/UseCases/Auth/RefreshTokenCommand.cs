using JobRadar.Application.Abstractions;
using JobRadar.Application.Common;
using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var stored = await _refreshTokenRepository.GetByHashAsync(TokenHasher.Hash(request.RefreshToken))
            ?? throw new UnauthorizedAccessException("Sessão inválida. Faça login novamente.");

        if (stored.WasRevoked)
        {
            // Reuso de token já rotacionado = possível roubo: derruba todas as sessões.
            await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId);
            throw new UnauthorizedAccessException("Sessão inválida. Faça login novamente.");
        }

        if (!stored.IsActive)
            throw new UnauthorizedAccessException("Sessão expirada. Faça login novamente.");

        var user = await _userRepository.GetByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("Sessão inválida. Faça login novamente.");

        var response = await AuthSession.IssueAsync(user, _tokenService, _refreshTokenRepository);

        stored.Revoke(TokenHasher.Hash(response.RefreshToken));
        await _refreshTokenRepository.UpdateAsync(stored);

        return response;
    }
}
