namespace Common.Application.Providers;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}