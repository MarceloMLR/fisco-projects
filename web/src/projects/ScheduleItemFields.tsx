import Box from '@mui/material/Box';
import { useId } from 'react';
import {
  type Control,
  Controller,
  type FieldErrors,
  useController,
  useWatch,
} from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { Project, Schedule, ScheduleItemKind } from '../api/client';
import { FormField } from '../components/FormField';
import { MultiSelect } from '../components/MultiSelect';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { formatDayText, maskTypedDay } from '../format/dates';
import { itemsAfter, itemsOf } from './schedule';
import {
  isScheduleItemError,
  noOwner,
  noParent,
  type ScheduleItemValues,
} from './scheduleItemValues';

/** A row of fields side by side, with the columns given; one under the other on a narrow screen. */
const rowSx = (columns: string) =>
  ({
    display: 'grid',
    gridTemplateColumns: { xs: '1fr', sm: columns },
    gap: 'var(--space-4)',
    alignItems: 'start',
  }) as const;

/**
 * The fields of a task or milestone, the same in "Nova tarefa" and in the details: the name, the phase, the task it is
 * in ("Dentro de", a task right in the phase, ADR-0088), the start typed as dd/mm/aaaa and, for a task, the duration in
 * business days and the owner (a person of the project who is not a viewer, ADR-0043); and the items it depends on,
 * without the ones that already depend on it, which would make a cycle (ADR-0019), nor the tasks with subtasks. The end
 * is computed by the API, with the calendar of the installation; the progress changes by its own action (ADR-0086). A
 * task with subtasks (`summary`) has only the name, the phase and the owner: the rest comes from its subtasks.
 */
export function ScheduleItemFields({
  control,
  errors,
  kind,
  schedule,
  project,
  itemId,
  endsOn,
  summary = false,
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
  /** The item has subtasks, and its dates and progress come from them (ADR-0088). */
  summary?: boolean;
}) {
  const { t } = useTranslation();
  const phaseHelpId = useId();
  const ownerHelpId = useId();
  const parentHelpId = useId();
  const { field: parentField } = useController({ control, name: 'parentId' });
  const phaseId = useWatch({ control, name: 'phaseId' });
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
  const items = itemsOf(schedule);
  // A task right in a phase holds subtasks, one level only (ADR-0088).
  const parentsIn = (phase: string) =>
    items.filter(
      (item) =>
        item.phaseId === phase &&
        item.kind === 'Task' &&
        item.parentId === null &&
        item.id !== itemId,
    );
  const parentOptions = [
    { value: noParent, label: t('projects.schedule.form.inPhase') },
    ...parentsIn(phaseId).map((item) => ({ value: item.id, label: item.name })),
  ];
  const parent = items.find((item) => item.id === parentField.value);
  const current = items.find((item) => item.id === itemId);
  const becomesSummary =
    parent !== undefined && !parent.hasSubtasks && parent.id !== current?.parentId;
  const after = itemId === undefined ? new Set<string>() : itemsAfter(itemId, schedule);
  const phaseNames = new Map(schedule.phases.map((phase) => [phase.id, phase.name]));
  // Only the items with dates of their own have dependencies, never the task the item goes into (ADR-0088).
  const predecessorOptions = items
    .filter(
      (item) =>
        item.id !== itemId && !after.has(item.id) && !item.hasSubtasks && item.id !== parent?.id,
    )
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
                onChange={(value) => {
                  field.onChange(value);
                  // A subtask is in the phase of its task: another phase starts right in it.
                  if (!parentsIn(value).some((item) => item.id === parentField.value)) {
                    parentField.onChange(noParent);
                  }
                }}
                invalid={error !== undefined}
                describedBy={error === undefined ? undefined : phaseHelpId}
              />
            </FormField>
          );
        }}
      />
      {!summary && (
        <FormField
          label={t('projects.schedule.form.parent')}
          optional={t('projects.schedule.form.optional')}
          error={errorText(errors.parentId?.message)}
          helper={
            becomesSummary
              ? t('projects.schedule.form.becomesSummary', { name: parent.name })
              : t('projects.schedule.form.parentHelp')
          }
          helpId={parentHelpId}
        >
          <Select
            label={t('projects.schedule.form.parent')}
            value={
              parentOptions.some((option) => option.value === parentField.value)
                ? parentField.value
                : noParent
            }
            options={parentOptions}
            onChange={parentField.onChange}
            invalid={errors.parentId !== undefined}
            describedBy={parentHelpId}
          />
        </FormField>
      )}
      {!summary && (
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
      )}
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
      {!summary && (
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
      )}
    </>
  );
}
