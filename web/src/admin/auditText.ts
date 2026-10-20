import type { TFunction } from 'i18next';
import {
  type AuditEntry,
  type AuditField,
  documentStatuses,
  type DocumentStatus,
  isClosedStatus,
  projectRoles,
  type ProjectRole,
  projectStatuses,
  type ProjectStatus,
  type ScheduleItemKind,
  systemRoles,
  type SystemRole,
  ticketPriorities,
  type TicketPriority,
  type TicketStatus,
  type TicketTeam,
  ticketTeams,
  type VendorTicketStatus,
  vendorTicketStatuses,
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

type SettingsField =
  | 'Name'
  | 'LogoFileName'
  | 'TimeZone'
  | 'MainColor'
  | 'DailySummaryAt'
  | 'NotificationRetentionDays';
const settingsFields: readonly string[] = [
  'Name',
  'LogoFileName',
  'TimeZone',
  'MainColor',
  'DailySummaryAt',
  'NotificationRetentionDays',
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

type ProjectField = 'Code' | 'Name' | 'Description' | 'ClientId' | 'Status';
const projectFields: readonly string[] = [
  'Code',
  'Name',
  'Description',
  'ClientId',
  'Status',
] satisfies ProjectField[];

const isProjectField = (field: string): field is ProjectField => projectFields.includes(field);

type ProjectMemberField = 'ProjectId' | 'UserId' | 'Role';
const projectMemberFields: readonly string[] = [
  'ProjectId',
  'UserId',
  'Role',
] satisfies ProjectMemberField[];

const isProjectMemberField = (field: string): field is ProjectMemberField =>
  projectMemberFields.includes(field);

type SchedulePhaseField = 'ProjectId' | 'Name';
const schedulePhaseFields: readonly string[] = ['ProjectId', 'Name'] satisfies SchedulePhaseField[];

const isSchedulePhaseField = (field: string): field is SchedulePhaseField =>
  schedulePhaseFields.includes(field);

type ScheduleItemField =
  | 'ProjectId'
  | 'PhaseId'
  | 'Kind'
  | 'Name'
  | 'Start'
  | 'Duration'
  | 'End'
  | 'Progress'
  | 'OwnerId'
  | 'CompletedOn'
  | 'ReachedOn';
const scheduleItemFields: readonly string[] = [
  'ProjectId',
  'PhaseId',
  'Kind',
  'Name',
  'Start',
  'Duration',
  'End',
  'Progress',
  'OwnerId',
  'CompletedOn',
  'ReachedOn',
] satisfies ScheduleItemField[];

const isScheduleItemField = (field: string): field is ScheduleItemField =>
  scheduleItemFields.includes(field);

type ScheduleDependencyField = 'ProjectId' | 'PredecessorId' | 'SuccessorId';
const scheduleDependencyFields: readonly string[] = [
  'ProjectId',
  'PredecessorId',
  'SuccessorId',
] satisfies ScheduleDependencyField[];

const isScheduleDependencyField = (field: string): field is ScheduleDependencyField =>
  scheduleDependencyFields.includes(field);

type ProjectTemplateField = 'Name' | 'Description' | 'IsActive';
const projectTemplateFields: readonly string[] = [
  'Name',
  'Description',
  'IsActive',
] satisfies ProjectTemplateField[];

const isProjectTemplateField = (field: string): field is ProjectTemplateField =>
  projectTemplateFields.includes(field);

type TicketField =
  | 'Number'
  | 'ProjectId'
  | 'Team'
  | 'Priority'
  | 'Title'
  | 'Description'
  | 'Status'
  | 'RequesterId'
  | 'AssigneeId';
const ticketFields: readonly string[] = [
  'Number',
  'ProjectId',
  'Team',
  'Priority',
  'Title',
  'Description',
  'Status',
  'RequesterId',
  'AssigneeId',
] satisfies TicketField[];

const isTicketField = (field: string): field is TicketField => ticketFields.includes(field);
type TicketDeadlineField = 'FirstResponseDays' | 'ResolutionDays';
const isTicketDeadlineField = (field: string): field is TicketDeadlineField =>
  field === 'FirstResponseDays' || field === 'ResolutionDays';
const isTicketTeam = (value: string): value is TicketTeam =>
  ticketTeams.some((team) => team === value);
const isTicketPriority = (value: string): value is TicketPriority =>
  ticketPriorities.some((priority) => priority === value);
const ticketStatuses: readonly string[] = [
  'Open',
  'Triage',
  'InProgress',
  'WaitingForRequester',
  'Resolved',
  'Closed',
] satisfies TicketStatus[];
const isTicketStatus = (value: string): value is TicketStatus => ticketStatuses.includes(value);

type VendorField = 'Name' | 'PortalUrl' | 'IsActive';
const vendorFields: readonly string[] = ['Name', 'PortalUrl', 'IsActive'] satisfies VendorField[];
const isVendorField = (field: string): field is VendorField => vendorFields.includes(field);

type VendorTicketField =
  'TicketId' | 'VendorId' | 'Code' | 'OpenedOn' | 'Status' | 'Link' | 'Note' | 'RegisteredById';
const vendorTicketFields: readonly string[] = [
  'TicketId',
  'VendorId',
  'Code',
  'OpenedOn',
  'Status',
  'Link',
  'Note',
  'RegisteredById',
] satisfies VendorTicketField[];
const isVendorTicketField = (field: string): field is VendorTicketField =>
  vendorTicketFields.includes(field);
const isVendorTicketStatus = (value: string): value is VendorTicketStatus =>
  vendorTicketStatuses.some((status) => status === value);

type ScheduleBaselineField = 'SavedOn' | 'Start' | 'End';
const scheduleBaselineFields: readonly string[] = [
  'SavedOn',
  'Start',
  'End',
] satisfies ScheduleBaselineField[];

const isScheduleBaselineField = (field: string): field is ScheduleBaselineField =>
  scheduleBaselineFields.includes(field);

type DefaultFolderField = 'Name' | 'IsAcceptance' | 'IsActive';
const defaultFolderFields: readonly string[] = [
  'Name',
  'IsAcceptance',
  'IsActive',
] satisfies DefaultFolderField[];
const isDefaultFolderField = (field: string): field is DefaultFolderField =>
  defaultFolderFields.includes(field);

type DocumentField =
  'ProjectId' | 'FolderId' | 'Name' | 'Status' | 'Version' | 'SchedulePhaseId' | 'ScheduleItemId';
const documentFields: readonly string[] = [
  'ProjectId',
  'FolderId',
  'Name',
  'Status',
  'Version',
  'SchedulePhaseId',
  'ScheduleItemId',
] satisfies DocumentField[];
const isDocumentField = (field: string): field is DocumentField => documentFields.includes(field);
const isDocumentStatus = (value: string): value is DocumentStatus =>
  documentStatuses.some((status) => status === value);

const scheduleTypes = ['SchedulePhase', 'ScheduleItem', 'ScheduleDependency'] as const;

/** A record of a phase, a task or milestone, or a dependency of a schedule. */
const isSchedule = (
  entry: AuditEntry,
): entry is AuditEntry & { itemType: (typeof scheduleTypes)[number] } =>
  scheduleTypes.some((type) => type === entry.itemType);

const isScheduleItemKind = (value: string | null | undefined): value is ScheduleItemKind =>
  value === 'Task' || value === 'Milestone';

const scheduleDays: readonly string[] = ['Start', 'End', 'CompletedOn', 'ReachedOn'];
const isProjectStatus = (value: string | null): value is ProjectStatus =>
  projectStatuses.some((status) => status === value);
const isProjectRole = (value: string | null): value is ProjectRole =>
  projectRoles.some((role) => role === value);
const isHoliday = (entry: AuditEntry) =>
  entry.itemType === 'CalculatedHoliday' || entry.itemType === 'LocalHoliday';
const isWeekday = (value: string): value is (typeof weekdays)[number] =>
  weekdays.some((day) => day === value);
const isRole = (value: string | null): value is SystemRole =>
  systemRoles.some((role) => role === value);

/** A field recorded unchanged in a change of the item, to say where it is (IsAuditContext in the API). */
const isContext = (entry: AuditEntry, change: AuditField) =>
  entry.action === 'Updated' && !change.secret && change.before === change.after;

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
  if (entry.itemType === 'Project' && field === 'Status' && isProjectStatus(value)) {
    return t(`projects.statuses.${value}`);
  }
  if (entry.itemType === 'ProjectMember' && field === 'Role' && isProjectRole(value)) {
    return t(`projects.roles.${value}`);
  }
  if (entry.itemType === 'ScheduleItem' && field === 'Kind' && isScheduleItemKind(value)) {
    return t(`projects.schedule.kinds.${value}`);
  }
  if (entry.itemType === 'ScheduleItem' && scheduleDays.includes(field)) {
    return formatDayText(value);
  }
  if (entry.itemType === 'ScheduleBaseline' && isScheduleBaselineField(field)) {
    return formatDayText(value);
  }
  if (entry.itemType === 'ProjectTemplate' && field === 'IsActive') {
    return t(value === 'true' ? 'templates.statuses.Active' : 'templates.statuses.Inactive');
  }
  if (entry.itemType === 'Ticket' && field === 'Team' && isTicketTeam(value)) {
    return t(`tickets.teams.${value}`);
  }
  if (entry.itemType === 'Ticket' && field === 'Priority' && isTicketPriority(value)) {
    return t(`tickets.priorities.${value}`);
  }
  if (entry.itemType === 'Ticket' && field === 'Status' && isTicketStatus(value)) {
    return t(`tickets.statuses.${value}`);
  }
  if (entry.itemType === 'Vendor' && field === 'IsActive') {
    return t(value === 'true' ? 'vendors.statuses.Active' : 'vendors.statuses.Inactive');
  }
  if (entry.itemType === 'VendorTicket' && field === 'Status' && isVendorTicketStatus(value)) {
    return t(`tickets.vendors.statuses.${value}`);
  }
  if (entry.itemType === 'VendorTicket' && field === 'OpenedOn') {
    return formatDayText(value);
  }
  if (entry.itemType === 'DefaultFolder' && field === 'IsActive') {
    return t(
      value === 'true' ? 'defaultFolders.statuses.Active' : 'defaultFolders.statuses.Inactive',
    );
  }
  if (entry.itemType === 'DefaultFolder' && field === 'IsAcceptance') {
    return t(value === 'true' ? 'audit.details.yes' : 'audit.details.no');
  }
  if (entry.itemType === 'Document' && field === 'Status' && isDocumentStatus(value)) {
    return t(`documents.statuses.${value}`);
  }
  // A time of day (07:30:00) without the seconds.
  if (entry.itemType === 'InstallationSettings' && field === 'DailySummaryAt') {
    return value.slice(0, 5);
  }
  return value;
}

/**
 * The item as the screen names it: a calculated holiday by its translated name, the deadlines of the tickets by the
 * priority, every other by its label.
 */
function labelOf(entry: AuditEntry, t: TFunction): string | null {
  if (
    entry.itemType === 'TicketDeadlineSetting' &&
    entry.itemLabel !== null &&
    isTicketPriority(entry.itemLabel)
  ) {
    return t(`tickets.priorities.${entry.itemLabel}`);
  }
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
  if (entry.itemType === 'Project' && isProjectField(field)) {
    return t(`audit.fields.Project.${field}`);
  }
  if (entry.itemType === 'ProjectMember' && isProjectMemberField(field)) {
    return t(`audit.fields.ProjectMember.${field}`);
  }
  if (entry.itemType === 'SchedulePhase' && isSchedulePhaseField(field)) {
    return t(`audit.fields.SchedulePhase.${field}`);
  }
  if (entry.itemType === 'ScheduleItem' && isScheduleItemField(field)) {
    return t(`audit.fields.ScheduleItem.${field}`);
  }
  if (entry.itemType === 'ScheduleDependency' && isScheduleDependencyField(field)) {
    return t(`audit.fields.ScheduleDependency.${field}`);
  }
  if (entry.itemType === 'ScheduleBaseline' && isScheduleBaselineField(field)) {
    return t(`audit.fields.ScheduleBaseline.${field}`);
  }
  if (entry.itemType === 'ProjectTemplate' && isProjectTemplateField(field)) {
    return t(`audit.fields.ProjectTemplate.${field}`);
  }
  if (entry.itemType === 'Ticket' && isTicketField(field)) {
    return t(`audit.fields.Ticket.${field}`);
  }
  if (entry.itemType === 'TicketDeadlineSetting' && isTicketDeadlineField(field)) {
    return t(`audit.fields.TicketDeadlineSetting.${field}`);
  }
  if (entry.itemType === 'Vendor' && isVendorField(field)) {
    return t(`audit.fields.Vendor.${field}`);
  }
  if (entry.itemType === 'VendorTicket' && isVendorTicketField(field)) {
    return t(`audit.fields.VendorTicket.${field}`);
  }
  if (entry.itemType === 'DefaultFolder' && isDefaultFolderField(field)) {
    return t(`audit.fields.DefaultFolder.${field}`);
  }
  if (entry.itemType === 'Document' && isDocumentField(field)) {
    return t(`audit.fields.Document.${field}`);
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
  if (entry.itemType === 'Project') {
    return projectTitleOf(entry, t);
  }
  // A vendor of the installation, included, changed, deactivated and reactivated (ADR-0028).
  if (entry.itemType === 'Vendor') {
    const situation = entry.action === 'Updated' ? changeOf(entry, 'IsActive') : undefined;
    if (situation !== undefined) {
      return t(
        situation.after === 'false'
          ? 'audit.actions.Vendor.Deactivated'
          : 'audit.actions.Vendor.Reactivated',
      );
    }
    return t(
      `audit.actions.Vendor.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
  }
  // A ticket at a vendor, registered in a ticket and updated (ADR-0028).
  if (entry.itemType === 'VendorTicket') {
    return t(
      `audit.actions.VendorTicket.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
  }
  if (entry.itemType === 'ProjectMember') {
    return t(`audit.actions.ProjectMember.${entry.action}`);
  }
  // A default folder of the documents, included, renamed, deactivated and reactivated (ADR-0031).
  if (entry.itemType === 'DefaultFolder') {
    const situation = entry.action === 'Updated' ? changeOf(entry, 'IsActive') : undefined;
    if (situation !== undefined) {
      return t(
        situation.after === 'false'
          ? 'audit.actions.DefaultFolder.Deactivated'
          : 'audit.actions.DefaultFolder.Reactivated',
      );
    }
    return t(
      `audit.actions.DefaultFolder.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
  }
  if (entry.itemType === 'Document') {
    return documentTitleOf(entry, t);
  }
  if (isSchedule(entry)) {
    return scheduleTitleOf(entry, t);
  }
  // The deadlines of a priority, changed by the Administrator (ADR-0025).
  if (entry.itemType === 'TicketDeadlineSetting') {
    return t('audit.actions.TicketDeadlineSetting.Updated');
  }
  // A ticket of a project, opened (ADR-0029).
  if (entry.itemType === 'Ticket') {
    return t(
      `audit.actions.Ticket.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
  }
  // A model of project, saved from a schedule, changed and removed (ADR-0017).
  if (entry.itemType === 'ProjectTemplate') {
    return t(
      `audit.actions.ProjectTemplate.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
  }
  // The baseline of a project, saved and saved again (ADR-0021).
  if (entry.itemType === 'ScheduleBaseline') {
    return t(
      `audit.actions.ScheduleBaseline.${entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated'}`,
    );
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

/** A project whose only change was the status says so by the title: completed, cancelled, reopened or changed. */
function projectTitleOf(entry: AuditEntry, t: TFunction): string {
  if (entry.action === 'Created' || entry.action === 'Deleted') {
    return t(`audit.actions.Project.${entry.action}`);
  }
  const [only, ...others] = entry.changes;
  if (only?.field !== 'Status' || others.length > 0) {
    return t('audit.actions.Project.Updated');
  }
  if (only.after === 'Completed' || only.after === 'Cancelled') {
    return t(`audit.actions.Project.${only.after}`);
  }
  return isProjectStatus(only.before) && isClosedStatus(only.before)
    ? t('audit.actions.Project.Reopened')
    : t('audit.actions.Project.StatusChanged');
}

/** A change of a document says what it was: a new version, the status or the link with the schedule (ADR-0031). */
function documentTitleOf(entry: AuditEntry, t: TFunction): string {
  if (entry.action === 'Created' || entry.action === 'Deleted') {
    return t(`audit.actions.Document.${entry.action}`);
  }
  const changed = entry.changes
    .filter((change) => !isContext(entry, change))
    .map((change) => change.field);
  if (changed.includes('Version')) {
    return t('audit.actions.Document.NewVersion');
  }
  if (changed.length > 0 && changed.every((field) => field === 'Status')) {
    return t('audit.actions.Document.StatusChanged');
  }
  if (
    changed.length > 0 &&
    changed.every((field) => field === 'SchedulePhaseId' || field === 'ScheduleItemId')
  ) {
    return t('audit.actions.Document.Linked');
  }
  return t('audit.actions.Document.Updated');
}

/** The folder of a document, by its current name. */
function folderOf(entry: AuditEntry): string {
  const folder = changeOf(entry, 'FolderId');
  return folder?.after ?? folder?.before ?? '';
}

/** A change of a schedule says whether it was a phase, a task, a milestone or a dependency. */
function scheduleTitleOf(entry: AuditEntry, t: TFunction): string {
  const action =
    entry.action === 'Created' || entry.action === 'Deleted' ? entry.action : 'Updated';
  if (entry.itemType === 'SchedulePhase') {
    return t(`audit.actions.SchedulePhase.${action}`);
  }
  if (entry.itemType === 'ScheduleDependency') {
    return t(`audit.actions.ScheduleDependency.${action}`);
  }
  // The kind comes in every record of an item (IsAuditContext in the API).
  const kind = changeOf(entry, 'Kind');
  const value = kind?.after ?? kind?.before;
  return t(`audit.actions.ScheduleItem.${isScheduleItemKind(value) ? value : 'Task'}.${action}`);
}

/**
 * The project a person was included in, removed from or had the role changed in, or of a change of a schedule, by
 * name.
 */
function projectOfMember(entry: AuditEntry): string {
  const project = changeOf(entry, 'ProjectId');
  return project?.after ?? project?.before ?? '';
}

/** The ticket of a registration at a vendor, by number and title (IsAuditContext in the API). */
function ticketOf(entry: AuditEntry): string {
  const ticket = changeOf(entry, 'TicketId');
  return ticket?.after ?? ticket?.before ?? '';
}

/** The vendor of a registration at a vendor, by name. */
function vendorOf(entry: AuditEntry): string {
  const vendor = changeOf(entry, 'VendorId');
  return vendor?.after ?? vendor?.before ?? '';
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
  // A person in a project is said with the role and the project.
  if (entry.itemType === 'ProjectMember') {
    const role = changeOf(entry, 'Role');
    const roleValue = role?.after ?? role?.before ?? null;
    return join(
      label,
      roleValue === null ? '' : valueOf(entry, 'Role', roleValue, t),
      projectOfMember(entry),
    );
  }
  // A dependency says which item comes first; every change of a schedule says the project.
  if (entry.itemType === 'ScheduleDependency') {
    const predecessor = changeOf(entry, 'PredecessorId');
    const name = predecessor?.after ?? predecessor?.before ?? null;
    return join(label, name === null ? '' : t('audit.dependsOn', { name }), projectOfMember(entry));
  }
  if (isSchedule(entry) && entry.action !== 'Updated') {
    return join(label, projectOfMember(entry));
  }
  // A registration at a vendor says the ticket, the number and the vendor: the ticket · "FX-20931 no Emissor Exemplo".
  if (entry.itemType === 'VendorTicket' && entry.action !== 'Updated') {
    return join(ticketOf(entry), t('audit.vendorTicket', { code: label, vendor: vendorOf(entry) }));
  }
  if (entry.itemType === 'Vendor' && changeOf(entry, 'IsActive') !== undefined) {
    return label;
  }
  if (entry.itemType === 'DefaultFolder' && changeOf(entry, 'IsActive') !== undefined) {
    return label;
  }
  // A document sent or removed says the folder and the project.
  if (entry.itemType === 'Document' && entry.action !== 'Updated') {
    return join(label, folderOf(entry), projectOfMember(entry));
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
  // The first field that changed, not one that only says where the item is.
  const change = entry.changes.find((candidate) => !isContext(entry, candidate));
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
  if (entry.itemType === 'Project') {
    return [{ label: t('audit.details.item.Project'), value: label }];
  }
  if (entry.itemType === 'ScheduleBaseline') {
    return [{ label: t('audit.details.item.ScheduleBaseline'), value: label }];
  }
  if (entry.itemType === 'ProjectTemplate') {
    return [{ label: t('audit.details.item.ProjectTemplate'), value: label }];
  }
  if (entry.itemType === 'ProjectMember') {
    return [
      { label: t('audit.details.item.ProjectMember'), value: label },
      { label: t('audit.fields.ProjectMember.ProjectId'), value: projectOfMember(entry) },
    ];
  }
  if (entry.itemType === 'TicketDeadlineSetting') {
    return [{ label: t('audit.details.item.TicketDeadlineSetting'), value: label }];
  }
  if (entry.itemType === 'Vendor') {
    return [{ label: t('audit.details.item.Vendor'), value: label }];
  }
  if (entry.itemType === 'VendorTicket') {
    return [
      { label: t('audit.details.item.VendorTicket'), value: label },
      { label: t('audit.fields.VendorTicket.VendorId'), value: vendorOf(entry) },
      { label: t('audit.fields.VendorTicket.TicketId'), value: ticketOf(entry) },
    ];
  }
  if (entry.itemType === 'DefaultFolder') {
    return [{ label: t('audit.details.item.DefaultFolder'), value: label }];
  }
  if (entry.itemType === 'Document') {
    return [
      { label: t('audit.details.item.Document'), value: label },
      ...(folderOf(entry) === ''
        ? []
        : [{ label: t('audit.fields.Document.FolderId'), value: folderOf(entry) }]),
      { label: t('audit.fields.ProjectMember.ProjectId'), value: projectOfMember(entry) },
    ];
  }
  if (entry.itemType === 'Ticket') {
    return [
      { label: t('audit.details.item.Ticket'), value: label },
      { label: t('audit.fields.ProjectMember.ProjectId'), value: projectOfMember(entry) },
    ];
  }
  if (isSchedule(entry)) {
    return [
      { label: t(`audit.details.item.${entry.itemType}`), value: label },
      { label: t('audit.fields.ProjectMember.ProjectId'), value: projectOfMember(entry) },
    ];
  }
  return [
    {
      label: t(entry.itemType === 'User' ? 'audit.details.item.User' : 'audit.details.item.Other'),
      value: label,
    },
  ];
}

/**
 * The fields of a task or milestone in the order of its form, and those of a baseline in the order of the day it was
 * saved, the start and the end, not in the alphabetical order the API keeps them: the start before the end.
 */
function inFormOrder(entry: AuditEntry): AuditEntry {
  const order =
    entry.itemType === 'ScheduleItem'
      ? scheduleItemFields
      : entry.itemType === 'ScheduleBaseline'
        ? scheduleBaselineFields
        : undefined;
  if (order === undefined) {
    return entry;
  }
  const place = (field: string) => {
    const index = order.indexOf(field);
    return index === -1 ? order.length : index;
  };
  return {
    ...entry,
    changes: entry.changes.toSorted((one, other) => place(one.field) - place(other.field)),
  };
}

/** Everything the Audit screen says about a record, in one place. */
export function describeAuditEntry(record: AuditEntry, t: TFunction): AuditDescription {
  const entry = inFormOrder(record);
  const title = titleOf(entry, t);
  const author = entry.actor?.name ?? t('audit.system');
  return {
    title,
    summary: summaryOf(entry, t),
    sentence: `${author} ${title.charAt(0).toLowerCase()}${title.slice(1)}`,
    properties: propertiesOf(entry, t),
    // A field that says where the item is comes unchanged in a change, such as the project of a person whose role
    // changed: it goes in the text of the record, not in the table of the changes.
    changes: entry.changes
      .filter((change) => !isContext(entry, change))
      .map((change) => ({
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
