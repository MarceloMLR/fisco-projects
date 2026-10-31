import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Project, projectProblems, type Schedule } from '../api/client';
import { queryKeys } from '../api/queries';
import { problemType } from './projectForm';
import { stepText } from './schedule';
import { failureOf } from './scheduleItemValues';
import { useNotify } from '../notices/notify';

/**
 * Undoes the last change of the schedule the person made, or redoes the last one undone (ADR-0091), and says what came
 * back, or why it could not.
 */
export function useScheduleStep(project: Project) {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const [pending, setPending] = useState(false);

  const run = useCallback(
    async (step: 'undo' | 'redo', schedule: Schedule) => {
      const asked = schedule[step];
      setPending(true);
      try {
        const { data, error, response } = await api.POST(
          step === 'undo' ? '/api/projects/{id}/schedule/undo' : '/api/projects/{id}/schedule/redo',
          { params: { path: { id: project.id } } },
        );
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        const done = data ?? asked;
        const what = done === null ? '' : stepText(done, t);
        if (response.ok) {
          const text = t(
            step === 'undo' ? 'projects.steps.messages.undone' : 'projects.steps.messages.redone',
            { what },
          );
          notify(text.charAt(0).toUpperCase() + text.slice(1));
        } else if (problemType(error) === projectProblems.scheduleUndoBlocked) {
          notify(
            t(
              step === 'undo'
                ? 'projects.steps.messages.undoBlocked'
                : 'projects.steps.messages.redoBlocked',
              { what },
            ),
            { tone: 'error' },
          );
        } else if (problemType(error) === projectProblems.scheduleNothingToUndo) {
          notify(t('projects.steps.messages.nothing'), { tone: 'error' });
        } else {
          notify(failureOf(error, response.status, t), { tone: 'error' });
        }
      } catch {
        notify(t('projects.schedule.messages.failed'), { tone: 'error' });
      } finally {
        setPending(false);
      }
    },
    [project.id, queryClient, notify, t],
  );

  return { run, pending };
}
