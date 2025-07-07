namespace Acm.Application.DataTransferObjects.Response;

public record UserResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public required bool IsEmailConfirmed { get; init; }
    public required bool IsLocked { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public IEnumerable<UserRoleResponse> Roles { get; init; } = [];
}

public record UserRoleResponse
{
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
}