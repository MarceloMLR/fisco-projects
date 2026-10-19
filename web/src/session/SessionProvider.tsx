import { useColorScheme } from '@mui/material/styles';
import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useCallback, useEffect, useMemo, useState } from 'react';
import { api, onUnauthorized, type ThemePreference } from '../api/client';
import { queryKeys } from '../api/queries';
import { type Session, SessionContext } from './SessionContext';

/** Queries of public data, read with or without a session: only the brand, not the rules under its key. */
const publicQueries: readonly string[] = [JSON.stringify(queryKeys.installation)];

/** The mode of the theme for a preference of the profile; Automatic follows the operating system. */
const modeOf = (theme: ThemePreference) =>
  theme === 'Light' ? 'light' : theme === 'Dark' ? 'dark' : 'system';

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
 * HttpOnly cookie; any 401 from the API means the session ended, and the screens go to the sign-in page. When the
 * session ends, what the screens read is thrown away, so whoever signs in next sees nothing of it (ADR-0080).
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>({ status: 'checking' });
  const queryClient = useQueryClient();
  const { setMode } = useColorScheme();

  // The theme of the profile once the session is known; without one, as on the sign-in page, the system's.
  const theme = session.status === 'signed-in' ? session.user.theme : undefined;
  useEffect(() => {
    if (session.status !== 'checking') {
      setMode(theme === undefined ? 'system' : modeOf(theme));
    }
  }, [session.status, theme, setMode]);

  // Everything read in the session is thrown away; the brand of the installation is public and stays, since the
  // sign-in page and the tab title keep showing it (removing it would leave them waiting for an answer forever).
  useEffect(() => {
    if (session.status === 'signed-out') {
      queryClient.removeQueries({
        predicate: (query) => !publicQueries.includes(JSON.stringify(query.queryKey)),
      });
      queryClient.getMutationCache().clear();
    }
  }, [session.status, queryClient]);

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

  const chooseTheme = useCallback(
    async (chosen: ThemePreference) => {
      if (session.status !== 'signed-in') {
        return;
      }
      const before = session.user;
      setSession({ status: 'signed-in', user: { ...before, theme: chosen } });
      try {
        const { response } = await api.PUT('/api/profile/theme', { body: { theme: chosen } });
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
        void queryClient.invalidateQueries({ queryKey: queryKeys.profile });
      } catch (error) {
        // Back to the theme the profile still has.
        setSession((current) =>
          current.status === 'signed-in' && current.user.id === before.id
            ? { status: 'signed-in', user: { ...current.user, theme: before.theme } }
            : current,
        );
        throw error;
      }
    },
    [session, queryClient],
  );

  const value = useMemo(
    () => ({ session, refresh, signOut, chooseTheme }),
    [session, refresh, signOut, chooseTheme],
  );
  return <SessionContext value={value}>{children}</SessionContext>;
}
