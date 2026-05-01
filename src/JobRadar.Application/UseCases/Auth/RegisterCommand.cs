using JobRadar.Application.Abstractions;
using JobRadar.Application.DTOs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<AuthResponse>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("Email já cadastrado.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.CreateWithPassword(request.Name, request.Email, passwordHash);

        await _userRepository.AddAsync(user);

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(token, user.Name, user.Email);
    }
}