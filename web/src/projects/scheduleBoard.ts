import type { Schedule, ScheduleItem } from '../api/client';

/** The columns of the board of the tasks, by the progress (ADR-0155). */
export const boardColumnIds = ['todo', 'doing', 'done'] as const;

export type BoardColumnId = (typeof boardColumnIds)[number];

/**
 * A card of the board: a task with its own dates, its phase and, for a subtask, the task it is in (ADR-0088), and the
 * project whose progress it changes; in Minhas tarefas, with the code of the project, shown on the card (ADR-0158).
 */
export interface BoardCard {
  item: ScheduleItem;
  phase: { name: string };
  parent: { name: string } | undefined;
  projectId: string;
  projectCode?: string;
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

/** The cards of the schedule of a project, in the order of the schedule: the tasks with their own dates. */
export function cardsOf(schedule: Schedule, projectId: string): BoardCard[] {
  const byId = new Map(
    schedule.phases.flatMap((phase) => phase.items.map((item) => [item.id, item])),
  );
  return schedule.phases.flatMap((phase) =>
    phase.items.filter(hasCard).map((item) => ({
      item,
      phase,
      parent: item.parentId === null ? undefined : byId.get(item.parentId),
      projectId,
    })),
  );
}

/**
 * The columns of the board, each with its cards in order (ADR-0155): A fazer and Em andamento by the end, from the
 * earliest; Concluída by the day of completion, from the latest; on a tie, the order the cards come in. A card drafted
 * to another column (`drafts`, while its progress is being saved) shows in that column.
 */
export function columnsOf(
  cards: readonly BoardCard[],
  drafts: ReadonlyMap<string, BoardColumnId> = new Map(),
): BoardColumn[] {
  const placed = new Map<BoardColumnId, { card: BoardCard; order: number }[]>(
    boardColumnIds.map((id) => [id, []]),
  );
  cards.forEach((card, order) => {
    placed.get(drafts.get(card.item.id) ?? columnOf(card.item))?.push({ card, order });
  });
  return boardColumnIds.map((id) => ({
    id,
    cards: (placed.get(id) ?? [])
      .sort((one, other) => compare(id, one.card.item, other.card.item) || one.order - other.order)
      .map(({ card }) => card),
  }));
}

/** The columns of the board of a schedule (ADR-0155), in the order of the schedule on a tie. */
export function boardColumns(
  schedule: Schedule,
  drafts: ReadonlyMap<string, BoardColumnId> = new Map(),
  projectId = '',
): BoardColumn[] {
  return columnsOf(cardsOf(schedule, projectId), drafts);
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
