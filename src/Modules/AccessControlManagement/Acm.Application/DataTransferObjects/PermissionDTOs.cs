namespace Acm.Application.DataTransferObjects
{
    public class MasterClaimDto
    {
        public Guid Id { get; init; }
        public required string ClaimType { get; init; } = "permission";
        public required string ClaimValue { get; init; }
        public required string DisplayName { get; init; }
        public required string? Description { get; init; }
    }

    public class CreatePermissionDto
    {
        public string? ClaimType { get; set; } = "permission";
        public string ClaimValue { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsTenantScoped { get; set; } = true;
        public bool IsSystemPermission { get; set; } = false;
    }

    public class UpdatePermissionDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsTenantScoped { get; set; } = true;
        public bool IsSystemPermission { get; set; } = false;
    }

    public class RolePermissionDto
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsTenantScoped { get; set; }
        public bool IsSystemPermission { get; set; }
    }

    public class UserPermissionDto
    {
        public Guid? Id { get; set; } // Null for role-based permissions
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsTenantScoped { get; set; }
        public bool IsSystemPermission { get; set; }
        public string Source { get; set; } = string.Empty; // "role" or "direct"
    }

    public class AssignPermissionsDto
    {
        public List<string> ClaimValues { get; set; } = new();
    }

    public class PermissionValidationDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Permission { get; set; } = string.Empty;
        public bool HasPermission { get; set; }
    }

    public class BulkPermissionValidationDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool HasAllPermissions { get; set; }
        public bool HasAnyPermission { get; set; }
        public List<string> GrantedPermissions { get; set; } = new();
        public List<string> MissingPermissions { get; set; } = new();
    }

    public class PermissionsByCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int PermissionCount { get; set; }
        public List<MasterClaimDto> Permissions { get; set; } = new();
    }
}
