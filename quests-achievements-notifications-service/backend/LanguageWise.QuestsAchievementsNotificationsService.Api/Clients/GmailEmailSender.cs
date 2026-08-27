using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, EmailContent content, CancellationToken cancellationToken = default);
}

public sealed class GmailEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions smtpOptions = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(smtpOptions.Username)
        && !string.IsNullOrWhiteSpace(smtpOptions.Password);

    public async Task SendAsync(
        string recipient,
        EmailContent content,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtpOptions.FromName, smtpOptions.Username));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = content.Subject;
        message.Body = new TextPart("plain") { Text = content.Body };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            smtpOptions.Host,
            smtpOptions.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);
        await client.AuthenticateAsync(smtpOptions.Username, smtpOptions.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}