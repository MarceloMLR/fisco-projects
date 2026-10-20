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

/** System roles, in the order the screens list them (as in the canvas menu). */
export const systemRoles = [
  'Administrator',
  'ProjectManager',
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
} as const;

/** Problem types of the tickets (TicketProblems in the Tickets module). */
export const ticketProblems = {
  mentionRefused: 'urn:fiscoprojects:problem:ticket-mention-refused',
  attachmentRefused: 'urn:fiscoprojects:problem:ticket-attachment-refused',
} as const;

/** Problem types of the user administration (UserProblems in the Users module). */
export const userProblems = {
  lastAdministrator: 'urn:fiscoprojects:problem:last-administrator',
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
