using JobRadar.Application.Common;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null || !user.HasValidPasswordResetToken ||
            !TokenHasher.FixedTimeEquals(user.PasswordResetTokenHash!, TokenHasher.Hash(request.Token)))
            throw new UnauthorizedAccessException("Link de recuperação inválido ou expirado. Solicite um novo.");

        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
        await _userRepository.UpdateAsync(user);

        // Troca de senha derruba todas as sessões existentes.
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
    }
}
