using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Favorites;

public record AddFavoriteCommand(Guid UserId, Guid JobId) : IRequest;

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IJobRepository _jobRepository;

    public AddFavoriteCommandHandler(IFavoriteRepository favoriteRepository, IJobRepository jobRepository)
    {
        _favoriteRepository = favoriteRepository;
        _jobRepository = jobRepository;
    }

    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId)
            ?? throw new KeyNotFoundException("Vaga não encontrada.");

        var alreadyFavorited = await _favoriteRepository.ExistsAsync(request.UserId, request.JobId);
        if (alreadyFavorited)
            throw new InvalidOperationException("Vaga já está nos favoritos.");

        var favorite = FavoriteJob.Create(request.UserId, request.JobId);
        await _favoriteRepository.AddAsync(favorite);
    }
}