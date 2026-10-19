import { cleanup, render, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Schedule } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoSchedule, demoScheduleWithSubtasks } from '../test/server';
import { ScheduleGantt } from './ScheduleGantt';

const text = ptBR.projects;
const oneDayLate = text.schedule.lateDays_one.replace('{{count}}', '1');

/**
 * jsdom does not lay anything out: the area of the timeline says it is 1200 px wide. The table takes 320 px and the
 * splitter 4, so the 30 days from 05/10/2026 to 03/11/2026 take 29.2 px each.
 */
const areaWidth = 1200;
const day = 29.2;

beforeEach(() => {
  vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockReturnValue(areaWidth);
  vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(areaWidth);
  vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(600);
  vi.stubGlobal(
    'ResizeObserver',
    class {
      constructor(private readonly callback: () => void) {}
      observe() {
        this.callback();
      }
      unobserve() {
        // Nothing to stop: the width never changes.
      }
      disconnect() {
        // Nothing to stop: the width never changes.
      }
    },
  );
});

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

async function drawTimeline(schedule: Schedule, editable = false, lines = 12) {
  const view = render(
    <ScheduleGantt
      schedule={schedule}
      scale="days"
      editable={editable}
      license={null}
      onChoose={vi.fn()}
      onMove={vi.fn()}
      onResize={vi.fn()}
      onLink={vi.fn()}
    />,
  );
  // The cells of FiscoProjects come a moment after the rows of Syncfusion.
  await vi.waitFor(() => {
    const rows = view.container.querySelectorAll('.e-row');
    expect(rows).toHaveLength(lines);
    expect(rows[0]?.textContent).not.toBe('');
  }, 5000);
  return view.container;
}

/** The days from the start of the schedule where an element of the timeline starts, and how many it takes. */
const daysOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.left ?? 'NaN') / day);
const lengthOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.width ?? 'NaN') / day);

/** The bar of a task or milestone, which Syncfusion names after it. */
const barOf = (container: HTMLElement, name: string) =>
  container.querySelector(`.e-taskbar-main-container[aria-label^="Name ${name} "]`);

// Spec: cronograma, requirement "Linha do tempo".
describe('Linha do tempo', () => {
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
