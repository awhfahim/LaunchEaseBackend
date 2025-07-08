using Acm.Application.Services;
using Acm.Application.Services.Implementations;

namespace Acm.Application.DataTransferObjects
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    public class CreateRoleDto
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }
        public bool IsGloballyLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsPhoneNumberConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? TenantId { get; set; } // For user-role context
    }

    public class AssignRoleDto
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid TenantId { get; set; }
    }

    public class ReplaceUserRolesDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
    }

    public class CreateRoleFromTemplateDto
    {
        public Guid TenantId { get; set; }
        public RoleTemplateType TemplateType { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class RoleWithPermissionsDto
    {
        public RoleDto Role { get; set; } = new();
        public List<RolePermissionDto> Permissions { get; set; } = new();
    }

    public class RoleSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
