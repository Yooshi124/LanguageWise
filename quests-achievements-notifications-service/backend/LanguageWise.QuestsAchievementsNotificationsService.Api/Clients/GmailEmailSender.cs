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

public interface ISmtpTransport
{
    Task SendAsync(
        MimeMessage message,
        SmtpOptions options,
        CancellationToken cancellationToken = default);
}

public sealed class MailKitSmtpTransport : ISmtpTransport
{
    public async Task SendAsync(
        MimeMessage message,
        SmtpOptions options,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(
            options.Host,
            options.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);
        await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

public sealed class GmailEmailSender(
    IOptions<SmtpOptions> options,
    ISmtpTransport smtpTransport) : IEmailSender
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

        await smtpTransport.SendAsync(message, smtpOptions, cancellationToken);
    }
}