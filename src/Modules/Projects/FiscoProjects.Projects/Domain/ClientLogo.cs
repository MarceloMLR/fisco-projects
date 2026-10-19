namespace FiscoProjects.Projects.Domain;

/// <summary>Why a logo of a client was refused.</summary>
public enum ClientLogoRefusal
{
    Format = 1,
    TooLarge = 2,
}

/// <summary>A format a logo of a client may have, with its extension and type.</summary>
public sealed record ClientLogoFormat(string Extension, string ContentType)
{
    public static readonly ClientLogoFormat Png = new("png", "image/png");
    public static readonly ClientLogoFormat Jpg = new("jpg", "image/jpeg");
}

/// <summary>The format of an accepted logo, or why it was refused.</summary>
public sealed record ClientLogoCheck(ClientLogoFormat? Format, ClientLogoRefusal? Refusal);

/// <summary>
/// The logo of a client is a PNG or a JPG of up to 1 MB (ADR-0015), told by its content, never by the name of the file.
/// </summary>
public static class ClientLogo
{
    public const int MaxBytes = 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpgSignature = [0xFF, 0xD8, 0xFF];

    public static ClientLogoCheck Check(ReadOnlySpan<byte> content)
    {
        if (content.Length > MaxBytes)
        {
            return new ClientLogoCheck(null, ClientLogoRefusal.TooLarge);
        }

        if (content.StartsWith(PngSignature))
        {
            return new ClientLogoCheck(ClientLogoFormat.Png, null);
        }

        return content.StartsWith(JpgSignature)
            ? new ClientLogoCheck(ClientLogoFormat.Jpg, null)
            : new ClientLogoCheck(null, ClientLogoRefusal.Format);
    }
}
