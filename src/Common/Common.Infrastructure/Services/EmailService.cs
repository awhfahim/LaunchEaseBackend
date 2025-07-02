using System.Net;
using System.Net.Mail;
using Common.Application.Options;
using Common.Application.Providers;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpOptions _smtpOptions;

    public EmailService(IOptions<SmtpOptions> emailSettings)
    {
        _smtpOptions = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password),
            EnableSsl = _smtpOptions.UseSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_smtpOptions.SenderEmail, _smtpOptions.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true // Set to true if sending HTML email
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}