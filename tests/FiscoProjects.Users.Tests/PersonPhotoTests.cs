using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Users.Tests;

// Spec: usuarios-e-papeis, requirement "Foto da pessoa" (ADR-0124): the photos of Microsoft 365 through the simulated
// Graph, kept only in the memory.
public sealed class PersonPhotoTests
{
    private const string HelenaId = "6f1e2d3c-4b5a-4978-8695-a4b3c2d1e0f9";

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    private readonly FakeGraph _graph = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordedLogger<GraphPersonPhotos> _logger = new();
    private readonly PersonPhotoCache _cache;

    public PersonPhotoTests() => _cache = new PersonPhotoCache(_clock);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static IReadOnlyList<UserIdentity> Helena => [new UserIdentity("entra", HelenaId)];

    [Fact(DisplayName = "Foto do Microsoft 365 no tamanho do avatar")]
    public async Task Photo_ComesInTheSizeOfTheAvatar_AndStaysInTheMemoryForAnHour()
    {
        _graph.Photos[HelenaId] = new FakeGraphPhoto(Jpeg);

        var first = await Photos().FindAsync(Helena, Cancellation);
        var again = await Photos().FindAsync(Helena, Cancellation);
        _clock.Advance(PersonPhotoCache.Kept);
        var later = await Photos().FindAsync(Helena, Cancellation);

        first!.Content.Should().Equal(Jpeg);
        first.ContentType.Should().Be("image/jpeg");
        again.Should().BeSameAs(first);
        later!.Content.Should().Equal(Jpeg);
        _graph.Requests.Select(address => address.AbsolutePath).Should().Equal(
            $"/v1.0/users/{HelenaId}/photos/96x96/$value", $"/v1.0/users/{HelenaId}/photos/96x96/$value");
    }

    [Fact(DisplayName = "Foto só no tamanho original")]
    public async Task Photo_WithoutTheSizeOfTheAvatar_ComesInTheLargestOne()
    {
        _graph.Photos[HelenaId] = new FakeGraphPhoto(Jpeg, "image/jpg", Sized: false);

        var photo = await Photos().FindAsync(Helena, Cancellation);

        photo!.Content.Should().Equal(Jpeg);
        photo.ContentType.Should().Be("image/jpeg", "Graph may call it image/jpg");
        _graph.Requests.Select(address => address.AbsolutePath).Should().Equal(
            $"/v1.0/users/{HelenaId}/photos/96x96/$value", $"/v1.0/users/{HelenaId}/photo/$value");
    }

    [Fact(DisplayName = "Pessoa sem foto no Microsoft 365")]
    public async Task NoPhoto_IsKeptAsNone()
    {
        var photo = await Photos().FindAsync(Helena, Cancellation);
        var again = await Photos().FindAsync(Helena, Cancellation);

        photo.Should().BeNull();
        again.Should().BeNull();
        _graph.Requests.Should().HaveCount(2, "the two sizes once, and then the memory");
    }

    [Fact(DisplayName = "Foto de quem entrou localmente")]
    public async Task LocalSignIn_HasNoPhoto_WithoutAskingGraph()
    {
        var photo = await Photos().FindAsync([new UserIdentity("local", "marina.costa@empresa.example")], Cancellation);
        var off = await Photos(enabled: false).FindAsync(Helena, Cancellation);

        photo.Should().BeNull();
        off.Should().BeNull();
        _graph.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "Foto que não é imagem")]
    public async Task OtherTypes_AndTooLargePhotos_AreNotShown()
    {
        _graph.Photos[HelenaId] = new FakeGraphPhoto("<svg/>"u8.ToArray(), "image/svg+xml");
        _graph.Photos["7a8b9c0d-1e2f-4a3b-8c4d-5e6f7a8b9c0d"] = new FakeGraphPhoto(new byte[GraphPersonPhotos.MaxBytes + 1]);

        var svg = await Photos().FindAsync(Helena, Cancellation);
        var large = await Photos().FindAsync([new UserIdentity("entra", "7a8b9c0d-1e2f-4a3b-8c4d-5e6f7a8b9c0d")], Cancellation);

        svg.Should().BeNull();
        large.Should().BeNull();
    }

    [Fact(DisplayName = "Microsoft 365 fora do ar na foto")]
    public async Task GraphDown_NoPhoto_LoggedWithoutThePerson_AndAskedAgainAfterAMinute()
    {
        _graph.Down = true;

        var photo = await Photos().FindAsync(Helena, Cancellation);
        await Photos().FindAsync(Helena, Cancellation);
        _graph.Down = false;
        _graph.Photos[HelenaId] = new FakeGraphPhoto(Jpeg);
        _clock.Advance(PersonPhotoCache.KeptAfterFailure);
        var back = await Photos().FindAsync(Helena, Cancellation);

        photo.Should().BeNull();
        back!.Content.Should().Equal(Jpeg);
        _logger.Entries.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
        _logger.Entries[0].Message.Should().Contain("503").And.NotContain(HelenaId, "the log does not tell who");
    }

    private GraphPersonPhotos Photos(bool enabled = true) => new(
        new GraphClients(_graph),
        new GraphToken(),
        Options.Create(new EntraSignInOptions
        {
            Enabled = enabled,
            TenantId = "8c0a9f3e-1b2d-4e5f-9a6b-7c8d9e0f1a2b",
            ClientId = "4d3c2b1a-0f9e-4d8c-b7a6-5e4d3c2b1a0f",
        }),
        _cache,
        _logger);
}
