namespace Acm.Application.Services.Implementations
{
    // Supporting types
    public enum RoleTemplateType
    {
        TenantAdmin,
        UserManager,
        Viewer,
        BasicUser
    }

    public class RoleTemplateDto
    {
        public RoleTemplateType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Permissions { get; set; } = Array.Empty<string>();
    }
}
