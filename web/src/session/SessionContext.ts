import { createContext, useContext } from 'react';
import type { SessionUser } from '../api/client';

export type Session =
  { status: 'checking' } | { status: 'signed-out' } | { status: 'signed-in'; user: SessionUser };

export interface SessionContextValue {
  session: Session;
  /** Asks the API who is in the session, after signing in. */
  refresh: () => Promise<void>;
  signOut: () => Promise<void>;
}

export const SessionContext = createContext<SessionContextValue | null>(null);

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext);
  if (value === null) {
    throw new Error('useSession must be used inside SessionProvider.');
  }
  return value;
}

/** What a protected screen tells the sign-in page: the address to go back to. */
export interface SignInState {
  from: { pathname: string; search: string; hash: string };
}
