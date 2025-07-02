using System.ComponentModel.DataAnnotations;

namespace Common.Application.Options;

public record AppOptions
{
    public const string SectionName = "AppOptions";
    [Required] public required string[] AllowedOriginsForCors { get; init; }

    [Required, Range(1, ushort.MaxValue)] public required ushort DefaultCacheAbsoluteExpiration { get; init; }

    [Required, Range(1, ushort.MaxValue)] public required ushort DefaultCacheSlidingExpiration { get; init; }
}
