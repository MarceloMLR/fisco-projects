namespace FiscoProjects.SharedKernel;

/// <summary>The rule of an address of the web a person types, as the portal of a vendor or a link of a project.</summary>
public static class WebAddress
{
    /// <summary>
    /// The address, trimmed, or null when nothing was typed; false when it is not an absolute http or https address of
    /// up to <paramref name="maxLength"/> characters.
    /// </summary>
    public static bool TryOf(string? text, int maxLength, out string? url)
    {
        url = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return url is null
            || (url.Length <= maxLength
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
    }
}
