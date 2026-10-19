import type { TFunction } from 'i18next';
import type { ProjectScheduleSummary, Schedule, ScheduleItem, SchedulePhase } from '../api/client';
import { formatDayMonthText } from '../format/dates';

/** A line of the Lista of the schedule: a phase, with its dates and progress, or a task or milestone under it. */
export type ScheduleRow =
  | { kind: 'phase'; id: string; phase: SchedulePhase }
  | { kind: 'item'; id: string; item: ScheduleItem; phase: SchedulePhase };

export const isPhaseRow = (row: ScheduleRow) => row.kind === 'phase';

/** The lines of the Lista: each phase in the order of the API, with its items below it in order. */
export function scheduleRows(schedule: Schedule): ScheduleRow[] {
  return schedule.phases.flatMap((phase) => [
    { kind: 'phase', id: phase.id, phase } as const,
    ...phase.items.map((item) => ({ kind: 'item', id: item.id, item, phase }) as const),
  ]);
}

/** Where a milestone is: reached (on a day), late (its day passed without it) or pending (ADR-0086). */
export type MilestoneStatus = 'reached' | 'late' | 'pending';

export function milestoneStatus(item: ScheduleItem): MilestoneStatus {
  if (item.reachedOn !== null) {
    return 'reached';
  }
  return item.late ? 'late' : 'pending';
}

/** Every task and milestone of the schedule, in the order of the Lista. */
export function itemsOf(schedule: Schedule): ScheduleItem[] {
  return schedule.phases.flatMap((phase) => phase.items);
}

/** A task or milestone of the schedule, with its phase; none when it is no longer in it. */
export function findItem(
  schedule: Schedule,
  itemId: string,
): { item: ScheduleItem; phase: SchedulePhase } | undefined {
  for (const phase of schedule.phases) {
    const item = phase.items.find((candidate) => candidate.id === itemId);
    if (item !== undefined) {
      return { item, phase };
    }
  }
  return undefined;
}

/** The names of the items an item depends on, in the order of the Lista. */
export function predecessorNames(item: ScheduleItem, schedule: Schedule): string[] {
  return itemsOf(schedule)
    .filter((other) => item.predecessorIds.includes(other.id))
    .map((other) => other.name);
}

/**
 * The items that depend on an item, directly or through others: an item cannot depend on them, since that would make
 * a cycle (ADR-0019).
 */
export function itemsAfter(itemId: string, schedule: Schedule): Set<string> {
  const items = itemsOf(schedule);
  const found = new Set<string>();
  const pending = [itemId];
  for (let current = pending.pop(); current !== undefined; current = pending.pop()) {
    for (const other of items) {
      if (other.predecessorIds.includes(current) && !found.has(other.id)) {
        found.add(other.id);
        pending.push(other.id);
      }
    }
  }
  return found;
}

/**
 * What the list of projects says under the strip of phases: the progress and the current phase ("62% ·
 * Configuração"), when the project starts ("Começa em 26/10") or when it was completed ("100% · concluído em 28/08").
 */
export function summaryText(summary: ProjectScheduleSummary, today: string, t: TFunction): string {
  if (summary.completedOn !== null) {
    return t('projects.schedule.summary.completed', {
      day: formatDayMonthText(summary.completedOn),
    });
  }
  if (summary.start > today) {
    return t('projects.schedule.summary.starts', { day: formatDayMonthText(summary.start) });
  }
  return summary.currentPhase === null
    ? t('projects.schedule.summary.progress', { progress: summary.progress })
    : t('projects.schedule.summary.phase', {
        progress: summary.progress,
        phase: summary.currentPhase,
      });
}
