import { describe, expect, it } from 'vitest';
import type { Schedule } from '../api/client';
import { datesIn } from '../format/dates';
import { spreadsheetDay } from '../format/xlsx';
import i18n from '../i18n';
import { demoPeople, demoProjects, demoScheduleWithSubtasks } from '../test/server';
import { unzip } from '../test/unzip';
import { itemSituation, scheduleReport, scheduleReportName, workSituation } from './scheduleReport';

const t = i18n.t.bind(i18n);
const dates = datesIn('America/Sao_Paulo');

/** The cells of a sheet by their reference (A1), each with its style and what it shows. */
function cellsOf(xml: string): Map<string, { style: string; value: string }> {
  const cells = new Map<string, { style: string; value: string }>();
  for (const match of xml.matchAll(/<c r="([A-Z]+\d+)"[^>]*?s="(\d+)"[^>]*?(?:\/>|>(.*?)<\/c>)/g)) {
    const [, reference = '', style = '', content = ''] = match;
    const value =
      /<t[^>]*>(.*?)<\/t>/.exec(content)?.[1] ?? /<v>(.*?)<\/v>/.exec(content)?.[1] ?? '';
    cells.set(reference, { style, value });
  }
  return cells;
}

/** ERP-FIN with "Testes integrados" in subtasks, the NF-e with the client and the baseline saved on 09/10/2026. */
function scheduleOfTheReport(): Schedule {
  const schedule = demoScheduleWithSubtasks();
  for (const phase of schedule.phases) {
    phase.items = phase.items.map((item) =>
      item.name === 'Integrar emissão de NF-e'
        ? { ...item, owner: null, ownedByClient: true }
        : item.name === 'Parametrizar regras de ICMS'
          ? {
              ...item,
              baselineStart: '2026-10-13',
              baselineEnd: '2026-10-20',
              baselineDeviation: 2,
            }
          : item,
    );
  }
  return {
    ...schedule,
    baseline: { savedOn: '2026-10-09', start: '2026-10-05', end: '2026-10-29', deviation: 2 },
  };
}

// Spec: cronograma, requirement "Exportar para o Excel" (ADR-0021, ADR-0099, ADR-0136).
describe('Exportar para o Excel', () => {
  it('Situação de cada linha', () => {
    expect(
      [
        { progress: 100, late: true },
        { progress: 80, late: true },
        { progress: 20, late: false },
        { progress: 0, late: false },
      ].map(workSituation),
    ).toEqual(['done', 'late', 'inProgress', 'notStarted']);
    const milestone = demoScheduleWithSubtasks().phases[2]?.items[1];
    if (milestone === undefined) {
      throw new Error('No milestone.');
    }
    expect(
      [{ ...milestone, reachedOn: '2026-11-03' }, { ...milestone, late: true }, milestone].map(
        itemSituation,
      ),
    ).toEqual(['reached', 'lateMilestone', 'pending']);
  });

  it('Cronograma no Excel', async () => {
    const project = demoProjects()[0];
    if (project === undefined) {
      throw new Error('No ERP-FIN.');
    }

    const files = await unzip(
      await scheduleReport({
        project,
        schedule: scheduleOfTheReport(),
        generatedAt: '2026-10-16T13:30:00Z',
        dates,
        t,
      }),
    );

    expect(scheduleReportName(project, t)).toBe('ERP-FIN - Cronograma.xlsx');
    const workbook = files.get('xl/workbook.xml') ?? '';
    expect(workbook).toContain('<sheet name="Cronograma" sheetId="1" r:id="rId1"/>');
    expect(workbook).toContain('<sheet name="Projeto" sheetId="2" r:id="rId2"/>');
    const sheet = cellsOf(files.get('xl/worksheets/sheet1.xml') ?? '');
    const row = (number: number) =>
      'ABCDEFGHIJKLMNOP'.split('').map((column) => sheet.get(`${column}${String(number)}`)?.value);
    const day = (value: string) => String(spreadsheetDay(value));

    expect(row(1)).toEqual([
      'Nº',
      'Fase',
      'Nome',
      'Tipo',
      'Responsável',
      'Início',
      'Fim',
      'Duração (dias úteis)',
      'Progresso',
      'Situação',
      'Atraso (dias úteis)',
      'Depende de (Nº)',
      'Caminho crítico',
      'Início na linha de base',
      'Fim na linha de base',
      'Diferença da linha de base (dias úteis)',
    ]);
    // The phase, in bold, with its dates and progress.
    expect(row(2).slice(0, 10)).toEqual([
      '1',
      'Levantamento',
      'Levantamento',
      'Fase',
      '',
      day('2026-10-05'),
      day('2026-10-15'),
      '',
      '0.93',
      'Atrasada',
    ]);
    expect(sheet.get('C2')?.style).toBe('5');
    // A task late by a business day, after the one it depends on.
    expect(row(5).slice(0, 13)).toEqual([
      '4',
      'Levantamento',
      'Validar cadastro de produtos',
      'Tarefa',
      demoPeople.rafael.name,
      day('2026-10-13'),
      day('2026-10-15'),
      '3',
      '0.8',
      'Atrasada',
      '1',
      '3',
      '',
    ]);
    expect(sheet.get('F5')?.style).toBe('6');
    expect(sheet.get('I5')?.style).toBe('7');
    // On the critical path, with the baseline two business days before.
    expect(row(7).slice(12)).toEqual(['Sim', day('2026-10-13'), day('2026-10-20'), '2']);
    // The client as the owner.
    expect(row(8)[4]).toBe(t('projects.schedule.clientOwner', { name: project.client?.name }));
    // The subtask, indented, depending on the line of the ICMS.
    expect(row(10).slice(1, 12)).toEqual([
      'Configuração',
      'Roteiro de testes',
      'Subtarefa',
      demoPeople.carla.name,
      day('2026-10-23'),
      day('2026-10-26'),
      '2',
      '0',
      'Não iniciada',
      '',
      '6',
    ]);
    expect(sheet.get('C10')?.style).toBe('8');
    // The milestone points at the line of the tests with the client and has no duration nor progress.
    expect(row(14).slice(0, 13)).toEqual([
      '13',
      'Homologação',
      'Aceite da homologação',
      'Marco',
      '',
      day('2026-11-03'),
      day('2026-11-03'),
      '',
      '',
      'Previsto',
      '',
      '12',
      'Sim',
    ]);
    // A phase without items, without dates.
    expect(row(15).slice(0, 7)).toEqual([
      '14',
      'Entrada em produção',
      'Entrada em produção',
      'Fase',
      '',
      '',
      '',
    ]);

    const tab = cellsOf(files.get('xl/worksheets/sheet2.xml') ?? '');
    const pairs = Array.from({ length: 13 }, (_, index) => [
      tab.get(`A${String(index + 1)}`)?.value,
      tab.get(`B${String(index + 1)}`)?.value,
    ]);
    expect(pairs).toEqual([
      ['Código', 'ERP-FIN'],
      ['Projeto', project.name],
      ['Cliente', project.client?.name],
      ['Gerente', demoPeople.marina.name],
      ['Início do cronograma', day('2026-10-05')],
      ['Fim do cronograma', day('2026-11-03')],
      ['Progresso', '0.42'],
      ['Tarefas atrasadas', '1'],
      ['Marcos atrasados', '0'],
      ['Linha de base salva em', day('2026-10-09')],
      ['Diferença do fim contra a linha de base (dias úteis)', '2'],
      ['Planilha gerada em', String(dates.toSpreadsheetDateTime('2026-10-16T13:30:00Z'))],
      ['Fuso horário', 'America/Sao_Paulo'],
    ]);
    // No e-mail goes in the spreadsheet: they are only in the XML of the managers (ADR-0136).
    expect([...files.values()].join('')).not.toContain('@');
  });
});
