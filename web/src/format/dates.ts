import type { TFunction } from 'i18next';

// Dates on screen (design system: 14/10/2026 and 09:42), in the time zone of the installation (ADR-0013): the API
// keeps them in UTC, and the screens show them as the company sees them, whatever the time zone of the browser.

/** The time zone of a new installation, and of the screens if its rules cannot be read (ADR-0013). */
export const defaultTimeZone = 'America/Sao_Paulo';

/** A day as the date field of the browser keeps it (2026-10-16), a day of the time zone of the installation. */
export type DayValue = string;

const dayLength = 86_400_000;

// The day 0 of the dates of a spreadsheet (Excel and the others), with the leap day of 1900 they count.
const spreadsheetEpoch = Date.UTC(1899, 11, 30);

// A day of the calendar has no time zone: its day of the week is the one of its midnight in UTC.
const weekday = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', timeZone: 'UTC' });
const longDay = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
});
const shortMonth = new Intl.DateTimeFormat('pt-BR', { month: 'short', timeZone: 'UTC' });
const longMonth = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric', timeZone: 'UTC' });

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

function dayValueOf(year: number, month: number, day: number): DayValue {
  return `${String(year)}-${pad(month)}-${pad(day)}`;
}

function partsOf(day: DayValue): [number, number, number] {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return [year, month, dayOfMonth];
}

/** The number of the day since 1970, to count days between two dates of the calendar. */
function dayNumber(day: DayValue): number {
  const [year, month, dayOfMonth] = partsOf(day);
  return Date.UTC(year, month - 1, dayOfMonth) / dayLength;
}

/** The calendar day a number of days after (or before) another. */
function addDays(day: DayValue, days: number): DayValue {
  const moment = new Date((dayNumber(day) + days) * dayLength);
  return dayValueOf(moment.getUTCFullYear(), moment.getUTCMonth() + 1, moment.getUTCDate());
}

/** The dates of the screens in one time zone. Each function keeps its identity, so tables do not draw again. */
export interface Dates {
  timeZone: string;
  /**
   * Last access of a person: now, today with the time, yesterday with the time, or the date (14/10/2026); "Nunca
   * entrou" for a person included from the directory who never signed in.
   */
  formatLastAccess: (isoUtc: string | null, t: TFunction, now?: Date) => string;
  /** The date of a moment (19/10/2026), as when a person joined a project. */
  formatDate: (isoUtc: string) => string;
  /** Day and month (16/10), as in the Audit list. */
  formatDayMonth: (isoUtc: string) => string;
  /** Time of day (09:58). */
  formatTime: (isoUtc: string) => string;
  /** Date and time (16/10/2026 09:58), as in the details of an audit record. */
  formatDateTime: (isoUtc: string) => string;
  /** The day of a moment. */
  toDayValue: (moment: Date) => DayValue;
  /** Midnight at the start of the day. */
  startOfDayValue: (day: DayValue) => Date;
  /** The day after, as the end of a period that includes its last day. */
  nextDayValue: (day: DayValue) => DayValue;
  /** The last days up to today, today included: 30 days by default. */
  lastDays: (count: number, today?: Date) => { from: DayValue; to: DayValue };
  /** A period as the Audit filter shows it (01/10 – 16/10). */
  formatPeriod: (from: DayValue, to: DayValue) => string;
  /** A day in full (01/10/2026), as the accessible name of the period says it. */
  formatDayValue: (day: DayValue) => string;
  /** The day of the week of a day of the calendar, in full (segunda-feira). */
  formatWeekday: (day: DayValue) => string;
  /**
   * A moment as a date and time of a spreadsheet (ADR-0099): the days since 30/12/1899, with the time as the fraction
   * of the day, on the clock of the time zone, as Excel keeps dates.
   */
  toSpreadsheetDateTime: (isoUtc: string) => number;
}

function create(timeZone: string): Dates {
  const time = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', timeZone });
  const date = new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone,
  });
  const dayMonth = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', timeZone });
  // Every part of a moment in the time zone, to find the day and the offset from UTC.
  const parts = new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hourCycle: 'h23',
    timeZone,
  });

  const fieldsOf = (moment: Date) => {
    const fields: Record<string, number> = {};
    for (const part of parts.formatToParts(moment)) {
      if (part.type !== 'literal') {
        fields[part.type] = Number(part.value);
      }
    }
    return {
      year: fields.year ?? 0,
      month: fields.month ?? 1,
      day: fields.day ?? 1,
      hour: fields.hour ?? 0,
      minute: fields.minute ?? 0,
      second: fields.second ?? 0,
    };
  };

  /** How far the clock of the time zone is from UTC at a moment, in milliseconds. */
  const offsetAt = (instant: number) => {
    const local = fieldsOf(new Date(instant));
    const asUtc = Date.UTC(
      local.year,
      local.month - 1,
      local.day,
      local.hour,
      local.minute,
      local.second,
    );
    return asUtc - Math.floor(instant / 1000) * 1000;
  };

  const toDayValue = (moment: Date): DayValue => {
    const local = fieldsOf(moment);
    return dayValueOf(local.year, local.month, local.day);
  };

  const startOfDayValue = (day: DayValue): Date => {
    const [year, month, dayOfMonth] = partsOf(day);
    const midnight = Date.UTC(year, month - 1, dayOfMonth);
    // The offset may change that day. Asked again at the first guess, it gives midnight; when the clock jumps over
    // midnight, that moment is still the day before, and the day starts at the first guess.
    const first = midnight - offsetAt(midnight);
    const second = midnight - offsetAt(first);
    return new Date(toDayValue(new Date(second)) === day ? second : first);
  };

  const nextDayValue = (day: DayValue): DayValue => addDays(day, 1);

  const formatWeekday = (day: DayValue): string =>
    weekday.format(new Date(dayNumber(day) * dayLength));

  const formatDayValue = formatDayText;

  return {
    timeZone,
    formatLastAccess: (isoUtc, t, now = new Date()) => {
      if (isoUtc === null) {
        return t('dates.never');
      }
      const moment = new Date(isoUtc);
      if (now.getTime() - moment.getTime() < 60_000) {
        return t('dates.now');
      }
      const days = dayNumber(toDayValue(now)) - dayNumber(toDayValue(moment));
      if (days === 0) {
        return t('dates.today', { time: time.format(moment) });
      }
      if (days === 1) {
        return t('dates.yesterday', { time: time.format(moment) });
      }
      return date.format(moment);
    },
    formatDate: (isoUtc) => date.format(new Date(isoUtc)),
    formatDayMonth: (isoUtc) => dayMonth.format(new Date(isoUtc)),
    formatTime: (isoUtc) => time.format(new Date(isoUtc)),
    formatDateTime: (isoUtc) => {
      const moment = new Date(isoUtc);
      return `${date.format(moment)} ${time.format(moment)}`;
    },
    toDayValue,
    startOfDayValue,
    nextDayValue,
    lastDays: (count, today = new Date()) => {
      const to = toDayValue(today);
      return { from: addDays(to, -(count - 1)), to };
    },
    formatPeriod: (from, to) =>
      `${formatDayValue(from).slice(0, 5)} – ${formatDayValue(to).slice(0, 5)}`,
    formatDayValue,
    formatWeekday,
    toSpreadsheetDateTime: (isoUtc) => {
      const local = fieldsOf(new Date(isoUtc));
      const asUtc = Date.UTC(
        local.year,
        local.month - 1,
        local.day,
        local.hour,
        local.minute,
        local.second,
      );
      return (asUtc - spreadsheetEpoch) / dayLength;
    },
  };
}

const byTimeZone = new Map<string, Dates>();

/** The dates of the screens in a time zone, the same object for the same time zone. */
export function datesIn(timeZone: string): Dates {
  let dates = byTimeZone.get(timeZone);
  if (dates === undefined) {
    dates = create(timeZone);
    byTimeZone.set(timeZone, dates);
  }
  return dates;
}

/** A day of the calendar as the screens show it (30/12/2026); a day has no time zone. */
export function formatDayText(day: DayValue): string {
  const [year, month, dayOfMonth] = partsOf(day);
  return `${pad(dayOfMonth)}/${pad(month)}/${String(year)}`;
}

/** The day and the month of a day of the calendar (26/10), as the strip of phases says when a project starts. */
export function formatDayMonthText(day: DayValue): string {
  return formatDayText(day).slice(0, 5);
}

/**
 * The period of a schedule as the header of a project shows it: 05/10 – 13/11/2026, with the year once when both
 * days are in it, and 15/12/2026 – 20/01/2027 otherwise.
 */
export function formatPeriodText(start: DayValue, end: DayValue): string {
  const sameYear = partsOf(start)[0] === partsOf(end)[0];
  return `${sameYear ? formatDayMonthText(start) : formatDayText(start)} – ${formatDayText(end)}`;
}

/** The day of the week of a day of the calendar, short (segunda, sábado), as a deadline says it. */
export function formatShortWeekdayText(day: DayValue): string {
  return weekday.format(new Date(dayNumber(day) * dayLength)).replace('-feira', '');
}

/** The day of the week and the day (segunda, 26/10), as a deadline says it. */
export function formatWeekdayDayText(day: DayValue): string {
  return `${formatShortWeekdayText(day)}, ${formatDayMonthText(day)}`;
}

/** A day in full, as the title of Meu trabalho shows today: Segunda-feira, 19 de outubro de 2026. */
export function formatLongDayText(day: DayValue): string {
  const text = longDay.format(new Date(dayNumber(day) * dayLength));
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** The short name of the month of a day (out, nov), as the box of a date shows it. */
export function formatShortMonthText(day: DayValue): string {
  return shortMonth.format(new Date(dayNumber(day) * dayLength)).replace('.', '');
}

/** The month and the year of a day of the calendar, in full: Outubro de 2026. */
export function formatMonthText(day: DayValue): string {
  const text = longMonth.format(new Date(dayNumber(day) * dayLength));
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** The number of the day of the month, with two digits (03), as the box of a date shows it. */
export function formatDayOfMonthText(day: DayValue): string {
  return pad(partsOf(day)[2]);
}

/** How many calendar days go from one day to another: 1 from a day to the next, negative backwards. */
export function daysBetween(from: DayValue, to: DayValue): number {
  return dayNumber(to) - dayNumber(from);
}

const typedDay = /^(\d{2})\/(\d{2})\/(\d{4})$/;

/** A day typed as dd/mm/aaaa (30/12/2026), or undefined when it is no day of the calendar. */
export function parseTypedDay(text: string): DayValue | undefined {
  const match = typedDay.exec(text.trim());
  if (match === null) {
    return undefined;
  }
  const [, day = '', month = '', year = ''] = match;
  const value = dayValueOf(Number(year), Number(month), Number(day));
  // 31/02 would roll over to March; a real day comes back the same.
  return addDays(value, 0) === value && Number(month) >= 1 && Number(month) <= 12
    ? value
    : undefined;
}

/** The digits of a day being typed, with the slashes in place: 30122026 becomes 30/12/2026. */
export function maskTypedDay(text: string): string {
  const digits = text.replace(/\D/g, '').slice(0, 8);
  return [digits.slice(0, 2), digits.slice(2, 4), digits.slice(4)]
    .filter((part) => part !== '')
    .join('/');
}
