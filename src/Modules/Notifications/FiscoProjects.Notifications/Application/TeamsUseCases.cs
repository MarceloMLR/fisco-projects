using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Application;

/// <summary>How the notices reach Teams in this installation (ADR-0149), from the configuration of the application.</summary>
public enum TeamsProvider
{
    /// <summary>Not configured: nothing goes to Teams.</summary>
    None,

    /// <summary>A folder of the disk, outside production: nothing leaves, and every active person may receive.</summary>
    Local,

    /// <summary>Microsoft Graph, with the token of the application: only who signs in by Entra ID receives.</summary>
    Graph,
}

/// <summary>
/// The sending to Teams of the installation: the implementation, the address of the screens and the client of the
/// application in Entra ID, which the package of Teams names.
/// </summary>
public sealed record TeamsSettings(TeamsProvider Provider, string? SiteUrl, string? EntraClientId)
{
    public bool IsAvailable => Provider != TeamsProvider.None;
}

/// <summary>Where the application of Teams of the installation is kept, created on the first reading.</summary>
public interface ITeamsAppStore
{
    /// <summary>The one row, tracked; created when it does not exist yet.</summary>
    Task<TeamsApp> GetAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Where the channels the people chose are kept.</summary>
public interface INoticePreferenceStore
{
    /// <summary>The channel each person chose; who never chose is left out.</summary>
    Task<IReadOnlyDictionary<Guid, ImmediateNoticeChannel>> ChannelsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>The choice of the person, tracked, or null.</summary>
    Task<NoticePreference?> FindAsync(Guid userId, CancellationToken cancellationToken);

    void Add(NoticePreference preference);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Where the notices of the queue of Teams are kept; saved with the queue of the e-mails.</summary>
public interface ITeamsNoticeStore
{
    void Add(TeamsNotice notice);

    /// <summary>The notices whose turn came, the oldest first, tracked.</summary>
    Task<IReadOnlyList<TeamsNotice>> DueAsync(DateTime now, int take, CancellationToken cancellationToken);

    /// <summary>Removes the notices sent or failed before the moment; how many.</summary>
    Task<int> RemoveOldAsync(DateTime before, CancellationToken cancellationToken);

    /// <summary>Saves the reservation or the result of a notice; false when another process changed it first.</summary>
    Task<bool> TrySaveAsync(TeamsNotice notice, CancellationToken cancellationToken);
}

/// <summary>What a notice says in Teams: the type of the manifest, the values of its text, where it is and its preview.</summary>
public sealed record TeamsMessage(
    string ActivityType, IReadOnlyList<KeyValuePair<string, string>> Parameters, string Topic, string Preview, string Path);

/// <summary>A notice to one person: the application of the installation, the person and what it says.</summary>
public sealed record TeamsDelivery(Guid ManifestId, Guid UserId, string Name, string? EntraObjectId, TeamsMessage Message);

/// <summary>How a try to send a notice to Teams ended.</summary>
public enum TeamsSendOutcome
{
    Sent,

    /// <summary>For a while, as Teams throttling or out of service: another try later.</summary>
    TryAgain,

    /// <summary>The application of Teams is not installed for the person.</summary>
    NotInstalled,

    /// <summary>Teams did not take it for good.</summary>
    Refused,
}

/// <summary>The end of a try, with the error of Teams, never the person nor the texts.</summary>
public sealed record TeamsSendResult(TeamsSendOutcome Outcome, string? Error = null)
{
    public static readonly TeamsSendResult Sent = new(TeamsSendOutcome.Sent);
}

/// <summary>
/// Sends a notice to the Activity of Teams of a person (ADR-0149): the folder of the disk in development and tests, the
/// Microsoft Graph in production. Only the queue calls it.
/// </summary>
public interface ITeamsSender
{
    Task<TeamsSendResult> SendAsync(TeamsDelivery delivery, CancellationToken cancellationToken);
}

/// <summary>A kind of notice in the manifest of Teams: its name, what it is and its text with the parameters.</summary>
public sealed record TeamsActivityType(string Type, string Description, string Template);

/// <summary>The texts of Teams, from the translation file of the notices (ADR-0013).</summary>
public interface ITeamsTexts
{
    /// <summary>What a notice says in Teams, with the values shown as the reader sees them.</summary>
    TeamsMessage MessageOf(NoticeKind kind, IReadOnlyDictionary<string, string> values, string? actorName, string path, string installationName);

    /// <summary>The kinds of notice of the manifest, one for each notice sent now.</summary>
    IReadOnlyList<TeamsActivityType> ActivityTypes();

    /// <summary>The short and the long description of the application, and the name of its page.</summary>
    TeamsAppTexts AppTexts(string installationName);
}

/// <summary>The texts of the application of Teams of the installation: the short and the long description, and its page.</summary>
public sealed record TeamsAppTexts(string ShortDescription, string FullDescription, string Page);

/// <summary>Where a notice sent now goes, for one person.</summary>
public readonly record struct ImmediateRoute(bool Email, bool Teams);

/// <summary>
/// The rule of the channel of the notices sent now (ADR-0149), in one place for the e-mail, Teams and the profile: Teams
/// counts only with the notices of Teams on and for who may receive them (with Graph, who signs in by Entra ID); then the
/// choice of the person decides. Otherwise, the e-mail.
/// </summary>
public sealed class NoticeRouting(TeamsSettings settings, ITeamsAppStore apps)
{
    private bool? _on;

    /// <summary>Whether the notices of Teams are on: configured in the application and turned on by the Administrator.</summary>
    public async Task<bool> TeamsOnAsync(CancellationToken cancellationToken) =>
        _on ??= settings.IsAvailable && (await apps.GetAsync(cancellationToken)).NoticesOn;

    /// <summary>Whether Teams reaches the person: any person with the local folder, only who signs in by Entra with Graph.</summary>
    public bool Reaches(string? entraObjectId) =>
        settings.Provider switch
        {
            TeamsProvider.Local => true,
            TeamsProvider.Graph => !string.IsNullOrWhiteSpace(entraObjectId),
            _ => false,
        };

    /// <summary>Whether the person may receive the notices sent now in Teams.</summary>
    public async Task<bool> AvailableForAsync(string? entraObjectId, CancellationToken cancellationToken) =>
        Reaches(entraObjectId) && await TeamsOnAsync(cancellationToken);

    public async Task<ImmediateRoute> RouteAsync(NoticeRecipient person, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(person);
        return RouteOf(person.Channel, await AvailableForAsync(person.EntraObjectId, cancellationToken));
    }

    public static ImmediateRoute RouteOf(ImmediateNoticeChannel choice, bool teamsAvailable) =>
        !teamsAvailable
            ? new ImmediateRoute(Email: true, Teams: false)
            : choice switch
            {
                ImmediateNoticeChannel.Email => new ImmediateRoute(Email: true, Teams: false),
                ImmediateNoticeChannel.Teams => new ImmediateRoute(Email: false, Teams: true),
                _ => new ImmediateRoute(Email: true, Teams: true),
            };
}

/// <summary>
/// The channel of Teams (ADR-0149): the notices sent now go to the queue of Teams of the people whose channel has Teams;
/// the rest, which waits for the daily summary, never goes to Teams.
/// </summary>
public sealed class TeamsChannel(ITeamsNoticeStore store, NoticeRouting routing, TimeProvider clock) : INoticeChannel
{
    public async Task DeliverAsync(
        Notice notice, IReadOnlyList<NoticeRecipient> recipients, string? actorName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        ArgumentNullException.ThrowIfNull(recipients);
        if (!EmailChannel.Immediate.Contains(notice.Kind) || !await routing.TeamsOnAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var values = JsonSerializer.Serialize(notice.Values);
        foreach (var person in recipients)
        {
            var route = await routing.RouteAsync(person, cancellationToken);
            if (route.Teams)
            {
                store.Add(new TeamsNotice(
                    person.UserId, notice.Kind, notice.ActorId, notice.ProjectId, notice.Path, values, route.Email, now));
            }
        }
    }
}

/// <summary>A failure of a try, for the log: the notice and the error, never the person nor the texts.</summary>
public sealed record TeamsFailure(Guid NoticeId, int Attempts, bool Final, string Error);

/// <summary>What one round of the sending to Teams did.</summary>
public sealed record TeamsDispatchResult(int Sent, int EmailedInstead, IReadOnlyList<TeamsFailure> Failures);

/// <summary>
/// Sends the notices of Teams whose turn came (ADR-0149), as the e-mails (ADR-0036): each one is reserved, sent with the
/// current name of who did it and marked sent, or failed with the wait for the next try. When Teams does not take it
/// for good, or after the last try, the person who chose only Teams receives the e-mail of the same notice.
/// </summary>
public sealed class DispatchTeamsNotices(
    ITeamsNoticeStore store,
    ITeamsSender sender,
    ITeamsTexts texts,
    ITeamsAppStore apps,
    IUserDirectory users,
    IInstallationSettings installation,
    EmailQueue emails,
    TimeProvider clock)
{
    /// <summary>The most notices of one round; the next round takes the rest.</summary>
    public const int BatchSize = 20;

    public async Task<TeamsDispatchResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var due = await store.DueAsync(clock.GetUtcNow().UtcDateTime, BatchSize, cancellationToken);
        if (due.Count == 0)
        {
            return new TeamsDispatchResult(0, 0, []);
        }

        var app = await apps.GetAsync(cancellationToken);
        var people = (await users.PeopleAsync([.. due.Select(notice => notice.UserId).Distinct()], cancellationToken))
            .ToDictionary(person => person.Id);
        var actors = await users.NamesAsync(
            [.. due.Where(notice => notice.ActorId is not null).Select(notice => notice.ActorId!.Value).Distinct()], cancellationToken);
        var installationName = (await installation.GetAsync(cancellationToken)).Name;
        var sent = 0;
        var emailed = 0;
        var failures = new List<TeamsFailure>();
        foreach (var notice in due)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            if (!notice.IsDue(now))
            {
                continue;
            }

            notice.Reserve(now);
            if (!await store.TrySaveAsync(notice, cancellationToken))
            {
                // Another process took it first.
                continue;
            }

            if (!people.TryGetValue(notice.UserId, out var person) || !person.IsActive)
            {
                // Who no longer uses the system hears about nothing, neither by e-mail.
                notice.Refuse(clock.GetUtcNow().UtcDateTime, "The person is no longer active.");
                await store.TrySaveAsync(notice, cancellationToken);
                continue;
            }

            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(notice.Values) ?? [];
            var actorName = notice.ActorId is { } actorId ? actors.GetValueOrDefault(actorId) : null;
            var result = await SendAsync(app, person, notice, values, actorName, installationName, cancellationToken);
            now = clock.GetUtcNow().UtcDateTime;
            switch (result.Outcome)
            {
                case TeamsSendOutcome.Sent:
                    notice.MarkSent(now);
                    sent++;
                    break;
                case TeamsSendOutcome.TryAgain:
                    notice.MarkFailed(now, result.Error ?? "Teams did not answer.");
                    failures.Add(new TeamsFailure(notice.Id, notice.Attempts, notice.Status == TeamsNoticeStatus.Failed, notice.LastError!));
                    break;
                default:
                    notice.Refuse(now, result.Error ?? result.Outcome.ToString());
                    failures.Add(new TeamsFailure(notice.Id, notice.Attempts, Final: true, notice.LastError!));
                    break;
            }

            if (notice.NeedsEmail && person.Email.Length > 0)
            {
                var content = NoticeEmails.ContentOf(
                    new Notice(notice.Kind, [person.Id], notice.ActorId, notice.ProjectId, notice.Path, values), person.Name, actorName);
                await emails.EnqueueAsync(notice.Kind.ToString(), person.Email, person.Name, content, cancellationToken);
                notice.MarkEmailedInstead();
                emailed++;
            }

            await store.TrySaveAsync(notice, cancellationToken);
        }

        return new TeamsDispatchResult(sent, emailed, failures);
    }

    private async Task<TeamsSendResult> SendAsync(
        TeamsApp app,
        PersonInfo person,
        TeamsNotice notice,
        IReadOnlyDictionary<string, string> values,
        string? actorName,
        string installationName,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = texts.MessageOf(notice.Kind, values, actorName, notice.Path, installationName);
            return await sender.SendAsync(
                new TeamsDelivery(app.ManifestId, person.Id, person.Name, person.EntraObjectId, message), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new TeamsSendResult(TeamsSendOutcome.TryAgain, $"{exception.GetType().Name}: {exception.Message}");
        }
    }
}

/// <summary>Removes the notices of Teams sent or failed before the days the installation keeps the e-mails (ADR-0058).</summary>
public sealed class ForgetOldTeamsNotices(ITeamsNoticeStore store, EmailSettings settings, TimeProvider clock)
{
    public Task<int> ExecuteAsync(CancellationToken cancellationToken) =>
        store.RemoveOldAsync(clock.GetUtcNow().UtcDateTime.AddDays(-settings.KeepDays), cancellationToken);
}

/// <summary>The channel of the notices sent now of the person, and whether Teams is there for the person.</summary>
public sealed record ImmediateNotices(ImmediateNoticeChannel Channel, bool TeamsAvailable);

/// <summary>The channel the person in the session chose (ADR-0149); both for who never chose.</summary>
public sealed class GetImmediateNotices(INoticePreferenceStore preferences, NoticeRouting routing, IUserDirectory users)
{
    public async Task<ImmediateNotices> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var channel = (await preferences.ChannelsAsync([userId], cancellationToken)).GetValueOrDefault(userId);
        var person = (await users.PeopleAsync([userId], cancellationToken)).SingleOrDefault();
        return new ImmediateNotices(channel, await routing.AvailableForAsync(person?.EntraObjectId, cancellationToken));
    }
}

/// <summary>Keeps the channel the person in the session chose for the notices sent now (ADR-0149).</summary>
public sealed class ChooseImmediateNotices(INoticePreferenceStore preferences)
{
    public async Task ExecuteAsync(Guid userId, ImmediateNoticeChannel channel, CancellationToken cancellationToken)
    {
        if (await preferences.FindAsync(userId, cancellationToken) is { } preference)
        {
            preference.Choose(channel);
        }
        else
        {
            preferences.Add(new NoticePreference(userId, channel));
        }

        await preferences.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>The application of Teams for the Administration: whether it is there, whether the notices are on and the last version.</summary>
public sealed record TeamsAppStatus(bool Available, bool NoticesOn, string? Version);

public sealed class GetTeamsApp(ITeamsAppStore apps, TeamsSettings settings)
{
    public async Task<TeamsAppStatus> ExecuteAsync(CancellationToken cancellationToken)
    {
        var app = await apps.GetAsync(cancellationToken);
        await apps.SaveChangesAsync(cancellationToken);
        return new TeamsAppStatus(settings.IsAvailable, settings.IsAvailable && app.NoticesOn, app.Version);
    }
}

/// <summary>
/// The Administrator turns the notices of Teams on or off (ADR-0149); on only with the sending to Teams configured in
/// the application. False when it was not possible.
/// </summary>
public sealed class ChangeTeamsNotices(ITeamsAppStore apps, TeamsSettings settings)
{
    public async Task<bool> ExecuteAsync(bool on, CancellationToken cancellationToken)
    {
        if (on && !settings.IsAvailable)
        {
            return false;
        }

        (await apps.GetAsync(cancellationToken)).TurnNotices(on);
        await apps.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>The manifest of the package of Teams, as the file manifest.json, and its version.</summary>
public sealed record TeamsPackage(string Manifest, string Version);

/// <summary>Writes the manifest of the application of Teams of the installation (ADR-0149).</summary>
public interface ITeamsManifest
{
    string Write(TeamsApp app, string version, string installationName, TeamsSettings settings);
}

/// <summary>
/// The manifest of the package of Teams of the installation, for the Administrator (ADR-0149); the screen adds the
/// icons it draws. The version goes up when the manifest or what the icons show changed. Null when the sending to Teams
/// is not configured.
/// </summary>
public sealed class PackageTeamsApp(ITeamsAppStore apps, ITeamsManifest manifests, IInstallationSettings installation, TeamsSettings settings)
{
    public async Task<TeamsPackage?> ExecuteAsync(string iconSource, CancellationToken cancellationToken)
    {
        if (!settings.IsAvailable)
        {
            return null;
        }

        var app = await apps.GetAsync(cancellationToken);
        var name = (await installation.GetAsync(cancellationToken)).Name;
        // The manifest without its version, with what the icons show, tells whether the package changed.
        var content = manifests.Write(app, "0.0.0", name, settings) + "\n" + iconSource;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var version = app.Package(hash);
        await apps.SaveChangesAsync(cancellationToken);
        return new TeamsPackage(manifests.Write(app, version, name, settings), version);
    }
}
