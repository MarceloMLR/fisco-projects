// @vitest-environment node
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../src/test/server';
import { loadDemo, screensAddress } from './load.ts';
import { demoPeople, demoProjects } from './plan.ts';

/**
 * The API of a development installation, simulated: the local sign-in sets a session cookie with the person, and every
 * call is kept as "person METHOD path", so the tests see who did what.
 */
const base = 'http://api.test';
const cookieName = '__Host-fiscoprojects-session';
const calls: string[] = [];
const bodies = new Map<string, unknown[]>();
let localSignIn = true;
let existing: string[] = [];
let ids = 0;

const personOfEmail = (email: string) =>
  Object.entries(demoPeople).find(([, value]) => value === email)?.[0];
const idOf = (person: string) => `0199f5a2-0000-7000-8000-${person.padStart(12, '0').slice(-12)}`;
const nextId = () => `0199f5a2-1111-7000-8000-${String(++ids).padStart(12, '0')}`;

function whoCalls(request: Request): string {
  const cookie = request.headers.get('cookie') ?? '';
  return new RegExp(`${cookieName}=([a-z]+)`).exec(cookie)?.[1] ?? 'anonymous';
}

async function keep(request: Request): Promise<void> {
  const path = new URL(request.url).pathname;
  calls.push(`${whoCalls(request)} ${request.method} ${path}`);
  if (request.method !== 'GET') {
    const text = await request.clone().text();
    bodies.set(path, [...(bodies.get(path) ?? []), text === '' ? null : JSON.parse(text)]);
  }
}

beforeEach(() => {
  calls.length = 0;
  bodies.clear();
  localSignIn = true;
  existing = [];
  ids = 0;
  server.use(
    http.all(`${base}/api/*`, async ({ request }) => {
      await keep(request);
      return undefined;
    }),
    http.get(`${base}/api/session/sign-in-options`, () => HttpResponse.json({ localSignIn })),
    http.post(`${base}/api/session/local`, async ({ request }) => {
      const { email } = (await request.json()) as { email: string };
      const person = personOfEmail(email);
      if (!localSignIn || person === undefined) {
        return new HttpResponse(null, { status: 404 });
      }
      return new HttpResponse(null, {
        status: 204,
        headers: {
          'Set-Cookie': `${cookieName}=${person}; path=/; secure; samesite=strict; httponly`,
        },
      });
    }),
    http.get(`${base}/api/session`, ({ request }) => {
      const person = whoCalls(request);
      return HttpResponse.json({
        id: idOf(person),
        name: person,
        email: `${person}@empresa.example`,
        role: person === 'paulo' ? 'Administrator' : 'User',
        theme: 'System',
      });
    }),
    http.get(`${base}/api/projects`, () =>
      HttpResponse.json({
        items: existing.map((code) => ({ code })),
        total: existing.length,
        today: '2026-10-19',
      }),
    ),
    http.post(`${base}/api/*`, () => HttpResponse.json({ id: nextId() }, { status: 201 })),
    http.put(`${base}/api/*`, () => new HttpResponse(null, { status: 204 })),
  );
});

const callsBy = (pattern: RegExp) => calls.filter((call) => pattern.test(call));
const people = (pattern: RegExp) =>
  [...new Set(callsBy(pattern).map((call) => call.split(' ')[0]))].sort();

// Spec: dados-de-demonstracao, requirement "Carga dos dados de demonstração".
describe('Carga dos dados de demonstração', () => {
  it('Carregar dados de demonstração', async () => {
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    const items = demoProjects.flatMap((project) => project.phases).flatMap((phase) => phase.items);
    expect(outcome).toEqual({
      loaded: true,
      firstDay: '2026-10-05',
      projects: 5,
      items: items.length,
    });
    // Everyone of the demo directory signs in, but the people who never signed in (ADR-0078).
    expect(people(/POST \/api\/session\/local$/)).toEqual(['anonymous']);
    expect(
      (bodies.get('/api/session/local') ?? [])
        .map((body) => (body as { email: string }).email)
        .sort(),
    ).toEqual(Object.values(demoPeople).sort());
    // The Administrator gives the roles and creates the clients, the projects and their members.
    expect(callsBy(/^paulo PUT \/api\/users\/.+\/role$/)).toEqual([
      `paulo PUT /api/users/${idOf('marina')}/role`,
      `paulo PUT /api/users/${idOf('beatriz')}/role`,
    ]);
    expect(callsBy(/^paulo POST \/api\/clients$/)).toHaveLength(3);
    expect(callsBy(/^paulo POST \/api\/projects$/)).toHaveLength(5);
    expect(people(/POST \/api\/projects\/[^/]+\/members$/)).toEqual(['paulo']);
    // The manager builds the schedules, marks the milestones and changes the status.
    expect(people(/\/schedule\/(phases|items)$/)).toEqual(['marina']);
    expect(people(/\/reached$/)).toEqual(['marina']);
    expect(people(/\/status$/)).toEqual(['marina']);
    // Each person in charge updates the progress of the task; the manager, of the tasks of no one.
    expect(people(/\/progress$/)).toEqual(['carla', 'diego', 'marina', 'rafael']);
    // The dates follow the day it runs: 19/10/2026 starts the main project on 05/10/2026.
    const first = [...bodies.entries()].find(([path]) => path.endsWith('/schedule/items'))?.[1][0];
    expect(first).toMatchObject({
      name: 'Reunião de abertura',
      start: '2026-10-05',
      ownerId: idOf('marina'),
    });
    expect(lines.at(-2)).toContain('2026-10-05');
    // And where to open the screens, with the code as it is now.
    expect(screensAddress).toBe('http://localhost:5173');
    expect(lines.at(-1)).toBe('Abra as telas em http://localhost:5173 (npm run dev).');
  });

  it('Dados já carregados', async () => {
    existing = ['ERP-FIN'];
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    expect(outcome).toEqual({ loaded: false, reason: 'already-loaded' });
    expect(calls.filter((call) => !call.includes(' GET '))).toEqual([
      'anonymous POST /api/session/local',
    ]);
    expect(lines).toEqual([
      expect.stringContaining('já estão carregados'),
      'Abra as telas em http://localhost:5173 (npm run dev).',
    ]);
  });

  it('Sem entrada local', async () => {
    localSignIn = false;

    await expect(loadDemo(base)).rejects.toThrow('A entrada local está desligada');
    expect(calls).toEqual(['anonymous GET /api/session/sign-in-options']);
  });
});

// Spec: dados-de-demonstracao, requirement "Linha de base na demonstração".
describe('Linha de base na demonstração', () => {
  it('Linha de base na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager saves the baseline of ERP-FIN and then puts off Parametrizar regras de ICMS, in the same project.
    const changes = callsBy(/ PUT .*\/schedule\/(baseline|items\/[^/]+\/dates)$/);
    expect(changes).toHaveLength(2);
    const [saved, moved] = changes;
    expect(saved).toMatch(/^marina PUT \/api\/projects\/[^/]+\/schedule\/baseline$/);
    expect(moved).toMatch(/^marina PUT \/api\/projects\/[^/]+\/schedule\/items\/[^/]+\/dates$/);
    expect(moved?.startsWith(saved?.replace(/\/baseline$/, '') ?? '')).toBe(true);
    // Two days later than planned, from 13/10/2026 to 15/10/2026: the API pushes the items after it.
    const dates = [...bodies.entries()].find(([path]) => path.endsWith('/dates'))?.[1];
    expect(dates).toEqual([{ start: '2026-10-15', end: null }]);
    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    expect(main?.baseline).toBe(true);
    expect(
      main?.phases.flatMap((phase) => phase.items).filter((item) => item.movedTo !== undefined),
    ).toMatchObject([{ name: 'Parametrizar regras de ICMS', day: 8, movedTo: 10 }]);
  });
});
