using System.Text.Json;
using System.Text.Json.Serialization;
using Acm.Api;
using Acm.Application.Options;
using Common.Application;
using Common.Application.Options;
using Common.HttpApi.Others;
using Common.Infrastructure;
using Common.Infrastructure.Extensions;
using Common.Infrastructure.Persistence;
using Dapper;
using DotNetEnv;
using LaunchEase.Api.Others;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Env.Load();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console()
    .CreateBootstrapLogger();

DefaultTypeMap.MatchNamesWithUnderscores = true;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables();

    builder.Services.BindAndValidateOptions<AppOptions>(AppOptions.SectionName);
    builder.Services.BindAndValidateOptions<JwtOptions>(JwtOptions.SectionName);
    builder.Services.BindAndValidateOptions<AdminUserSeedOptions>(AdminUserSeedOptions.SectionName);
    builder.Services.BindAndValidateOptions<ConnectionStringOptions>(ConnectionStringOptions.SectionName);

    builder.Services.AddMemoryCache();
    builder.Services.AddOpenApi();

    builder.Services.AddSerilog((_, lc) => lc
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(LogEventLevel.Error)
    );

    if (builder.Environment.IsDevelopment())
    {
        Serilog.Debugging.SelfLog.Enable(Console.Error);
    }

    builder.Services.AddControllers(opts =>
            {
                opts.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
                opts.OutputFormatters.RemoveType<StringOutputFormatter>();
                opts.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            }
        )
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = ctx => ctx.MakeValidationErrorResponse();
        });


    var appOptions = configuration.GetRequiredSection(AppOptions.SectionName).Get<AppOptions>();

    ArgumentNullException.ThrowIfNull(appOptions);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(nameof(AppOptions.AllowedOriginsForCors), x => x
            .WithOrigins(appOptions.AllowedOriginsForCors)
            .WithExposedHeaders(AppConstants.XsrfTokenHeaderKey)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
        );
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCommonInfrastructureServices(builder.Configuration);
    builder.Services.AddCommonApplicationServices();

    await builder.Services.RegisterAcmAsync(builder.Configuration, builder.Environment);

    builder.Services.AddHealthChecks();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    DefaultTypeMap.MatchNamesWithUnderscores = true;
    SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseCors(nameof(AppOptions.AllowedOriginsForCors))
        .UseAuthentication()
        .UseMiddleware<Acm.Infrastructure.Middleware.TenantIsolationMiddleware>()
        .UseAuthorization()
        .UseExceptionHandler();

    app.MapControllers();
    app.MapHealthChecks("api/health");

    Log.Information("Application started");
    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application start-up failed");
    return 1;
}
finally
{
    Log.Information("Shut down complete");
    await Log.CloseAndFlushAsync();
}