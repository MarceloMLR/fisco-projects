import Box from '@mui/material/Box';
import { useEffect, useState } from 'react';
import { Navigate, useSearchParams } from 'react-router';
import { api } from '../api/client';
import { microsoftSignIn } from '../session/microsoftSignIn';
import { rememberedEmail } from '../session/rememberedEmail';
import { type SignInState, useSession } from '../session/SessionContext';
import { signedOutHere } from '../session/signedOut';
import { safeItemPath } from './teamsPage';

/** The parts of a path of the application, as the sign-in page takes the screen to go back to. */
function partsOf(path: string): SignInState['from'] {
  const address = new URL(path, 'https://fiscoprojects.invalid');
  return { pathname: address.pathname, search: address.search, hash: address.hash };
}

/**
 * Whether the link of Teams goes straight to Microsoft (ADR-0153): with the sign-in through Entra ID and without "Sair"
 * in this browser since the last sign-in, with the last e-mail as the hint and the item as the way back. True once the
 * browser is leaving; false when the sign-in page should show.
 */
async function leftForMicrosoft(item: string): Promise<boolean> {
  if (signedOutHere()) {
    return false;
  }
  try {
    const options = await api.GET('/api/session/sign-in-options');
    if (options.data?.entraSignIn !== true) {
      return false;
    }
    const email = rememberedEmail();
    const { data } = await api.POST('/api/session/entra', {
      body: { email: email === '' ? null : email, returnUrl: item, previousUserId: null },
    });
    if (data === undefined) {
      return false;
    }
    microsoftSignIn.go(data.location);
    return true;
  } catch {
    return false;
  }
}

/**
 * The page of passage of the links of the page in Teams (ADR-0153), outside the structure of the screens and without a
 * session. The path of the item, only of the application itself, opens when there is a session; without one, the
 * browser goes straight to the sign-in of Microsoft, or, after "Sair" in this browser or without the sign-in through
 * Entra ID, to the sign-in page, which brings the person back to the item. Only the background while it decides.
 */
export function TeamsOpenPage() {
  const [search] = useSearchParams();
  const item = safeItemPath(search.get('item'));
  const { session } = useSession();
  const [signInPage, setSignInPage] = useState(false);
  const signedOut = session.status === 'signed-out';

  useEffect(() => {
    if (!signedOut) {
      return undefined;
    }
    let active = true;
    void leftForMicrosoft(item).then((left) => {
      if (active && !left) {
        setSignInPage(true);
      }
    });
    return () => {
      active = false;
    };
  }, [signedOut, item]);

  if (session.status === 'signed-in') {
    return <Navigate to={item} replace />;
  }
  if (signInPage) {
    const state: SignInState = { from: partsOf(item) };
    return <Navigate to="/sign-in" replace state={state} />;
  }
  // Only the background, as while the session is asked, also while the browser leaves for Microsoft.
  return <Box sx={{ minHeight: '100dvh', backgroundColor: 'var(--bg-page)' }} />;
}
