using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Tests;

// Spec: horas-do-projeto (domain side): "Horas previstas da tarefa" and "Estado das horas da tarefa" (ADR-0138).
public sealed class TaskHoursRulesTests
{
    private static readonly Guid Task = Guid.CreateVersion7();

    [Theory(DisplayName = "Horas previstas inválidas")]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(10.1)]
    [InlineData(10000)]
    public void PlannedHours_AreMoreThanZero_UpTo9999_InQuarters(double hours) =>
        PlannedHours.IsValid((decimal)hours).Should().BeFalse();

    [Theory(DisplayName = "Pôr as horas previstas")]
    [InlineData(0.25)]
    [InlineData(40)]
    [InlineData(9999)]
    public void PlannedHours_InQuarters_AreValid(double hours) =>
        PlannedHours.IsValid((decimal)hours).Should().BeTrue();

    [Fact(DisplayName = "Tarefa acima das previstas")]
    public void Task_Over_WhenTheLoggedPassThePlanned() =>
        TaskHours.Of(Task, "Parametrizar regras de ICMS", 40m, 52m, isDone: false).Balance
            .Should().Be(new HoursBalance(40m, 0m, 52m, 40m, -12m, 130, HoursBalanceState.Over));

    [Fact(DisplayName = "Tarefa perto das previstas")]
    public void Task_NearThePlanned_From90Percent_WhileOpen() =>
        TaskHours.Of(Task, "Integrar emissão de NF-e", 20m, 18m, isDone: false).Balance
            .Should().Be(new HoursBalance(20m, 0m, 18m, 20m, 2m, 90, HoursBalanceState.NearLimit));

    [Fact(DisplayName = "Tarefa concluída perto das previstas")]
    public void ADoneTask_IsWithin_NearThePlanned() =>
        TaskHours.Of(Task, "Mapear processos fiscais", 20m, 19m, isDone: true).Balance.State
            .Should().Be(HoursBalanceState.Within);

    [Fact(DisplayName = "Tarefa sem horas previstas")]
    public void ATaskWithoutPlannedHours_HasNoBalance() =>
        TaskHours.Of(Task, "Testes integrados", null, 6m, isDone: false)
            .Should().Be(new TaskHours(
                Task, "Testes integrados", null, 6m, new HoursBalance(null, 0m, 6m, null, null, null, HoursBalanceState.NoContract)));
}
