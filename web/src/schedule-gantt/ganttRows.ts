import type { Schedule, ScheduleItem, ScheduleLinkType, SchedulePhase } from '../api/client';
import type { DayValue } from '../format/dates';
import { missesDeadline } from '../projects/schedule';

/** What a line of the timeline is: a phase, or a task or milestone in it. */
export type GanttTarget =
  | { kind: 'phase'; phase: SchedulePhase }
  | { kind: 'item'; item: ScheduleItem; phase: SchedulePhase };

/**
 * How a line is drawn (Gantt of the design system): a phase or a task with subtasks as a summary (g-summary), a task
 * as a bar with the part done (g-bar), a milestone as a lozenge (g-milestone).
 */
export type GanttShape = 'summary' | 'bar' | 'milestone';

/** A line of the timeline: what the table and the chart draw of a phase, a task or a milestone. */
export interface GanttRow {
  /** The id of the phase or of the item. */
  key: string;
  name: string;
  /** 0 for a phase, 1 for its tasks and milestones, 2 for the subtasks (ADR-0088). */
  level: number;
  parentKey?: string;
  shape: GanttShape;
  /** The first and the last day; none for a phase without tasks and milestones. */
  start?: DayValue;
  end?: DayValue;
  progress: number;
  late: boolean;
  /** On the critical path, when it is shown (ADR-0021). */
  critical: boolean;
  /** It has lines inside it, which it can hide. */
  hasChildren: boolean;
  /** A task or milestone with dates of its own: it moves, has an edge pulled and links (ADR-0088). */
  ownDates: boolean;
  /** The dates of a task in the baseline (ADR-0021), drawn as a thin bar under its own. */
  baseline?: { start: DayValue; end: DayValue };
  /**
   * The deadline of a task or milestone (GanttA-2), marked at the end of its day: missed while the item is open past
   * it, by the business days it went past it.
   */
  deadline?: { day: DayValue; missed: boolean; days: number };
  /** The keys of the lines it depends on: they end before it starts (ADR-0019). */
  predecessors: string[];
  /** The latest end of a task or milestone with float, drawn as a thin line after it with the critical path (GanttB-1). */
  slackEnd?: DayValue;
  /** The kind of each dependency that is not "finishes, then starts", by the key of the line before (ADR-0137). */
  predecessorTypes?: Partial<Record<string, ScheduleLinkType>>;
  target: GanttTarget;
}

/**
 * The lines of the timeline: each phase in the order of the API with its items below it, and each subtask below its
 * task, which is then drawn as a summary, as a phase (ADR-0088), with the critical path when it is shown (ADR-0021).
 * Only a task with dates of its own has its dates in the baseline drawn, as the design system draws them.
 */
export function ganttRowsOf(schedule: Schedule, { critical = false } = {}): GanttRow[] {
  const rows: GanttRow[] = [];
  const known = new Set(schedule.phases.flatMap((phase) => phase.items.map((item) => item.id)));
  for (const phase of schedule.phases) {
    rows.push({
      key: phase.id,
      name: phase.name,
      level: 0,
      shape: 'summary',
      ...(phase.start === null || phase.end === null ? {} : { start: phase.start, end: phase.end }),
      progress: phase.progress,
      late: phase.late,
      critical: false,
      hasChildren: phase.items.length > 0,
      ownDates: false,
      predecessors: [],
      target: { kind: 'phase', phase },
    });
    for (const item of phase.items) {
      const milestone = item.kind === 'Milestone';
      const ownDates = !item.hasSubtasks;
      rows.push({
        key: item.id,
        name: item.name,
        // The API sends each task before its subtasks.
        level: item.parentId === null ? 1 : 2,
        parentKey: item.parentId ?? phase.id,
        shape: milestone ? 'milestone' : item.hasSubtasks ? 'summary' : 'bar',
        start: item.start,
        end: milestone ? item.start : item.end,
        progress: milestone ? 0 : item.progress,
        late: item.late,
        critical: critical && item.critical,
        hasChildren: item.hasSubtasks,
        ownDates,
        ...(ownDates && !milestone && item.baselineStart !== null && item.baselineEnd !== null
          ? { baseline: { start: item.baselineStart, end: item.baselineEnd } }
          : {}),
        ...(item.deadline === null
          ? {}
          : {
              deadline: {
                day: item.deadline,
                missed: missesDeadline(item),
                days: item.deadlineDeviation ?? 0,
              },
            }),
        ...(ownDates && item.float !== null && item.float > 0 && item.latestEnd !== null
          ? { slackEnd: item.latestEnd }
          : {}),
        predecessors: item.predecessorIds.filter((id) => known.has(id)),
        predecessorTypes: Object.fromEntries(
          item.links
            .filter((link) => link.type !== 'FinishToStart')
            .map((link) => [link.predecessorId, link.type]),
        ),
        target: { kind: 'item', item, phase },
      });
    }
  }
  return rows;
}

/** The lines on the screen: none inside a phase or a task that is hidden, nor inside one whose lines are hidden. */
export function shownRows(rows: readonly GanttRow[], collapsed: ReadonlySet<string>): GanttRow[] {
  const hidden = new Set<string>();
  return rows.filter((row) => {
    if (
      row.parentKey !== undefined &&
      (collapsed.has(row.parentKey) || hidden.has(row.parentKey))
    ) {
      hidden.add(row.key);
      return false;
    }
    return true;
  });
}
