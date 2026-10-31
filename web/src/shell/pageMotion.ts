import { useMatches } from 'react-router';
import { useEnterOnChange } from '../theme/motion';

/** The routes with an outlet whose content comes in on a change: the shell, the page of a project and the Administration. */
export const structureRoutes = { shell: 'shell', project: 'project', admin: 'admin' } as const;

/**
 * The content of an outlet comes in when the route right below it changes (ADR-0133): the content of the screens when
 * the page changes, the tab of a project when the tab changes, the section of the Administration when the section
 * changes. Only the search of the address changing, as a drawer opened by it, is no change.
 */
export function usePageMotion<T extends HTMLElement>(
  route: (typeof structureRoutes)[keyof typeof structureRoutes],
) {
  const matches = useMatches();
  const at = matches.findIndex((match) => match.id === route);
  const below = at < 0 ? undefined : matches[at + 1];
  return useEnterOnChange<T>(below === undefined ? '' : `${below.id}:${below.pathname}`);
}
