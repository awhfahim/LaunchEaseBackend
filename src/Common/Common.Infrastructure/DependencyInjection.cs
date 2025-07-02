using Common.Application.Data;
using Common.Application.Options;
using Common.Application.Providers;
using Common.Application.Services;
using Common.Domain.CoreProviders;
using Common.Infrastructure.Extensions;
using Common.Infrastructure.Providers;
using Common.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;

namespace Common.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IStringTransformationProvider, StringTransformationProvider>();
        services.TryAddSingleton<IGuidProvider, GuidProvider>();
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        
        services.BindAndValidateOptions<MinioOptions>(MinioOptions.SectionName);
        
        var minioAccessKey = configuration.GetRequiredSection(MinioOptions.SectionName)
            .GetValue<string>(nameof(MinioOptions.AccessKey));
        var minioSecretKey = configuration.GetRequiredSection(MinioOptions.SectionName)
            .GetValue<string>(nameof(MinioOptions.SecretKey));
        var endpoint = configuration.GetRequiredSection(MinioOptions.SectionName)
            .GetValue<string>(nameof(MinioOptions.Endpoint));
        
        ArgumentNullException.ThrowIfNull(minioAccessKey);
        ArgumentNullException.ThrowIfNull(minioSecretKey);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
        {
            var dockerEndpoint = configuration.GetRequiredSection(MinioOptions.SectionName)
                .GetValue<string>(nameof(MinioOptions.DockerEndpoint));
            
            endpoint = dockerEndpoint;
        }
        
        services.AddMinio(options =>
        {
            options.WithEndpoint(endpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .WithSSL();
        });
        
        services.TryAddScoped<IFileStorageService, MinioService>();
        services.TryAddSingleton<IExternalMinioService, ExternalMinioService>();
        
        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<IDbConnectionFactory, PgConnectionFactory>();

        return services;
    }
}
