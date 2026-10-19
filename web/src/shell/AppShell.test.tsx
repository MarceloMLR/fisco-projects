import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { marina, problem, server } from '../test/server';

const navigationName = ptBR.shell.mainNavigation;

// Spec: estrutura-das-telas.
describe('Barra lateral no computador', () => {
  it('Navegação principal', async () => {
    await renderApp('/');

    const sidebar = screen.getByRole('navigation', { name: navigationName });
    expect(within(sidebar).getByText(ptBR.app.name)).toBeInTheDocument();
    const items = within(sidebar)
      .getAllByRole('link')
      .map((link) => link.textContent);
    expect(items).toEqual([ptBR.app.name, ptBR.nav.home, ptBR.nav.projects, ptBR.nav.tickets]);
  });

  it('Item da página atual', async () => {
    for (const [path, item] of [
      ['/', ptBR.nav.home],
      ['/projects', ptBR.nav.projects],
      ['/tickets', ptBR.nav.tickets],
    ] as const) {
      const { unmount } = await renderApp(path);

      const sidebar = screen.getByRole('navigation', { name: navigationName });
      const current = within(sidebar)
        .getAllByRole('link')
        .filter((link) => link.getAttribute('aria-current') === 'page')
        .map((link) => link.textContent);
      expect(current, path).toEqual([item]);
      unmount();
    }
  });
});

describe('Cabeçalho com a trilha', () => {
  it('Trilha da página', async () => {
    await renderApp('/tickets');

    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    const levels = within(trail).getAllByRole('listitem');
    expect(levels.at(-1)).toHaveTextContent(ptBR.pages.tickets.title);
    expect(within(levels.at(-1) ?? trail).getByText(ptBR.pages.tickets.title)).toHaveAttribute(
      'aria-current',
      'page',
    );
  });
});

describe('Conteúdo na altura da janela', () => {
  it('Janela sem rolagem', async () => {
    for (const width of [1440, phoneWidth]) {
      const { unmount } = await renderApp('/', width);

      const frame = getComputedStyle(screen.getByTestId('app-frame'));
      expect(frame.height).toBe('100dvh');
      expect(frame.overflow).toBe('hidden');
      expect(getComputedStyle(screen.getByRole('main')).overflow).toBe('auto');
      unmount();
    }
  });
});

describe('Estrutura no celular', () => {
  it('Menu no celular', async () => {
    await renderApp('/', phoneWidth);
    expect(screen.queryByRole('navigation', { name: navigationName })).not.toBeInTheDocument();
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.home.title);

    fireEvent.click(screen.getByRole('button', { name: ptBR.shell.openMenu }));
    const menu = await screen.findByRole('dialog', { name: ptBR.shell.menu });
    expect(menu).toHaveTextContent(marina.name);
    expect(menu).toHaveTextContent(marina.email);
    const items = within(menu)
      .getAllByRole('link')
      .map((link) => link.textContent);
    expect(items).toEqual([
      ptBR.nav.home,
      ptBR.nav.projects,
      ptBR.nav.tickets,
      ptBR.pages.profile.title,
    ]);

    fireEvent.click(within(menu).getByRole('link', { name: ptBR.nav.projects }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: ptBR.shell.menu })).not.toBeInTheDocument();
    });
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.projects.title);

    // My profile, at the end of the menu.
    fireEvent.click(screen.getByRole('button', { name: ptBR.shell.openMenu }));
    fireEvent.click(
      within(await screen.findByRole('dialog', { name: ptBR.shell.menu })).getByRole('link', {
        name: ptBR.pages.profile.title,
      }),
    );
    expect(await screen.findByRole('heading', { level: 1, name: marina.name })).toBeInTheDocument();
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.profile.title);
  });
});

describe('Conta no cabeçalho', () => {
  it('Conta no cabeçalho', async () => {
    await renderApp('/');

    const account = screen.getByRole('button', {
      name: ptBR.shell.account.replace('{{name}}', marina.name),
    });
    expect(account).toHaveTextContent('MC');
    expect(account).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(account);

    const menu = await screen.findByRole('menu', { name: ptBR.shell.accountMenu });
    expect(account).toHaveAttribute('aria-expanded', 'true');
    expect(menu).toHaveTextContent(marina.name);
    expect(menu).toHaveTextContent(marina.email);
    expect(
      within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([ptBR.pages.profile.title, ptBR.shell.signOut]);
    const themes = within(menu).getByRole('radiogroup', { name: ptBR.profile.theme });
    expect(
      within(themes)
        .getAllByRole('radio')
        .map((option) => [option.getAttribute('aria-label'), option.getAttribute('aria-checked')]),
    ).toEqual([
      [ptBR.profile.themes.Light, 'false'],
      [ptBR.profile.themes.Dark, 'false'],
      [ptBR.profile.themes.Automatic, 'true'],
    ]);
  });

  it('Sair', async () => {
    let signedOutCalls = 0;
    server.use(
      http.post('/api/session/sign-out', ({ request }) => {
        signedOutCalls++;
        expect(request.headers.get('X-CSRF')).toBe('1');
        server.use(http.get('/api/session', () => problem(401)));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { router } = await renderApp('/projects');

    fireEvent.click(
      screen.getByRole('button', { name: ptBR.shell.account.replace('{{name}}', marina.name) }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: ptBR.shell.signOut }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.signIn.title }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/sign-in');
    expect(signedOutCalls).toBe(1);
  });
});
