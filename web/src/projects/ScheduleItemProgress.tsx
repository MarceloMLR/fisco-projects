import { zodResolver } from '@hookform/resolvers/zod';
import CheckOutlined from '@mui/icons-material/CheckOutlined';
import Box from '@mui/material/Box';
import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useId, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { api, type Project, type ScheduleItem } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { ProgressBar } from '../components/ProgressBar';
import { TextField } from '../components/TextField';
import { formatDayText } from '../format/dates';
import { invalidFields } from './projectForm';
import {
  failureOf,
  isScheduleItemError,
  progressSchema,
  type ProgressValues,
  scheduleItemErrors,
} from './scheduleItemValues';
import { completeTask } from './scheduleProgress';
import { MilestoneStatus } from './ScheduleStatus';
import { Button } from '../components/Button';

/** A part of the details of a task or milestone, with its heading. */
export function DetailsSection({ title, children }: { title: string; children: ReactNode }) {
  const headingId = useId();
  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-start',
        gap: 'var(--space-2)',
        paddingBottom: 'var(--space-4)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <Box
        component="h3"
        id={headingId}
        sx={{
          margin: 0,
          fontSize: 13,
          lineHeight: '18px',
          fontWeight: 500,
          color: 'var(--text-secondary)',
        }}
      >
        {title}
      </Box>
      {children}
    </Box>
  );
}

/** Invalidates what a change of the schedule changes: the projects (with the schedule) and the audit. */
function useRefresh() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  };
}

/**
 * The progress of a task in its details (ADR-0020, ADR-0086): the bar, the day it was completed and, for who may
 * update it (the API says so for each task, ADR-0043), "Iniciar" at 0%, which asks for the percentage, "Atualizar
 * progresso" above 0% and "Concluir" below 100%, saved at once.
 */
export function TaskProgress({
  project,
  item,
  onDone,
  subtasks = 0,
}: {
  project: Project;
  item: ScheduleItem;
  onDone: (message: string) => void;
  /** How many subtasks the task has: its progress is computed from them, and nobody updates it (ADR-0088). */
  subtasks?: number;
}) {
  const { t } = useTranslation();
  const refresh = useRefresh();
  const [asking, setAsking] = useState(false);
  const [pending, setPending] = useState(false);
  const [failure, setFailure] = useState<string>();

  const complete = async () => {
    setPending(true);
    setFailure(undefined);
    const failed = await completeTask(project.id, item.id, t);
    setPending(false);
    if (failed === undefined) {
      refresh();
      onDone(t('projects.schedule.messages.completed', { name: item.name }));
      return;
    }
    setFailure(failed);
  };

  return (
    <DetailsSection title={t('projects.schedule.details.progress')}>
      <ProgressBar
        value={item.progress}
        label={t('projects.schedule.itemProgress', { name: item.name })}
        width={240}
      />
      {item.completedOn !== null && (
        <DueDate state="met">
          {t('projects.schedule.completedOn', { day: formatDayText(item.completedOn) })}
        </DueDate>
      )}
      {subtasks > 0 && (
        <Box
          component="span"
          sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
        >
          {t('projects.schedule.computed', { count: subtasks })}
        </Box>
      )}
      {item.canUpdateProgress && (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)' }}>
          <Button
            variant="outlined"
            size="small"
            onClick={() => {
              setAsking(true);
            }}
          >
            {t(
              item.progress === 0
                ? 'projects.schedule.progressActions.start'
                : 'projects.schedule.progressActions.update',
            )}
          </Button>
          {item.progress < 100 && (
            <Button
              variant="outlined"
              size="small"
              startIcon={<CheckOutlined />}
              loading={pending}
              onClick={() => complete()}
            >
              {t('projects.schedule.progressActions.complete')}
            </Button>
          )}
        </Box>
      )}
      {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      {asking && (
        <ProgressDialog
          projectId={project.id}
          item={item}
          onClose={() => {
            setAsking(false);
          }}
          onSaved={(message) => {
            setAsking(false);
            refresh();
            onDone(message);
          }}
        />
      )}
    </DetailsSection>
  );
}

/**
 * The percentage of a task, typed: empty when the task starts ("Iniciar", ADR-0086), the current one when it is
 * updated; in the details of the task and in the queue of Meu trabalho.
 */
export function ProgressDialog({
  projectId,
  item,
  onClose,
  onSaved,
}: {
  projectId: string;
  item: Pick<ScheduleItem, 'id' | 'name' | 'progress'>;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const starting = item.progress === 0;
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ProgressValues>({
    resolver: zodResolver(progressSchema),
    defaultValues: { progress: starting ? '' : String(item.progress) },
  });
  const errorText = (message: string | undefined) =>
    isScheduleItemError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const progress = Number(values.progress.trim());
    try {
      const { error, response } = await api.PUT(
        '/api/projects/{id}/schedule/items/{itemId}/progress',
        { params: { path: { id: projectId, itemId: item.id } }, body: { progress } },
      );
      if (response.ok) {
        onSaved(t('projects.schedule.messages.progressUpdated', { name: item.name, progress }));
        return;
      }
      if (invalidFields(error).includes('progress')) {
        setError('progress', { message: scheduleItemErrors.progressInvalid });
        return;
      }
      setError('root', { message: failureOf(error, response.status, t) });
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(
        starting
          ? 'projects.schedule.progressDialog.startTitle'
          : 'projects.schedule.progressDialog.updateTitle',
      )}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.schedule.form.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projects.schedule.form.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Controller
          control={control}
          name="progress"
          render={({ field }) => (
            <TextField
              label={t('projects.schedule.form.progress')}
              inputMode="numeric"
              autoFocus
              error={errorText(errors.progress?.message)}
              helper={t('projects.schedule.progressDialog.help', { name: item.name })}
              {...field}
            />
          )}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}

/**
 * The state of a milestone in its details (ADR-0086): reached on a day, pending or late, and, for who edits the
 * schedule (ADR-0043), "Marcar como atingido" or, when reached, "Desmarcar", saved at once.
 */
export function MilestoneState({
  project,
  item,
  canEdit,
  onDone,
}: {
  project: Project;
  item: ScheduleItem;
  canEdit: boolean;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const refresh = useRefresh();
  const [pending, setPending] = useState(false);
  const [failure, setFailure] = useState<string>();
  const reached = item.reachedOn !== null;

  const toggle = async () => {
    setPending(true);
    setFailure(undefined);
    try {
      const { error, response } = await api.PUT(
        '/api/projects/{id}/schedule/items/{itemId}/reached',
        { params: { path: { id: project.id, itemId: item.id } }, body: { reached: !reached } },
      );
      if (response.ok) {
        refresh();
        onDone(
          t(
            reached ? 'projects.schedule.messages.unmarked' : 'projects.schedule.messages.reached',
            {
              name: item.name,
            },
          ),
        );
        return;
      }
      setFailure(failureOf(error, response.status, t));
    } catch {
      setFailure(t('projects.schedule.messages.failed'));
    } finally {
      setPending(false);
    }
  };

  return (
    <DetailsSection title={t('projects.schedule.details.state')}>
      <MilestoneStatus item={item} withYear />
      {canEdit && (
        <Button
          variant="outlined"
          size="small"
          startIcon={reached ? undefined : <CheckOutlined />}
          loading={pending}
          onClick={() => toggle()}
        >
          {t(
            reached
              ? 'projects.schedule.milestoneActions.unmark'
              : 'projects.schedule.milestoneActions.markReached',
          )}
        </Button>
      )}
      {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
    </DetailsSection>
  );
}
