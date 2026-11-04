import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { microsoftSignIn } from '../session/microsoftSignIn';
import { rememberEmail } from '../session/rememberedEmail';
import { rememberSignOut } from '../session/signedOut';
import { renderApp } from '../test/renderApp';
import { marina, server, signedOut } from '../test/server';
import { teamsOpenPath } from './teamsPage';

const text = ptBR.signIn;
const microsoft = 'https://login.microsoftonline.com/tenant/oauth2/v2.0/authorize?login_hint=x';
const diego = 'diego.torres@empresa.example';

/** The sign-in methods of the installation, as the API answers them. */
function methods(localSignIn: boolean, entraSignIn: boolean) {
  server.use(
    http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn, entraSignIn })),
  );
}

/** The API starts the sign-in through Entra ID; gives what the pages sent, one body per start. */
function startEntra() {
  const sent: unknown[] = [];
  server.use(
    http.post('/api/session/entra', async ({ request }) => {
      sent.push(await request.json());
      return HttpResponse.json({ location: microsoft });
    }),
  );
  return sent;
}

function watchMicrosoft() {
  return vi.spyOn(microsoftSignIn, 'go').mockImplementation(() => undefined);
}

function signInHeading() {
  return screen.findByRole('heading', { level: 1, name: text.title });
}

// Spec: entrada-e-sessao, requirement "Entrada direta pelo link do Teams" (ADR-0153).
describe('Entrada direta pelo link do Teams', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('Link do Teams com sessão', async () => {
    const sent = startEntra();
    const { router } = await renderApp(teamsOpenPath('/tickets/6'));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/tickets/6');
    });
    expect(sent).toEqual([]);
  });

  it('Link do Teams sem sessão', async () => {
    signedOut();
    methods(false, true);
    rememberEmail(diego);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp(teamsOpenPath('/tickets/6'));

    await waitFor(() => {
      expect(go).toHaveBeenCalledWith(microsoft);
    });
    // Straight to Microsoft, with the last e-mail as the hint and the ticket as the way back.
    expect(sent).toEqual([{ email: diego, returnUrl: '/tickets/6', previousUserId: null }]);
    expect(screen.queryByRole('heading', { name: text.title })).not.toBeInTheDocument();
  });

  it('Link do Teams sem e-mail lembrado', async () => {
    signedOut();
    methods(true, true);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp(teamsOpenPath('/tickets/6?tab=history'));

    await waitFor(() => {
      expect(go).toHaveBeenCalledWith(microsoft);
    });
    expect(sent).toEqual([
      { email: null, returnUrl: '/tickets/6?tab=history', previousUserId: null },
    ]);
  });

  it('Link do Teams depois de Sair', async () => {
    signedOut();
    methods(false, true);
    rememberEmail(diego);
    rememberSignOut();
    const sent = startEntra();
    const go = watchMicrosoft();
    const { router } = await renderApp(teamsOpenPath('/tickets/6'));

    // The sign-in page, as in a shared computer, and nothing asked to Microsoft by itself.
    expect(await signInHeading()).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/sign-in');
    expect(sent).toEqual([]);
    expect(go).not.toHaveBeenCalled();

    // Signing in brings the person back to the ticket.
    fireEvent.click(screen.getByRole('button', { name: text.submit }));
    await waitFor(() => {
      expect(go).toHaveBeenCalledWith(microsoft);
    });
    expect(sent).toEqual([{ email: diego, returnUrl: '/tickets/6', previousUserId: null }]);
  });

  it('Link do Teams sem a entrada pelo Entra', async () => {
    signedOut();
    methods(true, false);
    const sent = startEntra();
    const { router } = await renderApp(teamsOpenPath('/projects'));

    expect(await signInHeading()).toBeInTheDocument();
    expect(sent).toEqual([]);

    server.use(
      http.post('/api/session/local', () => new HttpResponse(null, { status: 204 })),
      http.get('/api/session', () => HttpResponse.json(marina)),
    );
    fireEvent.change(screen.getByLabelText(text.email), { target: { value: marina.email } });
    fireEvent.click(screen.getByRole('button', { name: text.submit }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects');
    });
  });

  it('Recusa da Microsoft pelo link do Teams', async () => {
    // Microsoft refused the sign-in the link of Teams started: the API sends the browser to the sign-in page with the
    // reason, never back to the passage, so nothing goes to Microsoft again by itself.
    signedOut();
    methods(false, true);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp('/sign-in?refused=guest');

    expect(await screen.findByRole('alert')).toHaveTextContent(text.refused.guest);
    expect(sent).toEqual([]);
    expect(go).not.toHaveBeenCalled();
  });

  it('Falha ao começar pelo link do Teams', async () => {
    signedOut();
    methods(false, true);
    server.use(http.post('/api/session/entra', () => new HttpResponse(null, { status: 500 })));
    const go = watchMicrosoft();
    await renderApp(teamsOpenPath('/tickets/6'));

    expect(await signInHeading()).toBeInTheDocument();
    expect(go).not.toHaveBeenCalled();
  });

  it('Link do Teams para outro site', async () => {
    const { router } = await renderApp(
      `/teams/open?item=${encodeURIComponent('https://outro-site.example/tickets/6')}`,
    );
    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/');
    });

    // Without a session, the way back is the home page too.
    cleanup();
    signedOut();
    methods(false, true);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp(`/teams/open?item=${encodeURIComponent('//outro-site.example')}`);
    await waitFor(() => {
      expect(go).toHaveBeenCalled();
    });
    expect(sent).toEqual([{ email: null, returnUrl: '/', previousUserId: null }]);
  });
});
