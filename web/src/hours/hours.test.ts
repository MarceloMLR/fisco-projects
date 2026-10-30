import { describe, expect, it } from 'vitest';
import { demoTimeEntries } from '../test/demoTimeEntries';
import {
  addDays,
  formatHours,
  hoursFieldText,
  hoursRows,
  parseHours,
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
