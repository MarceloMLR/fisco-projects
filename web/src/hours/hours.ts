import type { TimeEntry } from '../api/client';
import type { DayValue } from '../format/dates';

// The hours of a person (ADR-0128): in quarters of an hour, from 0,25 to 24, typed as "1,5", "1.5" or "1:30", and
// shown as "1,5 h"; the week from Monday to Sunday, as the calendar of Brazil starts it.

const dayLength = 86_400_000;

/** The least hours of an entry, a quarter of an hour, which is also the step of the hours. */
export const quarter = 0.25;

/** The most hours of an entry and of a person in a day. */
export const maxHours = 24;

const decimalHours = /^(\d{1,2})(?:[.,](\d{1,2}))?$/;
const clockHours = /^(\d{1,2}):([0-5]\d)$/;

/**
 * The hours typed as "1,5", "1.5" or "1:30", or undefined when they are not hours of an entry: from 0,25 to 24, in
 * quarters of an hour.
 */
export function parseHours(text: string): number | undefined {
  const typed = text.trim();
  const clock = clockHours.exec(typed);
  const decimal = clock === null ? decimalHours.exec(typed) : null;
  let hours: number | undefined;
  if (clock !== null) {
    hours = Number(clock[1]) + Number(clock[2]) / 60;
  } else if (decimal !== null) {
    hours = Number(`${decimal[1] ?? '0'}.${decimal[2] ?? '0'}`);
  }
  return hours !== undefined && isValidHours(hours) ? hours : undefined;
}

/** From 0,25 to 24 hours, in quarters of an hour. */
export function isValidHours(hours: number): boolean {
  return hours >= quarter && hours <= maxHours && Number.isInteger(hours / quarter);
}

const number = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 });

/** The hours as the screens show them: "1,5 h", "8 h", "27,25 h". */
export function formatHours(hours: number): string {
  return `${number.format(hours)} h`;
}

/** The hours as the field of an entry starts with them: "1,5", "8". */
export function hoursFieldText(hours: number): string {
  return number.format(hours);
}

function dayNumber(day: DayValue): number {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return Date.UTC(year, month - 1, dayOfMonth) / dayLength;
}

function dayOfNumber(value: number): DayValue {
  return new Date(value * dayLength).toISOString().slice(0, 10);
}

/** The day some days after (or before) another. */
export function addDays(day: DayValue, days: number): DayValue {
  return dayOfNumber(dayNumber(day) + days);
}

/** The Monday of the week of the day, which starts the week of Minhas horas. */
export function weekStart(day: DayValue): DayValue {
  // The 1st of January of 1970 was a Thursday: day 0 is 3 days after a Monday.
  const fromMonday = (((dayNumber(day) + 3) % 7) + 7) % 7;
  return addDays(day, -fromMonday);
}

/** The Sunday that ends the week that starts on the Monday. */
export function weekEnd(start: DayValue): DayValue {
  return addDays(start, 6);
}

/** A line of the week: a day with its total, or an entry of the day. */
export type HoursRow =
  | { kind: 'day'; id: string; day: DayValue; total: number }
  | { kind: 'entry'; id: string; entry: TimeEntry };

export const isDayRow = (row: HoursRow) => row.kind === 'day';

/**
 * The lines of the week: each day that has entries, the newest first, with its total, and its entries in the order
 * the API sends them (the newest day first, then in the order they were made).
 */
export function hoursRows(entries: readonly TimeEntry[]): HoursRow[] {
  const rows: HoursRow[] = [];
  for (const entry of entries) {
    const id = `day:${entry.day}`;
    if (!rows.some((row) => row.id === id)) {
      rows.push({
        kind: 'day',
        id,
        day: entry.day,
        total: entries
          .filter((item) => item.day === entry.day)
          .reduce((sum, item) => sum + item.hours, 0),
      });
    }
    rows.push({ kind: 'entry', id: entry.id, entry });
  }
  return rows;
}

/** What an entry was worked on, as the list says it: the task, the number and the title of the ticket, or none. */
export type EntryWork =
  { kind: 'task'; name: string | null } | { kind: 'ticket'; text: string } | { kind: 'project' };

export function workOf(entry: TimeEntry): EntryWork {
  if (entry.task !== null) {
    return { kind: 'task', name: entry.task.name };
  }
  if (entry.ticket !== null) {
    return { kind: 'ticket', text: `#${String(entry.ticket.number)} ${entry.ticket.title}` };
  }
  return { kind: 'project' };
}
