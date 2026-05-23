using JobRadar.Application.Abstractions;
using JobRadar.Application.UseCases.Jobs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace JobRadar.Tests.Handlers;

public class JobsHandlerTests
{
    [Fact]
    public async Task GetJobs_ReturnsPagedResult()
    {
        var repo = new Mock<IJobRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var jobs = new List<Job> { Job.Create("Dev", "Co", "Remote", "url", DateTime.UtcNow, ["C#"], "Test") };
        repo.Setup(r => r.GetAllAsync(null, null, null, 1, 20)).ReturnsAsync((jobs, 1));

        var handler = new GetJobsQueryHandler(repo.Object, cache);
        var result = await handler.Handle(new GetJobsQuery(null, null, null, 1, 20), default);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task SyncJobs_WithNoNewJobs_ReturnsZero()
    {
        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([]);

        var repo = new Mock<IJobRepository>();

        var emailService = new Mock<IEmailService>();
        var alertRepo = new Mock<IJobAlertRepository>();

        var handler = new SyncJobsCommandHandler([source.Object], repo.Object, emailService.Object, alertRepo.Object);
        var result = await handler.Handle(new SyncJobsCommand(), default);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task RemoveDuplicates_ReturnsRemovedCount()
    {
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.RemoveDuplicatesAsync()).ReturnsAsync(5);

        var handler = new RemoveDuplicatesCommandHandler(repo.Object);
        var result = await handler.Handle(new RemoveDuplicatesCommand(), default);

        Assert.Equal(5, result);
    }
}
