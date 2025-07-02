using System.ComponentModel.DataAnnotations;

namespace Common.Application.Options;

public class MinioOptions
{
    public const string SectionName = "MinioOptions";
    [Required] public required string Endpoint { get; init; }
    [Required] public required string DockerEndpoint { get; init; }
    [Required] public required string ExternalEndpoint { get; init; }
    [Required] public required string AccessKey { get; init; }
    [Required] public required string SecretKey { get; init; }
}