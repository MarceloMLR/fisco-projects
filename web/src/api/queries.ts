import { infiniteQueryOptions, queryOptions } from '@tanstack/react-query';
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
/** Filters of the list of acceptance terms, as the contract has them. */
export type AcceptancesParams = NonNullable<operations['ListAcceptances']['parameters']['query']>;

/** Filters of the list of credentials, as the contract has them. */
export type CredentialsParams = NonNullable<operations['ListCredentials']['parameters']['query']>;

/** Filters of the list of links, as the contract has them. */
export type LinksParams = NonNullable<operations['ListLinks']['parameters']['query']>;

/** Filters of the list of documents, as the contract has them. */
export type DocumentsParams = NonNullable<operations['ListDocuments']['parameters']['query']>;
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
  vendors: ['vendors'],
  email: ['email'],
  notifications: ['notifications'],
  documents: ['documents'],
  defaultFolders: ['default-folders'],
  links: ['links'],
  linkCategories: ['link-categories'],
  credentials: ['credentials'],
  acceptances: ['acceptances'],
  myWork: ['my-work'],
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

/** The overview of a project, which answers "está no prazo?" (ADR-0073). */
export function projectOverviewQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id, 'overview'] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}/overview', { params: { path: { id } }, signal })),
  });
}

/** The open projects the person is in, as the side bar and Meu trabalho list them (ADR-0066, ADR-0070). */
export function myProjectsQuery() {
  return projectsQuery({ mine: true, situation: 'Active', pageSize: 30 });
}

/** One project, with the people in it and what the person may do. */
export function projectQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}', { params: { path: { id } }, signal })),
  });
}

/** What completing or cancelling the project would leave behind (ADR-0043): the open tickets and the incomplete tasks. */
export function projectClosingQuery(id: string) {
  return queryOptions({
    queryKey: [...queryKeys.projects, id, 'closing'] as const,
    queryFn: ({ signal }) =>
      dataOf(api.GET('/api/projects/{id}/closing', { params: { path: { id } }, signal })),
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

/**
 * The shared mailbox the e-mails leave from and the last test of the sending (ADR-0036), read again every 5 seconds
 * while the test waits in the queue.
 */
export function emailStatusQuery() {
  return queryOptions({
    queryKey: queryKeys.email,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/email', { signal })),
    refetchInterval: (query) => (query.state.data?.lastTest?.status === 'Pending' ? 5000 : false),
  });
}

/** How many notifications of the person are not read, read again every minute (ADR-0038). */
export function unreadNotificationsQuery() {
  return queryOptions({
    queryKey: [...queryKeys.notifications, 'unread-count'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/notifications/unread-count', { signal })),
    refetchInterval: 60_000,
  });
}

/** The notifications of the person, the newest first, page by page (ADR-0038). */
export function notificationsQuery(unread: boolean) {
  return infiniteQueryOptions({
    queryKey: [...queryKeys.notifications, 'list', { unread }] as const,
    initialPageParam: 0,
    queryFn: ({ pageParam, signal }) =>
      dataOf(
        api.GET('/api/notifications', { params: { query: { unread, skip: pageParam } }, signal }),
      ),
    getNextPageParam: (last, pages) =>
      last.hasMore ? pages.reduce((count, page) => count + page.items.length, 0) : undefined,
  });
}

/** The active vendors, for new registrations of tickets at vendors (ADR-0028). */
export function vendorChoicesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.vendors, 'choices'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/tickets/vendor-choices', { signal })),
  });
}

/** Every vendor, for the Administrator (ADR-0028, ADR-0046). */
export function allVendorsQuery() {
  return queryOptions({
    queryKey: [...queryKeys.vendors, 'all'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/vendors', { signal })),
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

/** The folders of the documents of a project the person sees, with the counts, the total and the permissions. */
export function documentFoldersQuery(projectId: string) {
  return queryOptions({
    queryKey: [...queryKeys.documents, projectId, 'folders'] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/documents/folders', {
          params: { path: { projectId } },
          signal,
        }),
      ),
  });
}

/** A page of the documents of a folder, or of the search in the project. */
export function documentsQuery(projectId: string, params: DocumentsParams) {
  return queryOptions({
    queryKey: [...queryKeys.documents, projectId, 'list', params] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/documents', {
          params: { path: { projectId }, query: params },
          signal,
        }),
      ),
  });
}

/** A document with its versions. */
export function documentQuery(projectId: string, documentId: string) {
  return queryOptions({
    queryKey: [...queryKeys.documents, projectId, 'document', documentId] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/documents/{documentId}', {
          params: { path: { projectId, documentId } },
          signal,
        }),
      ),
  });
}

/** The default folders of the installation, for the Administration. */
export function defaultFoldersQuery() {
  return queryOptions({
    queryKey: [...queryKeys.defaultFolders] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/document-folders', { signal })),
  });
}

/** The links of a project, with the search and the category; the categories used and the total come without them. */
export function linksQuery(projectId: string, params: LinksParams = {}) {
  return queryOptions({
    queryKey: [...queryKeys.links, projectId, params] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/links', {
          params: { path: { projectId }, query: params },
          signal,
        }),
      ),
  });
}

/** The active categories of links, offered for new links. */
export function activeLinkCategoriesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.linkCategories, 'active'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/link-categories', { signal })),
  });
}

/** Every category of links, for the Administration. */
export function linkCategoriesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.linkCategories, 'all'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/admin/link-categories', { signal })),
  });
}

/**
 * The credentials of a project without the passwords, with the search and the environment; the environments and the
 * total come without them. A password is asked for apart and never kept here (ADR-0034).
 */
export function credentialsQuery(projectId: string, params: CredentialsParams = {}) {
  return queryOptions({
    queryKey: [...queryKeys.credentials, projectId, params] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/credentials', {
          params: { path: { projectId }, query: params },
          signal,
        }),
      ),
  });
}

/**
 * The acceptance terms of a project the person sees, of one status when asked; the counts are of all of them, with
 * whether the person manages the terms (ADR-0045).
 */
export function acceptancesQuery(projectId: string, params: AcceptancesParams = {}) {
  return queryOptions({
    queryKey: [...queryKeys.acceptances, projectId, 'list', params] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/acceptances', {
          params: { path: { projectId }, query: params },
          signal,
        }),
      ),
  });
}

/** A term with its history and what the person may do with it (ADR-0095). */
export function acceptanceQuery(projectId: string, termId: string) {
  return queryOptions({
    queryKey: [...queryKeys.acceptances, projectId, 'term', termId] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/acceptances/{termId}', {
          params: { path: { projectId, termId } },
          signal,
        }),
      ),
  });
}

/** The milestones of the schedule, with their days, for asking for and attaching terms. */
export function acceptanceMilestonesQuery(projectId: string) {
  return queryOptions({
    queryKey: [...queryKeys.acceptances, projectId, 'milestones'] as const,
    queryFn: ({ signal }) =>
      dataOf(
        api.GET('/api/projects/{projectId}/acceptances/milestones', {
          params: { path: { projectId } },
          signal,
        }),
      ),
  });
}

/** Meu trabalho of the person: the queue by urgency, in its three views (ADR-0070, ADR-0096). */
export function myWorkQuery() {
  return queryOptions({
    queryKey: queryKeys.myWork,
    queryFn: ({ signal }) => dataOf(api.GET('/api/my-work', { signal })),
  });
}

/** The next milestones of the projects of the person, for Meu trabalho (ADR-0096). */
export function upcomingMilestonesQuery() {
  return queryOptions({
    queryKey: [...queryKeys.projects, 'upcoming-milestones'] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/projects/upcoming-milestones', { signal })),
  });
}
