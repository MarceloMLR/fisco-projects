using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Tests;

// Spec: horas-do-projeto (domain side): "Horas contratadas", "Horas adicionais" and "Saldo do projeto" (ADR-0128).
public sealed class ProjectHoursRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 29);

    [Theory(DisplayName = "Horas contratadas inválidas")]
    [InlineData(0)]
    [InlineData(-8)]
    [InlineData(10.1)]
    [InlineData(100000)]
    public void ContractedHours_AreMoreThanZero_UpTo99999_InQuarters(double hours) =>
        ContractedHours.IsValid((decimal)hours).Should().BeFalse();

    [Theory(DisplayName = "Cadastrar as horas contratadas")]
    [InlineData(0.25)]
    [InlineData(400)]
    [InlineData(99999)]
    public void ContractedHours_InQuarters_AreValid(double hours) =>
        ContractedHours.IsValid((decimal)hours).Should().BeTrue();

    [Fact(DisplayName = "Incluir horas adicionais")]
    public void AdditionalHours_TrimTheReason()
    {
        var (form, invalid) = AdditionalHoursForm.Of(Today, 16m, "  Integração com um segundo banco  ", Today);

        invalid.Should().BeEmpty();
        form.Should().Be(new AdditionalHoursForm(Today, 16m, "Integração com um segundo banco"));
    }

    [Fact(DisplayName = "Horas adicionais sem motivo")]
    public void AdditionalHours_WithoutReason_NoHours_OrTomorrow_AreRefused()
    {
        AdditionalHoursForm.Of(Today.AddDays(1), 0m, "   ", Today).Invalid
            .Should().Equal(AdditionalHoursField.Day, AdditionalHoursField.Hours, AdditionalHoursField.Reason);
        AdditionalHoursForm.Of(null, 10_000m, new string('a', AdditionalHours.ReasonMaxLength + 1), Today).Invalid
            .Should().Equal(AdditionalHoursField.Day, AdditionalHoursField.Hours, AdditionalHoursField.Reason);
        AdditionalHoursForm.Of(Today, 2.3m, "Relatório", Today).Invalid.Should().Equal(AdditionalHoursField.Hours);
    }

    [Fact(DisplayName = "Projeto acima das horas contratadas")]
    public void Balance_Over_WhenTheLoggedPassTheTotal()
    {
        var balance = HoursBalance.Of(400m, 40m, 468m, isClosed: false);

        balance.Should().Be(new HoursBalance(400m, 40m, 468m, 440m, -28m, 106, HoursBalanceState.Over));
    }

    [Fact(DisplayName = "Projeto perto do limite")]
    public void Balance_NearLimit_From90Percent_InAnOpenProject()
    {
        HoursBalance.Of(160m, 0m, 146m, isClosed: false)
            .Should().Be(new HoursBalance(160m, 0m, 146m, 160m, 14m, 91, HoursBalanceState.NearLimit));
        HoursBalance.Of(100m, 0m, 90m, isClosed: false).State.Should().Be(HoursBalanceState.NearLimit);
        HoursBalance.Of(100m, 0m, 100m, isClosed: false).State.Should().Be(HoursBalanceState.NearLimit);
        HoursBalance.Of(100m, 0m, 89.75m, isClosed: false).State.Should().Be(HoursBalanceState.Within);
    }

    [Fact(DisplayName = "Projeto encerrado perto do limite")]
    public void Balance_Within_InAClosedProject_NearTheLimit()
    {
        HoursBalance.Of(160m, 0m, 150m, isClosed: true).State.Should().Be(HoursBalanceState.Within);
        HoursBalance.Of(160m, 0m, 170m, isClosed: true).State.Should().Be(HoursBalanceState.Over);
    }

    [Fact(DisplayName = "Projeto sem horas contratadas")]
    public void Balance_NoContract_WithoutContractedHours_EvenWithAdditional()
    {
        var balance = HoursBalance.Of(null, 24m, 36m, isClosed: false);

        balance.Should().Be(new HoursBalance(null, 24m, 36m, null, null, null, HoursBalanceState.NoContract));
    }

    [Fact(DisplayName = "Lista das horas dos projetos")]
    public void Order_MostUsedFirst_AndWithoutContractLast_ByCode()
    {
        var rows = new (string Code, HoursBalance Balance)[]
        {
            ("WMS-ALFA", HoursBalance.Of(240m, 0m, 12m, false)),
            ("PORTAL-NF", HoursBalance.Of(null, 0m, 36m, false)),
            ("ERP-FIN", HoursBalance.Of(400m, 40m, 468m, false)),
            ("AGRO-NFE", HoursBalance.Of(null, 0m, 0m, false)),
            ("SPED-27", HoursBalance.Of(160m, 0m, 146m, false)),
            ("TMS-DELTA", HoursBalance.Of(320m, 24m, 158m, false)),
        };

        HoursBalance.Order(rows, row => row.Balance, row => row.Code).Select(row => row.Code)
            .Should().Equal("ERP-FIN", "SPED-27", "TMS-DELTA", "WMS-ALFA", "AGRO-NFE", "PORTAL-NF");
    }
}
