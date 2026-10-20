import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { marina, problem, profileOf, server, signedInAs } from '../test/server';

const text = ptBR.profile.notices;

let receives: boolean;
let requests: boolean[];

beforeEach(() => {
  receives = true;
  requests = [];
  signedInAs('User');
  server.use(
    http.get('/api/profile', () =>
      HttpResponse.json({ ...profileOf(marina), receivesDailySummary: receives }),
    ),
    http.put('/api/profile/daily-summary', async ({ request }) => {
      const body = (await request.json()) as { receives: boolean };
      requests.push(body.receives);
      receives = body.receives;
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openCard() {
  await renderApp('/profile');
  return screen.findByRole('region', { name: text.title });
}

function summaryBox(card: HTMLElement) {
  return within(card).getByRole('checkbox', {
    name: text.receiveSummary.replace('{{time}}', '07:30'),
  });
}

// Spec: perfil, requirement "Avisos por e-mail no perfil".
describe('Avisos por e-mail no perfil', () => {
  it('Desligar o resumo', async () => {
    const card = await openCard();
    await waitFor(() => {
      expect(summaryBox(card)).toBeEnabled();
    });
    expect(summaryBox(card)).toBeChecked();

    fireEvent.click(summaryBox(card));

    await waitFor(() => {
      expect(requests).toEqual([false]);
    });
    await waitFor(() => {
      expect(summaryBox(card)).not.toBeChecked();
    });
  });

  it('Avisos na hora sempre ligados', async () => {
    const card = await openCard();

    for (const label of Object.values(text.immediate)) {
      const box = within(card).getByRole('checkbox', { name: label });
      expect(box).toBeChecked();
      expect(box).toBeDisabled();
    }
    expect(within(card).getByText(text.alwaysOn)).toBeInTheDocument();
  });

  it('Resumo desligado antes', async () => {
    receives = false;
    const card = await openCard();

    await waitFor(() => {
      expect(summaryBox(card)).not.toBeChecked();
    });
  });

  it('Falha ao salvar o resumo', async () => {
    server.use(http.put('/api/profile/daily-summary', () => problem(500)));
    const card = await openCard();
    await waitFor(() => {
      expect(summaryBox(card)).toBeEnabled();
    });

    fireEvent.click(summaryBox(card));

    expect(await screen.findByText(text.failed)).toBeInTheDocument();
    await waitFor(() => {
      expect(summaryBox(card)).toBeChecked();
    });
  });
});
