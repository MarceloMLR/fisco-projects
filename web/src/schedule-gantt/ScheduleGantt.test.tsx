import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { Schedule } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoSchedule, demoScheduleWithBaseline, demoScheduleWithSubtasks } from '../test/server';
import {
  barOf,
  chartOf,
  dayWidth,
  dragBar,
  hasBar,
  linkBars,
  spanOf,
  timelineLine,
  timelineLines,
  timelineOf,
  timelineTable,
} from '../test/timeline';
import { ScheduleGantt, type ScheduleGanttProps } from './ScheduleGantt';

const text = ptBR.projects;
const oneDayLate = text.schedule.lateDays_one.replace('{{count}}', '1');

/** Draws the timeline with these properties, and draws it again with others. */
function drawTimeline(props: Partial<ScheduleGanttProps> & { schedule: Schedule }) {
  const all: ScheduleGanttProps = {
    scale: 'days',
    editable: false,
    onChoose: vi.fn(),
    onMove: vi.fn(),
    onResize: vi.fn(),
    onLink: vi.fn(),
    ...props,
  };
  const view = render(<ScheduleGantt {...all} />);
  return {
    ...view,
    props: all,
    redraw: (changed: Partial<ScheduleGanttProps>) => {
      Object.assign(all, changed);
      view.rerender(<ScheduleGantt {...all} />);
    },
  };
}

const itemsOf = (schedule: Schedule) => schedule.phases.flatMap((phase) => phase.items);

// Spec: cronograma, requirement "Linha do tempo".
describe('Linha do tempo', () => {
  it('Linha do tempo pela tela', () => {
    const { container } = drawTimeline({ schedule: demoSchedule() });

    // Each phase with its tasks and milestone under it, and the progress; the icons say what they are.
    expect(timelineLines()).toEqual([
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
    const late = container.querySelector<HTMLElement>('.fp-gantt__progress--late');
    if (late === null) {
      throw new Error('No late task.');
    }
    expect(late.closest('[role="row"]')?.textContent).toContain('Validar cadastro de produtos');
    expect(within(late).getByRole('img', { name: oneDayLate })).toBeVisible();
    // The bars in the dates of the schedule: 13/10 to 15/10, three days; the phases in the dates of their items.
    expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([8, 3]);
    expect(spanOf(barOf('Configuração'))).toEqual([8, 17]);
    expect(barOf('Configuração').querySelector('.g-summary')).not.toBeNull();
    expect(container.querySelectorAll('.g-milestone')).toHaveLength(1);
    // A phase without items has no bar.
    expect(hasBar('Entrada em produção')).toBe(false);
    // The dependencies as arrows, and today on 16/10/2026, with "Hoje".
    expect(container.querySelectorAll('.g-dep')).toHaveLength(6);
    expect(container.querySelector('.g-today-label')?.textContent).toBe(text.timeline.today);
    const today = chartOf().querySelector('.g-today');
    expect(Math.floor(Number(today?.getAttribute('x1')) / dayWidth)).toBe(11);
    expect(container.textContent).not.toContain('Gantt');
  });

  it('Dias não úteis na linha do tempo', () => {
    const schedule = demoSchedule();
    drawTimeline({ schedule });

    const shaded = [...chartOf().querySelectorAll('.g-nonworking')].map((day) =>
      Math.round(Number(day.getAttribute('x')) / dayWidth),
    );
    // The days off within the period of the timeline, 05/10 to 03/11/2026.
    expect(shaded).toHaveLength(
      schedule.nonWorkingDays.filter((day) => day >= '2026-10-05' && day <= '2026-11-03').length,
    );
    // Saturday 10/10, Sunday 11/10 and the holiday on Monday 12/10; not Tuesday 13/10.
    expect(shaded).toEqual(expect.arrayContaining([5, 6, 7]));
    expect(shaded).not.toContain(8);
  });

  it('Barras que não se arrastam', () => {
    const { unmount } = drawTimeline({ schedule: demoSchedule(), editable: true });
    // Who edits drags the bars, pulls their edges and links them (ADR-0043).
    expect(timelineOf()).toHaveAttribute('data-editable', 'true');
    expect(barOf('Validar cadastro de produtos')).toHaveClass('g-item--draggable');
    expect(barOf('Validar cadastro de produtos').querySelectorAll('.g-handle')).toHaveLength(2);
    expect(barOf('Aceite da homologação').querySelector('.g-connector')).not.toBeNull();
    // Never a phase.
    expect(barOf('Configuração')).not.toHaveClass('g-item--draggable');
    unmount();

    // Who does not, and the phone, only see them.
    const { container, props } = drawTimeline({ schedule: demoSchedule() });
    expect(timelineOf()).toHaveAttribute('data-editable', 'false');
    expect(container.querySelectorAll('.g-item--draggable, .g-handle, .g-connector')).toHaveLength(
      0,
    );
    dragBar(barOf('Validar cadastro de produtos'), 3);
    expect(props.onMove).not.toHaveBeenCalled();
  });

  it('Tarefa-resumo como resumo', () => {
    // Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
    drawTimeline({ schedule: demoScheduleWithSubtasks(), editable: true });

    // Drawn as a phase, from 23/10 to 29/10, with its subtasks under it.
    const summary = barOf('Testes integrados');
    expect(summary.querySelector('.g-summary')).not.toBeNull();
    expect(spanOf(summary)).toEqual([18, 7]);
    expect(timelineLines().slice(7, 10)).toEqual([
      'Testes integrados0%',
      'Roteiro de testes0%',
      'Executar testes0%',
    ]);
    // Only the tasks with dates of their own move and link (ADR-0088).
    expect(summary).not.toHaveClass('g-item--draggable');
    expect(summary.querySelector('.g-connector')).toBeNull();
    expect(barOf('Roteiro de testes')).toHaveClass('g-item--draggable');
  });
});

// Spec: cronograma, requirement "Detalhe pela linha do tempo".
describe('Detalhe pela linha do tempo', () => {
  it('Reabrir o detalhe pela linha do tempo', () => {
    const schedule = demoSchedule();
    const mapping = schedule.phases[0]?.items[1];
    const { props, redraw } = drawTimeline({ schedule });
    const chosenLines = () =>
      within(timelineTable())
        .getAllByRole('row')
        .filter((row) => row.getAttribute('aria-selected') === 'true')
        .map((row) => row.querySelector('.fp-gantt__text')?.textContent);

    fireEvent.click(barOf('Mapear processos fiscais'));

    expect(props.onChoose).toHaveBeenLastCalledWith(
      expect.objectContaining({ kind: 'item', item: mapping }),
    );
    // The details open with the line chosen, in the table and in the chart, and it stays chosen while they are open.
    redraw({ selectedId: mapping?.id });
    expect(chosenLines()).toEqual(['Mapear processos fiscais']);
    expect(chartOf().querySelectorAll('.g-selected')).toHaveLength(1);
    expect(barOf('Mapear processos fiscais').querySelector('.g-bar')).toHaveClass(
      'g-bar--selected',
    );
    // Closed, the timeline forgets the line, and the same bar opens the details again.
    redraw({ selectedId: undefined });
    expect(chosenLines()).toEqual([]);
    expect(chartOf().querySelectorAll('.g-selected')).toHaveLength(0);
    fireEvent.click(barOf('Mapear processos fiscais'));
    expect(props.onChoose).toHaveBeenCalledTimes(2);
    // The line of the table chooses as the bar does, and so does the line of the chart beside the bar.
    fireEvent.click(timelineLine('Configuração'));
    expect(props.onChoose).toHaveBeenLastCalledWith(
      expect.objectContaining({ kind: 'phase', phase: schedule.phases[1] }),
    );
  });
});

// Spec: cronograma, requirement "Caminho crítico pela tela".
describe('Caminho crítico pela tela', () => {
  const isCritical = (name: string) => {
    const shape = barOf(name).querySelector('.g-bar, .g-milestone');
    return (
      shape?.classList.contains('g-bar--critical') === true ||
      shape?.classList.contains('g-milestone--critical') === true
    );
  };

  it('Caminho crítico na linha do tempo', () => {
    const { container, redraw } = drawTimeline({
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
      ].map(isCritical),
    ).toEqual([true, true, true, true, false, false]);
    // A thin bar under each of the seven tasks, at its dates in the baseline: Parametrizar regras de ICMS from 13/10
    // to 20/10, eight days.
    const baselines = [...container.querySelectorAll('.g-baseline')].map((bar) => [
      Math.round(Number(bar.getAttribute('x')) / dayWidth),
      Math.round(Number(bar.getAttribute('width')) / dayWidth),
    ]);
    expect(baselines).toHaveLength(7);
    expect(baselines).toContainEqual([8, 8]);

    // Without them, as "Exibir" turns them off (ADR-0071).
    redraw({ showBaseline: false, showCritical: false });
    expect(container.querySelectorAll('.g-baseline')).toHaveLength(0);
    expect(container.querySelectorAll('.g-bar--critical, .g-milestone--critical')).toHaveLength(0);
    expect(timelineOf()).toHaveAttribute('data-critical', 'false');
  });
});

// Spec: cronograma, requirement "Recolher na linha do tempo".
describe('Recolher na linha do tempo', () => {
  it('Recolher uma fase na linha do tempo', () => {
    const { props } = drawTimeline({ schedule: demoSchedule() });

    fireEvent.click(screen.getByRole('button', { name: 'Recolher Levantamento' }));

    // The tasks of the phase leave the table and the chart, without opening the details.
    expect(timelineLines().slice(0, 2)).toEqual(['Levantamento93%', 'Configuração27%']);
    expect(hasBar('Mapear processos fiscais')).toBe(false);
    expect(timelineLine('Levantamento')).toHaveAttribute('aria-expanded', 'false');
    // An arrow from a hidden task goes too; the phase stays, in its dates.
    expect(spanOf(barOf('Levantamento'))).toEqual([0, 11]);
    expect(props.onChoose).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Mostrar Levantamento' }));

    expect(timelineLines()).toHaveLength(12);
    expect(hasBar('Mapear processos fiscais')).toBe(true);
    expect(props.onChoose).not.toHaveBeenCalled();
  });
});

// Spec: cronograma, requirement "Linha do tempo pelo teclado".
describe('Linha do tempo pelo teclado', () => {
  it('Recolher pelo teclado', () => {
    drawTimeline({ schedule: demoSchedule() });
    const table = timelineTable();

    // In the table, on the first phase.
    act(() => {
      table.focus();
    });
    expect(table).toHaveAttribute('aria-activedescendant', timelineLine('Levantamento').id);
    fireEvent.keyDown(table, { key: 'ArrowLeft' });

    expect(timelineLine('Levantamento')).toHaveAttribute('aria-expanded', 'false');
    expect(timelineLines()).toHaveLength(9);

    fireEvent.keyDown(table, { key: 'ArrowRight' });

    expect(timelineLine('Levantamento')).toHaveAttribute('aria-expanded', 'true');
    expect(timelineLines()).toHaveLength(12);
    // Down and up the lines; left, from a task, goes to its phase.
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    expect(table).toHaveAttribute(
      'aria-activedescendant',
      timelineLine('Mapear processos fiscais').id,
    );
    fireEvent.keyDown(table, { key: 'ArrowLeft' });
    expect(table).toHaveAttribute('aria-activedescendant', timelineLine('Levantamento').id);
    fireEvent.keyDown(table, { key: 'End' });
    expect(table).toHaveAttribute('aria-activedescendant', timelineLine('Entrada em produção').id);
  });

  it('Nome da barra', () => {
    const schedule = demoSchedule();
    const validating = itemsOf(schedule).find(
      (item) => item.name === 'Validar cadastro de produtos',
    );
    if (validating !== undefined) {
      validating.progress = 40;
    }
    drawTimeline({ schedule });

    // The kind, the name, the dates and the progress; a milestone, its day.
    expect(
      screen.getByRole('img', { name: 'Tarefa Validar cadastro de produtos, 13/10 a 15/10, 40%' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('img', { name: 'Fase Configuração, 13/10 a 29/10, 27%' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('img', { name: 'Marco Aceite da homologação, 03/11' }),
    ).toBeInTheDocument();
  });
});

// Spec: cronograma, requirements "Arrastar na linha do tempo" and "Ligar duas barras": the events of the timeline.
describe('Mover e ligar na linha do tempo', () => {
  it('Barra arrastada em dias inteiros', () => {
    const schedule = demoSchedule();
    const validating = itemsOf(schedule).find(
      (item) => item.name === 'Validar cadastro de produtos',
    );
    const { props } = drawTimeline({ schedule, editable: true });

    dragBar(barOf('Validar cadastro de produtos'), 3);
    expect(props.onMove).toHaveBeenCalledWith(validating?.id, '2026-10-16');
    dragBar(barOf('Validar cadastro de produtos'), 5, 'end');
    expect(props.onResize).toHaveBeenCalledWith(validating?.id, '2026-10-13', '2026-10-20');
    dragBar(barOf('Validar cadastro de produtos'), -1, 'start');
    expect(props.onResize).toHaveBeenLastCalledWith(validating?.id, '2026-10-12', '2026-10-15');
    // A drag that ends on the same day changes nothing, and neither drag nor click chooses the line.
    dragBar(barOf('Validar cadastro de produtos'), 0.2);
    expect(props.onMove).toHaveBeenCalledTimes(1);
    expect(props.onChoose).not.toHaveBeenCalled();
  });

  it('Barra no lugar até a resposta', async () => {
    const schedule = demoSchedule();
    let answer: () => void = () => undefined;
    const onMove = vi.fn(
      () =>
        new Promise<void>((resolve) => {
          answer = resolve;
        }),
    );
    drawTimeline({ schedule, editable: true, onMove });

    dragBar(barOf('Validar cadastro de produtos'), 3);

    // Dropped, the bar waits at the new dates, floating, until the API answers.
    expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([11, 3]);
    expect(barOf('Validar cadastro de produtos')).toHaveClass('g-item--dragging');
    await act(async () => {
      answer();
      await new Promise((resolve) => setTimeout(resolve, 10));
    });
    // The schedule did not change: the bar goes back to its dates.
    expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([8, 3]);
    expect(barOf('Validar cadastro de produtos')).not.toHaveClass('g-item--dragging');
  });

  it('Ligar pela ponta da barra', () => {
    const schedule = demoSchedule();
    const items = itemsOf(schedule);
    const idOf = (name: string) => items.find((item) => item.name === name)?.id;
    const { props } = drawTimeline({ schedule, editable: true });

    linkBars(barOf('Integrar emissão de NF-e'), barOf('Testes integrados'));

    expect(props.onLink).toHaveBeenCalledWith(
      idOf('Integrar emissão de NF-e'),
      idOf('Testes integrados'),
    );
    // Dropped on a phase or on the bar itself, nothing links.
    linkBars(barOf('Integrar emissão de NF-e'), barOf('Homologação'));
    linkBars(barOf('Integrar emissão de NF-e'), barOf('Integrar emissão de NF-e'));
    expect(props.onLink).toHaveBeenCalledTimes(1);
    expect(props.onChoose).not.toHaveBeenCalled();
  });

  it('Divisória da tabela', () => {
    // A timeline 1,200 px wide: the table goes up to half of it.
    vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockReturnValue(1200);
    drawTimeline({ schedule: demoSchedule() });
    const divider = screen.getByRole('separator', { name: text.timeline.divider });
    const grid = () =>
      timelineOf().querySelector<HTMLElement>('.fp-gantt__grid')?.style.gridTemplateColumns;

    expect(divider).toHaveAttribute('aria-valuenow', '320');
    fireEvent.keyDown(divider, { key: 'ArrowRight' });
    expect(divider).toHaveAttribute('aria-valuenow', '336');
    expect(grid()).toMatch(/^336px /);
    // Dragged by the pointer, never narrower than 160 px.
    fireEvent.pointerDown(divider, { button: 0, clientX: 336 });
    fireEvent.pointerMove(window, { clientX: 100 });
    fireEvent.pointerUp(window, { clientX: 100 });
    expect(divider).toHaveAttribute('aria-valuenow', '160');
    fireEvent.keyDown(divider, { key: 'End' });
    expect(divider).toHaveAttribute('aria-valuenow', '600');
    vi.restoreAllMocks();
  });
});
