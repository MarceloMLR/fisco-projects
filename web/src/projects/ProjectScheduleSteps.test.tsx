import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Project, projectProblems, type Schedule } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  demoSchedule,
  problem,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;
const added = 'Configurar plano de contas';
const step = { action: 'TaskAdded', name: added } as const;
const what = text.steps.what.TaskAdded.replace('{{name}}', added);
const capitalized = what.charAt(0).toUpperCase() + what.slice(1);

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
const requests: string[] = [];

/** The schedule of ERP-FIN with the task the manager included last, which she may undo. */
function withTheIncludedTask(): Schedule {
  const current = demoSchedule();
  const configuration = current.phases[1];
  const first = configuration?.items[0];
  if (configuration === undefined || first === undefined) {
    throw new Error('No Configuração.');
  }
  configuration.items = [
    ...configuration.items,
    {
      ...first,
      id: '0199f5a2-7c1e-7d40-9a1b-600000000099',
      name: added,
      start: '2026-10-19',
      end: '2026-10-21',
      duration: 3,
      progress: 0,
      predecessorIds: [],
      critical: false,
    },
  ];
  return { ...current, undo: step };
}

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = withTheIncludedTask();
  permissions = { canEditSchedule: true };
  requests.length = 0;
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
    http.post('/api/projects/:id/schedule/undo', () => {
      requests.push('POST undo');
      schedule = { ...demoSchedule(), undo: null, redo: step };
      return HttpResponse.json(step);
    }),
    http.post('/api/projects/:id/schedule/redo', () => {
      requests.push('POST redo');
      schedule = withTheIncludedTask();
      return HttpResponse.json(step);
    }),
  );
});

async function openSchedule() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  const grid = await screen.findByRole('grid', { name: text.schedule.table });
  await waitFor(() => {
    expect(within(grid).getAllByText('Parametrizar regras de ICMS').length).toBeGreaterThan(0);
  });
  return grid;
}

// Spec: cronograma, requirement "Desfazer pela tela".
describe('Desfazer pela tela', () => {
  it('Desfazer pela tela', async () => {
    const grid = await openSchedule();
    const undo = screen.getByRole('button', { name: text.steps.undo });
    // The tip says what it undoes.
    expect(undo.parentElement).toHaveAttribute(
      'title',
      text.steps.undoWhat.replace('{{what}}', what),
    );

    fireEvent.click(undo);

    expect(
      await screen.findByText(text.steps.messages.undone.replace('{{what}}', capitalized)),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(within(grid).queryByText(added)).not.toBeInTheDocument();
    });
    // Nothing else to undo: the button is off.
    await waitFor(() => {
      expect(screen.getByRole('button', { name: text.steps.undo })).toBeDisabled();
    });
    expect(requests).toEqual(['POST undo']);
  });

  it('Refazer pela tela', async () => {
    schedule = { ...demoSchedule(), redo: step };
    const grid = await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
    const menu = await screen.findByRole('menu', { name: text.baseline.more });
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.steps.redo }));

    expect(
      await screen.findByText(text.steps.messages.redone.replace('{{what}}', capitalized)),
    ).toBeInTheDocument();
    expect(await within(grid).findByText(added)).toBeInTheDocument();
    expect(requests).toEqual(['POST redo']);
  });

  it('Desfazer bloqueado pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/schedule/undo', () => {
        requests.push('POST undo');
        schedule = { ...schedule, undo: null };
        return problem(409, projectProblems.scheduleUndoBlocked);
      }),
    );
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.steps.undo }));

    expect(
      await screen.findByText(text.steps.messages.undoBlocked.replace('{{what}}', what)),
    ).toBeInTheDocument();
    expect(requests).toEqual(['POST undo']);
  });

  it('Sem editar, sem desfazer', async () => {
    permissions = { canEditSchedule: false };
    signedInAs('User');
    schedule = { ...schedule, undo: null };
    await openSchedule();

    expect(screen.queryByRole('button', { name: text.steps.undo })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.baseline.more })).not.toBeInTheDocument();
  });
});
