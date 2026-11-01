using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: membros-do-projeto, requirement "Ausências das pessoas", and dados-pessoais, "Retenção das ausências" (ADR-0141).
public sealed class PersonAbsenceTests
{
    private static DateOnly D(int month, int day, int year = 2026) => new(year, month, day);

    [Fact(DisplayName = "Incluir ausência")]
    public void AnAbsence_GoesFromOneDay_To366Days()
    {
        PersonAbsence.IsValid(D(11, 3), D(11, 3)).Should().BeTrue();
        PersonAbsence.IsValid(D(11, 3), D(11, 14)).Should().BeTrue();
        PersonAbsence.IsValid(D(1, 1), D(1, 1, 2027)).Should().BeTrue("2026-01-01 to 2027-01-01 is 366 days");
    }

    [Fact(DisplayName = "Ausência inválida")]
    public void TheEndBeforeTheStart_OrMoreThan366Days_IsNotAnAbsence()
    {
        PersonAbsence.IsValid(D(11, 14), D(11, 3)).Should().BeFalse();
        PersonAbsence.IsValid(D(1, 1), D(1, 2, 2027)).Should().BeFalse();
        PersonAbsence.IsValid(new DateOnly(1999, 12, 31), D(1, 2)).Should().BeFalse();
        var refused = () => new PersonAbsence(Guid.CreateVersion7(), D(11, 14), D(11, 3), Guid.CreateVersion7(), DateTime.UtcNow);
        refused.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Ausência sobreposta")]
    public void Overlaps_CountsTheEnds()
    {
        var absence = new PersonAbsence(Guid.CreateVersion7(), D(11, 3), D(11, 14), Guid.CreateVersion7(), DateTime.UtcNow);

        absence.Overlaps(D(11, 14), D(11, 20)).Should().BeTrue();
        absence.Overlaps(D(10, 20), D(11, 3)).Should().BeTrue();
        absence.Overlaps(D(11, 5), D(11, 6)).Should().BeTrue();
        absence.Overlaps(D(11, 15), D(11, 20)).Should().BeFalse();
        absence.Overlaps(D(10, 20), D(11, 2)).Should().BeFalse();
    }

    [Fact(DisplayName = "Ausência apagada depois de um ano")]
    public void AnAbsence_IsKeptUntilAYearAfterItsEnd() =>
        PersonAbsence.KeptUntil(D(11, 14)).Should().Be(D(11, 14, 2027));
}
