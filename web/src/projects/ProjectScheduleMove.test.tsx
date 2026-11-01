import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Project, Schedule } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  demoSchedule,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
const moves: number[] = [];

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
  permissions = { canEditSchedule: true };
  moves.length = 0;
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
    http.post('/api/projects/:id/schedule/move', async ({ request }) => {
      const { days } = (await request.json()) as { days: number };
      moves.push(days);
      // The API keeps the move for the undo, with the signed days as its name (ADR-0140).
      schedule = {
        ...schedule,
        undo: { action: 'ScheduleMoved', name: days > 0 ? `+${String(days)}` : String(days) },
      };
      return HttpResponse.json({ moved: 7 });
    }),
  );
});

async function openSchedule() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  await screen.findByRole('grid', { name: text.schedule.table });
}

async function openMove() {
  fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
  const menu = await screen.findByRole('menu', { name: text.baseline.more });
  fireEvent.click(within(menu).getByRole('menuitem', { name: text.move.item }));
  return screen.findByRole('dialog', { name: text.move.title });
}

// Spec: cronograma, requirement "Mover o cronograma pela tela" (ADR-0140).
describe('Mover o cronograma pela tela', () => {
  it('Mover pela tela', async () => {
    await openSchedule();

    const dialog = await openMove();
    // Six open tasks and milestones in the demonstration schedule: the done ones stay.
    expect(
      within(dialog).getByText(text.move.whatMoves_other.replace('{{count}}', '6')),
    ).toBeVisible();
    expect(within(dialog).getByRole('radio', { name: text.move.later })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    fireEvent.change(within(dialog).getByLabelText(text.move.days), { target: { value: '5' } });
    expect(
      within(dialog).getByText(
        text.move.summary.later_other
          .replace('{{count}}', '6')
          .replace('{{days}}', text.move.businessDays_other.replace('{{count}}', '5')),
      ),
    ).toBeVisible();
    fireEvent.click(within(dialog).getByRole('button', { name: text.move.confirm }));

    expect(
      await screen.findByText(text.move.messages.moved_other.replace('{{count}}', '7')),
    ).toBeVisible();
    expect(moves).toEqual([5]);
    const undo = text.steps.undoWhat.replace(
      '{{what}}',
      text.steps.what.ScheduleMoved.replace('{{name}}', '+5'),
    );
    await waitFor(() => {
      expect(screen.getByTitle(undo)).toBeInTheDocument();
    });
  });

  it('Mover para antes pela tela', async () => {
    await openSchedule();

    const dialog = await openMove();
    fireEvent.click(within(dialog).getByRole('radio', { name: text.move.earlier }));
    fireEvent.change(within(dialog).getByLabelText(text.move.days), { target: { value: '3' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.move.confirm }));

    await waitFor(() => {
      expect(moves).toEqual([-3]);
    });
  });

  it('Dias inválidos pela tela', async () => {
    await openSchedule();

    const dialog = await openMove();
    const field = within(dialog).getByLabelText(text.move.days);
    for (const typed of ['0', '1000', '']) {
      fireEvent.change(field, { target: { value: typed } });
      fireEvent.click(within(dialog).getByRole('button', { name: text.move.confirm }));
      expect(await within(dialog).findByText(text.move.errors.daysInvalid)).toBeVisible();
    }
    expect(moves).toEqual([]);
  });

  it('Sem mover para quem não edita', async () => {
    permissions = { canEditSchedule: false };
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
    const menu = await screen.findByRole('menu', { name: text.baseline.more });

    expect(within(menu).queryByRole('menuitem', { name: text.move.item })).toBeNull();
  });
});
