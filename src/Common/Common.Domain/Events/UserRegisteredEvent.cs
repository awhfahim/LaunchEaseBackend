namespace Common.Domain.Events;

public sealed class UserRegisteredEvent
{
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? ProfilePictureUri { get; set; }
    public required DateTime DateOfBirth { get; set; }
    public string? Address { get; set; }
}