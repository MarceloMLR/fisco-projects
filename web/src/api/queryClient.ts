import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './queries';

/**
 * Asks once more after a failure of the network or of the server; a refusal of the API (4xx) does not change by
 * asking again, and a 401 already takes the person to the sign-in page.
 */
function worthAskingAgain(failures: number, error: unknown) {
  return failures < 1 && !(error instanceof ApiError && error.status < 500);
}

/**
 * The cache of the readings of the API (ADR-0080), only in the memory of the page: what the screens read stays for
 * the session, shows at once when a screen opens again and is asked again in the background. Nothing goes to the
 * storage of the browser, and the session provider empties it when the session ends.
 */
export function createQueryClient({ askAgain = true }: { askAgain?: boolean } = {}) {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: askAgain ? worthAskingAgain : false },
    },
  });
}
