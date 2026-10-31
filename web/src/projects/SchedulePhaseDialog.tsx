import { zodResolver } from '@hookform/resolvers/zod';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import Box from '@mui/material/Box';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type Project, projectProblems, type SchedulePhase } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { invalidFields, problemType } from './projectForm';
import { Button } from '../components/Button';
import type { NoticeOptions } from '../notices/notify';

const errors = {
  nameRequired: 'projects.schedule.phaseDialog.nameRequired',
  nameTooLong: 'projects.schedule.phaseDialog.nameTooLong',
} as const;

type ErrorKey = (typeof errors)[keyof typeof errors];

const isErrorKey = (message: string | undefined): message is ErrorKey =>
  Object.values(errors).some((key) => key === message);

const form = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: errors.nameRequired })
    .max(150, { error: errors.nameTooLong }),
});

type FormValues = z.input<typeof form>;

/**
 * A phase of the schedule (ADR-0019): "Nova fase" includes one after the others; choosing a phase renames it or,
 * after a confirmation that says how many tasks and milestones go with it, removes it.
 */
export function SchedulePhaseDialog({
  project,
  phase,
  onClose,
  onDone,
}: {
  project: Project;
  /** The phase to rename or remove; none for a new one. */
  phase?: SchedulePhase;
  onClose: () => void;
  /** The change was saved, or refused, with the message to show. */
  onDone: (message: string, options?: NoticeOptions) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [removing, setRemoving] = useState(false);
  const [removingPending, setRemovingPending] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: { name: phase?.name ?? '' },
  });
  const refreshed = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  };
  const failure = (problem: unknown, status: number) =>
    status === 409 && problemType(problem) === projectProblems.closed
      ? t('projects.schedule.messages.closed')
      : t('projects.schedule.messages.failed');

  const save = handleSubmit(async ({ name }) => {
    const trimmed = name.trim();
    try {
      const { error, response } =
        phase === undefined
          ? await api.POST('/api/projects/{id}/schedule/phases', {
              params: { path: { id: project.id } },
              body: { name: trimmed },
            })
          : await api.PUT('/api/projects/{id}/schedule/phases/{phaseId}', {
              params: { path: { id: project.id, phaseId: phase.id } },
              body: { name: trimmed },
            });
      if (response.ok) {
        refreshed();
        onDone(
          t(
            phase === undefined
              ? 'projects.schedule.messages.phaseAdded'
              : 'projects.schedule.messages.phaseRenamed',
            { name: trimmed },
          ),
        );
        return;
      }
      if (invalidFields(error).includes('name')) {
        setError('name', { message: errors.nameRequired });
        return;
      }
      setError('root', { message: failure(error, response.status) });
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  const remove = async () => {
    if (phase === undefined) {
      return;
    }
    setRemovingPending(true);
    try {
      const { error, response } = await api.DELETE('/api/projects/{id}/schedule/phases/{phaseId}', {
        params: { path: { id: project.id, phaseId: phase.id } },
      });
      if (response.ok) {
        refreshed();
        onDone(t('projects.schedule.messages.phaseRemoved', { name: phase.name }));
        return;
      }
      onDone(failure(error, response.status), { tone: 'error' });
    } catch {
      onDone(t('projects.schedule.messages.failed'), { tone: 'error' });
    } finally {
      setRemovingPending(false);
    }
  };

  const errorText = (message: string | undefined) => (isErrorKey(message) ? t(message) : undefined);

  return (
    <>
      <Dialog
        open={!removing}
        title={t(
          phase === undefined
            ? 'projects.schedule.phaseDialog.newTitle'
            : 'projects.schedule.phaseDialog.editTitle',
        )}
        onClose={onClose}
        onSubmit={(event) => save(event)}
        actions={
          <>
            {phase !== undefined && (
              <Button
                variant="text"
                startIcon={<DeleteOutlined />}
                onClick={() => {
                  setRemoving(true);
                }}
                sx={{ marginRight: 'auto', color: 'var(--status-danger-text)' }}
              >
                {t('projects.schedule.phaseDialog.remove')}
              </Button>
            )}
            <Button variant="outlined" onClick={onClose}>
              {t('projects.schedule.phaseDialog.cancel')}
            </Button>
            <Button type="submit" variant="contained">
              {t(
                phase === undefined
                  ? 'projects.schedule.phaseDialog.create'
                  : 'projects.schedule.phaseDialog.save',
              )}
            </Button>
          </>
        }
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
          <Controller
            control={control}
            name="name"
            render={({ field }) => (
              <TextField
                label={t('projects.schedule.phaseDialog.name')}
                maxLength={150}
                error={errorText(fieldErrors.name?.message)}
                {...field}
              />
            )}
          />
          {fieldErrors.root?.message !== undefined && (
            <Alert tone="danger">{fieldErrors.root.message}</Alert>
          )}
        </Box>
      </Dialog>
      {phase !== undefined && (
        <Dialog
          open={removing}
          alert
          title={t('projects.schedule.removePhase.title')}
          onClose={() => {
            setRemoving(false);
          }}
          actions={
            <>
              <Button
                variant="outlined"
                onClick={() => {
                  setRemoving(false);
                }}
              >
                {t('projects.schedule.removePhase.back')}
              </Button>
              <Button
                variant="contained"
                color="error"
                loading={removingPending}
                onClick={() => remove()}
              >
                {t('projects.schedule.removePhase.confirm')}
              </Button>
            </>
          }
        >
          {phase.items.length === 0
            ? t('projects.schedule.removePhase.text', { name: phase.name })
            : t('projects.schedule.removePhase.withItems', {
                name: phase.name,
                count: phase.items.length,
              })}
        </Dialog>
      )}
    </>
  );
}
