namespace FiscoProjects.Api.Tests;

internal static class ResponseAssertions
{
    public const string ProblemJson = "application/problem+json";

    public static void ShouldHaveMediaType(this HttpResponseMessage response, string mediaType)
    {
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be(mediaType);
    }
}
