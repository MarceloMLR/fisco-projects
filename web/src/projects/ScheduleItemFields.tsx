import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import { useId } from 'react';
import {
  type Control,
  Controller,
  type FieldError,
  type FieldErrors,
  useController,
  useWatch,
} from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { Project, Schedule, ScheduleItemKind } from '../api/client';
import { emailsOf, projectMembersQuery } from '../api/queries';
import { FormField } from '../components/FormField';
import { MultiSelect } from '../components/MultiSelect';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { formatDayText, maskTypedDay } from '../format/dates';
import { itemsAfter, itemsOf, linkTypes } from './schedule';
import {
  isScheduleItemError,
  type LinkSetting,
  noOwner,
  noParent,
  type ScheduleItemValues,
  clientOwner,
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
 * business days, the deadline (optional, also of a task with subtasks, which never moves the dates, GanttA-2), the
 * owner (a person of the project who is not a viewer, ADR-0043); and the items it depends on,
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
  // The e-mail below each name tells apart people of the same name; it comes from the members of the project.
  const { data: emails } = useQuery({
    ...projectMembersQuery(project.id),
    select: emailsOf,
  });
  const ownerOptions = [
    { value: noOwner, label: t('projects.schedule.noOwner') },
    // The client of the project may be in charge of a task, as its tests (ADR-0102).
    ...(project.client === null
      ? []
      : [
          {
            value: clientOwner,
            label: t('projects.schedule.form.clientOwner', { name: project.client.name }),
          },
        ]),
    ...project.members
      .filter((member) => member.role !== 'Viewer')
      .toSorted((one, other) => one.name.localeCompare(other.name, 'pt-BR'))
      .map((member) => ({ value: member.id, label: member.name, detail: emails?.get(member.id) })),
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
      <Controller
        control={control}
        name="deadline"
        render={({ field }) => (
          <TextField
            label={t('projects.schedule.form.deadline')}
            optional={t('projects.schedule.form.optional')}
            placeholder={t('projects.schedule.form.dayPlaceholder')}
            inputMode="numeric"
            error={errorText(errors.deadline?.message)}
            helper={t('projects.schedule.form.deadlineHelp')}
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
      {!summary && (
        <LinkSettings
          control={control}
          names={new Map(items.map((item) => [item.id, item.name]))}
          // The schema puts one message on the whole field.
          error={errorText((errors.linkSettings as FieldError | undefined)?.message)}
        />
      )}
    </>
  );
}

/**
 * The kind and the lag of each item chosen in "Depende de" (ADR-0137): one line per item, with its name, the kind
 * ("Termina, então começa", "Começa junto", "Termina junto") and the lag in business days, empty for none.
 */
function LinkSettings({
  control,
  names,
  error,
}: {
  control: Control<ScheduleItemValues>;
  names: ReadonlyMap<string, string>;
  error: string | undefined;
}) {
  const { t } = useTranslation();
  const helpId = useId();
  const predecessorIds = useWatch({ control, name: 'predecessorIds' });
  const { field } = useController({ control, name: 'linkSettings' });
  if (predecessorIds.length === 0) {
    return null;
  }
  const settings: Record<string, LinkSetting> = field.value;
  const change = (id: string, setting: Partial<LinkSetting>) => {
    field.onChange({
      ...settings,
      [id]: { type: 'FinishToStart', lag: '', ...settings[id], ...setting },
    });
  };
  const typeOptions = linkTypes.map((type) => ({
    value: type,
    label: t(`projects.schedule.links.types.${type}`),
  }));
  return (
    <Box
      role="group"
      aria-label={t('projects.schedule.links.label')}
      aria-describedby={helpId}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}
    >
      {predecessorIds.map((id) => {
        const name = names.get(id) ?? '';
        return (
          <Box
            key={id}
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr 88px', sm: 'minmax(0, 1fr) 200px 88px' },
              gap: 'var(--space-2)',
              alignItems: 'center',
            }}
          >
            <Box
              component="span"
              title={name}
              sx={{
                gridColumn: { xs: '1 / -1', sm: 'auto' },
                fontSize: 13,
                lineHeight: '18px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {name}
            </Box>
            <Select
              label={t('projects.schedule.links.type', { name })}
              size="sm"
              value={settings[id]?.type ?? 'FinishToStart'}
              options={typeOptions}
              onChange={(type) => {
                change(id, { type });
              }}
            />
            <TextField
              label={t('projects.schedule.links.lag', { name })}
              hideLabel
              size="sm"
              inputMode="numeric"
              placeholder={t('projects.schedule.links.lagShort')}
              value={settings[id]?.lag ?? ''}
              aria-invalid={error === undefined ? undefined : true}
              onChange={(event) => {
                change(id, { lag: event.target.value.replace(/[^\d-]/g, '').slice(0, 4) });
              }}
            />
          </Box>
        );
      })}
      <Box
        id={helpId}
        sx={{
          fontSize: 12,
          lineHeight: '16px',
          color: error === undefined ? 'var(--text-secondary)' : 'var(--status-danger-text)',
        }}
      >
        {error ?? t('projects.schedule.links.lagHelp')}
      </Box>
    </Box>
  );
}
