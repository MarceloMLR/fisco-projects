import type { TFunction } from 'i18next';
import type {
  ProjectScheduleSummary,
  Schedule,
  ScheduleItem,
  ScheduleLink,
  ScheduleLinkType,
  SchedulePhase,
} from '../api/client';
import type { DueState } from '../components/DueDate';
import { formatDayMonthText } from '../format/dates';

/** A line of the Lista of the schedule: a phase, with its dates and progress, or a task or milestone under it. */
export type ScheduleRow =
  | { kind: 'phase'; id: string; phase: SchedulePhase }
  | { kind: 'item'; id: string; item: ScheduleItem; phase: SchedulePhase };

export const isPhaseRow = (row: ScheduleRow) => row.kind === 'phase';

/**
 * The lines of the Lista: each phase in the order of the API, with its items below it in order, but the items of the
 * phases folded (Cronograma-4.2) and the subtasks of the tasks whose subtasks are hidden (ADR-0088).
 */
export function scheduleRows(
  schedule: Schedule,
  collapsed: ReadonlySet<string> = new Set(),
): ScheduleRow[] {
  return schedule.phases.flatMap((phase) => [
    { kind: 'phase', id: phase.id, phase } as const,
    ...(collapsed.has(phase.id) ? [] : phase.items)
      .filter((item) => item.parentId === null || !collapsed.has(item.parentId))
      .map((item) => ({ kind: 'item', id: item.id, item, phase }) as const),
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

/**
 * Whether an item says by how much it is late: a late task always ("Atrasada" while no business day was lost yet); a
 * late milestone, from the first business day lost, since its state already says "Atrasado" (ADR-0086).
 */
export function showsDelay(item: ScheduleItem): boolean {
  return item.late && (item.kind === 'Task' || item.lateDays > 0);
}

/**
 * What a late task or milestone says: "1 dia útil de atraso" or "3 dias úteis de atraso", as the API counts them;
 * "Atrasada" or "Atrasado" while the delay began on non-business days only (ADR-0020, ADR-0086).
 */
export function lateText(item: ScheduleItem, t: TFunction): string {
  if (item.lateDays > 0) {
    return t('projects.schedule.lateDays', { count: item.lateDays });
  }
  return t(
    item.kind === 'Milestone' ? 'projects.schedule.milestone.late' : 'projects.schedule.late',
  );
}

/** Whether a task or milestone is done: a task at 100%, a milestone reached (ADR-0020, ADR-0086). */
export function isDone(item: Pick<ScheduleItem, 'kind' | 'progress' | 'reachedOn'>): boolean {
  return item.kind === 'Milestone' ? item.reachedOn !== null : item.progress >= 100;
}

type DeadlineOf = Pick<
  ScheduleItem,
  'kind' | 'progress' | 'reachedOn' | 'deadline' | 'deadlineDeviation'
>;

/**
 * Whether an item still open went past its deadline (GanttA-2), as the API counts the missed ones: the business days
 * it went past it, by its end or today, are more than zero.
 */
export function missesDeadline(item: DeadlineOf): boolean {
  return item.deadlineDeviation !== null && item.deadlineDeviation > 0 && !isDone(item);
}

/**
 * How the deadline of an item is going, by icon, color and text (ADR-0065): missed while open; met when it was done on
 * it or before; on track before it. An item done after it says so without the alarm, since nothing is left to do. None
 * without a deadline.
 */
export function deadlineState(item: DeadlineOf): DueState | undefined {
  if (item.deadline === null || item.deadlineDeviation === null) {
    return undefined;
  }
  if (missesDeadline(item)) {
    return 'overdue';
  }
  return isDone(item) && item.deadlineDeviation <= 0 ? 'met' : 'onTrack';
}

/**
 * The deadline in words, with the business days the API counts: "Passou 1 dia útil da data-limite", "3 dias úteis
 * antes da data-limite" or "No dia da data-limite".
 */
export function deadlineText(item: Pick<ScheduleItem, 'deadlineDeviation'>, t: TFunction): string {
  const deviation = item.deadlineDeviation ?? 0;
  if (deviation > 0) {
    return t('projects.schedule.deadline.past', { count: deviation });
  }
  return deviation < 0
    ? t('projects.schedule.deadline.before', { count: -deviation })
    : t('projects.schedule.deadline.onDay');
}

/**
 * Who is in charge of a task as the screens say it (ADR-0102): the person, the client of the project, or nobody
 * (null). A task of the client of a project that lost its client says only "O cliente".
 */
export function ownerText(
  item: Pick<ScheduleItem, 'owner' | 'ownedByClient'>,
  clientName: string | null,
  t: TFunction,
): string | null {
  if (item.owner !== null) {
    return item.owner.name;
  }
  if (!item.ownedByClient) {
    return null;
  }
  return clientName === null
    ? t('projects.schedule.clientOwnerWithoutClient')
    : t('projects.schedule.clientOwner', { name: clientName });
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

/** The kinds of a dependency, in the order the screen offers them (ADR-0137). */
export const linkTypes: readonly ScheduleLinkType[] = [
  'FinishToStart',
  'StartToStart',
  'FinishToFinish',
];

/** The kind and the lag of the dependency of an item on another: "finishes, then starts" without lag by default. */
export function linkOf(
  item: Pick<ScheduleItem, 'links'>,
  predecessorId: string,
): Pick<ScheduleLink, 'type' | 'lag'> {
  return (
    item.links.find((link) => link.predecessorId === predecessorId) ?? {
      type: 'FinishToStart',
      lag: 0,
    }
  );
}

/**
 * The float of a task or milestone in words (GanttB-1): "4 dias úteis de folga", or "Sem folga: no caminho crítico";
 * nothing for a task with subtasks, which has none of its own.
 */
export function slackText(item: Pick<ScheduleItem, 'float'>, t: TFunction): string | undefined {
  if (item.float === null) {
    return undefined;
  }
  return item.float > 0
    ? t('projects.schedule.slack.days', { count: item.float })
    : t('projects.schedule.slack.none');
}

/** A lag with its sign, as "+2" or "-1". */
export const signedLag = (lag: number) => (lag > 0 ? `+${String(lag)}` : String(lag));

/**
 * What a dependency says beside the name, when it is not "finishes, then starts" without lag: "começa junto, +1",
 * "termina junto" or "+2" (ADR-0137); nothing otherwise.
 */
export function linkText(
  link: Pick<ScheduleLink, 'type' | 'lag'>,
  t: TFunction,
): string | undefined {
  const lag = link.lag === 0 ? undefined : signedLag(link.lag);
  if (link.type === 'FinishToStart') {
    return lag;
  }
  const kind = t(`projects.schedule.links.short.${link.type}`);
  return lag === undefined ? kind : `${kind}, ${lag}`;
}

/** The items an item depends on, each with its kind and lag when they are not the usual: "Testes (começa junto, +1)". */
export function predecessorTexts(item: ScheduleItem, schedule: Schedule, t: TFunction): string[] {
  return itemsOf(schedule)
    .filter((other) => item.predecessorIds.includes(other.id))
    .map((other) => {
      const text = linkText(linkOf(item, other.id), t);
      return text === undefined ? other.name : `${other.name} (${text})`;
    });
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
 * The items an item may depend on, as "Depende de" lists them in the details and in the Lista (ADR-0143): the ones
 * with dates of their own (ADR-0088), not the item, not the ones after it (they would close a cycle) and not the task
 * it is inside of; each with the name of its phase, in the order of the schedule.
 */
export function predecessorChoices(
  schedule: Schedule,
  itemId: string | undefined,
  parentId: string | undefined,
): { value: string; label: string; group: string | undefined }[] {
  const after = itemId === undefined ? new Set<string>() : itemsAfter(itemId, schedule);
  const phaseNames = new Map(schedule.phases.map((phase) => [phase.id, phase.name]));
  return itemsOf(schedule)
    .filter(
      (item) =>
        item.id !== itemId && !after.has(item.id) && !item.hasSubtasks && item.id !== parentId,
    )
    .map((item) => ({ value: item.id, label: item.name, group: phaseNames.get(item.phaseId) }));
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

/** What the Linha do tempo shows over the bars (ADR-0021, ADR-0071). */
export interface ScheduleDisplay {
  baseline: boolean;
  critical: boolean;
}

/** Both at first, as the canvas shows. */
export const fullDisplay: ScheduleDisplay = { baseline: true, critical: true };

/** A change of the schedule the person may undo or redo (ADR-0091). */
export type ScheduleStep = NonNullable<Schedule['undo']>;

/** What a change was, as undoing it says: "a inclusão da tarefa Configurar plano de contas". */
export function stepText(step: ScheduleStep, t: TFunction): string {
  return t(`projects.steps.what.${step.action}`, { name: step.name });
}

/** The tasks and milestones a move of the schedule takes: the open ones with dates of their own (ADR-0140). */
export function movableCount(schedule: Schedule): number {
  return itemsOf(schedule).filter((item) => !item.hasSubtasks && !isDone(item)).length;
}
