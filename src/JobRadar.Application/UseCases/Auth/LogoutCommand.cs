using JobRadar.Application.Common;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record LogoutCommand(Guid UserId, string? RefreshToken) : IRequest;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var stored = await _refreshTokenRepository.GetByHashAsync(TokenHasher.Hash(request.RefreshToken));
        if (stored is null || stored.UserId != request.UserId || stored.WasRevoked)
            return;

        stored.Revoke();
        await _refreshTokenRepository.UpdateAsync(stored);
    }
}
