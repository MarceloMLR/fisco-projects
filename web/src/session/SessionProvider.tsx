import { type ReactNode, useCallback, useEffect, useMemo, useState } from 'react';
import { api, onUnauthorized } from '../api/client';
import { type Session, SessionContext } from './SessionContext';

async function currentSession(): Promise<Session> {
  try {
    const { data } = await api.GET('/api/session');
    return data === undefined ? { status: 'signed-out' } : { status: 'signed-in', user: data };
  } catch {
    return { status: 'signed-out' };
  }
}

/**
 * Who is in the session (ADR-0054): asked to the API when the application opens. The browser keeps only the
 * HttpOnly cookie; any 401 from the API means the session ended, and the screens go to the sign-in page.
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>({ status: 'checking' });

  useEffect(() => {
    let active = true;
    void currentSession().then((current) => {
      if (active) {
        setSession(current);
      }
    });
    return () => {
      active = false;
    };
  }, []);

  useEffect(
    () =>
      onUnauthorized(() => {
        setSession({ status: 'signed-out' });
      }),
    [],
  );

  const refresh = useCallback(async () => {
    setSession(await currentSession());
  }, []);

  const signOut = useCallback(async () => {
    try {
      await api.POST('/api/session/sign-out');
    } finally {
      setSession({ status: 'signed-out' });
    }
  }, []);

  const value = useMemo(() => ({ session, refresh, signOut }), [session, refresh, signOut]);
  return <SessionContext value={value}>{children}</SessionContext>;
}
