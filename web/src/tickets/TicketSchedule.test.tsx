import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket, TicketEvent } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { itemsOf } from '../projects/schedule';
import { renderApp } from '../test/renderApp';
import {
  demoItem,
  demoPeople,
  demoSchedule,
  demoTicket,
  demoTickets,
  problem,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.tickets.schedule;
const schedule = demoSchedule();
const itemNamed = (name: string) => {
  const item = itemsOf(schedule).find((candidate) => candidate.name === name);
  if (item === undefined) {
    throw new Error(`No ${name}.`);
  }
  return item;
};
const tests = itemNamed('Testes integrados');
const acceptance = itemNamed('Aceite da homologação');

let ticket: Ticket;
const puts: unknown[] = [];

/** What the API says a ticket linked to Testes integrados reaches, as the schedule of the demonstration has it. */
const linkedToTests: Ticket['schedule'] = {
  items: [
    {
      id: tests.id,
      type: 'Task',
      name: tests.name,
      start: tests.start,
      end: tests.end,
      progress: tests.progress,
      isDone: false,
      lateDays: 2,
    },
  ],
  milestones: [{ id: acceptance.id, name: acceptance.name, day: acceptance.start, lateDays: 0 }],
  onCriticalPath: true,
  canChange: true,
};

const linkedEvent: TicketEvent = {
  id: '0199f5a2-7c1e-7d40-9a1b-930000000001',
  kind: 'ImpactsChanged',
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
  vendorTicketId: null,
  fromVendorStatus: null,
  toVendorStatus: null,
  note: null,
  linkedItems: ['Testes integrados'],
  unlinkedItems: [],
  at: '2026-10-19T20:00:00Z',
};

function serving(section: Ticket['schedule']) {
  const [, rejection] = demoTickets();
  if (rejection === undefined) {
    throw new Error('No #1.');
  }
  ticket = {
    ...demoTicket(rejection),
    status: 'InProgress',
    assignee: demoPeople.diego,
    schedule: section,
  };
}

beforeEach(() => {
  puts.length = 0;
  serving({ items: [], milestones: [], onCriticalPath: false, canChange: true });
  signedInAs('User');
  server.use(
    http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(schedule)),
    http.put('/api/tickets/1/impacts', async ({ request }) => {
      const body = (await request.json()) as { itemIds: string[] };
      puts.push(body);
      if (body.itemIds.length > 10) {
        return problem(400, 'urn:gerenware:problem:ticket-impact-refused');
      }
      ticket = { ...ticket, schedule: linkedToTests, events: [linkedEvent] };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openTicket() {
  await renderApp('/tickets/1');
  await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });
  return screen.getByRole('region', { name: text.title });
}

// Spec: chamados, requirement "Seção No cronograma" (ADR-0164).
describe('Seção No cronograma', () => {
  it('Seção para quem não atende', async () => {
    serving({ ...linkedToTests, canChange: false });
    const section = await openTicket();

    const items = within(section).getByRole('list', { name: text.items });
    expect(within(items).getByRole('link', { name: 'Testes integrados' })).toHaveAttribute(
      'href',
      `/projects/${ticket.project.id}/schedule?item=${tests.id}`,
    );
    expect(within(items).getByRole('img', { name: text.kinds.Task })).toBeInTheDocument();
    expect(within(items).getByText('2 dias úteis de atraso')).toBeInTheDocument();
    const threatened = within(section).getByRole('list', { name: text.threatens });
    expect(
      within(threatened).getByRole('link', { name: 'Aceite da homologação' }),
    ).toBeInTheDocument();
    expect(within(section).getByText(text.critical)).toBeInTheDocument();
    expect(within(section).queryByRole('button', { name: text.change })).toBeNull();
  });

  it('Seção para quem não atende, sem ligação', async () => {
    serving({ items: [], milestones: [], onCriticalPath: false, canChange: false });
    const section = await openTicket();

    expect(within(section).getByText(text.none)).toBeInTheDocument();
    expect(within(section).queryByRole('button', { name: text.link })).toBeNull();
  });
});

// Spec: chamados, requirement "Ligar pela tela" (ADR-0164).
describe('Ligar pela tela', () => {
  it('Ligar pela tela', async () => {
    const section = await openTicket();

    fireEvent.click(within(section).getByRole('button', { name: text.link }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    const field = await within(dialog).findByRole('combobox', { name: text.dialog.items });
    await waitFor(() => {
      expect(field).toBeEnabled();
    });
    fireEvent.mouseDown(field);
    // The tasks and milestones of the schedule, in its order, under their phases.
    expect(
      await screen.findByRole('option', { name: 'Aceite da homologação' }),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole('option', { name: 'Testes integrados' }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.confirm }));

    expect(
      await screen.findByText(text.messages.saved.replace('{{number}}', '1')),
    ).toBeInTheDocument();
    expect(puts).toEqual([{ itemIds: [tests.id] }]);
    const updated = screen.getByRole('region', { name: text.title });
    expect(within(updated).getByRole('link', { name: 'Testes integrados' })).toBeInTheDocument();
    expect(
      within(updated).getByRole('link', { name: 'Aceite da homologação' }),
    ).toBeInTheDocument();
    expect(within(updated).getByRole('button', { name: text.change })).toBeInTheDocument();
    // The conversation tells it, also in the history.
    fireEvent.click(screen.getByRole('radio', { name: ptBR.tickets.conversation.filters.history }));
    expect(
      screen.getByText('ligou o chamado ao cronograma · Testes integrados'),
    ).toBeInTheDocument();
  });

  it('Até 10 itens', async () => {
    serving({ ...linkedToTests });
    // Four more tasks in Levantamento: 12 tasks and milestones to choose from.
    const [first, ...others] = schedule.phases;
    if (first === undefined) {
      throw new Error('No phase.');
    }
    const larger = {
      ...schedule,
      phases: [
        {
          ...first,
          items: [
            ...first.items,
            ...[1, 2, 3, 4].map((index) =>
              demoItem(20 + index, first.id, {
                name: `Levantar regra ${String(index)}`,
                start: '2026-10-19',
                end: '2026-10-19',
              }),
            ),
          ],
        },
        ...others,
      ],
    };
    server.use(http.get('/api/projects/:id/schedule', () => HttpResponse.json(larger)));
    const section = await openTicket();

    fireEvent.click(within(section).getByRole('button', { name: text.change }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    const field = await within(dialog).findByRole('combobox', { name: text.dialog.items });
    await waitFor(() => {
      expect(field).toBeEnabled();
    });
    fireEvent.mouseDown(field);
    for (const name of itemsOf(larger)
      .map((item) => item.name)
      .filter((name) => name !== 'Testes integrados')) {
      fireEvent.click(await screen.findByRole('option', { name }));
    }

    expect(
      within(dialog).getByText(text.dialog.tooMany.replace('{{count}}', '10')),
    ).toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.confirm }));
    expect(puts).toEqual([]);
  });
});
