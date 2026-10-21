import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Ticket } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { typeInEditor } from '../test/editor';
import { renderApp } from '../test/renderApp';
import { demoPeople, demoTicket, demoTickets, marina, server, signedInAs } from '../test/server';

const text = ptBR.tickets;
let ticket: Ticket;
const sent: string[] = [];
const following: string[] = [];

const doc = (words: string) =>
  JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: [{ type: 'text', text: words }] }],
  });

function message(index: number, authorId: string, words: string): Ticket['messages'][number] {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-8000000001${String(index).padStart(2, '0')}`,
    authorId,
    body: doc(words),
    sentAt: `2026-10-15T${String(10 + Math.floor(index / 6)).padStart(2, '0')}:${String((index % 6) * 10).padStart(2, '0')}:00Z`,
  };
}

beforeEach(() => {
  const [, rejection] = demoTickets();
  if (rejection === undefined) {
    throw new Error('No #1.');
  }
  ticket = demoTicket(rejection);
  sent.length = 0;
  following.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/tickets/1', () => HttpResponse.json(ticket)),
    http.post('/api/tickets/1/messages', async ({ request }) => {
      // The raw form: the parser of the tests does not take the files of jsdom.
      sent.push(await request.text());
      ticket = {
        ...ticket,
        messages: [
          ...ticket.messages,
          { ...message(90, marina.id, 'Diego, veja o XML'), sentAt: '2026-10-15T12:00:00Z' },
        ],
        attachments: [
          ...ticket.attachments,
          {
            id: '0199f5a2-7c1e-7d40-9a1b-900000000001',
            messageId: message(90, marina.id, '').id,
            name: 'nota-3381.xml',
            size: 42 * 1024,
            contentType: 'application/xml',
            uploadedAt: '2026-10-15T12:00:00Z',
          },
        ],
      };
      return new HttpResponse(null, { status: 201 });
    }),
    http.put('/api/tickets/1/following', () => {
      following.push('PUT');
      ticket = { ...ticket, following: true, followers: [...ticket.followers, marina.id] };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openTicket() {
  await renderApp('/tickets/1');
  await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' });
}

// Spec: chamados, requirement "Conversa pela tela".
describe('Conversa pela tela', () => {
  it('Cores e lados por papel', async () => {
    ticket = {
      ...ticket,
      messages: [
        message(1, demoPeople.carla.id, 'A nota 3381 foi recusada'),
        message(2, demoPeople.rafael.id, 'Vou registrar no fornecedor'),
        message(3, marina.id, 'Prioridade alta'),
      ],
    };
    await openTicket();

    const list = screen.getByRole('list', { name: text.conversation.messages });
    const carla = within(list)
      .getByRole('article', { name: 'Mensagem de Carla Mendes' })
      .closest('li');
    const rafael = within(list)
      .getByRole('article', { name: 'Mensagem de Rafael Souza' })
      .closest('li');
    const own = within(list).getByRole('article', { name: 'Mensagem de Marina Costa' });
    expect([carla?.dataset.side, carla?.dataset.tone]).toEqual(['requester', 'requester']);
    expect([rafael?.dataset.side, rafael?.dataset.tone]).toEqual(['team', 'techlead']);
    expect(own.closest('li')?.dataset.tone).toBe('manager');
    // The tag names the part, so color is never alone; "(você)" on the person.
    expect(within(own).getByText(text.roles.Manager)).toBeVisible();
    expect(within(own).getByText(text.conversation.you)).toBeVisible();
    expect(
      screen.getByText(text.conversation.interactions_other.replace('{{count}}', '3')),
    ).toBeVisible();
  });

  it('Interações antigas recolhidas', async () => {
    ticket = {
      ...ticket,
      messages: Array.from({ length: 14 }, (_, index) =>
        message(
          index,
          index % 2 === 0 ? demoPeople.carla.id : demoPeople.diego.id,
          `Mensagem ${String(index)}`,
        ),
      ),
    };
    await openTicket();

    const list = screen.getByRole('list', { name: text.conversation.messages });
    expect(within(list).getAllByRole('article')).toHaveLength(10);
    expect(within(list).queryByText('Mensagem 3')).not.toBeInTheDocument();

    fireEvent.click(
      within(list).getByRole('button', {
        name: text.conversation.older_other.replace('{{count}}', '4'),
      }),
    );

    expect(within(list).getAllByRole('article')).toHaveLength(14);
    expect(within(list).getByText('Mensagem 0')).toBeVisible();
  });

  it('Responder pela tela', async () => {
    await openTicket();

    await typeInEditor(text.reply.label, 'veja o XML ');
    fireEvent.click(screen.getByRole('button', { name: ptBR.editor.mention }));
    const people = await screen.findByRole('menu', { name: ptBR.editor.mention });
    // The managers and the people involved, without the person who writes.
    expect(
      within(people)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual(['CMCarla Mendes', 'DTDiego Torres', 'RSRafael Souza']);
    fireEvent.click(within(people).getByRole('menuitem', { name: /Diego Torres/ }));
    const input = screen.getByLabelText(text.reply.files, { selector: 'input' });
    fireEvent.change(input, {
      target: { files: [new File(['<nfe/>'], 'nota-3381.xml', { type: 'text/xml' })] },
    });
    expect(screen.getByText('nota-3381.xml')).toBeVisible();

    fireEvent.click(screen.getByRole('button', { name: text.reply.send }));

    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    const form = sent[0] ?? '';
    expect(form).toContain('name="body"');
    expect(form).toContain(demoPeople.diego.id);
    expect(form).toContain('veja o XML');
    // The file goes in the field files (jsdom does not write its content).
    expect(form).toContain('name="files"');
    // The message at the end of the conversation, and the file in "Anexos da conversa".
    expect(await screen.findByText('Diego, veja o XML')).toBeVisible();
    const panel = screen.getByRole('complementary', { name: text.details.people });
    expect(within(panel).getByRole('link', { name: /^nota-3381\.xml/ })).toHaveAttribute(
      'href',
      '/api/tickets/1/attachments/0199f5a2-7c1e-7d40-9a1b-900000000001',
    );
  });

  it('Sem resposta para quem só vê', async () => {
    signedInAs('User');
    ticket = {
      ...ticket,
      permissions: {
        canSend: false,
        canAttachToDescription: false,
        actions: [],
        canChangePriority: false,
        canChangeAssignee: false,
        canChangeRequester: false,
        canTransfer: false,
        canRegisterVendorTicket: false,
      },
      following: false,
    };
    await openTicket();

    expect(screen.queryByRole('textbox', { name: text.reply.label })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: text.panel.follow }));

    expect(await screen.findByRole('button', { name: text.panel.unfollow })).toBeVisible();
    expect(following).toEqual(['PUT']);
  });
});

// Spec: chamados, requirement "Baixar anexos pela tela" (Chamados-2.1).
describe('Baixar anexos pela tela', () => {
  it('Baixar pela tela', async () => {
    ticket = {
      ...ticket,
      attachments: [
        {
          id: '0199f5a2-7c1e-7d40-9a1b-900000000002',
          messageId: null,
          name: 'erro-539.png',
          size: 12 * 1024,
          contentType: 'image/png',
          uploadedAt: '2026-10-15T09:00:00Z',
        },
      ],
    };
    await openTicket();

    const panel = screen.getByRole('complementary', { name: text.details.people });
    const section = within(panel).getByRole('region', { name: /^Anexos da conversa/ });
    expect(within(section).getByRole('link', { name: /^erro-539\.png/ })).toHaveAttribute(
      'href',
      '/api/tickets/1/attachments/0199f5a2-7c1e-7d40-9a1b-900000000002',
    );
    const download = within(section).getByRole('link', {
      name: text.conversation.download.replace('{{name}}', 'erro-539.png'),
    });
    expect(download).toHaveAttribute(
      'href',
      '/api/tickets/1/attachments/0199f5a2-7c1e-7d40-9a1b-900000000002?download=true',
    );
    expect(download).toHaveAttribute('download', 'erro-539.png');
    expect(
      within(section).getByRole('link', { name: text.panel.downloadAllLabel }),
    ).toHaveAttribute('href', '/api/tickets/1/attachments/archive');
  });

  it('Sem anexos para baixar', async () => {
    ticket = { ...ticket, attachments: [] };
    await openTicket();

    const panel = screen.getByRole('complementary', { name: text.details.people });
    const section = within(panel).getByRole('region', { name: /^Anexos da conversa/ });
    expect(within(section).getByText(text.panel.noAttachments)).toBeVisible();
    expect(
      within(section).queryByRole('link', { name: text.panel.downloadAllLabel }),
    ).not.toBeInTheDocument();
  });
});
