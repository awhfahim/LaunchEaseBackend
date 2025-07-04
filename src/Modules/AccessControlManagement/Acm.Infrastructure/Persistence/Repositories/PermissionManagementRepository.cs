using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class PermissionManagementRepository: IPermissionManagementRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PermissionManagementRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        #region Master Claims/Permissions Management

        public async Task<IEnumerable<MasterClaimDto>> GetAllPermissionsAsync()
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT id, claim_type, claim_value, display_name, description, category, 
                       is_tenant_scoped, is_system_permission, created_at, updated_at
                FROM master_claims 
                ORDER BY category, display_name";

            return await connection.QueryAsync<MasterClaimDto>(sql);
        }

        public async Task<IEnumerable<MasterClaimDto>> GetPermissionsByCategoryAsync(string category)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT id, claim_type, claim_value, display_name, description, category, 
                       is_tenant_scoped, is_system_permission, created_at, updated_at
                FROM master_claims 
                WHERE category = @Category
                ORDER BY display_name";

            return await connection.QueryAsync<MasterClaimDto>(sql, new { Category = category });
        }

        public async Task<MasterClaimDto> CreatePermissionAsync(CreatePermissionDto createPermissionDto)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO master_claims (id, claim_type, claim_value, display_name, description, 
                                         category, is_tenant_scoped, is_system_permission, created_at, updated_at)
                VALUES (@Id, @ClaimType, @ClaimValue, @DisplayName, @Description, 
                        @Category, @IsTenantScoped, @IsSystemPermission, @CreatedAt, @UpdatedAt)
                RETURNING id, claim_type, claim_value, display_name, description, category, 
                         is_tenant_scoped, is_system_permission, created_at, updated_at";

            return await connection.QuerySingleAsync<MasterClaimDto>(sql, new
            {
                Id = id,
                ClaimType = createPermissionDto.ClaimType ?? "permission",
                createPermissionDto.ClaimValue,
                createPermissionDto.DisplayName,
                createPermissionDto.Description,
                createPermissionDto.Category,
                createPermissionDto.IsTenantScoped,
                createPermissionDto.IsSystemPermission,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        public async Task<MasterClaimDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto updatePermissionDto)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                UPDATE master_claims 
                SET display_name = @DisplayName, 
                    description = @Description, 
                    category = @Category,
                    is_tenant_scoped = @IsTenantScoped,
                    is_system_permission = @IsSystemPermission,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, claim_type, claim_value, display_name, description, category, 
                         is_tenant_scoped, is_system_permission, created_at, updated_at";

            return await connection.QuerySingleAsync<MasterClaimDto>(sql, new
            {
                Id = permissionId,
                updatePermissionDto.DisplayName,
                updatePermissionDto.Description,
                updatePermissionDto.Category,
                updatePermissionDto.IsTenantScoped,
                updatePermissionDto.IsSystemPermission,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public async Task DeletePermissionAsync(Guid permissionId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // First remove this permission from all roles and users
                const string deleteFromRolesSql = @"
                    DELETE FROM role_claims 
                    WHERE claim_value = (SELECT claim_value FROM master_claims WHERE id = @PermissionId)";

                const string deleteFromUsersSql = @"
                    DELETE FROM user_claims 
                    WHERE claim_value = (SELECT claim_value FROM master_claims WHERE id = @PermissionId)";

                const string deleteMasterClaimSql = @"
                    DELETE FROM master_claims WHERE id = @PermissionId";

                await connection.ExecuteAsync(deleteFromRolesSql, new { PermissionId = permissionId }, transaction);
                await connection.ExecuteAsync(deleteFromUsersSql, new { PermissionId = permissionId }, transaction);
                await connection.ExecuteAsync(deleteMasterClaimSql, new { PermissionId = permissionId }, transaction);

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

        public async Task<IEnumerable<RolePermissionDto>> GetRolePermissionsAsync(Guid roleId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT rc.id, rc.role_id, rc.claim_type, rc.claim_value,
                       mc.display_name, mc.description, mc.category, mc.is_tenant_scoped, mc.is_system_permission
                FROM role_claims rc
                JOIN master_claims mc ON rc.claim_value = mc.claim_value
                WHERE rc.role_id = @RoleId
                ORDER BY mc.category, mc.display_name";

            return await connection.QueryAsync<RolePermissionDto>(sql, new { RoleId = roleId });
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<string> claimValues)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            // Only insert permissions that don't already exist for this role
            const string sql = @"
                INSERT INTO role_claims (id, role_id, claim_type, claim_value)
                SELECT gen_random_uuid(), @RoleId, 'permission', unnest(@ClaimValues::text[])
                WHERE NOT EXISTS (
                    SELECT 1 FROM role_claims 
                    WHERE role_id = @RoleId AND claim_value = ANY(@ClaimValues::text[])
                )";

            await connection.ExecuteAsync(sql, new { RoleId = roleId, ClaimValues = claimValues.ToArray() });
        }

        public async Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<string> claimValues)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                DELETE FROM role_claims 
                WHERE role_id = @RoleId AND claim_value = ANY(@ClaimValues::text[])";

            await connection.ExecuteAsync(sql, new { RoleId = roleId, ClaimValues = claimValues.ToArray() });
        }

        public async Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<string> claimValues)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Remove all existing permissions
                const string deleteSql = @"DELETE FROM role_claims WHERE role_id = @RoleId";
                await connection.ExecuteAsync(deleteSql, new { RoleId = roleId }, transaction);

                // Add new permissions
                if (claimValues.Any())
                {
                    const string insertSql = @"
                        INSERT INTO role_claims (id, role_id, claim_type, claim_value)
                        SELECT gen_random_uuid(), @RoleId, 'permission', unnest(@ClaimValues::text[])";

                    await connection.ExecuteAsync(insertSql, new { RoleId = roleId, ClaimValues = claimValues.ToArray() }, transaction);
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

        #region User Permission Management

        public async Task<IEnumerable<UserPermissionDto>> GetUserDirectPermissionsAsync(Guid userId, Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT uc.id, uc.user_id, uc.tenant_id, uc.claim_type, uc.claim_value,
                       mc.display_name, mc.description, mc.category, mc.is_tenant_scoped, mc.is_system_permission
                FROM user_claims uc
                JOIN master_claims mc ON uc.claim_value = mc.claim_value
                WHERE uc.user_id = @UserId AND uc.tenant_id = @TenantId
                ORDER BY mc.category, mc.display_name";

            return await connection.QueryAsync<UserPermissionDto>(sql, new { UserId = userId, TenantId = tenantId });
        }

        public async Task<IEnumerable<UserPermissionDto>> GetUserAllPermissionsAsync(Guid userId, Guid tenantId)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT DISTINCT mc.claim_value, mc.display_name, mc.description, mc.category, 
                       mc.is_tenant_scoped, mc.is_system_permission, 'role' as source
                FROM user_roles ur
                JOIN role_claims rc ON ur.role_id = rc.role_id
                JOIN master_claims mc ON rc.claim_value = mc.claim_value
                WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId
                
                UNION
                
                SELECT DISTINCT mc.claim_value, mc.display_name, mc.description, mc.category, 
                       mc.is_tenant_scoped, mc.is_system_permission, 'direct' as source
                FROM user_claims uc
                JOIN master_claims mc ON uc.claim_value = mc.claim_value
                WHERE uc.user_id = @UserId AND uc.tenant_id = @TenantId
                
                ORDER BY category, display_name";

            return await connection.QueryAsync<UserPermissionDto>(sql, new { UserId = userId, TenantId = tenantId });
        }

        public async Task AssignDirectPermissionsToUserAsync(Guid userId, Guid tenantId, IEnumerable<string> claimValues)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                INSERT INTO user_claims (id, user_id, tenant_id, claim_type, claim_value)
                SELECT gen_random_uuid(), @UserId, @TenantId, 'permission', unnest(@ClaimValues::text[])
                WHERE NOT EXISTS (
                    SELECT 1 FROM user_claims 
                    WHERE user_id = @UserId AND tenant_id = @TenantId AND claim_value = ANY(@ClaimValues::text[])
                )";

            await connection.ExecuteAsync(sql, new { 
                UserId = userId, 
                TenantId = tenantId, 
                ClaimValues = claimValues.ToArray() 
            });
        }

        public async Task RemoveDirectPermissionsFromUserAsync(Guid userId, Guid tenantId, IEnumerable<string> claimValues)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                DELETE FROM user_claims 
                WHERE user_id = @UserId AND tenant_id = @TenantId AND claim_value = ANY(@ClaimValues::text[])";

            await connection.ExecuteAsync(sql, new { 
                UserId = userId, 
                TenantId = tenantId, 
                ClaimValues = claimValues.ToArray() 
            });
        }

        #endregion

        #region Permission Validation

        public async Task<bool> UserHasPermissionAsync(Guid userId, Guid tenantId, string permission)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT COUNT(*) > 0 FROM (
                    -- Check business owner permission (bypasses all tenant restrictions)
                    SELECT 1 FROM user_roles ur
                    JOIN role_claims rc ON ur.role_id = rc.role_id
                    WHERE ur.user_id = @UserId AND rc.claim_value = 'business.owner'
                    
                    UNION
                    
                    -- Check from role permissions
                    SELECT 1 FROM user_roles ur
                    JOIN role_claims rc ON ur.role_id = rc.role_id
                    WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId AND rc.claim_value = @Permission
                    
                    UNION
                    
                    -- Check from direct user permissions
                    SELECT 1 FROM user_claims uc
                    WHERE uc.user_id = @UserId AND uc.tenant_id = @TenantId AND uc.claim_value = @Permission
                ) permissions";

            return await connection.QuerySingleAsync<bool>(sql, new { 
                UserId = userId, 
                TenantId = tenantId, 
                Permission = permission 
            });
        }

        public async Task<bool> UserHasAnyPermissionAsync(Guid userId, Guid tenantId, IEnumerable<string> permissions)
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                SELECT COUNT(*) > 0 FROM (
                    -- Check business owner permission
                    SELECT 1 FROM user_roles ur
                    JOIN role_claims rc ON ur.role_id = rc.role_id
                    WHERE ur.user_id = @UserId AND rc.claim_value = 'business.owner'
                    
                    UNION
                    
                    -- Check from role permissions
                    SELECT 1 FROM user_roles ur
                    JOIN role_claims rc ON ur.role_id = rc.role_id
                    WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId AND rc.claim_value = ANY(@Permissions::text[])
                    
                    UNION
                    
                    -- Check from direct user permissions
                    SELECT 1 FROM user_claims uc
                    WHERE uc.user_id = @UserId AND uc.tenant_id = @TenantId AND uc.claim_value = ANY(@Permissions::text[])
                ) permissions";

            return await connection.QuerySingleAsync<bool>(sql, new { 
                UserId = userId, 
                TenantId = tenantId, 
                Permissions = permissions.ToArray() 
            });
        }

        public async Task<bool> UserHasAllPermissionsAsync(Guid userId, Guid tenantId, IEnumerable<string> permissions)
        {
            var permissionArray = permissions.ToArray();
            if (!permissionArray.Any()) return true;

            await using var connection = await _connectionFactory.OpenConnectionAsync();
            
            const string sql = @"
                WITH user_permissions AS (
                    -- Business owner has all permissions
                    SELECT mc.claim_value FROM master_claims mc
                    WHERE EXISTS (
                        SELECT 1 FROM user_roles ur
                        JOIN role_claims rc ON ur.role_id = rc.role_id
                        WHERE ur.user_id = @UserId AND rc.claim_value = 'business.owner'
                    )
                    
                    UNION
                    
                    -- Role permissions
                    SELECT rc.claim_value FROM user_roles ur
                    JOIN role_claims rc ON ur.role_id = rc.role_id
                    WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId
                    
                    UNION
                    
                    -- Direct user permissions
                    SELECT uc.claim_value FROM user_claims uc
                    WHERE uc.user_id = @UserId AND uc.tenant_id = @TenantId
                )
                SELECT COUNT(DISTINCT required_permission) = @PermissionCount
                FROM unnest(@Permissions::text[]) AS required_permission
                WHERE required_permission IN (SELECT claim_value FROM user_permissions)";

            return await connection.QuerySingleAsync<bool>(sql, new { 
                UserId = userId, 
                TenantId = tenantId, 
                Permissions = permissionArray,
                PermissionCount = permissionArray.Length
            });
        }

        #endregion
    }