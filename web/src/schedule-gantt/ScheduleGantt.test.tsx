import { cleanup, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { Schedule } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoSchedule, demoScheduleWithBaseline, demoScheduleWithSubtasks } from '../test/server';
import { areaWidth, barOf, drawWith, simulateLayout } from './ganttTesting';

const text = ptBR.projects;
const oneDayLate = text.schedule.lateDays_one.replace('{{count}}', '1');

/**
 * The area of the timeline is 1200 px wide (simulateLayout). The table takes 320 px and the splitter 4, so the 30 days
 * from 05/10/2026 to 03/11/2026 take 29.2 px each.
 */
const day = (areaWidth - 320 - 4) / 30;

simulateLayout();

async function drawTimeline(schedule: Schedule, editable = false, lines = 12) {
  return (await drawWith({ schedule, editable }, lines)).container;
}

/** The days from the start of the schedule where an element of the timeline starts, and how many it takes. */
const daysOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.left ?? 'NaN') / day);
const lengthOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.width ?? 'NaN') / day);

// Spec: cronograma, requirement "Linha do tempo".
describe('Linha do tempo', { timeout: 20_000 }, () => {
  it('Linha do tempo pela tela', async () => {
    const container = await drawTimeline(demoSchedule());

    // Each phase with its tasks and milestone under it, and the progress; the icons say what they are.
    expect([...container.querySelectorAll('.e-row')].map((row) => row.textContent)).toEqual([
      'Levantamento93%',
      'Reunião de abertura100%',
      'Mapear processos fiscais100%',
      `Validar cadastro de produtos${oneDayLate}80%`,
      'Configuração27%',
      'Parametrizar regras de ICMS50%',
      'Integrar emissão de NF-e20%',
      'Testes integrados0%',
      'Homologação0%',
      'Testes com a equipe do cliente0%',
      `${text.schedule.kinds.Milestone}Aceite da homologação${text.timeline.milestone.pending}`,
      'Entrada em produção',
    ]);
    // The late task, by icon, color and text (ADR-0065); the icon says by how many business days.
    const late = container.querySelector<HTMLElement>('.fp-gantt-progress--late');
    if (late === null) {
      throw new Error('No late task.');
    }
    expect(late.closest('.e-row')?.textContent).toContain('Validar cadastro de produtos');
    expect(within(late).getByRole('img', { name: oneDayLate })).toBeVisible();
    // The bars in the dates of the schedule: 13/10 to 15/10, three days.
    const validating = barOf(container, 'Validar cadastro de produtos');
    expect([daysOf(validating), lengthOf(validating)]).toEqual([8, 3]);
    // The phases in the dates of their tasks and milestones: Configuração from 13/10 to 29/10.
    const configuration = barOf(container, 'Configuração');
    expect([daysOf(configuration), lengthOf(configuration)]).toEqual([8, 17]);
    expect(container.querySelectorAll('.e-gantt-milestone')).toHaveLength(1);
    // The dependencies as arrows, and today on 16/10/2026, with "Hoje".
    expect(container.querySelectorAll('.e-connector-line-container')).toHaveLength(6);
    const today = container.querySelector('.fp-gantt-today');
    expect(daysOf(today)).toBe(11);
    expect(today?.textContent).toBe(text.timeline.today);
    expect(container.textContent).not.toContain('Gantt');
  });

  it('Dias não úteis na linha do tempo', async () => {
    const schedule = demoSchedule();
    const container = await drawTimeline(schedule);

    const shaded = [...container.querySelectorAll('.e-holiday.fp-gantt-nonworking')].map(daysOf);
    expect(shaded).toHaveLength(schedule.nonWorkingDays.length);
    // Saturday 10/10, Sunday 11/10 and the holiday on Monday 12/10; not Tuesday 13/10.
    expect(shaded).toEqual(expect.arrayContaining([5, 6, 7]));
    expect(shaded).not.toContain(8);
  });

  it('Barras que não se arrastam', async () => {
    const cursors = async (editable: boolean) => {
      const container = await drawTimeline(demoSchedule(), editable);
      const bars = [...container.querySelectorAll<HTMLElement>('.fp-gantt-task')];
      const found = new Set(bars.map((bar) => bar.style.cursor));
      cleanup();
      return [...found];
    };

    // Who edits drags the bars; who does not, and the phone, only see them (ADR-0043).
    expect(await cursors(true)).toEqual(['move']);
    expect(await cursors(false)).toEqual(['default']);
  });

  it('Tarefa-resumo como resumo', async () => {
    // Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
    const container = await drawTimeline(demoScheduleWithSubtasks(), true, 14);

    const summary = barOf(container, 'Testes integrados');
    // Drawn as the bar of a phase, from 23/10 to 29/10, with its subtasks under it.
    expect(summary?.querySelector('.e-manualparent-main-container')).not.toBeNull();
    expect(summary?.classList.contains('fp-gantt-summary')).toBe(true);
    const names = [...container.querySelectorAll('.e-row')].map((row) => row.textContent);
    expect(names.slice(7, 10)).toEqual([
      'Testes integrados0%',
      'Roteiro de testes0%',
      'Executar testes0%',
    ]);
    // Only the tasks with dates of their own move (ADR-0088).
    expect(
      container.querySelector<HTMLElement>('.fp-gantt-task[aria-label^="Name Roteiro de testes "]')
        ?.style.cursor,
    ).toBe('move');
  });
});

// Spec: cronograma, requirement "Caminho crítico pela tela".
// Syncfusion draws more slowly when every test file runs together.
describe('Caminho crítico pela tela', { timeout: 20_000 }, () => {
  const isCritical = (container: HTMLElement, name: string) =>
    barOf(container, name)?.classList.contains('fp-gantt-critical');

  it('Caminho crítico na linha do tempo', async () => {
    const { container, redraw } = await drawWith({
      schedule: demoScheduleWithBaseline(),
      showBaseline: true,
      showCritical: true,
    });

    // The tasks and the milestone of the critical path in red; the ones with float as they are.
    expect(
      [
        'Reunião de abertura',
        'Parametrizar regras de ICMS',
        'Testes integrados',
        'Aceite da homologação',
        'Validar cadastro de produtos',
        'Integrar emissão de NF-e',
      ].map((name) => isCritical(container, name)),
    ).toEqual([true, true, true, true, false, false]);
    // A thin bar under each of the seven tasks, at its dates in the baseline: Parametrizar regras de ICMS from 13/10
    // to 20/10, eight days.
    const baselines = [...container.querySelectorAll('.e-baseline-bar')];
    expect(baselines).toHaveLength(7);
    const icms = baselines.find((bar) => daysOf(bar) === 8 && lengthOf(bar) === 8);
    expect(icms).toBeDefined();

    redraw({ showBaseline: false, showCritical: false });

    // Drawn again by Syncfusion, which takes longer when the tests run together.
    await vi.waitFor(() => {
      expect(container.querySelectorAll('.e-baseline-bar')).toHaveLength(0);
      expect(container.querySelectorAll('.fp-gantt-critical')).toHaveLength(0);
    }, 15_000);
  });
});
