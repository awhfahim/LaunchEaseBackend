using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class RoleManagementRepository : IRoleManagementRepository
{
    
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IPermissionManagementRepository _permissionRepository;

        public RoleManagementRepository(
            IDbConnectionFactory connectionFactory,
            IPermissionManagementRepository permissionRepository)
        {
            _connectionFactory = connectionFactory;
            _permissionRepository = permissionRepository;
        }

        #region Role CRUD Operations

        public async Task<IEnumerable<RoleDto>> GetTenantRolesAsync(Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at,
                       COUNT(ur.user_id) as user_count,
                       COUNT(rc.id) as permission_count
                FROM roles r
                LEFT JOIN user_roles ur ON r.id = ur.role_id
                LEFT JOIN role_claims rc ON r.id = rc.role_id
                WHERE r.tenant_id = @TenantId
                GROUP BY r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at
                ORDER BY r.name";

            return await connection.QueryAsync<RoleDto>(sql, new { TenantId = tenantId });
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at,
                       COUNT(ur.user_id) as user_count,
                       COUNT(rc.id) as permission_count
                FROM roles r
                LEFT JOIN user_roles ur ON r.id = ur.role_id
                LEFT JOIN role_claims rc ON r.id = rc.role_id
                WHERE r.id = @RoleId
                GROUP BY r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at";

            return await connection.QuerySingleOrDefaultAsync<RoleDto>(sql, new { RoleId = roleId });
        }

        public async Task<RoleDto?> GetRoleByNameAsync(Guid tenantId, string roleName)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at,
                       COUNT(ur.user_id) as user_count,
                       COUNT(rc.id) as permission_count
                FROM roles r
                LEFT JOIN user_roles ur ON r.id = ur.role_id
                LEFT JOIN role_claims rc ON r.id = rc.role_id
                WHERE r.tenant_id = @TenantId AND r.name = @RoleName
                GROUP BY r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at";

            return await connection.QuerySingleOrDefaultAsync<RoleDto>(sql, new { TenantId = tenantId, RoleName = roleName });
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO roles (id, tenant_id, name, description, created_at, updated_at)
                VALUES (@Id, @TenantId, @Name, @Description, @CreatedAt, @UpdatedAt)
                RETURNING id, tenant_id, name, description, created_at, updated_at";

            var role = await connection.QuerySingleAsync<RoleDto>(sql, new
            {
                Id = id,
                createRoleDto.TenantId,
                createRoleDto.Name,
                createRoleDto.Description,
                CreatedAt = now,
                UpdatedAt = now
            });

            role.UserCount = 0;
            role.PermissionCount = 0;

            return role;
        }

        public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleDto updateRoleDto)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                UPDATE roles 
                SET name = @Name, 
                    description = @Description, 
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, tenant_id, name, description, created_at, updated_at";

            var role = await connection.QuerySingleAsync<RoleDto>(sql, new
            {
                Id = roleId,
                updateRoleDto.Name,
                updateRoleDto.Description,
                UpdatedAt = DateTime.UtcNow
            });

            // Get counts
            const string countSql = @"
                SELECT COUNT(ur.user_id) as user_count, COUNT(rc.id) as permission_count
                FROM roles r
                LEFT JOIN user_roles ur ON r.id = ur.role_id
                LEFT JOIN role_claims rc ON r.id = rc.role_id
                WHERE r.id = @RoleId
                GROUP BY r.id";

            var counts = await connection.QuerySingleOrDefaultAsync<(int UserCount, int PermissionCount)>(countSql, new { RoleId = roleId });
            role.UserCount = counts.UserCount;
            role.PermissionCount = counts.PermissionCount;

            return role;
        }

        public async Task DeleteRoleAsync(Guid roleId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Remove role assignments from users
                const string deleteUserRolesSql = @"DELETE FROM user_roles WHERE role_id = @RoleId";
                await connection.ExecuteAsync(deleteUserRolesSql, new { RoleId = roleId }, transaction);

                // Remove role permissions
                const string deleteRoleClaimsSql = @"DELETE FROM role_claims WHERE role_id = @RoleId";
                await connection.ExecuteAsync(deleteRoleClaimsSql, new { RoleId = roleId }, transaction);

                // Delete the role
                const string deleteRoleSql = @"DELETE FROM roles WHERE id = @RoleId";
                await connection.ExecuteAsync(deleteRoleSql, new { RoleId = roleId }, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Role-User Management

        public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(Guid roleId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT u.id, u.email, u.first_name, u.last_name, u.is_email_confirmed, 
                       u.is_globally_locked, u.last_login_at, u.phone_number, u.is_phone_number_confirmed,
                       u.created_at, u.updated_at, ur.tenant_id
                FROM users u
                JOIN user_roles ur ON u.id = ur.user_id
                WHERE ur.role_id = @RoleId
                ORDER BY u.first_name, u.last_name";

            return await connection.QueryAsync<UserDto>(sql, new { RoleId = roleId });
        }

        public async Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId, Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at,
                       COUNT(DISTINCT ur2.user_id) as user_count,
                       COUNT(DISTINCT rc.id) as permission_count
                FROM roles r
                JOIN user_roles ur ON r.id = ur.role_id
                LEFT JOIN user_roles ur2 ON r.id = ur2.role_id
                LEFT JOIN role_claims rc ON r.id = rc.role_id
                WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId
                GROUP BY r.id, r.tenant_id, r.name, r.description, r.created_at, r.updated_at
                ORDER BY r.name";

            return await connection.QueryAsync<RoleDto>(sql, new { UserId = userId, TenantId = tenantId });
        }

        public async Task AssignUserToRoleAsync(Guid userId, Guid roleId, Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO user_roles (id, user_id, role_id, tenant_id)
                VALUES (@Id, @UserId, @RoleId, @TenantId)
                ON CONFLICT (user_id, role_id, tenant_id) DO NOTHING";

            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            });
        }

        public async Task RemoveUserFromRoleAsync(Guid userId, Guid roleId, Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM user_roles 
                WHERE user_id = @UserId AND role_id = @RoleId AND tenant_id = @TenantId";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            });
        }

        public async Task ReplaceUserRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Remove existing roles for this user in this tenant
                const string deleteSql = @"
                    DELETE FROM user_roles 
                    WHERE user_id = @UserId AND tenant_id = @TenantId";

                await connection.ExecuteAsync(deleteSql, new { UserId = userId, TenantId = tenantId }, transaction);

                // Add new roles
                if (roleIds.Any())
                {
                    const string insertSql = @"
                        INSERT INTO user_roles (id, user_id, role_id, tenant_id)
                        SELECT gen_random_uuid(), @UserId, unnest(@RoleIds::uuid[]), @TenantId";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        UserId = userId,
                        RoleIds = roleIds.ToArray(),
                        TenantId = tenantId
                    }, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Role Permission Management
        public async Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<string> permissions)
        {
            await _permissionRepository.AssignPermissionsToRoleAsync(roleId, permissions);
        }

        public async Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<string> permissions)
        {
            await _permissionRepository.RemovePermissionsFromRoleAsync(roleId, permissions);
        }

        public async Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions)
        {
            await _permissionRepository.ReplaceRolePermissionsAsync(roleId, permissions);
        }

        #endregion

        #region Role Templates

        public async Task<RoleDto> CreateRoleFromTemplateAsync(Guid tenantId, RoleTemplateType templateType, string roleName)
        {
            var template = GetRoleTemplate(templateType);
            
            var createRoleDto = new CreateRoleDto
            {
                TenantId = tenantId,
                Name = roleName,
                Description = template.Description
            };

            var role = await CreateRoleAsync(createRoleDto);
            
            if (template.Permissions.Any())
            {
                await AssignPermissionsToRoleAsync(role.Id, template.Permissions);
            }

            return role;
        }

        public async Task<IEnumerable<RoleTemplateDto>> GetAvailableRoleTemplatesAsync()
        {
            return await Task.FromResult(new[]
            {
                new RoleTemplateDto
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
                new RoleTemplateDto
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
                new RoleTemplateDto
                {
                    Type = RoleTemplateType.Viewer,
                    Name = "Viewer",
                    Description = "Read-only access to most resources",
                    Permissions = new[]
                    {
                        "users.view", "roles.view", "tenant.settings.view", "dashboard.view", "reports.view"
                    }
                },
                new RoleTemplateDto
                {
                    Type = RoleTemplateType.BasicUser,
                    Name = "Basic User",
                    Description = "Basic access with minimal permissions",
                    Permissions = new[]
                    {
                        "dashboard.view"
                    }
                }
            });
        }

        private static RoleTemplateDto GetRoleTemplate(RoleTemplateType templateType)
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

        #endregion

        #region Validation

        public async Task<bool> RoleExistsAsync(Guid tenantId, string roleName)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT COUNT(*) > 0 FROM roles 
                WHERE tenant_id = @TenantId AND name = @RoleName";

            return await connection.QuerySingleAsync<bool>(sql, new { TenantId = tenantId, RoleName = roleName });
        }

        public async Task<bool> CanDeleteRoleAsync(Guid roleId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();

            const string sql = @"
                SELECT COUNT(*) = 0 FROM user_roles 
                WHERE role_id = @RoleId";

            return await connection.QuerySingleAsync<bool>(sql, new { RoleId = roleId });
        }

        #endregion
}