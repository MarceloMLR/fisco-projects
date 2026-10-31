import { describe, expect, it } from 'vitest';
import { ganttPeriod } from './ganttLayout';
import { draggedDates, scaleTiers, timelineGeometry } from './timelineGeometry';

// Spec: cronograma, requirements "Linha do tempo" and "Escala e ajuste ao espaço": where each day falls in the chart.
describe('Geometria da linha do tempo', () => {
  it('Dias na escala Dias', () => {
    // 05/10 to 03/11/2026, 20 px a day.
    const geometry = timelineGeometry(ganttPeriod('2026-10-05', '2026-11-03', 'days'), 'days', 20);

    expect(geometry.width).toBe(600);
    // Validar cadastro de produtos, 13/10 to 15/10: from 160 px to 220 px.
    expect([geometry.xOf('2026-10-13'), geometry.xAfter('2026-10-15')]).toEqual([160, 220]);
    expect(geometry.dayWidth('2026-10-13')).toBe(20);
    expect(geometry.dayAt(165)).toBe('2026-10-13');
    // Never a day out of the period.
    expect([geometry.dayAt(-5), geometry.dayAt(900)]).toEqual(['2026-10-05', '2026-11-03']);
  });

  it('Dias nas escalas Semanas e Meses', () => {
    // Weeks of 70 px: 10 px a day, from Monday 05/10.
    const weeks = timelineGeometry(ganttPeriod('2026-10-05', '2026-11-03', 'weeks'), 'weeks', 70);
    expect([weeks.period.end, weeks.width, weeks.xOf('2026-10-13')]).toEqual([
      '2026-11-08',
      350,
      80,
    ]);
    expect(weeks.dayWidth('2026-10-13')).toBe(10);

    // Months of 310 px: October has 31 days of 10 px, November 30 days of a little more.
    const months = timelineGeometry(
      ganttPeriod('2026-10-05', '2026-11-03', 'months'),
      'months',
      310,
    );
    expect([
      months.xOf('2026-10-16'),
      months.xOf('2026-11-01'),
      months.xAfter('2026-10-31'),
    ]).toEqual([150, 310, 310]);
    expect(months.dayWidth('2026-11-01')).toBeCloseTo(310 / 30);
    expect([months.dayAt(150), months.dayAt(311), months.dayAt(619)]).toEqual([
      '2026-10-16',
      '2026-11-01',
      '2026-11-30',
    ]);
  });

  it('Escala em duas linhas', () => {
    const today = '2026-10-16';
    const days = scaleTiers(
      timelineGeometry(ganttPeriod('2026-10-05', '2026-11-03', 'days'), 'days', 20),
      today,
    );

    // The month and the year above, each day below, and today marked.
    expect(days.top.map((cell) => [cell.text, cell.x, cell.width])).toEqual([
      ['Out 2026', 0, 540],
      ['Nov 2026', 540, 60],
    ]);
    expect(days.bottom).toHaveLength(30);
    expect(days.bottom[0]?.text).toBe('5');
    expect(days.bottom.filter((cell) => cell.today).map((cell) => cell.text)).toEqual(['16']);

    // Each week by its Monday, and the week of today.
    const weeks = scaleTiers(
      timelineGeometry(ganttPeriod('2026-10-05', '2026-11-03', 'weeks'), 'weeks', 70),
      today,
    );
    expect(weeks.bottom.map((cell) => cell.text)).toEqual([
      '05/10',
      '12/10',
      '19/10',
      '26/10',
      '02/11',
    ]);
    expect(weeks.bottom.find((cell) => cell.today)?.text).toBe('12/10');

    // Each month, under its year.
    const months = scaleTiers(
      timelineGeometry(ganttPeriod('2026-10-05', '2026-11-03', 'months'), 'months', 310),
      today,
    );
    expect(months.bottom.map((cell) => [cell.text, cell.today])).toEqual([
      ['Out', true],
      ['Nov', false],
    ]);
    expect(months.top.map((cell) => [cell.text, cell.width])).toEqual([['2026', 620]]);
  });
});

// Spec: cronograma, requirement "Arrastar na linha do tempo".
describe('Datas de uma barra arrastada', () => {
  it('Arrastar e puxar as bordas', () => {
    // Validar cadastro de produtos, 13/10 to 15/10.
    expect(draggedDates('move', '2026-10-13', '2026-10-15', 3)).toEqual({
      start: '2026-10-16',
      end: '2026-10-18',
    });
    expect(draggedDates('end', '2026-10-13', '2026-10-15', 5)).toEqual({
      start: '2026-10-13',
      end: '2026-10-20',
    });
    expect(draggedDates('start', '2026-10-13', '2026-10-15', -2)).toEqual({
      start: '2026-10-11',
      end: '2026-10-15',
    });
    // An edge never goes past the other one: the task keeps a day.
    expect(draggedDates('end', '2026-10-13', '2026-10-15', -9)).toEqual({
      start: '2026-10-13',
      end: '2026-10-13',
    });
    expect(draggedDates('start', '2026-10-13', '2026-10-15', 9)).toEqual({
      start: '2026-10-15',
      end: '2026-10-15',
    });
  });
});
