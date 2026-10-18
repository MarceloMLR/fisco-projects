import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { SessionUser } from '../api/client';

// The API, simulated for the screen tests (ADR-0056). By default Marina is signed in and the local sign-in is on;
// each test changes only what it needs with server.use(...).

export const marina: SessionUser = {
  id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70',
  name: 'Marina Costa',
  email: 'marina.costa@empresa.example',
  role: 'User',
};

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
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

/** Nobody is signed in: the API answers 401 to "who is in the session". */
export function signedOut(): void {
  server.use(http.get('/api/session', () => problem(401)));
}
