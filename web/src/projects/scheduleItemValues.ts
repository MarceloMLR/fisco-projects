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
  cycle: 'projects.schedule.errors.cycle',
} as const;

export type ScheduleItemError = (typeof scheduleItemErrors)[keyof typeof scheduleItemErrors];

export const isScheduleItemError = (message: string | undefined): message is ScheduleItemError =>
  Object.values(scheduleItemErrors).some((error) => error === message);

const wholeNumber = /^\d{1,4}$/;

/**
 * The form of a task or milestone, checked as the API checks it: a task lasts from 1 to 999 business days and goes
 * from 0 to 100%; a milestone has neither (ADR-0018, ADR-0020). The fields are kept as typed.
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
    progress: z.string(),
    predecessorIds: z.array(z.string()),
  })
  .superRefine((values, context) => {
    if (values.kind === 'Milestone') {
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
    const progress = values.progress.trim();
    if (!wholeNumber.test(progress) || Number(progress) > 100) {
      context.addIssue({
        code: 'custom',
        path: ['progress'],
        message: scheduleItemErrors.progressInvalid,
      });
    }
  });

export type ScheduleItemValues = z.input<typeof scheduleItemSchema>;

/** The value of the owner field of a task without an owner (an empty value would show an empty select). */
export const noOwner = 'none';

/** A new task, in the phase given, without dates yet. */
export function newItemValues(phaseId: string): ScheduleItemValues {
  return {
    kind: 'Task',
    name: '',
    phaseId,
    start: '',
    duration: '1',
    ownerId: noOwner,
    progress: '0',
    predecessorIds: [],
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
    ownerId: item.owner?.id ?? noOwner,
    progress: String(item.progress),
    predecessorIds: item.predecessorIds,
  };
}

/** The body of a change of a task or milestone, as the API takes it. */
export function bodyOf(values: ScheduleItemValues) {
  const task = values.kind === 'Task';
  return {
    phaseId: values.phaseId,
    name: values.name.trim(),
    start: parseTypedDay(values.start) ?? '',
    duration: task ? Number(values.duration.trim()) : 0,
    ownerId: task && values.ownerId !== noOwner ? values.ownerId : null,
    progress: task ? Number(values.progress.trim()) : 0,
    predecessorIds: values.predecessorIds,
  };
}

const apiFields = {
  name: 'name',
  phaseId: 'phaseId',
  start: 'start',
  duration: 'duration',
  progress: 'progress',
  ownerId: 'ownerId',
  predecessorIds: 'predecessorIds',
} as const;

const fieldErrors = {
  name: scheduleItemErrors.nameRequired,
  phaseId: scheduleItemErrors.phaseRequired,
  start: scheduleItemErrors.startInvalid,
  duration: scheduleItemErrors.durationInvalid,
  progress: scheduleItemErrors.progressInvalid,
  ownerId: scheduleItemErrors.ownerInvalid,
  predecessorIds: scheduleItemErrors.predecessorsInvalid,
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
