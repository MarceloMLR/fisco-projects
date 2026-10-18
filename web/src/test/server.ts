import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { components } from '../api/schema';
import type { SessionUser } from '../api/client';

type UserResponse = components['schemas']['UserResponse'];

// The API, simulated for the screen tests (ADR-0056). By default Marina is signed in and the local sign-in is on;
// each test changes only what it needs with server.use(...).

export const marina: SessionUser = {
  id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70',
  name: 'Marina Costa',
  email: 'marina.costa@empresa.example',
  role: 'User',
};

/** People of the simulated installation, as the Users screen of the canvas shows them. */
export const demoUsers: UserResponse[] = [
  person(1, 'Paulo Nogueira', 'Administrator', 'Active', '2026-10-19T12:00:00Z'),
  person(2, 'Marina Costa', 'ProjectManager', 'Active', '2026-10-19T09:12:00Z'),
  person(3, 'Beatriz Lima', 'ProjectManager', 'Active', '2026-10-19T08:47:00Z'),
  person(4, 'Rafael Souza', 'User', 'Active', '2026-10-18T17:30:00Z'),
  person(5, 'Lívia Prado', 'User', 'Inactive', '2026-08-28T15:00:00Z'),
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

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
  http.get('/api/users', ({ request }) =>
    HttpResponse.json(usersPage(demoUsers, new URL(request.url))),
  ),
  http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn: true })),
  http.post('/api/session/sign-out', () => new HttpResponse(null, { status: 204 })),
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
