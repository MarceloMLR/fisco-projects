import { describe, expect, it } from 'vitest';
import { demoTimeEntries } from '../test/demoTimeEntries';
import { formatMonthText } from '../format/dates';
import {
  addDays,
  addMonths,
  formatHours,
  hoursFieldText,
  hoursRows,
  monthEnd,
  monthStart,
  parseHours,
  parseHoursUpTo,
  plainHoursText,
  weekEnd,
  weekStart,
  workOf,
} from './hours';

/** The number of a ticket as the screens show it, written so the color rule does not take it for a color. */
const ticketNumber = (number: number) => `#${String(number)}`;

// Spec: horas-do-projeto: "Apontar pela tela" and "Minhas horas pela tela" (ADR-0128).
describe('Horas digitadas', () => {
  it('Horas em decimal ou em horas e minutos', () => {
    expect(['1,5', '1.5', '1:30', ' 8 ', '0,25', '0:15', '24'].map(parseHours)).toEqual([
      1.5, 1.5, 1.5, 8, 0.25, 0.25, 24,
    ]);
  });

  it('Horas fora dos quartos de hora', () => {
    expect(['1,1', '0', '25', '1:20', 'uma', '', '-1', '24:15'].map(parseHours)).toEqual(
      Array.from({ length: 8 }, () => undefined),
    );
  });

  it('Horas na tela', () => {
    expect([1.5, 8, 27.25, 0.25].map(formatHours)).toEqual(['1,5 h', '8 h', '27,25 h', '0,25 h']);
    expect(hoursFieldText(7.75)).toBe('7,75');
  });
});

describe('Semana de Minhas horas', () => {
  it('Semana de segunda a domingo', () => {
    expect(weekStart('2026-10-29')).toBe('2026-10-26');
    expect(weekStart('2026-10-26')).toBe('2026-10-26');
    expect(weekStart('2026-11-01')).toBe('2026-10-26');
    expect(weekEnd('2026-10-26')).toBe('2026-11-01');
    expect(addDays('2026-10-26', -7)).toBe('2026-10-19');
    expect(weekStart('2027-01-01')).toBe('2026-12-28');
  });

  it('Dias com o total e os apontamentos', () => {
    const rows = hoursRows(demoTimeEntries());

    expect(rows.filter((row) => row.kind === 'day').map((row) => [row.day, row.total])).toEqual([
      ['2026-10-29', 3.5],
      ['2026-10-28', 8],
      ['2026-10-27', 7.5],
      ['2026-10-26', 8],
    ]);
    expect(rows.map((row) => row.kind)).toEqual([
      'day',
      'entry',
      'day',
      'entry',
      'entry',
      'day',
      'entry',
      'entry',
      'day',
      'entry',
    ]);
  });

  it('Tarefa, chamado ou só o projeto', () => {
    const [today, deleted, project, , ticket] = demoTimeEntries();

    expect(
      [today, deleted, project, ticket].map((entry) =>
        entry === undefined ? undefined : workOf(entry),
      ),
    ).toEqual([
      { kind: 'task', name: 'Roteiro de testes' },
      { kind: 'task', name: null },
      { kind: 'project' },
      { kind: 'ticket', text: `${ticketNumber(101)} Erro ao importar XML de entrada` },
    ]);
  });
});

// Spec: horas-do-projeto, "Horas contratadas", "Horas adicionais" and "Ver apontamentos pela tela" (ADR-0128).
describe('Horas dos projetos', () => {
  it('Muitas horas digitadas', () => {
    expect(
      ['400', '12,5', '12.25', ' 99999 ', '0,25'].map((text) => parseHoursUpTo(text, 99_999)),
    ).toEqual([400, 12.5, 12.25, 99_999, 0.25]);
    expect(
      ['0', '10,1', '1.000', '100000', '1:30', '', '-4'].map((text) =>
        parseHoursUpTo(text, 99_999),
      ),
    ).toEqual(Array.from({ length: 7 }, () => undefined));
    expect(parseHoursUpTo('10000', 9_999)).toBeUndefined();
    expect([1200, 12.5, 400.25].map(plainHoursText)).toEqual(['1200', '12,5', '400,25']);
  });

  it('Mês dos apontamentos', () => {
    expect(monthStart('2026-10-29')).toBe('2026-10-01');
    expect(monthEnd('2026-10-29')).toBe('2026-10-31');
    expect(monthEnd('2028-02-10')).toBe('2028-02-29');
    expect(addMonths('2026-10-01', -1)).toBe('2026-09-01');
    expect(addMonths('2026-01-01', -1)).toBe('2025-12-01');
    expect(addMonths('2026-12-01', 1)).toBe('2027-01-01');
    expect(formatMonthText('2026-10-01')).toBe('Outubro de 2026');
  });
});
