import { describe, expect, it } from 'vitest';
import { ganttPeriod, ganttUnitWidth, minUnitWidth } from './ganttLayout';

// Spec: cronograma, requirement "Escala e ajuste ao espaço".
describe('Escala e ajuste ao espaço', () => {
  it('Período ajustado ao espaço', () => {
    // 05/10 to 13/11/2026: 40 days in 1,000 px are 25 px each, wider than the narrowest day, so nothing scrolls.
    const period = ganttPeriod('2026-10-05', '2026-11-13', 'days');

    expect(period).toEqual({ start: '2026-10-05', end: '2026-11-13', units: 40 });
    expect(ganttUnitWidth('days', period.units, 1000)).toBe(25);
    expect(ganttUnitWidth('days', period.units, 1000) * period.units).toBe(1000);
  });

  it('Período que não cabe', () => {
    // A year of days in 1,000 px would be 2.7 px each: each day keeps the narrowest width, and the timeline scrolls.
    const period = ganttPeriod('2026-01-05', '2027-01-04', 'days');

    expect(period.units).toBe(365);
    expect(ganttUnitWidth('days', period.units, 1000)).toBe(minUnitWidth.days);
    expect(ganttUnitWidth('days', period.units, 1000) * period.units).toBeGreaterThan(1000);
  });

  it('Mudar a escala', () => {
    // Monday 05/10 to Friday 13/11/2026: six whole weeks, from Monday 05/10 to Sunday 15/11; and two whole months.
    const weeks = ganttPeriod('2026-10-05', '2026-11-13', 'weeks');
    const months = ganttPeriod('2026-10-05', '2026-11-13', 'months');

    expect(weeks).toEqual({ start: '2026-10-05', end: '2026-11-15', units: 6 });
    expect(months).toEqual({ start: '2026-10-01', end: '2026-11-30', units: 2 });
    expect(ganttUnitWidth('weeks', weeks.units, 900)).toBe(150);
    expect(ganttUnitWidth('months', months.units, 900)).toBe(450);
    // A schedule that starts on a Sunday and ends on a Monday spans three weeks; months across a year end count right.
    expect(ganttPeriod('2026-10-04', '2026-10-12', 'weeks')).toEqual({
      start: '2026-09-28',
      end: '2026-10-18',
      units: 3,
    });
    expect(ganttPeriod('2026-11-20', '2027-02-03', 'months').units).toBe(4);
  });

  it('Largura sem medida', () => {
    expect(ganttUnitWidth('weeks', 6, 0)).toBe(minUnitWidth.weeks);
    expect(ganttUnitWidth('months', 0, 800)).toBe(minUnitWidth.months);
  });
});
