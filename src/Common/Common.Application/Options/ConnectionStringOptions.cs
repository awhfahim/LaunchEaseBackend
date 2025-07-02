using System.ComponentModel.DataAnnotations;

namespace Common.Application.Options;

public record ConnectionStringOptions
{
    public const string SectionName = "ConnectionStringOptions";
    [Required] public required string Db { get; init; }
    [Required] public required string StackExchangeRedisUrl { get; init; }
}
