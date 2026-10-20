import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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

afterEach(() => {
  vi.restoreAllMocks();
});

async function openMore() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  const grid = await screen.findByRole('grid', { name: text.schedule.table });
  await waitFor(() => {
    expect(within(grid).getAllByText('Parametrizar regras de ICMS').length).toBeGreaterThan(0);
  });
  fireEvent.click(screen.getByRole('button', { name: text.baseline.more }));
  return screen.findByRole('menu', { name: text.baseline.more });
}

// Spec: cronograma, requirements "Exportar em PDF pela impressão" and "Exportar para o MS Project".
describe('Exportar o cronograma pela tela', () => {
  it('Escolher o período e a escala', async () => {
    const opened = vi.spyOn(window, 'open').mockReturnValue(null);
    const menu = await openMore();

    fireEvent.click(within(menu).getByRole('menuitem', { name: text.export.pdf }));
    const dialog = await screen.findByRole('dialog', { name: text.export.dialog.title });
    // From the start to the end of the schedule, in the scale of the tab, at first.
    const from = within(dialog).getByLabelText(text.export.dialog.from);
    expect(from).toHaveValue('2026-10-05');
    expect(within(dialog).getByLabelText(text.export.dialog.to)).toHaveValue('2026-11-03');
    fireEvent.change(from, { target: { value: '2026-10-12' } });
    fireEvent.click(within(dialog).getByRole('radio', { name: text.timeline.scales.weeks }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.export.dialog.open }));

    expect(opened).toHaveBeenCalledWith(
      `/projects/${project.id}/schedule/print?from=2026-10-12&to=2026-11-03&scale=weeks&baseline=1&critical=1`,
      '_blank',
      'noopener',
    );
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: text.export.dialog.title }),
      ).not.toBeInTheDocument();
    });
  });

  it('Exportar sem editar', async () => {
    permissions = { canEditSchedule: false };
    signedInAs('User');

    const menu = await openMore();

    expect(
      within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([text.export.pdf, text.export.xml]);
    // The XML is a file the server makes, downloaded by the link.
    expect(within(menu).getByRole('menuitem', { name: text.export.xml })).toHaveAttribute(
      'href',
      `/api/projects/${project.id}/schedule/ms-project`,
    );
  });

  it('Salvar como modelo pela tela', async () => {
    const bodies: unknown[] = [];
    server.use(
      http.post('/api/projects/:id/schedule/template', async ({ request }) => {
        bodies.push(await request.json());
        return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-700000000009' }, { status: 201 });
      }),
    );
    const menu = await openMore();

    fireEvent.click(within(menu).getByRole('menuitem', { name: text.templates.save }));
    const dialog = await screen.findByRole('dialog', { name: text.templates.saveDialog.title });
    const name = within(dialog).getByRole('textbox', { name: text.templates.saveDialog.name });
    // The name of the project comes first.
    expect(name).toHaveValue(project.name);
    fireEvent.change(name, { target: { value: 'Implantação de ERP' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.templates.saveDialog.save }));

    expect(
      await screen.findByText(
        text.templates.messages.saved.replace('{{name}}', 'Implantação de ERP'),
      ),
    ).toBeVisible();
    expect(bodies).toEqual([{ name: 'Implantação de ERP', description: null }]);
  });
});
