namespace FiscoProjects.Users.Contracts;

/// <summary>
/// Endpoint metadata of a file the browser keeps by its versioned address, such as a logo: the session is checked as
/// in any other call, but not renewed. A renewed session cookie makes the response "no-store", so the browser would
/// download the file again on every screen; the call that showed the screen has already renewed the session.
/// </summary>
public sealed class KeepSession
{
    public static readonly KeepSession Instance = new();

    private KeepSession()
    {
    }
}
