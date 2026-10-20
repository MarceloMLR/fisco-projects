using System.Collections.Concurrent;
using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Api.Tests;

/// <summary>A sender that keeps in memory what the queue sent, or fails while the test says so.</summary>
internal sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => [.. _sent];

    public bool Failing { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Failing)
        {
            throw new InvalidOperationException("The mailbox is out.");
        }

        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
