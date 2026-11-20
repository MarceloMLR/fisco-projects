using System.Globalization;
using GerenWare.Projects.Application;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;

namespace GerenWare.Projects.Infrastructure.StatusReports;

/// <summary>
/// The status report in PowerPoint (ADR-0165): the default template with the colors of the installation
/// (<see cref="DefaultStatusReportTemplate"/>), filled (<see cref="StatusReportFiller"/>) with the data written as the
/// screens write it, by the texts of the report.
/// </summary>
internal sealed class PptxStatusReportRenderer : IStatusReportRenderer
{
    private static ReportTexts Texts => ReportTexts.Default;

    public Task<byte[]> RenderAsync(StatusReportData data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        var template = DefaultStatusReportTemplate.Build(data.Brand.MainColor, data.Brand.SecondaryColor);
        return Task.FromResult(StatusReportFiller.Fill(template, ValuesOf(data)));
    }

    public string FileNameOf(string projectCode, DateOnly day) =>
        Texts["fileName"]
            .Replace("{code}", projectCode, StringComparison.Ordinal)
            .Replace("{day}", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
        + ".pptx";

    /// <summary>The values of the fields, as the screens write them (ADR-0013): days, business days, statuses and hours.</summary>
    public static StatusReportValues ValuesOf(StatusReportData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var report = new ReportScope();
        var fields = report.Fields;
        fields["instalacao.nome"] = data.Brand.Name;
        fields["projeto.codigo"] = data.ProjectCode;
        fields["projeto.nome"] = data.ProjectName;
        fields["projeto.situacao"] = Texts[$"projectStatuses.{data.Status}"];
        fields["projeto.gerente"] = string.Join(", ", data.Managers);
        fields["cliente.nome"] = data.ClientName ?? Texts["values.noClient"];
        fields["relatorio.data"] = ReportTexts.Day(data.Today);
        report.Pictures[StatusReportFields.InstallationLogo] = PictureOf(data.Brand.Logo);
        report.Pictures[StatusReportFields.ClientLogo] = PictureOf(data.ClientLogo);

        fields["andamento.real"] = data.Progress is { } progress ? Texts.Format("values.percent", progress.Actual) : Texts["values.none"];
        fields["andamento.previsto"] = data.Progress switch
        {
            null => Texts["values.noSchedule"],
            { Planned: { } planned } => Texts.Format("values.plannedProgress", planned),
            _ => string.Empty,
        };
        report.Bars[StatusReportFields.ProgressBar] = (data.Progress?.Actual ?? 0) / 100m;
        fields["prazo.fim"] = data.Deadline is { } deadline ? ReportTexts.Day(deadline.End) : Texts["values.none"];
        fields["prazo.desvio"] = DeviationOf(data.Deadline);
        var next = data.Milestones.FirstOrDefault();
        fields["marco.proximo"] = next is null ? Texts["values.noMilestone"] : Cut(next.Name, 60);
        fields["marco.proximo.quando"] = next is null
            ? string.Empty
            : Texts.Format("values.dayAndWhen", ReportTexts.Day(next.Day), WhenOf(next));
        fields["chamados.abertos"] = ReportTexts.Number(data.OpenTickets);
        fields["chamados.vencidos"] = data.OverdueTickets == 0 ? Texts["values.overdueNone"] : Texts.Count("values.overdue", data.OverdueTickets);
        fields["texto.resumo"] = data.Texts.Summary;
        fields["texto.foco"] = data.Texts.Focus;
        fields["texto.proximosPassos"] = data.Texts.NextSteps;
        fields["texto.cliente"] = data.Texts.ClientNeeds;

        report.Lists[StatusReportFields.Phase] = [.. data.Phases.Items.Select(phase => Scope(
            ("fase.nome", Cut(phase.Name, 60)),
            ("fase.periodo", Texts.Format("values.period", ReportTexts.Day(phase.Start), ReportTexts.Day(phase.End))),
            ("fase.andamento", Texts.Format("values.percent", phase.Progress)),
            ("fase.previsto", phase.Planned is { } planned ? Texts.Format("values.percent", planned) : Texts["values.none"])))];
        fields["fases.vazio"] = data.Phases.Items.Count == 0 ? Texts["values.noPhases"] : string.Empty;
        fields["fases.mais"] = data.Phases.More > 0 ? Texts.Count("values.morePhases", data.Phases.More) : string.Empty;
        report.Lists[StatusReportFields.Milestone] = [.. data.Milestones.Select(milestone => Scope(
            ("marco.nome", Cut(milestone.Name, 60)), ("marco.dia", ReportTexts.Day(milestone.Day)), ("marco.situacao", WhenOf(milestone))))];
        fields["marcos.vazio"] = data.Milestones.Count == 0 ? Texts["values.noMilestones"] : string.Empty;
        report.Lists[StatusReportFields.LateTask] = [.. data.LateTasks.Items.Select(task => Scope(
            ("atraso.tarefa", Cut(task.Name, 50)),
            ("atraso.atraso", task.NotStarted
                ? Texts.Format("values.shouldHaveStarted", task.Start.ToString("dd/MM", CultureInfo.InvariantCulture))
                : DaysOf(task.DaysLate)),
            ("atraso.empurra", PushesOf(task.Projection))))];
        fields["atrasos.vazio"] = data.LateTasks.Items.Count == 0 ? Texts["values.noLateTasks"] : string.Empty;
        fields["atrasos.mais"] = data.LateTasks.More > 0 ? Texts.Count("values.moreLateTasks", data.LateTasks.More) : string.Empty;
        if (data.Hours is { } hours)
        {
            Hours(report, hours);
        }

        return new StatusReportValues(report, [.. data.Risks.Select(RiskOf)], data.Hours is not null);
    }

    private static void Hours(ReportScope report, ReportHours hours)
    {
        var fields = report.Fields;
        fields["horas.contratadas"] = hours.Contracted is { } contracted ? HoursOf(contracted) : Texts["values.none"];
        fields["horas.adicionais"] = HoursOf(hours.Additional);
        fields["horas.apontadas"] = HoursOf(hours.Logged);
        fields["horas.saldo"] = hours.Balance is { } balance ? HoursOf(balance) : Texts["values.none"];
        fields["horas.situacao"] = Texts[$"hoursStates.{hours.State}"];
        report.Tones[StatusReportFields.HoursStateTag] = hours.State switch
        {
            ReportHoursState.Over => ReportTone.Danger,
            ReportHoursState.NearLimit => ReportTone.Warning,
            ReportHoursState.Within => ReportTone.Success,
            _ => ReportTone.Neutral,
        };
        var tasks = StatusReportList<ReportTaskHours>.Of(hours.Tasks, StatusReportLimits.TaskHours);
        report.Lists[StatusReportFields.TaskHours] = [.. tasks.Items.Select(task => Scope(
            ("horasTarefa.nome", Cut(task.Name, 90)),
            ("horasTarefa.previstas", task.Planned is { } planned ? HoursOf(planned) : Texts["values.none"]),
            ("horasTarefa.apontadas", HoursOf(task.Logged))))];
        fields["horasTarefas.vazio"] = tasks.Items.Count == 0 ? Texts["values.noTaskHours"] : string.Empty;
        var rest = hours.Tasks.Skip(StatusReportLimits.TaskHours).ToList();
        fields["horasTarefas.mais"] = rest.Count switch
        {
            0 => string.Empty,
            1 => Texts.Format(
                "values.moreTaskHoursOne", ReportTexts.Hours(rest.Sum(task => task.Planned ?? 0m)), ReportTexts.Hours(rest.Sum(task => task.Logged))),
            _ => Texts.Format(
                "values.moreTaskHoursMany",
                rest.Count,
                ReportTexts.Hours(rest.Sum(task => task.Planned ?? 0m)),
                ReportTexts.Hours(rest.Sum(task => task.Logged))),
        };
        fields["horas.fora"] = hours.Outside > 0 ? Texts.Format("values.outsideHours", ReportTexts.Hours(hours.Outside)) : string.Empty;
    }

    private static ReportScope RiskOf(StatusReportRiskItem risk)
    {
        var scope = new ReportScope();
        var fields = scope.Fields;
        fields["risco.numero"] = ReportTexts.Number(risk.Number);
        fields["risco.titulo"] = risk.Title;
        var status = Texts[$"ticketStatuses.{risk.Status}"];
        fields["risco.status"] = risk.Overdue ? Texts.Format("values.overdue", status) : status;
        fields["risco.prioridade"] = Texts[$"priorities.{risk.Priority}"];
        fields["risco.classificacao"] = Texts[$"riskLevels.{risk.Level}"];
        fields["risco.pontoDeAtencao"] = risk.Attention;
        fields["risco.fornecedor"] = risk.Vendor is { } vendor
            ? Texts.Format("values.vendor", vendor.Vendor, vendor.Code, Texts[$"vendorStatuses.{vendor.Status}"])
            : Texts["values.noVendor"];
        scope.Tones[StatusReportFields.RiskLevelTag] = risk.Level switch
        {
            RiskLevel.High => ReportTone.Danger,
            RiskLevel.Medium => ReportTone.Warning,
            _ => ReportTone.Neutral,
        };
        scope.Lists[StatusReportFields.RiskMilestone] = [.. risk.Milestones.Select(milestone => Scope(
            ("riscoMarco.nome", Cut(milestone.Name, 60)), ("riscoMarco.dia", ReportTexts.Day(milestone.Day)), ("riscoMarco.situacao", WhenOf(milestone))))];
        fields["riscoMarcos.vazio"] = risk.Milestones.Count == 0 ? Texts["values.noRiskMilestones"] : string.Empty;
        fields["riscoMarcos.mais"] = risk.MoreMilestones > 0 ? Texts.Count("values.moreRiskMilestones", risk.MoreMilestones) : string.Empty;
        return scope;
    }

    /// <summary>The end against the baseline (ADR-0021): after, before or on it, or that there is none.</summary>
    private static string DeviationOf(OverviewDeadline? deadline) => deadline switch
    {
        null => Texts["values.noSchedule"],
        { BaselineEnd: null } or { Deviation: null } => Texts["values.noBaseline"],
        { Deviation: > 0 } => Texts.Format("values.baselineLate", DaysOf(deadline.Deviation.Value), ReportTexts.Day(deadline.BaselineEnd.Value)),
        { Deviation: < 0 } => Texts.Format("values.baselineEarly", DaysOf(-deadline.Deviation.Value), ReportTexts.Day(deadline.BaselineEnd.Value)),
        _ => Texts.Format("values.baselineOnTime", ReportTexts.Day(deadline.BaselineEnd!.Value)),
    };

    /// <summary>When a milestone is: late, today or in some business days (ADR-0086).</summary>
    private static string WhenOf(StatusReportMilestone milestone) =>
        milestone.DaysLate is > 0
            ? Texts.Format("values.milestoneLate", DaysOf(milestone.DaysLate.Value))
            : milestone.BusinessDaysLeft == 0
                ? Texts["values.milestoneToday"]
                : Texts.Format("values.milestoneLeft", DaysOf(milestone.BusinessDaysLeft));

    /// <summary>What the projection of a late task pushes (ADR-0164): the nearest milestone and how many more, or the end of the project.</summary>
    private static string PushesOf(ScheduleProjection? projection)
    {
        if (projection is null)
        {
            return Texts["values.none"];
        }

        if (projection.Milestones.Count > 0)
        {
            var first = projection.Milestones[0];
            var text = Texts.Format("values.pushesMilestone", first.Name, DaysOf(first.Days));
            return projection.Milestones.Count switch
            {
                1 => text,
                2 => Texts.Format("values.pushesMoreOne", text),
                _ => Texts.Format("values.pushesMoreMany", text, projection.Milestones.Count - 1),
            };
        }

        return projection.ProjectEnd is { } end
            ? Texts.Format("values.pushesEnd", DaysOf(end.Days))
            : Texts["values.pushesNothing"];
    }

    private static string DaysOf(int days) => Texts.Count("values.businessDays", days);

    private static string HoursOf(decimal hours) => Texts.Format("values.hours", ReportTexts.Hours(hours));

    private static string Cut(string text, int length) => text.Length <= length ? text : text[..(length - 1)].TrimEnd() + "…";

    private static ReportPicture? PictureOf(StatusReportImage? image) =>
        image is null ? null : new ReportPicture(image.Content, image.ContentType);

    private static ReportScope Scope(params (string Name, string Value)[] fields)
    {
        var scope = new ReportScope();
        foreach (var (name, value) in fields)
        {
            scope.Fields[name] = value;
        }

        return scope;
    }
}
