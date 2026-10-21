using FiscoProjects.MyWork.Contracts;
using FiscoProjects.MyWork.Domain;

namespace FiscoProjects.MyWork.Tests;

// Spec: meu-trabalho (the rule of the queue): "Grupos da fila", "Aguardando você", "Ação de cada linha" and "Fila pela
// API" (the order) (ADR-0070, ADR-0096).
public sealed class MyWorkQueueTests
{
    private static readonly Guid Erp = Guid.CreateVersion7();

    private static MyWorkWindow WindowOf(int year, int month, int day) =>
        MyWorkWindow.Of(new DateOnly(year, month, day), TestBusinessDays.Of2026);

    private static MyWorkItem TaskDue(DateOnly due, int progress = 50, string name = "Configurar plano de contas") =>
        new(MyWorkKind.Task, Guid.CreateVersion7(), Erp, "ERP-FIN", name, MyWorkViews.ForMe)
        {
            DueOn = due,
            Task = new MyWorkTask("Configuração", progress),
        };

    private static MyWorkItem TicketDue(
        DateOnly? due, Guid? assignee = null, bool canAssign = false, bool waits = false, DateTime? since = null) =>
        new(MyWorkKind.Ticket, Guid.CreateVersion7(), Erp, "ERP-FIN", "Rejeição 539", MyWorkViews.ForMe)
        {
            DueOn = due,
            WaitsForYou = waits,
            WaitingSince = since,
            Ticket = new MyWorkTicket(
                104,
                waits ? MyWorkTicketStatus.WaitingForRequester : MyWorkTicketStatus.InProgress,
                MyWorkTeam.Development,
                Guid.CreateVersion7(),
                assignee,
                canAssign,
                6,
                MyWorkDeadline.Resolution),
        };

    [Fact(DisplayName = "Prazo vencido no grupo Atrasado")]
    public void PastDeadline_IsLate()
    {
        var window = WindowOf(2026, 10, 19);

        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 16), progress: 80), window).Should().Be(MyWorkGroup.Late);
    }

    [Fact(DisplayName = "Sexta-feira com o próximo dia útil na segunda")]
    public void Friday_HasMondayAsTheNextBusinessDay()
    {
        var window = WindowOf(2026, 10, 16);

        window.NextBusinessDay.Should().Be(new DateOnly(2026, 10, 19));
        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 16)), window).Should().Be(MyWorkGroup.Soon);
        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 19)), window).Should().Be(MyWorkGroup.Soon);
        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 20)), window).Should().Be(MyWorkGroup.Week);
    }

    [Fact(DisplayName = "Véspera de feriado")]
    public void EveOfAHoliday_SkipsIt()
    {
        var window = WindowOf(2026, 10, 30);

        window.NextBusinessDay.Should().Be(new DateOnly(2026, 11, 3));
        MyWorkQueue.GroupOf(TicketDue(new DateOnly(2026, 11, 3)), window).Should().Be(MyWorkGroup.Soon);
        MyWorkQueue.GroupOf(TicketDue(new DateOnly(2026, 11, 4)), window).Should().Be(MyWorkGroup.Week);
    }

    [Fact(DisplayName = "Sete dias corridos")]
    public void Week_IsSevenCalendarDays()
    {
        var window = WindowOf(2026, 10, 19);

        window.Until.Should().Be(new DateOnly(2026, 10, 26));
        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 26)), window).Should().Be(MyWorkGroup.Week);
        MyWorkQueue.GroupOf(TaskDue(new DateOnly(2026, 10, 27)), window).Should().BeNull();
    }

    [Fact(DisplayName = "Aguardando você qualquer que seja o prazo")]
    public void WaitingForYou_IgnoresTheDeadline()
    {
        var window = WindowOf(2026, 10, 19);

        MyWorkQueue.GroupOf(TicketDue(null, waits: true), window).Should().Be(MyWorkGroup.Waiting);
        MyWorkQueue.GroupOf(TicketDue(new DateOnly(2026, 10, 1), waits: true), window).Should().Be(MyWorkGroup.Waiting);
        MyWorkQueue.GroupOf(TicketDue(null), window).Should().BeNull();
    }

    [Fact(DisplayName = "Ações das tarefas")]
    public void Tasks_StartCompleteOrUpdate()
    {
        var window = WindowOf(2026, 10, 19);
        var start = TaskDue(new DateOnly(2026, 10, 19), progress: 0);
        var complete = TaskDue(new DateOnly(2026, 10, 16), progress: 80);
        var soon = TaskDue(new DateOnly(2026, 10, 20), progress: 30);
        var update = TaskDue(new DateOnly(2026, 10, 23), progress: 50);

        MyWorkQueue.ActionOf(start, MyWorkQueue.GroupOf(start, window)!.Value).Should().Be(MyWorkAction.Start);
        MyWorkQueue.ActionOf(complete, MyWorkQueue.GroupOf(complete, window)!.Value).Should().Be(MyWorkAction.Complete);
        MyWorkQueue.ActionOf(soon, MyWorkQueue.GroupOf(soon, window)!.Value).Should().Be(MyWorkAction.Complete);
        MyWorkQueue.ActionOf(update, MyWorkQueue.GroupOf(update, window)!.Value).Should().Be(MyWorkAction.Update);
    }

    [Fact(DisplayName = "Ações dos chamados")]
    public void Tickets_ReplyAssignOrOpen()
    {
        var due = new DateOnly(2026, 10, 20);

        MyWorkQueue.ActionOf(TicketDue(null, waits: true), MyWorkGroup.Waiting).Should().Be(MyWorkAction.Reply);
        MyWorkQueue.ActionOf(TicketDue(due, canAssign: true), MyWorkGroup.Soon).Should().Be(MyWorkAction.Assign);
        MyWorkQueue.ActionOf(TicketDue(due), MyWorkGroup.Soon).Should().Be(MyWorkAction.Open);
        MyWorkQueue.ActionOf(TicketDue(due, assignee: Guid.CreateVersion7(), canAssign: true), MyWorkGroup.Soon)
            .Should().Be(MyWorkAction.Open);
    }

    [Fact(DisplayName = "Ações dos pedidos de aceite")]
    public void Acceptances_ReplyOrRemind()
    {
        var forYou = new MyWorkItem(MyWorkKind.Acceptance, Guid.CreateVersion7(), Erp, "ERP-FIN", "Aceite", MyWorkViews.ForMe)
        {
            WaitsForYou = true,
        };
        var asked = forYou with { WaitsForYou = false, DueOn = new DateOnly(2026, 10, 20) };

        MyWorkQueue.ActionOf(forYou, MyWorkGroup.Waiting).Should().Be(MyWorkAction.Reply);
        MyWorkQueue.ActionOf(asked, MyWorkGroup.Soon).Should().Be(MyWorkAction.Remind);
    }

    [Fact(DisplayName = "Fila pela API")]
    public void Views_HaveTheirGroupsInOrder_AndTheItemsByDeadline()
    {
        var window = WindowOf(2026, 10, 19);
        var later = TaskDue(new DateOnly(2026, 10, 23), name: "Parametrizar regras de ICMS");
        var sooner = TaskDue(new DateOnly(2026, 10, 21), name: "Configurar plano de contas");
        var late = TaskDue(new DateOnly(2026, 10, 15), name: "Validar cadastro de produtos");
        var newer = TicketDue(null, waits: true, since: new DateTime(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc)) with
        {
            Views = MyWorkViews.ForMe | MyWorkViews.OpenedByMe,
        };
        var older = TicketDue(null, waits: true, since: new DateTime(2026, 10, 16, 8, 0, 0, DateTimeKind.Utc)) with
        {
            Views = MyWorkViews.ForMe | MyWorkViews.OpenedByMe,
        };
        var outside = TaskDue(new DateOnly(2026, 11, 3));
        var followed = TicketDue(new DateOnly(2026, 10, 20)) with { Views = MyWorkViews.Following };

        var views = MyWorkQueue.ViewsOf([later, outside, newer, sooner, followed, late, older], window);

        views.Select(view => (view.View, view.Count)).Should().Equal(
            (MyWorkView.ForMe, 5), (MyWorkView.OpenedByMe, 2), (MyWorkView.Following, 1));
        var forMe = views[0].Groups;
        forMe.Select(group => group.Group).Should().Equal(MyWorkGroup.Late, MyWorkGroup.Waiting, MyWorkGroup.Week);
        forMe[1].Entries.Select(entry => entry.Item).Should().Equal(older, newer);
        forMe[2].Entries.Select(entry => entry.Item).Should().Equal(sooner, later);
        views[2].Groups.Single().Group.Should().Be(MyWorkGroup.Soon);
    }
}
