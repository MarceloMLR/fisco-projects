import { describe, expect, it } from 'vitest';
import { demoSchedule, demoScheduleWithBaseline, demoScheduleWithSubtasks } from '../test/server';
import { dateOfDay, dayOfDate, endOfDay, ganttRowsOf, lastDayOf } from './ganttRows';

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
      key: 1,
      cssClass: 'fp-gantt-phase fp-gantt-late',
      progress: 93,
    });
    expect(opening).toMatchObject({
      key: 2,
      parentKey: 1,
      cssClass: 'fp-gantt-task',
      progress: 100,
    });
    expect(mapping?.predecessor).toBe('2FS');
    expect(validating).toMatchObject({
      cssClass: 'fp-gantt-task fp-gantt-late',
      predecessor: '3FS',
    });
    const acceptance = rows.find((row) => row.name === 'Aceite da homologação');
    expect(acceptance).toMatchObject({
      milestone: true,
      cssClass: 'fp-gantt-milestone',
      progress: 0,
    });
    expect(acceptance?.start).toEqual(acceptance?.end);
    // A phase without items has no dates, and no bar.
    const production = rows.at(-1);
    expect(production?.name).toBe('Entrada em produção');
    expect([production?.start, production?.end]).toEqual([undefined, undefined]);
  });

  it('Datas da linha do tempo', () => {
    // A task of 06/10 to 09/10 is drawn from the midnight of 06/10 to the midnight after 09/10.
    const mapping = ganttRowsOf(demoSchedule())[2];

    expect(mapping?.start).toEqual(new Date(2026, 9, 6));
    expect(mapping?.end).toEqual(new Date(2026, 9, 10));
    expect(dayOfDate(dateOfDay('2026-10-16'))).toBe('2026-10-16');
    expect(lastDayOf(endOfDay('2026-10-16'))).toBe('2026-10-16');
    // The Gantt may end a bar at the end of the working day, before midnight: the day is the same.
    expect(lastDayOf(new Date(2026, 9, 16, 17, 0))).toBe('2026-10-16');
  });

  it('Subtarefas na linha do tempo', () => {
    // Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
    const rows = ganttRowsOf(demoScheduleWithSubtasks());
    const row = (name: string) => rows.find((candidate) => candidate.name === name);

    const summary = row('Testes integrados');
    expect(summary?.parentKey).toBe(row('Configuração')?.key);
    expect(summary?.cssClass).toBe('fp-gantt-summary');
    expect(row('Roteiro de testes')?.parentKey).toBe(summary?.key);
    expect(row('Executar testes')?.parentKey).toBe(summary?.key);
    expect(row('Executar testes')?.predecessor).toBe(`${String(row('Roteiro de testes')?.key)}FS`);
    expect(row('Integrar emissão de NF-e')?.cssClass).toBe('fp-gantt-task');
  });
});

// Spec: cronograma, requirements "Linha de base pela tela" and "Caminho crítico pela tela".
describe('Linha de base e caminho crítico nas linhas', () => {
  it('Datas da linha de base e o caminho crítico', () => {
    const rows = ganttRowsOf(demoScheduleWithBaseline(), { critical: true });
    const row = (name: string) => rows.find((one) => one.name === name);

    // The thin bar from the first day of the baseline to the midnight after its last; none for a milestone.
    expect(row('Parametrizar regras de ICMS')).toMatchObject({
      baselineStart: dateOfDay('2026-10-13'),
      baselineEnd: endOfDay('2026-10-20'),
      cssClass: 'fp-gantt-task fp-gantt-critical',
    });
    expect(row('Aceite da homologação')).toMatchObject({
      cssClass: 'fp-gantt-milestone fp-gantt-critical',
    });
    expect(row('Aceite da homologação')?.baselineStart).toBeUndefined();
    expect(row('Validar cadastro de produtos')?.cssClass).toBe('fp-gantt-task fp-gantt-late');
    // Without the critical path in "Exibir", no line is red.
    expect(
      ganttRowsOf(demoScheduleWithBaseline()).some((one) => one.cssClass.includes('critical')),
    ).toBe(false);
  });
});
