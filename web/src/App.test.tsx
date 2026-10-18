import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { App } from './App';
import ptBR from './locales/pt-BR.json';

// Spec: aplicacao-web.
describe('Textos da interface em arquivos de tradução', () => {
    it('Texto da tela vem da tradução', () => {
        render(<App />);

        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(ptBR.app.name);
        expect(screen.getByText(ptBR.app.underConstruction)).toBeInTheDocument();
    });
});
