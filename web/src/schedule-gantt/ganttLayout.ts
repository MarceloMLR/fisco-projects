import { daysBetween, type DayValue } from '../format/dates';

/** The scales of the timeline (ADR-0071). */
export type GanttScale = 'days' | 'weeks' | 'months';

export const ganttScales: readonly GanttScale[] = ['days', 'weeks', 'months'];

/** The narrowest a day, a week or a month may be; narrower than that, the timeline scrolls sideways (ADR-0071). */
export const minUnitWidth: Record<GanttScale, number> = { days: 20, weeks: 44, months: 56 };

/** The days the timeline shows, whole units of its scale, and how many units they are. */
export interface GanttPeriod {
  start: DayValue;
  end: DayValue;
  units: number;
}

function partsOf(day: DayValue): [number, number, number] {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return [year, month, dayOfMonth];
}

function dayOf(moment: Date): DayValue {
  return moment.toISOString().slice(0, 10);
}

function utc(day: DayValue): Date {
  const [year, month, dayOfMonth] = partsOf(day);
  return new Date(Date.UTC(year, month - 1, dayOfMonth));
}

/** The day so many days after another (before, with a negative number). */
export function plusDays(day: DayValue, days: number): DayValue {
  const moment = utc(day);
  moment.setUTCDate(moment.getUTCDate() + days);
  return dayOf(moment);
}

/**
 * The period the timeline shows for a schedule from `start` to `end`: the days themselves; for weeks, from the Monday
 * before to the Sunday after; for months, from the first to the last day of the months.
 */
export function ganttPeriod(start: DayValue, end: DayValue, scale: GanttScale): GanttPeriod {
  if (scale === 'days') {
    return { start, end, units: daysBetween(start, end) + 1 };
  }
  if (scale === 'weeks') {
    // getUTCDay: Sunday is 0; Monday starts the week, as in the Brazilian calendar of the screens.
    const first = plusDays(start, -((utc(start).getUTCDay() + 6) % 7));
    const last = plusDays(end, (7 - utc(end).getUTCDay()) % 7);
    return { start: first, end: last, units: (daysBetween(first, last) + 1) / 7 };
  }
  const [startYear, startMonth] = partsOf(start);
  const [endYear, endMonth] = partsOf(end);
  const first = dayOf(new Date(Date.UTC(startYear, startMonth - 1, 1)));
  const last = dayOf(new Date(Date.UTC(endYear, endMonth, 0)));
  return { start: first, end: last, units: (endYear - startYear) * 12 + endMonth - startMonth + 1 };
}

/**
 * The width of each unit, so the period fills the width there is; never below the narrowest of the scale, when the
 * timeline scrolls sideways instead (ADR-0071).
 */
export function ganttUnitWidth(scale: GanttScale, units: number, width: number): number {
  const fit = units > 0 && width > 0 ? Math.floor((width / units) * 100) / 100 : 0;
  return Math.max(minUnitWidth[scale], fit);
}
