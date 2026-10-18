import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';

const navigationName = ptBR.shell.mainNavigation;

// Spec: estrutura-das-telas.
describe('Barra lateral no computador', () => {
  it('Navegação principal', () => {
    renderApp('/');

    const sidebar = screen.getByRole('navigation', { name: navigationName });
    expect(within(sidebar).getByText(ptBR.app.name)).toBeInTheDocument();
    const items = within(sidebar)
      .getAllByRole('link')
      .map((link) => link.textContent);
    expect(items).toEqual([ptBR.app.name, ptBR.nav.home, ptBR.nav.projects, ptBR.nav.tickets]);
  });

  it('Item da página atual', () => {
    for (const [path, item] of [
      ['/', ptBR.nav.home],
      ['/projects', ptBR.nav.projects],
      ['/tickets', ptBR.nav.tickets],
    ] as const) {
      const { unmount } = renderApp(path);

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
  it('Trilha da página', () => {
    renderApp('/tickets');

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
  it('Janela sem rolagem', () => {
    for (const width of [1440, phoneWidth]) {
      const { unmount } = renderApp('/', width);

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
    renderApp('/', phoneWidth);
    expect(screen.queryByRole('navigation', { name: navigationName })).not.toBeInTheDocument();
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.home.title);

    fireEvent.click(screen.getByRole('button', { name: ptBR.shell.openMenu }));
    const menu = await screen.findByRole('dialog', { name: ptBR.shell.menu });
    const items = within(menu)
      .getAllByRole('link')
      .map((link) => link.textContent);
    expect(items).toEqual([ptBR.nav.home, ptBR.nav.projects, ptBR.nav.tickets]);

    fireEvent.click(within(menu).getByRole('link', { name: ptBR.nav.projects }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: ptBR.shell.menu })).not.toBeInTheDocument();
    });
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.projects.title);
  });
});
