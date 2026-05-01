using JobRadar.Application.Abstractions;
using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null || user.PasswordHash is null)
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(token, user.Name, user.Email);
    }
}