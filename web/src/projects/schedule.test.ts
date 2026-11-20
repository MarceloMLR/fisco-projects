import { describe, expect, it } from 'vitest';
import type { ScheduleItem } from '../api/client';
import { demoItem } from '../test/server';
import { actualEndOf, phaseEndsOf, plannedEndOf } from './schedule';

const phaseId = '0199f5a2-7c1e-7d40-9a1b-00000000a001';

const task = (index: number, value: Partial<ScheduleItem>) =>
  demoItem(index, phaseId, {
    name: `Tarefa ${String(index)}`,
    start: '2026-10-13',
    end: '2026-10-15',
    ...value,
  });

// Spec: cronograma, requirement "Fim previsto e fim real" (ADR-0020, ADR-0021, ADR-0167).
describe('Fim previsto e fim real', () => {
  it('Tarefa concluída depois do previsto', () => {
    const done = task(1, { baselineEnd: '2026-10-15', progress: 100, completedOn: '2026-10-20' });

    expect([plannedEndOf(done), actualEndOf(done)]).toEqual(['2026-10-15', '2026-10-20']);
  });

  it('Marco atingido antes do previsto', () => {
    const milestone = task(2, {
      kind: 'Milestone',
      start: '2026-10-23',
      end: '2026-10-23',
      baselineEnd: '2026-10-23',
      reachedOn: '2026-10-22',
    });

    expect([plannedEndOf(milestone), actualEndOf(milestone)]).toEqual(['2026-10-23', '2026-10-22']);
  });

  it('Item em aberto ou incluído depois da linha de base', () => {
    const open = task(3, { baselineEnd: '2026-10-15', progress: 60 });
    const added = task(4, { baselineEnd: null, progress: 100, completedOn: '2026-10-16' });

    expect(actualEndOf(open)).toBeNull();
    expect(plannedEndOf(added)).toBeNull();
  });

  it('Fim previsto e real da fase', () => {
    const first = task(5, { baselineEnd: '2026-10-13', progress: 100, completedOn: '2026-10-14' });
    const second = task(6, { baselineEnd: '2026-10-15', progress: 100, completedOn: '2026-10-16' });

    expect(phaseEndsOf({ items: [first, second] })).toEqual({
      planned: '2026-10-15',
      actual: '2026-10-16',
    });
    // A task reopened leaves the phase without an actual end.
    const reopened = { ...second, progress: 80, completedOn: null };
    expect(phaseEndsOf({ items: [first, reopened] })).toEqual({
      planned: '2026-10-15',
      actual: null,
    });
    expect(phaseEndsOf({ items: [] })).toEqual({ planned: null, actual: null });
  });
});
