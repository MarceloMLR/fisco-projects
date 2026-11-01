import type { TFunction } from 'i18next';
import type { Schedule, ScheduleItem } from '../api/client';
import { daysBetween, type DayValue } from '../format/dates';
import { isDone, itemsOf, missesDeadline } from './schedule';

/** The filters of one kind each, without a choice (GanttA-3). */
export const plainFilters = [
  'late',
  'missedDeadlines',
  'critical',
  'mine',
  'nextTwoWeeks',
] as const;

export type PlainFilter = (typeof plainFilters)[number];

/** The owner of "Por responsável": a person, the client of the project, or nobody. */
export type FilterOwner = string;

/** The value of "Por responsável" for the tasks of the client (ADR-0102) and for the tasks without an owner. */
export const clientFilterOwner = 'client';
export const noFilterOwner = 'none';

/** One filter of the schedule at a time (GanttA-3). */
export type ScheduleFilter = { kind: PlainFilter } | { kind: 'owner'; owner: FilterOwner };

/** What a filter needs besides the item: today of the installation and who is in the session. */
export interface FilterContext {
  today: DayValue;
  me: string | undefined;
}

/** How many calendar days after today "Próximas duas semanas" reaches: two weeks, today included. */
const twoWeeks = 13;

const isPlain = (value: string | null): value is PlainFilter =>
  plainFilters.some((filter) => filter === value);

/** The filter the address asks for (`?filter=` and, by owner, `&owner=`); none for a value that does not exist. */
export function filterOf(search: URLSearchParams): ScheduleFilter | undefined {
  const kind = search.get('filter');
  if (isPlain(kind)) {
    return { kind };
  }
  const owner = search.get('owner');
  return kind === 'owner' && owner !== null && owner !== '' ? { kind: 'owner', owner } : undefined;
}

/** The address with the filter, or without it; the rest of the address stays as it is. */
export function withFilter(search: URLSearchParams, filter: ScheduleFilter | undefined) {
  const next = new URLSearchParams(search);
  next.delete('filter');
  next.delete('owner');
  if (filter !== undefined) {
    next.set('filter', filter.kind);
    if (filter.kind === 'owner') {
      next.set('owner', filter.owner);
    }
  }
  return next;
}

/** Whether a task or milestone passes the filter. */
export function matchesFilter(
  item: ScheduleItem,
  filter: ScheduleFilter,
  context: FilterContext,
): boolean {
  switch (filter.kind) {
    case 'late':
      return item.late;
    case 'missedDeadlines':
      return missesDeadline(item);
    case 'critical':
      return item.critical;
    case 'mine':
      return context.me !== undefined && item.owner?.id === context.me;
    case 'nextTwoWeeks':
      // Happening or starting from today to two weeks ahead, still open; the late ones have their own filter.
      return (
        !isDone(item) &&
        daysBetween(context.today, item.start) <= twoWeeks &&
        daysBetween(context.today, item.kind === 'Milestone' ? item.start : item.end) >= 0
      );
    case 'owner':
      if (filter.owner === clientFilterOwner) {
        return item.ownedByClient;
      }
      if (filter.owner === noFilterOwner) {
        return item.kind === 'Task' && item.owner === null && !item.ownedByClient;
      }
      return item.owner?.id === filter.owner;
  }
}

/**
 * The schedule with only the tasks and milestones that pass the filter, and the task each subtask is in, so the line
 * keeps its place; the phases without any of them stay out. The numbers of the schedule (the period, the progress, the
 * delays) stay those of the whole schedule.
 */
export function filteredSchedule(
  schedule: Schedule,
  filter: ScheduleFilter,
  context: FilterContext,
): Schedule {
  return {
    ...schedule,
    phases: schedule.phases
      .map((phase) => {
        const passing = new Set(
          phase.items.filter((item) => matchesFilter(item, filter, context)).map((item) => item.id),
        );
        const parents = new Set(
          phase.items
            .filter((item) => passing.has(item.id) && item.parentId !== null)
            .map((item) => item.parentId),
        );
        return {
          ...phase,
          items: phase.items.filter((item) => passing.has(item.id) || parents.has(item.id)),
        };
      })
      .filter((phase) => phase.items.length > 0),
  };
}

/** How many tasks and milestones pass the filter, the tasks a subtask is in only by itself. */
export function passingCount(
  schedule: Schedule,
  filter: ScheduleFilter,
  context: FilterContext,
): number {
  return itemsOf(schedule).filter((item) => matchesFilter(item, filter, context)).length;
}

/** An option of "Por responsável": the value and the name. */
export interface OwnerOption {
  owner: FilterOwner;
  label: string;
}

/**
 * The options of "Por responsável": the people in charge of some task, by name; the client, when the project has one
 * or a task is of the client; and "Sem responsável".
 */
export function ownerOptions(
  schedule: Schedule,
  clientName: string | null,
  t: TFunction,
): OwnerOption[] {
  const people = new Map<string, string>();
  let client = clientName !== null;
  for (const item of itemsOf(schedule)) {
    if (item.owner !== null) {
      people.set(item.owner.id, item.owner.name);
    }
    client ||= item.ownedByClient;
  }
  return [
    ...[...people]
      .toSorted(([, one], [, other]) => one.localeCompare(other, 'pt-BR'))
      .map(([owner, label]) => ({ owner, label })),
    ...(client
      ? [
          {
            owner: clientFilterOwner,
            label:
              clientName === null
                ? t('projects.schedule.clientOwnerWithoutClient')
                : t('projects.schedule.clientOwner', { name: clientName }),
          },
        ]
      : []),
    { owner: noFilterOwner, label: t('projects.schedule.noOwner') },
  ];
}

/** The name of a filter, as the button says it once chosen. */
export function filterText(
  filter: ScheduleFilter,
  options: readonly OwnerOption[],
  t: TFunction,
): string {
  if (filter.kind !== 'owner') {
    return t(`projects.schedule.filter.kinds.${filter.kind}`);
  }
  const name = options.find((option) => option.owner === filter.owner)?.label;
  return name ?? t('projects.schedule.filter.kinds.owner');
}
