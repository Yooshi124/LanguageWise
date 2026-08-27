using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class GmailEmailSenderTests
{
    [Test]
    public async Task SendAsync_BuildsPlainTextMessageUsingAuthenticatedAddress()
    {
        var transport = new RecordingSmtpTransport();
        var options = new SmtpOptions
        {
            Username = "sender@example.com",
            Password = "app-password",
            FromName = "LanguageWise"
        };
        var sender = new GmailEmailSender(Options.Create(options), transport);

        await sender.SendAsync(
            "recipient@example.com",
            new EmailContent("Achievement unlocked", "Congratulations!", false));

        Assert.Multiple(() =>
        {
            Assert.That(sender.IsConfigured, Is.True);
            Assert.That(transport.Message?.From.Mailboxes.Single().Address, Is.EqualTo(options.Username));
            Assert.That(transport.Message?.To.Mailboxes.Single().Address, Is.EqualTo("recipient@example.com"));
            Assert.That(transport.Message?.Subject, Is.EqualTo("Achievement unlocked"));
            Assert.That(transport.Message?.TextBody, Is.EqualTo("Congratulations!"));
            Assert.That(transport.Options, Is.SameAs(options));
        });
    }

    [Test]
    public void IsConfigured_WhenPasswordIsMissing_ReturnsFalse()
    {
        var sender = new GmailEmailSender(
            Options.Create(new SmtpOptions { Username = "sender@example.com" }),
            new RecordingSmtpTransport());

        Assert.That(sender.IsConfigured, Is.False);
    }

    private sealed class RecordingSmtpTransport : ISmtpTransport
    {
        internal MimeMessage? Message { get; private set; }
        internal SmtpOptions? Options { get; private set; }

        public Task SendAsync(
            MimeMessage message,
            SmtpOptions options,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            Options = options;
            return Task.CompletedTask;
        }
    }
}