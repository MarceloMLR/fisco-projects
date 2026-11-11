import type { MyTasks, ScheduleItem } from '../api/client';
import type { DayValue } from '../format/dates';
import { addDays, weekStart } from '../hours/hours';
import type { BoardCard } from '../projects/scheduleBoard';

/** A task of the person in Minhas tarefas (ADR-0158), with its project, phase and the task it is in. */
export interface MyTaskEntry {
  item: ScheduleItem;
  phase: string;
  parent: string | null;
  project: { id: string; code: string; name: string };
}

/** How the list groups the tasks: by project, as it starts, or by date. */
export type MyTasksGrouping = 'project' | 'date';

/** The groups by date, in their order. */
export const dateGroups = ['late', 'today', 'thisWeek', 'nextWeek', 'later', 'done'] as const;

export type DateGroup = (typeof dateGroups)[number];

/** A row of the list: the line of a group (a project or a date) or a task. */
export type MyTaskRow =
  | { kind: 'project'; id: string; project: MyTaskEntry['project']; count: number }
  | { kind: 'date'; id: string; group: DateGroup; count: number }
  | { kind: 'task'; id: string; entry: MyTaskEntry };

export const isGroupRow = (row: MyTaskRow) => row.kind !== 'task';

/** The tasks of the answer, one after the other, by project and in the order the API gives them. */
export function entriesOf(tasks: MyTasks | undefined): MyTaskEntry[] {
  return (tasks?.projects ?? []).flatMap((project) =>
    project.tasks.map((task) => ({
      item: task.item,
      phase: task.phase,
      parent: task.parent,
      project: { id: project.id, code: project.code, name: project.name },
    })),
  );
}

/**
 * The group of a task by its end, against today of the installation, with the week from Monday to Sunday (ADR-0158):
 * the completed ones, the late ones (the end before today), today, the rest of this week, the next week and later.
 */
export function dateGroupOf(
  item: Pick<ScheduleItem, 'progress' | 'end' | 'late'>,
  today: DayValue,
): DateGroup {
  if (item.progress >= 100) {
    return 'done';
  }
  if (item.late || item.end < today) {
    return 'late';
  }
  if (item.end === today) {
    return 'today';
  }
  const sunday = addDays(weekStart(today), 6);
  if (item.end <= sunday) {
    return 'thisWeek';
  }
  return item.end <= addDays(sunday, 7) ? 'nextWeek' : 'later';
}

/** The order of two tasks: the open ones by the end, the completed ones by the completion, the latest first. */
function compare(one: MyTaskEntry, other: MyTaskEntry): number {
  const oneDone = one.item.progress >= 100;
  const otherDone = other.item.progress >= 100;
  if (oneDone !== otherDone) {
    return oneDone ? 1 : -1;
  }
  return oneDone
    ? (other.item.completedOn ?? other.item.end).localeCompare(one.item.completedOn ?? one.item.end)
    : one.item.end.localeCompare(other.item.end) ||
        one.project.code.localeCompare(other.project.code);
}

/** The rows of the list by project: each project, by the code, with its tasks in the order of the API. */
export function rowsByProject(entries: readonly MyTaskEntry[]): MyTaskRow[] {
  const projects = new Map<string, MyTaskEntry[]>();
  for (const entry of entries) {
    projects.set(entry.project.id, [...(projects.get(entry.project.id) ?? []), entry]);
  }
  return [...projects.values()].flatMap((tasks) => {
    const project = tasks[0]?.project;
    return project === undefined
      ? []
      : [
          { kind: 'project' as const, id: `project:${project.id}`, project, count: tasks.length },
          ...tasks.map((entry) => ({ kind: 'task' as const, id: entry.item.id, entry })),
        ];
  });
}

/** The rows of the list by date: each group with tasks, in the order of the groups, and the tasks by the end. */
export function rowsByDate(entries: readonly MyTaskEntry[], today: DayValue): MyTaskRow[] {
  return dateGroups.flatMap((group) => {
    const tasks = entries.filter((entry) => dateGroupOf(entry.item, today) === group).sort(compare);
    return tasks.length === 0
      ? []
      : [
          { kind: 'date' as const, id: `date:${group}`, group, count: tasks.length },
          ...tasks.map((entry) => ({ kind: 'task' as const, id: entry.item.id, entry })),
        ];
  });
}

/** The cards of the Quadro: each task with the project whose progress it changes and the code it shows. */
export function cardsOf(entries: readonly MyTaskEntry[]): BoardCard[] {
  return [...entries].sort(compare).map((entry) => ({
    item: entry.item,
    phase: { name: entry.phase },
    parent: entry.parent === null ? undefined : { name: entry.parent },
    projectId: entry.project.id,
    projectCode: entry.project.code,
  }));
}

/** How many tasks are open, and in how many projects. */
export function openCounts(entries: readonly MyTaskEntry[]): { tasks: number; projects: number } {
  const open = entries.filter((entry) => entry.item.progress < 100);
  return { tasks: open.length, projects: new Set(open.map((entry) => entry.project.id)).size };
}
