import { describe, expect, it } from 'vitest';
import { demoMyTaskItems, demoMyTasks } from '../test/demoMyTasks';
import {
  cardsOf,
  dateGroupOf,
  entriesOf,
  type MyTaskRow,
  openCounts,
  rowsByDate,
  rowsByProject,
} from './myTasks';

// Spec: minhas-tarefas, requirements "Página Minhas tarefas", "Concluídas em Minhas tarefas" and "Agrupar por data"
// (ADR-0158). Today is Friday, 16/10/2026: the week goes from Monday 12/10 to Sunday 18/10.
const today = '2026-10-16';

/** The rows as text: a group by its key and its count, a task by its name. */
function shown(rows: readonly MyTaskRow[]): string[] {
  return rows.map((row) =>
    row.kind === 'task'
      ? row.entry.item.name
      : row.kind === 'project'
        ? `[${row.project.code} ${String(row.count)}]`
        : `[${row.group} ${String(row.count)}]`,
  );
}

const open = { progress: 0, late: false };

describe('Agrupar por data', () => {
  it('Grupo de cada tarefa', () => {
    expect(dateGroupOf({ ...open, end: '2026-10-15' }, today)).toBe('late');
    expect(dateGroupOf({ ...open, end: '2026-10-16' }, today)).toBe('today');
    expect(dateGroupOf({ ...open, end: '2026-10-17' }, today)).toBe('thisWeek');
    expect(dateGroupOf({ ...open, end: '2026-10-18' }, today)).toBe('thisWeek');
    expect(dateGroupOf({ ...open, end: '2026-10-19' }, today)).toBe('nextWeek');
    expect(dateGroupOf({ ...open, end: '2026-10-25' }, today)).toBe('nextWeek');
    expect(dateGroupOf({ ...open, end: '2026-10-26' }, today)).toBe('later');
    // Late by the API, as a task that should have started, even with the end ahead.
    expect(dateGroupOf({ progress: 10, late: true, end: '2026-10-20' }, today)).toBe('late');
    // A completed task, even late, is done.
    expect(dateGroupOf({ progress: 100, late: false, end: '2026-10-09' }, today)).toBe('done');
  });

  it('Semana de segunda a domingo', () => {
    // On Sunday, the week is still the one from Monday 12/10; the next one starts the day after.
    expect(dateGroupOf({ ...open, end: '2026-10-19' }, '2026-10-18')).toBe('nextWeek');
    // On Monday, the week starts that day.
    expect(dateGroupOf({ ...open, end: '2026-10-25' }, '2026-10-19')).toBe('thisWeek');
    expect(dateGroupOf({ ...open, end: '2026-11-01' }, '2026-10-19')).toBe('nextWeek');
  });

  it('Grupos por data', () => {
    const entries = entriesOf(demoMyTasks(demoMyTaskItems(), true));

    expect(shown(rowsByDate(entries, today))).toEqual([
      '[late 1]',
      'Parametrizar regras de ICMS',
      '[today 1]',
      'Validar plano de contas',
      '[thisWeek 1]',
      'Levantar notas de entrada',
      '[nextWeek 1]',
      'Roteiro de testes',
      '[later 1]',
      'Relatório final',
      '[done 1]',
      'Instalar o ERP',
    ]);
  });

  it('Grupo sem tarefas', () => {
    const entries = entriesOf(demoMyTasks(demoMyTaskItems(), false));

    // Without the completed ones, and without the groups that have no task.
    expect(shown(rowsByDate(entries, today))).not.toContain('[done 1]');
    expect(rowsByDate([], today)).toEqual([]);
  });
});

describe('Página Minhas tarefas', () => {
  it('Tarefas por projeto', () => {
    const entries = entriesOf(demoMyTasks(demoMyTaskItems(), false));

    expect(shown(rowsByProject(entries))).toEqual([
      '[ERP-FIN 3]',
      'Parametrizar regras de ICMS',
      'Validar plano de contas',
      'Roteiro de testes',
      '[FISC-SP 2]',
      'Levantar notas de entrada',
      'Relatório final',
    ]);
    expect(entries.find((entry) => entry.item.name === 'Roteiro de testes')).toMatchObject({
      phase: 'Homologação',
      parent: 'Testes integrados',
      project: { code: 'ERP-FIN' },
    });
  });

  it('Contagem das tarefas em aberto', () => {
    // The completed ones do not count.
    expect(openCounts(entriesOf(demoMyTasks(demoMyTaskItems(), true)))).toEqual({
      tasks: 5,
      projects: 2,
    });
    expect(openCounts([])).toEqual({ tasks: 0, projects: 0 });
  });
});

describe('Quadro em Minhas tarefas', () => {
  it('Cartões de vários projetos', () => {
    const cards = cardsOf(entriesOf(demoMyTasks(demoMyTaskItems(), true)));

    // The open ones by the end, then the completed ones; each with its project and its code.
    expect(cards.map((card) => [card.item.name, card.projectCode])).toEqual([
      ['Parametrizar regras de ICMS', 'ERP-FIN'],
      ['Validar plano de contas', 'ERP-FIN'],
      ['Levantar notas de entrada', 'FISC-SP'],
      ['Roteiro de testes', 'ERP-FIN'],
      ['Relatório final', 'FISC-SP'],
      ['Instalar o ERP', 'ERP-FIN'],
    ]);
    expect(cards[3]).toMatchObject({
      phase: { name: 'Homologação' },
      parent: { name: 'Testes integrados' },
      projectId: '0199f5a2-7c1e-7d40-9a1b-400000000001',
    });
  });
});
