using System.ComponentModel.DataAnnotations;

namespace Acm.Application.Options;

public record GoogleRecaptchaOptions
{
    public const string SectionName = "GoogleRecaptchaOptions";
    [Required] public required string SiteKey { get; init; }
    [Required] public required string SecretKey { get; init; }
    [Required] public required string VerificationEndpoint { get; init; }
}
