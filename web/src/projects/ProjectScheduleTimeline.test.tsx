import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { forwardRef, useEffect, useImperativeHandle } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { type Project, projectProblems, type Schedule, type ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { ganttRowsOf } from '../schedule-gantt/ganttRows';
import type { ScheduleGanttHandle, ScheduleGanttProps } from '../schedule-gantt/ScheduleGantt';
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

const text = ptBR.projects;

/**
 * jsdom does not draw the Syncfusion Gantt faithfully (ScheduleGantt.test.tsx covers the real one): the tab is tested
 * with a ScheduleGantt that lists its lines as buttons and keeps what it received, to raise its events as a drag, a
 * stretch or a link would.
 */
const gantt = vi.hoisted(() => ({
  props: undefined as ScheduleGanttProps | undefined,
  todayAsked: 0,
}));

vi.mock('../schedule-gantt/ScheduleGantt', () => {
  const Simulated = forwardRef<ScheduleGanttHandle, ScheduleGanttProps>(
    function Simulated(props, ref) {
      useEffect(() => {
        gantt.props = props;
      });
      useImperativeHandle(ref, () => ({
        goToToday: () => {
          gantt.todayAsked += 1;
        },
      }));
      return (
        <ul
          aria-label="simulated timeline"
          data-editable={String(props.editable)}
          data-detailed={String(props.detailed ?? false)}
          data-scale={props.scale}
        >
          {ganttRowsOf(props.schedule).map((row) => (
            <li key={row.key}>
              <button
                type="button"
                onClick={() => {
                  props.onChoose(row.target);
                }}
              >
                {row.name}
              </button>
            </li>
          ))}
        </ul>
      );
    },
  );
  return { ScheduleGantt: Simulated, default: Simulated };
});

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
  gantt.props = undefined;
  gantt.todayAsked = 0;
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

function timeline() {
  return screen.getByRole('list', { name: 'simulated timeline' });
}

async function openTimeline(width?: number) {
  const view = await renderApp(`/projects/${project.id}/schedule`, width);
  await screen.findByRole('list', { name: 'simulated timeline' });
  await waitFor(() => {
    expect(within(timeline()).getByRole('button', { name: 'Levantamento' })).toBeInTheDocument();
  });
  return view;
}

/** What the timeline received last. */
function received(): ScheduleGanttProps {
  if (gantt.props === undefined) {
    throw new Error('No timeline.');
  }
  return gantt.props;
}

const startOf = (name: string) =>
  received()
    .schedule.phases.flatMap((phase) => phase.items)
    .find((item) => item.name === name)?.start;

// Spec: cronograma, requirement "Aba Cronograma".
describe('Aba Cronograma', () => {
  it('Alternar Linha do tempo e Lista', async () => {
    const { router, unmount } = await openTimeline();

    // On a computer, the tab opens in the Linha do tempo, in days, filling the width.
    expect(screen.getByRole('radio', { name: text.timeline.views.timeline })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(timeline()).toHaveAttribute('data-scale', 'days');
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
    fireEvent.click(screen.getByRole('button', { name: text.timeline.today }));
    expect(gantt.todayAsked).toBe(1);

    fireEvent.click(screen.getByRole('radio', { name: text.timeline.views.list }));

    expect(await screen.findByRole('grid', { name: text.schedule.table })).toBeInTheDocument();
    expect(screen.queryByRole('list', { name: 'simulated timeline' })).not.toBeInTheDocument();
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
      expect(timeline()).toHaveAttribute('data-scale', 'weeks');
    });
    fireEvent.click(screen.getByRole('radio', { name: text.timeline.scales.months }));
    await waitFor(() => {
      expect(timeline()).toHaveAttribute('data-scale', 'months');
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
    expect(timeline()).toHaveAttribute('data-editable', 'true');

    act(() => {
      received().onMove(validating.id, '2026-10-16');
    });

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
    await waitFor(() => {
      expect(startOf('Validar cadastro de produtos')).toBe('2026-10-16');
    });
  });

  it('Puxar a borda de uma tarefa', async () => {
    const sent = answerDates();
    await openTimeline();
    const validating = itemNamed('Validar cadastro de produtos');

    act(() => {
      received().onResize(validating.id, '2026-10-13', '2026-10-20');
    });

    expect(
      await screen.findByText(
        text.schedule.messages.itemSaved.Task.replace('{{name}}', 'Validar cadastro de produtos'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: validating.id, start: '2026-10-13', end: '2026-10-20' }]);
  });

  it('Arrastar sem permissão', async () => {
    permissions = { canEditSchedule: false };
    const { unmount } = await openTimeline();

    // The technical lead sees the timeline, but its bars neither move nor link.
    expect(timeline()).toHaveAttribute('data-editable', 'false');
    expect(screen.queryByRole('button', { name: text.schedule.newItem })).not.toBeInTheDocument();
    unmount();

    // Nor on a phone, even for who edits the schedule.
    permissions = { canEditSchedule: true };
    await renderApp(`/projects/${project.id}/schedule?view=timeline`, phoneWidth);
    expect(await screen.findByRole('list', { name: 'simulated timeline' })).toHaveAttribute(
      'data-editable',
      'false',
    );
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
    await openTimeline();
    const invoices = itemNamed('Integrar emissão de NF-e');
    const tests = itemNamed('Testes integrados');

    act(() => {
      received().onLink(invoices.id, tests.id);
    });

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
    await waitFor(() => {
      expect(
        received()
          .schedule.phases.flatMap((phase) => phase.items)
          .find((item) => item.id === tests.id)?.predecessorIds,
      ).toContain(invoices.id);
    });
  });

  it('Ligação circular pela linha do tempo', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () =>
        problem(400, projectProblems.scheduleCycle),
      ),
    );
    await openTimeline();

    act(() => {
      received().onLink(
        itemNamed('Testes integrados').id,
        itemNamed('Parametrizar regras de ICMS').id,
      );
    });

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

    act(() => {
      received().onLink(
        itemNamed('Testes integrados').id,
        itemNamed('Testes com a equipe do cliente').id,
      );
    });

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

    act(() => {
      received().onLink(itemNamed('Integrar emissão de NF-e').id, itemNamed('Executar testes').id);
    });

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

    fireEvent.click(
      within(timeline()).getByRole('button', { name: 'Validar cadastro de produtos' }),
    );

    const drawer = await screen.findByRole('dialog', { name: text.schedule.details.label });
    expect(
      within(drawer).getByRole('heading', { name: 'Validar cadastro de produtos' }),
    ).toBeVisible();
    await waitFor(() => {
      expect(received().selectedId).toBe(itemNamed('Validar cadastro de produtos').id);
    });
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: text.schedule.details.label }),
      ).not.toBeInTheDocument();
    });

    // A phase opens the phase, for who edits the schedule.
    fireEvent.click(within(timeline()).getByRole('button', { name: 'Configuração' }));
    expect(
      await screen.findByRole('dialog', { name: text.schedule.phaseDialog.editTitle }),
    ).toBeVisible();
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
    // The full table beside the timeline, and only one timeline on the screen.
    const detailed = await within(expanded).findByRole('list', { name: 'simulated timeline' });
    expect(detailed).toHaveAttribute('data-detailed', 'true');
    expect(detailed).toHaveAttribute('data-editable', 'true');
    expect(screen.getAllByRole('list', { name: 'simulated timeline' })).toHaveLength(1);

    fireEvent.click(within(expanded).getByRole('button', { name: text.timeline.collapse }));

    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: text.timeline.expandedTitle }),
      ).not.toBeInTheDocument();
    });
    expect(await screen.findByRole('list', { name: 'simulated timeline' })).toHaveAttribute(
      'data-detailed',
      'false',
    );
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
    await openTimeline();

    // Both are on at first, and the legend says what they are.
    expect([received().showBaseline, received().showCritical]).toEqual([true, true]);
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
      expect([received().showBaseline, received().showCritical]).toEqual([false, false]);
    });
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
    await waitFor(() => {
      expect([received().detailed, received().showBaseline, received().showCritical]).toEqual([
        true,
        true,
        false,
      ]);
    });
  });

  it('Sem linha de base, a legenda não a cita', async () => {
    await openTimeline();

    // With the baseline on in "Exibir", but none saved.
    expect(received().showBaseline).toBe(true);
    expect(legendEntries()).not.toContain(text.timeline.legend.baseline);
  });
});
