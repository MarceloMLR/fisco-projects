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
  demoScheduleWithBaseline,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
const requests: string[] = [];

/** The schedule as the API sends it after the manager saves the baseline on 16/10/2026: every item on it. */
function savedToday(current: Schedule): Schedule {
  return {
    ...current,
    baseline: { savedOn: '2026-10-16', start: '2026-10-05', end: '2026-11-03', deviation: 0 },
    phases: current.phases.map((phase) => ({
      ...phase,
      items: phase.items.map((item) => ({
        ...item,
        baselineStart: item.start,
        baselineEnd: item.end,
        baselineDeviation: 0,
      })),
    })),
  };
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
    http.put('/api/projects/:id/schedule/baseline', () => {
      requests.push('PUT baseline');
      schedule = savedToday(schedule);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** Opens the Lista of the tab, which has the summary beside it. */
async function openSchedule() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  await screen.findByRole('grid', { name: text.schedule.table });
  await waitFor(() => {
    expect(nameCell('Parametrizar regras de ICMS')).toBeInTheDocument();
  });
}

/** The name of an item in the Lista, before its name in "Depende de" of the items after it. */
function nameCell(name: string) {
  const [first] = within(screen.getByRole('grid', { name: text.schedule.table })).getAllByText(
    name,
  );
  if (first === undefined) {
    throw new Error(`No ${name}.`);
  }
  return first;
}

async function chooseInMore(name: string) {
  fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
  const menu = await screen.findByRole('menu', { name: text.baseline.more });
  fireEvent.click(within(menu).getByRole('menuitem', { name }));
}

// Spec: cronograma, requirement "Linha de base pela tela".
describe('Linha de base pela tela', () => {
  it('Salvar linha de base pela tela', async () => {
    await openSchedule();
    expect(screen.queryByText(/Linha de base de/)).not.toBeInTheDocument();

    await chooseInMore(text.baseline.save);

    expect(await screen.findByText(text.baseline.messages.saved)).toBeInTheDocument();
    expect(
      await screen.findByText(text.baseline.summary.replace('{{day}}', '16/10/2026')),
    ).toBeInTheDocument();
    expect(screen.getByText(text.baseline.projectEnd.same)).toBeInTheDocument();
    // Saved right away, without asking.
    expect(requests).toEqual(['PUT baseline']);
  });

  it('Atualizar linha de base pela tela', async () => {
    schedule = demoScheduleWithBaseline();
    await openSchedule();
    // The end of the project slipped two business days since the baseline, in amber.
    expect(screen.getByText(text.baseline.summary.replace('{{day}}', '09/10/2026'))).toBeVisible();
    const slipped = screen.getByText(
      text.baseline.projectEnd.later_other.replace('{{count}}', '2'),
    );
    expect(slipped.closest('[data-state]')).toHaveAttribute('data-state', 'soon');

    // Back in the confirmation changes nothing.
    await chooseInMore(text.baseline.update);
    let dialog = await screen.findByRole('alertdialog', { name: text.baseline.confirm.title });
    expect(dialog).toHaveTextContent(text.baseline.confirm.text.replace('{{day}}', '09/10/2026'));
    fireEvent.click(within(dialog).getByRole('button', { name: text.baseline.confirm.back }));
    await waitFor(() => {
      expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    });
    expect(requests).toEqual([]);

    await chooseInMore(text.baseline.update);
    dialog = await screen.findByRole('alertdialog', { name: text.baseline.confirm.title });
    fireEvent.click(within(dialog).getByRole('button', { name: text.baseline.update }));

    expect(await screen.findByText(text.baseline.messages.updated)).toBeInTheDocument();
    expect(
      await screen.findByText(text.baseline.summary.replace('{{day}}', '16/10/2026')),
    ).toBeInTheDocument();
    expect(requests).toEqual(['PUT baseline']);
  });

  it('Linha de base no detalhe', async () => {
    permissions = { canEditSchedule: false };
    signedInAs('User');
    schedule = demoScheduleWithBaseline();
    const invoices = schedule.phases[1]?.items[1];
    if (invoices === undefined) {
      throw new Error('No Integrar emissão de NF-e.');
    }
    // As if it had been included after the baseline.
    Object.assign(invoices, { baselineStart: null, baselineEnd: null, baselineDeviation: null });
    await openSchedule();

    fireEvent.click(nameCell('Parametrizar regras de ICMS'));
    let drawer = await screen.findByRole('dialog', { name: text.schedule.details.label });
    const section = within(drawer).getByRole('region', { name: text.baseline.details.title });
    expect(within(section).getByText('13/10/2026')).toBeVisible();
    expect(within(section).getByText('20/10/2026')).toBeVisible();
    expect(
      within(section).getByText(text.baseline.item.later_other.replace('{{count}}', '2')),
    ).toBeVisible();
    fireEvent.keyDown(drawer, { key: 'Escape' });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    fireEvent.click(nameCell('Integrar emissão de NF-e'));
    drawer = await screen.findByRole('dialog', { name: text.schedule.details.label });
    expect(
      within(within(drawer).getByRole('region', { name: text.baseline.details.title })).getByText(
        text.baseline.details.notIn.Task,
      ),
    ).toBeVisible();
  });

  it('Sem editar, sem salvar a linha de base', async () => {
    permissions = { canEditSchedule: false };
    signedInAs('User');
    schedule = demoScheduleWithBaseline();
    await openSchedule();

    // The summary shows the baseline to everyone in the project; only who edits saves it.
    expect(screen.getByText(text.baseline.summary.replace('{{day}}', '09/10/2026'))).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
    const menu = await screen.findByRole('menu', { name: text.baseline.more });
    expect(
      within(menu).queryByRole('menuitem', { name: text.baseline.save }),
    ).not.toBeInTheDocument();
    expect(
      within(menu).queryByRole('menuitem', { name: text.baseline.update }),
    ).not.toBeInTheDocument();
  });
});
