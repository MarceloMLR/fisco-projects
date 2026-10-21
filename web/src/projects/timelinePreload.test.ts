import { describe, expect, it, vi } from 'vitest';

const loaded = vi.hoisted(() => vi.fn());

vi.unmock('./timelinePreload');
vi.mock('../schedule-gantt/ScheduleGantt', () => {
  loaded();
  return { default: () => null };
});

// Spec: estrutura-das-telas, requirement "Primeira abertura mais rápida".
describe('Primeira abertura mais rápida', () => {
  it('Linha do tempo carregada antes', async () => {
    const { preloadTimeline } = await import('./timelinePreload');

    preloadTimeline();

    await vi.waitFor(() => {
      expect(loaded).toHaveBeenCalledTimes(1);
    });
  });
});
