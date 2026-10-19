import type { TFunction } from 'i18next';
import {
  type AuditEntry,
  type AuditField,
  systemRoles,
  type SystemRole,
  weekdays,
} from '../api/client';
import { formatCnpj } from '../format/cnpj';
import { formatDayText } from '../format/dates';
import { calculatedHolidayName } from './calendarText';

/** How the Audit screen tells a record (canvas AdminAuditoria). */
export interface AuditDescription {
  /** What was done, as in the list: "Trocou um papel". */
  title: string;
  /** The item and what changed, in one line: "Beatriz Lima · Usuário → Gerente de projeto". */
  summary: string;
  /** Who did what, as the heading of the details: "Paulo Nogueira trocou um papel". */
  sentence: string;
  /** Properties of the item in the details. */
  properties: { label: string; value: string }[];
  /** Changed fields, with the values as the screen shows them. */
  changes: { field: string; before: string; after: string }[];
}

type UserField = 'Name' | 'Email' | 'Role' | 'IsActive';
const userFields: readonly string[] = ['Name', 'Email', 'Role', 'IsActive'] satisfies UserField[];

const isUserField = (field: string): field is UserField => userFields.includes(field);

type SettingsField = 'Name' | 'LogoFileName' | 'TimeZone' | 'MainColor';
const settingsFields: readonly string[] = [
  'Name',
  'LogoFileName',
  'TimeZone',
  'MainColor',
] satisfies SettingsField[];

const isSettingsField = (field: string): field is SettingsField => settingsFields.includes(field);

type HolidayField = 'Name' | 'Date' | 'RepeatsEveryYear' | 'IsConsidered';
const holidayFields: readonly string[] = [
  'Name',
  'Date',
  'RepeatsEveryYear',
  'IsConsidered',
] satisfies HolidayField[];

const isHolidayField = (field: string): field is HolidayField => holidayFields.includes(field);

type ClientField = 'Name' | 'Cnpj' | 'Observation' | 'IsActive' | 'LogoFileName';
const clientFields: readonly string[] = [
  'Name',
  'Cnpj',
  'Observation',
  'IsActive',
  'LogoFileName',
] satisfies ClientField[];

const isClientField = (field: string): field is ClientField => clientFields.includes(field);
const isHoliday = (entry: AuditEntry) =>
  entry.itemType === 'CalculatedHoliday' || entry.itemType === 'LocalHoliday';
const isWeekday = (value: string): value is (typeof weekdays)[number] =>
  weekdays.some((day) => day === value);
const isRole = (value: string | null): value is SystemRole =>
  systemRoles.some((role) => role === value);

function changeOf(entry: AuditEntry, field: string): AuditField | undefined {
  return entry.changes.find((change) => change.field === field);
}

/** A value of a changed field as the screen shows it: roles and situation in words, nothing as a dash. */
function valueOf(entry: AuditEntry, field: string, value: string | null, t: TFunction): string {
  if (value === null) {
    return t('audit.details.none');
  }
  if (entry.itemType === 'User' && field === 'Role' && isRole(value)) {
    return t(`users.roles.${value}`);
  }
  if (entry.itemType === 'User' && field === 'IsActive') {
    return t(value === 'true' ? 'users.status.Active' : 'users.status.Inactive');
  }
  // The days of the week as the API names them ("Monday, Tuesday"), in words of the screen.
  if (entry.itemType === 'WorkCalendar' && field === 'WorkingDays') {
    return value
      .split(', ')
      .map((day) => (isWeekday(day) ? t(`calendar.weekdays.${day}`) : day))
      .join(', ');
  }
  if (isHoliday(entry) && (field === 'IsConsidered' || field === 'RepeatsEveryYear')) {
    return t(value === 'true' ? 'audit.details.yes' : 'audit.details.no');
  }
  if (isHoliday(entry) && field === 'Date') {
    return formatDayText(value);
  }
  if (entry.itemType === 'Client' && field === 'Cnpj') {
    return formatCnpj(value);
  }
  if (entry.itemType === 'Client' && field === 'IsActive') {
    return t(value === 'true' ? 'clients.statuses.Active' : 'clients.statuses.Inactive');
  }
  return value;
}

/** The item as the screen names it: a calculated holiday by its translated name, every other by its label. */
function labelOf(entry: AuditEntry, t: TFunction): string | null {
  return entry.itemType === 'CalculatedHoliday' && entry.itemLabel !== null
    ? calculatedHolidayName(entry.itemLabel, t)
    : entry.itemLabel;
}

/** A holiday whose only change was to be considered or disregarded, said by the title alone. */
function considerationOf(entry: AuditEntry): AuditField | undefined {
  const [only, ...others] = entry.changes;
  return entry.action === 'Updated' && only?.field === 'IsConsidered' && others.length === 0
    ? only
    : undefined;
}

function fieldLabel(entry: AuditEntry, field: string, t: TFunction): string {
  if (entry.itemType === 'User' && isUserField(field)) {
    return t(`audit.fields.User.${field}`);
  }
  if (entry.itemType === 'InstallationSettings' && isSettingsField(field)) {
    return t(`audit.fields.InstallationSettings.${field}`);
  }
  if (entry.itemType === 'WorkCalendar' && field === 'WorkingDays') {
    return t('audit.fields.WorkCalendar.WorkingDays');
  }
  if (isHoliday(entry) && isHolidayField(field)) {
    return t(`audit.fields.Holiday.${field}`);
  }
  if (entry.itemType === 'Client' && isClientField(field)) {
    return t(`audit.fields.Client.${field}`);
  }
  return field;
}

function titleOf(entry: AuditEntry, t: TFunction): string {
  if (entry.action === 'SignedIn' || entry.action === 'SignInRefused') {
    return t(`audit.actions.SignIn.${entry.action}`);
  }
  if (entry.itemType === 'InstallationSettings' && entry.action === 'Updated') {
    return t('audit.actions.InstallationSettings.Updated');
  }
  if (entry.itemType === 'WorkCalendar' && entry.action === 'Updated') {
    return t('audit.actions.WorkCalendar.Updated');
  }
  if (isHoliday(entry)) {
    const consideration = considerationOf(entry);
    if (consideration !== undefined) {
      return t(
        consideration.after === 'true'
          ? 'audit.actions.Holiday.Considered'
          : 'audit.actions.Holiday.Disregarded',
      );
    }
    return t(`audit.actions.Holiday.${entry.action}`);
  }
  if (entry.itemType === 'Client') {
    const situation = entry.action === 'Updated' ? changeOf(entry, 'IsActive') : undefined;
    if (situation !== undefined) {
      return t(
        situation.after === 'false'
          ? 'audit.actions.Client.Deactivated'
          : 'audit.actions.Client.Reactivated',
      );
    }
    return t(`audit.actions.Client.${entry.action}`);
  }
  if (entry.itemType !== 'User') {
    return t(`audit.actions.Other.${entry.action}`);
  }
  if (entry.action !== 'Updated') {
    return t(`audit.actions.User.${entry.action}`);
  }
  if (changeOf(entry, 'Role') !== undefined) {
    return t('audit.actions.User.RoleChanged');
  }
  const situation = changeOf(entry, 'IsActive');
  if (situation !== undefined) {
    return t(
      situation.after === 'false'
        ? 'audit.actions.User.Deactivated'
        : 'audit.actions.User.Reactivated',
    );
  }
  return t('audit.actions.User.Updated');
}

function providerOf(entry: AuditEntry, t: TFunction): string {
  return entry.detail === 'local' || entry.detail === 'entra'
    ? t(`audit.providers.${entry.detail}`)
    : (entry.detail ?? '');
}

function reasonOf(entry: AuditEntry, t: TFunction): string {
  return entry.detail === 'NotInDirectory' || entry.detail === 'Inactive'
    ? t(`audit.refusals.${entry.detail}`)
    : (entry.detail ?? '');
}

function summaryOf(entry: AuditEntry, t: TFunction): string {
  const label = labelOf(entry, t) ?? '';
  const join = (...parts: string[]) => parts.filter((part) => part !== '').join(' · ');
  if (entry.action === 'SignedIn') {
    return providerOf(entry, t);
  }
  if (entry.action === 'SignInRefused') {
    return join(label, reasonOf(entry, t));
  }
  if (entry.action !== 'Updated') {
    return label;
  }
  // A role change says the role before and after; deactivation and reactivation are said by the title.
  const role = entry.itemType === 'User' ? changeOf(entry, 'Role') : undefined;
  if (role !== undefined) {
    return join(
      label,
      `${valueOf(entry, 'Role', role.before, t)} → ${valueOf(entry, 'Role', role.after, t)}`,
    );
  }
  if (
    (entry.itemType === 'User' || entry.itemType === 'Client') &&
    changeOf(entry, 'IsActive') !== undefined
  ) {
    return label;
  }
  if (isHoliday(entry) && considerationOf(entry) !== undefined) {
    return label;
  }
  const change = entry.changes[0];
  if (change === undefined) {
    return label;
  }
  const field = fieldLabel(entry, change.field, t).toLowerCase();
  return change.secret
    ? join(label, `${field} ${t('audit.details.secret').toLowerCase()}`)
    : join(
        label,
        `${field} ${valueOf(entry, change.field, change.before, t)} → ${valueOf(entry, change.field, change.after, t)}`,
      );
}

function propertiesOf(entry: AuditEntry, t: TFunction): AuditDescription['properties'] {
  if (entry.action === 'SignedIn') {
    return [{ label: t('audit.details.provider'), value: providerOf(entry, t) }];
  }
  if (entry.action === 'SignInRefused') {
    return [
      { label: t('audit.details.identity'), value: entry.itemLabel ?? '' },
      { label: t('audit.details.reason'), value: reasonOf(entry, t) },
    ];
  }
  // The settings are always the same item, with no label of their own.
  if (entry.itemType === 'InstallationSettings') {
    return [
      { label: t('audit.details.item.InstallationSettings'), value: t('pages.settings.title') },
    ];
  }
  if (entry.itemType === 'WorkCalendar') {
    return [{ label: t('audit.details.item.WorkCalendar'), value: t('pages.calendar.title') }];
  }
  const label = labelOf(entry, t);
  if (label === null) {
    return [];
  }
  if (isHoliday(entry)) {
    return [{ label: t('audit.details.item.Holiday'), value: label }];
  }
  if (entry.itemType === 'Client') {
    return [{ label: t('audit.details.item.Client'), value: label }];
  }
  return [
    {
      label: t(entry.itemType === 'User' ? 'audit.details.item.User' : 'audit.details.item.Other'),
      value: label,
    },
  ];
}

/** Everything the Audit screen says about a record, in one place. */
export function describeAuditEntry(entry: AuditEntry, t: TFunction): AuditDescription {
  const title = titleOf(entry, t);
  const author = entry.actor?.name ?? t('audit.system');
  return {
    title,
    summary: summaryOf(entry, t),
    sentence: `${author} ${title.charAt(0).toLowerCase()}${title.slice(1)}`,
    properties: propertiesOf(entry, t),
    changes: entry.changes.map((change) => ({
      field: fieldLabel(entry, change.field, t),
      before: change.secret
        ? t('audit.details.none')
        : valueOf(entry, change.field, change.before, t),
      after: change.secret
        ? t('audit.details.secret')
        : valueOf(entry, change.field, change.after, t),
    })),
  };
}
