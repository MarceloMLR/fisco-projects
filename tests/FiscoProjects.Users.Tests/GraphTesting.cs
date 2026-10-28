using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Tests;

/// <summary>The clients of Microsoft Graph, all to the simulated one.</summary>
internal sealed class GraphClients(FakeGraph graph) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(graph, disposeHandler: false);
}

/// <summary>The token of the application the simulated Graph accepts.</summary>
internal sealed class GraphToken : IGraphTokens
{
    public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeGraph.Token);
}
