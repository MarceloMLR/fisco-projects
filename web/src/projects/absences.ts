import type { TFunction } from 'i18next';
import type { AbsencePeriod } from '../api/client';
import { type DayValue, formatDayMonthText, formatDayText } from '../format/dates';

/** How many days ahead the list of Members tells the next absence of a person (ADR-0141). */
export const awayAheadDays = 30;

/** The most calendar days of one absence, as the API takes them. */
export const maxAbsenceDays = 366;

/** The day so many calendar days after another, as a day value. */
function daysAfter(day: DayValue, days: number): DayValue {
  const date = new Date(`${day}T12:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

/** The calendar days of a period, counting both ends. */
export function absenceDays(start: DayValue, end: DayValue): number {
  return (
    Math.round((Date.parse(`${end}T12:00:00Z`) - Date.parse(`${start}T12:00:00Z`)) / 86_400_000) + 1
  );
}

/** A period of an absence in full: "03/11/2026 a 14/11/2026", or the day alone when it is one. */
export function absenceText(period: AbsencePeriod, t: TFunction): string {
  return period.start === period.end
    ? formatDayText(period.start)
    : t('projects.absences.period', {
        start: formatDayText(period.start),
        end: formatDayText(period.end),
      });
}

/**
 * The absence of a person as the list of Members tells it (ADR-0141): "Ausente até 14/11" while it lasts ("Ausente hoje" on its last day), "Ausente de
 * 03/11 a 14/11" (or "Ausente em 06/11", of one day) when it starts in the next 30 days, nothing otherwise.
 */
export function awayText(
  period: AbsencePeriod | null,
  today: DayValue,
  t: TFunction,
): string | undefined {
  if (period === null || period.end < today) {
    return undefined;
  }
  if (period.start <= today) {
    return period.end === today
      ? t('projects.absences.awayToday')
      : t('projects.absences.awayUntil', { end: formatDayMonthText(period.end) });
  }
  if (period.start > daysAfter(today, awayAheadDays)) {
    return undefined;
  }
  return period.start === period.end
    ? t('projects.absences.awayOn', { day: formatDayMonthText(period.start) })
    : t('projects.absences.awayFrom', {
        start: formatDayMonthText(period.start),
        end: formatDayMonthText(period.end),
      });
}

/** The absences of the person in charge on the days of a task (ADR-0141): "Ausente de 05/11 a 14/11, durante a tarefa". */
export function ownerAwayText(periods: readonly AbsencePeriod[], t: TFunction): string | undefined {
  if (periods.length === 0) {
    return undefined;
  }
  const texts = periods.map((period) =>
    period.start === period.end
      ? formatDayMonthText(period.start)
      : t('projects.absences.shortPeriod', {
          start: formatDayMonthText(period.start),
          end: formatDayMonthText(period.end),
        }),
  );
  return t('projects.absences.duringTask', { periods: texts.join(', ') });
}
