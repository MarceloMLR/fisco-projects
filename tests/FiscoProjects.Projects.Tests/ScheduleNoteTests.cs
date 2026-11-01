using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirements "Notas da tarefa e do marco" and "Notas no desfazer e na auditoria" (ADR-0139).
public sealed class ScheduleNoteTests
{
    private const string Waiting = "Aguardando o cliente mandar os arquivos do plano de contas";
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItemPlan Plan(SchedulePhase phase, ScheduleItem item, string? note) =>
        new(phase.Id, item.Name, item.Start, item.Duration, null, null, [], Note: note);

    private static (Schedule Schedule, SchedulePhase Phase, ScheduleItem Task, ScheduleItem Milestone) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        var task = schedule.AddItem(
            ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, "Configurar plano de contas", D(10, 21), 4, null, 0, []),
            Days,
            Today);
        var milestone = schedule.AddItem(
            ScheduleItemKind.Milestone,
            new ScheduleItemPlan(phase.Id, "Plano aprovado", D(10, 28), 0, null, 0, [], Note: "  Depende da contabilidade do cliente\n  "),
            Days,
            Today);
        return (schedule, phase, task, milestone);
    }

    [Fact(DisplayName = "Escrever a nota")]
    public void TheNote_IsKeptTrimmed_WithItsLineBreaks()
    {
        var (schedule, phase, task, milestone) = NewSchedule();

        schedule.ChangeItem(task.Id, Plan(phase, task, $" {Waiting}\nLembrar na reunião de segunda.\t"), Days, Today);

        task.Note.Should().Be($"{Waiting}\nLembrar na reunião de segunda.");
        milestone.Note.Should().Be("Depende da contabilidade do cliente");
    }

    [Fact(DisplayName = "Nota mantida sem o campo")]
    public void APlanWithoutTheNote_AndTheTimeline_KeepIt()
    {
        var (schedule, phase, task, _) = NewSchedule();
        schedule.ChangeItem(task.Id, Plan(phase, task, Waiting), Days, Today);

        schedule.ChangeItem(task.Id, Plan(phase, task, null) with { Name = "Configurar o plano de contas" }, Days, Today);
        schedule.Reschedule(task.Id, D(10, 22), null, Days, Today);

        (task.Name, task.Start, task.Note).Should().Be(("Configurar o plano de contas", D(10, 22), Waiting));
    }

    [Fact(DisplayName = "Apagar a nota")]
    public void AnEmptyNote_TakesItOut()
    {
        var (schedule, phase, task, _) = NewSchedule();
        schedule.ChangeItem(task.Id, Plan(phase, task, Waiting), Days, Today);

        schedule.ChangeItem(task.Id, Plan(phase, task, "   "), Days, Today);

        task.Note.Should().BeNull();
    }

    [Fact(DisplayName = "Nota longa demais")]
    public void ANote_HasUpTo2000Characters_WithoutOtherControlCharacters()
    {
        var (schedule, phase, task, _) = NewSchedule();

        Schedule.NoteOf(new string('a', ScheduleItem.NoteMaxLength)).Should().HaveLength(ScheduleItem.NoteMaxLength);
        Schedule.NoteOf(new string('a', ScheduleItem.NoteMaxLength + 1)).Should().BeNull();
        Schedule.NoteOf("Arquivo\u0007recebido").Should().BeNull();
        Schedule.NoteOf("  ").Should().BeEmpty();
        var refused = () => schedule.ChangeItem(task.Id, Plan(phase, task, new string('a', 2_001)), Days, Today);
        refused.Should().Throw<ArgumentException>();
        task.Note.Should().BeNull();
    }

    [Fact(DisplayName = "Desfazer a nota")]
    public void TheNote_IsUndoneAndRedone()
    {
        var (schedule, phase, task, _) = NewSchedule();
        var before = schedule.Snapshot();
        schedule.ChangeItem(task.Id, Plan(phase, task, Waiting), Days, Today);
        var diff = ScheduleDiff.Between(before, schedule.Snapshot());

        schedule.ApplyChange(diff.Inverse()).Should().BeTrue();
        task.Note.Should().BeNull();

        schedule.ApplyChange(diff).Should().BeTrue();
        task.Note.Should().Be(Waiting);
    }

    [Fact(DisplayName = "Nota importada do MS Project")]
    public void AnOutline_BringsTheNotes()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var outline = new SchedulePlanOutline(
            [new OutlinePhase("Configuração")],
            [
                new OutlineItem(0, null, ScheduleItemKind.Task, "Configurar plano de contas", 0, 4, Note: Waiting),
                new OutlineItem(0, null, ScheduleItemKind.Milestone, "Plano aprovado", 5, 0),
            ],
            []);

        schedule.ApplyTemplate(outline, D(10, 21), Days, Today);

        schedule.Items.Select(item => (item.Name, item.Note)).Should().Equal(
            ("Configurar plano de contas", Waiting), ("Plano aprovado", (string?)null));
    }
}
