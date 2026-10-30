using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// The requests of signature of the tests (ADR-0129): the form the screens send, and the signers who answer on the page
/// of the local provider, by the link the drawer of the term shows.
/// </summary>
internal static class AcceptanceRequests
{
    /// <summary>Two signers of the client, outside the company and the system.</summary>
    public static readonly IReadOnlyList<(string Name, string Email)> ClientSigners =
        [("Ana Souza", "ana.souza@cliente.example"), ("Bruno Lima", "bruno.lima@cliente.example")];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public static string PathOf(Guid project) => $"/api/projects/{project}/acceptances";

    /// <summary>A small PDF, as a term would start.</summary>
    public static byte[] Pdf(string text = "termo") => Encoding.ASCII.GetBytes($"%PDF-1.7\n{text}\n%%EOF");

    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, IReadOnlyDictionary<string, string?> fields, string? fileName, byte[]? content)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            if (value is not null)
            {
                form.Add(new StringContent(value, Encoding.UTF8), name);
            }
        }

        if (fileName is not null && content is not null)
        {
            form.Add(new ByteArrayContent(content), "file", fileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    /// <summary>Asks for the signature of the term of the milestone, by default of the two signers of the client.</summary>
    public static Task<HttpResponseMessage> RequestAsync(
        HttpClient client,
        Guid project,
        Guid milestone,
        string title,
        IReadOnlyList<(string Name, string Email)>? signers = null,
        string fileName = "termo-homologacao.pdf",
        byte[]? content = null,
        string? message = "Os testes de homologação terminaram sem pendências. Assinem o termo, por favor.")
    {
        var fields = new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["title"] = title,
            ["message"] = message,
        };
        var index = 0;
        foreach (var (name, email) in signers ?? ClientSigners)
        {
            fields[$"signers[{index}].name"] = name;
            fields[$"signers[{index}].email"] = email;
            index++;
        }

        return PostFormAsync(client, PathOf(project), fields, fileName, content ?? Pdf());
    }

    public static async Task<Guid> RequestedAsync(
        HttpClient client, Guid project, Guid milestone, string title, IReadOnlyList<(string Name, string Email)>? signers = null) =>
        await IdOfAsync(await RequestAsync(client, project, milestone, title, signers));

    /// <summary>Attaches a term signed outside the system, without a request.</summary>
    public static Task<HttpResponseMessage> AttachAsync(HttpClient client, Guid project, Guid milestone, string title = "Aceite do escopo") =>
        PostFormAsync(client, $"{PathOf(project)}/attached", new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["title"] = title,
        }, "termo-escopo-assinado.pdf", Pdf("escopo"));

    /// <summary>Attaches the signed term received outside the system to a pending request.</summary>
    public static Task<HttpResponseMessage> AttachToAsync(
        HttpClient client, Guid project, Guid term, string fileName = "termo-recebido-por-email.pdf", byte[]? content = null) =>
        PostFormAsync(client, $"{PathOf(project)}/{term}/attached", new Dictionary<string, string?>(), fileName, content ?? Pdf("anexado"));

    public static async Task<JsonElement> DetailsAsync(HttpClient client, Guid project, Guid term) =>
        await client.GetFromJsonAsync<JsonElement>($"{PathOf(project)}/{term}", Cancellation);

    /// <summary>The link of the local page of the signer at <paramref name="index"/>, as the drawer shows it.</summary>
    public static async Task<string> LinkOfAsync(HttpClient manager, Guid project, Guid term, int index)
    {
        var signer = (await DetailsAsync(manager, project, term)).GetProperty("signers")[index];
        var link = signer.GetProperty("signingLink").GetString();
        link.Should().StartWith("/sign/");
        return link!;
    }

    /// <summary>The address of the API behind the local page of a link: /sign/{token} → /api/local-signatures/{token}.</summary>
    public static string ApiOf(string link) => $"/api/local-signatures/{link["/sign/".Length..]}";

    /// <summary>The signer of the link signs on the local page, without a session.</summary>
    public static async Task SignLocallyAsync(SessionApp app, string link)
    {
        using var anonymous = app.NewClient();
        var signed = await ProjectTeam.SendAsync(anonymous, HttpMethod.Post, $"{ApiOf(link)}/sign");
        signed.StatusCode.Should().Be(HttpStatusCode.NoContent, await signed.Content.ReadAsStringAsync(Cancellation));
    }

    /// <summary>The signer of the link refuses on the local page, without a session.</summary>
    public static async Task RefuseLocallyAsync(SessionApp app, string link, string reason)
    {
        using var anonymous = app.NewClient();
        var refused = await ProjectTeam.SendAsync(anonymous, HttpMethod.Post, $"{ApiOf(link)}/refuse", new { reason });
        refused.StatusCode.Should().Be(HttpStatusCode.NoContent, await refused.Content.ReadAsStringAsync(Cancellation));
    }

    /// <summary>Every signer of the request signs on the local page, in order.</summary>
    public static async Task SignAllAsync(ProjectTeam team, HttpClient manager, Guid project, Guid term)
    {
        var count = (await DetailsAsync(manager, project, term)).GetProperty("signers").GetArrayLength();
        var links = new List<string>();
        for (var index = 0; index < count; index++)
        {
            links.Add(await LinkOfAsync(manager, project, term, index));
        }

        foreach (var link in links)
        {
            await SignLocallyAsync(team.App, link);
        }
    }

    public static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// A request asked of a person of the project before the provider of signatures (ADR-0095), straight in the
    /// database, as the migration keeps it: pending, without a provider nor signers.
    /// </summary>
    public static async Task<Guid> RequestedBeforeAsync(ProjectTeam team, Guid project, Guid milestone, Guid recipient, string title)
    {
        var id = Guid.CreateVersion7();
        var at = team.App.Clock.GetUtcNow().UtcDateTime;
        await team.App.ExecuteAsync(
            """
            INSERT INTO [documents].[acceptance_terms]
                ([Id], [ProjectId], [MilestoneId], [Title], [Message], [RecipientId], [Status], [RequestedById], [RequestedAt], [FileId], [FileName], [FileSize])
            VALUES (@id, @project, @milestone, @title, NULL, @recipient, 'Pending', @manager, @at, 'Pedidos de aceite/termo-antigo.pdf', 'termo-antigo.pdf', 32);
            INSERT INTO [documents].[acceptance_events] ([Id], [TermId], [Kind], [ActorId], [At])
            VALUES (@event, @id, 'Requested', @manager, @at);
            """,
            ("@id", id),
            ("@project", project),
            ("@milestone", milestone),
            ("@title", title),
            ("@recipient", recipient),
            ("@manager", team.Marina),
            ("@at", at),
            ("@event", Guid.CreateVersion7()));
        return id;
    }
}

/// <summary>
/// A provider of signatures in memory, for what the local one does not do: failing, and signers who answer at the
/// provider without the system hearing about it, until the check (ADR-0129).
/// </summary>
internal sealed class FakeSignatureProvider : ISignatureProvider
{
    private readonly Dictionary<string, (SignatureRequest Request, List<SignerState> Signers)> _documents = [];

    public string Name => "Fake";

    /// <summary>Whether every call fails, as a provider out of the air.</summary>
    public bool Failing { get; set; }

    public List<string> Cancelled { get; } = [];

    public List<string> Reminded { get; } = [];

    public List<string> Deleted { get; } = [];

    public IReadOnlyList<SignatureRequest> Sent => [.. _documents.Values.Select(document => document.Request)];

    public Task<SentSignature> SendAsync(SignatureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Fail();
        var id = Guid.NewGuid().ToString("N");
        var signers = request.Signers.Select(_ => new SignerState(Guid.NewGuid().ToString("N"), AcceptanceSignerStatus.Pending)).ToList();
        _documents[id] = (request with { Content = Stream.Null }, signers);
        return Task.FromResult(new SentSignature(id, [.. signers.Select(signer => signer.SignerId)]));
    }

    public Task<IReadOnlyList<SignerState>> StatusAsync(string documentId, CancellationToken cancellationToken)
    {
        Fail();
        return Task.FromResult<IReadOnlyList<SignerState>>([.. _documents[documentId].Signers]);
    }

    public Task RemindAsync(string documentId, CancellationToken cancellationToken)
    {
        Fail();
        Reminded.Add(documentId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(string documentId, CancellationToken cancellationToken)
    {
        Fail();
        Cancelled.Add(documentId);
        return Task.CompletedTask;
    }

    public Task<SignedFile> SignedFileAsync(string documentId, CancellationToken cancellationToken)
    {
        Fail();
        return Task.FromResult(new SignedFile("termo-homologacao-assinado.pdf", new MemoryStream(AcceptanceRequests.Pdf("assinado no provedor"))));
    }

    public Task DeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        Fail();
        Deleted.Add(documentId);
        return Task.CompletedTask;
    }

    /// <summary>The signer at <paramref name="index"/> of the only document signs at the provider.</summary>
    public void Sign(int index, DateTime at)
    {
        var signers = _documents.Values.Single().Signers;
        signers[index] = signers[index] with { Status = AcceptanceSignerStatus.Signed, At = at };
    }

    private void Fail()
    {
        if (Failing)
        {
            throw new SignatureProviderException();
        }
    }
}
