import { describe, expect, it } from 'vitest';
import { choiceOf, printAddress, printScale, scaleSpans } from './schedulePrint';

// Spec: cronograma, requirement "Exportar em PDF pela impressão": what the print page draws.
describe('Página de impressão: período e escala', () => {
  it('Escolha na URL', () => {
    const choice = {
      from: '2026-10-12',
      to: '2026-11-03',
      scale: 'weeks' as const,
      display: { baseline: true, critical: false },
    };
    const address = printAddress('p1', choice);

    expect(address).toBe(
      '/projects/p1/schedule/print?from=2026-10-12&to=2026-11-03&scale=weeks&baseline=1&critical=0',
    );
    expect(choiceOf(new URL(address, 'http://x').searchParams, '2026-10-05', '2026-11-03')).toEqual(
      choice,
    );
    // Without a choice, or a wrong one, the dates of the schedule in days, with both on.
    expect(
      choiceOf(new URLSearchParams('from=ontem&to=2026-01-01&scale=x'), '2026-10-05', '2026-11-03'),
    ).toEqual({
      from: '2026-10-05',
      to: '2026-11-03',
      scale: 'days',
      display: { baseline: true, critical: true },
    });
  });

  it('Escala em dias, semanas e meses', () => {
    const scale = printScale('2026-10-05', '2026-10-14', 500);
    // Ten days in 500 px: 50 px each.
    expect([scale.x('2026-10-05'), scale.xEnd('2026-10-05'), scale.x('2026-10-14')]).toEqual([
      0, 50, 450,
    ]);
    expect(scaleSpans('2026-09-29', '2026-10-02', 'days')).toEqual({
      top: [
        { from: '2026-09-29', to: '2026-09-30', label: 'Set 2026' },
        { from: '2026-10-01', to: '2026-10-02', label: 'Out 2026' },
      ],
      bottom: [
        { from: '2026-09-29', to: '2026-09-29', label: '29' },
        { from: '2026-09-30', to: '2026-09-30', label: '30' },
        { from: '2026-10-01', to: '2026-10-01', label: '1' },
        { from: '2026-10-02', to: '2026-10-02', label: '2' },
      ],
    });
    // Weeks from Monday, by the day they start.
    expect(scaleSpans('2026-10-07', '2026-10-14', 'weeks').bottom).toEqual([
      { from: '2026-10-07', to: '2026-10-11', label: '05/10' },
      { from: '2026-10-12', to: '2026-10-14', label: '12/10' },
    ]);
    expect(scaleSpans('2026-12-20', '2027-01-10', 'months')).toEqual({
      top: [
        { from: '2026-12-20', to: '2026-12-31', label: '2026' },
        { from: '2027-01-01', to: '2027-01-10', label: '2027' },
      ],
      bottom: [
        { from: '2026-12-20', to: '2026-12-31', label: 'Dez' },
        { from: '2027-01-01', to: '2027-01-10', label: 'Jan' },
      ],
    });
  });
});
