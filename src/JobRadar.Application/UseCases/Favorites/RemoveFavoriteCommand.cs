using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Favorites;

public record RemoveFavoriteCommand(Guid UserId, Guid JobId) : IRequest;

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
{
    private readonly IFavoriteRepository _favoriteRepository;

    public RemoveFavoriteCommandHandler(IFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        await _favoriteRepository.RemoveAsync(request.UserId, request.JobId);
    }
}