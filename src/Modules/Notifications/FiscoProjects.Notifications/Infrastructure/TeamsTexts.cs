using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// The texts of Teams (ADR-0013, ADR-0149), from the section "teams" of the translation file of the notices: the text of
/// each kind of notice is in the manifest, with its parameters in braces, as {person}, and each notice sends the values
/// of the parameters its text has, shown as the reader sees them (a team, a day), as the e-mails do.
/// </summary>
internal sealed partial class TeamsTexts : ITeamsTexts
{
    /// <summary>Who did it, or the installation for what the system does.</summary>
    public const string Person = "person";

    /// <summary>The number of the ticket, with its sign: #104.</summary>
    public const string Ticket = "ticket";

    public TeamsMessage MessageOf(
        NoticeKind kind, IReadOnlyDictionary<string, string> values, string? actorName, string path, string installationName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var shown = EmailRenderer.Shown(values, installationName);
        shown.TryAdd(NoticeValues.Project, installationName);
        shown.TryAdd(NoticeValues.Title, string.Empty);
        shown[Person] = string.IsNullOrWhiteSpace(actorName) ? installationName : actorName;
        if (shown.TryGetValue(NoticeValues.Number, out var number))
        {
            shown[Ticket] = $"#{number}";
        }

        var template = TemplateOf(kind);
        var parameters = Parameters().Matches(template)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new KeyValuePair<string, string>(name, shown.GetValueOrDefault(name) ?? string.Empty))
            .ToList();
        return new TeamsMessage(
            NoticeEmails.KeyOf(kind),
            parameters,
            EmailRenderer.Text("teams.topic", shown),
            EmailRenderer.Text("teams.preview", shown),
            path);
    }

    public IReadOnlyList<TeamsActivityType> ActivityTypes() =>
        [.. EmailChannel.Immediate.Order().Select(kind => new TeamsActivityType(
            NoticeEmails.KeyOf(kind),
            EmailRenderer.Text($"teams.activities.{NoticeEmails.KeyOf(kind)}.description", NoValues),
            TemplateOf(kind)))];

    public TeamsAppTexts AppTexts(string installationName)
    {
        var values = new Dictionary<string, string> { ["installation"] = installationName };
        return new TeamsAppTexts(
            EmailRenderer.Text("teams.app.shortDescription", values),
            EmailRenderer.Text("teams.app.fullDescription", values),
            EmailRenderer.Text("teams.app.page", values));
    }

    private static readonly IReadOnlyDictionary<string, string> NoValues = new Dictionary<string, string>();

    private static string TemplateOf(NoticeKind kind) =>
        EmailRenderer.Text($"teams.activities.{NoticeEmails.KeyOf(kind)}.template", NoValues);

    [GeneratedRegex(@"(?<!\{)\{(\w+)\}(?!\})")]
    private static partial Regex Parameters();
}

/// <summary>
/// The manifest of the application of Teams of the installation (ADR-0149), as Teams reads it: the id that never
/// changes, the version, the names and texts of the installation, the page of the FiscoProjects in Teams with the
/// address of the item, the domain of the screens, the application in Entra ID, the permissions the application asks
/// when installed and one kind of notice for each notice sent now. The icons are files of the package, drawn by the
/// screen.
/// </summary>
internal sealed class TeamsAppManifest(ITeamsTexts texts) : ITeamsManifest
{
    /// <summary>The version of the schema of the manifest, with the activities and the permissions of the application.</summary>
    public const string SchemaVersion = "1.17";

    /// <summary>The id of the one page of the application, which the link of each notice opens.</summary>
    public const string PageId = "home";

    public const string ColorIcon = "color.png";

    public const string OutlineIcon = "outline.png";

    public string Write(TeamsApp app, string version, string installationName, TeamsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(settings);
        var site = (settings.SiteUrl ?? throw new InvalidOperationException("The package of Teams needs the address of the screens.")).TrimEnd('/');
        var host = new Uri(site).Host;
        var appTexts = texts.AppTexts(installationName);
        var manifest = new JsonObject
        {
            ["$schema"] = $"https://developer.microsoft.com/json-schemas/teams/v{SchemaVersion}/MicrosoftTeams.schema.json",
            ["manifestVersion"] = SchemaVersion,
            ["version"] = version,
            ["id"] = app.ManifestId.ToString(),
            ["developer"] = new JsonObject
            {
                ["name"] = Cut(installationName, 32),
                ["websiteUrl"] = site + "/",
                ["privacyUrl"] = site + "/",
                ["termsOfUseUrl"] = site + "/",
            },
            ["name"] = new JsonObject { ["short"] = Cut(installationName, 30), ["full"] = Cut(installationName, 100) },
            ["description"] = new JsonObject
            {
                ["short"] = Cut(appTexts.ShortDescription, 80),
                ["full"] = Cut(appTexts.FullDescription, 4000),
            },
            ["icons"] = new JsonObject { ["color"] = ColorIcon, ["outline"] = OutlineIcon },
            ["accentColor"] = "#FFFFFF",
            ["staticTabs"] = new JsonArray
            {
                new JsonObject
                {
                    ["entityId"] = PageId,
                    ["name"] = appTexts.Page,
                    ["contentUrl"] = site + "/teams?item={page.subPageId}&theme={app.theme}",
                    ["websiteUrl"] = site + "/",
                    ["scopes"] = new JsonArray { "personal" },
                },
            },
            ["validDomains"] = new JsonArray { host },
        };
        if (!string.IsNullOrWhiteSpace(settings.EntraClientId))
        {
            manifest["webApplicationInfo"] = new JsonObject
            {
                ["id"] = settings.EntraClientId,
                ["resource"] = $"api://{host}/{settings.EntraClientId}",
            };
        }

        manifest["authorization"] = new JsonObject
        {
            ["permissions"] = new JsonObject
            {
                ["resourceSpecific"] = new JsonArray
                {
                    new JsonObject { ["name"] = "TeamsActivity.Send.User", ["type"] = "Application" },
                    new JsonObject { ["name"] = "TeamsAppInstallation.Read.User", ["type"] = "Application" },
                },
            },
        };
        var activityTypes = new JsonArray();
        foreach (var type in texts.ActivityTypes())
        {
            activityTypes.Add(new JsonObject
            {
                ["type"] = type.Type,
                ["description"] = type.Description,
                ["templateText"] = type.Template,
            });
        }

        manifest["activities"] = new JsonObject { ["activityTypes"] = activityTypes };
        return manifest.ToJsonString(Indented);
    }

    // A file for Teams, never put in a page: the accents stay as they are.
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Cut(string text, int length) => text.Length <= length ? text : text[..length].TrimEnd();
}
