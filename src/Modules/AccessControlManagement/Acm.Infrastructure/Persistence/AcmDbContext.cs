using Acm.Domain.Entities;
using Common.Domain.Interfaces;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Acm.Infrastructure.Persistence;

public class AcmDbContext : DbContext
{
    public AcmDbContext(DbContextOptions<AcmDbContext> options) : base(options)
    {
    }

    public DbSet<MasterClaim> MasterClaims => Set<MasterClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcmDbContext).Assembly);

        modelBuilder.Entity<MasterClaim>()
            .ToTable("master_claims", "public", (x) => x.ExcludeFromMigrations());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var autoIncrementalEntityInterface = Array.Find(entityType.ClrType.GetInterfaces(), i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseEntity<>));

            if (autoIncrementalEntityInterface is not null)
            {
                const string id = nameof(IBaseEntity<int>.Id);

                var idProperty = entityType.ClrType.GetProperty(id);

                if (idProperty is not null)
                {
                    modelBuilder.Entity(entityType.ClrType).Property(idProperty.PropertyType, id);
                }
            }

            if (entityType.GetTableName() == "__EFMigrationsHistory")
            {
                continue;
            }

            // Set table name to pluralized snake_case
            entityType.SetTableName(entityType.ClrType.Name
                .Pluralize(inputIsKnownToBeSingular: false)
                .Underscore().ToLowerInvariant());

            // Set column names to snake_case
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(property.Name.Underscore().ToLowerInvariant());
            }

            // Set primary keys to snake_case
            foreach (var key in entityType.GetKeys())
            {
                if (key.IsPrimaryKey())
                {
                    key.SetName(key.GetName().Underscore().ToLowerInvariant());
                }
            }

            // Set index names to snake_case
            foreach (var index in entityType.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName().Underscore().ToLowerInvariant());
            }

            // Set foreign key constraint names to snake_case
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName().Underscore().ToLowerInvariant());
            }
        }
    }
}