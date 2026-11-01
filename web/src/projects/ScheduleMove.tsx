import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useQueryClient } from '@tanstack/react-query';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type Project, type Schedule } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { SegmentedControl } from '../components/SegmentedControl';
import { TextField } from '../components/TextField';
import { movableCount } from './schedule';
import { failureOf } from './scheduleItemValues';

/** The most business days of a move, as the API takes them (ADR-0140). */
const maxMove = 999;

const daysInvalid = 'projects.move.errors.daysInvalid';

type Direction = 'later' | 'earlier';

const schema = z.object({
  direction: z.enum(['later', 'earlier']),
  days: z
    .string()
    .trim()
    .refine((value) => /^\d{1,3}$/.test(value) && Number(value) >= 1 && Number(value) <= maxMove, {
      error: daysInvalid,
    }),
});

type Values = z.input<typeof schema>;

/**
 * "Mover o cronograma" (ADR-0140): the direction, the business days and what moves; the open tasks and milestones go
 * the business days given, and the done ones, the deadlines and the baseline stay. One change for the undo.
 */
export function MoveScheduleDialog({
  project,
  schedule,
  onClose,
  onMoved,
}: {
  project: Project;
  schedule: Schedule;
  onClose: () => void;
  onMoved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { direction: 'later', days: '' },
  });
  const direction = useWatch({ control, name: 'direction' });
  const typed = useWatch({ control, name: 'days' }).trim();
  const count = movableCount(schedule);

  const move = handleSubmit(async (values) => {
    const days = Number(values.days.trim()) * (values.direction === 'later' ? 1 : -1);
    try {
      const { data, error, response } = await api.POST('/api/projects/{id}/schedule/move', {
        params: { path: { id: project.id } },
        body: { days },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onMoved(t('projects.move.messages.moved', { count: data?.moved ?? 0 }));
        return;
      }
      if (response.status === 400) {
        setError('days', { message: daysInvalid });
        return;
      }
      setError('root', { message: failureOf(error, response.status, t) });
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  const directions: { value: Direction; label: string }[] = [
    { value: 'later', label: t('projects.move.later') },
    { value: 'earlier', label: t('projects.move.earlier') },
  ];
  const days = /^\d{1,3}$/.test(typed) ? Number(typed) : undefined;
  return (
    <Dialog
      open
      title={t('projects.move.title')}
      onClose={onClose}
      onSubmit={(event) => move(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.move.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projects.move.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Controller
          control={control}
          name="direction"
          render={({ field }) => (
            <FormField label={t('projects.move.direction')}>
              <SegmentedControl
                label={t('projects.move.direction')}
                value={field.value}
                options={directions}
                onChange={field.onChange}
              />
            </FormField>
          )}
        />
        <Controller
          control={control}
          name="days"
          render={({ field }) => (
            <TextField
              label={t('projects.move.days')}
              inputMode="numeric"
              autoComplete="off"
              maxLength={3}
              width={160}
              error={errors.days?.message === daysInvalid ? t(daysInvalid) : undefined}
              helper={t('projects.move.daysHelp')}
              {...field}
            />
          )}
        />
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {days === undefined || days < 1 || days > maxMove
            ? t('projects.move.whatMoves', { count })
            : t(`projects.move.summary.${direction}`, {
                count,
                days: t('projects.move.businessDays', { count: days }),
              })}
        </Box>
        <Box
          component="p"
          sx={{ margin: 0, fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
        >
          {t('projects.move.stays')}
        </Box>
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}
