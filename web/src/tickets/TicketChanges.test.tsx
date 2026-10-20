import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket, TicketRow } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoPeople, demoTicket, demoTickets, server, signedInAs } from '../test/server';

const text = ptBR.tickets;
let rows: TicketRow[];
const assigned: unknown[] = [];

function rowOf(title: string): HTMLElement {
  const row = screen.getByText(title).closest('[role="row"]');
  if (!(row instanceof HTMLElement)) {
    throw new Error(`No row of ${title}.`);
  }
  return row;
}

beforeEach(() => {
  // The manager may change the assignee and the requester of every ticket of the list.
  rows = demoTickets().map((row) => ({
    ...row,
    canChangeAssignee: true,
    canChangeRequester: true,
  }));
  assigned.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/tickets', () => HttpResponse.json({ items: rows, total: rows.length })),
    http.get('/api/tickets/assignee-choices', () =>
      HttpResponse.json([demoPeople.diego, demoPeople.marina]),
    ),
    http.put('/api/tickets/assignee', async ({ request }) => {
      const body = (await request.json()) as { numbers: number[]; assigneeId: string };
      assigned.push(body);
      rows = rows.map((row) =>
        body.numbers.includes(row.number) ? { ...row, assignee: demoPeople.diego } : row,
      );
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

// Spec: chamados, requirement "Trocas pela tela".
describe('Trocas pela tela', () => {
  it('Atribuir em lote pela tela', async () => {
    await renderApp('/tickets');
    const [first, second] = rows;
    if (first === undefined || second === undefined) {
      throw new Error('No tickets.');
    }
    await screen.findByText(first.title);

    fireEvent.click(within(rowOf(first.title)).getByRole('checkbox'));
    fireEvent.click(within(rowOf(second.title)).getByRole('checkbox'));
    const bar = screen.getByRole('region', { name: text.bulk.label });
    expect(within(bar).getByText('2 selecionados')).toBeInTheDocument();
    fireEvent.click(within(bar).getByRole('button', { name: text.bulk.assign }));
    const dialog = await screen.findByRole('dialog', { name: 'Atribuir 2 chamados' });
    fireEvent.mouseDown(
      await within(dialog).findByRole('combobox', { name: text.people.assignee }),
    );
    fireEvent.click(await screen.findByRole('option', { name: 'Diego Torres' }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.people.save }));

    expect(await screen.findByText('2 chamados atribuídos a Diego Torres.')).toBeInTheDocument();
    expect(assigned).toEqual([
      { numbers: [first.number, second.number], assigneeId: demoPeople.diego.id },
    ]);
    await waitFor(() => {
      expect(within(rowOf(first.title)).getByText('Diego Torres')).toBeInTheDocument();
    });
    expect(screen.queryByRole('region', { name: text.bulk.label })).not.toBeInTheDocument();
  });

  it('Atribuir pelo link da linha', async () => {
    await renderApp('/tickets');
    const [first] = rows;
    if (first === undefined) {
      throw new Error('No tickets.');
    }
    await screen.findByText(first.title);

    fireEvent.click(within(rowOf(first.title)).getByRole('button', { name: text.assignLink }));
    const dialog = await screen.findByRole('dialog', { name: text.people.assignTitle_one });
    fireEvent.mouseDown(
      await within(dialog).findByRole('combobox', { name: text.people.assignee }),
    );
    fireEvent.click(await screen.findByRole('option', { name: 'Diego Torres' }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.people.save }));

    expect(await screen.findByText('Chamado atribuído a Diego Torres.')).toBeInTheDocument();
    expect(assigned).toEqual([{ numbers: [first.number], assigneeId: demoPeople.diego.id }]);
  });

  it('Sem seleção para quem não troca', async () => {
    rows = rows.map((row) => ({ ...row, canChangeAssignee: false, canChangeRequester: false }));
    await renderApp('/tickets');
    await screen.findByText(rows[0]?.title ?? '');

    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.assignLink })).not.toBeInTheDocument();
  });

  it('Transferir pela tela', async () => {
    const [, rejection] = demoTickets();
    if (rejection === undefined) {
      throw new Error('No #1.');
    }
    let ticket: Ticket = {
      ...demoTicket(rejection),
      status: 'InProgress',
      assignee: demoPeople.diego,
      permissions: {
        canSend: true,
        canAttachToDescription: false,
        actions: [],
        canChangePriority: true,
        canChangeAssignee: true,
        canChangeRequester: true,
        canTransfer: true,
        canRegisterVendorTicket: false,
      },
    };
    const moved: unknown[] = [];
    server.use(
      http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
      http.put('/api/tickets/1/team', async ({ request }) => {
        moved.push(await request.json());
        ticket = { ...ticket, team: 'Functional', assignee: null };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await renderApp('/tickets/1');
    await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });

    fireEvent.click(screen.getByRole('button', { name: text.actionMenu }));
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Transferir para Funcional' }));
    const dialog = await screen.findByRole('dialog', { name: 'Transferir para Funcional' });
    fireEvent.click(within(dialog).getByRole('button', { name: text.transfer.confirm }));

    expect(await screen.findByText('Chamado transferido para Funcional.')).toBeInTheDocument();
    expect(moved).toEqual([{ team: 'Functional' }]);
    await waitFor(() => {
      expect(screen.getAllByText(text.teams.Functional).length).toBeGreaterThan(0);
    });
    expect(screen.getAllByText(text.noAssignee).length).toBeGreaterThan(0);
  });
});
