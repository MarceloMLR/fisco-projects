import type { Schedule, ScheduleItem, SchedulePhase } from '../api/client';

/** The columns of the board of the tasks, by the progress (ADR-0155). */
export const boardColumnIds = ['todo', 'doing', 'done'] as const;

export type BoardColumnId = (typeof boardColumnIds)[number];

/** A card of the board: a task with its own dates, its phase and, for a subtask, the task it is in (ADR-0088). */
export interface BoardCard {
  item: ScheduleItem;
  phase: SchedulePhase;
  parent: ScheduleItem | undefined;
}

export interface BoardColumn {
  id: BoardColumnId;
  cards: BoardCard[];
}

/** What moving a card to a column does (ADR-0086): takes it to 100% or to 0% at once, or asks for the percentage. */
export type BoardMove = 'complete' | 'restart' | 'ask';

/** The column of a task by its progress: A fazer at 0%, Em andamento from 1% to 99%, Concluída at 100%. */
export function columnOf(item: Pick<ScheduleItem, 'progress'>): BoardColumnId {
  if (item.progress <= 0) {
    return 'todo';
  }
  return item.progress >= 100 ? 'done' : 'doing';
}

/** Whether a task or milestone has a card: the tasks with their own dates, never a summary task or a milestone. */
export function hasCard(item: Pick<ScheduleItem, 'kind' | 'hasSubtasks'>): boolean {
  return item.kind === 'Task' && !item.hasSubtasks;
}

/**
 * The columns of the board, each with its cards in order (ADR-0155): A fazer and Em andamento by the end, from the
 * earliest; Concluída by the day of completion, from the latest; on a tie, the order of the schedule. A card drafted to
 * another column (`drafts`, while its progress is being saved) shows in that column.
 */
export function boardColumns(
  schedule: Schedule,
  drafts: ReadonlyMap<string, BoardColumnId> = new Map(),
): BoardColumn[] {
  const byId = new Map(
    schedule.phases.flatMap((phase) => phase.items.map((item) => [item.id, item])),
  );
  const placed = new Map<BoardColumnId, { card: BoardCard; order: number }[]>(
    boardColumnIds.map((id) => [id, []]),
  );
  let order = 0;
  for (const phase of schedule.phases) {
    for (const item of phase.items) {
      order += 1;
      if (!hasCard(item)) {
        continue;
      }
      const parent = item.parentId === null ? undefined : byId.get(item.parentId);
      const column = drafts.get(item.id) ?? columnOf(item);
      placed.get(column)?.push({ card: { item, phase, parent }, order });
    }
  }
  return boardColumnIds.map((id) => ({
    id,
    cards: (placed.get(id) ?? [])
      .sort((one, other) => compare(id, one.card.item, other.card.item) || one.order - other.order)
      .map(({ card }) => card),
  }));
}

/** The order of two cards in a column: the end ascending, or the completion descending in Concluída. */
function compare(column: BoardColumnId, one: ScheduleItem, other: ScheduleItem): number {
  if (column === 'done') {
    return (other.completedOn ?? other.end).localeCompare(one.completedOn ?? one.end);
  }
  return one.end.localeCompare(other.end);
}

/** What moving a task to a column does; none when it is already there. */
export function moveOf(
  item: Pick<ScheduleItem, 'progress'>,
  to: BoardColumnId,
): BoardMove | undefined {
  if (columnOf(item) === to) {
    return undefined;
  }
  switch (to) {
    case 'done':
      return 'complete';
    case 'todo':
      return 'restart';
    case 'doing':
      return 'ask';
  }
}

/** The progress a move saves at once; none when the move asks for the percentage. */
export function progressOf(move: BoardMove): number | undefined {
  switch (move) {
    case 'complete':
      return 100;
    case 'restart':
      return 0;
    case 'ask':
      return undefined;
  }
}
