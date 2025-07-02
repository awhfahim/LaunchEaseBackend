using System.ComponentModel.DataAnnotations;

namespace Common.Application.Options;

public record CommonConnectionStringOptions
{
    public const string SectionName = "CommonConnectionStringOptions";
    [Required] public required string CommonDb { get; init; }
    [Required] public required string StackExchangeRedisUrl { get; init; }
}
