import createClient, { type Middleware } from 'openapi-fetch';
import type { components, paths } from './schema';

export type SessionUser = components['schemas']['SessionUserResponse'];
export type SystemRole = components['schemas']['SystemRole'];
export type UserRow = components['schemas']['UserResponse'];
export type AuditEntry = components['schemas']['AuditEntryResponse'];
export type AuditField = components['schemas']['AuditFieldResponse'];
export type ThemePreference = components['schemas']['ThemePreference'];
export type Profile = components['schemas']['ProfileResponse'];
export type Installation = components['schemas']['InstallationResponse'];
export type CalendarYear = components['schemas']['CalendarYearResponse'];
export type CalendarDay = components['schemas']['CalendarDayResponse'];
export type CalendarDayKind = components['schemas']['CalendarDayKind'];
export type DayOfWeek = components['schemas']['DayOfWeek'];
export type ScheduleRecalculation = components['schemas']['ScheduleRecalculationResponse'];
export type RecalculatedSchedule = components['schemas']['RecalculatedScheduleResponse'];
export type ClientRow = components['schemas']['ClientResponse'];
export type ClientStatus = components['schemas']['ClientStatus'];
export type ClientChoice = components['schemas']['ClientChoiceResponse'];
export type Person = components['schemas']['PersonResponse'];
export type ProjectRow = components['schemas']['ProjectListItemResponse'];
export type Project = components['schemas']['ProjectResponse'];
export type ProjectPerson = components['schemas']['ProjectPersonResponse'];
export type ProjectMember = components['schemas']['ProjectMemberResponse'];
export type ProjectStatus = components['schemas']['ProjectStatus'];
export type ProjectRole = components['schemas']['ProjectRole'];
export type ProjectSituation = components['schemas']['ProjectSituation'];
export type ProjectScheduleSummary = components['schemas']['ProjectScheduleSummaryResponse'];
export type Schedule = components['schemas']['ScheduleResponse'];
export type SchedulePhase = components['schemas']['SchedulePhaseResponse'];
export type ScheduleItem = components['schemas']['ScheduleItemResponse'];
export type ProjectTemplate = components['schemas']['TemplateResponse'];
export type ProjectClosing = components['schemas']['ProjectClosingResponse'];
export type ProjectFolder = components['schemas']['ProjectFolderResponse'];
export type ProjectFolderChoice = components['schemas']['ProjectFolderChoiceResponse'];
export type ProjectFolderRefusal = NonNullable<components['schemas']['ProjectFolderRefusal']>;
export type ScheduleItemKind = components['schemas']['ScheduleItemKind'];
export type TicketRow = components['schemas']['TicketListItemResponse'];
export type Ticket = components['schemas']['TicketResponse'];
export type TicketCounts = components['schemas']['TicketCountsResponse'];
export type TicketChoice = components['schemas']['TicketProjectChoiceResponse'];
export type TicketTeam = components['schemas']['TicketTeam'];
export type TicketPriority = components['schemas']['TicketPriority'];
export type TicketStatus = components['schemas']['TicketStatus'];
export type TicketView = components['schemas']['TicketView'];
export type TicketSituation = components['schemas']['TicketSituation'];
export type TicketAction = components['schemas']['TicketAction'];
export type TicketDue = components['schemas']['TicketDueResponse'];
export type TicketDueState = components['schemas']['TicketDueState'];
export type TicketDeadlines = components['schemas']['TicketDeadlinesResponse'];
export type TicketDeadlineSetting = components['schemas']['TicketDeadlineSettingResponse'];
export type TicketEvent = components['schemas']['TicketEventResponse'];
export type TicketFlow = components['schemas']['TicketFlowResponse'];
export type TicketTransition = components['schemas']['TicketTransitionResponse'];
export type Vendor = components['schemas']['VendorResponse'];
export type VendorTicket = components['schemas']['VendorTicketResponse'];
export type TicketVendor = components['schemas']['TicketVendorResponse'];
export type VendorTicketStatus = components['schemas']['VendorTicketStatus'];
export type EmailStatus = components['schemas']['EmailStatusResponse'];
export type EmailTest = components['schemas']['EmailTestResponse'];
export type DefaultFolder = components['schemas']['DefaultFolderResponse'];
export type DocumentFolders = components['schemas']['DocumentFoldersResponse'];
export type DocumentFolder = components['schemas']['DocumentFolderResponse'];
export type DocumentRow = components['schemas']['DocumentResponse'];
export type DocumentDetails = components['schemas']['DocumentDetailsResponse'];
export type DocumentStatus = components['schemas']['DocumentStatus'];
export type DocumentStatusFilter = NonNullable<components['schemas']['DocumentStatusFilter']>;
export type Acceptance = components['schemas']['AcceptanceResponse'];
export type AcceptanceList = components['schemas']['AcceptanceListResponse'];
export type AcceptanceDetails = components['schemas']['AcceptanceDetailsResponse'];
export type AcceptanceStatus = components['schemas']['AcceptanceStatus'];
export type AcceptanceEventKind = components['schemas']['AcceptanceEventKind'];
export type AcceptanceMilestone = components['schemas']['AcceptanceMilestoneResponse'];
export type ScheduleItemType = components['schemas']['ScheduleItemType'];
export type LinkCategory = components['schemas']['LinkCategoryResponse'];
export type LinkList = components['schemas']['LinkListResponse'];
export type ProjectLink = components['schemas']['LinkResponse'];
export type LinkCategoryCount = components['schemas']['LinkCategoryCountResponse'];
export type Credential = components['schemas']['CredentialResponse'];
export type CredentialList = components['schemas']['CredentialListResponse'];
export type Spreadsheet = components['schemas']['SpreadsheetResponse'];
export type MyWork = components['schemas']['MyWorkResponse'];
export type MyWorkView = components['schemas']['MyWorkView'];
export type MyWorkGroup = components['schemas']['MyWorkGroup'];
export type MyWorkItem = components['schemas']['MyWorkItemResponse'];
export type MyWorkAction = components['schemas']['MyWorkAction'];
export type UpcomingMilestone = components['schemas']['UpcomingMilestoneResponse'];
export type ProjectOverview = components['schemas']['ProjectOverviewResponse'];
export type OverviewRow = components['schemas']['OverviewRowResponse'];
export type TimeEntry = components['schemas']['TimeEntryResponse'];
export type TimeEntryList = components['schemas']['TimeEntryListResponse'];
export type TimeEntryProject = components['schemas']['TimeEntryProjectResponse'];
export type TimeEntryWork = components['schemas']['TimeEntryWorkResponse'];
export type OverviewTimeline = components['schemas']['OverviewTimelineResponse'];
export type OverviewTeam = components['schemas']['OverviewTeamResponse'];
/** Who may take an action of the flow (TicketActors in the Tickets module). */
export type TicketActor = 'Attendant' | 'Requester' | 'Manager' | 'Administrator' | 'System';

/** Statuses of a project in the order of its life (ADR-0016); the last two close it. */
export const projectStatuses = [
  'Planning',
  'InProgress',
  'Paused',
  'Completed',
  'Cancelled',
] as const satisfies readonly ProjectStatus[];

/** Whether a project is completed or cancelled: read-only until reopened (ADR-0016). */
export const isClosedStatus = (status: ProjectStatus) =>
  status === 'Completed' || status === 'Cancelled';

/** Roles in a project, in the order the screens list them (ADR-0040). */
export const projectRoles = [
  'Manager',
  'TechLead',
  'Contributor',
  'Developer',
  'Viewer',
] as const satisfies readonly ProjectRole[];

/** The days of the week from Monday to Sunday, the order of the Calendar screen. */
export const weekdays = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const satisfies readonly DayOfWeek[];

/** The kinds of dates of the calendar, in the order of the filter. */
export const calendarDayKinds = [
  'NationalHoliday',
  'OptionalHoliday',
  'LocalHoliday',
] as const satisfies readonly CalendarDayKind[];

/** The years the calendar answers for (CalendarYears in the Calendar module). */
export const calendarYears = { first: 1900, last: 2199 } as const;

/** Themes in the order the screens offer them (ADR-0014). */
export const themePreferences = [
  'Light',
  'Dark',
  'Automatic',
] as const satisfies readonly ThemePreference[];

/** Item types the Audit screen filters by, in the order of the filter (AuditItemTypes and the module entities). */
export const auditItemTypes = [
  'SignIn',
  'User',
  'InstallationSettings',
  'WorkCalendar',
  'CalculatedHoliday',
  'LocalHoliday',
  'Client',
  'Project',
  'ProjectMember',
  'SchedulePhase',
  'ScheduleItem',
  'ScheduleDependency',
  'ScheduleBaseline',
  'ProjectTemplate',
  'Ticket',
  'TicketDeadlineSetting',
  'Vendor',
  'VendorTicket',
  'DefaultFolder',
  'Document',
  'LinkCategory',
  'ProjectLink',
  'PortalCredential',
  'AcceptanceTerm',
  'TimeEntry',
  'AuditExport',
  'AuditCleanup',
] as const;
export type AuditItemType = (typeof auditItemTypes)[number];

/** The teams a ticket goes to, in the order of the screens (ADR-0068). */
export const ticketTeams = ['Functional', 'Development'] as const satisfies readonly TicketTeam[];

/** The priorities of a ticket, from the lowest to the highest, as the opening offers them. */
export const ticketPriorities = [
  'Low',
  'Medium',
  'High',
  'Critical',
] as const satisfies readonly TicketPriority[];

/** The statuses of a ticket at a vendor, in the order of its cycle (ADR-0028). */
export const vendorTicketStatuses = [
  'Open',
  'InAnalysis',
  'WaitingForUs',
  'Resolved',
  'Closed',
] as const satisfies readonly VendorTicketStatus[];

/** The statuses of a document, in the order of its life (ADR-0031). */
export const documentStatuses = [
  'Draft',
  'InReview',
  'Approved',
  'Obsolete',
] as const satisfies readonly DocumentStatus[];

/** System roles, in the order the screens list them (as in the canvas menu). */
export const systemRoles = [
  'Administrator',
  'ProjectManager',
  'TechLead',
  'User',
] as const satisfies readonly SystemRole[];

/** Problem types the API returns when it refuses a sign-in (SignInProblems in the Users module). */
export const signInProblems = {
  notInDirectory: 'urn:fiscoprojects:problem:not-in-demo-directory',
  inactive: 'urn:fiscoprojects:problem:user-inactive',
} as const;

/** Problem types of a refused logo (InstallationProblems in the Administration module). */
export const installationProblems = {
  logoFormat: 'urn:fiscoprojects:problem:logo-format',
  logoTooLarge: 'urn:fiscoprojects:problem:logo-too-large',
  logoUnsafe: 'urn:fiscoprojects:problem:logo-unsafe',
} as const;

/** Problem types of the clients (ClientProblems in the Projects module). */
export const clientProblems = {
  cnpjInUse: 'urn:fiscoprojects:problem:client-cnpj-in-use',
  logoFormat: 'urn:fiscoprojects:problem:client-logo-format',
  logoTooLarge: 'urn:fiscoprojects:problem:client-logo-too-large',
  hasProjects: 'urn:fiscoprojects:problem:client-has-projects',
} as const;

/** Problem types of the projects (ProjectProblems in the Projects module). */
export const projectProblems = {
  codeInUse: 'urn:fiscoprojects:problem:project-code-in-use',
  closed: 'urn:fiscoprojects:problem:project-closed',
  memberExists: 'urn:fiscoprojects:problem:project-member-exists',
  lastManager: 'urn:fiscoprojects:problem:project-last-manager',
  scheduleCycle: 'urn:fiscoprojects:problem:schedule-cycle',
  scheduleNothingToUndo: 'urn:fiscoprojects:problem:schedule-nothing-to-undo',
  scheduleUndoBlocked: 'urn:fiscoprojects:problem:schedule-undo-blocked',
  templateNameInUse: 'urn:fiscoprojects:problem:template-name-in-use',
  hasOpenTickets: 'urn:fiscoprojects:problem:project-has-open-tickets',
  ticketsChanged: 'urn:fiscoprojects:problem:project-tickets-changed',
  hasFiles: 'urn:fiscoprojects:problem:project-has-files',
} as const;

/** Problem types of the tickets (TicketProblems in the Tickets module). */
/** The problem of an export of the audit with more records than a spreadsheet takes (ADR-0099), with the total. */
export const auditProblems = {
  exportTooLarge: 'urn:fiscoprojects:problem:audit-export-too-large',
} as const;

export const ticketProblems = {
  mentionRefused: 'urn:fiscoprojects:problem:ticket-mention-refused',
  attachmentRefused: 'urn:fiscoprojects:problem:ticket-attachment-refused',
  ticketClosed: 'urn:fiscoprojects:problem:ticket-closed',
  actionNotAllowedNow: 'urn:fiscoprojects:problem:ticket-action-not-allowed-now',
  ticketChanged: 'urn:fiscoprojects:problem:ticket-changed',
  personRefused: 'urn:fiscoprojects:problem:ticket-person-refused',
  ticketFinished: 'urn:fiscoprojects:problem:ticket-finished',
  vendorNameInUse: 'urn:fiscoprojects:problem:vendor-name-in-use',
} as const;

/** Problem types of the documents (DocumentProblems in the Documents module). */
export const documentProblems = {
  projectClosed: 'urn:fiscoprojects:problem:project-closed',
  documentRefused: 'urn:fiscoprojects:problem:document-refused',
  acceptanceFolderReadOnly: 'urn:fiscoprojects:problem:acceptance-folder-read-only',
  documentChanged: 'urn:fiscoprojects:problem:document-changed',
  folderNameInUse: 'urn:fiscoprojects:problem:document-folder-name-in-use',
} as const;

/** Problem types of the credentials (CredentialProblems in the Credentials module). */
export const credentialProblems = {
  projectClosed: 'urn:fiscoprojects:problem:project-closed',
} as const;

/** Problem types of the hours (TimeEntryProblems in the TimeTracking module). */
export const timeEntryProblems = {
  projectClosed: 'urn:fiscoprojects:problem:project-closed',
} as const;

/** Problem types of the acceptance terms (AcceptanceProblems in the Documents module). */
export const acceptanceProblems = {
  projectClosed: 'urn:fiscoprojects:problem:project-closed',
  answered: 'urn:fiscoprojects:problem:acceptance-answered',
  fileRefused: 'urn:fiscoprojects:problem:acceptance-file-refused',
} as const;

/** Problem types of the links (LinkProblems in the Documents module). */
export const linkProblems = {
  categoryNameInUse: 'urn:fiscoprojects:problem:link-category-name-in-use',
} as const;

/** Problem types of the user administration (UserProblems in the Users module). */
export const userProblems = {
  lastAdministrator: 'urn:fiscoprojects:problem:last-administrator',
  anonymized: 'urn:fiscoprojects:problem:user-anonymized',
  active: 'urn:fiscoprojects:problem:user-active',
} as const;

type Listener = () => void;
const unauthorizedListeners = new Set<Listener>();

/** Calls the listener whenever the API answers 401, that is, when the session ended. Returns the unsubscribe. */
export function onUnauthorized(listener: Listener): () => void {
  unauthorizedListeners.add(listener);
  return () => {
    unauthorizedListeners.delete(listener);
  };
}

const sessionEnded: Middleware = {
  onResponse({ response }) {
    if (response.status === 401) {
      for (const listener of unauthorizedListeners) {
        listener();
      }
    }
  },
};

/**
 * Client of the API, typed from the contract (ADR-0076), at the same address as the page (ADR-0054). The session
 * cookie goes with every call; the X-CSRF header is what the API asks of every change.
 */
export const api = createClient<paths>({
  baseUrl: window.location.origin,
  headers: { 'X-CSRF': '1' },
  // Looked up on each call, not kept from when the module loaded, so the tests' simulated API sees it.
  fetch: (request) => globalThis.fetch(request),
});
api.use(sessionEnded);
