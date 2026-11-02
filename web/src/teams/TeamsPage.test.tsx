import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { defaultInstallation, server, signedOut } from '../test/server';
import { safeItemPath, schemeOfTeams } from './teamsPage';

const open = ptBR.teamsPage.open.replace('{{name}}', 'FiscoProjects');

beforeEach(() => {
  // Teams opens the page in its own frame, with no session of the application.
  signedOut();
});

async function openLink(path: string, width?: number) {
  await renderApp(path, width);
  return screen.findByRole('link', { name: open });
}

// Spec: avisos, requirement "Página do FiscoProjects no Teams".
describe('Página do FiscoProjects no Teams', () => {
  it('Abrir o item no navegador', async () => {
    const link = await openLink(`/teams?item=${encodeURIComponent('/tickets/104')}&theme=default`);

    expect(link).toHaveAttribute('href', '/tickets/104');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    expect(screen.getByRole('heading', { level: 1, name: 'FiscoProjects' })).toBeInTheDocument();
  });

  it('Página sem item', async () => {
    // Teams leaves the place of the item empty when the person opens the page by its bar.
    const link = await openLink('/teams?item=&theme=dark');

    expect(link).toHaveAttribute('href', '/');
  });

  it('Endereço de fora recusado', async () => {
    const link = await openLink(
      `/teams?item=${encodeURIComponent('https://outro.example/tickets/104')}`,
    );

    expect(link).toHaveAttribute('href', '/');
  });

  it('Marca da instalação e tema do Teams', async () => {
    server.use(
      http.get('/api/installation', () =>
        HttpResponse.json({ ...defaultInstallation, name: 'Projetos da Empresa' }),
      ),
    );
    await renderApp('/teams?item=%2Ftickets%2F104&theme=contrast', phoneWidth);

    const link = await screen.findByRole('link', {
      name: ptBR.teamsPage.open.replace('{{name}}', 'Projetos da Empresa'),
    });
    expect(link.closest('[data-theme]')).toHaveAttribute('data-theme', 'dark');
  });

  it.each([
    ['/tickets/104', '/tickets/104'],
    ['/projects/0193f0a0/schedule?item=0193f0a1', '/projects/0193f0a0/schedule?item=0193f0a1'],
    [null, '/'],
    ['', '/'],
    ['tickets/104', '/'],
    ['//outro.example/tickets', '/'],
    ['/\\outro.example', '/'],
    ['/\t/outro.example', '/'],
    ['javascript:alert(1)', '/'],
    ['https://outro.example', '/'],
  ])('Caminho do item %j', (item, path) => {
    expect(safeItemPath(item)).toBe(path);
  });

  it.each([
    ['default', 'light'],
    ['dark', 'dark'],
    ['contrast', 'dark'],
    [null, undefined],
    ['{app.theme}', undefined],
  ])('Tema do Teams %j', (theme, scheme) => {
    expect(schemeOfTeams(theme)).toBe(scheme);
  });
});
