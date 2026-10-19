import createClient, { type Middleware } from 'openapi-fetch';
import type { components, paths } from './schema';

export type SessionUser = components['schemas']['SessionUserResponse'];
export type SystemRole = components['schemas']['SystemRole'];
export type UserRow = components['schemas']['UserResponse'];
export type AuditEntry = components['schemas']['AuditEntryResponse'];
export type AuditField = components['schemas']['AuditFieldResponse'];
export type ThemePreference = components['schemas']['ThemePreference'];
export type Profile = components['schemas']['ProfileResponse'];
export type Installation = components['schemas']['InstallationResponse'];

/** Themes in the order the screens offer them (ADR-0014). */
export const themePreferences = [
  'Light',
  'Dark',
  'Automatic',
] as const satisfies readonly ThemePreference[];

/** Item types the Audit screen filters by, in the order of the filter (AuditItemTypes and the module entities). */
export const auditItemTypes = ['SignIn', 'User', 'InstallationSettings'] as const;
export type AuditItemType = (typeof auditItemTypes)[number];

/** System roles, in the order the screens list them (as in the canvas menu). */
export const systemRoles = [
  'Administrator',
  'ProjectManager',
  'User',
] as const satisfies readonly SystemRole[];

/** Problem types the API returns when it refuses a sign-in (SignInProblems in the Users module). */
export const signInProblems = {
  notInDirectory: 'urn:fiscoprojects:problem:not-in-demo-directory',
  inactive: 'urn:fiscoprojects:problem:user-inactive',
} as const;

/** Problem types of a refused logo (InstallationProblems in the Administration module). */
export const installationProblems = {
  logoFormat: 'urn:fiscoprojects:problem:logo-format',
  logoTooLarge: 'urn:fiscoprojects:problem:logo-too-large',
  logoUnsafe: 'urn:fiscoprojects:problem:logo-unsafe',
} as const;

/** Problem types of the user administration (UserProblems in the Users module). */
export const userProblems = {
  lastAdministrator: 'urn:fiscoprojects:problem:last-administrator',
} as const;

type Listener = () => void;
const unauthorizedListeners = new Set<Listener>();

/** Calls the listener whenever the API answers 401, that is, when the session ended. Returns the unsubscribe. */
export function onUnauthorized(listener: Listener): () => void {
  unauthorizedListeners.add(listener);
  return () => {
    unauthorizedListeners.delete(listener);
  };
}

const sessionEnded: Middleware = {
  onResponse({ response }) {
    if (response.status === 401) {
      for (const listener of unauthorizedListeners) {
        listener();
      }
    }
  },
};

/**
 * Client of the API, typed from the contract (ADR-0076), at the same address as the page (ADR-0054). The session
 * cookie goes with every call; the X-CSRF header is what the API asks of every change.
 */
export const api = createClient<paths>({
  baseUrl: window.location.origin,
  headers: { 'X-CSRF': '1' },
  // Looked up on each call, not kept from when the module loaded, so the tests' simulated API sees it.
  fetch: (request) => globalThis.fetch(request),
});
api.use(sessionEnded);
