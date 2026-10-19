import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { delay, http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuditEntry, UserRow } from '../api/client';
import { queryKeys } from '../api/queries';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { auditPage, demoAuditEntries, demoUsers, marina, server, usersPage } from '../test/server';

/** The people and the audit of this test, changed by the simulated API as the real one would. */
let people: UserRow[];
let entries: AuditEntry[];
/** Calls to the lists, in order: which list and with which filters. */
const calls: URL[] = [];
/** How long the simulated API takes to answer each list, in milliseconds. */
const wait = { users: 0, audit: 0 };

const administrator = { ...marina, role: 'Administrator' as const };

beforeEach(() => {
  // 12:00:30 of October 19th in the time zone of the tests; only the date is simulated, timers stay real.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  people = structuredClone(demoUsers);
  entries = structuredClone(demoAuditEntries);
  calls.length = 0;
  wait.users = 0;
  wait.audit = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(administrator)),
    http.get('/api/users', async ({ request }) => {
      const url = new URL(request.url);
      calls.push(url);
      await delay(wait.users);
      return HttpResponse.json(usersPage(people, url));
    }),
    http.get('/api/audit', async ({ request }) => {
      const url = new URL(request.url);
      calls.push(url);
      await delay(wait.audit);
      return HttpResponse.json(auditPage(entries, url));
    }),
    http.put('/api/users/:id/role', async ({ request, params }) => {
      const person = people.find((candidate) => candidate.id === params.id);
      if (person === undefined) {
        return new HttpResponse(null, { status: 404 });
      }
      const { role } = (await request.json()) as { role: UserRow['role'] };
      entries.unshift(roleChange(person, role));
      person.role = role;
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

/** The record the API writes when the role of a person changes, made from the newest demo record (a role change). */
function roleChange(person: UserRow, role: UserRow['role']): AuditEntry {
  const [newest] = demoAuditEntries;
  if (newest === undefined) {
    throw new Error('The demo audit has no records.');
  }
  return {
    ...structuredClone(newest),
    id: '0199f5a2-7c1e-7d40-9a1b-0000000000ff',
    occurredAt: '2026-10-19T15:00:00Z',
    itemId: person.id,
    itemLabel: person.name,
    changes: [{ field: 'Role', before: person.role, after: role, secret: false, reference: null }],
  };
}

/** Waits a real while, as a person would see the screen. */
function pause(ms: number) {
  return act(() => new Promise<void>((resolve) => setTimeout(resolve, ms)));
}

function callsTo(path: string, filter: (url: URL) => boolean = () => true) {
  return calls.filter((url) => url.pathname === path && filter(url)).length;
}

const isList = (url: URL) => url.searchParams.get('pageSize') === '25';
const isCount = (url: URL) => url.searchParams.get('pageSize') === '1';

function grid(name: string) {
  return screen.getByRole('grid', { name });
}

function rowsOf(name: string) {
  return within(grid(name)).queryAllByRole('row').slice(1);
}

function section(name: string) {
  fireEvent.click(
    within(screen.getByRole('navigation', { name: ptBR.admin.sections.label })).getByRole('link', {
      name: new RegExp(`^${name}`),
    }),
  );
}

async function openUsers() {
  const view = await renderApp('/admin/users');
  await waitFor(() => {
    expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
  });
  return view;
}

// Spec: aplicacao-web, requirement "Dados da API guardados durante a sessão".
describe('Dados da API guardados durante a sessão', () => {
  it('Volta a uma lista já vista', async () => {
    await openUsers();
    section(ptBR.pages.audit.title);
    await waitFor(() => {
      expect(rowsOf(ptBR.audit.table)).toHaveLength(5);
    });
    const before = callsTo('/api/users', isList);
    wait.users = 1000;

    section(ptBR.pages.users.title);

    // The people of the last time, at once, with no indicator, long before the API answers.
    await pause(50);
    expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
    expect(within(grid(ptBR.users.table)).queryByRole('progressbar')).not.toBeInTheDocument();
    // And the list is asked again, to bring it up to date.
    expect(callsTo('/api/users', isList)).toBe(before + 1);

    // The answer takes long: past 300 ms, the bar says the list is being brought up to date, over the same rows.
    await waitFor(() => {
      expect(within(grid(ptBR.users.table)).getByRole('progressbar')).toBeInTheDocument();
    });
    expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
    // And it leaves when the answer arrives.
    await waitFor(
      () => {
        expect(within(grid(ptBR.users.table)).queryByRole('progressbar')).not.toBeInTheDocument();
      },
      { timeout: 2000 },
    );
  });

  it('Alteração atualiza as listas', async () => {
    await openUsers();
    section(ptBR.pages.audit.title);
    await waitFor(() => {
      expect(rowsOf(ptBR.audit.table)).toHaveLength(5);
    });
    section(ptBR.pages.users.title);
    await waitFor(() => {
      expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
    });
    const lists = callsTo('/api/users', isList);
    const counts = callsTo('/api/users', isCount);

    const row = rowsOf(ptBR.users.table).find((candidate) =>
      candidate.textContent.includes('Rafael Souza'),
    );
    fireEvent.click(
      within(row ?? document.body).getByRole('button', {
        name: ptBR.users.menu.button.replace('{{name}}', 'Rafael Souza'),
      }),
    );
    fireEvent.click(
      await screen.findByRole('menuitemradio', { name: ptBR.users.roles.ProjectManager }),
    );

    // The list and the count are asked again, and the list shows the new role.
    await waitFor(() => {
      expect(callsTo('/api/users', isList)).toBe(lists + 1);
    });
    expect(callsTo('/api/users', isCount)).toBe(counts + 1);
    await waitFor(() => {
      expect(
        rowsOf(ptBR.users.table).find((candidate) =>
          candidate.textContent.includes('Rafael Souza'),
        ),
      ).toHaveTextContent(ptBR.users.roles.ProjectManager);
    });

    // The Audit, opened again, shows the record of the change.
    section(ptBR.pages.audit.title);
    await waitFor(() => {
      expect(rowsOf(ptBR.audit.table)).toHaveLength(6);
    });
    expect(rowsOf(ptBR.audit.table)[0]).toHaveTextContent('Rafael Souza');
  });

  it('Saída descarta os dados guardados', async () => {
    const { queryClient } = await openUsers();
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
    server.use(
      http.post('/api/session/sign-out', () => {
        server.use(http.get('/api/session', () => new HttpResponse(null, { status: 401 })));
        return new HttpResponse(null, { status: 204 });
      }),
    );

    fireEvent.click(
      screen.getByRole('button', {
        name: ptBR.shell.account.replace('{{name}}', administrator.name),
      }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: ptBR.shell.signOut }));
    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.signIn.title }),
    ).toBeInTheDocument();

    // Nothing of the session is kept for whoever signs in next; the sign-in page reads only the brand again.
    expect(
      queryClient
        .getQueryCache()
        .getAll()
        .map((query) => query.queryKey),
    ).toEqual([queryKeys.installation]);

    server.use(
      http.post('/api/session/local', () => {
        server.use(http.get('/api/session', () => HttpResponse.json(administrator)));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    wait.users = 200;
    const before = callsTo('/api/users', isList);
    fireEvent.change(screen.getByLabelText(ptBR.signIn.email), {
      target: { value: administrator.email },
    });
    fireEvent.click(screen.getByRole('button', { name: ptBR.signIn.submit }));

    // Back in the Users screen, the list is asked again, and no row of before shows while it is on its way.
    await screen.findByRole('grid', { name: ptBR.users.table });
    expect(rowsOf(ptBR.users.table)).toHaveLength(0);
    await waitFor(() => {
      expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
    });
    expect(callsTo('/api/users', isList)).toBe(before + 1);
  });

  it('Dados só na memória', async () => {
    await openUsers();
    section(ptBR.pages.audit.title);
    await waitFor(() => {
      expect(rowsOf(ptBR.audit.table)).toHaveLength(5);
    });

    const stored = [window.localStorage, window.sessionStorage].flatMap((storage) =>
      Array.from({ length: storage.length }, (_, index) => {
        const key = storage.key(index) ?? '';
        return `${key}=${storage.getItem(key) ?? ''}`;
      }),
    );
    expect(window.sessionStorage).toHaveLength(0);
    for (const item of stored) {
      expect(item).not.toMatch(/@empresa\.example|Rafael|Beatriz|users|audit/i);
    }
  });
});

// Spec: aplicacao-web, requirement "Espera nas listas".
describe('Espera nas listas', () => {
  it('Resposta rápida sem indicador', async () => {
    await openUsers();
    wait.users = 150;

    fireEvent.mouseDown(screen.getByRole('combobox', { name: ptBR.users.filters.role }));
    fireEvent.click(await screen.findByRole('option', { name: ptBR.users.roles.Administrator }));

    // While the answer is on its way, the rows of before stay, with no indicator.
    await pause(60);
    expect(rowsOf(ptBR.users.table)).toHaveLength(people.length);
    expect(within(grid(ptBR.users.table)).queryByRole('progressbar')).not.toBeInTheDocument();

    // Then the new rows, still with no indicator.
    await waitFor(() => {
      expect(rowsOf(ptBR.users.table)).toHaveLength(
        people.filter((person) => person.role === 'Administrator').length,
      );
    });
    expect(within(grid(ptBR.users.table)).queryByRole('progressbar')).not.toBeInTheDocument();
  });
});
