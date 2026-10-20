namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>How the e-mails of the installation leave (ADR-0036).</summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    /// <summary>The folder of the disk, for development and tests: nothing leaves (ADR-0004).</summary>
    public const string LocalProvider = "Local";

    /// <summary>Which implementation sends the e-mails; empty while none is configured, and the e-mails wait in the queue.</summary>
    public string? Provider { get; set; }

    /// <summary>The shared mailbox the e-mails leave from, as projetos@empresa.com.br.</summary>
    public string? Mailbox { get; set; }

    /// <summary>The address of the screens, for the button of each e-mail, as https://projetos.empresa.com.br.</summary>
    public string? SiteUrl { get; set; }

    /// <summary>How many days the queue keeps the content of the e-mails sent or failed (ADR-0058).</summary>
    public int KeepDays { get; set; } = 30;

    public LocalEmailOptions Local { get; set; } = new();

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalEmailOptions
{
    /// <summary>Folder of the e-mails, absolute or relative to the folder of the application.</summary>
    public string Folder { get; set; } = "App_Data/emails";
}
