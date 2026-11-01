import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Project, projectProblems, type Schedule, type ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
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
const editText = ptBR.projects.tableEdit;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
/** What the screen asked of the API: the path after the schedule and the body. */
const sent: { path: string; body: unknown }[] = [];

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
  signedInAs('ProjectManager');
  const keep = async (request: Request, path: string) => {
    sent.push({ path, body: await request.json() });
  };
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
    http.put('/api/projects/:id/schedule/items/:itemId', async ({ request, params }) => {
      await keep(request, `items/${String(params.itemId)}`);
      return HttpResponse.json({ pushed: 0 });
    }),
    http.put('/api/projects/:id/schedule/items/:itemId/dates', async ({ request, params }) => {
      await keep(request, `dates/${String(params.itemId)}`);
      return HttpResponse.json({ pushed: 2 });
    }),
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ request, params }) => {
      await keep(request, `progress/${String(params.itemId)}`);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

function rowOf(name: string) {
  const row = table().querySelector<HTMLElement>(`[data-id="${itemNamed(name).id}"]`);
  if (row === null) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

/** Types in the field of a cell and waits for the cell to keep it, as the Data Grid sets the value of a cell later. */
async function typeIn(field: HTMLElement, typed: string, shown = typed) {
  fireEvent.change(field, { target: { value: typed } });
  await waitFor(() => {
    expect(field).toHaveValue(shown);
  });
}

function cellOf(name: string, field: string): HTMLElement {
  const cell = rowOf(name).querySelector<HTMLElement>(`[data-field="${field}"]`);
  if (cell === null) {
    throw new Error(`No cell ${field} for ${name}.`);
  }
  return cell;
}

async function openList() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  await waitFor(() => {
    expect(within(table()).getAllByRole('row').length).toBeGreaterThan(1);
  });
}

async function turnOn() {
  fireEvent.click(screen.getByRole('button', { name: editText.button }));
  await screen.findByText(editText.help);
}

/** Starts the edition of a cell by Enter, as the keyboard does, and gives its field. */
async function editCell(name: string, field: string, label: string) {
  const cell = cellOf(name, field);
  fireEvent.click(cell);
  fireEvent.keyDown(cell, { key: 'Enter' });
  return within(cellOf(name, field)).findByRole('textbox', { name: label });
}

// Spec: cronograma, requirements "Modo de edição na Lista", "Células editáveis na Lista", "Salvar pela célula" and
// "Valor inválido na célula" (ADR-0143).
describe('Edição na tabela', () => {
  it('Ligar o modo de edição', async () => {
    await openList();
    const button = screen.getByRole('button', { name: editText.button });
    expect(button).toHaveAttribute('aria-pressed', 'false');

    await turnOn();

    expect(button).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(within(rowOf('Testes integrados')).getByText('Testes integrados'));
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    fireEvent.click(button);
    expect(screen.queryByText(editText.help)).not.toBeInTheDocument();
    fireEvent.click(within(rowOf('Testes integrados')).getByText('Testes integrados'));
    expect(await screen.findByRole('dialog', { name: text.details.label })).toBeVisible();
  });

  it('Modo de edição de quem só vê', async () => {
    permissions = { canEditSchedule: false };
    schedule = scheduleSeenBy(demoSchedule(), demoPeople.beatriz.id, true);
    await openList();

    expect(screen.queryByRole('button', { name: editText.button })).not.toBeInTheDocument();
  });

  it('Células da tarefa-resumo e do marco', async () => {
    schedule = demoScheduleWithSubtasks();
    await openList();
    await turnOn();

    fireEvent.click(cellOf('Testes integrados', 'duration'));
    fireEvent.keyDown(cellOf('Testes integrados', 'duration'), { key: 'Enter' });
    fireEvent.click(cellOf('Aceite da homologação', 'end'));
    fireEvent.keyDown(cellOf('Aceite da homologação', 'end'), { key: 'Enter' });

    expect(within(table()).queryByRole('textbox')).not.toBeInTheDocument();
    expect(await editCell('Testes integrados', 'name', text.columns.name)).toHaveValue(
      'Testes integrados',
    );
  });

  it('Progresso do responsável na tabela', async () => {
    permissions = { canEditSchedule: false };
    signedInAs('User');
    schedule = scheduleSeenBy(demoSchedule(), demoPeople.rafael.id);
    await openList();
    await turnOn();

    fireEvent.click(cellOf('Parametrizar regras de ICMS', 'name'));
    fireEvent.keyDown(cellOf('Parametrizar regras de ICMS', 'name'), { key: 'Enter' });
    expect(within(table()).queryByRole('textbox')).not.toBeInTheDocument();
    const progress = await editCell(
      'Parametrizar regras de ICMS',
      'progress',
      text.columns.progress,
    );
    expect(progress).toHaveValue('50');
    await typeIn(progress, '60');
    fireEvent.keyDown(progress, { key: 'Tab' });

    await waitFor(() => {
      expect(sent).toEqual([
        { path: `progress/${itemNamed('Parametrizar regras de ICMS').id}`, body: { progress: 60 } },
      ]);
    });
  });

  it('Duração pela célula', async () => {
    await openList();
    await turnOn();
    const task = itemNamed('Integrar emissão de NF-e');

    const duration = await editCell('Integrar emissão de NF-e', 'duration', text.columns.duration);
    expect(duration).toHaveValue('8');
    await typeIn(duration, '10');
    fireEvent.keyDown(duration, { key: 'Enter' });

    expect(
      await screen.findByText(text.messages.itemSaved.Task.replace('{{name}}', task.name)),
    ).toBeVisible();
    expect(sent).toEqual([
      {
        path: `items/${task.id}`,
        body: {
          phaseId: task.phaseId,
          name: task.name,
          start: task.start,
          duration: 10,
          ownerId: null,
          ownedByClient: false,
          progress: null,
          predecessorIds: task.predecessorIds,
          parentId: null,
          deadline: null,
        },
      },
    ]);
  });

  it('Datas pela célula', async () => {
    await openList();
    await turnOn();
    const task = itemNamed('Integrar emissão de NF-e');

    const start = await editCell('Integrar emissão de NF-e', 'start', text.columns.start);
    await typeIn(start, '21102026', '21/10/2026');
    fireEvent.keyDown(start, { key: 'Enter' });
    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    expect(
      await screen.findByText(
        `${text.messages.itemSaved.Task.replace('{{name}}', task.name)} 2 itens foram empurrados.`,
      ),
    ).toBeVisible();
    const end = await editCell('Integrar emissão de NF-e', 'end', text.columns.end);
    await typeIn(end, '30/10/2026');
    fireEvent.keyDown(end, { key: 'Enter' });

    await waitFor(() => {
      expect(sent).toEqual([
        { path: `dates/${task.id}`, body: { start: '2026-10-21', end: null } },
        { path: `dates/${task.id}`, body: { start: task.start, end: '2026-10-30' } },
      ]);
    });
  });

  it('Dependências pela célula', async () => {
    await openList();
    await turnOn();
    const task = itemNamed('Integrar emissão de NF-e');
    const cell = cellOf('Integrar emissão de NF-e', 'dependsOn');

    fireEvent.click(cell);
    fireEvent.keyDown(cell, { key: 'Enter' });
    const field = await screen.findByRole('combobox', { name: text.columns.dependsOn });
    fireEvent.mouseDown(field);
    fireEvent.click(await screen.findByRole('option', { name: 'Mapear processos fiscais' }));
    // Esc closes the list, and stays with the select; Tab saves the cell.
    fireEvent.keyDown(field, { key: 'Escape' });
    expect(field).toBeInTheDocument();
    await waitFor(() => {
      expect(field).toHaveAttribute('aria-expanded', 'false');
    });
    fireEvent.keyDown(field, { key: 'Tab' });

    await waitFor(() => {
      expect(sent).toEqual([
        {
          path: `items/${task.id}`,
          body: expect.objectContaining({
            predecessorIds: [itemNamed('Mapear processos fiscais').id],
          }) as unknown,
        },
      ]);
    });
  });

  it('Progresso pela célula', async () => {
    await openList();
    await turnOn();
    const task = itemNamed('Integrar emissão de NF-e');

    const progress = await editCell('Integrar emissão de NF-e', 'progress', text.columns.progress);
    await typeIn(progress, '60');
    fireEvent.keyDown(progress, { key: 'Tab' });

    expect(
      await screen.findByText(
        text.messages.progressUpdated.replace('{{name}}', task.name).replace('{{progress}}', '60'),
      ),
    ).toBeVisible();
    expect(sent).toEqual([{ path: `progress/${task.id}`, body: { progress: 60 } }]);
  });

  it('Duração inválida na célula', async () => {
    await openList();
    await turnOn();

    const duration = await editCell('Integrar emissão de NF-e', 'duration', text.columns.duration);
    await typeIn(duration, '0');
    fireEvent.keyDown(duration, { key: 'Enter' });

    await waitFor(() => {
      expect(duration).toHaveAccessibleDescription(text.errors.durationInvalid);
    });
    expect(duration).toBeInTheDocument();
    expect(duration).toHaveAttribute('aria-invalid', 'true');
    expect(sent).toEqual([]);
  });

  it('Dependência circular pela célula', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () =>
        problem(409, projectProblems.scheduleCycle),
      ),
    );
    await openList();
    await turnOn();

    const name = await editCell('Integrar emissão de NF-e', 'name', text.columns.name);
    await typeIn(name, 'Integrar a NF-e');
    fireEvent.keyDown(name, { key: 'Enter' });

    expect(await screen.findByText(text.errors.cycle)).toBeVisible();
    await waitFor(() => {
      expect(within(table()).queryByRole('textbox')).not.toBeInTheDocument();
    });
    expect(rowOf('Integrar emissão de NF-e')).toBeInTheDocument();
  });
});
