using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using GerenWare.Projects.Application;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;
using GerenWare.Projects.Infrastructure.StatusReports;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace GerenWare.Projects.Tests;

// Spec: relatorio-de-status, requirements "Rascunho do relatório", "Textos e riscos do relatório", "O que vem do
// sistema", "Riscos no relatório", "Horas no relatório", "Modelo padrão do relatório" and "Campos do modelo" (module side).
public sealed class StatusReportTests
{
    private static readonly HashSet<int> OpenTickets = [539, 541, 545];
    private static readonly DateOnly Today = new(2026, 11, 20);

    private static (StatusReportForm? Form, IReadOnlyList<StatusReportField> Invalid) FormOf(
        string summary = "Cadastros validados", IReadOnlyList<StatusReportRiskInput>? risks = null) =>
        StatusReportForm.Of(summary, "Integração fiscal", "Homologar", "Aprovar o roteiro", risks ?? [], false, OpenTickets);

    [Fact(DisplayName = "Texto longo demais")]
    public void LongText_IsRefusedInItsField()
    {
        var (form, invalid) = FormOf(new string('a', StatusReportLimits.TextMaxLength + 1));

        form.Should().BeNull();
        invalid.Should().Equal(StatusReportField.Summary);
        FormOf(new string('a', StatusReportLimits.TextMaxLength)).Form.Should().NotBeNull();
    }

    [Fact]
    public void Texts_KeepTheLineBreaks_AndRefuseOtherControlCharacters()
    {
        StatusReportTexts.TextOf("  Primeira linha\r\nSegunda\t linha  ", 100).Should().Be("Primeira linha\nSegunda\t linha");
        StatusReportTexts.TextOf("Texto\u0007", 100).Should().BeNull();
        StatusReportTexts.TextOf(null, 100).Should().BeEmpty();
    }

    [Fact(DisplayName = "Chamado fechado como risco")]
    public void RiskThatIsNotAnOpenTicket_IsRefused()
    {
        var (form, invalid) = FormOf(risks: [new StatusReportRiskInput(538, RiskLevel.High, string.Empty)]);

        form.Should().BeNull();
        invalid.Should().Equal(StatusReportField.Risks);
    }

    [Fact(DisplayName = "Riscos demais")]
    public void MoreThanTenRisks_AreRefused()
    {
        var open = Enumerable.Range(1, 11).ToHashSet();

        var (form, invalid) = StatusReportForm.Of(
            null, null, null, null, [.. open.Select(number => new StatusReportRiskInput(number, RiskLevel.Low, null))], false, open);

        form.Should().BeNull();
        invalid.Should().Equal(StatusReportField.Risks);
    }

    [Fact]
    public void Risks_RefuseARepeatedTicket_AMissingLevel_AndALongPointOfAttention()
    {
        FormOf(risks: [new(539, RiskLevel.High, null), new(539, RiskLevel.Low, null)]).Invalid.Should().Equal(StatusReportField.Risks);
        FormOf(risks: [new(539, null, null)]).Invalid.Should().Equal(StatusReportField.Risks);
        FormOf(risks: [new(539, RiskLevel.High, new string('a', StatusReportLimits.AttentionMaxLength + 1))])
            .Invalid.Should().Equal(StatusReportField.Risks);
        FormOf(risks: [new(539, RiskLevel.High, "Depende do fornecedor"), new(541, RiskLevel.Low, null)])
            .Form!.Risks.Should().Equal(new StatusReportRisk(539, RiskLevel.High, "Depende do fornecedor"), new StatusReportRisk(541, RiskLevel.Low, string.Empty));
    }

    [Fact(DisplayName = "Rascunho sem relatório anterior")]
    public void FirstDraft_SuggestsTheTicketsThatThreatenMilestones_AsMedium()
    {
        StatusReportSuggestions.Of(null, OpenTickets, [545, 539])
            .Should().Equal(new StatusReportRisk(545, RiskLevel.Medium, string.Empty), new StatusReportRisk(539, RiskLevel.Medium, string.Empty));
    }

    [Fact(DisplayName = "Textos do último relatório")]
    public void NextDraft_KeepsTheRisksOfTheLastReport_FirstAndWithTheirLevel()
    {
        var notes = new StatusReportNotes(Guid.CreateVersion7());
        notes.Keep(FormOf(risks: [new(541, RiskLevel.High, "Depende do fornecedor")]).Form!, Guid.CreateVersion7(), DateTime.UtcNow);

        StatusReportSuggestions.Of(notes, OpenTickets, [539, 541])
            .Should().Equal(new StatusReportRisk(541, RiskLevel.High, "Depende do fornecedor"), new StatusReportRisk(539, RiskLevel.Medium, string.Empty));
        notes.Texts.Summary.Should().Be("Cadastros validados");
    }

    [Fact(DisplayName = "Risco do último relatório já resolvido")]
    public void RiskOfTheLastReportNoLongerOpen_IsNotSuggested()
    {
        var notes = new StatusReportNotes(Guid.CreateVersion7());
        notes.Keep(FormOf(risks: [new(541, RiskLevel.High, null)]).Form!, Guid.CreateVersion7(), DateTime.UtcNow);

        StatusReportSuggestions.Of(notes, new HashSet<int> { 539 }, []).Should().BeEmpty();
    }

    [Fact(DisplayName = "Relatório abre sem reparo")]
    public void Report_PassesTheValidator_WithTheSlidesInTheOrderOfTheTemplate()
    {
        using var document = Render(Data() with { Risks = [Risk(539), Risk(541)], Hours = Hours(200m) });

        new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken).Should().BeEmpty();
        TitlesOf(document).Should().Equal("Status do projeto", "Cronograma", "Próximos passos", "Risco", "Risco", "Horas do projeto");
        document.PresentationPart!.Presentation!.SlideSize!.Cx!.Value.Should().Be(12192000);
    }

    [Fact(DisplayName = "Relatório sem riscos")]
    public void ReportWithoutRisks_HasNoSlideOfARisk()
    {
        using var document = Render(Data());

        new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken).Should().BeEmpty();
        TitlesOf(document).Should().Equal("Status do projeto", "Cronograma", "Próximos passos");
    }

    [Fact(DisplayName = "Risco com chamado no fornecedor")]
    public void RiskSlide_ShowsTheTicket_TheVendor_AndTheMilestones()
    {
        using var document = Render(Data() with { Risks = [Risk(539)] });

        var text = TextOf(Slides(document)[3]);
        text.Should().Contain("#539 · Rejeição 539 na emissão").And.Contain("Em andamento · prazo vencido").And.Contain("Alta");
        text.Should().Contain("Classificação: Alta").And.Contain("Depende do retorno do fornecedor");
        text.Should().Contain("Fornecedor Fictício · 77812 · Em análise").And.Contain("Homologação").And.Contain("04/12/2026");
        var tag = ShapeNamed(Slides(document)[3], "risco.classificacao");
        tag.ShapeProperties!.GetFirstChild<A.SolidFill>()!.RgbColorModelHex!.Val!.Value.Should().Be(StatusReportFiller.ColorsOf(ReportTone.Danger).Background);
        ShapeNamed(Slides(document)[3], "risco.classificacao.icone").ShapeProperties!.GetFirstChild<A.PresetGeometry>()!.Preset!.Value
            .Should().Be(A.ShapeTypeValues.Triangle);
    }

    [Fact(DisplayName = "Relatório sem horas")]
    public void ReportWithoutHours_HasNoSlideOfTheHours()
    {
        using var document = Render(Data() with { Hours = null });

        TitlesOf(document).Should().NotContain("Horas do projeto");
        string.Concat(Slides(document).Select(TextOf)).Should().NotContain("Contratadas").And.NotContain("h apontadas");
    }

    [Fact(DisplayName = "Relatório com horas")]
    public void ReportWithHours_ShowsTheTotals_AndTheHoursOfTheTasks()
    {
        using var document = Render(Data() with { Hours = Hours(200m) });

        var text = TextOf(Slides(document).Last());
        text.Should().Contain("200 h").And.Contain("20 h").And.Contain("180 h").And.Contain("40 h").And.Contain("Dentro das horas");
        text.Should().Contain("Configurar plano de contas").And.Contain("32 h").And.Contain("6 h apontadas fora das tarefas do cronograma");
    }

    [Fact(DisplayName = "Projeto sem horas contratadas")]
    public void ProjectWithoutContractedHours_SaysSo_AndShowsOnlyTheLoggedHours()
    {
        using var document = Render(Data() with { Hours = Hours(null) });

        var text = TextOf(Slides(document).Last());
        text.Should().Contain("Sem horas contratadas").And.Contain("180 h");
        ShapeNamed(Slides(document).Last(), "horas.situacao").ShapeProperties!.GetFirstChild<A.SolidFill>()!.RgbColorModelHex!.Val!.Value
            .Should().Be(StatusReportFiller.ColorsOf(ReportTone.Neutral).Background);
    }

    [Fact]
    public void HoursOfManyTasks_ShowFifteen_AndTheTotalsOfTheOthers()
    {
        var tasks = Enumerable.Range(1, 18).Select(index => new ReportTaskHours(Guid.CreateVersion7(), $"Tarefa {index}", 10m, 4.5m)).ToList();

        using var document = Render(Data() with { Hours = Hours(200m) with { Tasks = tasks } });

        var table = Slides(document).Last().Descendants<A.Table>().Single();
        table.Elements<A.TableRow>().Should().HaveCount(16);
        TextOf(Slides(document).Last()).Should().Contain("e mais 3 tarefas, com 30 h previstas e 13,5 h apontadas");
    }

    [Fact(DisplayName = "Andamento da fase contra o previsto")]
    public void ScheduleSlide_ShowsEachPhaseWithItsProgressAndThePlannedOne()
    {
        using var document = Render(Data());

        var phases = Slides(document)[1].Descendants<P.GraphicFrame>()
            .Single(frame => frame.NonVisualGraphicFrameProperties!.NonVisualDrawingProperties!.Name == "fases");
        var rows = phases.Descendants<A.TableRow>().Select(row => row.Elements<A.TableCell>().Select(cell => cell.InnerText).ToList()).ToList();
        rows.Skip(1).First().Should().Equal("Configuração", "13/10/2026 a 30/10/2026", "30%", "50%");
    }

    [Fact(DisplayName = "Lista numa tabela")]
    public void ListInATable_RepeatsTheModelRowForEachItem()
    {
        using var document = Render(Data());

        var phases = Slides(document)[1].Descendants<P.GraphicFrame>()
            .Single(frame => frame.NonVisualGraphicFrameProperties!.NonVisualDrawingProperties!.Name == "fases");
        phases.Descendants<A.TableRow>().Should().HaveCount(5, "the titles and one row for each of the four phases");
        TextOf(Slides(document)[1]).Should().NotContain("{{");
    }

    [Fact(DisplayName = "Lista vazia")]
    public void EmptyList_TakesTheTableOut_AndSaysSo()
    {
        using var document = Render(Data() with { LateTasks = new StatusReportList<StatusReportLateTask>([], 0) });

        Slides(document)[1].Descendants<P.GraphicFrame>()
            .Should().NotContain(frame => frame.NonVisualGraphicFrameProperties!.NonVisualDrawingProperties!.Name == "atrasos");
        TextOf(Slides(document)[1]).Should().Contain("Nenhuma tarefa atrasada.");
    }

    [Fact(DisplayName = "Tarefas atrasadas além do limite")]
    public void LateTasks_ShowWhatTheyPush_AndHowManyMore()
    {
        using var document = Render(Data());

        var text = TextOf(Slides(document)[1]);
        text.Should().Contain("Integrar emissão de NF-e").And.Contain("3 dias úteis")
            .And.Contain("o marco Homologação em 2 dias úteis e mais 1 marco").And.Contain("e mais 2 tarefas atrasadas");
        text.Should().Contain("Devia ter começado em 16/11");
    }

    [Fact(DisplayName = "Projeto sem cronograma")]
    public void ProjectWithoutSchedule_SaysSoInTheProgressThePhasesAndTheMilestones()
    {
        using var document = Render(Data() with
        {
            Progress = null,
            Deadline = null,
            Phases = new StatusReportList<StatusReportPhase>([], 0),
            Milestones = [],
            LateTasks = new StatusReportList<StatusReportLateTask>([], 0),
        });

        new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken).Should().BeEmpty();
        TextOf(Slides(document)[0]).Should().Contain("Sem cronograma");
        TextOf(Slides(document)[1]).Should().Contain("Sem cronograma.").And.Contain("Nenhum marco em aberto.");
    }

    [Fact(DisplayName = "Cores da instalação no relatório")]
    public void InstallationColor_IsTheMainColorOfTheTheme()
    {
        using var document = Render(Data() with { Brand = Data().Brand with { MainColor = "#0F766E" } });

        var theme = document.PresentationPart!.SlideMasterParts.Single().ThemePart!.Theme!;
        theme.ThemeElements!.ColorScheme!.Accent1Color!.RgbColorModelHex!.Val!.Value.Should().Be("0F766E");
        using var standard = Render(Data());
        standard.PresentationPart!.SlideMasterParts.Single().ThemePart!.Theme!.ThemeElements!.ColorScheme!.Accent1Color!.RgbColorModelHex!.Val!.Value
            .Should().Be("047857");
    }

    [Fact(DisplayName = "Instalação e cliente sem logo")]
    public void WithoutLogos_TheNamesAreWritten()
    {
        using var document = Render(Data());

        var first = Slides(document)[0];
        ShapeNamed(first, "logo.instalacao").InnerText.Should().Be("Consultoria Fictícia");
        ShapeNamed(first, "logo.cliente").InnerText.Should().Be("Distribuidora Exemplo");
        first.Descendants<P.Picture>().Should().BeEmpty();
    }

    [Fact]
    public void Logos_BecomePicturesFittedInTheirShapes_AlsoAnSvg()
    {
        var svg = Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 100"><rect width="400" height="100"/></svg>""");
        var data = Data() with
        {
            Brand = Data().Brand with { Logo = new StatusReportImage(svg, "image/svg+xml") },
            ClientLogo = new StatusReportImage(Png(200, 100), "image/png"),
        };

        using var document = Render(data);

        new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken).Should().BeEmpty();
        var pictures = Slides(document)[0].Descendants<P.Picture>().ToList();
        pictures.Should().HaveCount(2);
        var client = pictures.Single(picture => picture.NonVisualPictureProperties!.NonVisualDrawingProperties!.Name == "logo.cliente");
        client.NonVisualPictureProperties!.NonVisualDrawingProperties!.Description!.Value.Should().Be("Distribuidora Exemplo");
        var size = client.ShapeProperties!.Transform2D!.Extents!;
        (size.Cx!.Value / (double)size.Cy!.Value).Should().BeApproximately(2.0, 0.01);
        var installation = pictures.Single(picture => picture.NonVisualPictureProperties!.NonVisualDrawingProperties!.Name == "logo.instalacao");
        installation.BlipFill!.Blip!.OuterXml.Should().Contain("svgBlip");
    }

    [Fact(DisplayName = "Campo quebrado em pedaços")]
    public void FieldKeptInPieces_TakesTheValue_WithTheFormatOfWhereItStarts()
    {
        var template = DefaultStatusReportTemplate.Build(null, null);
        using (var stream = new MemoryStream())
        {
            stream.Write(template);
            using (var document = PresentationDocument.Open(stream, true))
            {
                var slide = Slides(document)[2];
                var paragraph = ShapeNamed(slide, "{{texto.foco}}").Descendants<A.Paragraph>().Single();
                var run = paragraph.Elements<A.Run>().Single();
                foreach (var (text, bold) in new[] { ("{{proj", true), ("eto.no", false), ("me}}", false) })
                {
                    var piece = (A.Run)run.CloneNode(true);
                    piece.RunProperties!.Bold = bold;
                    piece.Text!.Text = text;
                    run.InsertBeforeSelf(piece);
                }

                run.Remove();
                slide.Save();
            }

            template = stream.ToArray();
        }

        using var filled = Open(StatusReportFiller.Fill(template, PptxStatusReportRenderer.ValuesOf(Data())));

        var runs = ShapeNamed(Slides(filled)[2], "{{texto.foco}}").Descendants<A.Run>().ToList();
        runs.Should().ContainSingle();
        (runs[0].Text!.Text, runs[0].RunProperties!.Bold!.Value).Should().Be(("Implantação do ERP financeiro", true));
    }

    [Fact]
    public void LineBreaksOfAText_BecomeParagraphs()
    {
        using var document = Render(Data() with
        {
            Texts = new StatusReportTexts("Primeira linha\nSegunda linha", string.Empty, string.Empty, string.Empty),
        });

        ShapeNamed(Slides(document)[0], "{{texto.resumo}}").Descendants<A.Paragraph>().Select(paragraph => paragraph.InnerText)
            .Should().Equal("Primeira linha", "Segunda linha");
    }

    [Fact]
    public void ProgressBar_IsAsWideAsTheShareOfItsTrack()
    {
        using var document = Render(Data());

        var track = ShapeNamed(Slides(document)[0], "barra.andamento.trilho").ShapeProperties!.Transform2D!.Extents!.Cx!.Value;
        var bar = ShapeNamed(Slides(document)[0], "barra.andamento").ShapeProperties!.Transform2D!.Extents!.Cx!.Value;
        ((double)bar / track).Should().BeApproximately(0.62, 0.001);
    }

    [Fact]
    public void StatusSlide_ShowsTheProjectTheNumbersAndTheSummary()
    {
        using var document = Render(Data());

        var text = TextOf(Slides(document)[0]);
        text.Should().Contain("ERP-FIN · Implantação do ERP financeiro").And.Contain("Em andamento").And.Contain("Marina Costa");
        text.Should().Contain("20/11/2026").And.Contain("62%").And.Contain("Previsto para hoje: 70%");
        text.Should().Contain("2 dias úteis depois da linha de base (02/12/2026)").And.Contain("Homologação");
        text.Should().Contain("04/12/2026 · em 10 dias úteis").And.Contain("1 vencido").And.Contain("Cadastros validados");
        text.Should().NotContain("{{");
    }

    [Fact]
    public void FileName_HasTheCodeAndTheDay()
    {
        new PptxStatusReportRenderer().FileNameOf("ERP-FIN", Today).Should().Be("ERP-FIN - Relatório de status - 2026-11-20.pptx");
    }

    private static PresentationDocument Render(StatusReportData data) =>
        Open(new PptxStatusReportRenderer().RenderAsync(data, TestContext.Current.CancellationToken).GetAwaiter().GetResult());

    private static PresentationDocument Open(byte[] content) => PresentationDocument.Open(new MemoryStream(content), false);

    private static List<P.Slide> Slides(PresentationDocument document)
    {
        var presentation = document.PresentationPart!;
        return [.. presentation.Presentation!.SlideIdList!.Elements<P.SlideId>()
            .Select(id => ((SlidePart)presentation.GetPartById(id.RelationshipId!)).Slide!)];
    }

    private static List<string> TitlesOf(PresentationDocument document) =>
        [.. Slides(document).Select(slide => ShapeNamed(slide, "Título").InnerText)];

    private static P.Shape ShapeNamed(P.Slide slide, string name) =>
        slide.Descendants<P.Shape>().First(shape => shape.NonVisualShapeProperties!.NonVisualDrawingProperties!.Name == name);

    private static string TextOf(P.Slide slide) =>
        string.Join("\n", slide.Descendants<A.Paragraph>().Select(paragraph => paragraph.InnerText));

    private static byte[] Png(int width, int height)
    {
        var png = new byte[24];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(png, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), (uint)width);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), (uint)height);
        return png;
    }

    private static StatusReportRiskItem Risk(int number) => new(
        number,
        $"Rejeição {number} na emissão",
        ReportTicketStatus.InProgress,
        ReportTicketPriority.High,
        true,
        new ReportVendorTicket("Fornecedor Fictício", "77812", ReportVendorStatus.InAnalysis),
        RiskLevel.High,
        "Depende do retorno do fornecedor",
        [new StatusReportMilestone("Homologação", new DateOnly(2026, 12, 4), 10, null)],
        0);

    private static ReportHours Hours(decimal? contracted) => new(
        contracted,
        contracted is null ? 0m : 20m,
        180m,
        contracted is null ? null : 40m,
        contracted is null ? ReportHoursState.NoContract : ReportHoursState.Within,
        [new ReportTaskHours(Guid.CreateVersion7(), "Configurar plano de contas", 40m, 32m)],
        6m);

    private static StatusReportData Data()
    {
        var projection = new ScheduleProjection(
            Guid.CreateVersion7(),
            new DateOnly(2026, 11, 20),
            new DateOnly(2026, 11, 25),
            3,
            [
                new ProjectedMilestone(Guid.CreateVersion7(), "Homologação", new DateOnly(2026, 12, 4), new DateOnly(2026, 12, 8), 2),
                new ProjectedMilestone(Guid.CreateVersion7(), "Go-live", new DateOnly(2026, 12, 11), new DateOnly(2026, 12, 15), 2),
            ],
            null);
        return new StatusReportData(
            Today,
            new StatusReportBrand("Consultoria Fictícia", null, null, null),
            "ERP-FIN",
            "Implantação do ERP financeiro",
            ProjectStatus.InProgress,
            "Distribuidora Exemplo",
            null,
            ["Marina Costa"],
            new OverviewProgress(62, 70, true),
            new OverviewDeadline(new DateOnly(2026, 12, 4), new DateOnly(2026, 12, 2), 2),
            new StatusReportList<StatusReportPhase>(
            [
                new("Configuração", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 30), 30, 50),
                new("Integrações", new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 20), 40, 60),
                new("Testes", new DateOnly(2026, 11, 23), new DateOnly(2026, 12, 4), 0, 0),
                new("Implantação", new DateOnly(2026, 12, 7), new DateOnly(2026, 12, 11), 0, null),
            ], 0),
            [new StatusReportMilestone("Homologação", new DateOnly(2026, 12, 4), 10, null)],
            new StatusReportList<StatusReportLateTask>(
            [
                new("Integrar emissão de NF-e", new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 17), 3, false, projection),
                new("Treinar usuários-chave", new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 27), 0, true, null),
            ], 2),
            7,
            1,
            new StatusReportTexts("Cadastros validados", "Implantação do ERP financeiro", "Homologar", "Aprovar o roteiro"),
            [],
            null);
    }
}
