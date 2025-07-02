namespace Acm.Application.Options;

public class AdminUserSeedOptions
{
    public const string SectionName = "AdminUserSeedOptions";
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public required string Password { get; set; }
    public required bool IsTemporaryPassword { get; set; }
    public required char UserType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required long UserStatusId { get; set; }
}
