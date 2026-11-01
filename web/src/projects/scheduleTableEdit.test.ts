import { describe, expect, it } from 'vitest';
import type { ScheduleItem } from '../api/client';
import { demoSchedule } from '../test/server';
import {
  cellError,
  cellRequest,
  cellValueOf,
  editableFields,
  endInvalid,
} from './scheduleTableEdit';
import { scheduleItemErrors } from './scheduleItemValues';

function itemNamed(name: string): ScheduleItem {
  const item = demoSchedule()
    .phases.flatMap((phase) => phase.items)
    .find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

// Spec: cronograma, requirements "Células editáveis na Lista", "Salvar pela célula" and "Valor inválido na célula"
// (ADR-0143).
describe('Regras da edição na tabela', () => {
  it('Células editáveis de cada item', () => {
    const task = itemNamed('Integrar emissão de NF-e');
    const milestone = itemNamed('Aceite da homologação');

    expect([...editableFields(task, true)].toSorted()).toEqual(
      ['dependsOn', 'duration', 'end', 'name', 'progress', 'start'].toSorted(),
    );
    expect([...editableFields(milestone, true)].toSorted()).toEqual(['dependsOn', 'name', 'start']);
    expect([...editableFields({ ...task, hasSubtasks: true }, true)]).toEqual(['name']);
    // Who does not edit the schedule updates only the progress of the tasks it may.
    expect([...editableFields(task, false)]).toEqual(['progress']);
    expect([...editableFields({ ...task, canUpdateProgress: false }, false)]).toEqual([]);
  });

  it('Valor de início das células', () => {
    const task = itemNamed('Integrar emissão de NF-e');

    expect(cellValueOf(task, 'start')).toBe('19/10/2026');
    expect(cellValueOf(task, 'end')).toBe('28/10/2026');
    expect(cellValueOf(task, 'duration')).toBe('8');
    expect(cellValueOf(task, 'progress')).toBe('20');
  });

  it('Conferência das células', () => {
    const task = itemNamed('Integrar emissão de NF-e');

    expect(cellError('name', '  ', task)).toBe(scheduleItemErrors.nameRequired);
    expect(cellError('name', 'n'.repeat(201), task)).toBe(scheduleItemErrors.nameTooLong);
    expect(cellError('start', '31/02/2026', task)).toBe(scheduleItemErrors.startInvalid);
    expect(cellError('end', '16/10/2026', task)).toBe(endInvalid);
    expect(cellError('end', '19/10/2026', task)).toBeUndefined();
    expect(cellError('duration', '0', task)).toBe(scheduleItemErrors.durationInvalid);
    expect(cellError('duration', '1000', task)).toBe(scheduleItemErrors.durationInvalid);
    expect(cellError('duration', '10', task)).toBeUndefined();
    expect(cellError('progress', '101', task)).toBe(scheduleItemErrors.progressInvalid);
    expect(cellError('progress', '60', task)).toBeUndefined();
  });

  it('Pedido de cada célula', () => {
    const task = itemNamed('Integrar emissão de NF-e');

    const duration = cellRequest('duration', '10', task);
    expect(duration?.kind === 'item' ? duration.body : undefined).toMatchObject({
      duration: 10,
      name: task.name,
      start: task.start,
      progress: null,
    });
    expect(cellRequest('name', ` ${task.name} `, task)).toBeUndefined();
    expect(cellRequest('start', '21/10/2026', task)).toEqual({
      kind: 'dates',
      start: '2026-10-21',
      end: null,
    });
    expect(cellRequest('end', '30/10/2026', task)).toEqual({
      kind: 'dates',
      start: '2026-10-19',
      end: '2026-10-30',
    });
    expect(cellRequest('progress', '60', task)).toEqual({ kind: 'progress', progress: 60 });
    const dependsOn = cellRequest('dependsOn', ['a', 'b'], task);
    expect(dependsOn?.kind === 'item' ? dependsOn.body.predecessorIds : undefined).toEqual([
      'a',
      'b',
    ]);
    expect(cellRequest('dependsOn', [...task.predecessorIds], task)).toBeUndefined();
  });
});
