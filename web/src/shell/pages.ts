import AdminPanelSettingsOutlined from '@mui/icons-material/AdminPanelSettingsOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import HomeOutlined from '@mui/icons-material/HomeOutlined';
import { useMatches } from 'react-router';
import { useSession } from '../session/SessionContext';

/** The main navigation, in the order of the sidebar (ADR-0011, ADR-0066). */
export const mainNavigation = [
  { key: 'home', path: '/', labelKey: 'nav.home', icon: HomeOutlined, administratorOnly: false },
  {
    key: 'projects',
    path: '/projects',
    labelKey: 'nav.projects',
    icon: FolderOutlined,
    administratorOnly: false,
  },
  {
    key: 'tickets',
    path: '/tickets',
    labelKey: 'nav.tickets',
    icon: ConfirmationNumberOutlined,
    administratorOnly: false,
  },
  // The Administration belongs to the Administrator only (ADR-0046).
  {
    key: 'admin',
    path: '/admin',
    labelKey: 'nav.admin',
    icon: AdminPanelSettingsOutlined,
    administratorOnly: true,
  },
] as const;

export type NavigationKey = (typeof mainNavigation)[number]['key'];

export type PageTitleKey =
  | 'pages.home.title'
  | 'pages.projects.title'
  | 'pages.tickets.title'
  | 'pages.admin.title'
  | 'pages.users.title'
  | 'pages.audit.title'
  | 'pages.settings.title'
  | 'pages.calendar.title'
  | 'pages.clients.title'
  | 'pages.templates.title'
  | 'pages.vendors.title'
  | 'pages.defaultFolders.title'
  | 'pages.linkCategories.title'
  | 'pages.newProject.title'
  | 'pages.project.title'
  | 'pages.projectSchedule.title'
  | 'pages.projectMembers.title'
  | 'pages.projectTickets.title'
  | 'pages.projectDocuments.title'
  | 'pages.projectLinks.title'
  | 'pages.projectCredentials.title'
  | 'pages.newTicket.title'
  | 'pages.ticket.title'
  | 'pages.ticketFlow.title'
  | 'pages.ticketDeadlines.title'
  | 'pages.profile.title';

/**
 * A level of the trail above the page: an area that leads to its own address, or the project of the address, shown by
 * its code and leading to the project.
 */
export type TrailLevel = { titleKey: PageTitleKey; path: string } | { project: true };

/**
 * What a route tells the shell about its page: the sidebar item it belongs to (none for My profile, reached from
 * the account menu), its title and the trail above it.
 */
export interface PageHandle {
  navigation?: NavigationKey;
  titleKey: PageTitleKey;
  trail?: readonly TrailLevel[];
}

function isPageHandle(handle: unknown): handle is PageHandle {
  return typeof handle === 'object' && handle !== null && 'titleKey' in handle;
}

/** The page of the current route. Every route inside the shell declares a PageHandle. */
export function useCurrentPage(): PageHandle {
  const page = useMatches()
    .map((match) => match.handle)
    .findLast(isPageHandle);
  if (page === undefined) {
    throw new Error('The current route has no page handle.');
  }
  return page;
}

/** Whether the person in the session is the Administrator. */
export function useIsAdministrator(): boolean {
  const { session } = useSession();
  return session.status === 'signed-in' && session.user.role === 'Administrator';
}

/** The items of the main navigation the person in the session may see. */
export function useMainNavigation() {
  const isAdministrator = useIsAdministrator();
  return mainNavigation.filter((item) => !item.administratorOnly || isAdministrator);
}
