import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { marina, problem, profileOf, server, signedInAs } from '../test/server';

const text = ptBR.profile.notices;

let receives: boolean;
let requests: boolean[];
let channel: string;
let teamsAvailable: boolean;
let channels: string[];

beforeEach(() => {
  receives = true;
  requests = [];
  channel = 'Both';
  teamsAvailable = false;
  channels = [];
  signedInAs('User');
  server.use(
    http.get('/api/profile/immediate-notices', () =>
      HttpResponse.json({ channel, teamsAvailable }),
    ),
    http.put('/api/profile/immediate-notices', async ({ request }) => {
      const body = (await request.json()) as { channel: string };
      channels.push(body.channel);
      channel = body.channel;
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

// Spec: perfil, requirement "Avisos no perfil".
describe('Avisos no perfil', () => {
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
    expect(within(card).queryByRole('radiogroup', { name: text.channel })).not.toBeInTheDocument();
  });

  it('Canal de quem nunca escolheu', async () => {
    teamsAvailable = true;
    const card = await openCard();

    const group = await within(card).findByRole('radiogroup', { name: text.channel });
    expect(within(group).getByRole('radio', { name: text.channels.Both })).toBeChecked();
    expect(within(card).getByText(text.channelHelp)).toBeInTheDocument();
    expect(
      within(card).queryByRole('checkbox', { name: text.immediate.messages }),
    ).not.toBeInTheDocument();
  });

  it('Escolher o Teams', async () => {
    teamsAvailable = true;
    const card = await openCard();
    const group = await within(card).findByRole('radiogroup', { name: text.channel });

    fireEvent.click(within(group).getByRole('radio', { name: text.channels.Teams }));

    await waitFor(() => {
      expect(channels).toEqual(['Teams']);
    });
    await waitFor(() => {
      expect(within(group).getByRole('radio', { name: text.channels.Teams })).toBeChecked();
    });
  });

  it('Falha ao salvar o canal', async () => {
    teamsAvailable = true;
    server.use(http.put('/api/profile/immediate-notices', () => problem(500)));
    const card = await openCard();
    const group = await within(card).findByRole('radiogroup', { name: text.channel });

    fireEvent.click(within(group).getByRole('radio', { name: text.channels.Email }));

    expect(await screen.findByText(text.channelFailed)).toBeInTheDocument();
    await waitFor(() => {
      expect(within(group).getByRole('radio', { name: text.channels.Both })).toBeChecked();
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
