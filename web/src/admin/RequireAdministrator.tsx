import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useIsAdministrator } from '../shell/pages';

/** The Administration is the Administrator's only (ADR-0046): anyone else goes to the home page. */
export function RequireAdministrator({ children }: { children: ReactNode }) {
  return useIsAdministrator() ? children : <Navigate to="/" replace />;
}
