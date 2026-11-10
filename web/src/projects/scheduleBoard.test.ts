import { describe, expect, it } from 'vitest';
import type { Schedule } from '../api/client';
import { demoSchedule, demoScheduleWithSubtasks } from '../test/server';
import { boardColumns, columnOf, hasCard, moveOf, progressOf } from './scheduleBoard';
import { itemsOf } from './schedule';

/** The names of the cards in each column. */
const namesOf = (schedule: Schedule, drafts?: ReadonlyMap<string, 'todo' | 'doing' | 'done'>) =>
  boardColumns(schedule, drafts).map((column) => [
    column.id,
    column.cards.map((card) => card.item.name),
  ]);

function idOf(schedule: Schedule, name: string): string {
  const item = itemsOf(schedule).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item.id;
}

// Spec: cronograma, requirements "Visualização Quadro", "Ordem dos cartões" and "Mover o cartão no quadro" (ADR-0155).
describe('Regras do quadro', () => {
  it('Coluna de cada progresso', () => {
    expect(columnOf({ progress: 0 })).toBe('todo');
    expect(columnOf({ progress: 1 })).toBe('doing');
    expect(columnOf({ progress: 99 })).toBe('doing');
    expect(columnOf({ progress: 100 })).toBe('done');
  });

  it('Só as tarefas com datas próprias têm cartão', () => {
    expect(hasCard({ kind: 'Task', hasSubtasks: false })).toBe(true);
    expect(hasCard({ kind: 'Task', hasSubtasks: true })).toBe(false);
    expect(hasCard({ kind: 'Milestone', hasSubtasks: false })).toBe(false);
  });

  it('Tarefas nas colunas, em ordem', () => {
    // A fazer and Em andamento by the end; Concluída by the completion, from the latest; no milestone.
    expect(namesOf(demoSchedule())).toEqual([
      ['todo', ['Testes integrados', 'Testes com a equipe do cliente']],
      [
        'doing',
        ['Validar cadastro de produtos', 'Parametrizar regras de ICMS', 'Integrar emissão de NF-e'],
      ],
      ['done', ['Mapear processos fiscais', 'Reunião de abertura']],
    ]);
  });

  it('Empate na ordem segue o cronograma', () => {
    const schedule = demoSchedule();
    for (const item of itemsOf(schedule)) {
      if (item.kind === 'Task' && item.progress === 0) {
        item.end = '2026-11-03';
      }
    }
    expect(namesOf(schedule)[0]).toEqual([
      'todo',
      ['Testes integrados', 'Testes com a equipe do cliente'],
    ]);
  });

  it('Subtarefas no quadro, sem a tarefa-resumo', () => {
    const schedule = demoScheduleWithSubtasks();
    const todo = boardColumns(schedule)[0]?.cards ?? [];
    expect(todo.map((card) => [card.item.name, card.phase.name, card.parent?.name])).toEqual([
      ['Roteiro de testes', 'Configuração', 'Testes integrados'],
      ['Executar testes', 'Configuração', 'Testes integrados'],
      ['Testes com a equipe do cliente', 'Homologação', undefined],
    ]);
  });

  it('Cartão em rascunho na coluna nova', () => {
    const schedule = demoSchedule();
    const drafts = new Map([[idOf(schedule, 'Integrar emissão de NF-e'), 'done' as const]]);
    expect(namesOf(schedule, drafts)[2]).toEqual([
      'done',
      ['Integrar emissão de NF-e', 'Mapear processos fiscais', 'Reunião de abertura'],
    ]);
  });

  it('O que mover para cada coluna faz', () => {
    expect(moveOf({ progress: 50 }, 'done')).toBe('complete');
    expect(moveOf({ progress: 50 }, 'todo')).toBe('restart');
    expect(moveOf({ progress: 0 }, 'doing')).toBe('ask');
    expect(moveOf({ progress: 100 }, 'doing')).toBe('ask');
    expect(moveOf({ progress: 50 }, 'doing')).toBeUndefined();
    expect(progressOf('complete')).toBe(100);
    expect(progressOf('restart')).toBe(0);
    expect(progressOf('ask')).toBeUndefined();
  });
});
