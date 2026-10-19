import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { server } from '../test/server';

const navigationName = ptBR.shell.mainNavigation;
const logoUrl = '/api/installation/logo?v=639000000000000000';

function signedOut() {
  server.use(http.get('/api/session', () => new HttpResponse(null, { status: 401 })));
}

function installation(name: string, logo: string | null = null) {
  server.use(http.get('/api/installation', () => HttpResponse.json({ name, logoUrl: logo })));
}

/** The brand at the top of the sidebar: the link to the home page, with the name and the logo or the badge. */
async function sidebarBrand(name: string) {
  const sidebar = screen.getByRole('navigation', { name: navigationName });
  return (await within(sidebar).findByText(name)).closest('a') as HTMLElement;
}

// Spec: configuracao-da-instalacao, requirements "Nome da instalação", "Logo da instalação" and "Marca visível sem
// sessão" (screens that show the brand).
describe('Marca da instalação', () => {
  it('Nome padrão', async () => {
    await renderApp('/');

    expect(await sidebarBrand('FiscoProjects')).toHaveAttribute('href', '/');
    await waitFor(() => {
      expect(document.title).toBe('FiscoProjects');
    });
  });

  it('Nome da instalação na barra lateral e na aba', async () => {
    installation('Consultoria Fictícia');

    await renderApp('/');

    expect(await sidebarBrand('Consultoria Fictícia')).toBeInTheDocument();
    expect(screen.queryByText('FiscoProjects')).not.toBeInTheDocument();
    await waitFor(() => {
      expect(document.title).toBe('Consultoria Fictícia');
    });
  });

  it('Logo no lugar do selo', async () => {
    installation('Consultoria Fictícia', logoUrl);

    await renderApp('/');

    const brand = await sidebarBrand('Consultoria Fictícia');
    // The logo is decoration beside the name: no text of its own.
    expect(brand.querySelector('img')).toHaveAttribute('src', logoUrl);
    expect(brand.querySelector('img')).toHaveAttribute('alt', '');
    expect(brand.querySelector('svg')).toBeNull();
  });

  it('Selo sem logo', async () => {
    installation('Consultoria Fictícia');

    await renderApp('/');

    const brand = await sidebarBrand('Consultoria Fictícia');
    expect(brand.querySelector('img')).toBeNull();
    expect(brand.querySelector('svg')).not.toBeNull();
  });

  it('Marca na página de entrada', async () => {
    signedOut();
    installation('Consultoria Fictícia', logoUrl);

    await renderApp('/sign-in');

    const name = await screen.findByText('Consultoria Fictícia');
    expect(name.querySelector('img')).toHaveAttribute('src', logoUrl);
    await waitFor(() => {
      expect(document.title).toBe('Consultoria Fictícia');
    });
  });

  it('Nome do produto se a marca não chega', async () => {
    signedOut();
    server.use(http.get('/api/installation', () => new HttpResponse(null, { status: 500 })));

    await renderApp('/sign-in');

    expect(await screen.findByText(ptBR.app.name)).toBeInTheDocument();
  });
});
