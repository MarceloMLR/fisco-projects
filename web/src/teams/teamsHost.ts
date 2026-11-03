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

/** Whether the page is inside a frame, as in Teams; outside one, the library of Teams is not even loaded. */
export function insideAFrame(): boolean {
  return window.parent !== window;
}

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
 * Teams around the page, or null: outside a frame, when the library does not load, or when Teams does not answer in
 * time. The library of Teams (@microsoft/teams-js) is imported only here, and only when the page needs it (ADR-0152).
 */
export async function connectToTeams(waitMs = teamsWaitMs): Promise<TeamsHost | null> {
  if (!insideAFrame()) {
    return null;
  }
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
