import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Project, ProjectStatus } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  clientChoicesOf,
  demoClients,
  type DemoProject,
  demoProjects,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;
const erpFin = '0199f5a2-7c1e-7d40-9a1b-400000000001';

let projects: DemoProject[];
const requests: string[] = [];

/** What the person in the session may do in the projects of the test, as the API answers it. */
let permissions: Project['permissions'];

const closed = (status: ProjectStatus) => status === 'Completed' || status === 'Cancelled';

beforeEach(() => {
  projects = demoProjects();
  requests.length = 0;
  permissions = { canEdit: true, canChangeStatus: true };
  const current = projects;
  const find = (id: unknown) => current.find((project) => project.id === id);
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(current, new URL(request.url))),
    ),
    http.get('/api/clients/choices', () => HttpResponse.json(clientChoicesOf(demoClients()))),
    http.get('/api/projects/:id', ({ params }) => {
      const project = find(params.id);
      if (project === undefined) {
        return new HttpResponse(null, { status: 404 });
      }
      return HttpResponse.json({
        ...project,
        permissions: { ...permissions, canEdit: permissions.canEdit && !closed(project.status) },
      });
    }),
    http.put('/api/projects/:id', async ({ request, params }) => {
      const body = (await request.json()) as Pick<Project, 'code' | 'name' | 'description'> & {
        clientId: string | null;
      };
      requests.push(`PUT ${body.code} ${body.name}`);
      const project = find(params.id);
      if (project !== undefined) {
        Object.assign(project, { code: body.code, name: body.name, description: body.description });
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/projects/:id/status', async ({ request, params }) => {
      const { status } = (await request.json()) as { status: ProjectStatus };
      requests.push(`PUT status ${status}`);
      const project = find(params.id);
      if (project !== undefined) {
        project.status = status;
      }
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openProject(id = erpFin, width?: number) {
  const view = await renderApp(`/projects/${id}`, width);
  await screen.findByRole('heading', { level: 1 });
  return view;
}

async function openActions() {
  fireEvent.click(screen.getByRole('button', { name: text.actions.button }));
  return screen.findByRole('menu', { name: text.actions.button });
}

// Spec: projetos, requirement "Cabeçalho do projeto".
describe('Cabeçalho do projeto', () => {
  it('Cabeçalho do projeto', async () => {
    await openProject();

    expect(
      screen.getByRole('heading', { level: 1, name: 'Implantação do ERP financeiro' }),
    ).toBeVisible();
    expect(screen.getByText(text.statuses.InProgress)).toBeVisible();
    const meta = screen.getByTestId('project-meta');
    expect(meta).toHaveTextContent('ERP-FIN');
    expect(meta).toHaveTextContent('Alfa Distribuidora');
    expect(meta).toHaveTextContent(`${text.header.manager} Marina Costa`);
    expect(meta).toHaveTextContent(`${text.header.techLead} Rafael Souza`);
    expect(meta).toHaveTextContent(text.noSchedule);
    expect(screen.getByTestId('project-logo').querySelector('img')).toHaveAttribute(
      'src',
      '/api/clients/0199f5a2-7c1e-7d40-9a1b-300000000001/logo?v=1',
    );

    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(
      within(trail)
        .getAllByRole('listitem')
        .map((level) => level.textContent),
    ).toEqual([ptBR.pages.projects.title, 'ERP-FIN', ptBR.pages.project.title]);
    expect(within(trail).getByRole('link', { name: 'ERP-FIN' })).toHaveAttribute(
      'href',
      `/projects/${erpFin}`,
    );
    expect(screen.getByRole('tab', { name: ptBR.pages.project.title })).toHaveAttribute(
      'aria-selected',
      'true',
    );
  });

  it('Visão geral com a descrição e a equipe', async () => {
    await openProject();

    const description = screen.getByRole('region', { name: text.overview.description });
    expect(description).toHaveTextContent(
      'Implantação do ERP na matriz, com o financeiro e a contabilidade.',
    );
    const team = screen.getByRole('region', { name: text.overview.team });
    expect(
      within(team)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual([
      expect.stringContaining(`${text.overview.teams.management}Marina Costa, Rafael Souza`),
      expect.stringContaining(`${text.overview.teams.functional}${text.overview.people_zero}`),
      expect.stringContaining(
        `${text.overview.teams.development}${text.overview.people_one.replace('{{count}}', '1')}`,
      ),
    ]);
  });

  it('Projeto interno sem descrição', async () => {
    await openProject('0199f5a2-7c1e-7d40-9a1b-400000000003');

    expect(screen.getByTestId('project-meta')).toHaveTextContent(text.internal);
    expect(screen.getByTestId('project-logo')).toHaveTextContent('PF');
    expect(screen.getByRole('region', { name: text.overview.description })).toHaveTextContent(
      text.overview.noDescription,
    );
  });

  it('Projeto fora do alcance', async () => {
    await renderApp('/projects/0199f5a2-7c1e-7d40-9a1b-400000000099');

    expect(await screen.findByText(text.notFound)).toBeVisible();
  });

  it('Página do projeto no celular', async () => {
    await openProject(erpFin, phoneWidth);

    expect(screen.getByRole('button', { name: text.actions.button })).toBeVisible();
    expect(screen.getByRole('region', { name: text.overview.team })).toBeVisible();
  });
});

// Spec: projetos, requirement "Ações do projeto".
describe('Ações do projeto', () => {
  it('Editar dados pela tela', async () => {
    // The tech lead changes the data, but not the status (ADR-0043).
    permissions = { canEdit: true, canChangeStatus: false };
    await openProject();

    const menu = await openActions();
    expect(
      within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([text.actions.edit]);
    expect(within(menu).queryByRole('menuitemradio')).not.toBeInTheDocument();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.form.editTitle });
    expect(within(dialog).getByRole('textbox', { name: text.form.code })).toHaveValue('ERP-FIN');
    fireEvent.change(within(dialog).getByRole('textbox', { name: text.form.name }), {
      target: { value: 'Implantação do ERP financeiro e fiscal' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: 'Implantação do ERP financeiro e fiscal',
      }),
    ).toBeVisible();
    expect(
      screen.getByText(
        text.messages.changed.replace('{{name}}', 'Implantação do ERP financeiro e fiscal'),
      ),
    ).toBeInTheDocument();
    expect(requests).toEqual(['PUT ERP-FIN Implantação do ERP financeiro e fiscal']);
  });

  it('Pausar e retomar pela tela', async () => {
    await openProject();

    let menu = await openActions();
    expect(
      within(menu).getByRole('menuitemradio', { name: text.statuses.InProgress }),
    ).toHaveAttribute('aria-checked', 'true');
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.statuses.Paused }));

    await waitFor(() => {
      expect(screen.getByText(text.statuses.Paused)).toBeVisible();
    });
    menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.statuses.InProgress }));
    await waitFor(() => {
      expect(screen.getByText(text.statuses.InProgress)).toBeVisible();
    });
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(requests).toEqual(['PUT status Paused', 'PUT status InProgress']);
  });

  it('Concluir projeto pela tela', async () => {
    await openProject();

    let menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    let confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });
    expect(confirm).toHaveAccessibleDescription(
      text.confirm.completeText.replace('{{name}}', 'Implantação do ERP financeiro'),
    );
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.back }));
    await waitFor(() => {
      expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    });
    expect(requests).toEqual([]);

    menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.completeAction }));

    expect(await screen.findByText(text.closed.Completed)).toBeVisible();
    expect(screen.getByText(text.statuses.Completed)).toBeVisible();
    await waitFor(() => {
      expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    });
    menu = await openActions();
    expect(
      within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([text.actions.reopen]);
    expect(requests).toEqual(['PUT status Completed']);
  });

  it('Cancelar projeto pela tela', async () => {
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.cancel }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.cancelTitle });
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.cancelAction }));

    expect(await screen.findByText(text.closed.Cancelled)).toBeVisible();
    expect(requests).toEqual(['PUT status Cancelled']);
  });

  it('Reabrir projeto pela tela', async () => {
    await openProject('0199f5a2-7c1e-7d40-9a1b-400000000004');

    expect(screen.getByText(text.closed.Completed)).toBeVisible();
    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.reopen }));

    await waitFor(() => {
      expect(screen.getByText(text.statuses.InProgress)).toBeVisible();
    });
    expect(screen.queryByText(text.closed.Completed)).not.toBeInTheDocument();
    expect(requests).toEqual(['PUT status InProgress']);
  });

  it('Leitor sem ações', async () => {
    permissions = { canEdit: false, canChangeStatus: false };
    await openProject();

    expect(screen.queryByRole('button', { name: text.actions.button })).not.toBeInTheDocument();
  });
});
