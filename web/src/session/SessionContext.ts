import { createContext, useContext } from 'react';
import type { SessionUser, ThemePreference } from '../api/client';

/**
 * How a session ended: the person chose "Sair", or a call came back 401 while someone was in it (the id of who was).
 * Without it, nobody was in a session when the application opened.
 */
export type SessionEnd = { by: 'signOut' } | { by: 'expired'; userId: string };

export type Session =
  | { status: 'checking' }
  | { status: 'signed-out'; ended?: SessionEnd }
  | { status: 'signed-in'; user: SessionUser };

export interface SessionContextValue {
  session: Session;
  /** Asks the API who is in the session, after signing in. */
  refresh: () => Promise<void>;
  signOut: () => Promise<void>;
  /**
   * Applies the theme at once and keeps it in the profile (ADR-0014). If the API refuses, the theme goes back to
   * the one before and the promise is rejected, so the screen can tell the person.
   */
  chooseTheme: (theme: ThemePreference) => Promise<void>;
}

export const SessionContext = createContext<SessionContextValue | null>(null);

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext);
  if (value === null) {
    throw new Error('useSession must be used inside SessionProvider.');
  }
  return value;
}

/**
 * What a protected screen tells the sign-in page: the address to go back to and, when the session expired, who was in
 * it, so only that person goes back there.
 */
export interface SignInState {
  from: { pathname: string; search: string; hash: string };
  userId?: string;
}
