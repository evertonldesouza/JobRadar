using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Favorites;

public record GetFavoritesQuery(Guid UserId) : IRequest<IEnumerable<JobDto>>;

public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, IEnumerable<JobDto>>
{
    private readonly IFavoriteRepository _favoriteRepository;

    public GetFavoritesQueryHandler(IFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task<IEnumerable<JobDto>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var favorites = await _favoriteRepository.GetByUserIdAsync(request.UserId);

        return favorites.Select(f => new JobDto(
            f.Job.Id,
            f.Job.Title,
            f.Job.Company,
            f.Job.Location,
            f.Job.Url,
            f.Job.PublishedAt,
            f.Job.Technologies,
            f.Job.Source
        ));
    }
}