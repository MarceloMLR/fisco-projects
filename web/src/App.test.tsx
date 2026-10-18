import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import ptBR from './locales/pt-BR.json';
import { renderApp } from './test/renderApp';

// Spec: aplicacao-web.
describe('Textos da interface em arquivos de tradução', () => {
  it('Texto da tela vem da tradução', () => {
    renderApp('/');

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(ptBR.pages.home.title);
    const navigation = screen.getByRole('navigation', { name: ptBR.shell.mainNavigation });
    for (const label of Object.values(ptBR.nav)) {
      expect(within(navigation).getByRole('link', { name: label })).toBeInTheDocument();
    }
  });
});
