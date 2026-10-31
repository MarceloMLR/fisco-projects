import { describe, expect, it } from 'vitest';
import { demoSchedule, demoScheduleWithBaseline, demoScheduleWithSubtasks } from '../test/server';
import { ganttRowsOf, shownRows } from './ganttRows';

// Spec: cronograma, requirement "Linha do tempo": the lines and the dates the timeline draws come from the API.
describe('Linhas da linha do tempo', () => {
  it('Fases, tarefas e marcos em linhas', () => {
    const schedule = demoSchedule();

    const rows = ganttRowsOf(schedule);

    expect(rows.map((row) => row.name)).toEqual([
      'Levantamento',
      'Reunião de abertura',
      'Mapear processos fiscais',
      'Validar cadastro de produtos',
      'Configuração',
      'Parametrizar regras de ICMS',
      'Integrar emissão de NF-e',
      'Testes integrados',
      'Homologação',
      'Testes com a equipe do cliente',
      'Aceite da homologação',
      'Entrada em produção',
    ]);
    const [survey, opening, mapping, validating] = rows;
    expect(survey).toMatchObject({
      key: schedule.phases[0]?.id,
      level: 0,
      shape: 'summary',
      late: true,
      progress: 93,
      hasChildren: true,
      ownDates: false,
    });
    expect(opening).toMatchObject({
      level: 1,
      parentKey: survey?.key,
      shape: 'bar',
      progress: 100,
      ownDates: true,
    });
    expect(mapping?.predecessors).toEqual([opening?.key]);
    expect(validating).toMatchObject({ late: true, predecessors: [mapping?.key] });
    const acceptance = rows.find((row) => row.name === 'Aceite da homologação');
    expect(acceptance).toMatchObject({ shape: 'milestone', progress: 0 });
    expect(acceptance?.start).toEqual(acceptance?.end);
    // A phase without items has no dates, and no bar.
    const production = rows.at(-1);
    expect(production?.name).toBe('Entrada em produção');
    expect([production?.start, production?.end, production?.hasChildren]).toEqual([
      undefined,
      undefined,
      false,
    ]);
  });

  it('Datas da linha do tempo', () => {
    // The days of the API, as they are: a task of 06/10 to 09/10 is drawn from 06/10 to the end of 09/10.
    const rows = ganttRowsOf(demoSchedule());

    expect([rows[2]?.start, rows[2]?.end]).toEqual(['2026-10-06', '2026-10-09']);
    expect([rows[4]?.start, rows[4]?.end]).toEqual(['2026-10-13', '2026-10-29']);
  });

  it('Subtarefas na linha do tempo', () => {
    // Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
    const rows = ganttRowsOf(demoScheduleWithSubtasks());
    const row = (name: string) => rows.find((candidate) => candidate.name === name);

    const summary = row('Testes integrados');
    expect(summary).toMatchObject({
      parentKey: row('Configuração')?.key,
      level: 1,
      shape: 'summary',
      hasChildren: true,
      ownDates: false,
    });
    expect(row('Roteiro de testes')).toMatchObject({ parentKey: summary?.key, level: 2 });
    expect(row('Executar testes')).toMatchObject({
      parentKey: summary?.key,
      predecessors: [row('Roteiro de testes')?.key],
      ownDates: true,
    });
    expect(row('Integrar emissão de NF-e')?.shape).toBe('bar');
  });

  it('Linhas recolhidas', () => {
    // Spec: cronograma, requirement "Recolher na linha do tempo".
    const rows = ganttRowsOf(demoScheduleWithSubtasks());
    const keyOf = (name: string) => rows.find((row) => row.name === name)?.key ?? '';

    const names = (collapsed: string[]) =>
      shownRows(rows, new Set(collapsed)).map((row) => row.name);

    // A phase hides its tasks, its milestones and their subtasks; a task, only its subtasks.
    expect(names([keyOf('Configuração')])).not.toContain('Executar testes');
    expect(names([keyOf('Configuração')])).not.toContain('Parametrizar regras de ICMS');
    expect(names([keyOf('Configuração')])).toContain('Homologação');
    expect(names([keyOf('Testes integrados')])).toContain('Testes integrados');
    expect(names([keyOf('Testes integrados')])).not.toContain('Roteiro de testes');
    expect(names([])).toHaveLength(rows.length);
  });
});

// Spec: cronograma, requirements "Linha de base pela tela" and "Caminho crítico pela tela".
describe('Linha de base e caminho crítico nas linhas', () => {
  it('Datas da linha de base e o caminho crítico', () => {
    const rows = ganttRowsOf(demoScheduleWithBaseline(), { critical: true });
    const row = (name: string) => rows.find((one) => one.name === name);

    // The thin bar from the first to the last day of the baseline; none for a milestone.
    expect(row('Parametrizar regras de ICMS')).toMatchObject({
      baseline: { start: '2026-10-13', end: '2026-10-20' },
      critical: true,
    });
    expect(row('Aceite da homologação')).toMatchObject({ critical: true });
    expect(row('Aceite da homologação')?.baseline).toBeUndefined();
    expect(row('Validar cadastro de produtos')).toMatchObject({ critical: false, late: true });
    // Without the critical path in "Exibir", no line is red.
    expect(ganttRowsOf(demoScheduleWithBaseline()).some((one) => one.critical)).toBe(false);
  });
});
