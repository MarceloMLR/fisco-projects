import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { UserRow } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoUsers, problem, server, signedInAs, usersPage } from '../test/server';

const text = ptBR.users;

/** The people of this test, changed by the simulated API as the real one would. */
let people: UserRow[];
const requests: { method: string; path: string; body: unknown; csrf: string | null }[] = [];

function find(id: string) {
  const person = people.find((candidate) => candidate.id === id);
  if (person === undefined) {
    throw new Error(`Unknown person ${id}.`);
  }
  return person;
}

async function record(request: Request) {
  const url = new URL(request.url);
  requests.push({
    method: request.method,
    path: url.pathname,
    body: request.method === 'PUT' ? await request.clone().json() : undefined,
    csrf: request.headers.get('X-CSRF'),
  });
}

beforeEach(() => {
  // 12:00:30 in the time zone of the tests (vite.config.ts); only the date is simulated, timers stay real.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  people = structuredClone(demoUsers);
  requests.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/users', ({ request }) =>
      HttpResponse.json(usersPage(people, new URL(request.url))),
    ),
    http.put('/api/users/:id/role', async ({ request, params }) => {
      await record(request);
      find(String(params.id)).role = (
        (await request.clone().json()) as { role: UserRow['role'] }
      ).role;
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/users/:id/deactivate', async ({ request, params }) => {
      await record(request);
      find(String(params.id)).status = 'Inactive';
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/users/:id/reactivate', async ({ request, params }) => {
      await record(request);
      find(String(params.id)).status = 'Active';
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

function bodyRows() {
  return within(table()).getAllByRole('row').slice(1);
}

/** The people in the table, by e-mail, in the order shown. */
function emails() {
  return bodyRows().map((row) => within(row).getByText(/@empresa\.example$/).textContent);
}

function rowOf(name: string) {
  const row = bodyRows().find((candidate) => candidate.textContent.includes(name));
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

async function openUsers() {
  const view = await renderApp('/admin/users');
  await waitFor(() => {
    expect(emails()).toHaveLength(people.length);
  });
  return view;
}

async function openMenuOf(name: string) {
  fireEvent.click(
    within(rowOf(name)).getByRole('button', { name: text.menu.button.replace('{{name}}', name) }),
  );
  return screen.findByRole('menu', { name: text.menu.button.replace('{{name}}', name) });
}

// Spec: usuarios-e-papeis, requirement "Tela Usuários".
describe('Tela Usuários', () => {
  // Spec: usuarios-e-papeis, requirement "Tela Usuários".
  it('Pessoa que nunca entrou', async () => {
    people.push({
      id: '0199f5a2-7c1e-7d40-9a1b-000000000009',
      name: 'Carla Mendes',
      email: 'carla.mendes@empresa.example',
      role: 'User',
      status: 'Active',
      lastSignInAt: null,
    });
    await openUsers();

    expect(within(rowOf('Carla Mendes')).getAllByRole('gridcell')[3]).toHaveTextContent(
      ptBR.dates.never,
    );
  });

  it('Lista de usuários', async () => {
    await openUsers();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      `${ptBR.pages.users.title}5`,
    );
    expect(emails()).toEqual([
      'beatriz.lima@empresa.example',
      'livia.prado@empresa.example',
      'marina.costa@empresa.example',
      'paulo.nogueira@empresa.example',
      'rafael.souza@empresa.example',
    ]);
    expect(rowOf('Paulo Nogueira')).toHaveTextContent('PNPaulo Nogueira');
    const cells = (name: string) =>
      within(rowOf(name))
        .getAllByRole('gridcell')
        .slice(1, 4)
        .map((cell) => cell.textContent);
    expect(cells('Paulo Nogueira')).toEqual([
      text.roles.Administrator,
      text.status.Active,
      'Agora',
    ]);
    expect(cells('Marina Costa')).toEqual([
      text.roles.ProjectManager,
      text.status.Active,
      ptBR.dates.today.replace('{{time}}', '09:12'),
    ]);
    expect(cells('Rafael Souza')).toEqual([
      text.roles.User,
      text.status.Active,
      ptBR.dates.yesterday.replace('{{time}}', '17:30'),
    ]);
    expect(cells('Lívia Prado')).toEqual([text.roles.User, text.status.Inactive, '28/08/2026']);
    expect(rowOf('Lívia Prado')).toHaveClass('row-muted');
    expect(rowOf('Paulo Nogueira')).not.toHaveClass('row-muted');
  });

  it('Busca por nome ou e-mail', async () => {
    await openUsers();
    const search = screen.getByRole('searchbox', { name: text.search });

    fireEvent.change(search, { target: { value: 'LIVIA' } });
    await waitFor(() => {
      expect(emails()).toEqual(['livia.prado@empresa.example']);
    });

    fireEvent.change(search, { target: { value: 'rafael.sou' } });
    await waitFor(() => {
      expect(emails()).toEqual(['rafael.souza@empresa.example']);
    });
  });

  it('Filtro por papel e situação', async () => {
    await openUsers();

    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.filters.role }));
    fireEvent.click(await screen.findByRole('option', { name: text.roles.ProjectManager }));
    await waitFor(() => {
      expect(emails()).toEqual(['beatriz.lima@empresa.example', 'marina.costa@empresa.example']);
    });

    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.filters.status }));
    fireEvent.click(await screen.findByRole('option', { name: text.status.Inactive }));
    expect(await screen.findByText(text.empty)).toBeInTheDocument();
  });

  it('Trocar o papel na tela', async () => {
    await openUsers();

    const menu = await openMenuOf('Rafael Souza');
    expect(within(menu).getByRole('menuitemradio', { name: text.roles.User })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.roles.ProjectManager }));

    expect(
      await screen.findByText(
        text.messages.roleChanged
          .replace('{{name}}', 'Rafael Souza')
          .replace('{{role}}', text.roles.ProjectManager),
      ),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(within(rowOf('Rafael Souza')).getAllByRole('gridcell')[1]).toHaveTextContent(
        text.roles.ProjectManager,
      );
    });
    expect(requests).toEqual([
      {
        method: 'PUT',
        path: `/api/users/${demoUsers[3]?.id ?? ''}/role`,
        body: { role: 'ProjectManager' },
        csrf: '1',
      },
    ]);
  });

  // Spec: usuarios-e-papeis, requirement "Papel no sistema" (ADR-0097).
  it('Papel Líder técnico', async () => {
    await openUsers();

    const menu = await openMenuOf('Rafael Souza');
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.roles.TechLead }));

    await waitFor(() => {
      expect(within(rowOf('Rafael Souza')).getAllByRole('gridcell')[1]).toHaveTextContent(
        text.roles.TechLead,
      );
    });
    expect(requests).toEqual([
      expect.objectContaining({
        path: `/api/users/${demoUsers[3]?.id ?? ''}/role`,
        body: { role: 'TechLead' },
      }),
    ]);
    // The filter by role offers it too.
    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.filters.role }));
    expect(await screen.findByRole('option', { name: text.roles.TechLead })).toBeInTheDocument();
  });

  it('Desativar e reativar na tela', async () => {
    await openUsers();

    fireEvent.click(
      within(await openMenuOf('Marina Costa')).getByRole('menuitem', {
        name: text.menu.deactivate,
      }),
    );
    expect(
      await screen.findByText(text.messages.deactivated.replace('{{name}}', 'Marina Costa')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(rowOf('Marina Costa')).toHaveClass('row-muted');
    });
    expect(within(rowOf('Marina Costa')).getAllByRole('gridcell')[2]).toHaveTextContent(
      text.status.Inactive,
    );

    const menu = await openMenuOf('Marina Costa');
    expect(
      within(menu).queryByRole('menuitem', { name: text.menu.deactivate }),
    ).not.toBeInTheDocument();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.menu.reactivate }));
    await waitFor(() => {
      expect(rowOf('Marina Costa')).not.toHaveClass('row-muted');
    });
    expect(requests.map((request) => request.path)).toEqual([
      `/api/users/${demoUsers[1]?.id ?? ''}/deactivate`,
      `/api/users/${demoUsers[1]?.id ?? ''}/reactivate`,
    ]);
  });

  it('Recusa do último administrador na tela', async () => {
    server.use(
      http.put('/api/users/:id/role', () =>
        problem(409, 'urn:fiscoprojects:problem:last-administrator'),
      ),
    );
    await openUsers();

    const menu = await openMenuOf('Paulo Nogueira');
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.roles.User }));

    expect(await screen.findByText(text.messages.lastAdministrator)).toBeInTheDocument();
    expect(within(rowOf('Paulo Nogueira')).getAllByRole('gridcell')[1]).toHaveTextContent(
      text.roles.Administrator,
    );
  });
});

// Spec: aplicacao-web, requirement "Menus fecham na hora".
describe('Menus fecham na hora', () => {
  // jsdom lays nothing out; with a height, the menu of MUI would take its usual time to leave.
  const height = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
  afterEach(() => {
    if (height !== undefined) {
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', height);
    }
  });

  it('Escolha num menu de ações', async () => {
    await openUsers();
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
      configurable: true,
      get: () => 200,
    });
    const menu = await openMenuOf('Marina Costa');
    const paper = menu.closest('.MuiPaper-root');

    // Choosing the role she already has only closes the menu.
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.roles.ProjectManager }));

    expect((paper as HTMLElement | null)?.style.transition).toMatch(/^opacity 0ms/);
    await act(() => new Promise<void>((resolve) => setTimeout(resolve, 20)));
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
  });
});
