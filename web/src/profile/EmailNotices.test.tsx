import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { marina, problem, profileOf, server, signedInAs } from '../test/server';

const text = ptBR.profile.notices;

interface Channels {
  email: boolean;
  teams: boolean;
}

let receives: boolean;
let requests: boolean[];
let chosen: Channels;
let teamsAvailable: boolean;
let saved: Channels[];

beforeEach(() => {
  receives = true;
  requests = [];
  chosen = { email: true, teams: false };
  teamsAvailable = false;
  saved = [];
  signedInAs('User');
  server.use(
    http.get('/api/profile/immediate-notices', () =>
      HttpResponse.json({ ...chosen, teamsAvailable }),
    ),
    http.put('/api/profile/immediate-notices', async ({ request }) => {
      const body = (await request.json()) as Channels;
      saved.push(body);
      chosen = body;
      return new HttpResponse(null, { status: 204 });
    }),
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

async function emailBox(card: HTMLElement) {
  const box = await within(card).findByRole('checkbox', { name: text.byEmail });
  await waitFor(() => {
    expect(box).toBeEnabled();
  });
  return box;
}

// Spec: perfil, requirement "Canais dos avisos no perfil" (ADR-0151).
describe('Canais dos avisos no perfil', () => {
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

  it('Desligar o e-mail', async () => {
    const card = await openCard();
    const email = await emailBox(card);
    expect(within(card).queryByText(text.onlyInBell)).not.toBeInTheDocument();

    fireEvent.click(email);

    await waitFor(() => {
      expect(saved).toEqual([{ email: false, teams: false }]);
    });
    await waitFor(() => {
      expect(email).not.toBeChecked();
    });
    expect(within(card).getByRole('status')).toHaveTextContent(text.onlyInBell);
  });

  it('Escolher o Teams', async () => {
    teamsAvailable = true;
    const card = await openCard();
    const email = await emailBox(card);
    const teams = within(card).getByRole('checkbox', { name: text.byTeams });

    fireEvent.click(teams);
    await waitFor(() => {
      expect(teams).toBeChecked();
    });
    await waitFor(() => {
      expect(email).toBeEnabled();
    });
    fireEvent.click(email);

    await waitFor(() => {
      expect(saved).toEqual([
        { email: true, teams: true },
        { email: false, teams: true },
      ]);
    });
    await waitFor(() => {
      expect(email).not.toBeChecked();
    });
    expect(teams).toBeChecked();
    expect(within(card).queryByText(text.onlyInBell)).not.toBeInTheDocument();
  });

  it('Canal de quem nunca escolheu', async () => {
    teamsAvailable = true;
    const card = await openCard();

    expect(await emailBox(card)).toBeChecked();
    expect(within(card).getByRole('checkbox', { name: text.byTeams })).not.toBeChecked();
    expect(within(card).getByText(text.nowHelp)).toBeInTheDocument();
  });

  it('Ajuda do aplicativo do Teams', async () => {
    teamsAvailable = true;
    const card = await openCard();
    await emailBox(card);

    expect(within(card).getByText(text.teamsHelp)).toBeInTheDocument();
  });

  it('Teams fora do cartão', async () => {
    const card = await openCard();
    await emailBox(card);

    expect(within(card).queryByRole('checkbox', { name: text.byTeams })).not.toBeInTheDocument();
    expect(within(card).queryByText(text.teamsHelp)).not.toBeInTheDocument();
    expect(summaryBox(card)).toBeInTheDocument();
  });

  it('Teams ligado fora do cartão não conta', async () => {
    chosen = { email: false, teams: true };
    const card = await openCard();

    expect(await within(card).findByRole('status')).toHaveTextContent(text.onlyInBell);
  });

  it('Falha ao gravar', async () => {
    server.use(http.put('/api/profile/immediate-notices', () => problem(500)));
    const card = await openCard();
    const email = await emailBox(card);

    fireEvent.click(email);

    expect(await screen.findByText(text.channelFailed)).toBeInTheDocument();
    await waitFor(() => {
      expect(email).toBeChecked();
    });
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
