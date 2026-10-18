import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import HomeOutlined from '@mui/icons-material/HomeOutlined';
import { useMatches } from 'react-router';

/** The main navigation, in the order of the sidebar (ADR-0011, ADR-0066). */
export const mainNavigation = [
  { key: 'home', path: '/', labelKey: 'nav.home', icon: HomeOutlined },
  { key: 'projects', path: '/projects', labelKey: 'nav.projects', icon: FolderOutlined },
  { key: 'tickets', path: '/tickets', labelKey: 'nav.tickets', icon: ConfirmationNumberOutlined },
] as const;

export type NavigationKey = (typeof mainNavigation)[number]['key'];

/** What a route tells the shell about its page: the sidebar item it belongs to and its title. */
export interface PageHandle {
  navigation: NavigationKey;
  titleKey: 'pages.home.title' | 'pages.projects.title' | 'pages.tickets.title';
}

function isPageHandle(handle: unknown): handle is PageHandle {
  return (
    typeof handle === 'object' && handle !== null && 'titleKey' in handle && 'navigation' in handle
  );
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
