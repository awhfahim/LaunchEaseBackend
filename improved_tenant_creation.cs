// Enhanced Tenant Creation with Dynamic Permission System
using LaunchEase.Common.Application.Services;

async Task<Guid> CreateTenantWithDynamicPermissionsAsync(
    Tenant tenant, 
    User adminUser, 
    string adminRoleName = "Tenant Administrator",
    RoleTemplateType adminRoleTemplate = RoleTemplateType.TenantAdmin)
{
    await using var connection = await _connectionFactory.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // 1. Create Tenant
        const string createTenantSql = @"
            INSERT INTO tenants (id, name, slug, logo_url, contact_email, created_at, updated_at)
            VALUES (@Id, @Name, @Slug, @LogoUrl, @ContactEmail, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(createTenantSql, tenant, transaction: transaction);

        // 2. Create Admin User (global user, not tenant-specific)
        const string createAdminUserSql = @"
            INSERT INTO users (id, email, first_name, last_name, password_hash, security_stamp,
                              is_email_confirmed, is_globally_locked, global_lockout_end, global_access_failed_count, 
                              last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at)
            VALUES (@Id, @Email, @FirstName, @LastName, @PasswordHash, @SecurityStamp,
                    @IsEmailConfirmed, @IsGloballyLocked, @GlobalLockoutEnd, @GlobalAccessFailedCount, 
                    @LastLoginAt, @PhoneNumber, @IsPhoneNumberConfirmed, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(createAdminUserSql, adminUser, transaction: transaction);

        // 3. Create User-Tenant relationship
        const string createUserTenantSql = @"
            INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at)
            VALUES (@Id, @UserId, @TenantId, @IsActive, @JoinedAt)";

        await connection.ExecuteAsync(createUserTenantSql, new
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            TenantId = tenant.Id,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        }, transaction: transaction);

        // 4. Create Admin Role using Role Management Service
        var roleService = new RoleManagementService(_connectionFactory, _permissionService);
        
        // Create role from template with all tenant admin permissions
        var adminRole = await CreateRoleFromTemplateInTransactionAsync(
            connection, transaction, tenant.Id, adminRoleName, adminRoleTemplate);

        // 5. Assign Admin Role to User
        const string createUserRoleSql = @"
            INSERT INTO user_roles (id, user_id, role_id, tenant_id)
            VALUES (@Id, @UserId, @RoleId, @TenantId)";

        await connection.ExecuteAsync(createUserRoleSql, new
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            TenantId = tenant.Id
        }, transaction: transaction);

        await transaction.CommitAsync();
        
        _logger.LogInformation("Successfully created tenant {TenantName} with admin user {AdminEmail} and role template {RoleTemplate}", 
            tenant.Name, adminUser.Email, adminRoleTemplate);

        return tenant.Id;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Failed to create tenant {TenantName} with admin user {AdminEmail}. Transaction rolled back.", 
            tenant.Name, adminUser.Email);
        throw;
    }
}

async Task<RoleDto> CreateRoleFromTemplateInTransactionAsync(
    IDbConnection connection, 
    IDbTransaction transaction, 
    Guid tenantId, 
    string roleName, 
    RoleTemplateType templateType)
{
    var now = DateTime.UtcNow;
    var roleId = Guid.NewGuid();

    // Get template permissions
    var template = GetRoleTemplate(templateType);

    // Create role
    const string createRoleSql = @"
        INSERT INTO roles (id, tenant_id, name, description, created_at, updated_at)
        VALUES (@Id, @TenantId, @Name, @Description, @CreatedAt, @UpdatedAt)";

    await connection.ExecuteAsync(createRoleSql, new
    {
        Id = roleId,
        TenantId = tenantId,
        Name = roleName,
        Description = template.Description,
        CreatedAt = now,
        UpdatedAt = now
    }, transaction: transaction);

    // Assign permissions from master_claims table
    if (template.Permissions.Any())
    {
        const string assignPermissionsSql = @"
            INSERT INTO role_claims (id, role_id, claim_type, claim_value)
            SELECT gen_random_uuid(), @RoleId, 'permission', unnest(@ClaimValues::text[])
            WHERE EXISTS (
                SELECT 1 FROM master_claims mc 
                WHERE mc.claim_value = ANY(@ClaimValues::text[])
            )";

        await connection.ExecuteAsync(assignPermissionsSql, new
        {
            RoleId = roleId,
            ClaimValues = template.Permissions
        }, transaction: transaction);
    }

    return new RoleDto
    {
        Id = roleId,
        TenantId = tenantId,
        Name = roleName,
        Description = template.Description,
        CreatedAt = now,
        UpdatedAt = now,
        UserCount = 0,
        PermissionCount = template.Permissions.Length
    };
}

static RoleTemplateDto GetRoleTemplate(RoleTemplateType templateType)
{
    return templateType switch
    {
        RoleTemplateType.TenantAdmin => new RoleTemplateDto
        {
            Type = RoleTemplateType.TenantAdmin,
            Name = "Tenant Administrator",
            Description = "Full administrative access within the tenant",
            Permissions = new[]
            {
                "users.view", "users.create", "users.edit", "users.delete", "users.invite", "users.manage.roles",
                "roles.view", "roles.create", "roles.edit", "roles.delete", "roles.manage.permissions",
                "tenant.settings.view", "tenant.settings.edit",
                "dashboard.view", "reports.view", "reports.export",
                "authentication.view", "authentication.edit", "authorization.view", "authorization.edit",
                "audit.view"
            }
        },
        RoleTemplateType.UserManager => new RoleTemplateDto
        {
            Type = RoleTemplateType.UserManager,
            Name = "User Manager",
            Description = "Manage users and their basic permissions",
            Permissions = new[]
            {
                "users.view", "users.create", "users.edit", "users.invite", "users.manage.roles",
                "roles.view", "dashboard.view"
            }
        },
        RoleTemplateType.Viewer => new RoleTemplateDto
        {
            Type = RoleTemplateType.Viewer,
            Name = "Viewer",
            Description = "Read-only access to most resources",
            Permissions = new[]
            {
                "users.view", "roles.view", "tenant.settings.view", "dashboard.view", "reports.view"
            }
        },
        RoleTemplateType.BasicUser => new RoleTemplateDto
        {
            Type = RoleTemplateType.BasicUser,
            Name = "Basic User",
            Description = "Basic access with minimal permissions",
            Permissions = new[] { "dashboard.view" }
        },
        _ => throw new ArgumentException($"Unknown role template type: {templateType}")
    };
}

async Task CreateDefaultAdminClaimsAsync(IDbConnection connection, IDbTransaction transaction, Guid roleId)
{
    // Get default admin permissions from master_claims table for tenant-scoped permissions
    const string getDefaultPermissionsSql = @"
        SELECT claim_value 
        FROM master_claims 
        WHERE is_tenant_scoped = true 
        AND category IN ('users', 'roles', 'tenant', 'dashboard', 'auth', 'reports', 'audit')
        AND claim_value NOT LIKE 'global.%'";

    var defaultPermissions = await connection.QueryAsync<string>(getDefaultPermissionsSql, transaction: transaction);

    if (defaultPermissions.Any())
    {
        const string insertClaimSql = @"
            INSERT INTO role_claims (id, role_id, claim_type, claim_value)
            VALUES (@Id, @RoleId, @ClaimType, @ClaimValue)";

        var claimInserts = defaultPermissions.Select(permission => new
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            ClaimType = "permission",
            ClaimValue = permission
        });

        await connection.ExecuteAsync(insertClaimSql, claimInserts, transaction: transaction);
    }
}

// Alternative approach using a single SQL statement with CTEs for better performance
async Task<Guid> CreateAsyncOptimized(Tenant tenant, Role adminRole, User adminUser, UserRole userRole)
{
    await using var connection = await _connectionFactory.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        const string sql = @"
            WITH tenant_insert AS (
                INSERT INTO tenants (id, name, slug, logo_url, contact_email, created_at, updated_at)
                VALUES (@TenantId, @TenantName, @TenantSlug, @TenantLogoUrl, @TenantContactEmail, @TenantCreatedAt, @TenantUpdatedAt)
                RETURNING id
            ),
            role_insert AS (
                INSERT INTO roles (id, tenant_id, name, description, created_at, updated_at)
                SELECT @RoleId, @RoleTenantId, @RoleName, @RoleDescription, @RoleCreatedAt, @RoleUpdatedAt
                FROM tenant_insert
                RETURNING id
            ),
            user_insert AS (
                INSERT INTO users (id, email, first_name, last_name, password_hash, security_stamp,
                                  is_email_confirmed, is_globally_locked, global_lockout_end, global_access_failed_count, 
                                  last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at)
                VALUES (@UserId, @UserEmail, @UserFirstName, @UserLastName, @UserPasswordHash, @UserSecurityStamp,
                        @UserIsEmailConfirmed, @UserIsGloballyLocked, @UserGlobalLockoutEnd, @UserGlobalAccessFailedCount, 
                        @UserLastLoginAt, @UserPhoneNumber, @UserIsPhoneNumberConfirmed, @UserCreatedAt, @UserUpdatedAt)
                RETURNING id
            )
            INSERT INTO user_roles (id, user_id, role_id)
            SELECT @UserRoleId, user_insert.id, role_insert.id
            FROM user_insert, role_insert";

        var parameters = new
        {
            // Tenant parameters
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TenantSlug = tenant.Slug,
            TenantLogoUrl = tenant.LogoUrl,
            TenantContactEmail = tenant.ContactEmail,
            TenantCreatedAt = tenant.CreatedAt,
            TenantUpdatedAt = tenant.UpdatedAt,
            
            // Role parameters
            RoleId = adminRole.Id,
            RoleTenantId = adminRole.TenantId,
            RoleName = adminRole.Name,
            RoleDescription = adminRole.Description,
            RoleCreatedAt = adminRole.CreatedAt,
            RoleUpdatedAt = adminRole.UpdatedAt,
            
            // User parameters
            UserId = adminUser.Id,
            UserEmail = adminUser.Email,
            UserFirstName = adminUser.FirstName,
            UserLastName = adminUser.LastName,
            UserPasswordHash = adminUser.PasswordHash,
            UserSecurityStamp = adminUser.SecurityStamp,
            UserIsEmailConfirmed = adminUser.IsEmailConfirmed,
            UserIsGloballyLocked = adminUser.IsGloballyLocked,
            UserGlobalLockoutEnd = adminUser.GlobalLockoutEnd,
            UserGlobalAccessFailedCount = adminUser.GlobalAccessFailedCount,
            UserLastLoginAt = adminUser.LastLoginAt,
            UserPhoneNumber = adminUser.PhoneNumber,
            UserIsPhoneNumberConfirmed = adminUser.IsPhoneNumberConfirmed,
            UserCreatedAt = adminUser.CreatedAt,
            UserUpdatedAt = adminUser.UpdatedAt,
            
            // UserRole parameters
            UserRoleId = userRole.Id
        };

        await connection.ExecuteAsync(sql, parameters, transaction: transaction);

        // Still need to add claims separately
        await CreateDefaultAdminClaimsAsync(connection, transaction, adminRole.Id);

        await transaction.CommitAsync();
        
        _logger.LogInformation("Successfully created tenant {TenantName} with admin user {AdminEmail}", 
            tenant.Name, adminUser.Email);

        return tenant.Id;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Failed to create tenant {TenantName} with admin user {AdminEmail}. Transaction rolled back.", 
            tenant.Name, adminUser.Email);
        throw;
    }
}
