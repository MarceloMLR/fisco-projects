import type { TFunction } from 'i18next';

// Dates on screen (design system: 14/10/2026 and 09:42). Until the installation has its own time zone setting,
// they use the browser's.

const time = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' });
const date = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});

function startOfDay(moment: Date): number {
  return new Date(moment.getFullYear(), moment.getMonth(), moment.getDate()).getTime();
}

/** Last access of a person: now, today with the time, yesterday with the time, or the date (14/10/2026). */
export function formatLastAccess(isoUtc: string, t: TFunction, now: Date = new Date()): string {
  const moment = new Date(isoUtc);
  if (now.getTime() - moment.getTime() < 60_000) {
    return t('dates.now');
  }
  const days = Math.round((startOfDay(now) - startOfDay(moment)) / 86_400_000);
  if (days === 0) {
    return t('dates.today', { time: time.format(moment) });
  }
  if (days === 1) {
    return t('dates.yesterday', { time: time.format(moment) });
  }
  return date.format(moment);
}

const dayMonth = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' });

/** Day and month (16/10), as in the Audit list. */
export function formatDayMonth(isoUtc: string): string {
  return dayMonth.format(new Date(isoUtc));
}

/** Time of day (09:58). */
export function formatTime(isoUtc: string): string {
  return time.format(new Date(isoUtc));
}

/** Date and time (16/10/2026 09:58), as in the details of an audit record. */
export function formatDateTime(isoUtc: string): string {
  const moment = new Date(isoUtc);
  return `${date.format(moment)} ${time.format(moment)}`;
}

/** A day as the date field of the browser keeps it (2026-10-16), in the browser's time zone. */
export type DayValue = string;

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function toDayValue(moment: Date): DayValue {
  return `${String(moment.getFullYear())}-${pad(moment.getMonth() + 1)}-${pad(moment.getDate())}`;
}

/** Midnight at the start of the day, in the browser's time zone. */
export function startOfDayValue(day: DayValue): Date {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return new Date(year, month - 1, dayOfMonth);
}

/** The day after, as the end of a period that includes its last day. */
export function nextDayValue(day: DayValue): DayValue {
  const start = startOfDayValue(day);
  return toDayValue(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 1));
}

/** The last days up to today, today included: 30 days by default. */
export function lastDays(
  count: number,
  today: Date = new Date(),
): { from: DayValue; to: DayValue } {
  const from = new Date(today.getFullYear(), today.getMonth(), today.getDate() - (count - 1));
  return { from: toDayValue(from), to: toDayValue(today) };
}

/** A period as the Audit filter shows it (01/10 – 16/10). */
export function formatPeriod(from: DayValue, to: DayValue): string {
  return `${dayMonth.format(startOfDayValue(from))} – ${dayMonth.format(startOfDayValue(to))}`;
}

/** A day in full (01/10/2026), as the accessible name of the period says it. */
export function formatDayValue(day: DayValue): string {
  return date.format(startOfDayValue(day));
}
