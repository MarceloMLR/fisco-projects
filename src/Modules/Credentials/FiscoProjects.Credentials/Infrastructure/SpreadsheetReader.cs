using System.Globalization;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FiscoProjects.Credentials.Application;

namespace FiscoProjects.Credentials.Infrastructure;

/// <summary>
/// Reads the spreadsheets of the import of the credentials (ADR-0094): the .xlsx by the Open XML SDK, only the values
/// of the cells, and the .csv by <see cref="CsvText"/>. The first line with a value is the header; the empty lines are
/// left out; every value is trimmed text.
/// </summary>
internal sealed class SpreadsheetReader : ISpreadsheetReader
{
    /// <summary>The most characters a part of a .xlsx may have once unpacked, against a file made to grow too much.</summary>
    private const long MaxCharactersInPart = 40_000_000;

    public async Task<SpreadsheetResult> ReadAsync(string fileName, Stream content, int? sheet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = Path.GetExtension(fileName ?? string.Empty).ToUpperInvariant();
        if (extension is not (".XLSX" or ".CSV"))
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.Type);
        }

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        if (memory.Length > CredentialImportLimits.MaxBytes)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.TooLarge);
        }

        memory.Position = 0;
        return extension == ".XLSX" ? ReadWorkbook(memory, sheet) : ReadCsv(memory.ToArray());
    }

    private static SpreadsheetResult ReadWorkbook(MemoryStream content, int? sheet)
    {
        // A .xlsx is a ZIP file; a protected one, or an old .xls with another name, is not.
        var start = content.GetBuffer();
        if (content.Length < 4 || start[0] != 0x50 || start[1] != 0x4B || start[2] != 0x03 || start[3] != 0x04)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.Unreadable);
        }

        try
        {
            using var document = SpreadsheetDocument.Open(content, false, new OpenSettings { MaxCharactersInPart = MaxCharactersInPart });
            var workbook = document.WorkbookPart;
            var sheets = workbook?.Workbook?.Sheets?.Elements<Sheet>().ToList() ?? [];
            if (workbook is null || sheets.Count == 0)
            {
                return SpreadsheetResult.Refused(SpreadsheetRefusal.Unreadable);
            }

            var chosen = sheet is { } index && index >= 0 && index < sheets.Count ? index : 0;
            if (sheets[chosen].Id?.Value is not { } partId || workbook.GetPartById(partId) is not WorksheetPart part)
            {
                return SpreadsheetResult.Refused(SpreadsheetRefusal.Unreadable);
            }

            var shared = workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().Select(TextOf).ToList() ?? [];
            var rows = new List<SpreadsheetRow>();
            var previous = 0;
            foreach (var row in part.Worksheet?.Descendants<Row>() ?? [])
            {
                var number = row.RowIndex?.Value is { } rowIndex ? (int)rowIndex : previous + 1;
                previous = number;
                var cells = new List<string>();
                var column = 0;
                foreach (var cell in row.Elements<Cell>())
                {
                    column = ColumnOf(cell.CellReference?.Value) ?? column + 1;
                    if (column > CredentialImportLimits.MaxColumns + 1)
                    {
                        return SpreadsheetResult.Refused(SpreadsheetRefusal.TooManyRows);
                    }

                    while (cells.Count < column - 1)
                    {
                        cells.Add(string.Empty);
                    }

                    cells.Add(ValueOf(cell, shared));
                }

                rows.Add(new SpreadsheetRow(number, cells));
            }

            return Shape([.. sheets.Select(item => item.Name?.Value ?? string.Empty)], chosen, rows);
        }
        catch (Exception exception) when (exception is OpenXmlPackageException or InvalidDataException or FileFormatException
            or InvalidOperationException or XmlException or FormatException or OverflowException or ArgumentException)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.Unreadable);
        }
    }

    private static SpreadsheetResult ReadCsv(byte[] content)
    {
        if (CsvText.Decode(content) is not { } text)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.Unreadable);
        }

        var records = CsvText.Parse(text);
        if (records.Any(record => record.Cells.Count > CredentialImportLimits.MaxColumns))
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.TooManyRows);
        }

        return Shape(["CSV"], 0, [.. records.Select(record => new SpreadsheetRow(record.Line, record.Cells))]);
    }

    /// <summary>
    /// The header and the lines under it: the first line with a value is the header, an empty name becomes the letter
    /// of the column, the empty lines are left out and every line has a value for each column.
    /// </summary>
    internal static SpreadsheetResult Shape(IReadOnlyList<string> sheets, int sheet, IReadOnlyList<SpreadsheetRow> rows)
    {
        var filled = rows
            .Select(row => row with { Cells = [.. row.Cells.Select(cell => cell.Trim())] })
            .Where(row => row.Cells.Any(cell => cell.Length > 0))
            .ToList();
        if (filled.Count < 2)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.Empty);
        }

        var width = filled.Max(row => row.Cells.Count);
        if (width > CredentialImportLimits.MaxColumns)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.TooManyRows);
        }

        var lines = filled.Skip(1).ToList();
        if (lines.Count > CredentialImportLimits.MaxRows)
        {
            return SpreadsheetResult.Refused(SpreadsheetRefusal.TooManyRows);
        }

        var header = filled[0].Cells;
        var columns = Enumerable.Range(0, width)
            .Select(index => index < header.Count && header[index].Length > 0 ? header[index] : $"Coluna {LetterOf(index + 1)}")
            .ToList();
        return new SpreadsheetResult(
            new Spreadsheet(
                sheets,
                sheet,
                columns,
                [.. lines.Select(row => row with { Cells = [.. Enumerable.Range(0, width).Select(index => index < row.Cells.Count ? row.Cells[index] : string.Empty)] })]),
            SpreadsheetRefusal.None);
    }

    /// <summary>The value kept in a cell as text: the shared or inline text, the number without the regional format, or the result of a formula.</summary>
    private static string ValueOf(Cell cell, IReadOnlyList<string> shared)
    {
        var value = cell.CellValue?.Text ?? string.Empty;
        var type = cell.DataType?.Value;
        if (type == CellValues.SharedString)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < shared.Count
                ? shared[index]
                : string.Empty;
        }

        if (type == CellValues.InlineString)
        {
            return cell.InlineString is { } inline ? TextOf(inline) : value;
        }

        if (type == CellValues.Boolean)
        {
            return value == "1" ? "TRUE" : "FALSE";
        }

        if (type == CellValues.String || type == CellValues.Error)
        {
            return value;
        }

        // A number: an integer without decimals, any other as the shortest text that keeps it.
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? Math.Abs(number) < 1e15 && Math.Floor(number) == number
                ? ((long)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString(CultureInfo.InvariantCulture)
            : value;
    }

    /// <summary>The text of a shared or inline text, without its phonetic guides.</summary>
    private static string TextOf(OpenXmlElement item) =>
        string.Concat(item.Elements<Text>().Select(text => text.Text))
        + string.Concat(item.Elements<Run>().Select(run => run.Text?.Text ?? string.Empty));

    /// <summary>The number of the column of a reference, as 3 for "C12"; null without one.</summary>
    internal static int? ColumnOf(string? reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        var column = 0;
        foreach (var character in reference.TakeWhile(char.IsAsciiLetter))
        {
            column = (column * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return column == 0 ? null : column;
    }

    /// <summary>The letter of a column, as "C" for 3 and "AB" for 28.</summary>
    internal static string LetterOf(int column)
    {
        var letters = new StringBuilder();
        while (column > 0)
        {
            var rest = (column - 1) % 26;
            letters.Insert(0, (char)('A' + rest));
            column = (column - 1) / 26;
        }

        return letters.ToString();
    }
}

/// <summary>
/// The CSV of a spreadsheet as the Excel in Portuguese saves it: UTF-8 (with or without BOM), UTF-16 with BOM or, when
/// not UTF-8, Windows-1252; the separator of the first record, the most common of semicolon, comma and tab outside the
/// quotes; the fields between quotes with doubled quotes and line breaks inside.
/// </summary>
internal static class CsvText
{
    /// <summary>A record: the number of the line where it starts and its fields.</summary>
    internal sealed record CsvRecord(int Line, IReadOnlyList<string> Cells);

    /// <summary>The text of the file; null when it is not text.</summary>
    public static string? Decode(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        string text;
        if (content.AsSpan().StartsWith((byte[])[0xEF, 0xBB, 0xBF]))
        {
            text = Encoding.UTF8.GetString(content, 3, content.Length - 3);
        }
        else if (content.AsSpan().StartsWith((byte[])[0xFF, 0xFE]))
        {
            text = Encoding.Unicode.GetString(content, 2, content.Length - 2);
        }
        else if (content.AsSpan().StartsWith((byte[])[0xFE, 0xFF]))
        {
            text = Encoding.BigEndianUnicode.GetString(content, 2, content.Length - 2);
        }
        else
        {
            try
            {
                text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(content);
            }
            catch (DecoderFallbackException)
            {
                text = (CodePagesEncodingProvider.Instance.GetEncoding(1252) ?? Encoding.Latin1).GetString(content);
            }
        }

        // A file with a NUL character is not text, as a .xls or a .xlsx with the wrong name.
        return text.Contains('\0', StringComparison.Ordinal) ? null : text;
    }

    public static IReadOnlyList<CsvRecord> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var separator = SeparatorOf(text);
        var records = new List<CsvRecord>();
        var cells = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var line = 1;
        var start = 1;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    if (character == '\n')
                    {
                        line++;
                    }

                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                quoted = true;
            }
            else if (character == separator)
            {
                cells.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\n' or '\r')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                cells.Add(field.ToString());
                field.Clear();
                records.Add(new CsvRecord(start, [.. cells]));
                cells.Clear();
                line++;
                start = line;
            }
            else
            {
                field.Append(character);
            }
        }

        if (field.Length > 0 || cells.Count > 0)
        {
            cells.Add(field.ToString());
            records.Add(new CsvRecord(start, [.. cells]));
        }

        return records;
    }

    /// <summary>The separator of the first record: the most common of semicolon, comma and tab outside the quotes.</summary>
    internal static char SeparatorOf(string text)
    {
        var counts = new Dictionary<char, int> { [';'] = 0, [','] = 0, ['\t'] = 0 };
        var quoted = false;
        foreach (var character in text)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character is '\n' or '\r')
            {
                break;
            }
            else if (!quoted && counts.ContainsKey(character))
            {
                counts[character]++;
            }
        }

        var most = counts.OrderByDescending(count => count.Value).First();
        return most.Value == 0 ? ';' : most.Key;
    }
}
