import { describe, expect, it } from 'vitest';
import type { Schedule } from '../api/client';
import i18n from '../i18n';
import { demoPeople, demoSchedule, demoScheduleWithSubtasks, demoToday } from '../test/server';
import {
  filterOf,
  filteredSchedule,
  matchesFilter,
  ownerOptions,
  passingCount,
  type ScheduleFilter,
  withFilter,
} from './scheduleFilter';
import { itemsOf } from './schedule';

const t = i18n.t.bind(i18n);
const context = { today: demoToday, me: demoPeople.rafael.id };

/** The phases and the names of their items that a filter leaves. */
const shapeOf = (schedule: Schedule, filter: ScheduleFilter) =>
  filteredSchedule(schedule, filter, context).phases.map((phase) => [
    phase.name,
    phase.items.map((item) => item.name),
  ]);

// Spec: cronograma, requirement "Filtros do cronograma" (GanttA-3). Today is 16/10/2026.
describe('Filtros do cronograma', () => {
  it('Filtrar as atrasadas', () => {
    expect(shapeOf(demoSchedule(), { kind: 'late' })).toEqual([
      ['Levantamento', ['Validar cadastro de produtos']],
    ]);
    expect(passingCount(demoSchedule(), { kind: 'late' }, context)).toBe(1);
  });

  it('Caminho crítico', () => {
    expect(shapeOf(demoSchedule(), { kind: 'critical' })).toEqual([
      ['Levantamento', ['Reunião de abertura', 'Mapear processos fiscais']],
      ['Configuração', ['Parametrizar regras de ICMS', 'Testes integrados']],
      ['Homologação', ['Testes com a equipe do cliente', 'Aceite da homologação']],
    ]);
  });

  it('Minhas tarefas', () => {
    expect(shapeOf(demoSchedule(), { kind: 'mine' })).toEqual([
      ['Levantamento', ['Mapear processos fiscais', 'Validar cadastro de produtos']],
      ['Configuração', ['Parametrizar regras de ICMS']],
    ]);
    // Without a session nothing is anyone's.
    const item = itemsOf(demoSchedule())[1];
    if (item === undefined) {
      throw new Error('No item.');
    }
    expect(matchesFilter(item, { kind: 'mine' }, { today: demoToday, me: undefined })).toBe(false);
  });

  it('Por responsável', () => {
    const schedule = demoScheduleWithSubtasks();
    // A subtask comes under its task, which does not count by itself.
    expect(shapeOf(schedule, { kind: 'owner', owner: demoPeople.carla.id })).toEqual([
      ['Configuração', ['Testes integrados', 'Roteiro de testes']],
    ]);
    expect(passingCount(schedule, { kind: 'owner', owner: demoPeople.carla.id }, context)).toBe(1);
    // The tasks without an owner, not the milestones, which never have one.
    expect(shapeOf(schedule, { kind: 'owner', owner: 'none' })).toEqual([
      ['Configuração', ['Integrar emissão de NF-e', 'Testes integrados', 'Executar testes']],
      ['Homologação', ['Testes com a equipe do cliente']],
    ]);
    const withClient: Schedule = {
      ...schedule,
      phases: schedule.phases.map((phase) => ({
        ...phase,
        items: phase.items.map((item) =>
          item.name === 'Integrar emissão de NF-e' ? { ...item, ownedByClient: true } : item,
        ),
      })),
    };
    expect(shapeOf(withClient, { kind: 'owner', owner: 'client' })).toEqual([
      ['Configuração', ['Integrar emissão de NF-e']],
    ]);
  });

  it('Próximas duas semanas', () => {
    // From 16/10 to 29/10: the tasks still open happening in it; not the late one, the done ones nor the ones after.
    expect(shapeOf(demoScheduleWithSubtasks(), { kind: 'nextTwoWeeks' })).toEqual([
      [
        'Configuração',
        [
          'Parametrizar regras de ICMS',
          'Integrar emissão de NF-e',
          'Testes integrados',
          'Roteiro de testes',
          'Executar testes',
        ],
      ],
    ]);
  });

  it('Datas-limite estouradas', () => {
    const schedule = demoSchedule();
    for (const phase of schedule.phases) {
      phase.items = phase.items.map((item) =>
        item.name === 'Validar cadastro de produtos'
          ? { ...item, deadline: '2026-10-14', deadlineDeviation: 2 }
          : item.name === 'Mapear processos fiscais'
            ? { ...item, deadline: '2026-10-08', deadlineDeviation: 1 }
            : item,
      );
    }

    // The done one went past it, but is not missed any more.
    expect(shapeOf(schedule, { kind: 'missedDeadlines' })).toEqual([
      ['Levantamento', ['Validar cadastro de produtos']],
    ]);
  });

  it('Filtro sem resultado', () => {
    const filtered = filteredSchedule(demoSchedule(), { kind: 'missedDeadlines' }, context);

    expect(filtered.phases).toEqual([]);
    // The numbers stay those of the whole schedule.
    expect([filtered.start, filtered.end, filtered.progress]).toEqual([
      '2026-10-05',
      '2026-11-03',
      42,
    ]);
  });

  it('Filtro no endereço', () => {
    const search = new URLSearchParams('view=list&scale=weeks');

    const late = withFilter(search, { kind: 'late' });
    const carla = withFilter(late, { kind: 'owner', owner: demoPeople.carla.id });

    expect(late.toString()).toBe('view=list&scale=weeks&filter=late');
    expect(filterOf(late)).toEqual({ kind: 'late' });
    expect(filterOf(carla)).toEqual({ kind: 'owner', owner: demoPeople.carla.id });
    expect(withFilter(carla, undefined).toString()).toBe('view=list&scale=weeks');
    // A filter that does not exist is ignored.
    expect(filterOf(new URLSearchParams('filter=everything'))).toBeUndefined();
    expect(filterOf(new URLSearchParams('filter=owner'))).toBeUndefined();
  });

  it('Opções de responsável', () => {
    expect(
      ownerOptions(demoScheduleWithSubtasks(), 'Alfa Distribuidora', t).map(
        (option) => option.label,
      ),
    ).toEqual([
      demoPeople.carla.name,
      demoPeople.marina.name,
      demoPeople.rafael.name,
      t('projects.schedule.clientOwner', { name: 'Alfa Distribuidora' }),
      t('projects.schedule.noOwner'),
    ]);
    expect(ownerOptions(demoSchedule(), null, t).map((option) => option.owner)).toEqual([
      demoPeople.marina.id,
      demoPeople.rafael.id,
      'none',
    ]);
  });
});
