using System.Web;
using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Tests;

// Spec: usuarios-e-papeis, requirement "Diretório da Microsoft" (ADR-0119): the directory of the tenant through the
// simulated Graph, and the company directory that joins it with the demo one.
public sealed class CompanyDirectoryTests
{
    private const string HelenaId = "6F1E2D3C-4B5A-4978-8695-A4B3C2D1E0F9";

    private readonly FakeGraph _graph = new()
    {
        Users =
        {
            new FakeGraphUser(HelenaId, "Helena Duarte", "helena.duarte@empresa.example", "helena.duarte@empresa.example"),
            new FakeGraphUser("1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f", "Helga Moura", "helga.moura@empresa.example", "helga.moura@empresa.example", Enabled: false),
            new FakeGraphUser("2d3e4f5a-6b7c-4d8e-9f0a-1b2c3d4e5f6a", "Heitor Lins", "heitor@fornecedor.example", "heitor_fornecedor.example#EXT#@empresa.example", Guest: true),
            new FakeGraphUser("3e4f5a6b-7c8d-4e9f-0a1b-2c3d4e5f6a7b", "Otávio Reis", null, "otavio.reis@empresa.example"),
        },
    };

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Busca no Graph só dos membros ativos")]
    public async Task Search_FindsTheActiveMembers_WithTheLinkOfTheSignIn()
    {
        var found = await Graph().SearchAsync("he", 20, Cancellation);

        found.Should().Equal(new DirectoryPerson("entra", HelenaId.ToLowerInvariant(), "Helena Duarte", "helena.duarte@empresa.example"));
        var query = HttpUtility.ParseQueryString(_graph.Requests.Single().Query);
        query["$search"].Should().Be("\"displayName:he\" OR \"mail:he\" OR \"userPrincipalName:he\"");
        query["$select"].Should().Be("id,displayName,mail,userPrincipalName", "only what the search uses (ADR-0058)");
        query["$filter"].Should().Be("userType eq 'Member' and accountEnabled eq true");
    }

    [Fact(DisplayName = "Diretório da Microsoft pelo começo das palavras e do e-mail")]
    public async Task Search_FindsByTheWordsOfTheName_AndTheSignInNameWithoutMailbox()
    {
        var bySurname = await Graph().SearchAsync("Dua", 20, Cancellation);
        var bySignInName = await Graph().SearchAsync("otavio", 20, Cancellation);
        var everyone = await Graph().SearchAsync(null, 20, Cancellation);

        bySurname.Select(person => person.Name).Should().Equal("Helena Duarte");
        bySignInName.Should().Equal(new DirectoryPerson(
            "entra", "3e4f5a6b-7c8d-4e9f-0a1b-2c3d4e5f6a7b", "Otávio Reis", "otavio.reis@empresa.example"));
        everyone.Select(person => person.Name).Should().Equal("Helena Duarte", "Otávio Reis");
        HttpUtility.ParseQueryString(_graph.Requests.Last().Query)["$search"].Should().BeNull();
    }

    [Fact(DisplayName = "Texto com aspas na busca do Graph")]
    public async Task Search_EscapesTheQuotesOfTheText()
    {
        await Graph().SearchAsync("a\"b\\c", 5, Cancellation);

        HttpUtility.ParseQueryString(_graph.Requests.Single().Query)["$search"]
            .Should().StartWith("\"displayName:a\\\"b\\\\c\" OR");
    }

    [Fact(DisplayName = "Pessoa do diretório da Microsoft pelo identificador")]
    public async Task Find_GivesOnlyActiveMembers()
    {
        var directory = Graph();

        (await directory.FindAsync("entra", HelenaId.ToLowerInvariant(), Cancellation))!.Name.Should().Be("Helena Duarte");
        (await directory.FindAsync("entra", "1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f", Cancellation)).Should().BeNull("disabled");
        (await directory.FindAsync("entra", "2d3e4f5a-6b7c-4d8e-9f0a-1b2c3d4e5f6a", Cancellation)).Should().BeNull("a guest");
        (await directory.FindAsync("entra", Guid.CreateVersion7().ToString(), Cancellation)).Should().BeNull("unknown");
        var asked = _graph.Requests.Count;
        (await directory.FindAsync("entra", "../me", Cancellation)).Should().BeNull();
        (await directory.FindAsync("local", HelenaId, Cancellation)).Should().BeNull();
        _graph.Requests.Should().HaveCount(asked, "neither an id that is not of an object nor another provider goes to Graph");
    }

    [Fact(DisplayName = "Diretório da Microsoft desligado sem a entrada pelo Entra")]
    public async Task WithoutTheEntraSignIn_GraphIsNotAsked()
    {
        var directory = Graph(enabled: false);

        (await directory.SearchAsync("he", 20, Cancellation)).Should().BeEmpty();
        (await directory.FindAsync("entra", HelenaId, Cancellation)).Should().BeNull();
        _graph.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "Diretório da empresa com as duas entradas")]
    public async Task Company_JoinsTheDemoAndTheMicrosoftDirectories_ByName()
    {
        var company = new CompanyDirectory([Local(), Graph()], new RecordedLogger<CompanyDirectory>());

        var found = await company.SearchAsync("he", 20, Cancellation);
        var helena = await company.FindAsync("entra", HelenaId.ToLowerInvariant(), Cancellation);
        var demo = await company.FindAsync("local", "helio.neves@empresa.example", Cancellation);

        found.Select(person => (person.Provider, person.Name)).Should().Equal(("entra", "Helena Duarte"), ("local", "Hélio Neves"));
        helena!.Email.Should().Be("helena.duarte@empresa.example");
        demo!.Name.Should().Be("Hélio Neves");
    }

    [Fact(DisplayName = "Diretório que falha fica fora da busca")]
    public async Task Company_LeavesOutTheDirectoryThatFails_AndLogsWithoutTheText()
    {
        _graph.Down = true;
        var logger = new RecordedLogger<CompanyDirectory>();
        var company = new CompanyDirectory([Local(), Graph()], logger);

        var found = await company.SearchAsync("hel", 20, Cancellation);
        var person = await company.FindAsync("entra", HelenaId.ToLowerInvariant(), Cancellation);

        found.Select(person => person.Name).Should().Equal("Hélio Neves");
        person.Should().BeNull();
        logger.Entries.Should().HaveCount(2).And.OnlyContain(entry => entry.Level == LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain("entra").And.Contain("503")
            .And.NotContain("hel", "the text searched is personal");
    }

    private GraphPeopleDirectory Graph(bool enabled = true) => new(
        new Clients(_graph),
        new FixedTokens(),
        Options.Create(new EntraSignInOptions
        {
            Enabled = enabled,
            TenantId = "8c0a9f3e-1b2d-4e5f-9a6b-7c8d9e0f1a2b",
            ClientId = "4d3c2b1a-0f9e-4d8c-b7a6-5e4d3c2b1a0f",
        }));

    private static LocalPeopleDirectory Local() => new(Options.Create(new LocalSignInOptions
    {
        Enabled = true,
        People = [new LocalPerson { Name = "Hélio Neves", Email = "helio.neves@empresa.example" }],
    }));

    private sealed class Clients(FakeGraph graph) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(graph, disposeHandler: false);
    }

    private sealed class FixedTokens : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeGraph.Token);
    }
}

/// <summary>A logger that keeps what was written, to check the level and that nothing personal goes to the log.</summary>
internal sealed class RecordedLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
