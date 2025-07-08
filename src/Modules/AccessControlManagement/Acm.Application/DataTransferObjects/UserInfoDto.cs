namespace Acm.Application.DataTransferObjects;

public class UserInfoDto
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
    public required Guid TenantId { get; set; }
    public required ICollection<string> Roles { get; set; }
    public required ICollection<string> Permissions { get; set; }
}