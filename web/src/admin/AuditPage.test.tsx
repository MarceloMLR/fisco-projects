import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { auditPage, demoAuditEntries, server, signedInAs } from '../test/server';

const text = ptBR.audit;
const requests: URL[] = [];

beforeEach(() => {
  // 12:00:30 of October 19th in the time zone of the tests; only the date is simulated, timers stay real.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  requests.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/audit', ({ request }) => {
      const url = new URL(request.url);
      requests.push(url);
      return HttpResponse.json(auditPage(demoAuditEntries, url));
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

function lastRequest() {
  const url = requests.at(-1);
  if (url === undefined) {
    throw new Error('The screen did not ask for the audit.');
  }
  return url.searchParams;
}

function bodyRows() {
  return within(screen.getByRole('grid', { name: text.table }))
    .getAllByRole('row')
    .slice(1);
}

/** Text of an element as a screen reader says it, without what is hidden from it (the avatar initials). */
function readable(element: HTMLElement) {
  const copy = element.cloneNode(true) as HTMLElement;
  copy.querySelectorAll('[aria-hidden="true"]').forEach((hidden) => {
    hidden.remove();
  });
  return copy.textContent;
}

/** Each row as the person reads it: when, who and what, cell by cell. */
function rowTexts() {
  return bodyRows().map((row) => within(row).getAllByRole('gridcell').map(readable));
}

function rowOf(title: string) {
  const row = bodyRows().find((candidate) => candidate.textContent.includes(title));
  if (row === undefined) {
    throw new Error(`No row for ${title}.`);
  }
  return row;
}

async function openAudit(width?: number) {
  const view = await renderApp('/admin/audit', width);
  await waitFor(() => {
    expect(bodyRows()).toHaveLength(5);
  });
  return view;
}

// Spec: auditoria, requirement "Tela de Auditoria".
describe('Tela de Auditoria', () => {
  it('Lista da auditoria', async () => {
    await openAudit();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(ptBR.pages.audit.title);
    expect(rowTexts()).toEqual([
      [
        '19/1010:14',
        'Paulo Nogueira',
        `${text.actions.User.RoleChanged}Beatriz Lima · Usuário → Gerente de projeto`,
      ],
      ['19/1009:58', 'Marina Costa', `${text.actions.SignIn.SignedIn}${text.providers.local}`],
      [
        '19/1008:05',
        text.system,
        `${text.actions.SignIn.SignInRefused}joao.silva@parceiro.example · ${text.refusals.NotInDirectory}`,
      ],
      ['18/1011:47', 'Paulo Nogueira', `${text.actions.User.Deactivated}Lívia Prado`],
      ['17/1009:02', text.system, `${text.actions.User.Created}Rafael Souza`],
    ]);
    expect(screen.getByText('1–5 de 5 no período')).toBeInTheDocument();
    expect(Object.fromEntries(lastRequest())).toEqual({
      from: '2026-09-20T03:00:00.000Z',
      to: '2026-10-20T03:00:00.000Z',
      page: '1',
      pageSize: '25',
    });
    expect(
      screen.getByRole('button', { name: 'Período de 20/09/2026 a 19/10/2026' }),
    ).toHaveTextContent('20/09 – 19/10');
  });

  it('Troca de papel na lista', async () => {
    await openAudit();

    const row = rowOf(text.actions.User.RoleChanged);

    expect(within(row).getByText(text.actions.User.RoleChanged)).toBeInTheDocument();
    expect(
      within(row).getByText('Beatriz Lima · Usuário → Gerente de projeto'),
    ).toBeInTheDocument();
  });

  it('Filtro por pessoa', async () => {
    await openAudit();

    fireEvent.change(screen.getByRole('combobox', { name: text.filters.person }), {
      target: { value: 'Pau' },
    });
    fireEvent.click(await screen.findByRole('option', { name: 'Paulo Nogueira' }));

    await waitFor(() => {
      expect(lastRequest().get('actorId')).toBe('0199f5a2-7c1e-7d40-9a1b-000000000001');
    });
    await waitFor(() => {
      expect(rowTexts().map((cells) => cells[1])).toEqual(['Paulo Nogueira', 'Paulo Nogueira']);
    });
    expect(lastRequest().get('page')).toBe('1');
  });

  it('Filtro por tipo de item', async () => {
    await openAudit();

    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.filters.type }));
    fireEvent.click(await screen.findByRole('option', { name: text.itemTypes.SignIn }));

    await waitFor(() => {
      expect(bodyRows()).toHaveLength(2);
    });
    expect(lastRequest().get('itemType')).toBe('SignIn');
  });

  it('Filtro por período', async () => {
    await openAudit();

    fireEvent.click(screen.getByRole('button', { name: /^Período de/ }));
    const period = await screen.findByRole('dialog', { name: text.filters.period });
    fireEvent.change(within(period).getByLabelText(text.filters.from), {
      target: { value: '2026-09-01' },
    });
    fireEvent.change(within(period).getByLabelText(text.filters.to), {
      target: { value: '2026-10-16' },
    });

    await waitFor(() => {
      expect(lastRequest().get('to')).toBe('2026-10-17T03:00:00.000Z');
    });
    expect(lastRequest().get('from')).toBe('2026-09-01T03:00:00.000Z');
    fireEvent.keyDown(period, { key: 'Escape' });
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.filters.period })).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(rowTexts()).toEqual([
        ['10/0909:00', 'Paulo Nogueira', `${text.actions.SignIn.SignedIn}${text.providers.local}`],
      ]);
    });
    expect(screen.getByRole('button', { name: /^Período de/ })).toHaveTextContent('01/09 – 16/10');
  });

  it('Detalhes de um registro', async () => {
    await openAudit();
    const details = screen.getByRole('complementary', { name: text.details.label });

    // The first record of the page, until another one is chosen.
    expect(within(details).getByRole('heading', { level: 2 })).toHaveTextContent(
      'Paulo Nogueira trocou um papel',
    );
    expect(within(details).getByText('19/10/2026 10:14')).toBeInTheDocument();
    expect(within(details).getByText('Beatriz Lima')).toBeInTheDocument();
    const changes = within(details).getByRole('table');
    expect(
      within(changes)
        .getAllByRole('row')
        .map((row) => row.textContent),
    ).toEqual(['CampoAntesDepois', 'Papel no sistemaUsuárioGerente de projeto']);
    expect(rowOf(text.actions.User.RoleChanged)).toHaveAttribute('aria-selected', 'true');

    fireEvent.click(rowOf(text.actions.SignIn.SignInRefused));

    await waitFor(() => {
      expect(within(details).getByRole('heading', { level: 2 })).toHaveTextContent(
        'Sistema recusou uma entrada',
      );
    });
    expect(within(details).getByText('joao.silva@parceiro.example')).toBeInTheDocument();
    expect(within(details).getByText(text.refusals.NotInDirectory)).toBeInTheDocument();
    expect(within(details).queryByRole('table')).not.toBeInTheDocument();
    expect(rowOf(text.actions.SignIn.SignInRefused)).toHaveAttribute('aria-selected', 'true');
  });

  it('Detalhes no celular', async () => {
    await openAudit(phoneWidth);

    expect(
      screen.queryByRole('complementary', { name: text.details.label }),
    ).not.toBeInTheDocument();
    fireEvent.click(rowOf(text.actions.User.Deactivated));
    const details = await screen.findByRole('dialog', { name: text.details.label });

    expect(within(details).getByRole('heading', { level: 2 })).toHaveTextContent(
      'Paulo Nogueira desativou um usuário',
    );
    expect(
      within(within(details).getByRole('table'))
        .getAllByRole('row')
        .map((row) => row.textContent),
    ).toEqual(['CampoAntesDepois', 'SituaçãoAtivoInativo']);

    fireEvent.click(within(details).getByRole('button', { name: text.details.close }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    });
  });
});
