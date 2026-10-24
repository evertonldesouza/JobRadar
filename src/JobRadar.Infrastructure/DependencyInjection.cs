using JobRadar.Application.Abstractions;
using JobRadar.Domain.Interfaces;
using JobRadar.Infrastructure.Persistence;
using JobRadar.Infrastructure.Persistence.Repositories;
using JobRadar.Infrastructure.Services;
using JobRadar.Infrastructure.Sources;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobRadar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddDataProtection()
            .SetApplicationName("JobRadar")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IJobAlertRepository, JobAlertRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<ITokenService, TokenService>();
        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
            services.AddSingleton<IEmailService, SmtpEmailService>();
        else
            services.AddSingleton<IEmailService, MockEmailService>();

        services.AddHttpClient<RemoteOkSource>();
        services.AddHttpClient<AdzunaSource>();
        services.AddHttpClient<RemotiveSource>();
        services.AddHttpClient<CareerNestSource>();
        services.AddHttpClient<JobsBaseSource>();
        services.AddTransient<IJobSource, RemoteOkSource>();
        services.AddTransient<IJobSource, AdzunaSource>();
        services.AddTransient<IJobSource, RemotiveSource>();
        services.AddTransient<IJobSource, CareerNestSource>();
        services.AddTransient<IJobSource, JobsBaseSource>();

        return services;
    }
}
