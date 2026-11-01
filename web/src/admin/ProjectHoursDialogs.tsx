import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { type AdditionalHours, api } from '../api/client';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { type DayValue, formatDayText, maskTypedDay, parseTypedDay } from '../format/dates';
import { parseHoursUpTo, plainHoursText } from '../hours/hours';
import { Button } from '../components/Button';

/**
 * The most contracted hours of a project, the most of one addition and the most planned hours of a task, as the API
 * has them (ADR-0128, ADR-0138).
 */
const mostContracted = 99_999;
const mostAdditional = 9_999;
const mostPlanned = 9_999;

const errors = {
  contractedInvalid: 'projectHours.errors.contractedInvalid',
  plannedInvalid: 'projectHours.errors.plannedInvalid',
  dayInvalid: 'projectHours.errors.dayInvalid',
  dayFuture: 'projectHours.errors.dayFuture',
  hoursInvalid: 'projectHours.errors.hoursInvalid',
  reasonRequired: 'projectHours.errors.reasonRequired',
} as const;

type HoursError = (typeof errors)[keyof typeof errors];
const isHoursError = (message: string | undefined): message is HoursError =>
  Object.values(errors).some((error) => error === message);

/** The fields the API refused, from the errors of its validation problem. */
function refusedFields(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/** The project a dialog is about, as its subtitle shows it. */
export interface HoursProject {
  id: string;
  code: string;
  name: string;
}

const contractedSchema = z.object({
  hours: z
    .string()
    .refine(
      (hours) => hours.trim() === '' || parseHoursUpTo(hours, mostContracted) !== undefined,
      errors.contractedInvalid,
    ),
});

/**
 * "Alterar" the contracted hours of a project (canvas Horas do projeto, ADR-0128): the hours sold with it, or the
 * field left empty, which takes them out, and the project goes without a balance.
 */
export function ContractedHoursDialog({
  project,
  contracted,
  onClose,
  onSaved,
}: {
  project: HoursProject;
  /** The hours now; null without them. */
  contracted: number | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<z.input<typeof contractedSchema>>({
    resolver: zodResolver(contractedSchema),
    defaultValues: { hours: contracted === null ? '' : plainHoursText(contracted) },
  });

  const save = handleSubmit(async ({ hours }) => {
    const value = hours.trim() === '' ? null : (parseHoursUpTo(hours, mostContracted) ?? null);
    try {
      const { response } = await api.PUT('/api/admin/project-hours/{projectId}/contracted', {
        params: { path: { projectId: project.id } },
        body: { hours: value },
      });
      if (response.ok) {
        onSaved(
          t(
            value === null
              ? 'projectHours.messages.contractedRemoved'
              : 'projectHours.messages.contractedSaved',
          ),
        );
        return;
      }
      if (response.status === 400) {
        setError('hours', { message: errors.contractedInvalid });
        return;
      }
      setError('root', { message: t('projectHours.messages.failed') });
    } catch {
      setError('root', { message: t('projectHours.messages.failed') });
    }
  });

  const error = fieldErrors.hours?.message;
  return (
    <Dialog
      open
      title={t('projectHours.contractedDialog.title')}
      subtitle={t('projectHours.entries.subtitle', { code: project.code, name: project.name })}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projectHours.contractedDialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projectHours.contractedDialog.save')}
          </Button>
        </>
      }
    >
      <TextField
        label={t('projectHours.contractedDialog.hours')}
        inputMode="decimal"
        autoComplete="off"
        maxLength={8}
        helper={t('projectHours.contractedDialog.help')}
        error={isHoursError(error) ? t(error) : undefined}
        {...register('hours')}
      />
      {fieldErrors.root?.message !== undefined && (
        <Alert tone="danger">{fieldErrors.root.message}</Alert>
      )}
    </Dialog>
  );
}

const plannedSchema = z.object({
  hours: z
    .string()
    .refine(
      (hours) => hours.trim() === '' || parseHoursUpTo(hours, mostPlanned) !== undefined,
      errors.plannedInvalid,
    ),
});

/**
 * "Alterar" the planned hours of a task of the schedule, only for the Administrator (ADR-0138): the hours the task
 * should take, or the field left empty, which takes them out.
 */
export function PlannedHoursDialog({
  projectId,
  task,
  planned,
  onClose,
  onSaved,
}: {
  projectId: string;
  /** The task, by its id in the schedule and its name. */
  task: { id: string; name: string };
  /** The hours now; null without them. */
  planned: number | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<z.input<typeof plannedSchema>>({
    resolver: zodResolver(plannedSchema),
    defaultValues: { hours: planned === null ? '' : plainHoursText(planned) },
  });

  const save = handleSubmit(async ({ hours }) => {
    const value = hours.trim() === '' ? null : (parseHoursUpTo(hours, mostPlanned) ?? null);
    try {
      const { response } = await api.PUT('/api/admin/project-hours/{projectId}/tasks/{itemId}', {
        params: { path: { projectId, itemId: task.id } },
        body: { hours: value },
      });
      if (response.ok) {
        onSaved(
          t(
            value === null
              ? 'projectHours.messages.plannedRemoved'
              : 'projectHours.messages.plannedSaved',
            { name: task.name },
          ),
        );
        return;
      }
      if (response.status === 400) {
        setError('hours', { message: errors.plannedInvalid });
        return;
      }
      setError('root', {
        message: t(
          response.status === 404
            ? 'projectHours.messages.taskGone'
            : 'projectHours.messages.failed',
        ),
      });
    } catch {
      setError('root', { message: t('projectHours.messages.failed') });
    }
  });

  const error = fieldErrors.hours?.message;
  return (
    <Dialog
      open
      title={t('projectHours.plannedDialog.title')}
      subtitle={task.name}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projectHours.plannedDialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projectHours.plannedDialog.save')}
          </Button>
        </>
      }
    >
      <TextField
        label={t('projectHours.plannedDialog.hours')}
        inputMode="decimal"
        autoComplete="off"
        maxLength={7}
        helper={t('projectHours.plannedDialog.help')}
        error={isHoursError(error) ? t(error) : undefined}
        {...register('hours')}
      />
      {fieldErrors.root?.message !== undefined && (
        <Alert tone="danger">{fieldErrors.root.message}</Alert>
      )}
    </Dialog>
  );
}

function additionalSchema(today: DayValue) {
  return z.object({
    day: z
      .string()
      .refine((day) => parseTypedDay(day) !== undefined, errors.dayInvalid)
      .refine((day) => (parseTypedDay(day) ?? '') <= today, errors.dayFuture),
    hours: z
      .string()
      .refine((hours) => parseHoursUpTo(hours, mostAdditional) !== undefined, errors.hoursInvalid),
    reason: z.string().trim().min(1, errors.reasonRequired).max(500, errors.reasonRequired),
  });
}

type AdditionalValues = z.input<ReturnType<typeof additionalSchema>>;

/**
 * "Incluir horas adicionais" and "Corrigir horas adicionais" (canvas Horas do projeto, AdminAdicional, ADR-0128): the
 * day, not after today, and the hours side by side, and the reason, the request out of the scope that brought them.
 */
export function AdditionalHoursDialog({
  project,
  additional,
  today,
  onClose,
  onSaved,
}: {
  project: HoursProject;
  /** The additional hours to correct; none for new ones. */
  additional?: AdditionalHours;
  /** Today in the time zone of the installation. */
  today: DayValue;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<AdditionalValues>({
    resolver: zodResolver(additionalSchema(today)),
    defaultValues: {
      day: formatDayText(additional?.day ?? today),
      hours: additional === undefined ? '' : plainHoursText(additional.hours),
      reason: additional?.reason ?? '',
    },
  });
  const errorText = (message: string | undefined) =>
    isHoursError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const body = {
      day: parseTypedDay(values.day) ?? today,
      hours: parseHoursUpTo(values.hours, mostAdditional) ?? 0,
      reason: values.reason.trim(),
    };
    try {
      const { response, error } =
        additional === undefined
          ? await api.POST('/api/admin/project-hours/{projectId}/additional', {
              params: { path: { projectId: project.id } },
              body,
            })
          : await api.PUT('/api/admin/project-hours/{projectId}/additional/{id}', {
              params: { path: { projectId: project.id, id: additional.id } },
              body,
            });
      if (response.ok) {
        onSaved(
          t(
            additional === undefined
              ? 'projectHours.messages.added'
              : 'projectHours.messages.saved',
          ),
        );
        return;
      }
      if (response.status === 400) {
        const refused = refusedFields(error);
        const fields = [
          ['day', errors.dayInvalid],
          ['hours', errors.hoursInvalid],
          ['reason', errors.reasonRequired],
        ] as const;
        for (const [field, message] of fields) {
          if (refused.includes(field)) {
            setError(field, { message });
          }
        }
        return;
      }
      setError('root', { message: t('projectHours.messages.failed') });
    } catch {
      setError('root', { message: t('projectHours.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(
        additional === undefined
          ? 'projectHours.additionalDialog.newTitle'
          : 'projectHours.additionalDialog.editTitle',
      )}
      subtitle={t('projectHours.entries.subtitle', { code: project.code, name: project.name })}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projectHours.additionalDialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(
              additional === undefined
                ? 'projectHours.additionalDialog.add'
                : 'projectHours.additionalDialog.save',
            )}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
            gap: 'var(--space-4)',
          }}
        >
          <Controller
            control={control}
            name="day"
            render={({ field }) => (
              <TextField
                label={t('projectHours.additionalDialog.day')}
                inputMode="numeric"
                autoComplete="off"
                maxLength={10}
                error={errorText(fieldErrors.day?.message)}
                name={field.name}
                ref={field.ref}
                value={field.value}
                onBlur={field.onBlur}
                onChange={(event) => {
                  field.onChange(maskTypedDay(event.target.value));
                }}
              />
            )}
          />
          <TextField
            label={t('projectHours.additionalDialog.hours')}
            inputMode="decimal"
            autoComplete="off"
            maxLength={7}
            error={errorText(fieldErrors.hours?.message)}
            {...register('hours')}
          />
        </Box>
        <TextArea
          label={t('projectHours.additionalDialog.reason')}
          rows={3}
          maxLength={510}
          error={errorText(fieldErrors.reason?.message)}
          {...register('reason')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
