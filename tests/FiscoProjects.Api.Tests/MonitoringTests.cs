using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FiscoProjects.Api.Tests;

// Spec: monitoramento, requirements "Telemetria no Application Insights" and "Nada sensível nos logs" (ADR-0033,
// ADR-0057, ADR-0112).
public sealed class MonitoringTests(SqlServerFixture sqlServer)
{
    /// <summary>A connection of Application Insights that never leaves the machine: both endpoints are closed ports.</summary>
    private const string LocalConnection =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/;LiveEndpoint=http://127.0.0.1:9/";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Sem a conexão")]
    public async Task WithoutTheConnection_NothingIsCollected()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);

        app.Services.GetService<TracerProvider>().Should().BeNull();
        app.Services.GetService<MeterProvider>().Should().BeNull();
        (await app.NewClient().GetAsync("/health", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Com a conexão")]
    public async Task WithTheConnection_TracesMetricsAndLogsAreCollected_ButNotTheHealth()
    {
        await using var app = await SessionApp.StartAsync(sqlServer,
            builder => builder.Configuration["AzureMonitor:ConnectionString"] = LocalConnection);

        app.Services.GetService<TracerProvider>().Should().NotBeNull();
        app.Services.GetService<MeterProvider>().Should().NotBeNull();
        var requests = app.Services.GetRequiredService<IOptionsMonitor<AspNetCoreTraceInstrumentationOptions>>().Get(Options.DefaultName);
        requests.Filter.Should().NotBeNull();
        requests.Filter!(RequestTo("/health")).Should().BeFalse();
        requests.Filter(RequestTo("/api/projects")).Should().BeTrue();
        Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED").Should().Be("true");
        (await app.NewClient().GetAsync("/health", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Fluxos sensíveis com todos os logs ligados")]
    public async Task NoLog_HasAPasswordOrTheSessionCookie()
    {
        var logs = new CapturingLoggerProvider();
        await using var team = await ProjectTeam.StartAsync(sqlServer, builder =>
        {
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            builder.Logging.AddProvider(logs);
            builder.Logging.AddFilter<CapturingLoggerProvider>(category: null, LogLevel.Trace);
        });
        const string first = "Senha-Monitorada-7Q!x";
        const string second = "Outra-Senha-9Z#k";

        using var diego = team.App.NewClient();
        using var signIn = new HttpRequestMessage(HttpMethod.Post, "/api/session/local") { Content = JsonContent.Create(new { email = "diego.torres@empresa.example" }) };
        signIn.Headers.Add("X-CSRF", "1");
        var signedIn = await diego.SendAsync(signIn, Cancellation);
        var cookie = signedIn.Headers.GetValues("Set-Cookie").Single().Split(';')[0].Split('=', 2)[1];
        (await diego.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        var marina = await team.ClientOfAsync(team.Marina);
        var path = $"/api/projects/{team.Erp}/credentials";
        var added = await ProjectTeam.SendAsync(marina, HttpMethod.Post, path, new
        {
            portal = "ERP",
            environment = "Produção",
            url = (string?)null,
            username = "erp.producao",
            password = first,
            notes = (string?)null,
        });
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        var id = (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        (await marina.GetFromJsonAsync<JsonElement>($"{path}/{id}/password", Cancellation)).GetProperty("password").GetString().Should().Be(first);
        (await ProjectTeam.SendAsync(marina, HttpMethod.Put, $"{path}/{id}", new
        {
            portal = "ERP",
            environment = "Produção",
            url = (string?)null,
            username = "erp.producao",
            password = second,
            notes = (string?)null,
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        logs.Entries.Should().NotBeEmpty("every log, of every level, was kept");
        logs.Entries.Should().NotContain(entry => entry.Contains(first, StringComparison.Ordinal))
            .And.NotContain(entry => entry.Contains(second, StringComparison.Ordinal))
            .And.NotContain(entry => entry.Contains(cookie, StringComparison.Ordinal));
    }

    private static DefaultHttpContext RequestTo(string path)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        return http;
    }

    /// <summary>Keeps every log as text: the message, the values of its state and the exception.</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _entries = new();

        public IReadOnlyCollection<string> Entries => [.. _entries];

        public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class Logger(string category, ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                entries.Enqueue($"{category} scope {state}");
                return null;
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? string.Join(' ', pairs.Select(pair => $"{pair.Key}={pair.Value}"))
                    : string.Empty;
                entries.Enqueue($"{category} {logLevel} {formatter(state, exception)} {values} {exception}");
            }
        }
    }
}
