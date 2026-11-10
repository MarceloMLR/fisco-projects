import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Project, Schedule, ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoProjects,
  demoSchedule,
  demoScheduleWithSubtasks,
  type DemoProject,
  problem,
  projectsPage,
  scheduleSeenBy,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.schedule;
const board = ptBR.projects.board;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
/** The progress the screen asked for, by task. */
const sent: { id: string; progress: number }[] = [];
/** The answer to the progress: saved, or refused with a status. */
let refusal: number | undefined;

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
  sent.length = 0;
  refusal = undefined;
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
    // The API keeps the progress, and the schedule comes back with it, as the server computes it.
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ request, params }) => {
      const { progress } = (await request.json()) as { progress: number };
      const id = String(params.itemId);
      sent.push({ id, progress });
      if (refusal !== undefined) {
        return problem(refusal);
      }
      for (const phase of schedule.phases) {
        phase.items = phase.items.map((item) =>
          item.id === id
            ? {
                ...item,
                progress,
                completedOn: progress === 100 ? '2026-10-16' : null,
                late: false,
              }
            : item,
        );
      }
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

const columnName = (id: 'todo' | 'doing' | 'done', count: number) =>
  (count === 0 ? board.column_zero : count === 1 ? board.column_one : board.column_other)
    .replace('{{name}}', board.columns[id])
    .replace('{{count}}', String(count));

/** The column of the board, found by its name and its count. */
function column(id: 'todo' | 'doing' | 'done', count: number) {
  return screen.getByRole('region', { name: columnName(id, count) });
}

/** The names of the cards of a column, in order. */
function cardsIn(element: HTMLElement): string[] {
  return within(element)
    .queryAllByRole('listitem')
    .map((card) => card.querySelector('button')?.textContent ?? '')
    .filter((name) => name !== '');
}

function cardOf(name: string): HTMLElement {
  const card = document.querySelector<HTMLElement>(`[data-item="${itemNamed(name).id}"]`);
  if (card === null) {
    throw new Error(`No card for ${name}.`);
  }
  return card;
}

async function openBoard(query = '', width?: number) {
  await renderApp(`/projects/${project.id}/schedule?view=board${query}`, width);
  await screen.findByRole('region', { name: board.label });
}

/** A transfer of the drag, as the browser gives it. */
function transfer() {
  const data = new Map<string, string>();
  return {
    effectAllowed: 'all',
    dropEffect: 'none',
    setData: (type: string, value: string) => data.set(type, value),
    getData: (type: string) => data.get(type) ?? '',
  };
}

/** Drags the card of a task to a column, as the pointer does on a computer. */
function drag(name: string, to: HTMLElement) {
  const dataTransfer = transfer();
  fireEvent.dragStart(cardOf(name), { dataTransfer });
  fireEvent.dragOver(to, { dataTransfer });
  fireEvent.drop(to, { dataTransfer });
  fireEvent.dragEnd(cardOf(name), { dataTransfer });
}

// Spec: cronograma, requirements "Visualização Quadro", "Cartão do quadro", "Ordem dos cartões" and "Quadro com o filtro e no
// celular" (ADR-0155). Today is 16/10/2026.
describe('Quadro das tarefas', () => {
  it('Tarefas nas colunas do quadro', async () => {
    schedule = demoScheduleWithSubtasks();
    await openBoard();

    // The summary task (Testes integrados) and the milestone have no card; the subtasks do.
    expect(cardsIn(column('todo', 3))).toEqual([
      'Roteiro de testes',
      'Executar testes',
      'Testes com a equipe do cliente',
    ]);
    expect(cardsIn(column('doing', 3))).toHaveLength(3);
    expect(cardsIn(column('done', 2))).toHaveLength(2);
    expect(screen.queryByRole('button', { name: 'Testes integrados' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Aceite da homologação' })).not.toBeInTheDocument();
    // The view stays chosen in the toolbar.
    expect(screen.getByRole('radio', { name: ptBR.projects.timeline.views.board })).toBeChecked();
  });

  it('Cartão da tarefa', async () => {
    schedule = demoScheduleWithSubtasks();
    const script = itemNamed('Roteiro de testes');
    Object.assign(script, { progress: 40, late: true, lateDays: 2 });
    await openBoard();

    const card = cardOf('Roteiro de testes');
    expect(column('doing', 4)).toContainElement(card);
    expect(within(card).getByText('Configuração · Testes integrados')).toBeVisible();
    expect(within(card).getByText(demoPeople.carla.name)).toBeVisible();
    expect(within(card).getByText(text.lateDays_other.replace('{{count}}', '2'))).toBeVisible();
    expect(
      within(card).getByRole('progressbar', {
        name: text.itemProgress.replace('{{name}}', 'Roteiro de testes'),
      }),
    ).toHaveAttribute('aria-valuenow', '40');
    // A task without an owner says so, and a done one says when it was completed.
    expect(within(cardOf('Testes com a equipe do cliente')).getByText(text.noOwner)).toBeVisible();
    expect(
      within(cardOf('Mapear processos fiscais')).getByText(
        text.completedOn.replace('{{day}}', '09/10/2026'),
      ),
    ).toBeVisible();
  });

  it('Ordem dos cartões', async () => {
    await openBoard();

    // Em andamento by the end, from the earliest; Concluída by the completion, from the latest.
    expect(cardsIn(column('doing', 3))).toEqual([
      'Validar cadastro de produtos',
      'Parametrizar regras de ICMS',
      'Integrar emissão de NF-e',
    ]);
    expect(cardsIn(column('done', 2))).toEqual(['Mapear processos fiscais', 'Reunião de abertura']);
    expect(cardsIn(column('todo', 2))).toEqual([
      'Testes integrados',
      'Testes com a equipe do cliente',
    ]);
  });

  it('Detalhe pelo cartão', async () => {
    await openBoard();

    fireEvent.click(
      within(cardOf('Integrar emissão de NF-e')).getByRole('button', {
        name: 'Integrar emissão de NF-e',
      }),
    );
    const drawer = await screen.findByRole('dialog', { name: text.details.label });
    expect(within(drawer).getByDisplayValue('Integrar emissão de NF-e')).toBeVisible();
  });

  it('Quadro com filtro', async () => {
    // Marina is in charge of Reunião de abertura and of Testes integrados.
    await openBoard('&filter=mine');

    expect(cardsIn(column('todo', 1))).toEqual(['Testes integrados']);
    expect(cardsIn(column('doing', 0))).toEqual([]);
    expect(cardsIn(column('done', 1))).toEqual(['Reunião de abertura']);
    expect(within(column('doing', 0)).getByText(board.columnEmpty)).toBeVisible();
  });

  it('Quadro com filtro sem tarefas', async () => {
    await openBoard('&filter=missedDeadlines');

    const shown = screen.getByRole('region', { name: board.label });
    expect(within(shown).getByText(text.filter.empty)).toBeVisible();
    expect(within(shown).queryAllByRole('listitem')).toEqual([]);
  });

  it('Quadro no celular', async () => {
    schedule = scheduleSeenBy(demoScheduleWithSubtasks(), demoPeople.carla.id);
    permissions = { canEditSchedule: false };
    await openBoard('', phoneWidth);

    expect(column('todo', 3)).toBeVisible();
    expect(column('doing', 3)).toBeVisible();
    expect(column('done', 2)).toBeVisible();
    // On a phone, cards move only by "Mover".
    const script = cardOf('Roteiro de testes');
    expect(script).toHaveAttribute('draggable', 'false');
    expect(
      within(script).getByRole('button', {
        name: board.move.replace('{{name}}', 'Roteiro de testes'),
      }),
    ).toBeVisible();
  });
});

// Spec: cronograma, requirements "Mover o cartão no quadro" and "Quem move o cartão" (ADR-0155).
describe('Mover o cartão no quadro', () => {
  it('Arrastar para Concluída', async () => {
    await openBoard();

    drag('Parametrizar regras de ICMS', column('done', 2));

    expect(
      await screen.findByText(
        text.messages.completed.replace('{{name}}', 'Parametrizar regras de ICMS'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: itemNamed('Parametrizar regras de ICMS').id, progress: 100 }]);
    await waitFor(() => {
      expect(cardsIn(column('done', 3))).toContain('Parametrizar regras de ICMS');
    });
    expect(cardOf('Parametrizar regras de ICMS')).not.toHaveAttribute('aria-busy');
  });

  it('Arrastar para Em andamento pede o percentual', async () => {
    await openBoard();

    drag('Testes integrados', column('doing', 3));

    const dialog = await screen.findByRole('dialog', { name: text.progressDialog.startTitle });
    expect(sent).toEqual([]);
    fireEvent.change(within(dialog).getByLabelText(text.form.progress), {
      target: { value: '30' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByText(
        text.messages.progressUpdated
          .replace('{{name}}', 'Testes integrados')
          .replace('{{progress}}', '30'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: itemNamed('Testes integrados').id, progress: 30 }]);
    await waitFor(() => {
      expect(cardsIn(column('doing', 4))).toContain('Testes integrados');
    });
  });

  it('Voltar para A fazer', async () => {
    await openBoard();

    drag('Integrar emissão de NF-e', column('todo', 2));

    expect(
      await screen.findByText(board.restarted.replace('{{name}}', 'Integrar emissão de NF-e')),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: itemNamed('Integrar emissão de NF-e').id, progress: 0 }]);
    await waitFor(() => {
      expect(cardsIn(column('todo', 3))).toContain('Integrar emissão de NF-e');
    });
  });

  it('Mover pelo menu', async () => {
    // Carla is in charge of Roteiro de testes, at 0%, and does not edit the schedule.
    schedule = scheduleSeenBy(demoScheduleWithSubtasks(), demoPeople.carla.id);
    permissions = { canEditSchedule: false };
    await openBoard();

    fireEvent.click(
      within(cardOf('Roteiro de testes')).getByRole('button', {
        name: board.move.replace('{{name}}', 'Roteiro de testes'),
      }),
    );
    const menu = await screen.findByRole('menu', {
      name: board.move.replace('{{name}}', 'Roteiro de testes'),
    });
    const moveTo = (id: 'todo' | 'doing' | 'done') =>
      board.moveTo.replace('{{column}}', board.columns[id]);
    expect(
      within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([moveTo('doing'), moveTo('done')]);
    fireEvent.click(within(menu).getByRole('menuitem', { name: moveTo('done') }));

    expect(
      await screen.findByText(text.messages.completed.replace('{{name}}', 'Roteiro de testes')),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: itemNamed('Roteiro de testes').id, progress: 100 }]);
    // Choosing in the menu does not open the details.
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
  });

  it('Cartão de quem não move', async () => {
    permissions = { canEditSchedule: false };
    schedule = scheduleSeenBy(demoSchedule(), demoPeople.marina.id, true);
    await openBoard();

    const card = cardOf('Parametrizar regras de ICMS');
    expect(card).toHaveAttribute('draggable', 'false');
    expect(screen.queryByRole('button', { name: /^Mover / })).not.toBeInTheDocument();
    // Dragging it changes nothing.
    drag('Parametrizar regras de ICMS', column('done', 2));
    expect(sent).toEqual([]);
    // The details open as usual.
    fireEvent.click(card);
    expect(await screen.findByRole('dialog', { name: text.details.label })).toBeVisible();
  });

  it('Cartão da tarefa de outra pessoa', async () => {
    // Carla moves only the cards of her tasks.
    schedule = scheduleSeenBy(demoScheduleWithSubtasks(), demoPeople.carla.id);
    permissions = { canEditSchedule: false };
    await openBoard();

    expect(cardOf('Parametrizar regras de ICMS')).toHaveAttribute('draggable', 'false');
    expect(
      within(cardOf('Parametrizar regras de ICMS')).queryByRole('button', {
        name: board.move.replace('{{name}}', 'Parametrizar regras de ICMS'),
      }),
    ).not.toBeInTheDocument();
    expect(cardOf('Roteiro de testes')).toHaveAttribute('draggable', 'true');
  });

  it('Recusa ao mover', async () => {
    refusal = 500;
    await openBoard();

    drag('Parametrizar regras de ICMS', column('done', 2));

    expect(await screen.findByText(text.messages.failed)).toBeInTheDocument();
    await waitFor(() => {
      expect(cardsIn(column('doing', 3))).toContain('Parametrizar regras de ICMS');
    });
    expect(cardsIn(column('done', 2))).not.toContain('Parametrizar regras de ICMS');
  });
});
