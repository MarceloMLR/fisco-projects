import type { TFunction } from 'i18next';
import type { UseFormSetError } from 'react-hook-form';
import { z } from 'zod';
import { projectProblems, type ScheduleItem } from '../api/client';
import { formatDayText, parseTypedDay } from '../format/dates';
import { invalidFields, problemType } from './projectForm';

/** The keys of the messages of the fields of a task or milestone, as the form and the API name the problems. */
export const scheduleItemErrors = {
  nameRequired: 'projects.schedule.errors.nameRequired',
  nameTooLong: 'projects.schedule.errors.nameTooLong',
  phaseRequired: 'projects.schedule.errors.phaseRequired',
  startInvalid: 'projects.schedule.errors.startInvalid',
  durationInvalid: 'projects.schedule.errors.durationInvalid',
  progressInvalid: 'projects.schedule.errors.progressInvalid',
  ownerInvalid: 'projects.schedule.errors.ownerInvalid',
  predecessorsInvalid: 'projects.schedule.errors.predecessorsInvalid',
  parentInvalid: 'projects.schedule.errors.parentInvalid',
  deadlineInvalid: 'projects.schedule.errors.deadlineInvalid',
  cycle: 'projects.schedule.errors.cycle',
} as const;

export type ScheduleItemError = (typeof scheduleItemErrors)[keyof typeof scheduleItemErrors];

export const isScheduleItemError = (message: string | undefined): message is ScheduleItemError =>
  Object.values(scheduleItemErrors).some((error) => error === message);

const wholeNumber = /^\d{1,4}$/;

/**
 * The form of a task or milestone, checked as the API checks it: a task lasts from 1 to 999 business days; a milestone
 * has no duration (ADR-0018). The progress is not in it: it changes by its own action (ADR-0086). A task with
 * subtasks (`summary`) takes its dates from them, so its duration is not checked (ADR-0088). The deadline is optional,
 * any task or milestone may have one (GanttA-2). The fields are kept as typed.
 */
export const scheduleItemSchema = z
  .object({
    kind: z.enum(['Task', 'Milestone']),
    name: z
      .string()
      .trim()
      .min(1, { error: scheduleItemErrors.nameRequired })
      .max(200, { error: scheduleItemErrors.nameTooLong }),
    phaseId: z.string().min(1, { error: scheduleItemErrors.phaseRequired }),
    start: z.string().refine((value) => parseTypedDay(value) !== undefined, {
      error: scheduleItemErrors.startInvalid,
    }),
    duration: z.string(),
    ownerId: z.string(),
    predecessorIds: z.array(z.string()),
    parentId: z.string(),
    deadline: z.string().refine(
      (value) => value.trim() === '' || parseTypedDay(value) !== undefined,
      { error: scheduleItemErrors.deadlineInvalid },
    ),
    summary: z.boolean().optional(),
  })
  .superRefine((values, context) => {
    if (values.kind === 'Milestone' || values.summary === true) {
      return;
    }
    const duration = values.duration.trim();
    if (!wholeNumber.test(duration) || Number(duration) < 1 || Number(duration) > 999) {
      context.addIssue({
        code: 'custom',
        path: ['duration'],
        message: scheduleItemErrors.durationInvalid,
      });
    }
  });

export type ScheduleItemValues = z.input<typeof scheduleItemSchema>;

/** The progress of a task as typed in its dialog: a whole number from 0 to 100 (ADR-0020). */
export const progressSchema = z.object({
  progress: z
    .string()
    .trim()
    .refine((value) => wholeNumber.test(value) && Number(value) <= 100, {
      error: scheduleItemErrors.progressInvalid,
    }),
});

export type ProgressValues = z.input<typeof progressSchema>;

/** The value of the owner field of a task without an owner (an empty value would show an empty select). */
export const noOwner = 'none';

/** The value of the owner field of a task the client of the project is in charge of (ADR-0102). */
export const clientOwner = 'client';

/** The value of "Dentro de" for an item right in the phase (ADR-0088). */
export const noParent = 'phase';

/** A new task, in the phase given and, for a subtask, in the task given, without dates yet. */
export function newItemValues(phaseId: string, parentId: string = noParent): ScheduleItemValues {
  return {
    kind: 'Task',
    name: '',
    phaseId,
    start: '',
    duration: '1',
    ownerId: noOwner,
    predecessorIds: [],
    parentId,
    deadline: '',
  };
}

/** The form of a task or milestone as it is saved. */
export function valuesOfItem(item: ScheduleItem): ScheduleItemValues {
  return {
    kind: item.kind,
    name: item.name,
    phaseId: item.phaseId,
    start: formatDayText(item.start),
    duration: String(item.duration),
    ownerId: item.ownedByClient ? clientOwner : (item.owner?.id ?? noOwner),
    predecessorIds: item.predecessorIds,
    parentId: item.parentId ?? noParent,
    deadline: item.deadline === null ? '' : formatDayText(item.deadline),
    summary: item.hasSubtasks,
  };
}

/**
 * The fields of a task or milestone as the API takes them, without the progress: a new item starts at 0%, and a
 * change keeps the progress as it is (ADR-0086). A task with subtasks sends no dependencies nor task it is in: the API
 * looks only at its phase, name and owner (ADR-0088).
 */
export function bodyOf(values: ScheduleItemValues) {
  const task = values.kind === 'Task';
  const summary = values.summary === true;
  return {
    phaseId: values.phaseId,
    name: values.name.trim(),
    start: parseTypedDay(values.start) ?? '',
    duration: task ? Number(values.duration.trim()) : 0,
    ownerId:
      task && values.ownerId !== noOwner && values.ownerId !== clientOwner ? values.ownerId : null,
    ownedByClient: task && values.ownerId === clientOwner,
    predecessorIds: summary ? [] : values.predecessorIds,
    parentId: summary || values.parentId === noParent ? null : values.parentId,
    // An empty deadline takes it out (GanttA-2).
    deadline: values.deadline.trim() === '' ? null : (parseTypedDay(values.deadline) ?? null),
  };
}

const apiFields = {
  name: 'name',
  phaseId: 'phaseId',
  start: 'start',
  duration: 'duration',
  ownerId: 'ownerId',
  predecessorIds: 'predecessorIds',
  parentId: 'parentId',
  deadline: 'deadline',
} as const;

const fieldErrors = {
  name: scheduleItemErrors.nameRequired,
  phaseId: scheduleItemErrors.phaseRequired,
  start: scheduleItemErrors.startInvalid,
  duration: scheduleItemErrors.durationInvalid,
  ownerId: scheduleItemErrors.ownerInvalid,
  predecessorIds: scheduleItemErrors.predecessorsInvalid,
  parentId: scheduleItemErrors.parentInvalid,
  deadline: scheduleItemErrors.deadlineInvalid,
} as const;

/**
 * Puts on the fields what the API refused: the invalid fields, or the dependency that would make a cycle.
 * Returns false when the problem is of none of the fields.
 */
export function showRefusal(problem: unknown, setError: UseFormSetError<ScheduleItemValues>) {
  if (problemType(problem) === projectProblems.scheduleCycle) {
    setError('predecessorIds', { message: scheduleItemErrors.cycle });
    return true;
  }
  const fields = invalidFields(problem);
  const refused = Object.entries(apiFields).filter(([, apiField]) => fields.includes(apiField));
  for (const [field] of refused) {
    const name = field as keyof typeof apiFields;
    setError(name, { message: fieldErrors[name] });
  }
  return refused.length > 0;
}

/** The message of a refused change that is no field's: the project was closed meanwhile, or another failure. */
export function failureOf(problem: unknown, status: number, t: TFunction) {
  return status === 409 && problemType(problem) === projectProblems.closed
    ? t('projects.schedule.messages.closed')
    : t('projects.schedule.messages.failed');
}
