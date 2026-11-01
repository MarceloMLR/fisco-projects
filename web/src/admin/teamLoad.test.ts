import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { demoTeamLoad } from '../test/demoTeamLoad';
import {
  absencePeriodText,
  loadText,
  shownPeople,
  taskPeriodText,
  teamLoadMeta,
  weekAfter,
  weekHoursText,
  weekText,
} from './teamLoad';

const t = i18n.t.bind(i18n);

// Spec: horas-do-projeto, requirement "Carga da equipe pela tela" (ADR-0142).
describe('Textos da carga da equipe', () => {
  it('Carga e capacidade da semana', () => {
    expect(loadText({ load: 36, capacity: 40 }, t)).toBe('36 de 40 h');
    expect(loadText({ load: 13.67, capacity: 30 }, t)).toBe('13,7 de 30 h');
    expect(weekHoursText(12.5)).toBe('12,5 h');
    expect(weekText({ start: '2026-10-26', end: '2026-11-01' })).toBe('26/10 – 01/11');
    expect(absencePeriodText({ start: '2026-10-29', end: '2026-10-30' }, t)).toBe('29/10 a 30/10');
    expect(absencePeriodText({ start: '2026-11-06', end: '2026-11-06' }, t)).toBe('06/11');
    expect(taskPeriodText({ start: '2026-10-26', end: '2026-10-30' }, t)).toBe(
      '26/10/2026 a 30/10/2026',
    );
    expect(weekAfter('2026-10-26', -1)).toBe('2026-10-19');
    expect(weekAfter('2026-12-28', 1)).toBe('2027-01-04');
  });

  it('Pessoas da visão e da busca', () => {
    const { people } = demoTeamLoad();

    expect(shownPeople(people, 'Overloaded', '').map((person) => person.name)).toEqual([
      'Diego Torres',
    ]);
    expect(shownPeople(people, 'All', 'CARLA').map((person) => person.name)).toEqual([
      'Carla Mendes',
    ]);
    expect(shownPeople(people, 'All', 'rafael.souza@').map((person) => person.name)).toEqual([
      'Rafael Souza',
    ]);
    expect(teamLoadMeta(people, t)).toBe('5 pessoas · 1 acima da capacidade');
    expect(teamLoadMeta(people.slice(0, 1), t)).toBe('1 pessoa');
  });
});
