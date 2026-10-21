import type { TFunction } from 'i18next';
import { api } from '../api/client';
import { failureOf } from './scheduleItemValues';

/**
 * Takes a task to 100% ("Concluir", ADR-0086), as the details of the task and the queue of Meu trabalho do it: nothing
 * when it worked, the text of the failure otherwise.
 */
export async function completeTask(
  projectId: string,
  itemId: string,
  t: TFunction,
): Promise<string | undefined> {
  try {
    const { error, response } = await api.PUT(
      '/api/projects/{id}/schedule/items/{itemId}/progress',
      { params: { path: { id: projectId, itemId } }, body: { progress: 100 } },
    );
    return response.ok ? undefined : failureOf(error, response.status, t);
  } catch {
    return t('projects.schedule.messages.failed');
  }
}
