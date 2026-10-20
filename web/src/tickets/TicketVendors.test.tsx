import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket, TicketEvent, TicketRow, VendorTicket } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoPeople, demoTicket, demoTickets, problem, server, signedInAs } from '../test/server';

const text = ptBR.tickets.vendors;
let ticket: Ticket;
const registrations: unknown[] = [];
const updates: unknown[] = [];

const record: VendorTicket = {
  id: '0199f5a2-7c1e-7d40-9a1b-910000000001',
  vendorId: '0199f5a2-7c1e-7d40-9a1b-900000000001',
  vendor: 'Emissor Exemplo',
  code: 'FX-20931',
  openedOn: '2026-10-19',
  status: 'InAnalysis',
  link: 'https://suporte.emissor.example/FX-20931',
  note: null,
  registeredById: demoPeople.diego.id,
  registeredAt: '2026-10-19T19:55:00Z',
  updatedAt: '2026-10-19T19:55:00Z',
  staleDays: null,
};

const event = (fields: Partial<TicketEvent>): TicketEvent => ({
  id: '0199f5a2-7c1e-7d40-9a1b-920000000001',
  kind: 'VendorTicketRegistered',
  actorId: demoPeople.diego.id,
  from: null,
  to: null,
  action: null,
  assigneeId: null,
  fromPriority: null,
  toPriority: null,
  fromTeam: null,
  toTeam: null,
  fromPersonId: null,
  toPersonId: null,
  vendorTicketId: record.id,
  fromVendorStatus: null,
  toVendorStatus: 'InAnalysis',
  note: null,
  at: '2026-10-19T19:55:00Z',
  ...fields,
});

function serving(canRegister: boolean) {
  const [, rejection] = demoTickets();
  if (rejection === undefined) {
    throw new Error('No #1.');
  }
  ticket = {
    ...demoTicket(rejection),
    status: 'InProgress',
    assignee: demoPeople.diego,
    permissions: { ...demoTicket(rejection).permissions, canRegisterVendorTicket: canRegister },
  };
}

beforeEach(() => {
  registrations.length = 0;
  updates.length = 0;
  serving(true);
  signedInAs('User');
  server.use(
    http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
    http.post('/api/tickets/1/vendor-tickets', async ({ request }) => {
      const body = (await request.json()) as { code: string; status: VendorTicket['status'] };
      registrations.push(body);
      ticket = {
        ...ticket,
        vendorTickets: [{ ...record, code: body.code, status: body.status }],
        events: [event({ toVendorStatus: body.status })],
      };
      return HttpResponse.json({ id: record.id }, { status: 201 });
    }),
    http.put('/api/tickets/1/vendor-tickets/:id', async ({ request }) => {
      const body = (await request.json()) as { status: VendorTicket['status']; note: string };
      updates.push(body);
      ticket = {
        ...ticket,
        vendorTickets: ticket.vendorTickets.map((item) => ({ ...item, status: body.status })),
        events: [
          ...ticket.events,
          event({
            id: '0199f5a2-7c1e-7d40-9a1b-920000000002',
            kind: 'VendorStatusChanged',
            fromVendorStatus: 'InAnalysis',
            toVendorStatus: body.status,
            note: body.note,
            at: '2026-10-19T20:30:00Z',
          }),
        ],
      };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openTicket() {
  await renderApp('/tickets/1');
  await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });
  return screen.getByRole('region', { name: text.title });
}

// Spec: chamados, requirement "Fornecedores pela tela".
describe('Fornecedores pela tela', () => {
  it('Registrar pela tela', async () => {
    const section = await openTicket();
    expect(within(section).getByText(text.none)).toBeInTheDocument();

    fireEvent.click(within(section).getByRole('button', { name: text.register.button }));
    const dialog = await screen.findByRole('dialog', { name: text.register.title });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.fields.vendor }));
    // Only the active vendors.
    expect(screen.queryByRole('option', { name: 'Arquivo Fiscal Exemplo' })).toBeNull();
    fireEvent.click(await screen.findByRole('option', { name: 'Emissor Exemplo' }));
    fireEvent.change(within(dialog).getByLabelText(text.fields.code), {
      target: { value: 'FX-20931' },
    });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.fields.status }));
    fireEvent.click(await screen.findByRole('option', { name: text.statuses.InAnalysis }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.register.confirm }));

    await waitFor(() => {
      expect(registrations).toHaveLength(1);
    });
    expect(registrations[0]).toMatchObject({
      vendorId: '0199f5a2-7c1e-7d40-9a1b-900000000001',
      code: 'FX-20931',
      status: 'InAnalysis',
      link: null,
      note: null,
    });
    const item = await within(section).findByRole('listitem', {
      name: 'Chamado FX-20931 no fornecedor Emissor Exemplo',
    });
    expect(within(item).getByText('Em análise')).toBeInTheDocument();
    expect(
      await screen.findByText('registrou o chamado FX-20931 no fornecedor Emissor Exemplo'),
    ).toBeInTheDocument();
    expect(
      await screen.findByText('Chamado FX-20931 registrado no fornecedor Emissor Exemplo.'),
    ).toBeInTheDocument();
  });

  it('Número obrigatório', async () => {
    const section = await openTicket();
    fireEvent.click(within(section).getByRole('button', { name: text.register.button }));
    const dialog = await screen.findByRole('dialog', { name: text.register.title });

    fireEvent.click(within(dialog).getByRole('button', { name: text.register.confirm }));

    expect(await within(dialog).findByText(text.errors.vendorRequired)).toBeInTheDocument();
    expect(within(dialog).getByText(text.errors.codeRequired)).toBeInTheDocument();
    expect(registrations).toHaveLength(0);
  });

  it('Atualizar o status no fornecedor', async () => {
    ticket = { ...ticket, vendorTickets: [record], events: [event({})] };
    const section = await openTicket();

    fireEvent.click(within(section).getByRole('button', { name: 'Atualizar o chamado FX-20931' }));
    const dialog = await screen.findByRole('dialog', { name: text.update.title });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.fields.status }));
    fireEvent.click(await screen.findByRole('option', { name: text.statuses.WaitingForUs }));
    fireEvent.change(within(dialog).getByLabelText(/Anotação/), {
      target: { value: 'Pediram o XML da nota rejeitada.' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.update.confirm }));

    await waitFor(() => {
      expect(updates).toEqual([
        { status: 'WaitingForUs', note: 'Pediram o XML da nota rejeitada.' },
      ]);
    });
    expect(await within(section).findByText(text.statuses.WaitingForUs)).toBeInTheDocument();
    expect(
      await screen.findByText(
        'atualizou o chamado FX-20931 no Emissor Exemplo · Em análise → Aguardando nosso retorno',
      ),
    ).toBeInTheDocument();
    expect(screen.getByText('Pediram o XML da nota rejeitada.')).toBeInTheDocument();
  });

  it('Sem retorno do fornecedor', async () => {
    ticket = { ...ticket, vendorTickets: [{ ...record, staleDays: 6 }] };

    const section = await openTicket();

    const stale = within(section).getByText('Sem retorno do fornecedor há 6 dias úteis');
    expect(stale.closest('[data-state]')).toHaveAttribute('data-state', 'soon');
    expect(within(section).getByRole('link', { name: 'FX-20931' })).toHaveAttribute(
      'href',
      record.link,
    );
  });

  it('Solicitante não registra no fornecedor', async () => {
    serving(false);
    ticket = { ...ticket, vendorTickets: [record] };

    const section = await openTicket();

    expect(within(section).getByText('Emissor Exemplo')).toBeInTheDocument();
    expect(within(section).queryByRole('button')).toBeNull();
  });

  it('Chamado fechado no meio do registro', async () => {
    server.use(
      http.post('/api/tickets/1/vendor-tickets', () =>
        problem(409, 'urn:fiscoprojects:problem:ticket-closed'),
      ),
    );
    const section = await openTicket();
    fireEvent.click(within(section).getByRole('button', { name: text.register.button }));
    const dialog = await screen.findByRole('dialog', { name: text.register.title });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.fields.vendor }));
    fireEvent.click(await screen.findByRole('option', { name: 'Emissor Exemplo' }));
    fireEvent.change(within(dialog).getByLabelText(text.fields.code), {
      target: { value: 'FX-20931' },
    });

    fireEvent.click(within(dialog).getByRole('button', { name: text.register.confirm }));

    expect(await within(dialog).findByText(text.messages.closed)).toBeInTheDocument();
  });

  it('Com fornecedor', async () => {
    const [first, ...others] = demoTickets();
    if (first === undefined) {
      throw new Error('No ticket.');
    }
    const atVendor: TicketRow = {
      ...first,
      vendor: {
        id: record.id,
        vendor: 'Emissor Exemplo',
        code: 'FX-20931',
        status: 'InAnalysis',
        staleDays: null,
      },
    };
    const views: (string | null)[] = [];
    server.use(
      http.get('/api/tickets/counts', () =>
        HttpResponse.json({
          all: 4,
          open: 4,
          unassigned: 4,
          unassignedInMyTeams: 4,
          assignedToMe: 0,
          openedByMe: 1,
          following: 1,
          withVendor: 1,
        }),
      ),
      http.get('/api/tickets', ({ request }) => {
        const view = new URL(request.url).searchParams.get('view');
        views.push(view);
        const items = view === 'WithVendor' ? [atVendor] : [atVendor, ...others];
        return HttpResponse.json({ items, total: items.length });
      }),
    );

    await renderApp('/tickets');
    const nav = await screen.findByRole('navigation', { name: ptBR.tickets.views.label });
    const view = within(nav).getByRole('button', { name: /Com fornecedor/ });
    expect(within(view).getByText('1')).toBeInTheDocument();
    fireEvent.click(view);

    await waitFor(() => {
      expect(views).toContain('WithVendor');
    });
    const row = (await screen.findByText(atVendor.title)).closest('[role="row"]');
    if (!(row instanceof HTMLElement)) {
      throw new Error('No row.');
    }
    expect(within(row).getByText(/· no Emissor Exemplo/)).toBeInTheDocument();
  });
});
