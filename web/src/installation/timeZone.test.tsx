import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { auditPage, demoAuditEntries, server, signedInAs } from '../test/server';

const asked: URL[] = [];

function installationIn(timeZone: string) {
  server.use(
    http.get('/api/installation/rules', () =>
      HttpResponse.json({ timeZone, dailySummaryAt: '07:30:00' }),
    ),
  );
}

beforeEach(() => {
  // 12:00:30 of October 19th in Brasília, the time zone of the browser of the tests; 00:00:30 of the 20th in Tokyo.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  asked.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/audit', ({ request }) => {
      const url = new URL(request.url);
      asked.push(url);
      return HttpResponse.json(auditPage(demoAuditEntries, url));
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

// Spec: configuracao-da-instalacao, requirement "Fuso da instalação" (the screens).
describe('Datas no fuso da instalação', () => {
  it('Horas no fuso da instalação', async () => {
    // The installation is in Tokyo and the browser in Brasília: the screens show the time of Tokyo.
    installationIn('Asia/Tokyo');

    await renderApp('/admin/users');

    const users = await screen.findByRole('grid', { name: ptBR.users.table });
    // Marina signed in at 09:12 of the 19th in Brasília, which is 21:12 in Tokyo, the day before the 20th there.
    expect(
      await within(users).findByText(ptBR.dates.yesterday.replace('{{time}}', '21:12')),
    ).toBeInTheDocument();
  });

  it('Horas da Auditoria no fuso da instalação', async () => {
    installationIn('Asia/Tokyo');

    await renderApp('/admin/audit');

    const grid = await screen.findByRole('grid', { name: ptBR.audit.table });
    // The role change at 10:14 of the 19th in Brasília is 22:14 in Tokyo.
    expect(await within(grid).findByText('22:14')).toBeInTheDocument();
    expect(within(grid).queryByText('10:14')).not.toBeInTheDocument();
  });

  it('Período no fuso da instalação', async () => {
    installationIn('Asia/Tokyo');

    await renderApp('/admin/audit');

    // The last 30 days of Tokyo, from midnight of September 21st to midnight after October 20th, there.
    await waitFor(() => {
      expect(asked.at(-1)?.searchParams.get('from')).toBe('2026-09-20T15:00:00.000Z');
    });
    expect(asked.at(-1)?.searchParams.get('to')).toBe('2026-10-20T15:00:00.000Z');
    expect(screen.getByRole('button', { name: /^Período de/ })).toHaveTextContent('21/09 – 20/10');
  });

  it('Fuso padrão sem as regras', async () => {
    server.use(http.get('/api/installation/rules', () => new HttpResponse(null, { status: 500 })));

    await renderApp('/admin/audit');

    const grid = await screen.findByRole('grid', { name: ptBR.audit.table });
    expect(await within(grid).findByText('10:14')).toBeInTheDocument();
  });
});
