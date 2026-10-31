import { act, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import {
  type SimulatedAnimations,
  simulateAnimations,
  stopSimulatingAnimations,
} from '../test/motion';
import { renderApp } from '../test/renderApp';
import { motion, pageFrames } from '../theme/motion';

let animations: SimulatedAnimations;

const pageMoves = () => animations.played.filter(({ frames }) => frames === pageFrames);

// Spec: estrutura-das-telas, requirement "Movimento da troca de página" (ADR-0133).
describe('Movimento da troca de página', () => {
  beforeEach(() => {
    animations = simulateAnimations();
  });
  afterEach(() => {
    stopSimulatingAnimations();
  });

  it('Página que entra subindo', async () => {
    const { router } = await renderApp('/projects');
    await screen.findByRole('heading', { level: 1, name: ptBR.pages.projects.title });
    // The first opening does not move.
    expect(pageMoves()).toEqual([]);

    await act(() => router.navigate('/tickets'));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.tickets.title }),
    ).toBeInTheDocument();
    const [move] = pageMoves();
    expect(move?.element.tagName).toBe('MAIN');
    expect(move?.frames).toEqual(pageFrames);
    expect(move?.options).toEqual({ duration: motion.page, easing: motion.enter, fill: 'none' });
    // The sidebar and the header stay.
    expect(pageMoves()).toHaveLength(1);
  });

  it('Endereço com a mesma página', async () => {
    const { router } = await renderApp('/tickets');
    await screen.findByRole('heading', { level: 1, name: ptBR.pages.tickets.title });

    await act(() => router.navigate('/tickets?view=WithVendor'));

    expect(pageMoves()).toEqual([]);
  });
});
