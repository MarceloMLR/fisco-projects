import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { absenceDays, absenceText, awayText, ownerAwayText } from './absences';

const t = i18n.t.bind(i18n);
const today = '2026-10-19';

// Spec: membros-do-projeto, requirement "Ausências na lista de Membros" (ADR-0141).
describe('Texto das ausências', () => {
  it('Ausente hoje na lista', () => {
    expect(awayText({ start: '2026-10-12', end: '2026-11-14' }, today, t)).toBe(
      'Ausente até 14/11',
    );
    expect(awayText({ start: '2026-10-19', end: '2026-10-19' }, today, t)).toBe('Ausente hoje');
    expect(awayText({ start: '2026-10-26', end: '2026-11-06' }, today, t)).toBe(
      'Ausente de 26/10 a 06/11',
    );
    expect(awayText({ start: '2026-11-06', end: '2026-11-06' }, today, t)).toBe('Ausente em 06/11');
    // Thirty days ahead is still told; past it, or already over, nothing.
    expect(awayText({ start: '2026-11-18', end: '2026-11-20' }, today, t)).toBe(
      'Ausente de 18/11 a 20/11',
    );
    expect(awayText({ start: '2026-11-19', end: '2026-11-20' }, today, t)).toBeUndefined();
    expect(awayText({ start: '2026-10-05', end: '2026-10-16' }, today, t)).toBeUndefined();
    expect(awayText(null, today, t)).toBeUndefined();
  });

  it('Período da ausência', () => {
    expect(absenceText({ start: '2026-11-03', end: '2026-11-14' }, t)).toBe(
      '03/11/2026 a 14/11/2026',
    );
    expect(absenceText({ start: '2026-11-06', end: '2026-11-06' }, t)).toBe('06/11/2026');
    expect(absenceDays('2026-11-03', '2026-11-14')).toBe(12);
    expect(absenceDays('2026-01-01', '2026-12-31')).toBe(365);
  });

  it('Responsável ausente durante a tarefa', () => {
    expect(
      ownerAwayText(
        [
          { start: '2026-10-26', end: '2026-10-26' },
          { start: '2026-11-05', end: '2026-11-14' },
        ],
        t,
      ),
    ).toBe('Responsável ausente: 26/10, 05/11 a 14/11, durante a tarefa');
    expect(ownerAwayText([], t)).toBeUndefined();
  });
});
