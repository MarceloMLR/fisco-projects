import { act, fireEvent, screen, waitFor } from '@testing-library/react';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { api } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { marina, problem, server, signedOut } from '../test/server';

function signInHeading() {
  return screen.findByRole('heading', { level: 1, name: ptBR.signIn.title });
}

// Spec: entrada-e-sessao, requirement "Página de entrada".
describe('Página de entrada', () => {
  it('Tela aberta sem sessão', async () => {
    signedOut();
    const { router } = await renderApp('/projects?view=list');

    expect(await signInHeading()).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/sign-in');

    server.use(
      http.post('/api/session/local', () => new Response(null, { status: 204 })),
      http.get('/api/session', () => Response.json(marina)),
    );
    fireEvent.change(screen.getByLabelText(ptBR.signIn.email), {
      target: { value: marina.email },
    });
    fireEvent.click(screen.getByRole('button', { name: ptBR.signIn.submit }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.projects.title }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/projects');
    expect(router.state.location.search).toBe('?view=list');
  });

  it('Página de entrada com sessão', async () => {
    const { router } = await renderApp('/sign-in');

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/');
  });
});

// Spec: entrada-e-sessao, requirement "Sessão protegida".
describe('Sessão protegida', () => {
  it('Sessão expirada na tela', async () => {
    const { router } = await renderApp('/tickets');
    expect(screen.getByRole('heading', { level: 1, name: ptBR.pages.tickets.title })).toBeVisible();

    // Any call of the screen that comes back 401 ends the session.
    server.use(http.get('/api/session/sign-in-options', () => problem(401)));
    await act(async () => {
      await api.GET('/api/session/sign-in-options');
    });

    expect(router.state.location.pathname).toBe('/sign-in');
    expect(await signInHeading()).toBeInTheDocument();
  });

  it('API fora do ar ao abrir', async () => {
    server.use(http.get('/api/session', () => Response.error()));
    const { router } = await renderApp('/');

    // The redirect happens right after the check of the session.
    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/sign-in');
    });
  });
});
