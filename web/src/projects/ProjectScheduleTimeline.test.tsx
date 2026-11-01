import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Project, projectProblems, type Schedule, type ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  demoSchedule,
  demoScheduleWithBaseline,
  demoScheduleWithSubtasks,
  problem,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';
import {
  barOf,
  dragBar,
  linkBars,
  spanOf,
  timelineLine,
  timelineLines,
  timelineOf,
  timelineTable,
} from '../test/timeline';

const text = ptBR.projects;

// The tab with the real Linha do tempo (ADR-0134): jsdom lays nothing out, so each day of the scale Dias takes the
// narrowest width, and the pointer is pressed and moved where a person would (src/test/timeline.ts).

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
  permissions = { canEditSchedule: true };
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.get('/api/projects/:id', ({ params }) =>
      params.id === project.id
        ? HttpResponse.json({ ...project, permissions: { ...project.permissions, ...permissions } })
        : new HttpResponse(null, { status: 404 }),
    ),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(schedule)),
  );
});

async function openTimeline(width?: number, path = `/projects/${project.id}/schedule`) {
  const view = await renderApp(path, width);
  await screen.findByRole('treegrid', { name: text.timeline.table }, { timeout: 5000 });
  await waitFor(() => {
    expect(timelineLine('Levantamento')).toBeInTheDocument();
  });
  return view;
}

// Spec: cronograma, requirement "Aba Cronograma".
describe('Aba Cronograma', () => {
  it('Alternar Linha do tempo e Lista', async () => {
    const { router, unmount } = await openTimeline();

    // On a computer, the tab opens in the Linha do tempo, in days, filling the width.
    expect(screen.getByRole('radio', { name: text.timeline.views.timeline })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(timelineOf()).toHaveAttribute('data-scale', 'days');
    expect(screen.queryByRole('grid', { name: text.schedule.table })).not.toBeInTheDocument();
    // The legend says what each color is.
    const legend = screen.getByRole('list', { name: text.timeline.legend.label });
    expect(
      within(legend)
        .getAllByRole('listitem')
        .map((entry) => entry.textContent),
    ).toEqual([
      text.timeline.legend.done,
      text.timeline.legend.todo,
      text.timeline.legend.critical,
      text.timeline.legend.phase,
      text.timeline.legend.milestone,
      text.timeline.legend.nonWorking,
    ]);
    expect(screen.getByRole('progressbar', { name: text.schedule.progress })).toHaveAttribute(
      'aria-valuenow',
      '42',
    );
    // "Hoje" brings today into view.
    const scroller = timelineOf().querySelector<HTMLElement>('.fp-gantt__scroller');
    expect(scroller?.scrollLeft).toBe(0);
    fireEvent.click(screen.getByRole('button', { name: text.timeline.today }));
    expect(scroller?.scrollLeft).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('radio', { name: text.timeline.views.list }));

    expect(await screen.findByRole('grid', { name: text.schedule.table })).toBeInTheDocument();
    expect(screen.queryByRole('treegrid')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.timeline.today })).not.toBeInTheDocument();
    expect(router.state.location.search).toBe('?view=list');
    unmount();

    // On a phone, the tab opens in the Lista.
    await renderApp(`/projects/${project.id}/schedule`, phoneWidth);
    expect(await screen.findByRole('grid', { name: text.schedule.table })).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: text.timeline.views.list })).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });
});

// Spec: cronograma, requirement "Escala e ajuste ao espaço".
describe('Escala pela tela', () => {
  it('Mudar a escala pela tela', async () => {
    const { router } = await openTimeline();

    fireEvent.click(screen.getByRole('radio', { name: text.timeline.scales.weeks }));
    await waitFor(() => {
      expect(timelineOf()).toHaveAttribute('data-scale', 'weeks');
    });
    // Each week by its Monday.
    expect(timelineOf().querySelector('.g-scale-text')?.textContent).toBe('05/10');
    fireEvent.click(screen.getByRole('radio', { name: text.timeline.scales.months }));
    await waitFor(() => {
      expect(timelineOf()).toHaveAttribute('data-scale', 'months');
    });
    expect(router.state.location.search).toBe('?scale=months');
  });
});

// Spec: cronograma, requirement "Arrastar na linha do tempo".
describe('Arrastar na linha do tempo', () => {
  /** The simulated API of the new dates: 16/10 to 20/10 for the task, and the two items after it pushed. */
  function answerDates() {
    const sent: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId/dates', async ({ request, params }) => {
        const body = (await request.json()) as { start: string; end: string | null };
        sent.push({ id: params.itemId, ...body });
        Object.assign(itemNamed('Validar cadastro de produtos'), {
          start: body.start,
          end: body.end ?? '2026-10-20',
          duration: body.end === null ? 3 : 6,
        });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: body.end === null ? 2 : 0 });
      }),
    );
    return sent;
  }

  it('Arrastar uma tarefa', async () => {
    const sent = answerDates();
    await openTimeline();
    const validating = itemNamed('Validar cadastro de produtos');
    expect(timelineOf()).toHaveAttribute('data-editable', 'true');

    // From 13/10 to 16/10.
    dragBar(barOf('Validar cadastro de produtos'), 3);

    const saved = text.schedule.messages.itemSaved.Task.replace(
      '{{name}}',
      'Validar cadastro de produtos',
    );
    expect(
      await screen.findByText(
        text.schedule.messages.pushed_other.replace('{{saved}}', saved).replace('{{count}}', '2'),
      ),
    ).toBeInTheDocument();
    // Only the start: the API keeps the duration in business days and computes the end.
    expect(sent).toEqual([{ id: validating.id, start: '2026-10-16', end: null }]);
    // The dates the API sent back: 16/10 to 20/10.
    await waitFor(() => {
      expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([11, 5]);
    });
    expect(barOf('Validar cadastro de produtos')).not.toHaveClass('g-item--dragging');
  });

  it('Puxar a borda de uma tarefa', async () => {
    const sent = answerDates();
    await openTimeline();
    const validating = itemNamed('Validar cadastro de produtos');

    // The end, from 15/10 to 20/10.
    dragBar(barOf('Validar cadastro de produtos'), 5, 'end');

    expect(
      await screen.findByText(
        text.schedule.messages.itemSaved.Task.replace('{{name}}', 'Validar cadastro de produtos'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: validating.id, start: '2026-10-13', end: '2026-10-20' }]);
  });

  it('Arrastar sem permissão', async () => {
    const sent = answerDates();
    permissions = { canEditSchedule: false };
    const { unmount } = await openTimeline();

    // The technical lead sees the timeline, but its bars neither move nor link.
    expect(timelineOf()).toHaveAttribute('data-editable', 'false');
    expect(
      barOf('Validar cadastro de produtos').querySelector('.g-handle, .g-connector'),
    ).toBeNull();
    dragBar(barOf('Validar cadastro de produtos'), 3);
    expect(screen.queryByRole('button', { name: text.schedule.newItem })).not.toBeInTheDocument();
    unmount();

    // Nor on a phone, even for who edits the schedule.
    permissions = { canEditSchedule: true };
    await openTimeline(phoneWidth, `/projects/${project.id}/schedule?view=timeline`);
    expect(timelineOf()).toHaveAttribute('data-editable', 'false');
    expect(sent).toEqual([]);
  });
});

// Spec: cronograma, requirement "Ligar duas barras".
describe('Ligar duas barras', () => {
  it('Ligar duas barras', async () => {
    const sent: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request, params }) => {
        const body = (await request.json()) as { predecessorIds: string[] };
        sent.push({ id: params.itemId, ...body });
        Object.assign(itemNamed('Testes integrados'), { predecessorIds: body.predecessorIds });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    const { container } = await openTimeline();
    const invoices = itemNamed('Integrar emissão de NF-e');
    const tests = itemNamed('Testes integrados');
    expect(container.querySelectorAll('.g-dep')).toHaveLength(6);

    linkBars(barOf('Integrar emissão de NF-e'), barOf('Testes integrados'));

    expect(
      await screen.findByText(
        text.timeline.messages.linked
          .replace('{{successor}}', 'Testes integrados')
          .replace('{{predecessor}}', 'Integrar emissão de NF-e'),
      ),
    ).toBeInTheDocument();
    // The change of the task, as its details send it, with one more dependency and without the progress.
    expect(sent).toEqual([
      {
        id: tests.id,
        phaseId: tests.phaseId,
        name: 'Testes integrados',
        start: '2026-10-23',
        duration: 5,
        ownerId: tests.owner?.id,
        // The client in charge stays as it was (ADR-0102).
        ownedByClient: false,
        progress: null,
        predecessorIds: [itemNamed('Parametrizar regras de ICMS').id, invoices.id],
        // It stays where it is, right in the phase (ADR-0088).
        parentId: null,
      },
    ]);
    // And the arrow appears.
    await waitFor(() => {
      expect(container.querySelectorAll('.g-dep')).toHaveLength(7);
    });
  });

  it('Ligação circular pela linha do tempo', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () =>
        problem(400, projectProblems.scheduleCycle),
      ),
    );
    const { container } = await openTimeline();

    linkBars(barOf('Testes integrados'), barOf('Parametrizar regras de ICMS'));

    expect(
      await screen.findByText(
        text.timeline.messages.cycle
          .replace('{{predecessor}}', 'Testes integrados')
          .replace('{{successor}}', 'Parametrizar regras de ICMS'),
      ),
    ).toBeInTheDocument();
    expect(itemNamed('Parametrizar regras de ICMS').predecessorIds).toEqual([
      itemNamed('Mapear processos fiscais').id,
    ]);
    expect(container.querySelectorAll('.g-dep')).toHaveLength(6);
  });
});

// Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
describe('Subtarefas na linha do tempo', () => {
  it('Ligar a uma tarefa-resumo', async () => {
    schedule = demoScheduleWithSubtasks();
    const sent: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        sent.push(await request.json());
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openTimeline();

    // The task with subtasks has no point to link from; dropped on it, a link says why it does not link.
    expect(barOf('Testes integrados').querySelector('.g-connector')).toBeNull();
    linkBars(barOf('Integrar emissão de NF-e'), barOf('Testes integrados'));

    expect(
      await screen.findByText(
        text.timeline.messages.summaryLink.replace('{{name}}', 'Testes integrados'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([]);
  });

  it('Ligar subtarefas mantém a tarefa', async () => {
    schedule = demoScheduleWithSubtasks();
    const sent: Record<string, unknown>[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        sent.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openTimeline();

    linkBars(barOf('Integrar emissão de NF-e'), barOf('Executar testes'));

    await waitFor(() => {
      expect(sent).toEqual([
        expect.objectContaining({
          name: 'Executar testes',
          parentId: itemNamed('Testes integrados').id,
        }),
      ]);
    });
  });
});

// Spec: cronograma, requirement "Detalhe pela linha do tempo".
describe('Detalhe pela linha do tempo', () => {
  it('Detalhe pela linha do tempo', async () => {
    await openTimeline();

    fireEvent.click(barOf('Validar cadastro de produtos'));

    const drawer = await screen.findByRole('dialog', { name: text.schedule.details.label });
    expect(
      within(drawer).getByRole('heading', { name: 'Validar cadastro de produtos' }),
    ).toBeVisible();
    await waitFor(() => {
      expect(timelineLine('Validar cadastro de produtos')).toHaveAttribute('aria-selected', 'true');
    });
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: text.schedule.details.label }),
      ).not.toBeInTheDocument();
    });
    expect(timelineLine('Validar cadastro de produtos')).toHaveAttribute('aria-selected', 'false');

    // A phase opens the phase, for who edits the schedule.
    fireEvent.click(timelineLine('Configuração'));
    expect(
      await screen.findByRole('dialog', { name: text.schedule.phaseDialog.editTitle }),
    ).toBeVisible();
  });
});

// Spec: cronograma, requirement "Linha do tempo pelo teclado".
describe('Linha do tempo pelo teclado', () => {
  it('Detalhe pelo teclado', async () => {
    await openTimeline();
    const table = timelineTable();

    // Into the table, two lines down, and Enter.
    act(() => {
      table.focus();
    });
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    fireEvent.keyDown(table, { key: 'Enter' });

    const drawer = await screen.findByRole('dialog', { name: text.schedule.details.label });
    expect(within(drawer).getByRole('heading', { name: 'Mapear processos fiscais' })).toBeVisible();
  });
});

// Spec: cronograma, requirement "Linha do tempo sem licença de terceiros".
describe('Linha do tempo sem licença de terceiros', () => {
  it('Instalação sem chave de licença', async () => {
    // The installation has no key of Syncfusion, and the timeline asks for none: any call the test does not simulate
    // fails it.
    const { container } = await openTimeline();

    // Complete: the lines, the bars, the arrows and today, without a notice of a trial.
    expect(timelineLines()).toHaveLength(12);
    expect(container.querySelectorAll('.g-bar')).toHaveLength(7);
    expect(container.querySelectorAll('.g-dep')).toHaveLength(6);
    expect(container.querySelector('.g-today-label')).not.toBeNull();
    expect(container.textContent).not.toMatch(/syncfusion|licen[cs]|trial|avalia/i);
  });

  it('Barra que volta na recusa', async () => {
    let refuse: () => void = () => undefined;
    const refused = new Promise<void>((resolve) => {
      refuse = resolve;
    });
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId/dates', async () => {
        await refused;
        return problem(409, projectProblems.closed);
      }),
    );
    await openTimeline();

    dragBar(barOf('Validar cadastro de produtos'), 3);

    // While the API answers, the bar stays where it was dropped.
    expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([11, 3]);
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 50));
    });
    expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([11, 3]);
    refuse();

    // Refused, it goes back to its dates, and the screen says why.
    expect(await screen.findByText(text.schedule.messages.closed)).toBeInTheDocument();
    await waitFor(() => {
      expect(spanOf(barOf('Validar cadastro de produtos'))).toEqual([8, 3]);
    });
    expect(barOf('Validar cadastro de produtos')).not.toHaveClass('g-item--dragging');
  });
});

// Spec: cronograma, requirement "Expandir o cronograma".
describe('Expandir o cronograma', () => {
  it('Expandir o cronograma', async () => {
    await openTimeline();

    fireEvent.click(screen.getByRole('button', { name: text.timeline.expand }));

    const expanded = await screen.findByRole('dialog', { name: text.timeline.expandedTitle });
    expect(within(expanded).getByText('ERP-FIN · Implantação do ERP financeiro')).toBeVisible();
    expect(
      within(expanded).getByRole('radiogroup', { name: text.timeline.scales.label }),
    ).toBeVisible();
    expect(within(expanded).getByRole('button', { name: text.timeline.today })).toBeVisible();
    expect(within(expanded).getByRole('button', { name: text.schedule.newItem })).toBeVisible();
    // The full table beside the timeline: name, start, end, deadline, owner and progress.
    await within(expanded).findByRole('treegrid', { name: text.timeline.table });
    expect(timelineOf(expanded)).toHaveAttribute('data-detailed', 'true');
    expect(timelineOf(expanded)).toHaveAttribute('data-editable', 'true');
    expect(
      within(timelineTable(expanded))
        .getAllByRole('columnheader')
        .map((header) => header.textContent),
    ).toEqual([
      text.timeline.columns.name,
      text.timeline.columns.start,
      text.timeline.columns.end,
      text.timeline.columns.deadline,
      text.timeline.columns.owner,
      text.timeline.columns.progress,
    ]);
    expect(timelineLine('Validar cadastro de produtos', expanded).textContent).toContain(
      '13/1015/10Rafael',
    );

    fireEvent.click(within(expanded).getByRole('button', { name: text.timeline.collapse }));

    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: text.timeline.expandedTitle }),
      ).not.toBeInTheDocument();
    });
    expect(timelineOf()).toHaveAttribute('data-detailed', 'false');
  });
});

function legendEntries() {
  return within(screen.getByRole('list', { name: text.timeline.legend.label }))
    .getAllByRole('listitem')
    .map((entry) => entry.textContent);
}

// Spec: cronograma, requirement "Caminho crítico pela tela".
describe('Caminho crítico pela tela', () => {
  it('Exibir sem linha de base e caminho crítico', async () => {
    schedule = demoScheduleWithBaseline();
    const { container } = await openTimeline();

    // Both are on at first, and the legend says what they are.
    expect(timelineOf()).toHaveAttribute('data-baseline', 'true');
    expect(timelineOf()).toHaveAttribute('data-critical', 'true');
    expect(container.querySelectorAll('.g-baseline')).toHaveLength(7);
    expect(legendEntries()).toEqual([
      text.timeline.legend.done,
      text.timeline.legend.todo,
      text.timeline.legend.critical,
      text.timeline.legend.baseline,
      text.timeline.legend.phase,
      text.timeline.legend.milestone,
      text.timeline.legend.nonWorking,
    ]);

    fireEvent.click(screen.getByRole('button', { name: text.baseline.display }));
    const menu = screen.getByRole('menu', { name: text.baseline.displayLabel });
    fireEvent.click(
      within(menu).getByRole('menuitemcheckbox', { name: text.baseline.show.baseline }),
    );
    fireEvent.click(
      within(menu).getByRole('menuitemcheckbox', { name: text.baseline.show.critical }),
    );

    // Each one on its own, and the menu stays open to turn the other.
    expect(
      within(menu)
        .getAllByRole('menuitemcheckbox')
        .map((option) => option.getAttribute('aria-checked')),
    ).toEqual(['false', 'false']);
    await waitFor(() => {
      expect(timelineOf()).toHaveAttribute('data-critical', 'false');
    });
    expect(timelineOf()).toHaveAttribute('data-baseline', 'false');
    expect(container.querySelectorAll('.g-baseline, .g-bar--critical')).toHaveLength(0);
    fireEvent.keyDown(menu, { key: 'Escape' });
    await waitFor(() => {
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
    expect(legendEntries()).toEqual([
      text.timeline.legend.done,
      text.timeline.legend.todo,
      text.timeline.legend.phase,
      text.timeline.legend.milestone,
      text.timeline.legend.nonWorking,
    ]);

    // Expandir keeps what "Exibir" has, with the two options right in its header.
    fireEvent.click(screen.getByRole('button', { name: text.timeline.expand }));
    const expanded = await screen.findByRole('dialog', { name: text.timeline.expandedTitle });
    const baseline = within(expanded).getByRole('checkbox', { name: text.baseline.show.baseline });
    expect(baseline).not.toBeChecked();
    expect(
      within(expanded).getByRole('checkbox', { name: text.baseline.show.critical }),
    ).not.toBeChecked();
    fireEvent.click(baseline);
    await within(expanded).findByRole('treegrid', { name: text.timeline.table });
    await waitFor(() => {
      expect(timelineOf(expanded)).toHaveAttribute('data-baseline', 'true');
    });
    expect(timelineOf(expanded)).toHaveAttribute('data-critical', 'false');
    expect(timelineOf(expanded)).toHaveAttribute('data-detailed', 'true');
  });

  it('Sem linha de base, a legenda não a cita', async () => {
    const { container } = await openTimeline();

    // With the baseline on in "Exibir", but none saved.
    expect(timelineOf()).toHaveAttribute('data-baseline', 'true');
    expect(container.querySelectorAll('.g-baseline')).toHaveLength(0);
    expect(legendEntries()).not.toContain(text.timeline.legend.baseline);
  });
});

// Spec: cronograma, requirement "Data-limite pela tela" (GanttA-2).
describe('Data-limite na linha do tempo', () => {
  it('Marcador da data-limite', async () => {
    Object.assign(itemNamed('Testes integrados'), {
      deadline: '2026-10-28',
      deadlineDeviation: 1,
    });
    Object.assign(itemNamed('Integrar emissão de NF-e'), {
      deadline: '2026-10-30',
      deadlineDeviation: -2,
    });
    const { container } = await openTimeline();

    const markers = [...container.querySelectorAll('.g-deadline')];
    expect(markers.map((marker) => marker.getAttribute('data-day'))).toEqual([
      '2026-10-30',
      '2026-10-28',
    ]);
    // Missed in the color of the alarm; at the end of the day, 05/10 being the first day of the timeline.
    expect(markers.map((marker) => marker.classList.contains('g-deadline--missed'))).toEqual([
      false,
      true,
    ]);
    expect(markers[1]?.querySelector('.g-deadline-line')?.getAttribute('x1')).toBe(String(24 * 20));
    expect(barOf('Testes integrados')).toHaveAccessibleName(
      text.timeline.bars.missedDeadline_one
        .replace('{{label}}', 'Tarefa Testes integrados, 23/10 a 29/10, 0%')
        .replace('{{count}}', '1')
        .replace('{{day}}', '28/10'),
    );
    expect(legendEntries()).toContain(text.timeline.legend.deadline);
  });

  it('Data-limite depois do fim do cronograma', async () => {
    // The schedule ends on 03/11: the timeline widens to the deadline three days later.
    Object.assign(itemNamed('Aceite da homologação'), {
      deadline: '2026-11-06',
      deadlineDeviation: -3,
    });
    const { container } = await openTimeline();

    const marker = container.querySelector('.g-deadline');
    expect(marker?.getAttribute('data-day')).toBe('2026-11-06');
    expect(marker?.querySelector('.g-deadline-line')?.getAttribute('x1')).toBe(String(33 * 20));
  });

  it('Sem data-limite na legenda', async () => {
    await openTimeline();

    expect(legendEntries()).not.toContain(text.timeline.legend.deadline);
  });
});
