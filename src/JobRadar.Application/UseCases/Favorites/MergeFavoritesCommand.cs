using JobRadar.Application.DTOs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Favorites;

/// <summary>
/// Sincroniza favoritos salvos localmente (visitante) com a conta do usuário.
/// Idempotente: favoritos já existentes são ignorados e ids inexistentes são descartados.
/// </summary>
public record MergeFavoritesCommand(Guid UserId, IReadOnlyList<Guid> JobIds) : IRequest<IReadOnlyList<JobDto>>;

public sealed class MergeFavoritesCommandHandler : IRequestHandler<MergeFavoritesCommand, IReadOnlyList<JobDto>>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IJobRepository _jobRepository;

    public MergeFavoritesCommandHandler(IFavoriteRepository favoriteRepository, IJobRepository jobRepository)
    {
        _favoriteRepository = favoriteRepository;
        _jobRepository = jobRepository;
    }

    public async Task<IReadOnlyList<JobDto>> Handle(MergeFavoritesCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> requestedIds = request.JobIds ?? Array.Empty<Guid>();

        var requested = requestedIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requested.Count > 0)
        {
            var alreadySaved = (await _favoriteRepository.GetJobIdsByUserIdAsync(request.UserId)).ToHashSet();
            var candidates = requested.Where(id => !alreadySaved.Contains(id)).ToList();

            if (candidates.Count > 0)
            {
                // Só persiste favoritos de vagas que realmente existem na base.
                var existing = (await _jobRepository.GetExistingIdsAsync(candidates, cancellationToken)).ToHashSet();

                await _favoriteRepository.AddRangeAsync(
                    candidates.Where(existing.Contains).Select(id => FavoriteJob.Create(request.UserId, id)));
            }
        }

        var favorites = await _favoriteRepository.GetByUserIdAsync(request.UserId);

        return favorites
            .Select(f => JobDto.FromJob(f.Job))
            .OrderByDescending(j => j.PublishedAt)
            .ToList();
    }
}
