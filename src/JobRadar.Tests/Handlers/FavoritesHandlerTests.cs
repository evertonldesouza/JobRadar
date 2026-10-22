using JobRadar.Application.UseCases.Favorites;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Moq;

namespace JobRadar.Tests.Handlers;

public class FavoritesHandlerTests
{
    private readonly Mock<IFavoriteRepository> _favRepo;
    private readonly Mock<IJobRepository> _jobRepo;

    public FavoritesHandlerTests()
    {
        _favRepo = new Mock<IFavoriteRepository>();
        _jobRepo = new Mock<IJobRepository>();
    }

    [Fact]
    public async Task AddFavorite_WithValidJob_AddsSuccessfully()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/job", DateTime.UtcNow, ["C#"], "Test");
        _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(job);
        _favRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        var handler = new AddFavoriteCommandHandler(_favRepo.Object, _jobRepo.Object);
        await handler.Handle(new AddFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        _favRepo.Verify(r => r.AddAsync(It.IsAny<FavoriteJob>()), Times.Once);
    }

    [Fact]
    public async Task AddFavorite_WithNonExistentJob_ThrowsKeyNotFound()
    {
        _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);

        var handler = new AddFavoriteCommandHandler(_favRepo.Object, _jobRepo.Object);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new AddFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task AddFavorite_AlreadyFavorited_ThrowsInvalidOperation()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/job", DateTime.UtcNow, ["C#"], "Test");
        _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(job);
        _favRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        var handler = new AddFavoriteCommandHandler(_favRepo.Object, _jobRepo.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task GetFavorites_ReturnsUserFavorites()
    {
        var userId = Guid.NewGuid();
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/job", DateTime.UtcNow, ["C#"], "Test");
        var fav = FavoriteJob.Create(userId, job.Id);

        var favField = typeof(FavoriteJob).GetField("<Job>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        favField?.SetValue(fav, job);

        _favRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<FavoriteJob> { fav });

        var handler = new GetFavoritesQueryHandler(_favRepo.Object);
        var result = await handler.Handle(new GetFavoritesQuery(userId), default);

        Assert.Single(result);
    }

    [Fact]
    public async Task RemoveFavorite_RemovesSuccessfully()
    {
        _favRepo.Setup(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        var handler = new RemoveFavoriteCommandHandler(_favRepo.Object);
        await handler.Handle(new RemoveFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        _favRepo.Verify(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
    }
}
