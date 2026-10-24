using JobRadar.Application.Abstractions;
using JobRadar.Application.Common;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobRadar.Application.UseCases.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

/// <summary>
/// Solicita recuperação de senha. Nunca revela se o email existe:
/// a resposta é sempre a mesma; o email só sai se houver conta com senha.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || user.PasswordHash is null)
        {
            _logger.LogDebug("Recuperação solicitada para email sem conta com senha (resposta genérica)");
            return;
        }

        var rawToken = _tokenService.GenerateRefreshToken();
        user.RequestPasswordReset(TokenHasher.Hash(rawToken), DateTime.UtcNow.Add(ResetTokenLifetime));
        await _userRepository.UpdateAsync(user);

        var resetUrl = FrontendRedirect.BuildPasswordResetUrl(_configuration["Frontend:BaseUrl"], rawToken);
        if (resetUrl is null)
        {
            _logger.LogWarning("Frontend:BaseUrl não configurado; link de recuperação não enviado para {UserId}", user.Id);
            return;
        }

        await _emailService.SendPasswordResetAsync(user, resetUrl);
    }
}
