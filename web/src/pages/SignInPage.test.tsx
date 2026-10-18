import { fireEvent, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { signInProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
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
      http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn: false })),
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
