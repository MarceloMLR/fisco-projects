import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket, TicketDeadlineSetting, TicketRow } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoPeople, demoTicket, demoTickets, server, signedInAs } from '../test/server';

const text = ptBR.tickets;
let ticket: Ticket;
const priorities: unknown[] = [];

beforeEach(() => {
  const [, rejection] = demoTickets();
  if (rejection === undefined) {
    throw new Error('No #1.');
  }
  ticket = {
    ...demoTicket(rejection),
    status: 'WaitingForRequester',
    assignee: demoPeople.diego,
    permissions: {
      canSend: true,
      canAttachToDescription: false,
      actions: [],
      canChangePriority: true,
    },
    deadlines: {
      firstResponse: { state: 'Met', dueOn: '2026-10-20', daysLate: null },
      firstRespondedAt: '2026-10-19T12:16:00Z',
      resolution: { state: 'Paused', dueOn: null, daysLate: null },
      usedDays: 1,
      totalDays: 2,
      pausedDays: 2,
    },
  };
  priorities.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
    http.put('/api/tickets/1/priority', async ({ request }) => {
      const body = (await request.json()) as { priority: Ticket['priority'] };
      priorities.push(body);
      ticket = {
        ...ticket,
        priority: body.priority,
        status: 'InProgress',
        deadlines: ticket.deadlines && {
          ...ticket.deadlines,
          resolution: { state: 'Soon', dueOn: '2026-10-21', daysLate: null },
        },
      };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openTicket() {
  await renderApp('/tickets/1');
  await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });
}

// Spec: chamados, requirement "Prazos pela tela".
describe('Prazos pela tela', () => {
  it('Prazo vencido na lista', async () => {
    const [first, ...others] = demoTickets();
    if (first === undefined) {
      throw new Error('No ticket.');
    }
    const late: TicketRow = {
      ...first,
      resolution: { state: 'Overdue', dueOn: '2026-10-15', daysLate: 2 },
    };
    server.use(
      http.get('/api/tickets', () =>
        HttpResponse.json({ items: [late, ...others], total: others.length + 1 }),
      ),
    );

    await renderApp('/tickets');

    const row = (await screen.findByText(late.title)).closest('[role="row"]');
    if (!(row instanceof HTMLElement)) {
      throw new Error('No row of the late ticket.');
    }
    const due = within(row).getByText('Venceu há 2 dias úteis');
    expect(due.closest('[data-state]')).toHaveAttribute('data-state', 'overdue');
    expect(
      screen.getByRole('columnheader', { name: text.columns.statusAndDue }),
    ).toBeInTheDocument();
  });

  it('Prazos no chamado', async () => {
    await openTicket();

    expect(screen.getAllByText(text.due.paused).length).toBeGreaterThan(0);
    const panel = screen.getByRole('region', { name: text.panel.deadlines });
    expect(within(panel).getByText(text.panel.resolution)).toBeInTheDocument();
    expect(within(panel).getByText(/Cumprida em/)).toBeInTheDocument();
    expect(within(panel).getByText('2 dias úteis aguardando o solicitante')).toBeInTheDocument();
    expect(within(panel).getByText('1 de 2 dias úteis')).toBeInTheDocument();
    expect(within(panel).getByRole('progressbar', { name: text.panel.usedLabel })).toHaveAttribute(
      'aria-valuenow',
      '50',
    );
  });

  it('Mudar a prioridade pela tela', async () => {
    await openTicket();
    fireEvent.click(screen.getByRole('button', { name: text.actionMenu }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.priorityChange.menu }));
    const dialog = await screen.findByRole('dialog', { name: text.priorityChange.title });

    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.priorityChange.label }));
    fireEvent.click(await screen.findByRole('option', { name: text.priorities.Critical }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.priorityChange.save }));

    await screen.findByText(text.priorityChange.done);
    expect(priorities).toEqual([{ priority: 'Critical' }]);
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getAllByText(text.priorities.Critical).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Vence quarta, 21/10').length).toBeGreaterThan(0);
  });

  it('Mudança de prioridade na conversa', async () => {
    ticket = {
      ...ticket,
      events: [
        {
          id: '0199f5a2-7c1e-7d40-9a1b-a00000000021',
          kind: 'PriorityChanged',
          actorId: demoPeople.marina.id,
          from: null,
          to: null,
          action: null,
          assigneeId: null,
          fromPriority: 'Medium',
          toPriority: 'High',
          at: '2026-10-19T13:00:00Z',
        },
        {
          id: '0199f5a2-7c1e-7d40-9a1b-a00000000022',
          kind: 'StatusChanged',
          actorId: demoPeople.diego.id,
          from: 'InProgress',
          to: 'WaitingForRequester',
          action: 'AskForInformation',
          assigneeId: null,
          fromPriority: null,
          toPriority: null,
          at: '2026-10-19T14:00:00Z',
        },
      ],
    };
    await openTicket();

    expect(screen.getByText('mudou a prioridade · Média → Alta')).toBeInTheDocument();
    expect(
      screen.getByText('pediu informações · Em andamento → Aguardando solicitante · prazo pausado'),
    ).toBeInTheDocument();
  });
});

// Spec: chamados, requirement "Prazos pela tela".
describe('Prazos dos chamados', () => {
  it('Prazos na Administração', async () => {
    signedInAs('Administrator');
    let deadlines: TicketDeadlineSetting[] = [
      { priority: 'Low', firstResponseDays: 1, resolutionDays: 10 },
      { priority: 'Medium', firstResponseDays: 1, resolutionDays: 5 },
      { priority: 'High', firstResponseDays: 1, resolutionDays: 2 },
      { priority: 'Critical', firstResponseDays: 1, resolutionDays: 1 },
    ];
    const saved: unknown[] = [];
    server.use(
      http.get('/api/admin/ticket-deadlines', () => HttpResponse.json(deadlines)),
      http.put('/api/admin/ticket-deadlines', async ({ request }) => {
        const body = (await request.json()) as { deadlines: TicketDeadlineSetting[] };
        saved.push(body);
        deadlines = body.deadlines;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await renderApp('/admin/ticket-deadlines');
    const medium = await screen.findByRole('spinbutton', {
      name: 'Resolução da prioridade Média, em dias úteis',
    });

    fireEvent.change(medium, { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: ptBR.ticketDeadlines.save }));
    expect(await screen.findByText(ptBR.ticketDeadlines.invalid)).toBeInTheDocument();
    expect(saved).toHaveLength(0);

    fireEvent.change(medium, { target: { value: '3' } });
    fireEvent.click(screen.getByRole('button', { name: ptBR.ticketDeadlines.save }));

    await screen.findByText(ptBR.ticketDeadlines.saved);
    expect(saved).toEqual([
      {
        deadlines: [
          { priority: 'Low', firstResponseDays: 1, resolutionDays: 10 },
          { priority: 'Medium', firstResponseDays: 1, resolutionDays: 3 },
          { priority: 'High', firstResponseDays: 1, resolutionDays: 2 },
          { priority: 'Critical', firstResponseDays: 1, resolutionDays: 1 },
        ],
      },
    ]);
  });
});
