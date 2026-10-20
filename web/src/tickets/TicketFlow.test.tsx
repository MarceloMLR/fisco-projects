import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { typeInEditor } from '../test/editor';
import { renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoTicket,
  demoTickets,
  marina,
  problem,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.tickets;
let ticket: Ticket;
const taken: unknown[] = [];

const doc = (words: string) =>
  JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: [{ type: 'text', text: words }] }],
  });

const diegoAnswer: Ticket['messages'][number] = {
  id: '0199f5a2-7c1e-7d40-9a1b-800000000101',
  authorId: demoPeople.diego.id,
  body: doc('Vou olhar o XML da devolução.'),
  sentAt: '2026-10-15T10:00:00Z',
};

const tookIt: Ticket['events'] = [
  {
    id: '0199f5a2-7c1e-7d40-9a1b-a00000000001',
    kind: 'AssigneeSet',
    actorId: demoPeople.diego.id,
    from: null,
    to: null,
    action: null,
    assigneeId: demoPeople.diego.id,
    fromPriority: null,
    toPriority: null,
    at: '2026-10-15T10:00:00Z',
  },
  {
    id: '0199f5a2-7c1e-7d40-9a1b-a00000000002',
    kind: 'StatusChanged',
    actorId: demoPeople.diego.id,
    from: 'Open',
    to: 'InProgress',
    action: 'FirstResponse',
    assigneeId: null,
    fromPriority: null,
    toPriority: null,
    at: '2026-10-15T10:00:00Z',
  },
];

beforeEach(() => {
  const [, rejection] = demoTickets();
  if (rejection === undefined) {
    throw new Error('No #1.');
  }
  // #1 in progress with Diego, who answered it first; Marina, the manager, serves it too.
  ticket = {
    ...demoTicket(rejection),
    status: 'InProgress',
    assignee: demoPeople.diego,
    messages: [diegoAnswer],
    events: tookIt,
    permissions: {
      canSend: true,
      canAttachToDescription: false,
      actions: [
        { action: 'AskForInformation', to: 'WaitingForRequester', requiresMessage: true },
        { action: 'Resolve', to: 'Resolved', requiresMessage: false },
      ],
      canChangePriority: true,
    },
  };
  taken.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
    http.post('/api/tickets/1/transitions', async ({ request }) => {
      const body = (await request.json()) as { action: string; body: string | null };
      taken.push(body);
      const at = '2026-10-15T11:00:00Z';
      const to = body.action === 'Resolve' ? 'Resolved' : 'WaitingForRequester';
      ticket = {
        ...ticket,
        status: to,
        messages:
          body.body === null
            ? ticket.messages
            : [
                ...ticket.messages,
                {
                  id: '0199f5a2-7c1e-7d40-9a1b-800000000102',
                  authorId: marina.id,
                  body: body.body,
                  sentAt: at,
                },
              ],
        events: [
          ...ticket.events,
          {
            id: '0199f5a2-7c1e-7d40-9a1b-a00000000003',
            kind: 'StatusChanged',
            actorId: marina.id,
            from: 'InProgress',
            to,
            action: body.action as 'Resolve',
            assigneeId: null,
            fromPriority: null,
            toPriority: null,
            at,
          },
        ],
        permissions: { ...ticket.permissions, actions: [], canChangePriority: false },
      };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openTicket() {
  await renderApp('/tickets/1');
  await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });
}

// Spec: chamados, requirement "Status pela tela".
describe('Status pela tela', () => {
  it('Resolver pela tela', async () => {
    await openTicket();
    // The main action is the button; the others are in the "⋮".
    const resolve = screen.getByRole('button', { name: text.actions.Resolve });

    fireEvent.click(resolve);

    await screen.findByText(text.actionDone.Resolve);
    expect(taken).toEqual([{ action: 'Resolve', body: null }]);
    expect(screen.getByText(text.statuses.Resolved)).toBeInTheDocument();
    expect(
      screen.getByText(/marcou como resolvido · Em andamento → Resolvido/),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.actions.Resolve })).not.toBeInTheDocument();
  });

  it('Pedir informações pela tela', async () => {
    await openTicket();
    fireEvent.click(screen.getByRole('button', { name: text.actionMenu }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.actions.AskForInformation }));
    const dialog = await screen.findByRole('dialog', { name: text.actions.AskForInformation });

    // Without the text, the dialog asks for it.
    fireEvent.click(within(dialog).getByRole('button', { name: text.actions.AskForInformation }));
    expect(await within(dialog).findByRole('alert')).toHaveTextContent(text.actionDialog.needed);
    await typeInEditor(text.actionDialog.label, 'Qual a série da nota?');
    fireEvent.click(within(dialog).getByRole('button', { name: text.actions.AskForInformation }));

    await screen.findByText(text.actionDone.AskForInformation);
    expect(taken).toHaveLength(1);
    expect(JSON.stringify(taken[0])).toContain('Qual a série da nota?');
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getAllByText(text.statuses.WaitingForRequester).length).toBeGreaterThan(0);
    expect(
      screen.getByRole('article', {
        name: text.conversation.message.replace('{{name}}', marina.name),
      }),
    ).toHaveTextContent('Qual a série da nota?');
  });
});

// Spec: chamados, requirement "Histórico na conversa".
describe('Histórico na conversa', () => {
  it('Filtrar o histórico', async () => {
    await openTicket();
    expect(screen.getByText(/assumiu o chamado/)).toBeInTheDocument();
    expect(screen.getByText('3 interações')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('radio', { name: text.conversation.filters.history }));

    expect(screen.queryAllByRole('article')).toHaveLength(0);
    expect(screen.getByText(/assumiu o chamado/)).toBeInTheDocument();
    expect(screen.getByText(/respondeu · Aberto → Em andamento/)).toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: text.details.description }),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('radio', { name: text.conversation.filters.messages }));

    expect(screen.getAllByRole('article')).toHaveLength(1);
    expect(screen.queryByText(/assumiu o chamado/)).not.toBeInTheDocument();
  });

  it('Mudança feita pelo sistema', async () => {
    ticket = {
      ...ticket,
      status: 'Closed',
      events: [
        ...tookIt,
        {
          id: '0199f5a2-7c1e-7d40-9a1b-a00000000009',
          kind: 'StatusChanged',
          actorId: null,
          from: 'Resolved',
          to: 'Closed',
          action: 'AutoClose',
          assigneeId: null,
          fromPriority: null,
          toPriority: null,
          at: '2026-10-22T12:00:00Z',
        },
      ],
      permissions: {
        canSend: false,
        canAttachToDescription: false,
        actions: [],
        canChangePriority: false,
      },
    };
    await openTicket();

    expect(
      screen.getByText(/fechou sem retorno do solicitante · Resolvido → Fechado/),
    ).toHaveTextContent(`${text.events.system} fechou sem retorno`);
    expect(screen.queryByRole('textbox', { name: text.reply.label })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.actionMenu })).not.toBeInTheDocument();
  });

  it('Status mudou antes da ação', async () => {
    server.use(
      http.post('/api/tickets/1/transitions', () =>
        problem(409, 'urn:fiscoprojects:problem:ticket-action-not-allowed-now'),
      ),
    );
    await openTicket();

    fireEvent.click(screen.getByRole('button', { name: text.actions.Resolve }));

    await screen.findByText(text.actionStale);
  });
});

// Spec: chamados, requirement "Status pela tela".
describe('Fluxo de chamados', () => {
  it('Página do fluxo', async () => {
    await renderApp('/tickets');
    fireEvent.click(await screen.findByRole('link', { name: text.flow.link }));

    const table = await screen.findByRole('grid', { name: text.flow.transitions });
    await within(table).findByText(text.actions.AskForInformation);
    const ask = within(table).getByText(text.actions.AskForInformation).closest('[role="row"]');
    if (!(ask instanceof HTMLElement)) {
      throw new Error('No row of Pedir informações.');
    }
    expect(within(ask).getByText(text.flow.actors.Attendant)).toBeInTheDocument();
    expect(within(ask).getByText(text.flow.needsMessage)).toBeInTheDocument();
    expect(within(ask).getByText(text.statuses.WaitingForRequester)).toBeInTheDocument();
    const autoClose = within(table).getByText(text.actions.AutoClose).closest('[role="row"]');
    if (!(autoClose instanceof HTMLElement)) {
      throw new Error('No row of the automatic closing.');
    }
    expect(within(autoClose).getByText(text.flow.automatic)).toBeInTheDocument();
    expect(screen.getByText(/fecha sozinho depois de 5 dias úteis/)).toBeInTheDocument();
  });
});
