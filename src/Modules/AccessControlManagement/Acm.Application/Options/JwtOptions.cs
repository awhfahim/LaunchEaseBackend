using System.ComponentModel.DataAnnotations;

namespace Acm.Application.Options;

public record JwtOptions
{
    public const string SectionName = "JwtOptions";
    [Required] public required string Secret { get; init; }
    [Required] public required uint AccessTokenExpiryMinutes { get; init; }
    [Required] public required uint RefreshTokenExpiryMinutes { get; init; }
}