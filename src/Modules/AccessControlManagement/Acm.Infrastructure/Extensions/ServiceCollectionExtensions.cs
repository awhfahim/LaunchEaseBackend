using System.Text;
using Acm.Application;
using Acm.Application.Options;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Persistence;
using Common.Application.Options;
using Common.Application.Providers;
using Common.Domain.Enums;
using Common.Infrastructure.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Acm.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static bool IsRunningInContainer(this IServiceCollection services, IConfiguration configuration)
    {
        return configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
    }

    // public static async Task<IServiceCollection> SeedStatusOfUserAsync(this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     var dbUrl = configuration.GetRequiredSection(AcmConnectionStringOptions.SectionName)
    //         .GetValue<string>(nameof(AcmConnectionStringOptions.AcmDb));
    //
    //     var optionsBuilder = new DbContextOptionsBuilder<AcmDbContext>();
    //     optionsBuilder.UseNpgsql(dbUrl);
    //
    //     await using var dbContext = new AcmDbContext(optionsBuilder.Options);
    //
    //     if (await CheckBeforeSeed(dbContext) is false)
    //     {
    //         return services;
    //     }
    //
    //     var statusOfUsers = StatusOfUserSeed.Data;
    //
    //     var existingDataHeaders = await dbContext.StatusOfUsers
    //         .Where(x => statusOfUsers.Select(y => y.Id).Contains(x.Id))
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     var newHeaders = statusOfUsers.Where(x => existingDataHeaders.All(y => y.Id != x.Id)).ToList();
    //
    //     if (newHeaders.Count == 0)
    //     {
    //         return services;
    //     }
    //
    //     await dbContext.StatusOfUsers.AddRangeAsync(newHeaders);
    //     await dbContext.SaveChangesAsync();
    //
    //     return services;
    // }
    //
    // public static async Task<IServiceCollection> SeedAdminUserAsync(this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     var seedData = configuration.GetRequiredSection(AdminUserSeedOptions.SectionName)
    //         .Get<AdminUserSeedOptions>();
    //
    //     ArgumentNullException.ThrowIfNull(seedData);
    //
    //     var dbUrl = configuration.GetRequiredSection(AcmConnectionStringOptions.SectionName)
    //         .GetValue<string>(nameof(AcmConnectionStringOptions.AcmDb));
    //
    //     var authCryptographyService = new AuthCryptographyService();
    //     var dateTimeProvider = new DateTimeProvider();
    //
    //     var optionsBuilder = new DbContextOptionsBuilder<AcmDbContext>();
    //     optionsBuilder.UseNpgsql(dbUrl);
    //
    //     await using var dbContext = new AcmDbContext(optionsBuilder.Options);
    //
    //     if (await CheckBeforeSeed(dbContext) is false)
    //     {
    //         return services;
    //     }
    //
    //     var admin = await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == seedData.UserName);
    //
    //     if (admin is not null)
    //     {
    //         return services;
    //     }
    //
    //     var entity = new User
    //     {
    //         UserName = seedData.UserName,
    //         FullName = seedData.FullName,
    //         UserType = 'I',
    //         StatusId = StatusOfUserSeed.Data.First(x => x.Id == 1).Id,
    //         IsLocked = false,
    //         LoginAttemptCount = 0,
    //         PasswordHash = await authCryptographyService.HashPasswordAsync(seedData.Password),
    //         IsTemporaryPassword = seedData.IsTemporaryPassword,
    //         CreatedAtUtc = dateTimeProvider.CurrentUtcTime
    //     };
    //
    //     await dbContext.Users.AddAsync(entity);
    //     await dbContext.SaveChangesAsync();
    //     Console.WriteLine("Super Admin seed done");
    //     return services;
    // }
    //
    // public static async Task<IServiceCollection> SeedPermissionsAsync(this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     var dbUrl = configuration.GetRequiredSection(AcmConnectionStringOptions.SectionName)
    //         .GetValue<string>(nameof(AcmConnectionStringOptions.AcmDb));
    //
    //     var dateTimeProvider = new DateTimeProvider();
    //
    //     var optionsBuilder = new DbContextOptionsBuilder<AcmDbContext>()
    //         .UseNpgsql(dbUrl);
    //
    //     await using var dbContext = new AcmDbContext(optionsBuilder.Options);
    //
    //     if (await CheckBeforeSeed(dbContext) is false)
    //     {
    //         return services;
    //     }
    //
    //     await using var transaction = await dbContext.Database.BeginTransactionAsync();
    //     try
    //     {
    //         await InsertPermissions(dbContext, dateTimeProvider);
    //         await SeedAdminUserPermissionAsync(dbContext, configuration, dateTimeProvider);
    //         await transaction.CommitAsync();
    //     }
    //     catch (Exception error)
    //     {
    //         Console.WriteLine(error);
    //         await transaction.RollbackAsync();
    //     }
    //
    //     return services;
    // }
    //
    // private static async Task<bool> SeedAdminUserPermissionAsync(AcmDbContext dbContext,
    //     IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    // {
    //     var seedData = configuration.GetRequiredSection(AdminUserSeedOptions.SectionName)
    //         .Get<AdminUserSeedOptions>();
    //
    //     ArgumentNullException.ThrowIfNull(seedData);
    //
    //     var superAdmin = await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == seedData.UserName);
    //
    //     if (superAdmin is null)
    //     {
    //         return false;
    //     }
    //
    //     var superAdminPermission = await dbContext.AuthorizablePermissions.FirstOrDefaultAsync(x =>
    //         x.Name == MatchablePermission.SuperAdmin.ToString());
    //
    //     if (superAdminPermission is null)
    //     {
    //         return false;
    //     }
    //
    //     var hasSuperAdminRole = await dbContext.AuthorizableRoles.AnyAsync(x =>
    //         x.Name == MatchablePermission.SuperAdmin.ToString());
    //
    //     if (hasSuperAdminRole is false)
    //     {
    //         var role = new AuthorizableRole
    //         {
    //             Name = MatchablePermission.SuperAdmin.ToString(),
    //             CreatedById = superAdmin.Id,
    //             UpdatedById = null,
    //             CreatedAtUtc = dateTimeProvider.CurrentUtcTime
    //         };
    //
    //         await dbContext.AuthorizableRoles.AddAsync(role);
    //         await dbContext.SaveChangesAsync();
    //     }
    //
    //     var superAdminRole = await dbContext.AuthorizableRoles.FirstOrDefaultAsync(x =>
    //         x.Name == MatchablePermission.SuperAdmin.ToString());
    //
    //     if (superAdminRole is null)
    //     {
    //         return false;
    //     }
    //
    //     var existingRolePermission = await dbContext.RolePermissions.AnyAsync(x =>
    //         x.AuthorizableRoleId == superAdminRole.Id &&
    //         x.AuthorizablePermissionId == superAdminPermission.Id);
    //
    //     if (existingRolePermission is false)
    //     {
    //         var rp = new RolePermission
    //         {
    //             AuthorizableRoleId = superAdminRole.Id,
    //             AuthorizablePermissionId = superAdminPermission.Id,
    //             CreatedById = superAdmin.Id,
    //             CreatedAtUtc = dateTimeProvider.CurrentUtcTime
    //         };
    //
    //         await dbContext.RolePermissions.AddAsync(rp);
    //         await dbContext.SaveChangesAsync();
    //     }
    //
    //     var superAdminUserRole = await dbContext.UserRoles.AnyAsync(x =>
    //         x.UserId == superAdmin.Id && x.AuthorizableRoleId == superAdminRole.Id);
    //
    //     if (superAdminUserRole)
    //     {
    //         return false;
    //     }
    //
    //     var superAdminUserRoleForAssign = new UserRole
    //     {
    //         AuthorizableRoleId = superAdminRole.Id,
    //         UserId = superAdmin.Id,
    //         CreatedById = superAdmin.Id,
    //         UpdatedById = null,
    //         CreatedAtUtc = dateTimeProvider.CurrentUtcTime
    //     };
    //
    //     await dbContext.UserRoles.AddAsync(superAdminUserRoleForAssign);
    //     await dbContext.SaveChangesAsync();
    //     return true;
    // }
    //
    // private static async Task<bool> CheckBeforeSeed(AcmDbContext dbContext)
    // {
    //     if (EF.IsDesignTime)
    //     {
    //         return false;
    //     }
    //
    //     var canConnect = await dbContext.Database.CanConnectAsync();
    //
    //     if (canConnect is false)
    //     {
    //         return false;
    //     }
    //
    //     var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    //
    //     return pendingMigrations.Any() is false;
    // }
    //
    // private static async Task InsertPermissions(AcmDbContext dbContext, DateTimeProvider dateTimeProvider)
    // {
    //     if (EF.IsDesignTime)
    //     {
    //         return;
    //     }
    //
    //     var permissions = Enum.GetNames<MatchablePermission>();
    //
    //     var existingPermissions = await dbContext.AuthorizablePermissions
    //         .Where(e => permissions.Contains(e.Name))
    //         .Select(x => x.Name)
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     var insertablePermissions = permissions
    //         .Where(e => existingPermissions.Contains(e) is false)
    //         .Select(elem => new AuthorizablePermission
    //         {
    //             Name = elem, CreatedAtUtc = dateTimeProvider.CurrentUtcTime,
    //         })
    //         .ToList();
    //
    //     await dbContext.AuthorizablePermissions.AddRangeAsync(insertablePermissions);
    //     await dbContext.SaveChangesAsync();
    // }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>();
        ArgumentNullException.ThrowIfNull(jwtOptions);
    
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
                };
    
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[AcmAppConstants.AccessTokenCookieKey];
                        return Task.CompletedTask;
                    }
                };
            });
    
        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // Tenant policy - requires valid tenant claim
            options.AddPolicy("TenantPolicy", policy =>
                policy.Requirements.Add(new Authorization.Handlers.TenantRequirement()));
    
            // Permission-based policies
            var permissions = new[]
            {
                "users.view", "users.create", "users.edit", "users.delete",
                "roles.view", "roles.create", "roles.edit", "roles.delete",
                "tenants.view", "tenants.edit",
                "dashboard.view", "system.admin"
            };
    
            foreach (var permission in permissions)
            {
                options.AddPolicy($"Permission.{permission}", policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });
    
        return services;
    }

    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var dbUrl = configuration.GetRequiredSection(ConnectionStringOptions.SectionName)
            .GetValue<string>(nameof(ConnectionStringOptions.Db));

        ArgumentException.ThrowIfNullOrEmpty(dbUrl);

        services.AddDbContext<AcmDbContext>(
            opts =>
            {
                if (hostEnvironment.IsDevelopment())
                {
                    opts.LogTo(Console.WriteLine, LogLevel.Information);
                    opts.EnableSensitiveDataLogging();
                }

                if (hostEnvironment.IsProduction())
                {
                    opts.LogTo(Console.WriteLine, LogLevel.Warning);
                }

                opts
                    .UseNpgsql(dbUrl)
                    .UseEnumCheckConstraints();
            });

        return services;
    }
    
}