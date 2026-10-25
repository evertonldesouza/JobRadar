using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Auth;

public record GetMeQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetMeQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.CreatedAt,
            user.PasswordHash is not null,
            user.GoogleId is not null);
    }
}
