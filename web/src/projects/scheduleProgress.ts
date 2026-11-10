import type { TFunction } from 'i18next';
import { api } from '../api/client';
import { failureOf } from './scheduleItemValues';

/**
 * Saves the progress of a task, as the details, the queue of Meu trabalho and the Quadro do (ADR-0086, ADR-0155):
 * nothing when it worked, the text of the failure otherwise.
 */
export async function saveTaskProgress(
  projectId: string,
  itemId: string,
  progress: number,
  t: TFunction,
): Promise<string | undefined> {
  try {
    const { error, response } = await api.PUT(
      '/api/projects/{id}/schedule/items/{itemId}/progress',
      { params: { path: { id: projectId, itemId } }, body: { progress } },
    );
    return response.ok ? undefined : failureOf(error, response.status, t);
  } catch {
    return t('projects.schedule.messages.failed');
  }
}

/**
 * Takes a task to 100% ("Concluir", ADR-0086), as the details of the task and the queue of Meu trabalho do it: nothing
 * when it worked, the text of the failure otherwise.
 */
export function completeTask(
  projectId: string,
  itemId: string,
  t: TFunction,
): Promise<string | undefined> {
  return saveTaskProgress(projectId, itemId, 100, t);
}
