import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys, taskEntriesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { formatHours } from '../hours/hours';
import { TimeEntryTable } from '../hours/TimeEntryTable';
import { useNotify } from '../notices/notify';

const noEntries: never[] = [];

/**
 * "Ver apontamentos" of the hours of a task (ADR-0144): the entries of every person in the task, grouped by day, the
 * newest first, with the person, what was done and the hours of each, and the total of the task with how many people
 * logged it. The "⋮" is only on the entries the person may correct, by the rules of an entry (ADR-0128).
 */
export function TaskEntriesDialog({
  projectId,
  task,
  onClose,
}: {
  projectId: string;
  /** The task, by its id in the schedule and its name. */
  task: { id: string; name: string };
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data, isFetching, error } = useQuery(taskEntriesQuery(projectId, task.id));
  const entries = data?.items ?? noEntries;
  const people = new Set(entries.map((entry) => entry.person.id)).size;

  return (
    <Dialog
      open
      size="lg"
      title={t('projectHours.tasks.entries.title')}
      subtitle={task.name}
      onClose={onClose}
      actions={
        <Button variant="outlined" onClick={onClose}>
          {t('dialog.close')}
        </Button>
      }
    >
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          height: { xs: '60vh', md: 440 },
          minHeight: 240,
        }}
      >
        {error === null ? (
          <TimeEntryTable
            label={t('projectHours.tasks.entries.table', { name: task.name })}
            entries={entries}
            first="person"
            showWork={false}
            loading={isFetching}
            emptyLabel={t('projectHours.tasks.entries.empty')}
            footer={t('projectHours.tasks.entries.total', {
              hours: formatHours(data?.total ?? 0),
              count: people,
            })}
            onChanged={(message) => {
              // The hours of the tasks, these entries, Minhas horas and the audit change with the entry.
              void queryClient.invalidateQueries({ queryKey: queryKeys.projectHours });
              void queryClient.invalidateQueries({ queryKey: queryKeys.timeEntries });
              void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
              notify(message);
            }}
          />
        ) : (
          <Alert tone="danger">{t('projectHours.tasks.entries.loadFailed')}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
