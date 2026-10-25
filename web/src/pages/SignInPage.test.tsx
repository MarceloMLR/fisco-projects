import { fireEvent, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { signInProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { microsoftSignIn } from '../session/microsoftSignIn';
import { phoneWidth, renderApp } from '../test/renderApp';
import { marina, problem, server, signedOut } from '../test/server';

const text = ptBR.signIn;

function typeEmail(value: string) {
  fireEvent.change(screen.getByLabelText(text.email), { target: { value } });
}

function submit() {
  fireEvent.click(screen.getByRole('button', { name: text.submit }));
}

/** The API accepts the sign-in: the next "who is in the session" answers Marina. */
function acceptSignIn(onRequest?: (request: Request) => Promise<void>) {
  server.use(
    http.post('/api/session/local', async ({ request }) => {
      await onRequest?.(request);
      server.use(http.get('/api/session', () => HttpResponse.json(marina)));
      return new HttpResponse(null, { status: 204 });
    }),
  );
}

// Spec: entrada-e-sessao, requirements "Página de entrada" and "Entrada local".
describe('Entrada local na tela', () => {
  it('Entrada com uma pessoa da demonstração', async () => {
    signedOut();
    const sent: { email?: string; csrf?: string | null } = {};
    acceptSignIn(async (request) => {
      sent.email = ((await request.json()) as { email: string }).email;
      sent.csrf = request.headers.get('X-CSRF');
    });
    const { router } = await renderApp('/sign-in');

    typeEmail(`  ${marina.email.toUpperCase()} `);
    submit();

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/');
    expect(sent).toEqual({ email: marina.email.toUpperCase(), csrf: '1' });
  });

  it('E-mail lembrado', async () => {
    signedOut();
    acceptSignIn();
    const first = await renderApp('/sign-in');
    typeEmail(marina.email);
    submit();
    await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title });
    first.unmount();

    signedOut();
    await renderApp('/sign-in');

    expect(screen.getByLabelText(text.email)).toHaveValue(marina.email);
  });

  it('E-mail fora da demonstração', async () => {
    signedOut();
    server.use(http.post('/api/session/local', () => problem(403, signInProblems.notInDirectory)));
    const { router } = await renderApp('/sign-in');

    typeEmail('alguem@empresa.example');
    submit();

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(text.refused.title);
    expect(alert).toHaveTextContent(text.refused.notInDirectory);
    expect(router.state.location.pathname).toBe('/sign-in');
    expect(window.localStorage.length).toBe(0);
  });

  it('Conta desativada', async () => {
    signedOut();
    server.use(http.post('/api/session/local', () => problem(403, signInProblems.inactive)));
    await renderApp('/sign-in');

    typeEmail(marina.email);
    submit();

    expect(await screen.findByRole('alert')).toHaveTextContent(text.refused.inactive);
  });

  it('Falha ao entrar', async () => {
    signedOut();
    server.use(http.post('/api/session/local', () => problem(500)));
    await renderApp('/sign-in');

    typeEmail(marina.email);
    submit();

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(text.failed.title);
    expect(alert).toHaveTextContent(text.failed.body);
  });

  it('E-mail vazio ou inválido', async () => {
    signedOut();
    let calls = 0;
    server.use(
      http.post('/api/session/local', () => {
        calls++;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await renderApp('/sign-in');

    submit();
    expect(await screen.findByText(text.errors.emailRequired)).toBeInTheDocument();
    expect(screen.getByLabelText(text.email)).toHaveAttribute('aria-invalid', 'true');

    typeEmail('marina.costa');
    submit();
    expect(await screen.findByText(text.errors.emailInvalid)).toBeInTheDocument();
    expect(calls).toBe(0);
  });

  it('Textos da entrada de demonstração', async () => {
    signedOut();
    await renderApp('/sign-in');

    expect(await screen.findByText(text.hintLocal)).toBeVisible();
    expect(screen.getByText(text.noteLocal)).toBeVisible();
    expect(screen.queryByText(text.note)).not.toBeInTheDocument();
  });

  it('Textos da entrada pela Microsoft', async () => {
    signedOut();
    server.use(
      http.get('/api/session/sign-in-options', () =>
        HttpResponse.json({ localSignIn: false, entraSignIn: true }),
      ),
    );
    await renderApp('/sign-in');

    await waitFor(() => {
      expect(screen.getByText(text.hint)).toBeVisible();
    });
    expect(screen.getByText(text.note)).toBeVisible();
  });

  it('Página de entrada no celular', async () => {
    signedOut();
    await renderApp('/sign-in', phoneWidth);

    expect(screen.getByRole('heading', { level: 1, name: text.title })).toBeInTheDocument();
    expect(screen.getByText(text.lead)).toBeInTheDocument();
    expect(screen.getByText(text.foot)).toBeInTheDocument();
  });
});

const microsoft = 'https://login.microsoftonline.com/tenant/oauth2/v2.0/authorize?login_hint=x';

/** The sign-in methods of the installation, as the API answers them. */
function methods(localSignIn: boolean, entraSignIn: boolean) {
  server.use(
    http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn, entraSignIn })),
  );
}

/** The API starts the sign-in through Entra ID; gives what the page sent. */
function startEntra() {
  const sent: { body?: unknown; csrf?: string | null } = {};
  server.use(
    http.post('/api/session/entra', async ({ request }) => {
      sent.body = await request.json();
      sent.csrf = request.headers.get('X-CSRF');
      return HttpResponse.json({ location: microsoft });
    }),
  );
  return sent;
}

function watchMicrosoft() {
  return vi.spyOn(microsoftSignIn, 'go').mockImplementation(() => undefined);
}

// Spec: entrada-e-sessao, requirements "Entrada pelo Entra ID", "Recusas na entrada pelo Entra" and "Entrada local"
// (ADR-0117).
describe('Entrada pelo Entra na tela', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('Entrada pelo Entra com a dica do e-mail', async () => {
    signedOut();
    methods(false, true);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp('/sign-in');

    typeEmail(marina.email);
    submit();

    await waitFor(() => {
      expect(go).toHaveBeenCalledWith(microsoft);
    });
    expect(sent).toEqual({
      body: { email: marina.email, returnUrl: '/', previousUserId: null },
      csrf: '1',
    });
    expect(screen.getByRole('button', { name: text.submit })).toBeDisabled();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('Tela pedida na entrada pelo Entra', async () => {
    signedOut();
    methods(false, true);
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp('/projects?status=open');
    await screen.findByLabelText(text.email);

    typeEmail(marina.email);
    submit();

    await waitFor(() => {
      expect(go).toHaveBeenCalled();
    });
    expect(sent.body).toMatchObject({ returnUrl: '/projects?status=open' });
  });

  it('Entradas local e pelo Entra juntas', async () => {
    signedOut();
    methods(true, true);
    server.use(http.post('/api/session/local', () => problem(403, signInProblems.notInDirectory)));
    const sent = startEntra();
    const go = watchMicrosoft();
    await renderApp('/sign-in');

    expect(await screen.findByText(text.hintBoth)).toBeVisible();
    expect(screen.getByText(text.noteBoth)).toBeVisible();
    typeEmail('joana.lima@empresa.example');
    submit();

    await waitFor(() => {
      expect(go).toHaveBeenCalledWith(microsoft);
    });
    expect(sent.body).toMatchObject({ email: 'joana.lima@empresa.example' });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('Falha ao começar a entrada pelo Entra', async () => {
    signedOut();
    methods(false, true);
    server.use(http.post('/api/session/entra', () => problem(500)));
    const go = watchMicrosoft();
    await renderApp('/sign-in');

    typeEmail(marina.email);
    submit();

    expect(await screen.findByRole('alert')).toHaveTextContent(text.failed.title);
    expect(go).not.toHaveBeenCalled();
  });

  it.each([
    ['Convidado recusado', 'guest', text.refused.guest],
    ['Sem atribuição no Entra', 'notAssigned', text.refused.notAssigned],
    ['Desativado na entrada pelo Entra', 'inactive', text.refused.inactive],
    ['Falha na volta da Microsoft', 'failed', text.failed.body],
  ])('%s', async (_, refused, message) => {
    signedOut();
    methods(false, true);
    const { router } = await renderApp(`/sign-in?refused=${refused}`);

    expect(await screen.findByRole('alert')).toHaveTextContent(message);
    await waitFor(() => {
      expect(router.state.location.search).toBe('');
    });
    expect(router.state.location.pathname).toBe('/sign-in');
  });
});
