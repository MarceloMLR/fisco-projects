import { useOutletContext } from 'react-router';
import type { Project } from '../api/client';
import type { Notify } from '../notices/notify';

/** What the page of a project gives to its tabs: the project, read once, and the notice of the screens. */
export interface ProjectOutlet {
  project: Project;
  /** Shows a notice at the top of the screen, such as "Diego Torres entrou no projeto." (useNotify). */
  notify: Notify;
}

/** The project of the tab, from the page of the project (ProjectLayout). */
export function useProjectOutlet(): ProjectOutlet {
  return useOutletContext<ProjectOutlet>();
}
