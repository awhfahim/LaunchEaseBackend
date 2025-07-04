using Acm.Application;
using Acm.Application.Options;
using Acm.Infrastructure;
using Acm.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Acm.Api;

public static class AccessControlManagementExtensions
{
    public static async Task<IServiceCollection> RegisterAcmAsync(this IServiceCollection services,
        IConfigurationRoot configuration, IHostEnvironment hostEnvironment, string? prefix = null)
    {
        var recaptchaOptions = configuration.GetRequiredSection(GoogleRecaptchaOptions.SectionName)
            .Get<GoogleRecaptchaOptions>();

        ArgumentNullException.ThrowIfNull(recaptchaOptions);

        services.AddHttpClient(GoogleRecaptchaOptions.SectionName,
            httpClient => httpClient.BaseAddress = new Uri(recaptchaOptions.VerificationEndpoint));


        // if (services.IsRunningInContainer(configuration))
        // {
        //     await services.MigrateAcmDbAsync(configuration);
        // }

        services.AddDatabaseConfig(configuration, hostEnvironment);

        // await services.SeedStatusOfUserAsync(configuration);
        // await services.SeedAdminUserAsync(configuration);
        // services.AddJwtAuth(configuration);
        
        services.RegisterSecurityManagementApplicationServices();
        await services.RegisterSecurityManagementInfrastructureServices(configuration);
        return services;
    }
}