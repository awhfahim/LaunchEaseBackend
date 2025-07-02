namespace Common.Application.Options;

public class EmailSettings
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public required bool EnableSsl { get; set; }
}