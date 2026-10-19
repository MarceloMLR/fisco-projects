import type { Schedule, ScheduleItem, SchedulePhase } from '../api/client';
import type { DayValue } from '../format/dates';

/** What a line of the timeline is: a phase, or a task or milestone in it. */
export type GanttTarget =
  | { kind: 'phase'; phase: SchedulePhase }
  | { kind: 'item'; item: ScheduleItem; phase: SchedulePhase };

/**
 * A line of the timeline as the Syncfusion Gantt takes it. The Gantt names the lines by numbers, and its
 * dependencies are texts such as "2FS,5FS", so each line gets a number of its own.
 */
export interface GanttRow {
  key: number;
  name: string;
  /** Midnight, in the time of the browser, of the first day; none for a phase without items. */
  start?: Date;
  /** Midnight after the last day (the Gantt draws up to it); a milestone ends on its own day. */
  end?: Date;
  progress: number;
  parentKey?: number;
  predecessor?: string;
  milestone: boolean;
  cssClass: string;
  target: GanttTarget;
}

/** Midnight of a day of the calendar, in the time of the browser: the Gantt draws days without time zones. */
export function dateOfDay(day: DayValue): Date {
  const [year = 0, month = 1, dayOfMonth = 1] = day.split('-').map(Number);
  return new Date(year, month - 1, dayOfMonth);
}

/** The day of the calendar a moment of the Gantt falls on, in the time of the browser. */
export function dayOfDate(moment: Date): DayValue {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${String(moment.getFullYear())}-${pad(moment.getMonth() + 1)}-${pad(moment.getDate())}`;
}

/** Midnight after a day: where the bar of a task that ends on it stops. */
export function endOfDay(day: DayValue): Date {
  const moment = dateOfDay(day);
  moment.setDate(moment.getDate() + 1);
  return moment;
}

/** The last day of a bar that stops at a moment: a bar that stops at midnight ends the day before. */
export function lastDayOf(end: Date): DayValue {
  return dayOfDate(new Date(end.getTime() - 1));
}

/**
 * The lines of the timeline: each phase in the order of the API with its items below it; the classes say what each
 * one is, for the colors of the design system (fp-gantt), and the dependencies point to the numbers of the lines.
 */
export function ganttRowsOf(schedule: Schedule): GanttRow[] {
  const rows: GanttRow[] = [];
  const keys = new Map<string, number>();
  let next = 1;
  for (const phase of schedule.phases) {
    const phaseKey = next++;
    rows.push({
      key: phaseKey,
      name: phase.name,
      ...(phase.start === null || phase.end === null
        ? {}
        : { start: dateOfDay(phase.start), end: endOfDay(phase.end) }),
      progress: phase.progress,
      milestone: false,
      cssClass: phase.late ? 'fp-gantt-phase fp-gantt-late' : 'fp-gantt-phase',
      target: { kind: 'phase', phase },
    });
    for (const item of phase.items) {
      const key = next++;
      keys.set(item.id, key);
      const milestone = item.kind === 'Milestone';
      rows.push({
        key,
        name: item.name,
        start: dateOfDay(item.start),
        end: milestone ? dateOfDay(item.start) : endOfDay(item.end),
        progress: milestone ? 0 : item.progress,
        parentKey: phaseKey,
        milestone,
        cssClass: [
          milestone ? 'fp-gantt-milestone' : 'fp-gantt-task',
          item.late ? 'fp-gantt-late' : '',
        ]
          .join(' ')
          .trim(),
        target: { kind: 'item', item, phase },
      });
    }
  }
  for (const row of rows) {
    if (row.target.kind === 'item' && row.target.item.predecessorIds.length > 0) {
      row.predecessor = row.target.item.predecessorIds
        .flatMap((id) => {
          const key = keys.get(id);
          return key === undefined ? [] : [`${String(key)}FS`];
        })
        .join(',');
    }
  }
  return rows;
}
