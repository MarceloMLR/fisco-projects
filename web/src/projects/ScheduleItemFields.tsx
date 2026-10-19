import Box from '@mui/material/Box';
import { useId } from 'react';
import { type Control, Controller, type FieldErrors } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { Project, Schedule, ScheduleItemKind } from '../api/client';
import { FormField } from '../components/FormField';
import { MultiSelect } from '../components/MultiSelect';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { formatDayText, maskTypedDay } from '../format/dates';
import { itemsAfter, itemsOf } from './schedule';
import { isScheduleItemError, noOwner, type ScheduleItemValues } from './scheduleItemValues';

/** A row of fields side by side, with the columns given; one under the other on a narrow screen. */
const rowSx = (columns: string) =>
  ({
    display: 'grid',
    gridTemplateColumns: { xs: '1fr', sm: columns },
    gap: 'var(--space-4)',
    alignItems: 'start',
  }) as const;

/**
 * The fields of a task or milestone, the same in "Nova tarefa" and in the details: the name, the phase, the start
 * typed as dd/mm/aaaa and, for a task, the duration in business days and the owner (a person of the project who is not
 * a viewer, ADR-0043); and the items it depends on, without the ones that already depend on it, which would make a
 * cycle (ADR-0019). The end is computed by the API, with the calendar of the installation; the progress changes by its
 * own action (ADR-0086).
 */
export function ScheduleItemFields({
  control,
  errors,
  kind,
  schedule,
  project,
  itemId,
  endsOn,
}: {
  control: Control<ScheduleItemValues>;
  errors: FieldErrors<ScheduleItemValues>;
  kind: ScheduleItemKind;
  schedule: Schedule;
  project: Project;
  /** The item being changed; none for a new one. */
  itemId?: string;
  /** The end the API computed for the item as saved. */
  endsOn?: string;
}) {
  const { t } = useTranslation();
  const phaseHelpId = useId();
  const ownerHelpId = useId();
  const task = kind === 'Task';
  const errorText = (message: string | undefined) =>
    isScheduleItemError(message) ? t(message) : undefined;
  const phaseOptions = schedule.phases.map((phase) => ({ value: phase.id, label: phase.name }));
  const ownerOptions = [
    { value: noOwner, label: t('projects.schedule.noOwner') },
    ...project.members
      .filter((member) => member.role !== 'Viewer')
      .toSorted((one, other) => one.name.localeCompare(other.name, 'pt-BR'))
      .map((member) => ({ value: member.id, label: member.name })),
  ];
  const after = itemId === undefined ? new Set<string>() : itemsAfter(itemId, schedule);
  const phaseNames = new Map(schedule.phases.map((phase) => [phase.id, phase.name]));
  const predecessorOptions = itemsOf(schedule)
    .filter((item) => item.id !== itemId && !after.has(item.id))
    .map((item) => ({ value: item.id, label: item.name, group: phaseNames.get(item.phaseId) }));
  const durationHelp =
    endsOn === undefined
      ? t('projects.schedule.form.durationHelp')
      : t('projects.schedule.form.endsOn', { day: formatDayText(endsOn) });

  return (
    <>
      <Controller
        control={control}
        name="name"
        render={({ field }) => (
          <TextField
            label={t('projects.schedule.form.name')}
            maxLength={200}
            error={errorText(errors.name?.message)}
            {...field}
          />
        )}
      />
      <Controller
        control={control}
        name="phaseId"
        render={({ field }) => {
          const error = errorText(errors.phaseId?.message);
          return (
            <FormField label={t('projects.schedule.form.phase')} error={error} helpId={phaseHelpId}>
              <Select
                label={t('projects.schedule.form.phase')}
                value={field.value}
                options={phaseOptions}
                onChange={field.onChange}
                invalid={error !== undefined}
                describedBy={error === undefined ? undefined : phaseHelpId}
              />
            </FormField>
          );
        }}
      />
      <Box sx={rowSx('1fr 1fr')}>
        <Controller
          control={control}
          name="start"
          render={({ field }) => (
            <TextField
              label={t(task ? 'projects.schedule.form.start' : 'projects.schedule.form.day')}
              placeholder={t('projects.schedule.form.dayPlaceholder')}
              inputMode="numeric"
              error={errorText(errors.start?.message)}
              {...field}
              onChange={(event) => {
                field.onChange(maskTypedDay(event.target.value));
              }}
            />
          )}
        />
        {task && (
          <Controller
            control={control}
            name="duration"
            render={({ field }) => (
              <TextField
                label={t('projects.schedule.form.duration')}
                inputMode="numeric"
                error={errorText(errors.duration?.message)}
                helper={durationHelp}
                {...field}
              />
            )}
          />
        )}
      </Box>
      {task && (
        <Controller
          control={control}
          name="ownerId"
          render={({ field }) => {
            const error = errorText(errors.ownerId?.message);
            return (
              <FormField
                label={t('projects.schedule.form.owner')}
                optional={t('projects.schedule.form.optional')}
                error={error}
                helpId={ownerHelpId}
              >
                <Select
                  label={t('projects.schedule.form.owner')}
                  value={field.value}
                  options={ownerOptions}
                  onChange={field.onChange}
                  invalid={error !== undefined}
                  describedBy={error === undefined ? undefined : ownerHelpId}
                />
              </FormField>
            );
          }}
        />
      )}
      <Controller
        control={control}
        name="predecessorIds"
        render={({ field }) => (
          <MultiSelect
            label={t('projects.schedule.form.dependsOn')}
            optional={t('projects.schedule.form.optional')}
            value={field.value}
            options={predecessorOptions}
            onChange={field.onChange}
            placeholder={t('projects.schedule.form.dependsOnPlaceholder')}
            noOptionsText={t('projects.schedule.form.noItems')}
            helper={t('projects.schedule.form.dependsOnHelp')}
            error={errorText(errors.predecessorIds?.message)}
          />
        )}
      />
    </>
  );
}
