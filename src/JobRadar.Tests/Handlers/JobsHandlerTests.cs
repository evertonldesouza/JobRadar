using JobRadar.Application.Abstractions;
using JobRadar.Application.UseCases.Jobs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using JobRadar.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JobRadar.Tests.Handlers;

public class JobsHandlerTests
{
    [Fact]
    public async Task GetJobs_ReturnsPagedResult()
    {
        var repo = new Mock<IJobRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var jobs = new List<Job> { Job.Create("Dev", "Co", "Remote", "https://example.com/job", DateTime.UtcNow, ["C#"], "Test") };
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

        var handler = new SyncJobsCommandHandler([source.Object], repo.Object, emailService.Object, alertRepo.Object, NullLogger<SyncJobsCommandHandler>.Instance);
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

    [Fact]
    public async Task SyncJobs_BackfillsSalaryOnExistingJob()
    {
        // A vaga já existe na base (sem salário) e agora a fonte informa a faixa.
        var existingUrl = "https://example.com/salario";
        var tracked = Job.Create("Dev", "Co", "Remote", existingUrl, DateTime.UtcNow, ["C#"], "Test");
        var incoming = Job.Create("Dev", "Co", "Remote", existingUrl, DateTime.UtcNow, ["C#"], "Test",
            new SalaryRange(80000, 100000, "USD", SalaryPeriod.Year));

        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([incoming]);

        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetExistingUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingUrl]);
        repo.Setup(r => r.GetExistingDeduplicationKeysAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetByUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([tracked]);

        var handler = new SyncJobsCommandHandler(
            [source.Object], repo.Object,
            new Mock<IEmailService>().Object, new Mock<IJobAlertRepository>().Object,
            NullLogger<SyncJobsCommandHandler>.Instance);

        var result = await handler.Handle(new SyncJobsCommand(), default);

        Assert.Equal(0, result);                      // nada novo para inserir
        Assert.True(tracked.HasSalary);               // mas o salário foi completado
        Assert.Equal(80000, tracked.SalaryMin);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Job>>()), Times.Never);
    }

    [Fact]
    public async Task SyncJobs_AppliesRetentionWhenConfigured()
    {
        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([]);

        var repo = new Mock<IJobRepository>();

        var handler = new SyncJobsCommandHandler(
            [source.Object], repo.Object,
            new Mock<IEmailService>().Object, new Mock<IJobAlertRepository>().Object,
            NullLogger<SyncJobsCommandHandler>.Instance);

        // Sem vagas novas e sem vaga alguma, o fluxo retorna antes da retenção.
        var result = await handler.Handle(new SyncJobsCommand(RetentionDays: 90), default);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SyncJobs_ExpiresOldJobsAndInvalidatesCache()
    {
        var fresh = Job.Create("Dev", "Co", "Remote", "https://example.com/novo", DateTime.UtcNow, ["C#"], "Test");

        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([fresh]);

        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetExistingUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetExistingDeduplicationKeysAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetByUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.RemoveOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Há vaga nova, então o fluxo de alertas roda: sem alertas cadastrados.
        var alertRepo = new Mock<IJobAlertRepository>();
        alertRepo.Setup(a => a.GetAllAsync()).ReturnsAsync([]);

        var handler = new SyncJobsCommandHandler(
            [source.Object], repo.Object,
            new Mock<IEmailService>().Object, alertRepo.Object,
            NullLogger<SyncJobsCommandHandler>.Instance);

        await handler.Handle(new SyncJobsCommand(RetentionDays: 90), default);

        repo.Verify(r => r.RemoveOlderThanAsync(
            It.Is<DateTime>(cutoff => cutoff < DateTime.UtcNow && cutoff > DateTime.UtcNow.AddDays(-91)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncJobs_WithExistingAndDuplicatedJobs_ImportsNothing()
    {
        var existing = Job.Create("Dev", "Co", "Remote", "https://example.com/1", DateTime.UtcNow, ["C#"], "Test");
        var duplicated = Job.Create("Dev", "Co", "Remote", "https://example.com/2", DateTime.UtcNow, ["C#"], "Test");

        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([existing, duplicated]);

        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetExistingUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["https://example.com/1"]);
        repo.Setup(r => r.GetExistingDeduplicationKeysAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetByUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new SyncJobsCommandHandler(
            [source.Object], repo.Object,
            new Mock<IEmailService>().Object, new Mock<IJobAlertRepository>().Object,
            NullLogger<SyncJobsCommandHandler>.Instance);

        var result = await handler.Handle(new SyncJobsCommand(), default);

        Assert.Equal(0, result);
        repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Job>>()), Times.Never);
    }

    [Fact]
    public async Task SyncJobs_WithNewJob_ImportsIt()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/nova", DateTime.UtcNow, ["C#"], "Test");

        var source = new Mock<IJobSource>();
        source.Setup(s => s.Name).Returns("Test");
        source.Setup(s => s.FetchJobsAsync()).ReturnsAsync([job]);

        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetExistingUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetExistingDeduplicationKeysAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetByUrlsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var alertRepo = new Mock<IJobAlertRepository>();
        alertRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var handler = new SyncJobsCommandHandler(
            [source.Object], repo.Object,
            new Mock<IEmailService>().Object, alertRepo.Object,
            NullLogger<SyncJobsCommandHandler>.Instance);

        var result = await handler.Handle(new SyncJobsCommand(), default);

        Assert.Equal(1, result);
        repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Job>>()), Times.Once);
    }

    [Fact]
    public async Task GetJobById_WithExistingJob_ReturnsDto()
    {
        var job = Job.Create("Dev Backend", "Co", "Remote", "https://example.com/job", DateTime.UtcNow, ["C#"], "Test");
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);

        var handler = new GetJobByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetJobByIdQuery(job.Id), default);

        Assert.Equal(job.Id, result.Id);
        Assert.Equal("Dev Backend", result.Title);
        Assert.Equal("Test", result.Source);
    }

    [Fact]
    public async Task GetJobById_WithUnknownJob_ThrowsKeyNotFound()
    {
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);

        var handler = new GetJobByIdQueryHandler(repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetJobByIdQuery(Guid.NewGuid()), default));
    }
}
