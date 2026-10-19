import { useOutletContext } from 'react-router';
import type { Project } from '../api/client';

/** What the page of a project gives to its tabs: the project, read once, and the message at the bottom. */
export interface ProjectOutlet {
  project: Project;
  /** Shows a message at the bottom of the page, such as "Diego Torres entrou no projeto." */
  notify: (message: string) => void;
}

/** The project of the tab, from the page of the project (ProjectLayout). */
export function useProjectOutlet(): ProjectOutlet {
  return useOutletContext<ProjectOutlet>();
}
