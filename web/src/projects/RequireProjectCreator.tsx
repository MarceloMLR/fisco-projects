import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useSession } from '../session/SessionContext';

/** Only the Administrator and the Project manager create projects (ADR-0043): anyone else goes to the projects. */
export function RequireProjectCreator({ children }: { children: ReactNode }) {
  const { session } = useSession();
  const role = session.status === 'signed-in' ? session.user.role : undefined;
  return role === 'Administrator' || role === 'ProjectManager' ? (
    children
  ) : (
    <Navigate to="/projects" replace />
  );
}
