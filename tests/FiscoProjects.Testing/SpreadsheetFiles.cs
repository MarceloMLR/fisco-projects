using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace FiscoProjects.Testing;

/// <summary>
/// Spreadsheets for the tests of the import of the credentials (ADR-0094), made with the Open XML SDK as Excel saves
/// them: the texts as shared strings, the numbers as numbers and the empty cells left out.
/// </summary>
public static class SpreadsheetFiles
{
    /// <summary>A .xlsx with the sheets, each with its lines; a null value is an empty cell, a double a number.</summary>
    public static byte[] Workbook(params (string Name, object?[][] Rows)[] sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);
        using var memory = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(memory, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook(new Sheets());
            var shared = workbook.AddNewPart<SharedStringTablePart>();
            shared.SharedStringTable = new SharedStringTable();
            var texts = new List<string>();
            uint id = 1;
            foreach (var (name, rows) in sheets)
            {
                var part = workbook.AddNewPart<WorksheetPart>();
                var data = new SheetData();
                for (var index = 0; index < rows.Length; index++)
                {
                    var row = new Row { RowIndex = (uint)(index + 1) };
                    for (var column = 0; column < rows[index].Length; column++)
                    {
                        var reference = $"{(char)('A' + column)}{index + 1}";
                        switch (rows[index][column])
                        {
                            case null:
                                break;
                            case double number:
                                row.Append(new Cell { CellReference = reference, CellValue = new CellValue(number) });
                                break;
                            case var value:
                                var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                                if (!texts.Contains(text))
                                {
                                    texts.Add(text);
                                    shared.SharedStringTable.Append(new SharedStringItem(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
                                }

                                row.Append(new Cell
                                {
                                    CellReference = reference,
                                    DataType = CellValues.SharedString,
                                    CellValue = new CellValue(texts.IndexOf(text)),
                                });
                                break;
                        }
                    }

                    data.Append(row);
                }

                part.Worksheet = new Worksheet(data);
                workbook.Workbook.Sheets!.Append(new Sheet { Id = workbook.GetIdOfPart(part), SheetId = id++, Name = name });
            }
        }

        return memory.ToArray();
    }
}
