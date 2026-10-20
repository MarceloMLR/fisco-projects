import type { DayValue } from '../format/dates';
import type { GanttScale } from '../schedule-gantt/ganttLayout';
import type { ScheduleDisplay } from './schedule';

/** What the print page shows (ADR-0021): the period, the scale and what "Exibir" had on. */
export interface PrintChoice {
  from: DayValue;
  to: DayValue;
  scale: GanttScale;
  display: ScheduleDisplay;
}

/** The address of the print page of a project with a choice. */
export function printAddress(projectId: string, choice: PrintChoice): string {
  const query = new URLSearchParams({
    from: choice.from,
    to: choice.to,
    scale: choice.scale,
    baseline: choice.display.baseline ? '1' : '0',
    critical: choice.display.critical ? '1' : '0',
  });
  return `/projects/${projectId}/schedule/print?${query.toString()}`;
}

const isDay = (value: string | null): value is DayValue =>
  value !== null && /^\d{4}-\d{2}-\d{2}$/.test(value);

/** The choice in the address of the print page; the schedule fills what is missing or wrong. */
export function choiceOf(search: URLSearchParams, start: DayValue, end: DayValue): PrintChoice {
  const from = search.get('from');
  const to = search.get('to');
  const scale = search.get('scale');
  const first = isDay(from) ? from : start;
  const last = isDay(to) && to >= first ? to : end >= first ? end : first;
  return {
    from: first,
    to: last,
    scale: scale === 'weeks' || scale === 'months' ? scale : 'days',
    display: { baseline: search.get('baseline') !== '0', critical: search.get('critical') !== '0' },
  };
}

/** The number of a day, counted from 01/01/1970, without time zones. */
export function dayNumber(day: DayValue): number {
  const [year = 1970, month = 1, date = 1] = day.split('-').map(Number);
  return Math.round(Date.UTC(year, month - 1, date) / 86_400_000);
}

/** The day of a number of <see cref="dayNumber"/>. */
export function dayOfNumber(number: number): DayValue {
  return new Date(number * 86_400_000).toISOString().slice(0, 10);
}

/** Where the days of the period fall in a strip of a width: each day takes the same part. */
export interface PrintScale {
  width: number;
  first: number;
  days: number;
  /** The left of a day, from the start of the period. */
  x: (day: DayValue) => number;
  /** The right of a day: the left of the next one. */
  xEnd: (day: DayValue) => number;
}

export function printScale(from: DayValue, to: DayValue, width: number): PrintScale {
  const first = dayNumber(from);
  const days = Math.max(1, dayNumber(to) - first + 1);
  const unit = width / days;
  return {
    width,
    first,
    days,
    x: (day) => (dayNumber(day) - first) * unit,
    xEnd: (day) => (dayNumber(day) - first + 1) * unit,
  };
}

/** A span of the scale, such as a month over the days or a week below them: its first day, its last and its label. */
export interface ScaleSpan {
  from: DayValue;
  to: DayValue;
  label: string;
}

const monthNames = new Intl.DateTimeFormat('pt-BR', { month: 'short', timeZone: 'UTC' });

function monthLabel(day: DayValue): string {
  const text = monthNames.format(new Date(`${day}T00:00:00Z`)).replace('.', '');
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/**
 * The two rows of the scale over the strip, as the timeline shows them (ADR-0071): the months ("Out 2026") over the
 * days or the weeks, the years over the months; each span cut at the period.
 */
export function scaleSpans(
  from: DayValue,
  to: DayValue,
  scale: GanttScale,
): { top: ScaleSpan[]; bottom: ScaleSpan[] } {
  const first = dayNumber(from);
  const last = dayNumber(to);
  const days = Array.from({ length: last - first + 1 }, (_, index) => dayOfNumber(first + index));
  const group = (
    keyOf: (day: DayValue) => string,
    labelOf: (day: DayValue) => string,
  ): ScaleSpan[] => {
    const spans: ScaleSpan[] = [];
    for (const day of days) {
      const current = spans.at(-1);
      if (current !== undefined && keyOf(current.from) === keyOf(day)) {
        current.to = day;
      } else {
        spans.push({ from: day, to: day, label: labelOf(day) });
      }
    }
    return spans;
  };
  const month = (day: DayValue) => day.slice(0, 7);
  // Weeks start on Monday, as the timeline.
  const monday = (day: DayValue) => {
    const number = dayNumber(day);
    const weekday = (new Date(number * 86_400_000).getUTCDay() + 6) % 7;
    return dayOfNumber(number - weekday);
  };
  if (scale === 'months') {
    return {
      top: group(
        (day) => day.slice(0, 4),
        (day) => day.slice(0, 4),
      ),
      bottom: group(month, monthLabel),
    };
  }
  return {
    top: group(month, (day) => `${monthLabel(day)} ${day.slice(0, 4)}`),
    bottom:
      scale === 'weeks'
        ? group(monday, (day) => `${monday(day).slice(8, 10)}/${monday(day).slice(5, 7)}`)
        : group(
            (day) => day,
            (day) => String(Number(day.slice(8, 10))),
          ),
  };
}
