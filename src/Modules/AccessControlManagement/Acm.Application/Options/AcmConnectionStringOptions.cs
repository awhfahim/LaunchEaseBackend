using System.ComponentModel.DataAnnotations;

namespace Acm.Application.Options;

public record AcmConnectionStringOptions
{
    public const string SectionName = "AcmConnectionStringOptions";
    [Required] public required string AcmDb { get; init; }
    [Required] public required string StackExchangeRedisUrl { get; init; }
}