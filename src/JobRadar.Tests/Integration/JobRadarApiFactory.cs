using JobRadar.Domain.Entities;
using JobRadar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobRadar.Tests.Integration;

/// <summary>
/// Sobe a API real em memória (pipeline completo) com o PostgreSQL trocado
/// por EF InMemory e sem o background service de sync.
/// </summary>
public sealed class JobRadarApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"jobradar-tests-{Guid.NewGuid()}";
    private bool _seeded;

    /// <summary>Vaga semeada usada pelos testes de detalhe/favoritos.</summary>
    public Guid SeededJobId { get; private set; }

    /// <summary>Segunda vaga semeada (mesma tecnologia, outra fonte).</summary>
    public Guid SecondSeededJobId { get; private set; }

    /// <summary>
    /// Popula a base em memória uma única vez por factory. Precisa ser chamado
    /// depois do primeiro <c>CreateClient()</c> (o host precisa existir).
    /// </summary>
    public void Seed()
    {
        if (_seeded)
            return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var python = Job.Create(
            "Python Backend Developer", "Acme", "Remote", "https://example.com/jobs/python",
            DateTime.UtcNow.AddDays(-1), ["python", "django"], "TestSource");
        var dotnet = Job.Create(
            ".NET Developer", "Globex", "São Paulo", "https://example.com/jobs/dotnet",
            DateTime.UtcNow.AddDays(-2), ["c#", ".net", "sql"], "TestSource");
        var dataJob = Job.Create(
            "Data Engineer", "Initech", "Remote", "https://example.com/jobs/data",
            DateTime.UtcNow.AddDays(-3), ["python", "sql", "airflow"], "TestSource");

        db.Jobs.AddRange(python, dotnet, dataJob);
        db.SaveChanges();

        SeededJobId = python.Id;
        SecondSeededJobId = dataJob.Id;
        _seeded = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting é o canal confiável para injetar configuração em APIs de
        // minimal hosting via WebApplicationFactory (vai para o host e daí
        // para o WebApplicationBuilder, antes de qualquer leitura de config).
        builder.UseSetting("Jwt:Secret", "integration-tests-secret-com-mais-de-32-caracteres-0123456789");
        builder.UseSetting("Jwt:Issuer", "JobRadar");
        builder.UseSetting("Jwt:Audience", "JobRadar");
        builder.UseSetting("Frontend:BaseUrl", "https://example.com/");

        builder.ConfigureServices(services =>
        {
            // Remove o registro do PostgreSQL (inclui options configuration do Npgsql).
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext)))).ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Sem sync automático durante os testes.
            var hosted = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in hosted)
                services.Remove(descriptor);
        });
    }
}
