import type { TFunction } from 'i18next';
import type { Project, ProjectTaskHours, SessionUser, TaskHours } from '../api/client';
import { formatHours } from '../hours/hours';

const number = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 });

/** The hours of a task as the Lista shows them (ADR-0138): "52 de 40 h", or "12 h" without planned hours. */
export function taskHoursText(task: TaskHours, t: TFunction): string {
  return task.planned === null
    ? formatHours(task.logged)
    : t('projectHours.tasks.loggedOfPlanned', {
        logged: number.format(task.logged),
        planned: formatHours(task.planned),
      });
}

/** The balance of a task: "12 h acima", "Restam 2 h" or "Sem horas previstas" (ADR-0138). */
export function taskBalanceText(task: TaskHours, t: TFunction): string {
  const { balance } = task;
  if (balance.state === 'NoContract' || balance.balance === null) {
    return t('projectHours.tasks.noPlanned');
  }
  return balance.state === 'Over'
    ? t('projectHours.balance.over', { hours: formatHours(-balance.balance) })
    : t('projectHours.balance.left', { hours: formatHours(balance.balance) });
}

/** Whether the state of the hours of a task asks for attention in the Lista: over or near the planned hours. */
export function needsAttention(task: TaskHours): boolean {
  return task.balance.state === 'Over' || task.balance.state === 'NearLimit';
}

/** The hours of each task of a project by the id of the task; empty while they load or for whoever does not see them. */
export function taskHoursById(hours: ProjectTaskHours | undefined): ReadonlyMap<string, TaskHours> {
  return new Map((hours?.items ?? []).map((task) => [task.itemId, task]));
}

/**
 * Whether the person sees the hours of the tasks of the project (ADR-0144): the Administrator, and the manager of the
 * project. The API decides; this only keeps the screen from asking what would be refused with 403.
 */
export function seesTaskHours(project: Project, user: SessionUser | undefined): boolean {
  return (
    user !== undefined &&
    (user.role === 'Administrator' ||
      project.members.some((member) => member.id === user.id && member.role === 'Manager'))
  );
}
