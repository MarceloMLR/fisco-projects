import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { components } from '../api/schema';
import type { Installation, Profile, SessionUser } from '../api/client';

type UserResponse = components['schemas']['UserResponse'];
type AuditEntryResponse = components['schemas']['AuditEntryResponse'];

// The API, simulated for the screen tests (ADR-0056). By default Marina is signed in and the local sign-in is on;
// each test changes only what it needs with server.use(...).

export const marina: SessionUser = {
  id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70',
  name: 'Marina Costa',
  email: 'marina.costa@empresa.example',
  role: 'User',
  theme: 'Automatic',
};

/**
 * People of the simulated installation, as the Users screen of the canvas shows them. The tests run in the time
 * zone set in vite.config.ts, where 2026-10-19T15:00:00Z is 12:00.
 */
export const demoUsers: UserResponse[] = [
  person(1, 'Paulo Nogueira', 'Administrator', 'Active', '2026-10-19T15:00:00Z'),
  person(2, 'Marina Costa', 'ProjectManager', 'Active', '2026-10-19T12:12:00Z'),
  person(3, 'Beatriz Lima', 'ProjectManager', 'Active', '2026-10-19T11:47:00Z'),
  person(4, 'Rafael Souza', 'User', 'Active', '2026-10-18T20:30:00Z'),
  person(5, 'Lívia Prado', 'User', 'Inactive', '2026-08-28T18:00:00Z'),
];

function person(
  index: number,
  name: string,
  role: UserResponse['role'],
  status: UserResponse['status'],
  lastSignInAt: string,
): UserResponse {
  const email = `${name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(' ', '.')}@empresa.example`;
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-00000000000${String(index)}`,
    name,
    email,
    role,
    status,
    lastSignInAt,
  };
}

const withoutAccents = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();

/** The list of users as the API answers it: search without accents, filters and pages, in alphabetical order. */
export function usersPage(users: readonly UserResponse[], url: URL) {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const role = url.searchParams.get('role');
  const status = url.searchParams.get('status');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = users
    .filter(
      (user) =>
        withoutAccents(user.name).includes(search) || withoutAccents(user.email).includes(search),
    )
    .filter((user) => role === null || user.role === role)
    .filter((user) => status === null || user.status === status)
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

const paulo = { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' };
const marinaActor = { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' };

function auditEntry(
  index: number,
  occurredAt: string,
  actor: AuditEntryResponse['actor'],
  record: Pick<AuditEntryResponse, 'action' | 'itemType'> & Partial<AuditEntryResponse>,
): AuditEntryResponse {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-1000000000${String(index).padStart(2, '0')}`,
    occurredAt,
    actor,
    itemId: null,
    itemLabel: null,
    detail: null,
    changes: [],
    ...record,
  };
}

/**
 * Audit records of the simulated installation, newest first, as in the Audit screen of the canvas. In the time
 * zone of the tests (vite.config.ts), 2026-10-19T13:14:00Z is 10:14 of October 19th.
 */
export const demoAuditEntries: AuditEntryResponse[] = [
  auditEntry(1, '2026-10-19T13:14:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000003',
    itemLabel: 'Beatriz Lima',
    changes: [{ field: 'Role', before: 'User', after: 'ProjectManager', secret: false }],
  }),
  auditEntry(2, '2026-10-19T12:58:00Z', marinaActor, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: marinaActor.id,
    detail: 'local',
  }),
  auditEntry(3, '2026-10-19T11:05:00Z', null, {
    action: 'SignInRefused',
    itemType: 'SignIn',
    itemLabel: 'joao.silva@parceiro.example',
    detail: 'NotInDirectory',
  }),
  auditEntry(4, '2026-10-18T14:47:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000005',
    itemLabel: 'Lívia Prado',
    changes: [{ field: 'IsActive', before: 'true', after: 'false', secret: false }],
  }),
  auditEntry(5, '2026-10-17T12:02:00Z', null, {
    action: 'Created',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000004',
    itemLabel: 'Rafael Souza',
    changes: [
      { field: 'Email', before: null, after: 'rafael.souza@empresa.example', secret: false },
      { field: 'IsActive', before: null, after: 'true', secret: false },
      { field: 'Name', before: null, after: 'Rafael Souza', secret: false },
      { field: 'Role', before: null, after: 'User', secret: false },
    ],
  }),
  auditEntry(6, '2026-09-10T12:00:00Z', paulo, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: paulo.id,
    detail: 'local',
  }),
];

/** The audit list as the API answers it: by author, item type and period (end excluded), newest first, in pages. */
export function auditPage(entries: readonly AuditEntryResponse[], url: URL) {
  const actorId = url.searchParams.get('actorId');
  const itemType = url.searchParams.get('itemType');
  const from = url.searchParams.get('from');
  const to = url.searchParams.get('to');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = entries
    .filter((entry) => actorId === null || entry.actor?.id === actorId)
    .filter((entry) => itemType === null || entry.itemType === itemType)
    .filter((entry) => from === null || Date.parse(entry.occurredAt) >= Date.parse(from))
    .filter((entry) => to === null || Date.parse(entry.occurredAt) < Date.parse(to))
    .toSorted((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

/** The brand of the simulated installation: the name nobody changed yet, and no logo. */
export const defaultInstallation: Installation = { name: 'FiscoProjects', logoUrl: null };

/** My profile of a person in the session, who signs in locally and last did so at 09:12 of October 19th. */
export function profileOf(person: SessionUser): Profile {
  return { ...person, lastSignInAt: '2026-10-19T12:12:00Z', signInMethods: ['local'] };
}

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
  http.get('/api/users', ({ request }) =>
    HttpResponse.json(usersPage(demoUsers, new URL(request.url))),
  ),
  http.get('/api/audit', ({ request }) =>
    HttpResponse.json(auditPage(demoAuditEntries, new URL(request.url))),
  ),
  http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn: true })),
  http.post('/api/session/sign-out', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/profile', () => HttpResponse.json(profileOf(marina))),
  http.put('/api/profile/theme', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/installation', () => HttpResponse.json(defaultInstallation)),
  http.get('/api/installation/rules', () => HttpResponse.json({ timeZone: 'America/Sao_Paulo' })),
];

export const server = setupServer(...handlers);

export function problem(status: number, type?: string) {
  return HttpResponse.json(
    { status, title: 'Problem', ...(type === undefined ? {} : { type }) },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  );
}

/** Marina is signed in with the given role in the system. */
export function signedInAs(role: SessionUser['role']): void {
  server.use(http.get('/api/session', () => HttpResponse.json({ ...marina, role })));
}

/** Nobody is signed in: the API answers 401 to "who is in the session". */
export function signedOut(): void {
  server.use(http.get('/api/session', () => problem(401)));
}
