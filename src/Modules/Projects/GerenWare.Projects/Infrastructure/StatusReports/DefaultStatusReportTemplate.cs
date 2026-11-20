using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using GerenWare.SharedKernel;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace GerenWare.Projects.Infrastructure.StatusReports;

/// <summary>
/// The default template of the status report (ADR-0165), made by code at each report, only with the fields and the
/// labels of the translation file: a 16:9 PowerPoint, white, in Calibri, with the colors of the installation in the
/// theme (ADR-0146) and the others from the design system. The slides: "Status do projeto", "Cronograma", "Próximos
/// passos", the one of a risk and "Horas do projeto", in this order.
/// </summary>
internal static class DefaultStatusReportTemplate
{
    private const long Emu = 914400;
    private const string Font = "Calibri";
    private const double Left = 0.5;
    private const double Width = 12.333;

    private static ReportTexts Texts => ReportTexts.Default;

    private static ReportTokens Tokens => ReportTokens.Default;

    /// <summary>The template, with the main and the secondary colors of the installation (<c>#RRGGBB</c>), when it has them.</summary>
    public static byte[] Build(string? mainColor, string? secondaryColor)
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();
            var masterPart = presentationPart.AddNewPart<SlideMasterPart>();
            var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
            layoutPart.SlideLayout = new P.SlideLayout(
                new P.CommonSlideData(EmptyTree()) { Name = "Blank" },
                new P.ColorMapOverride(new A.MasterColorMapping()))
            {
                Type = P.SlideLayoutValues.Blank,
            };
            layoutPart.AddPart(masterPart);
            var themePart = masterPart.AddNewPart<ThemePart>();
            themePart.Theme = Theme(mainColor, secondaryColor);
            masterPart.SlideMaster = new P.SlideMaster(
                new P.CommonSlideData(
                    new P.Background(new P.BackgroundStyleReference(new A.SchemeColor { Val = A.SchemeColorValues.Background1 }) { Index = 1001 }),
                    EmptyTree()),
                new P.ColorMap
                {
                    Background1 = A.ColorSchemeIndexValues.Light1,
                    Text1 = A.ColorSchemeIndexValues.Dark1,
                    Background2 = A.ColorSchemeIndexValues.Light2,
                    Text2 = A.ColorSchemeIndexValues.Dark2,
                    Accent1 = A.ColorSchemeIndexValues.Accent1,
                    Accent2 = A.ColorSchemeIndexValues.Accent2,
                    Accent3 = A.ColorSchemeIndexValues.Accent3,
                    Accent4 = A.ColorSchemeIndexValues.Accent4,
                    Accent5 = A.ColorSchemeIndexValues.Accent5,
                    Accent6 = A.ColorSchemeIndexValues.Accent6,
                    Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                    FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink,
                },
                new P.SlideLayoutIdList(new P.SlideLayoutId { Id = 2147483649U, RelationshipId = masterPart.GetIdOfPart(layoutPart) }),
                new P.TextStyles(new P.TitleStyle(), new P.BodyStyle(), new P.OtherStyle()));
            presentationPart.AddPart(themePart);

            var slideIds = new P.SlideIdList();
            uint id = 256;
            foreach (var slide in new[] { StatusSlide(), ScheduleSlide(), NextStepsSlide(), RiskSlide(), HoursSlide() })
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                slidePart.Slide = slide;
                slidePart.AddPart(layoutPart);
                slideIds.Append(new P.SlideId { Id = id++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
            }

            presentationPart.Presentation.Append(
                new P.SlideMasterIdList(new P.SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(masterPart) }),
                slideIds,
                new P.SlideSize { Cx = 12192000, Cy = 6858000 },
                new P.NotesSize { Cx = 6858000, Cy = 9144000 });
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private static P.Slide StatusSlide()
    {
        var canvas = Header(Texts["slides.status"]);
        canvas.Text(
            StatusReportFields.ClientLogo, 8.55, 0.4, 2.0, 0.6, 12, A.TextAlignmentTypeValues.Right, A.TextAnchoringTypeValues.Center,
            Run("{{cliente.nome}}", 12, true, Scheme(A.SchemeColorValues.Text1)));
        canvas.Text(
            Texts["shapes.projectData"], Left, 1.6, Width, 0.35, 11, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run(Texts["labels.clientLabel"], 11, false, Secondary()),
            Run("{{cliente.nome}}", 11, false, Scheme(A.SchemeColorValues.Text1)),
            Run("      " + Texts["labels.situationLabel"], 11, false, Secondary()),
            Run("{{projeto.situacao}}", 11, false, Scheme(A.SchemeColorValues.Text1)),
            Run("      " + Texts["labels.managerLabel"], 11, false, Secondary()),
            Run("{{projeto.gerente}}", 11, false, Scheme(A.SchemeColorValues.Text1)),
            Run("      " + Texts["labels.dateLabel"], 11, false, Secondary()),
            Run("{{relatorio.data}}", 11, false, Scheme(A.SchemeColorValues.Text1)));

        double[] columns = [0.5, 3.643, 6.786, 9.929];
        const double top = 2.15;
        const double cardWidth = 2.904;
        Card(canvas, columns[0], top, cardWidth, Texts["labels.progress"], "{{andamento.real}}", 24, "{{andamento.previsto}}");
        canvas.Rect(StatusReportFields.ProgressTrack, columns[0] + 0.2, top + 1.0, cardWidth - 0.4, 0.08, Scheme(A.SchemeColorValues.Accent4), null, 0);
        canvas.Rect(StatusReportFields.ProgressBar, columns[0] + 0.2, top + 1.0, cardWidth - 0.4, 0.08, Scheme(A.SchemeColorValues.Accent1), null, 0);
        Card(canvas, columns[1], top, cardWidth, Texts["labels.deadline"], "{{prazo.fim}}", 24, "{{prazo.desvio}}");
        Card(canvas, columns[2], top, cardWidth, Texts["labels.nextMilestone"], "{{marco.proximo}}", 16, "{{marco.proximo.quando}}");
        Card(canvas, columns[3], top, cardWidth, Texts["labels.tickets"], "{{chamados.abertos}}", 24, "{{chamados.vencidos}}");

        canvas.Label(Texts["labels.summary"], Left, 4.0, Width);
        canvas.Text("{{texto.resumo}}", Left, 4.4, Width, 2.55, 12, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run("{{texto.resumo}}", 12, false, Scheme(A.SchemeColorValues.Text1)));
        return canvas.Slide();
    }

    private static P.Slide ScheduleSlide()
    {
        var canvas = Header(Texts["slides.schedule"]);
        canvas.Label(Texts["labels.phases"], Left, 1.6, 4.6);
        canvas.Note("{{fases.mais}}", 5.1, 1.6, 2.3, A.TextAlignmentTypeValues.Right);
        canvas.Note("{{fases.vazio}}", Left, 2.0, 6.9, A.TextAlignmentTypeValues.Left);
        canvas.Table(
            "fases", Left, 2.0, [2.9, 2.2, 0.9, 0.9], 0.3, 10,
            [Texts["labels.phase"], Texts["labels.period"], Texts["labels.progress"], Texts["labels.planned"]],
            ["{{fase.nome}}", "{{fase.periodo}}", "{{fase.andamento}}", "{{fase.previsto}}"]);

        canvas.Label(Texts["labels.milestones"], 7.7, 1.6, 5.133);
        canvas.Note("{{marcos.vazio}}", 7.7, 2.0, 5.133, A.TextAlignmentTypeValues.Left);
        canvas.Table(
            "marcos", 7.7, 2.0, [2.533, 1.1, 1.5], 0.3, 10,
            [Texts["labels.milestone"], Texts["labels.day"], Texts["labels.milestoneState"]],
            ["{{marco.nome}}", "{{marco.dia}}", "{{marco.situacao}}"]);

        canvas.Label(Texts["labels.lateTasks"], 7.7, 3.95, 2.6);
        canvas.Note("{{atrasos.mais}}", 10.3, 3.95, 2.533, A.TextAlignmentTypeValues.Right);
        canvas.Note("{{atrasos.vazio}}", 7.7, 4.35, 5.133, A.TextAlignmentTypeValues.Left);
        canvas.Table(
            "atrasos", 7.7, 4.35, [1.9, 1.25, 1.983], 0.3, 10,
            [Texts["labels.task"], Texts["labels.delay"], Texts["labels.pushes"]],
            ["{{atraso.tarefa}}", "{{atraso.atraso}}", "{{atraso.empurra}}"]);
        return canvas.Slide();
    }

    private static P.Slide NextStepsSlide()
    {
        var canvas = Header(Texts["slides.nextSteps"]);
        (string Label, string Field)[] columns =
        [
            (Texts["labels.focus"], "{{texto.foco}}"),
            (Texts["labels.nextSteps"], "{{texto.proximosPassos}}"),
            (Texts["labels.clientNeeds"], "{{texto.cliente}}"),
        ];
        for (var index = 0; index < columns.Length; index++)
        {
            var x = Left + (index * 4.2165);
            canvas.Rect(Texts["shapes.mark"], x, 1.6, 0.6, 0.05, Scheme(A.SchemeColorValues.Accent1), null, 0);
            canvas.Label(columns[index].Label, x, 1.75, 3.9);
            canvas.Text(columns[index].Field, x, 2.2, 3.9, 4.75, 12, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
                Run(columns[index].Field, 12, false, Scheme(A.SchemeColorValues.Text1)));
        }

        return canvas.Slide();
    }

    private static P.Slide RiskSlide()
    {
        var canvas = Header(Texts["slides.risk"]);
        canvas.Text(Texts["shapes.ticket"], Left, 1.6, 9.4, 0.8, 20, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run("#{{risco.numero}} · {{risco.titulo}}", 20, true, Scheme(A.SchemeColorValues.Text1)));
        canvas.Tag(StatusReportFields.RiskLevelTag, 10.333, 1.62, 2.5, 0.42, 11,
            Run(Texts["labels.classification"], 11, false, null),
            Run("{{risco.classificacao}}", 11, true, null));

        (string Label, string Field, double X, double Width)[] facts =
        [
            (Texts["labels.riskStatus"], "{{risco.status}}", Left, 3.0),
            (Texts["labels.riskPriority"], "{{risco.prioridade}}", 3.7, 3.0),
            (Texts["labels.riskVendor"], "{{risco.fornecedor}}", 6.9, 5.933),
        ];
        foreach (var (label, field, x, width) in facts)
        {
            canvas.Text(label, x, 2.6, width, 0.3, 10, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
                Run(label, 10, false, Secondary()));
            canvas.Text(field, x, 2.9, width, 0.45, 13, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
                Run(field, 13, false, Scheme(A.SchemeColorValues.Text1)));
        }

        canvas.Label(Texts["labels.attention"], Left, 3.6, 6.0);
        canvas.Text("{{risco.pontoDeAtencao}}", Left, 4.0, 6.0, 2.95, 12, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run("{{risco.pontoDeAtencao}}", 12, false, Scheme(A.SchemeColorValues.Text1)));
        canvas.Label(Texts["labels.riskMilestones"], 7.0, 3.6, 5.833);
        canvas.Note("{{riscoMarcos.vazio}}", 7.0, 4.0, 5.833, A.TextAlignmentTypeValues.Left);
        canvas.Table(
            "riscoMarcos", 7.0, 4.0, [3.133, 1.2, 1.5], 0.3, 10,
            [Texts["labels.milestone"], Texts["labels.day"], Texts["labels.milestoneState"]],
            ["{{riscoMarco.nome}}", "{{riscoMarco.dia}}", "{{riscoMarco.situacao}}"]);
        canvas.Note("{{riscoMarcos.mais}}", 7.0, 6.0, 5.833, A.TextAlignmentTypeValues.Left);
        return canvas.Slide();
    }

    private static P.Slide HoursSlide()
    {
        var canvas = Header(Texts["slides.hours"]);
        double[] columns = [0.5, 3.643, 6.786, 9.929];
        const double top = 1.6;
        const double cardWidth = 2.904;
        (string Label, string Field)[] cards =
        [
            (Texts["labels.contracted"], "{{horas.contratadas}}"),
            (Texts["labels.additional"], "{{horas.adicionais}}"),
            (Texts["labels.logged"], "{{horas.apontadas}}"),
            (Texts["labels.balance"], "{{horas.saldo}}"),
        ];
        for (var index = 0; index < cards.Length; index++)
        {
            Card(canvas, columns[index], top, cardWidth, cards[index].Label, cards[index].Field, 22, null, 1.25);
        }

        canvas.Tag(StatusReportFields.HoursStateTag, columns[3] + 0.2, top + 0.88, cardWidth - 0.4, 0.28, 9,
            Run("{{horas.situacao}}", 9, true, null));

        canvas.Label(Texts["labels.taskHours"], Left, 3.05, 6.0);
        canvas.Note("{{horas.fora}}", 6.5, 3.05, 6.333, A.TextAlignmentTypeValues.Right);
        canvas.Note("{{horasTarefas.vazio}}", Left, 3.45, Width, A.TextAlignmentTypeValues.Left);
        canvas.Table(
            "horasTarefas", Left, 3.45, [8.333, 2.0, 2.0], 0.2, 9,
            [Texts["labels.task"], Texts["labels.plannedHours"], Texts["labels.loggedHours"]],
            ["{{horasTarefa.nome}}", "{{horasTarefa.previstas}}", "{{horasTarefa.apontadas}}"]);
        canvas.Note("{{horasTarefas.mais}}", Left, 6.78, Width, A.TextAlignmentTypeValues.Left, 9);
        return canvas.Slide();
    }

    /// <summary>
    /// A slide with what every slide has: the title, the project, the logo of the installation (or its name), the line
    /// of the main color and the foot with the project, the report, the day and the number of the slide.
    /// </summary>
    private static Canvas Header(string title)
    {
        var canvas = new Canvas();
        canvas.Text(Texts["shapes.title"], Left, 0.4, 7.9, 0.55, 24, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run(title, 24, true, Scheme(A.SchemeColorValues.Text1)));
        canvas.Text(Texts["shapes.project"], Left, 0.95, 7.9, 0.35, 12, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run("{{projeto.codigo}} · {{projeto.nome}}", 12, false, Secondary()));
        canvas.Text(
            StatusReportFields.InstallationLogo, 10.833, 0.4, 2.0, 0.6, 12, A.TextAlignmentTypeValues.Right, A.TextAnchoringTypeValues.Center,
            Run("{{instalacao.nome}}", 12, true, Scheme(A.SchemeColorValues.Text1)));
        canvas.Rect(Texts["shapes.line"], Left, 1.42, Width, 0.03, Scheme(A.SchemeColorValues.Accent1), null, 0);
        canvas.Text(Texts["shapes.footer"], Left, 7.1, 10.0, 0.3, 9, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run($"{{{{projeto.codigo}}}} · {Texts["report"]} · {{{{relatorio.data}}}}", 9, false, Secondary()));
        canvas.SlideNumber(11.833, 7.1, 1.0, 0.3);
        return canvas;
    }

    private static void Card(
        Canvas canvas, double x, double y, double width, string label, string field, int size, string? detail, double height = 1.6)
    {
        canvas.Rect(Texts["shapes.card"], x, y, width, height, Scheme(A.SchemeColorValues.Background1), Scheme(A.SchemeColorValues.Accent3), Tokens.Radius("radius-md"));
        canvas.Text(label, x + 0.2, y + 0.12, width - 0.4, 0.3, 10, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run(label, 10, false, Secondary()));
        canvas.Text(field, x + 0.2, y + 0.42, width - 0.4, 0.55, size, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
            Run(field, size, true, Scheme(A.SchemeColorValues.Text1)));
        if (detail is not null)
        {
            canvas.Text(detail, x + 0.2, y + 1.12, width - 0.4, 0.42, 10, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
                Run(detail, 10, false, Secondary()));
        }
    }

    private static A.Theme Theme(string? mainColor, string? secondaryColor)
    {
        static A.RgbColorModelHex Rgb(string value) => new() { Val = value };
        static string Hex(string? color, string fallback) => color is { Length: 7 } ? color[1..].ToUpperInvariant() : fallback;
        var name = Product.Name;
        return new A.Theme(
            new A.ThemeElements(
                new A.ColorScheme(
                    new A.Dark1Color(Rgb(Tokens.Color("text"))),
                    new A.Light1Color(Rgb(Tokens.Color("bg-surface"))),
                    new A.Dark2Color(Rgb(Tokens.Color("text-secondary"))),
                    new A.Light2Color(Rgb(Tokens.Color("bg-subtle"))),
                    new A.Accent1Color(Rgb(Hex(mainColor, Tokens.Color("action")))),
                    new A.Accent2Color(Rgb(Hex(secondaryColor, Tokens.Color("bg-selected")))),
                    new A.Accent3Color(Rgb(Tokens.Color("border"))),
                    new A.Accent4Color(Rgb(Tokens.Color("progress-track"))),
                    new A.Accent5Color(Rgb(Tokens.Color("status-info-text"))),
                    new A.Accent6Color(Rgb(Tokens.Color("status-warning-text"))),
                    new A.Hyperlink(Rgb(Tokens.Color("action-text"))),
                    new A.FollowedHyperlinkColor(Rgb(Tokens.Color("action-text"))))
                { Name = name },
                new A.FontScheme(
                    new A.MajorFont(new A.LatinFont { Typeface = Font }, new A.EastAsianFont { Typeface = string.Empty }, new A.ComplexScriptFont { Typeface = string.Empty }),
                    new A.MinorFont(new A.LatinFont { Typeface = Font }, new A.EastAsianFont { Typeface = string.Empty }, new A.ComplexScriptFont { Typeface = string.Empty }))
                { Name = name },
                new A.FormatScheme(
                    new A.FillStyleList(PlaceholderFill(), PlaceholderFill(), PlaceholderFill()),
                    new A.LineStyleList(PlaceholderLine(), PlaceholderLine(), PlaceholderLine()),
                    new A.EffectStyleList(new A.EffectStyle(new A.EffectList()), new A.EffectStyle(new A.EffectList()), new A.EffectStyle(new A.EffectList())),
                    new A.BackgroundFillStyleList(PlaceholderFill(), PlaceholderFill(), PlaceholderFill()))
                { Name = name }),
            new A.ObjectDefaults(),
            new A.ExtraColorSchemeList())
        { Name = name };
    }

    private static A.SolidFill PlaceholderFill() => new(new A.SchemeColor { Val = A.SchemeColorValues.PhColor });

    private static A.Outline PlaceholderLine() => new(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor })) { Width = 9525 };

    private static P.ShapeTree EmptyTree() =>
        new(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                new P.NonVisualGroupShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.GroupShapeProperties(new A.TransformGroup()));

    private static long E(double inches) => (long)Math.Round(inches * Emu, MidpointRounding.AwayFromZero);

    private static A.SchemeColor Scheme(A.SchemeColorValues value) => new() { Val = value };

    private static A.SchemeColor Secondary() => Scheme(A.SchemeColorValues.Text2);

    /// <summary>A piece of text in Calibri, of the size and the color given; without a color, the one of where it is.</summary>
    private static A.Run Run(string text, int size, bool bold, OpenXmlElement? color)
    {
        var properties = new A.RunProperties { Language = "pt-BR", FontSize = size * 100, Bold = bold, Dirty = false };
        if (color is not null)
        {
            properties.Append(new A.SolidFill(color));
        }

        properties.Append(new A.LatinFont { Typeface = "+mn-lt" });
        return new A.Run(properties, new A.Text(text));
    }

    /// <summary>The shapes of a slide, with their ids in order.</summary>
    private sealed class Canvas
    {
        private readonly List<OpenXmlElement> _shapes = [];
        private uint _next = 2;

        public P.Slide Slide()
        {
            var tree = EmptyTree();
            tree.Append(_shapes);
            return new P.Slide(new P.CommonSlideData(tree), new P.ColorMapOverride(new A.MasterColorMapping()));
        }

        public void Text(
            string name,
            double x,
            double y,
            double width,
            double height,
            int size,
            A.TextAlignmentTypeValues alignment,
            A.TextAnchoringTypeValues anchor,
            params A.Run[] runs)
        {
            _shapes.Add(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = name },
                    new P.NonVisualShapeDrawingProperties { TextBox = true },
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(Transform(x, y, width, height), Geometry(A.ShapeTypeValues.Rectangle, 0, 0), new A.NoFill()),
                Body(anchor, 0, Paragraph(alignment, size, runs))));
        }

        /// <summary>A title of a part of a slide, in the text color.</summary>
        public void Label(string text, double x, double y, double width) =>
            Text(text, x, y, width, 0.35, 14, A.TextAlignmentTypeValues.Left, A.TextAnchoringTypeValues.Top,
                Run(text, 14, true, Scheme(A.SchemeColorValues.Text1)));

        /// <summary>A short line in the secondary color, such as the text of an empty list or "e mais N".</summary>
        public void Note(string field, double x, double y, double width, A.TextAlignmentTypeValues alignment, int size = 10) =>
            Text(field, x, y, width, size < 10 ? 0.25 : 0.35, size, alignment, A.TextAnchoringTypeValues.Top, Run(field, size, false, Secondary()));

        public void Rect(string name, double x, double y, double width, double height, OpenXmlElement fill, OpenXmlElement? line, int radius)
        {
            var properties = new P.ShapeProperties(
                Transform(x, y, width, height),
                Geometry(radius > 0 ? A.ShapeTypeValues.RoundRectangle : A.ShapeTypeValues.Rectangle, radius, Math.Min(width, height)),
                new A.SolidFill(fill.CloneNode(true)));
            properties.Append(line is null
                ? new A.Outline(new A.NoFill())
                : new A.Outline(new A.SolidFill(line.CloneNode(true))) { Width = 12700 });
            _shapes.Add(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = name },
                    new P.NonVisualShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                properties,
                Body(A.TextAnchoringTypeValues.Center, 0, Paragraph(A.TextAlignmentTypeValues.Left, 10))));
        }

        /// <summary>A tag of a state (ADR-0065): the shape colored by the tone of the value and the icon before the text.</summary>
        public void Tag(string name, double x, double y, double width, double height, int size, params A.Run[] runs)
        {
            var (background, text) = StatusReportFiller.ColorsOf(ReportTone.Neutral);
            var radius = Tokens.Radius("radius-sm");
            _shapes.Add(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = name },
                    new P.NonVisualShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(
                    Transform(x, y, width, height),
                    Geometry(A.ShapeTypeValues.RoundRectangle, radius, Math.Min(width, height)),
                    new A.SolidFill(new A.RgbColorModelHex { Val = background }),
                    new A.Outline(new A.NoFill())),
                Body(A.TextAnchoringTypeValues.Center, height * 0.85, Paragraph(A.TextAlignmentTypeValues.Left, size, runs))));
            var icon = height * 0.4;
            _shapes.Add(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = StatusReportFields.IconOf(name) },
                    new P.NonVisualShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(
                    Transform(x + (height * 0.3), y + ((height - icon) / 2), icon, icon),
                    Geometry(StatusReportFiller.IconOf(ReportTone.Neutral), 0, 0),
                    new A.SolidFill(new A.RgbColorModelHex { Val = text }),
                    new A.Outline(new A.NoFill()))));
        }

        /// <summary>A table of a list: the row of the titles and the model row, repeated for each item.</summary>
        public void Table(string name, double x, double y, double[] columns, double rowHeight, int size, string[] titles, string[] fields)
        {
            var table = new A.Table(
                new A.TableProperties { FirstRow = true },
                new A.TableGrid(columns.Select(width => new A.GridColumn { Width = E(width) })),
                Row(titles, rowHeight, size, header: true),
                Row(fields, rowHeight, size, header: false));
            _shapes.Add(new P.GraphicFrame(
                new P.NonVisualGraphicFrameProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = name },
                    new P.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoGrouping = true }),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.Transform(new A.Offset { X = E(x), Y = E(y) }, new A.Extents { Cx = E(columns.Sum()), Cy = E(rowHeight * 2) }),
                new A.Graphic(new A.GraphicData(table) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" })));
        }

        public void SlideNumber(double x, double y, double width, double height)
        {
            var paragraph = new A.Paragraph(
                new A.ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Right },
                new A.Field(
                    new A.RunProperties(new A.SolidFill(Secondary()), new A.LatinFont { Typeface = "+mn-lt" }) { Language = "pt-BR", FontSize = 900 },
                    new A.Text("‹#›"))
                {
                    Id = "{6D5F2E1A-3C4B-4E7A-9F21-0B8C7D6E5A41}",
                    Type = "slidenum",
                },
                new A.EndParagraphRunProperties { Language = "pt-BR", FontSize = 900 });
            _shapes.Add(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = _next++, Name = Texts["shapes.slideNumber"] },
                    new P.NonVisualShapeDrawingProperties { TextBox = true },
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(Transform(x, y, width, height), Geometry(A.ShapeTypeValues.Rectangle, 0, 0), new A.NoFill()),
                Body(A.TextAnchoringTypeValues.Top, 0, paragraph)));
        }

        private static A.TableRow Row(string[] texts, double height, int size, bool header)
        {
            var row = new A.TableRow { Height = E(height) };
            foreach (var text in texts)
            {
                var properties = new A.TableCellProperties(
                    new A.LeftBorderLineProperties(new A.NoFill()),
                    new A.RightBorderLineProperties(new A.NoFill()),
                    new A.TopBorderLineProperties(new A.NoFill()),
                    new A.BottomBorderLineProperties(new A.SolidFill(Scheme(A.SchemeColorValues.Accent3))) { Width = 9525 })
                {
                    LeftMargin = (int)E(0.06),
                    RightMargin = (int)E(0.06),
                    TopMargin = (int)E(0.025),
                    BottomMargin = (int)E(0.025),
                    Anchor = A.TextAnchoringTypeValues.Center,
                };
                properties.Append(header
                    ? new A.SolidFill(Scheme(A.SchemeColorValues.Background2))
                    : new A.NoFill());
                row.Append(new A.TableCell(
                    new A.TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        Paragraph(A.TextAlignmentTypeValues.Left, size, Run(text, size, header, header ? Secondary() : Scheme(A.SchemeColorValues.Text1)))),
                    properties));
            }

            return row;
        }

        private static A.Transform2D Transform(double x, double y, double width, double height) =>
            new(new A.Offset { X = E(x), Y = E(y) }, new A.Extents { Cx = E(width), Cy = E(height) });

        /// <summary>The geometry; a rounded corner in pixels of the design system, at 96 pixels an inch, on its shorter side.</summary>
        private static A.PresetGeometry Geometry(A.ShapeTypeValues type, int radius, double shorterSide)
        {
            var adjust = new A.AdjustValueList();
            if (type == A.ShapeTypeValues.RoundRectangle && shorterSide > 0)
            {
                var share = (long)Math.Round(radius / 96.0 / shorterSide * 100000, MidpointRounding.AwayFromZero);
                adjust.Append(new A.ShapeGuide { Name = "adj", Formula = $"val {share}" });
            }

            return new A.PresetGeometry(adjust) { Preset = type };
        }

        private static P.TextBody Body(A.TextAnchoringTypeValues anchor, double leftInset, params A.Paragraph[] paragraphs)
        {
            var body = new P.TextBody(
                new A.BodyProperties
                {
                    Wrap = A.TextWrappingValues.Square,
                    LeftInset = (int)E(leftInset),
                    TopInset = 0,
                    RightInset = 0,
                    BottomInset = 0,
                    Anchor = anchor,
                },
                new A.ListStyle());
            body.Append(paragraphs);
            return body;
        }

        private static A.Paragraph Paragraph(A.TextAlignmentTypeValues alignment, int size, params A.Run[] runs)
        {
            var paragraph = new A.Paragraph(new A.ParagraphProperties { Alignment = alignment });
            paragraph.Append(runs);
            paragraph.Append(new A.EndParagraphRunProperties { Language = "pt-BR", FontSize = size * 100 });
            return paragraph;
        }
    }
}
