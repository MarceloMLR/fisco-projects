import { schemeOfTeams } from './teamsPage';

/** The theme of the page in Teams, as the page applies it. */
export type TeamsScheme = ReturnType<typeof schemeOfTeams>;

/**
 * Teams around the page (ADR-0152), once the library of Teams answered: the token of the person for the application of
 * the installation, asked again on each reading and never kept here (Teams keeps it and renews it), and the theme of
 * Teams, now and when the person changes it.
 */
export interface TeamsHost {
  token: () => Promise<string>;
  scheme: TeamsScheme;
  onScheme: (listener: (scheme: TeamsScheme) => void) => void;
}

/** How long the page waits for Teams to answer, before showing the page of always (ADR-0152). */
export const teamsWaitMs = 3000;

/** How long the page waits for the token, which may need the consent of the person. */
export const tokenWaitMs = 10000;

function within<T>(promise: Promise<T>, ms: number): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    const timer = setTimeout(() => {
      reject(new Error('Teams did not answer in time.'));
    }, ms);
    promise.then(
      (value) => {
        clearTimeout(timer);
        resolve(value);
      },
      (error: unknown) => {
        clearTimeout(timer);
        reject(error instanceof Error ? error : new Error('Teams refused.'));
      },
    );
  });
}

/**
 * Teams around the page, or null: when the library does not load or when Teams does not answer in time, as outside
 * Teams. The page is in a frame of Teams on the computer and on the web, and in a window of its own in Teams on the
 * phone, so only the answer of Teams tells. The library of Teams (@microsoft/teams-js) is imported only here, and only
 * when the page needs it, for the notice of a ticket (ADR-0152).
 */
export async function connectToTeams(waitMs = teamsWaitMs): Promise<TeamsHost | null> {
  try {
    const { app, authentication } = await within(import('@microsoft/teams-js'), waitMs);
    await within(app.initialize(), waitMs);
    const context = await within(app.getContext(), waitMs);
    return {
      token: () => within(authentication.getAuthToken(), tokenWaitMs),
      scheme: schemeOfTeams(context.app.theme),
      onScheme: (listener) => {
        app.registerOnThemeChangeHandler((theme) => {
          listener(schemeOfTeams(theme));
        });
      },
    };
  } catch {
    return null;
  }
}
