namespace Acm.Application.DataTransferObjects;

public record TenantResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public required DateTime CreatedAt { get; set; }
}