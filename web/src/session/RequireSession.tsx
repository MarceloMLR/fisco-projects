import Box from '@mui/material/Box';
import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { type SignInState, useSession } from './SessionContext';

/** Only the background of the page, while the application asks the API who is in the session. */
export function SessionPending() {
  return (
    <Box
      data-testid="session-pending"
      sx={{ minHeight: '100dvh', backgroundColor: 'var(--bg-page)' }}
    />
  );
}

/**
 * Screens that need a session: without one, the sign-in page, which brings the person back here, unless the person
 * chose "Sair" or someone else signs in after the session expired.
 */
export function RequireSession({ children }: { children: ReactNode }) {
  const { session } = useSession();
  const { pathname, search, hash } = useLocation();

  if (session.status === 'checking') {
    return <SessionPending />;
  }
  if (session.status === 'signed-out') {
    // After "Sair", the next sign-in goes to Início, whoever signs in (Acesso-1.1).
    if (session.ended?.by === 'signOut') {
      return <Navigate to="/sign-in" replace />;
    }
    const state: SignInState = {
      from: { pathname, search, hash },
      ...(session.ended?.by === 'expired' ? { userId: session.ended.userId } : {}),
    };
    return <Navigate to="/sign-in" replace state={state} />;
  }
  return children;
}
