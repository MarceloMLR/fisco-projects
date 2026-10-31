import {
  daysBetween,
  type DayValue,
  formatDayMonthText,
  formatShortMonthText,
} from '../format/dates';
import { type GanttPeriod, type GanttScale, plusDays } from './ganttLayout';

/** The measures of the timeline, from the Gantt of the design system (fp-gantt). */
export const timelineSizes = {
  /** A line of the table and of the chart (rowHeight 40, on the page of the schedule). */
  row: 40,
  /** The header: the scale in two tiers, and the names of the columns of the table. */
  head: 48,
  /** The line between the two tiers of the scale. */
  tier: 24,
  /** A bar of a task; it stands 2px above the middle of the line, so the baseline fits under it. */
  bar: 18,
  barTop: 9,
  /** The lozenge of a milestone, from its middle to a corner. */
  milestone: 8,
} as const;

/** Where the days of the period fall in the chart, and the day under a point. */
export interface TimelineGeometry {
  scale: GanttScale;
  period: GanttPeriod;
  /** The width of a day, a week or a month. */
  unit: number;
  /** The width of the whole period. */
  width: number;
  /** The left edge of a day. */
  xOf: (day: DayValue) => number;
  /** The right edge of a day: where a bar that ends on it stops. */
  xAfter: (day: DayValue) => number;
  /** How wide a day is: the same in days and weeks, its share of its month in months. */
  dayWidth: (day: DayValue) => number;
  /** The day under a point of the chart, within the period. */
  dayAt: (x: number) => DayValue;
}

function partsOf(day: DayValue): [number, number, number] {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return [year, month, dayOfMonth];
}

function daysInMonth(year: number, month: number): number {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

/**
 * The geometry of the timeline for a period and the width of its unit. Days and weeks divide the width evenly among
 * the days; each month takes the same width, its days dividing it, as the scale of months shows them.
 */
export function timelineGeometry(
  period: GanttPeriod,
  scale: GanttScale,
  unit: number,
): TimelineGeometry {
  const width = unit * period.units;
  const clamp = (x: number) => Math.min(Math.max(x, 0), Math.max(width - 0.001, 0));
  if (scale !== 'months') {
    const day = scale === 'days' ? unit : unit / 7;
    const xOf = (value: DayValue) => daysBetween(period.start, value) * day;
    return {
      scale,
      period,
      unit,
      width,
      xOf,
      xAfter: (value) => xOf(value) + day,
      dayWidth: () => day,
      dayAt: (x) => plusDays(period.start, Math.floor(clamp(x) / day)),
    };
  }
  const [firstYear, firstMonth] = partsOf(period.start);
  const monthIndex = (year: number, month: number) => (year - firstYear) * 12 + month - firstMonth;
  const xOf = (value: DayValue) => {
    const [year, month, dayOfMonth] = partsOf(value);
    return (monthIndex(year, month) + (dayOfMonth - 1) / daysInMonth(year, month)) * unit;
  };
  return {
    scale,
    period,
    unit,
    width,
    xOf,
    xAfter: (value) => xOf(plusDays(value, 1)),
    dayWidth: (value) => xOf(plusDays(value, 1)) - xOf(value),
    dayAt: (x) => {
      const at = clamp(x) / unit;
      const index = Math.floor(at);
      const year = firstYear + Math.floor((firstMonth - 1 + index) / 12);
      const month = ((firstMonth - 1 + index) % 12) + 1;
      const dayOfMonth = Math.floor((at - index) * daysInMonth(year, month)) + 1;
      return `${String(year)}-${String(month).padStart(2, '0')}-${String(dayOfMonth).padStart(2, '0')}`;
    },
  };
}

/** A cell of the scale: where it starts, how wide it is and what it says. */
export interface ScaleCell {
  x: number;
  width: number;
  text: string;
  /** The day, week or month of today, in the color of the action (g-scale-today). */
  today: boolean;
}

/** "Out", from "out": the month as the scale writes it. */
function monthName(day: DayValue): string {
  const text = formatShortMonthText(day);
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/**
 * The two tiers of the scale (ADR-0071): "Out 2026" over the days or the weeks, the year over the months; below, each
 * day, each week by its Monday (13/10) or each month (Out).
 */
export function scaleTiers(
  geometry: TimelineGeometry,
  today: DayValue,
): { top: ScaleCell[]; bottom: ScaleCell[] } {
  const { period, scale } = geometry;
  const bottom: ScaleCell[] = [];
  const top: ScaleCell[] = [];
  const step = scale === 'days' ? 1 : scale === 'weeks' ? 7 : 0;
  let day = period.start;
  while (daysBetween(day, period.end) >= 0) {
    const next =
      step > 0
        ? plusDays(day, step)
        : (() => {
            const [year, month] = partsOf(day);
            return plusDays(day, daysInMonth(year, month));
          })();
    const x = geometry.xOf(day);
    const width = geometry.xOf(next) - x;
    const last = plusDays(next, -1);
    bottom.push({
      x,
      width,
      text:
        scale === 'days'
          ? String(partsOf(day)[2])
          : scale === 'weeks'
            ? formatDayMonthText(day)
            : monthName(day),
      today: daysBetween(day, today) >= 0 && daysBetween(today, last) >= 0,
    });
    day = next;
  }
  // The top tier groups the cells of the bottom one by month, or by year over the months.
  for (const cell of bottom) {
    const day = geometry.dayAt(cell.x);
    const [year] = partsOf(day);
    const text = scale === 'months' ? String(year) : `${monthName(day)} ${String(year)}`;
    const previous = top.at(-1);
    if (previous?.text === text) {
      previous.width = cell.x + cell.width - previous.x;
    } else {
      top.push({ x: cell.x, width: cell.width, text, today: false });
    }
  }
  return { top, bottom };
}

/** What a drag does to a bar: moves it whole, or pulls its first or its last day. */
export type DragMode = 'move' | 'start' | 'end';

/**
 * The dates of a bar dragged so many days: both of them when it moves, as the server keeps the duration in business
 * days (ADR-0019); one when an edge is pulled, never past the other one, so a task keeps at least a day.
 */
export function draggedDates(
  mode: DragMode,
  start: DayValue,
  end: DayValue,
  days: number,
): { start: DayValue; end: DayValue } {
  if (mode === 'move') {
    return { start: plusDays(start, days), end: plusDays(end, days) };
  }
  if (mode === 'start') {
    const moved = plusDays(start, days);
    return { start: daysBetween(moved, end) < 0 ? end : moved, end };
  }
  const moved = plusDays(end, days);
  return { start, end: daysBetween(start, moved) < 0 ? start : moved };
}
