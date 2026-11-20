using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace GerenWare.Projects.Infrastructure.StatusReports;

/// <summary>
/// Fills a template of the status report with the values (ADR-0165), the same for the default template and, later, the
/// one of the company:
/// <list type="bullet">
/// <item>a field between double braces (<c>{{projeto.nome}}</c>) takes the value with the format of where it starts,
/// also when the PowerPoint kept it in pieces, and a line break of the value makes a new paragraph;</item>
/// <item>a row of a table with fields of a list (<see cref="StatusReportFields.Lists"/>) repeats for each item, and a
/// table of an empty list leaves the slide;</item>
/// <item>the slide with fields of a risk repeats for each risk and leaves without risks, and the slide with fields of
/// the hours leaves without "Mostrar horas";</item>
/// <item>a shape of a logo becomes the picture, fitted in it, or keeps its text without the logo; a bar takes the
/// share of its track; a tag takes the colors and the icon of its tone.</item>
/// </list>
/// </summary>
internal static partial class StatusReportFiller
{
    private const string SvgExtension = "{96DAC541-7B7A-43D3-8B79-37D633B846F1}";
    private const string SvgNamespace = "http://schemas.microsoft.com/office/drawing/2016/SVG/main";

    /// <summary>A PNG of one transparent pixel: the picture the format asks beside an SVG, for older versions.</summary>
    private static readonly byte[] EmptyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9.]+)\s*\}\}")]
    private static partial Regex Field();

    /// <summary>The template filled with the values, as a new file.</summary>
    public static byte[] Fill(byte[] template, StatusReportValues values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);
        using var stream = new MemoryStream();
        stream.Write(template);
        using (var document = PresentationDocument.Open(stream, true))
        {
            var presentation = document.PresentationPart ?? throw new InvalidOperationException("The template has no presentation.");
            var slideIds = presentation.Presentation?.SlideIdList?.Elements<P.SlideId>().ToList() ?? [];
            foreach (var slideId in slideIds)
            {
                var slide = (SlidePart)presentation.GetPartById(slideId.RelationshipId!);
                var fields = FieldsOf(slide.Slide!);
                if (fields.Any(field => StatusReportFields.PrefixOf(field) == StatusReportFields.Hours) && !values.ShowHours)
                {
                    Remove(presentation, slideId, slide);
                }
                else if (fields.Any(field => StatusReportFields.PrefixOf(field) == StatusReportFields.Risk))
                {
                    FillRisks(presentation, slideId, slide, values);
                }
                else
                {
                    FillSlide(slide, new Lookup([values.Report]));
                }
            }

            presentation.Presentation!.Save();
        }

        return stream.ToArray();
    }

    /// <summary>The names of the fields of an element, also of the ones kept in pieces.</summary>
    public static IReadOnlyList<string> FieldsOf(OpenXmlElement root) =>
        [.. root.Descendants<A.Paragraph>()
            .SelectMany(paragraph => Field().Matches(string.Concat(paragraph.Elements<A.Run>().Select(run => run.Text?.Text))))
            .Select(match => match.Groups[1].Value)];

    private static void FillRisks(PresentationPart presentation, P.SlideId slideId, SlidePart slide, StatusReportValues values)
    {
        if (values.Risks.Count == 0)
        {
            Remove(presentation, slideId, slide);
            return;
        }

        // The copies are made from the slide as the template has it, before any is filled.
        var slides = new List<SlidePart> { slide };
        var previous = slideId;
        foreach (var _ in values.Risks.Skip(1))
        {
            var (copy, copyId) = Copy(presentation, slide, previous);
            slides.Add(copy);
            previous = copyId;
        }

        foreach (var (part, risk) in slides.Zip(values.Risks))
        {
            FillSlide(part, new Lookup([risk, values.Report]));
        }
    }

    private static (SlidePart Part, P.SlideId Id) Copy(PresentationPart presentation, SlidePart source, P.SlideId after)
    {
        var copy = presentation.AddNewPart<SlidePart>();
        using (var content = source.GetStream(FileMode.Open, FileAccess.Read))
        {
            copy.FeedData(content);
        }

        foreach (var related in source.Parts)
        {
            copy.AddPart(related.OpenXmlPart, related.RelationshipId);
        }

        var list = presentation.Presentation!.SlideIdList!;
        var id = new P.SlideId
        {
            Id = list.Elements<P.SlideId>().Max(item => item.Id!.Value) + 1,
            RelationshipId = presentation.GetIdOfPart(copy),
        };
        after.InsertAfterSelf(id);
        return (copy, id);
    }

    private static void Remove(PresentationPart presentation, P.SlideId slideId, SlidePart slide)
    {
        slideId.Remove();
        presentation.DeletePart(slide);
    }

    private static void FillSlide(SlidePart part, Lookup lookup)
    {
        var slide = part.Slide!;
        FillTables(slide, lookup);
        foreach (var shape in slide.Descendants<P.Shape>().ToList())
        {
            var name = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value ?? string.Empty;
            if (lookup.Tone(name) is { } tone)
            {
                ColorTag(shape, tone);
            }
            else if (name.EndsWith(".icone", StringComparison.Ordinal) && lookup.Tone(name[..^".icone".Length]) is { } iconTone)
            {
                ColorIcon(shape, iconTone);
            }
            else if (lookup.Bar(name) is { } share)
            {
                SizeBar(slide, shape, name, share);
            }
        }

        FillText(slide, lookup);
        foreach (var shape in slide.Descendants<P.Shape>().ToList())
        {
            var name = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value ?? string.Empty;
            if (lookup.Picture(name) is { } picture)
            {
                Picture(part, shape, picture);
            }
        }

        slide.Save();
    }

    private static void FillTables(P.Slide slide, Lookup lookup)
    {
        foreach (var frame in slide.Descendants<P.GraphicFrame>().ToList())
        {
            if (frame.Descendants<A.Table>().FirstOrDefault() is not { } table)
            {
                continue;
            }

            foreach (var row in table.Elements<A.TableRow>().ToList())
            {
                var list = FieldsOf(row).Select(StatusReportFields.PrefixOf).FirstOrDefault(StatusReportFields.Lists.Contains);
                if (list is null)
                {
                    continue;
                }

                var items = lookup.List(list);
                if (items.Count == 0)
                {
                    frame.Remove();
                    break;
                }

                foreach (var item in items)
                {
                    var copy = (A.TableRow)row.CloneNode(true);
                    FillText(copy, lookup.With(item));
                    row.InsertBeforeSelf(copy);
                }

                row.Remove();
            }
        }
    }

    private static void FillText(OpenXmlElement root, Lookup lookup)
    {
        foreach (var paragraph in root.Descendants<A.Paragraph>().ToList())
        {
            if (Replace(paragraph, lookup))
            {
                SplitLines(paragraph);
            }
        }
    }

    /// <summary>
    /// Puts the value of each field of the paragraph in the piece of text where the field starts, with its format, and
    /// empties the other pieces of the field; whether the paragraph had any field.
    /// </summary>
    private static bool Replace(A.Paragraph paragraph, Lookup lookup)
    {
        var runs = paragraph.Elements<A.Run>().ToList();
        var texts = runs.Select(run => run.Text?.Text ?? string.Empty).ToList();
        var matches = Field().Matches(string.Concat(texts));
        if (matches.Count == 0)
        {
            return false;
        }

        for (var index = matches.Count - 1; index >= 0; index--)
        {
            var match = matches[index];
            var value = lookup.Field(match.Groups[1].Value) ?? string.Empty;
            var (startRun, startOffset) = Locate(texts, match.Index);
            var (endRun, endOffset) = Locate(texts, match.Index + match.Length - 1);
            if (startRun == endRun)
            {
                texts[startRun] = texts[startRun][..startOffset] + value + texts[startRun][(endOffset + 1)..];
                continue;
            }

            texts[startRun] = texts[startRun][..startOffset] + value;
            for (var middle = startRun + 1; middle < endRun; middle++)
            {
                texts[middle] = string.Empty;
            }

            texts[endRun] = texts[endRun][(endOffset + 1)..];
        }

        for (var index = 0; index < runs.Count; index++)
        {
            if (texts[index].Length == 0 && index > 0)
            {
                runs[index].Remove();
            }
            else
            {
                runs[index].Text ??= new A.Text();
                runs[index].Text!.Text = texts[index];
            }
        }

        return true;
    }

    private static (int Run, int Offset) Locate(List<string> texts, int position)
    {
        var start = 0;
        for (var index = 0; index < texts.Count; index++)
        {
            if (position < start + texts[index].Length)
            {
                return (index, position - start);
            }

            start += texts[index].Length;
        }

        return (texts.Count - 1, texts[^1].Length);
    }

    /// <summary>A paragraph whose text has line breaks becomes one paragraph for each line, with the same format.</summary>
    private static void SplitLines(A.Paragraph paragraph)
    {
        if (!paragraph.Elements<A.Run>().Any(run => run.Text?.Text.Contains('\n', StringComparison.Ordinal) == true))
        {
            return;
        }

        var lines = new List<A.Paragraph>();
        var current = EmptyCopy(paragraph);
        foreach (var child in paragraph.ChildElements)
        {
            if (child is A.ParagraphProperties or A.EndParagraphRunProperties)
            {
                continue;
            }

            if (child is not A.Run run || run.Text?.Text.Contains('\n', StringComparison.Ordinal) != true)
            {
                Append(current, child.CloneNode(true));
                continue;
            }

            var parts = run.Text.Text.Split('\n');
            for (var index = 0; index < parts.Length; index++)
            {
                if (index > 0)
                {
                    lines.Add(current);
                    current = EmptyCopy(paragraph);
                }

                var piece = (A.Run)run.CloneNode(true);
                piece.Text!.Text = parts[index];
                Append(current, piece);
            }
        }

        lines.Add(current);
        foreach (var line in lines)
        {
            paragraph.InsertBeforeSelf(line);
        }

        paragraph.Remove();
    }

    private static A.Paragraph EmptyCopy(A.Paragraph paragraph)
    {
        var copy = new A.Paragraph();
        if (paragraph.ParagraphProperties is { } properties)
        {
            copy.Append(properties.CloneNode(true));
        }

        if (paragraph.Elements<A.EndParagraphRunProperties>().FirstOrDefault() is { } end)
        {
            copy.Append(end.CloneNode(true));
        }

        return copy;
    }

    private static void Append(A.Paragraph paragraph, OpenXmlElement child)
    {
        if (paragraph.Elements<A.EndParagraphRunProperties>().FirstOrDefault() is { } end)
        {
            end.InsertBeforeSelf(child);
        }
        else
        {
            paragraph.Append(child);
        }
    }

    /// <summary>The colors of a tag of a state, from the design system: the background and the text (ADR-0065).</summary>
    public static (string Background, string Text) ColorsOf(ReportTone tone)
    {
        var tokens = ReportTokens.Default;
        var name = tone switch
        {
            ReportTone.Danger => "danger",
            ReportTone.Warning => "warning",
            ReportTone.Success => "success",
            _ => "neutral",
        };
        return (tokens.Color($"status-{name}-bg"), tokens.Color($"status-{name}-text"));
    }

    /// <summary>The icon of a tone, so a state is never told by the color alone.</summary>
    public static A.ShapeTypeValues IconOf(ReportTone tone) => tone switch
    {
        ReportTone.Danger => A.ShapeTypeValues.Triangle,
        ReportTone.Warning => A.ShapeTypeValues.Diamond,
        ReportTone.Success => A.ShapeTypeValues.Ellipse,
        _ => A.ShapeTypeValues.Donut,
    };

    private static void ColorTag(P.Shape shape, ReportTone tone)
    {
        var (background, text) = ColorsOf(tone);
        SetFill(shape, background);
        foreach (var run in shape.Descendants<A.Run>())
        {
            var properties = run.RunProperties ??= new A.RunProperties();
            properties.RemoveAllChildren<A.SolidFill>();
            properties.RemoveAllChildren<A.NoFill>();
            properties.PrependChild(new A.SolidFill(new A.RgbColorModelHex { Val = text }));
        }
    }

    private static void ColorIcon(P.Shape shape, ReportTone tone)
    {
        SetFill(shape, ColorsOf(tone).Text);
        if (shape.ShapeProperties?.GetFirstChild<A.PresetGeometry>() is { } geometry)
        {
            geometry.Preset = IconOf(tone);
        }
    }

    private static void SetFill(P.Shape shape, string color)
    {
        var properties = shape.ShapeProperties ??= new P.ShapeProperties();
        properties.RemoveAllChildren<A.SolidFill>();
        properties.RemoveAllChildren<A.NoFill>();
        var fill = new A.SolidFill(new A.RgbColorModelHex { Val = color });
        if (properties.GetFirstChild<A.PresetGeometry>() is { } geometry)
        {
            geometry.InsertAfterSelf(fill);
        }
        else
        {
            properties.Append(fill);
        }
    }

    private static void SizeBar(P.Slide slide, P.Shape bar, string name, decimal share)
    {
        var track = slide.Descendants<P.Shape>()
            .FirstOrDefault(shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == $"{name}.trilho");
        var width = track?.ShapeProperties?.Transform2D?.Extents?.Cx?.Value;
        if (width is null || bar.ShapeProperties?.Transform2D?.Extents is not { } extents)
        {
            return;
        }

        extents.Cx = (long)Math.Round(width.Value * Math.Clamp(share, 0m, 1m), MidpointRounding.AwayFromZero);
    }

    /// <summary>The shape of a logo becomes the picture, fitted in it and centered, with the name as its description.</summary>
    private static void Picture(SlidePart part, P.Shape shape, ReportPicture picture)
    {
        var properties = shape.NonVisualShapeProperties!.NonVisualDrawingProperties!;
        var transform = shape.ShapeProperties?.Transform2D;
        if (transform?.Offset is not { } offset || transform.Extents is not { } extents)
        {
            return;
        }

        var (x, y, width, height) = Fit(
            offset.X!.Value, offset.Y!.Value, extents.Cx!.Value, extents.Cy!.Value, ImageSize.Of(picture.Content, picture.ContentType));
        var blip = new A.Blip();
        if (picture.ContentType == "image/svg+xml")
        {
            blip.Embed = Image(part, EmptyPng, ImagePartType.Png);
            var svg = new OpenXmlUnknownElement("asvg", "svgBlip", SvgNamespace);
            svg.SetAttribute(new OpenXmlAttribute(
                "r", "embed", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", Image(part, picture.Content, ImagePartType.Svg)));
            blip.Append(new A.BlipExtensionList(new A.BlipExtension(svg) { Uri = SvgExtension }));
        }
        else
        {
            blip.Embed = Image(part, picture.Content, picture.ContentType == "image/png" ? ImagePartType.Png : ImagePartType.Jpeg);
        }

        var name = string.Concat(shape.Descendants<A.Text>().Select(text => text.Text));
        var pictureShape = new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = properties.Id, Name = properties.Name, Description = name },
                new P.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true }),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.BlipFill(blip, new A.Stretch(new A.FillRectangle())),
            new P.ShapeProperties(
                new A.Transform2D(new A.Offset { X = x, Y = y }, new A.Extents { Cx = width, Cy = height }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));
        shape.InsertBeforeSelf(pictureShape);
        shape.Remove();
    }

    private static string Image(SlidePart part, byte[] content, PartTypeInfo type)
    {
        var image = part.AddImagePart(type);
        using (var stream = new MemoryStream(content, writable: false))
        {
            image.FeedData(stream);
        }

        return part.GetIdOfPart(image);
    }

    /// <summary>The place of a picture of the size given inside a box, as large as fits, centered; the whole box without a size.</summary>
    private static (long X, long Y, long Width, long Height) Fit(long x, long y, long width, long height, (double Width, double Height)? size)
    {
        if (size is not { Width: > 0, Height: > 0 } image)
        {
            return (x, y, width, height);
        }

        var scale = Math.Min(width / image.Width, height / image.Height);
        var fitted = ((long)(image.Width * scale), (long)(image.Height * scale));
        return (x + ((width - fitted.Item1) / 2), y + ((height - fitted.Item2) / 2), fitted.Item1, fitted.Item2);
    }

    /// <summary>The values a slide reads: the field is looked for in each place, the innermost first.</summary>
    private sealed class Lookup(IReadOnlyList<ReportScope> scopes)
    {
        public Lookup With(ReportScope item) => new([item, .. scopes]);

        public string? Field(string name) =>
            scopes.Select(scope => scope.Fields.TryGetValue(name, out var value) ? value : null).FirstOrDefault(value => value is not null);

        public IReadOnlyList<ReportScope> List(string name) =>
            scopes.Select(scope => scope.Lists.TryGetValue(name, out var list) ? list : null).FirstOrDefault(list => list is not null) ?? [];

        public ReportTone? Tone(string name) =>
            scopes.Select(scope => scope.Tones.TryGetValue(name, out var tone) ? tone : (ReportTone?)null).FirstOrDefault(tone => tone is not null);

        public decimal? Bar(string name) =>
            scopes.Select(scope => scope.Bars.TryGetValue(name, out var share) ? share : (decimal?)null).FirstOrDefault(share => share is not null);

        public ReportPicture? Picture(string name) =>
            scopes.Select(scope => scope.Pictures.TryGetValue(name, out var picture) ? picture : null).FirstOrDefault(picture => picture is not null);
    }
}

/// <summary>The size of an image, from its content: the header of a PNG, the frame of a JPG or the size of an SVG.</summary>
internal static class ImageSize
{
    public static (double Width, double Height)? Of(byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        try
        {
            return contentType switch
            {
                "image/png" when content.Length >= 24 =>
                    (BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(16)), BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(20))),
                "image/jpeg" => OfJpeg(content),
                "image/svg+xml" => OfSvg(content),
                _ => null,
            };
        }
        catch (Exception exception) when (exception is XmlException or ArgumentException or FormatException or IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static (double, double)? OfJpeg(byte[] content)
    {
        var index = 2;
        while (index + 9 < content.Length && content[index] == 0xFF)
        {
            var marker = content[index + 1];
            var length = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 2));
            // The frames that start a picture, but the ones of the tables (0xC4, 0xC8, 0xCC).
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return (BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 7)), BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 5)));
            }

            index += 2 + length;
        }

        return null;
    }

    private static (double, double)? OfSvg(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        reader.MoveToContent();
        if (reader.GetAttribute("viewBox") is { } box
            && box.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries) is { Length: 4 } parts)
        {
            return (Number(parts[2]), Number(parts[3]));
        }

        return reader.GetAttribute("width") is { } width && reader.GetAttribute("height") is { } height
            ? (Number(width), Number(height))
            : null;
    }

    private static double Number(string value) =>
        double.Parse(new string([.. value.TakeWhile(character => char.IsDigit(character) || character is '.' or '-')]), CultureInfo.InvariantCulture);
}
