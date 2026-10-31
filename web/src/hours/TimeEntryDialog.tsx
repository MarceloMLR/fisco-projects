import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import { useId } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type TimeEntry, timeEntryProblems } from '../api/client';
import { timeEntryProjectsQuery, timeEntryWorkQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { Select, type SelectOption } from '../components/Select';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { type DayValue, formatDayText, maskTypedDay, parseTypedDay } from '../format/dates';
import { problemType } from '../projects/projectForm';
import { hoursFieldText, parseHours } from './hours';
import { Button } from '../components/Button';

const errors = {
  dayInvalid: 'hours.errors.dayInvalid',
  dayFuture: 'hours.errors.dayFuture',
  hoursInvalid: 'hours.errors.hoursInvalid',
  projectRequired: 'hours.errors.projectRequired',
  descriptionRequired: 'hours.errors.descriptionRequired',
  workInvalid: 'hours.errors.workInvalid',
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

/** The task or the ticket of an entry, as the field keeps it: "task:{id}", "ticket:{id}" or empty for the project only. */
const workValue = (entry: TimeEntry | undefined) =>
  entry?.task != null
    ? `task:${entry.task.id}`
    : entry?.ticket != null
      ? `ticket:${entry.ticket.id}`
      : '';

/**
 * The rules of an entry of hours as the API has them (ADR-0128): the day typed as dd/mm/aaaa and not after today, the
 * hours as "1,5" or "1:30", in quarters of an hour up to 24, the project and the description up to 500 characters.
 */
function schemaOf(today: DayValue) {
  return z.object({
    day: z
      .string()
      .refine((day) => parseTypedDay(day) !== undefined, errors.dayInvalid)
      .refine((day) => (parseTypedDay(day) ?? '') <= today, errors.dayFuture),
    hours: z.string().refine((hours) => parseHours(hours) !== undefined, errors.hoursInvalid),
    projectId: z.string().min(1, errors.projectRequired),
    work: z.string(),
    description: z
      .string()
      .trim()
      .min(1, errors.descriptionRequired)
      .max(500, errors.descriptionRequired),
  });
}

type Values = z.input<ReturnType<typeof schemaOf>>;

/**
 * "Apontar horas" and "Corrigir horas" (canvas Horas do projeto, ADR-0128): the day and the hours side by side, the
 * project, the task or the ticket of the project, optional, and what was done. The projects are the open ones the
 * person logs hours in; the tasks and the tickets come with the project chosen.
 */
export function TimeEntryDialog({
  entry,
  today,
  day,
  onClose,
  onSaved,
}: {
  /** The entry to correct; none for a new one. */
  entry?: TimeEntry;
  /** Today in the time zone of the installation. */
  today: DayValue;
  /** The day a new entry starts with. */
  day: DayValue;
  onClose: () => void;
  /** The entry was saved, with the message and its day. */
  onSaved: (message: string, day: DayValue) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const { data: projects, isError } = useQuery(timeEntryProjectsQuery());
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    formState: { errors: fieldErrors },
  } = useForm<Values>({
    resolver: zodResolver(schemaOf(today)),
    defaultValues: {
      day: formatDayText(entry?.day ?? day),
      hours: entry === undefined ? '' : hoursFieldText(entry.hours),
      projectId: entry?.project.id ?? '',
      work: workValue(entry),
      description: entry?.description ?? '',
    },
  });
  const projectId = useWatch({ control, name: 'projectId' });
  const { data: work } = useQuery({ ...timeEntryWorkQuery(projectId), enabled: projectId !== '' });
  const errorText = (message: string | undefined) =>
    isHoursError(message) ? t(message) : undefined;

  const projectOptions: SelectOption<string>[] = [
    { value: '', label: t('hours.dialog.chooseProject'), disabled: true },
    ...(projects ?? []).map((project) => ({
      value: project.id,
      label: `${project.code} · ${project.name}`,
    })),
    ...(entry !== undefined && !(projects ?? []).some((project) => project.id === entry.project.id)
      ? [{ value: entry.project.id, label: `${entry.project.code} · ${entry.project.name}` }]
      : []),
  ];
  const keptTask =
    entry?.task != null &&
    entry.project.id === projectId &&
    !(work?.tasks ?? []).some((task) => task.id === entry.task?.id)
      ? [
          {
            value: `task:${entry.task.id}`,
            label: entry.task.name ?? t('hours.deletedTask'),
            detail: t('hours.dialog.task'),
          },
        ]
      : [];
  const workOptions: SelectOption<string>[] = [
    { value: '', label: t('hours.onlyProject') },
    ...(work?.tasks ?? []).map((task) => ({
      value: `task:${task.id}`,
      label: task.name,
      detail: t('hours.dialog.task'),
    })),
    ...keptTask,
    ...(work?.tickets ?? []).map((ticket) => ({
      value: `ticket:${ticket.id}`,
      label: `#${String(ticket.number)} ${ticket.title}`,
      detail: t(ticket.isClosed ? 'hours.dialog.closedTicket' : 'hours.dialog.ticket'),
    })),
  ];

  const save = handleSubmit(async (values) => {
    const chosenDay = parseTypedDay(values.day) ?? today;
    const body = {
      projectId: values.projectId,
      day: chosenDay,
      hours: parseHours(values.hours) ?? 0,
      description: values.description.trim(),
      scheduleItemId: values.work.startsWith('task:') ? values.work.slice('task:'.length) : null,
      ticketId: values.work.startsWith('ticket:') ? values.work.slice('ticket:'.length) : null,
    };
    try {
      const { response, error } =
        entry === undefined
          ? await api.POST('/api/time-entries', { body })
          : await api.PUT('/api/time-entries/{entryId}', {
              params: { path: { entryId: entry.id } },
              body,
            });
      if (response.ok) {
        onSaved(
          t(entry === undefined ? 'hours.messages.added' : 'hours.messages.saved'),
          chosenDay,
        );
        return;
      }
      if (response.status === 400) {
        const refused = refusedFields(error);
        const fields = [
          ['day', 'day', errors.dayInvalid],
          ['hours', 'hours', errors.hoursInvalid],
          ['projectId', 'projectId', errors.projectRequired],
          ['description', 'description', errors.descriptionRequired],
          ['scheduleItemId', 'work', errors.workInvalid],
          ['ticketId', 'work', errors.workInvalid],
        ] as const;
        for (const [field, name, message] of fields) {
          if (refused.includes(field)) {
            setError(name, { message });
          }
        }
        return;
      }
      setError('root', {
        message:
          response.status === 409 && problemType(error) === timeEntryProblems.projectClosed
            ? t('hours.messages.closed')
            : t('hours.messages.failed'),
      });
    } catch {
      setError('root', { message: t('hours.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(entry === undefined ? 'hours.dialog.newTitle' : 'hours.dialog.editTitle')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('hours.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(entry === undefined ? 'hours.dialog.add' : 'hours.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {isError && <Alert tone="danger">{t('hours.messages.failed')}</Alert>}
        {projects?.length === 0 && entry === undefined && (
          <Alert tone="info">{t('hours.messages.noProjects')}</Alert>
        )}
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
                label={t('hours.dialog.day')}
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
            label={t('hours.dialog.hours')}
            inputMode="decimal"
            autoComplete="off"
            maxLength={5}
            helper={t('hours.dialog.hoursHelp')}
            error={errorText(fieldErrors.hours?.message)}
            {...register('hours')}
          />
        </Box>
        <FormField
          label={t('hours.dialog.project')}
          error={errorText(fieldErrors.projectId?.message)}
          helpId={`${ids}-project-help`}
        >
          <Controller
            control={control}
            name="projectId"
            render={({ field }) => (
              <Select<string>
                label={t('hours.dialog.project')}
                value={field.value}
                options={projectOptions}
                invalid={fieldErrors.projectId !== undefined}
                describedBy={`${ids}-project-help`}
                onChange={(value) => {
                  field.onChange(value);
                  setValue('work', '');
                }}
              />
            )}
          />
        </FormField>
        <FormField
          label={t('hours.dialog.work')}
          error={errorText(fieldErrors.work?.message)}
          helpId={`${ids}-work-help`}
        >
          <Controller
            control={control}
            name="work"
            render={({ field }) => (
              <Select<string>
                label={t('hours.dialog.work')}
                value={field.value}
                options={workOptions}
                invalid={fieldErrors.work !== undefined}
                describedBy={`${ids}-work-help`}
                disabled={projectId === ''}
                onChange={field.onChange}
              />
            )}
          />
        </FormField>
        <TextArea
          label={t('hours.dialog.description')}
          rows={3}
          maxLength={510}
          error={errorText(fieldErrors.description?.message)}
          {...register('description')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
