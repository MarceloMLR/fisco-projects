import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import ptBR from './locales/pt-BR.json';
import { renderApp } from './test/renderApp';

// Spec: estrutura-das-telas.
describe('Endereços das telas', () => {
  it('Endereço de uma página', () => {
    for (const [path, title] of [
      ['/projects', ptBR.pages.projects.title],
      ['/tickets', ptBR.pages.tickets.title],
    ] as const) {
      const { unmount } = renderApp(path);

      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(title);
      unmount();
    }
  });

  it('Endereço desconhecido', async () => {
    const { router } = renderApp('/does-not-exist');

    expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent(
      ptBR.pages.home.title,
    );
    expect(router.state.location.pathname).toBe('/');
  });
});
