using FiscoProjects.Calendar.Domain;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Calendar.Infrastructure.Persistence;

internal sealed class WorkCalendarConfiguration : IEntityTypeConfiguration<WorkCalendar>
{
    public void Configure(EntityTypeBuilder<WorkCalendar> calendar)
    {
        calendar.ToTable("work_calendar");
        calendar.Property(c => c.WorkingDays).HasConversion<int>();

        // The one row, created with the database, Monday to Friday (ADR-0018). The audit names the days; the item is
        // always the same, so it has no label.
        calendar.HasData(new { Id = WorkCalendar.SingleId, WorkingDays = WorkCalendar.DefaultWorkingDays });
    }
}

internal sealed class CalculatedHolidayConfiguration : IEntityTypeConfiguration<CalculatedHoliday>
{
    /// <summary>
    /// Fixed ids of the rows created with the database, one per holiday the national calendar calculates. A holiday
    /// created by a new law comes in a new version, with its own row in a new migration.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, Guid> Ids = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        [BrazilianNationalCalendar.NewYearsDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b11"),
        [BrazilianNationalCalendar.CarnivalMonday] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b12"),
        [BrazilianNationalCalendar.CarnivalTuesday] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b13"),
        [BrazilianNationalCalendar.AshWednesday] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b14"),
        [BrazilianNationalCalendar.GoodFriday] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b15"),
        [BrazilianNationalCalendar.Tiradentes] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b16"),
        [BrazilianNationalCalendar.LabourDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b17"),
        [BrazilianNationalCalendar.CorpusChristi] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b18"),
        [BrazilianNationalCalendar.IndependenceDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b19"),
        [BrazilianNationalCalendar.OurLadyOfAparecida] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1a"),
        [BrazilianNationalCalendar.PublicServantsDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1b"),
        [BrazilianNationalCalendar.AllSoulsDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1c"),
        [BrazilianNationalCalendar.RepublicDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1d"),
        [BrazilianNationalCalendar.BlackConsciousnessDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1e"),
        [BrazilianNationalCalendar.ChristmasEve] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1f"),
        [BrazilianNationalCalendar.ChristmasDay] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b20"),
        [BrazilianNationalCalendar.NewYearsEve] = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b21"),
    };

    public void Configure(EntityTypeBuilder<CalculatedHoliday> holiday)
    {
        holiday.ToTable("calculated_holidays");
        holiday.Property(h => h.Key).HasMaxLength(CalculatedHoliday.KeyMaxLength);
        holiday.HasIndex(h => h.Key).IsUnique();

        // One row per calculated holiday, with the default of ADR-0083, so a choice is always a change.
        holiday.HasData(new BrazilianNationalCalendar().Holidays.Select(definition => new
        {
            Id = Ids[definition.Key],
            definition.Key,
            IsConsidered = definition.ConsideredByDefault,
        }));

        // Audit (ADR-0039): the key names the holiday, and the screen translates it; it never changes.
        holiday.HasAuditLabel(h => h.Key);
        holiday.Property(h => h.Key).ExcludeFromAudit();
    }
}

internal sealed class LocalHolidayConfiguration : IEntityTypeConfiguration<LocalHoliday>
{
    public void Configure(EntityTypeBuilder<LocalHoliday> holiday)
    {
        holiday.ToTable("local_holidays");
        holiday.Property(h => h.Name).HasMaxLength(LocalHoliday.NameMaxLength);
        holiday.HasIndex(h => h.Date);

        // Audit (ADR-0039): the holiday is named by its name.
        holiday.HasAuditLabel(h => h.Name);
    }
}
