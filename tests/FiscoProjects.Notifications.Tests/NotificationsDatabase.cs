using FiscoProjects.Administration.Infrastructure;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Notifications.Tests;

/// <summary>
/// A migrated database with the Notifications module registered as the host does, the Administration module for the
/// name of the installation, and a sender the test controls.
/// </summary>
internal sealed class NotificationsDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private NotificationsDatabase(TestDatabase database, FakeTimeProvider clock, FakeEmailSender sender)
    {
        _database = database;
        Clock = clock;
        Sender = sender;
    }

    public FakeTimeProvider Clock { get; }

    public FakeEmailSender Sender { get; }

    public static async Task<NotificationsDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var sender = new FakeEmailSender();
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IHostEnvironment>(new TestEnvironment());
            services.AddSingleton<IEmailSender>(sender);
            services.AddLogging();
            services.AddAdministrationModule(configuration);
            services.AddNotificationsModule(configuration);
        });
        await database.MigrateAsync();
        return new NotificationsDatabase(database, clock, sender);
    }

    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        return await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    /// <summary>Puts an e-mail in the queue, as an action of another module does, and saves it.</summary>
    public async Task EnqueueAsync(string toAddress = "marina.costa@empresa.example", string toName = "Marina Costa")
    {
        await using var scope = _database.NewScope();
        await scope.ServiceProvider.GetRequiredService<EmailQueue>().EnqueueAsync(
            EmailKinds.Test,
            toAddress,
            toName,
            new EmailContent("test.subject", ["test.greeting"], new Dictionary<string, string> { ["name"] = toName }),
            TestContext.Current.CancellationToken);
        await scope.ServiceProvider.GetRequiredService<IOutboxStore>().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public NotificationsDbContext NewContext() => _database.NewContext<NotificationsDbContext>();

    public AsyncServiceScope NewScope() => _database.NewScope();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "FiscoProjects.Notifications.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

/// <summary>A sender that keeps what it sent, or fails while the test says so.</summary>
internal sealed class FakeEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _sent = [];

    public IReadOnlyList<EmailMessage> Sent => _sent;

    public bool Failing { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Failing)
        {
            throw new InvalidOperationException("The mailbox is out.");
        }

        _sent.Add(message);
        return Task.CompletedTask;
    }
}
