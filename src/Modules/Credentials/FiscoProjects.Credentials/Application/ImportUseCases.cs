using FiscoProjects.Credentials.Domain;

namespace FiscoProjects.Credentials.Application;

/// <summary>Why a spreadsheet was not read (ADR-0094).</summary>
public enum SpreadsheetRefusal
{
    None = 0,

    /// <summary>Neither .xlsx nor .csv.</summary>
    Type = 1,

    /// <summary>Corrupt, protected by a password or an old .xls saved with another name.</summary>
    Unreadable = 2,

    /// <summary>No line with a value under the header.</summary>
    Empty = 3,

    /// <summary>Larger than the import takes.</summary>
    TooLarge = 4,

    /// <summary>More lines or columns than the import takes.</summary>
    TooManyRows = 5,
}

/// <summary>A line of a spreadsheet: its number in the spreadsheet and the text of each column.</summary>
public sealed record SpreadsheetRow(int Number, IReadOnlyList<string> Cells);

/// <summary>
/// A spreadsheet read for the import: the names of its sheets (one for a CSV), the sheet read, the header (one name for
/// each column) and the lines under it, all as text.
/// </summary>
public sealed record Spreadsheet(IReadOnlyList<string> Sheets, int Sheet, IReadOnlyList<string> Columns, IReadOnlyList<SpreadsheetRow> Rows);

/// <summary>A spreadsheet read, or why it was not.</summary>
public sealed record SpreadsheetResult(Spreadsheet? Spreadsheet, SpreadsheetRefusal Refusal)
{
    public static SpreadsheetResult Refused(SpreadsheetRefusal refusal) => new(null, refusal);
}

/// <summary>The limits of the import (ADR-0094): the size of the file, the lines and the columns.</summary>
public static class CredentialImportLimits
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxRows = 2_000;
    public const int MaxColumns = 50;
}

/// <summary>Reads a .xlsx or a .csv as text, for the import of the credentials (ADR-0094).</summary>
public interface ISpreadsheetReader
{
    /// <summary>Reads the sheet (the first when none or not there) of the file; never throws for a bad file.</summary>
    Task<SpreadsheetResult> ReadAsync(string fileName, Stream content, int? sheet, CancellationToken cancellationToken);
}

/// <summary>A credential mapped from a line of the spreadsheet, with the number of the line.</summary>
public sealed record ImportedCredential(int Row, string? Portal, string? Environment, string? Url, string? Username, string? Password, string? Notes);

/// <summary>A line that is not valid, with its fields.</summary>
public sealed record InvalidImportRow(int Row, IReadOnlyList<CredentialField> Fields);

public enum ImportOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Refused = 5,
    Invalid = 6,
}

/// <summary>What happened to an import: the spreadsheet read or how many were included, or why nothing was.</summary>
public sealed record ImportResult(
    ImportOutcome Outcome,
    Spreadsheet? Spreadsheet = null,
    int Imported = 0,
    SpreadsheetRefusal Refusal = SpreadsheetRefusal.None,
    IReadOnlyList<InvalidImportRow>? InvalidRows = null)
{
    public static ImportResult Of(CredentialResult refusal) => new(refusal.Outcome switch
    {
        CredentialOutcome.NotFound => ImportOutcome.NotFound,
        CredentialOutcome.Forbidden => ImportOutcome.Forbidden,
        _ => ImportOutcome.ProjectClosed,
    });
}

/// <summary>
/// Reads a spreadsheet for the import (ADR-0034, ADR-0094): only who removes credentials imports (ADR-0045), while the
/// project is open. Nothing is saved, not even the file, which has the passwords as they are.
/// </summary>
public sealed class PreviewCredentialImport(CredentialAreas areas, ISpreadsheetReader reader)
{
    public async Task<ImportResult> ExecuteAsync(
        Guid projectId, string fileName, long size, Stream content, int? sheet, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        var (_, refusal) = await areas.ForChangeAsync(projectId, viewer, permissions => permissions.CanDelete, cancellationToken);
        if (refusal is not null)
        {
            return ImportResult.Of(refusal);
        }

        if (size > CredentialImportLimits.MaxBytes)
        {
            return new(ImportOutcome.Refused, Refusal: SpreadsheetRefusal.TooLarge);
        }

        var read = await reader.ReadAsync(fileName, content, sheet, cancellationToken);
        return read.Spreadsheet is { } spreadsheet
            ? new(ImportOutcome.Done, spreadsheet)
            : new(ImportOutcome.Refused, Refusal: read.Refusal);
    }
}

/// <summary>
/// Includes the credentials mapped from the spreadsheet, all or none (ADR-0034): the same rules of a credential
/// included on the screen, the password protected the same way, and each one in the audit as included. Only who
/// removes credentials imports (ADR-0045), while the project is open.
/// </summary>
public sealed class ImportCredentials(CredentialAreas areas, ICredentialStore credentials, IPasswordProtector protector, TimeProvider clock)
{
    public async Task<ImportResult> ExecuteAsync(
        Guid projectId, IReadOnlyList<ImportedCredential> items, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(viewer);
        var (_, refusal) = await areas.ForChangeAsync(projectId, viewer, permissions => permissions.CanDelete, cancellationToken);
        if (refusal is not null)
        {
            return ImportResult.Of(refusal);
        }

        if (items.Count is 0 or > CredentialImportLimits.MaxRows)
        {
            return new(ImportOutcome.Invalid, InvalidRows: []);
        }

        var forms = items
            .Select(item => (item.Row, Checked: CredentialForm.Of(
                item.Portal, item.Environment, item.Url, item.Username, item.Password, item.Notes, passwordNeeded: true)))
            .ToList();
        var invalid = forms
            .Where(form => form.Checked.Form is null)
            .Select(form => new InvalidImportRow(form.Row, form.Checked.Invalid))
            .ToList();
        if (invalid.Count > 0)
        {
            return new(ImportOutcome.Invalid, InvalidRows: invalid);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        credentials.AddRange(forms.Select(form => new PortalCredential(
            projectId, form.Checked.Form!, protector.Protect(form.Checked.Form!.Password!), viewer.UserId, now)));
        await credentials.SaveChangesAsync(cancellationToken);
        return new(ImportOutcome.Done, Imported: forms.Count);
    }
}
