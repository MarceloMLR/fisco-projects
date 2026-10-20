import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { components } from '../api/schema';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { server, signedInAs } from '../test/server';

type Item = components['schemas']['NotificationResponse'];

const text = ptBR.notifications;

/** The notifications as the simulated API keeps them, changed by the requests as the real one would. */
let items: Item[];
const reads: string[] = [];

function item(
  id: string,
  kind: Item['kind'],
  values: Record<string, string>,
  createdAt: string,
  read = false,
): Item {
  return { id, kind, actor: 'Diego Torres', path: '/tickets/14', values, createdAt, read };
}

beforeEach(() => {
  reads.length = 0;
  items = [
    item(
      '0199f5a2-0000-7000-8000-000000000001',
      'TicketMentioned',
      { number: '14', project: 'ERP-FIN' },
      '2026-10-20T14:48:00Z',
    ),
    item(
      '0199f5a2-0000-7000-8000-000000000002',
      'TicketMessage',
      { number: '98', project: 'FISC-SP' },
      '2026-10-20T12:00:00Z',
    ),
    item(
      '0199f5a2-0000-7000-8000-000000000003',
      'ProjectMemberAdded',
      { project: 'TRIB-RS', role: 'Contributor' },
      '2026-10-19T13:12:00Z',
    ),
    item(
      '0199f5a2-0000-7000-8000-000000000004',
      'TicketResolved',
      { number: '95', project: 'ERP-FIN' },
      '2026-10-19T12:05:00Z',
      true,
    ),
  ];
  signedInAs('User');
  const unread = () => items.filter((notification) => !notification.read).length;
  server.use(
    http.get('/api/notifications/unread-count', () => HttpResponse.json({ unread: unread() })),
    http.get('/api/notifications', ({ request }) => {
      const onlyUnread = new URL(request.url).searchParams.get('unread') === 'true';
      return HttpResponse.json({
        items: items.filter((notification) => !onlyUnread || !notification.read),
        unread: unread(),
        hasMore: false,
      });
    }),
    http.put('/api/notifications/:id/read', ({ params }) => {
      reads.push(String(params.id));
      items = items.map((notification) =>
        notification.id === params.id ? { ...notification, read: true } : notification,
      );
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/notifications/read-all', () => {
      reads.push('all');
      items = items.map((notification) => ({ ...notification, read: true }));
      return new HttpResponse(null, { status: 204 });
    }),
  );
  // 12:00 of October 20th in Brasília; only the date is simulated, timers stay real.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-20T15:00:00Z') });
});

afterEach(() => {
  vi.useRealTimers();
});

async function bell(count: number) {
  return screen.findByRole('button', { name: `Notificações, ${String(count)} não lidas` });
}

// Spec: avisos, requirement "Sino e painel".
describe('Sino e painel', () => {
  it('Contador de não lidas', async () => {
    await renderApp('/tickets');

    const button = await bell(3);

    expect(within(button).getByTestId('bell-count')).toHaveTextContent('3');
  });

  it('Abrir uma notificação', async () => {
    const view = await renderApp('/tickets');
    fireEvent.click(await bell(3));
    const panel = await screen.findByRole('dialog', { name: text.title });

    expect(await within(panel).findByText('Hoje')).toBeInTheDocument();
    expect(within(panel).getByText('há 12 min · ERP-FIN')).toBeInTheDocument();
    fireEvent.click(await within(panel).findByText('Diego Torres mencionou você no chamado #14'));

    await waitFor(() => {
      expect(view.router.state.location.pathname).toBe('/tickets/14');
    });
    expect(reads).toEqual(['0199f5a2-0000-7000-8000-000000000001']);
    expect(await bell(2)).toBeInTheDocument();
  });

  it('Marcar todas como lidas', async () => {
    await renderApp('/tickets');
    fireEvent.click(await bell(3));
    const panel = await screen.findByRole('dialog', { name: text.title });

    fireEvent.click(within(panel).getByRole('button', { name: text.readAll }));

    expect(await within(panel).findByText(text.emptyUnread)).toBeInTheDocument();
    // The panel is modal: the bell behind it is hidden from the screen readers meanwhile.
    expect(
      await screen.findByRole('button', { name: text.bell, hidden: true }),
    ).toBeInTheDocument();
    expect(screen.queryByTestId('bell-count')).not.toBeInTheDocument();
    expect(reads).toEqual(['all']);
  });

  it('Ver todas', async () => {
    await renderApp('/tickets');
    fireEvent.click(await bell(3));
    const panel = await screen.findByRole('dialog', { name: text.title });
    await within(panel).findByText('Diego Torres mencionou você no chamado #14');
    expect(
      within(panel).queryByText('O chamado #95 foi resolvido e aguarda a sua confirmação'),
    ).not.toBeInTheDocument();

    fireEvent.click(within(panel).getByRole('button', { name: text.seeAll }));

    expect(
      await within(panel).findByText('O chamado #95 foi resolvido e aguarda a sua confirmação'),
    ).toBeInTheDocument();
    expect(within(panel).getByText('Ontem')).toBeInTheDocument();
    expect(within(panel).getByRole('radio', { name: text.all })).toBeChecked();
  });
});

describe('Atualização do sino', () => {
  it('Atualização a cada minuto', async () => {
    vi.useFakeTimers({
      toFake: ['Date', 'setTimeout', 'clearTimeout', 'setInterval', 'clearInterval'],
      now: new Date('2026-10-20T15:00:00Z'),
      shouldAdvanceTime: true,
    });
    await renderApp('/tickets');
    await bell(3);

    items = [
      item(
        '0199f5a2-0000-7000-8000-000000000005',
        'TicketAssigned',
        { number: '21', project: 'ERP-FIN' },
        '2026-10-20T15:00:30Z',
      ),
      ...items,
    ];
    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });

    expect(await bell(4)).toBeInTheDocument();
  });
});
