import type { QueryClient } from '@tanstack/react-query';
import {
  projectMembersQuery,
  projectOverviewQuery,
  projectQuery,
  projectScheduleQuery,
  ticketQuery,
} from './queries';

/**
 * A reading started ahead, without waiting for it (ADR-0106): a static stale time asks the API only for what the cache does
 * not have yet, so a reading already there is left as it is, and the screen shows it at once and asks again in the
 * background, as always (ADR-0080). A failure is the screen's to show, when it reads.
 */
function ahead(reading: Promise<unknown>): void {
  reading.catch(() => undefined);
}

/** The tabs of a project with a reading of their own besides the header. */
type TabWithReading = 'overview' | 'schedule' | 'members';

/**
 * What the page of a project reads before showing a tab: the project and the reading of the tab, at once, instead of
 * the tab waiting for the project (ADR-0106).
 */
export function prefetchProject(
  queryClient: QueryClient,
  projectId: string,
  tab: TabWithReading | 'other' = 'overview',
): void {
  // The project and the tab; the counts of the header come with the header, once the project is there.
  ahead(queryClient.query({ ...projectQuery(projectId), staleTime: 'static' }));
  if (tab === 'overview') {
    ahead(queryClient.query({ ...projectOverviewQuery(projectId), staleTime: 'static' }));
  } else if (tab === 'schedule') {
    ahead(queryClient.query({ ...projectScheduleQuery(projectId), staleTime: 'static' }));
  } else if (tab === 'members') {
    ahead(queryClient.query({ ...projectMembersQuery(projectId), staleTime: 'static' }));
  }
}

/** The ticket a person is about to open, as the page of the ticket reads it. */
export function prefetchTicket(queryClient: QueryClient, number: number): void {
  ahead(queryClient.query({ ...ticketQuery(number), staleTime: 'static' }));
}
