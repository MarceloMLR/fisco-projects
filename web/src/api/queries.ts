import { queryOptions } from '@tanstack/react-query';
import { api } from './client';
import type { operations } from './schema';

/** Filters of the list of users, as the contract has them. */
export type UsersParams = NonNullable<operations['ListUsers']['parameters']['query']>;
/** Filters of the list of projects, as the contract has them. */
export type ProjectsParams = NonNullable<operations['ListProjects']['parameters']['query']>;
/** Filters of the people to choose, as the contract has them. */
export type PeopleParams = NonNullable<operations['ListPeople']['parameters']['query']>;
/** Filters of the list of clients, as the contract has them. */
export type ClientsParams = NonNullable<operations['ListClients']['parameters']['query']>;
/** Filters of the list of tickets, as the contract has them. */
export type TicketsParams = NonNullable<operations['ListTickets']['parameters']['query']>;
/** Filters of the audit, as the contract has them. */
export type AuditParams = NonNullable<operations['ListAuditEntries']['parameters']['query']>;

/** An answer of the API without data, with its status and the Problem Details (RFC 9457) that came with it. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: unknown,
  ) {
    super(`The API answered ${String(status)}.`);
    this.name = 'ApiError';
  }
}

/** The data of a call of the typed client; an answer without data becomes an ApiError. */
async function dataOf<Data>(call: Promise<{ data?: Data; error?: unknown; response: Response }>) {
  const { data, error, response } = await call;
  if (data === undefined) {
    throw new ApiError(response.status, error);
  }
  return data;
}

/**
 * The start of the keys of each resource, to ask again for everything that shows it after a change (ADR-0080).
 * Every key of a resource starts with its entry here.
 */
export const queryKeys = {
  users: ['users'],
  audit: ['audit'],
  profile: ['profile'],
  installation: ['installation'],
  calendar: ['calendar'],
  clients: ['clients'],
  projects: ['projects'],
  people: ['people'],
  templates: ['templates'],
  tickets: ['tickets'],
  ticketDeadlines: ['ticket-deadlines'],
} as const;

/** A page of the users, with the filters of the Users screen; with pageSize 1, only the count. */
export function usersQuery(params: UsersParams) {
  return queryOptions({
    queryKey: [...queryKeys.users, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/users', { params: { query: params }, signal })),
  });
}

/** A page of the clients, with the filters of the Clients screen; with pageSize 1, only the count. */
export function clientsQuery(params: ClientsParams) {
  return queryOptions({
    queryKey: [...queryKeys.clients, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/clients', { params: { query: params }, signal })),
  });
}

/** The clients to choose for a project or to filter the projects, active and inactive, in alphabetical order. */
export function clientChoicesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.clients, 'choices'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/clients/choices', { signal })),
  });
}

/** A page of the projects the person sees, with the filters of the Projects screen. */
export function projectsQuery(params: ProjectsParams) {
  return queryOptions({
    queryKey: [...queryKeys.projects, 'list', params] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects', { params: { query: params }, signal })),
  });
}

/** One project, with the people in it and what the person may do. */
export function projectQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}', { params: { path: { id } }, signal })),
  });
}

/** The people of a project, by name, with their role, when they joined and their last access. */
export function projectMembersQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id, 'members'] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}/members', { params: { path: { id } }, signal })),
  });
}

/** The schedule of a project: phases, tasks and milestones with the dates and delays computed by the API. */
export function projectScheduleQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id, 'schedule'] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}/schedule', { params: { path: { id } }, signal })),
  });
}

/**
 * The people of the company to choose, such as the manager or the tech lead of a project or a person to include in
 * it: the active users and the people of the directory who never signed in (ADR-0005).
 */
export function peopleQuery(params: PeopleParams) {
  return queryOptions({
    queryKey: [...queryKeys.people, params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/people', { params: { query: params }, signal })),
  });
}

/** A page of the audit, with the filters of the Audit screen. */
export function auditQuery(params: AuditParams) {
  return queryOptions({
    queryKey: [...queryKeys.audit, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/audit', { params: { query: params }, signal })),
  });
}

/** My profile: the person in the session, how they sign in and the theme they chose. */
export function profileQuery() {
  return queryOptions({
    queryKey: queryKeys.profile,
    queryFn: ({ signal }) => dataOf(api.GET('/api/profile', { signal })),
  });
}

/** The brand of the installation (name and logo), read without a session too, as the sign-in page shows it. */
export function installationQuery() {
  return queryOptions({
    queryKey: queryKeys.installation,
    queryFn: ({ signal }) => dataOf(api.GET('/api/installation', { signal })),
  });
}

/**
 * What the browser needs from the configuration of the installation: the license of the timeline (ADR-0087). It
 * changes only with the configuration, so it is read once a session.
 */
export function clientSettingsQuery() {
  return queryOptions({
    queryKey: [...queryKeys.installation, 'client-settings'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/client-settings', { signal })),
    staleTime: Infinity,
  });
}

/** The rules of the installation for the screens of a session: the time zone of dates and times (ADR-0013). */
export function generalRulesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.installation, 'rules'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/installation/rules', { signal })),
  });
}

/** The calendar of a year: the working days and the dates, with the calculated holidays found for the year. */
export function calendarYearQuery(year: number) {
  return queryOptions({
    queryKey: [...queryKeys.calendar, year] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/calendar/{year}', { params: { path: { year } }, signal })),
  });
}

/** The active models of project, for a new project (ADR-0017). */
export function templatesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.templates, 'active'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/project-templates', { signal })),
  });
}

/** Every model of project, for the Administrator (ADR-0046). */
export function allTemplatesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.templates, 'all'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/project-templates', { signal })),
  });
}

/** A page of the tickets the person sees, with the view and the filters of the lists (ADR-0029, ADR-0044). */
export function ticketsQuery(params: TicketsParams) {
  return queryOptions({
    queryKey: [...queryKeys.tickets, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/tickets', { params: { query: params }, signal })),
  });
}

/**
 * Who may be the assignee or the requester of all the tickets of the numbers (ADR-0027, ADR-0068); the rule is the
 * server's.
 */
export function ticketPeopleChoicesQuery(
  kind: 'assignee' | 'requester',
  numbers: readonly number[],
) {
  return queryOptions({
    queryKey: [
      ...queryKeys.tickets,
      'people-choices',
      kind,
      [...numbers].sort((a, b) => a - b),
    ] as const,
    queryFn: ({ signal }) =>
      dataOf(
        kind === 'assignee'
          ? api.GET('/api/tickets/assignee-choices', {
              params: { query: { number: [...numbers] } },
              signal,
            })
          : api.GET('/api/tickets/requester-choices', {
              params: { query: { number: [...numbers] } },
              signal,
            }),
      ),
  });
}

/** How many tickets each view has, in all the projects the person sees or in one of them. */
export function ticketCountsQuery(projectId?: string) {
  return queryOptions({
    queryKey: [...queryKeys.tickets, 'counts', projectId ?? 'all'] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/tickets/counts', {
          params: { query: projectId === undefined ? {} : { projectId } },
          signal,
        }),
      ),
  });
}

/**
 * The projects the person sees, for the filter of the lists and for opening tickets, with the teams that serve each
 * one. Asked again with the projects, since a project closed or a person included changes them.
 */
export function ticketChoicesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.projects, 'ticket-choices'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/tickets/choices', { signal })),
  });
}

/** One ticket by its number, with its people. */
/** The deadlines of each priority, for the Administration (ADR-0025). */
export function ticketDeadlinesQuery() {
  return queryOptions({
    queryKey: queryKeys.ticketDeadlines,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/ticket-deadlines', { signal })),
  });
}

/** The flow of the tickets, read from its table (ADR-0024); it changes only with a new version of the application. */
export function ticketFlowQuery() {
  return queryOptions({
    queryKey: [...queryKeys.tickets, 'flow'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/tickets/flow', { signal })),
    staleTime: Infinity,
  });
}

export function ticketQuery(number: number) {
  return queryOptions({
    queryKey: [...queryKeys.tickets, number] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/tickets/{number}', { params: { path: { number } }, signal })),
  });
}
