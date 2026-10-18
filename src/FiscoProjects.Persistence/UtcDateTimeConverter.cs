using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FiscoProjects.Persistence;

/// <summary>
/// Stores date and time in UTC (ADR-0013). Local values are converted, unspecified values are taken as UTC,
/// and values read from the database are marked as UTC.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
