using System.Collections.Concurrent;
using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// Keeps the notices of Teams in memory instead of sending them (ADR-0149), as the e-mails; it answers as Teams would
/// for a person without the application of Teams, for a refusal and for Teams out of service.
/// </summary>
internal sealed class RecordingTeamsSender : ITeamsSender
{
    private readonly ConcurrentQueue<TeamsDelivery> _sent = new();

    public IReadOnlyList<TeamsDelivery> Sent => [.. _sent];

    /// <summary>The people without the application of Teams installed.</summary>
    public ConcurrentDictionary<Guid, bool> NotInstalled { get; } = new();

    /// <summary>When on, Teams refuses every notice for good.</summary>
    public bool Refusing { get; set; }

    /// <summary>When on, Teams is out of service, and every try is tried again later.</summary>
    public bool Down { get; set; }

    public Task<TeamsSendResult> SendAsync(TeamsDelivery delivery, CancellationToken cancellationToken)
    {
        if (Down)
        {
            return Task.FromResult(new TeamsSendResult(TeamsSendOutcome.TryAgain, "Microsoft Graph answered 503."));
        }

        if (NotInstalled.ContainsKey(delivery.UserId))
        {
            return Task.FromResult(new TeamsSendResult(TeamsSendOutcome.NotInstalled, "The application of Teams is not installed for the person."));
        }

        if (Refusing)
        {
            return Task.FromResult(new TeamsSendResult(TeamsSendOutcome.Refused, "Microsoft Graph answered 400 BadRequest."));
        }

        _sent.Enqueue(delivery);
        return Task.FromResult(TeamsSendResult.Sent);
    }
}
