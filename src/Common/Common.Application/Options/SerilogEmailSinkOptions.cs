using System.ComponentModel.DataAnnotations;

namespace Common.Application.Options;

public class SerilogEmailSinkOptions
{
    public const string SectionName = "SerilogEmailSinkOptions";
    [Required] public required string EmailFrom { get; init; }
    [Required] public required string EmailTo { get; init; }
    [Required] public required string EmailSubject { get; init; }
    [Required] public required string SmtpHost { get; init; }
    [Required] public required int SmtpPort { get; init; }
    [Required] public required string SmtpUsername { get; init; }
    [Required] public required string SmtpPassword { get; init; }
    [Required] public required string MinimumLogLevel { get; init; }
}