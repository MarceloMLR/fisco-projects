using System.Xml.Linq;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirement "Exportar para o MS Project" (ADR-0021, ADR-0051). The calendar of 2026 has 12/10 as a
// holiday, on a Monday.
public sealed class MsProjectExportTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);
    private static readonly XNamespace Ns = MsProjectExport.Ns;
    private static readonly Guid Rafael = Guid.CreateVersion7();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, ScheduleItem Task, ScheduleItem Milestone) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        schedule.AddPhase("Homologação");
        var task = schedule.AddItem(
            ScheduleItemKind.Task, new ScheduleItemPlan(phase.Id, "Parametrizar regras de ICMS", D(10, 13), 3, Rafael, 40, []), Days, Today);
        var milestone = schedule.AddItem(
            ScheduleItemKind.Milestone, new ScheduleItemPlan(phase.Id, "Aceite da configuração", D(10, 15), 0, null, 0, [task.Id]), Days, Today);
        return (schedule, task, milestone);
    }

    private static XDocument Export(Schedule schedule) =>
        MsProjectExport.Of(
            "ERP-FIN",
            "Implantação do ERP financeiro",
            schedule,
            schedule.Figures(Today),
            Days,
            new Dictionary<Guid, string> { [Rafael] = "Rafael Souza" },
            new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));

    private static XElement TaskNamed(XDocument document, string name) =>
        document.Descendants(Ns + "Task").Single(task => (string?)task.Element(Ns + "Name") == name);

    private static string? Value(XElement element, string name) => (string?)element.Element(Ns + name);

    [Fact(DisplayName = "XML do cronograma")]
    public void Export_HasThePhaseTheTaskAndTheMilestone_InLevels_WithDatesAndTheDependency()
    {
        var (schedule, _, _) = NewSchedule();

        var document = Export(schedule);

        document.Root!.Name.Should().Be(Ns + "Project");
        var phase = TaskNamed(document, "Configuração");
        var task = TaskNamed(document, "Parametrizar regras de ICMS");
        var milestone = TaskNamed(document, "Aceite da configuração");
        (Value(phase, "OutlineLevel"), Value(phase, "Summary")).Should().Be(("1", "1"));
        (Value(task, "OutlineLevel"), Value(task, "OutlineNumber"), Value(task, "Start"), Value(task, "Finish"), Value(task, "Duration"))
            .Should().Be(("2", "1.1", "2026-10-13T08:00:00", "2026-10-15T17:00:00", "PT24H0M0S"));
        (Value(task, "PercentComplete"), Value(task, "ConstraintType"), Value(task, "ConstraintDate"))
            .Should().Be(("40", "4", "2026-10-13T08:00:00"));
        (Value(milestone, "Milestone"), Value(milestone, "Duration"), Value(milestone, "Start"))
            .Should().Be(("1", "PT0H0M0S", "2026-10-15T17:00:00"));
        var link = milestone.Element(Ns + "PredecessorLink")!;
        (Value(link, "PredecessorUID"), Value(link, "Type")).Should().Be((Value(task, "UID"), "1"));
        // The empty phase has nothing to carry; the owner goes as a resource assigned to the task.
        document.Descendants(Ns + "Task").Select(one => Value(one, "Name")).Should().NotContain("Homologação");
        Value(document.Descendants(Ns + "Resource").Single(), "Name").Should().Be("Rafael Souza");
        Value(document.Descendants(Ns + "Assignment").Single(), "TaskUID").Should().Be(Value(task, "UID"));
    }

    [Fact(DisplayName = "Calendário no XML")]
    public void Export_HasTheWorkingDaysOfTheWeek_AndTheHolidayAsAnException()
    {
        var (schedule, _, _) = NewSchedule();

        var weekDays = Export(schedule).Descendants(Ns + "WeekDay").ToList();

        // Sunday is 1 and Saturday 7.
        weekDays.Where(day => Value(day, "DayType") != "0")
            .Select(day => (Value(day, "DayType"), Value(day, "DayWorking")))
            .Should().Equal(("1", "0"), ("2", "1"), ("3", "1"), ("4", "1"), ("5", "1"), ("6", "1"), ("7", "0"));
        weekDays.Where(day => Value(day, "DayType") == "0")
            .Select(day => Value(day.Element(Ns + "TimePeriod")!, "FromDate"))
            .Should().Contain("2026-10-12T00:00:00");
    }

    [Fact(DisplayName = "Linha de base no XML")]
    public void Export_HasTheBaselineOfEachItem()
    {
        var (schedule, task, _) = NewSchedule();
        schedule.SaveBaseline(Today);
        schedule.Reschedule(task.Id, D(10, 14), null, Days, Today);

        var exported = TaskNamed(Export(schedule), "Parametrizar regras de ICMS");

        var baseline = exported.Element(Ns + "Baseline")!;
        (Value(baseline, "Number"), Value(baseline, "Start"), Value(baseline, "Finish"))
            .Should().Be(("0", "2026-10-13T08:00:00", "2026-10-15T17:00:00"));
        Value(exported, "Start").Should().Be("2026-10-14T08:00:00");
    }
}
