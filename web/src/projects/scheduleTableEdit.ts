import type { ScheduleItem } from '../api/client';
import { type DayValue, formatDayText, parseTypedDay } from '../format/dates';
import { type ScheduleItemError, scheduleItemErrors } from './scheduleItemValues';

/** The cells of the Lista that edit in place, in the mode "Editar na tabela" (ADR-0143). */
export type EditableField = 'name' | 'start' | 'end' | 'duration' | 'dependsOn' | 'progress';

/** What a cell holds while it is edited: the text typed, or the ids of the items of "Depende de". */
export type CellValue = string | readonly string[];

/** A cell edited and its value, kept on the row by the column until the Lista saves it. */
export interface CellEdit {
  field: EditableField;
  value: CellValue;
}

/** The message of a cell whose end comes before the start or is not a day (ADR-0143). */
export const endInvalid = 'projects.schedule.errors.endInvalid';

export type CellError = ScheduleItemError | typeof endInvalid;

const wholeNumber = /^\d{1,4}$/;
const maxNameLength = 200;
const maxDuration = 999;

/**
 * The cells of an item that edit in place (ADR-0143): who edits the schedule changes the name of any task or
 * milestone, and the start, the end and the duration (of a task) and "Depende de" of the ones with dates of their own
 * (a summary task takes its dates from its subtasks, ADR-0088); the progress of a task with dates of its own, whoever
 * may update it (ADR-0086).
 */
export function editableFields(item: ScheduleItem, canEdit: boolean): ReadonlySet<EditableField> {
  const fields = new Set<EditableField>();
  const own = !item.hasSubtasks;
  const task = item.kind === 'Task';
  if (canEdit) {
    fields.add('name');
    if (own) {
      fields.add('start');
      fields.add('dependsOn');
      if (task) {
        fields.add('end');
        fields.add('duration');
      }
    }
  }
  if (own && task && item.canUpdateProgress) {
    fields.add('progress');
  }
  return fields;
}

/** The value a cell starts its edition with: the day as dd/mm/aaaa, the numbers as text, the ids of the dependencies. */
export function cellValueOf(item: ScheduleItem, field: EditableField): CellValue {
  switch (field) {
    case 'name':
      return item.name;
    case 'start':
      return formatDayText(item.start);
    case 'end':
      return formatDayText(item.end);
    case 'duration':
      return String(item.duration);
    case 'progress':
      return String(item.progress);
    case 'dependsOn':
      return item.predecessorIds;
  }
}

const textOf = (value: CellValue) => (typeof value === 'string' ? value : '').trim();

/**
 * Why a cell cannot be saved, as the form of the item checks it: the name from 1 to 200 characters, the day as
 * dd/mm/aaaa, the end on the start or after it, the duration from 1 to 999 business days and the progress from 0 to
 * 100; nothing when it can.
 */
export function cellError(
  field: EditableField,
  value: CellValue,
  item: ScheduleItem,
): CellError | undefined {
  const typed = textOf(value);
  switch (field) {
    case 'name':
      return typed === ''
        ? scheduleItemErrors.nameRequired
        : typed.length > maxNameLength
          ? scheduleItemErrors.nameTooLong
          : undefined;
    case 'start':
      return parseTypedDay(typed) === undefined ? scheduleItemErrors.startInvalid : undefined;
    case 'end': {
      const end = parseTypedDay(typed);
      return end === undefined || end < item.start ? endInvalid : undefined;
    }
    case 'duration':
      return wholeNumber.test(typed) && Number(typed) >= 1 && Number(typed) <= maxDuration
        ? undefined
        : scheduleItemErrors.durationInvalid;
    case 'progress':
      return wholeNumber.test(typed) && Number(typed) <= 100
        ? undefined
        : scheduleItemErrors.progressInvalid;
    case 'dependsOn':
      return undefined;
  }
}

/** The body of the edition of an item with the fields as they are, but the ones changed (ADR-0143). */
export interface ItemBody {
  phaseId: string;
  name: string;
  start: DayValue;
  duration: number;
  ownerId: string | null;
  ownedByClient: boolean;
  progress: null;
  predecessorIds: string[];
  parentId: string | null;
  deadline: DayValue | null;
}

/** What a cell asks of the API: the edition of the item, the new dates or the progress. */
export type CellRequest =
  | { kind: 'item'; body: ItemBody }
  | { kind: 'dates'; start: DayValue; end: DayValue | null }
  | { kind: 'progress'; progress: number };

function bodyOf(item: ScheduleItem, changes: Partial<ItemBody>): ItemBody {
  return {
    phaseId: item.phaseId,
    name: item.name,
    start: item.start,
    duration: item.duration,
    ownerId: item.owner?.id ?? null,
    ownedByClient: item.ownedByClient,
    // The progress has its own action (ADR-0086); the note and the kinds and lags of the dependencies kept stay.
    progress: null,
    predecessorIds: [...item.predecessorIds],
    parentId: item.parentId,
    deadline: item.deadline,
    ...changes,
  };
}

const sameIds = (a: readonly string[], b: readonly string[]) =>
  a.length === b.length && a.every((id) => b.includes(id));

/**
 * The request of a valid cell, as the details and the Linha do tempo ask it (ADR-0143): the name, the duration and
 * "Depende de" by the edition of the item; the start by the dates without the end, which keep the duration and push
 * the ones after, as dragging the bar; the end by the dates with the start, as pulling its edge; the progress by its
 * own action. Nothing when the value did not change.
 */
export function cellRequest(
  field: EditableField,
  value: CellValue,
  item: ScheduleItem,
): CellRequest | undefined {
  const typed = textOf(value);
  switch (field) {
    case 'name':
      return typed === item.name
        ? undefined
        : { kind: 'item', body: bodyOf(item, { name: typed }) };
    case 'duration': {
      const duration = Number(typed);
      return duration === item.duration
        ? undefined
        : { kind: 'item', body: bodyOf(item, { duration }) };
    }
    case 'dependsOn': {
      const ids = typeof value === 'string' ? [] : [...value];
      return sameIds(ids, item.predecessorIds)
        ? undefined
        : { kind: 'item', body: bodyOf(item, { predecessorIds: ids }) };
    }
    case 'start': {
      const start = parseTypedDay(typed);
      return start === undefined || start === item.start
        ? undefined
        : { kind: 'dates', start, end: null };
    }
    case 'end': {
      const end = parseTypedDay(typed);
      return end === undefined || end === item.end
        ? undefined
        : { kind: 'dates', start: item.start, end };
    }
    case 'progress': {
      const progress = Number(typed);
      return progress === item.progress ? undefined : { kind: 'progress', progress };
    }
  }
}
