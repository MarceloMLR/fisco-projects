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
  permissions = {
    canEdit: true,
    canChangeStatus: true,
    canManagePeople: true,
    canManageManagers: true,
    canEditSchedule: true,
    canDelete: false,
    canChooseFolder: false,
  };
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
        permissions: {
          ...permissions,
          canEdit: permissions.canEdit && !closed(project.status),
          canManagePeople: permissions.canManagePeople && !closed(project.status),
          canManageManagers: permissions.canManageManagers && !closed(project.status),
        },
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
    expect(meta).toHaveTextContent('05/10 – 03/11/2026');
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

  it('Abas sem barra de rolagem', async () => {
    await openProject();

    // The mark of the chosen tab sits on the line of the header, and nothing is left to scroll up and down; only
    // sideways, when the tabs do not fit.
    const tabs = screen.getByRole('tablist', { name: text.header.tabs });
    expect(tabs).toHaveStyle({ overflowX: 'auto', overflowY: 'hidden', marginBottom: '-1px' });
  });

  it('Datas do cronograma no cabeçalho', async () => {
    const erp = projects[0];
    if (erp === undefined) {
      throw new Error('No ERP-FIN.');
    }
    erp.scheduleEnd = '2026-11-13';
    await openProject();

    const meta = screen.getByTestId('project-meta');
    expect(meta).toHaveTextContent('05/10 – 13/11/2026');
    expect(meta).not.toHaveTextContent(text.noSchedule);
  });

  it('Visão geral com o resumo do projeto', async () => {
    await openProject();

    expect(await screen.findByRole('region', { name: text.overview.summary })).toBeVisible();
    expect(screen.getByRole('region', { name: text.overview.team })).toBeVisible();
    expect(
      screen.queryByText('Implantação do ERP na matriz, com o financeiro e a contabilidade.'),
    ).not.toBeInTheDocument();
  });

  it('Projeto interno sem cliente', async () => {
    await openProject('0199f5a2-7c1e-7d40-9a1b-400000000003');

    expect(screen.getByTestId('project-meta')).toHaveTextContent(text.internal);
    expect(screen.getByTestId('project-meta')).toHaveTextContent(text.noSchedule);
    expect(screen.getByTestId('project-logo')).toHaveTextContent('PF');
  });

  it('Projeto fora do alcance', async () => {
    await renderApp('/projects/0199f5a2-7c1e-7d40-9a1b-400000000099');

    expect(await screen.findByText(text.notFound)).toBeVisible();
  });

  it('Página do projeto no celular', async () => {
    await openProject(erpFin, phoneWidth);

    expect(screen.getByRole('button', { name: text.actions.button })).toBeVisible();
    expect(await screen.findByRole('region', { name: text.overview.summary })).toBeVisible();
  });
});

// Spec: projetos, requirement "Ações do projeto".
describe('Ações do projeto', () => {
  it('Editar dados pela tela', async () => {
    // The tech lead changes the data, but not the status (ADR-0043).
    permissions = {
      canEdit: true,
      canChangeStatus: false,
      canManagePeople: true,
      canManageManagers: false,
      canEditSchedule: false,
      canDelete: false,
      canChooseFolder: false,
    };
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
    permissions = {
      canEdit: false,
      canChangeStatus: false,
      canManagePeople: false,
      canManageManagers: false,
      canEditSchedule: false,
      canDelete: false,
      canChooseFolder: false,
    };
    await openProject();

    expect(screen.queryByRole('button', { name: text.actions.button })).not.toBeInTheDocument();
  });
});

// Spec: projetos, requirement "Encerramento pela tela".
describe('Encerramento pela tela', () => {
  const summary = (openTickets: number, incompleteTasks: number, pendingAcceptances = 0) => {
    server.use(
      http.get('/api/projects/:id/closing', () =>
        HttpResponse.json({ openTickets, incompleteTasks, pendingAcceptances }),
      ),
    );
  };

  it('Conclusão impedida pela tela', async () => {
    summary(2, 0);
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });

    expect(
      await within(confirm).findByText(/O projeto tem 2 chamados não fechados/),
    ).toBeInTheDocument();
    expect(within(confirm).getByRole('link', { name: text.confirm.seeTickets })).toHaveAttribute(
      'href',
      `/projects/${erpFin}/tickets`,
    );
    expect(
      within(confirm).getByRole('button', { name: text.confirm.completeAction }),
    ).toBeDisabled();
    expect(requests).toEqual([]);
  });

  it('Conclusão recusada pela API', async () => {
    server.use(
      http.put('/api/projects/:id/status', () =>
        HttpResponse.json(
          {
            status: 409,
            title: 'Open tickets',
            type: 'urn:fiscoprojects:problem:project-has-open-tickets',
            openTickets: 1,
          },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openProject();
    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });

    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.completeAction }));

    expect(
      await within(confirm).findByText(/O projeto tem 1 chamado não fechado/),
    ).toBeInTheDocument();
    expect(screen.queryByText(text.closed.Completed)).not.toBeInTheDocument();
  });

  it('Aviso de tarefas incompletas', async () => {
    summary(0, 3);
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });
    expect(
      await within(confirm).findByText(
        '3 tarefas estão abaixo de 100%. O projeto pode ser concluído assim mesmo.',
      ),
    ).toBeInTheDocument();
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.completeAction }));

    expect(await screen.findByText(text.closed.Completed)).toBeVisible();
    expect(requests).toEqual(['PUT status Completed']);
  });

  it('Aviso de aceites pendentes', async () => {
    summary(0, 0, 1);
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.complete }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.completeTitle });
    expect(
      await within(confirm).findByText(
        /1 pedido de aceite espera a resposta\. O projeto pode ser concluído assim mesmo\./,
      ),
    ).toBeInTheDocument();
    expect(
      within(confirm).getByRole('link', { name: text.confirm.seeAcceptances }),
    ).toHaveAttribute('href', `/projects/${erpFin}/acceptances`);
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.completeAction }));

    expect(await screen.findByText(text.closed.Completed)).toBeVisible();
    expect(requests).toEqual(['PUT status Completed']);
  });

  it('Cancelamento pela tela', async () => {
    summary(2, 1);
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.cancel }));
    const confirm = await screen.findByRole('alertdialog', { name: text.confirm.cancelTitle });
    expect(
      await within(confirm).findByText(
        'Os 2 chamados abertos serão fechados, com o motivo "projeto cancelado" na conversa.',
      ),
    ).toBeInTheDocument();
    // The tasks below 100% do not matter to a cancellation.
    expect(within(confirm).queryByText(/abaixo de 100%/)).not.toBeInTheDocument();
    fireEvent.click(within(confirm).getByRole('button', { name: text.confirm.cancelAction }));

    expect(await screen.findByText(text.closed.Cancelled)).toBeVisible();
    expect(requests).toEqual(['PUT status Cancelled']);
  });
});

// Spec: projetos, requirement "Exclusão pela tela" (ADR-0041, ADR-0111).
describe('Exclusão pela tela', () => {
  const deletion = text.deletion;

  function asAdministrator() {
    signedInAs('Administrator');
    permissions = { ...permissions, canDelete: true };
    server.use(
      http.get('/api/projects/:id/deletion', ({ params }) =>
        HttpResponse.json({
          id: params.id,
          code: 'ERP-FIN',
          name: 'Implantação do ERP financeiro',
          tickets: 2,
          documents: 1,
          links: 0,
          credentials: 3,
          acceptanceTerms: 1,
          scheduleItems: 12,
          members: 5,
          timeEntries: 40,
        }),
      ),
      http.post('/api/projects/:id/deletion', async ({ request, params }) => {
        const { code } = (await request.json()) as { code: string };
        requests.push(`POST deletion ${code}`);
        projects.splice(
          projects.findIndex((project) => project.id === params.id),
          1,
        );
        return new HttpResponse(null, { status: 204 });
      }),
    );
  }

  it('Excluir pela tela', async () => {
    asAdministrator();
    await openProject();

    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.delete }));
    const dialog = await screen.findByRole('alertdialog', {
      name: deletion.title.replace('{{code}}', 'ERP-FIN'),
    });
    const list = await within(dialog).findByRole('list');
    expect(
      within(list)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual([
      deletion.counts.tickets_other.replace('{{count}}', '2'),
      deletion.counts.documents_one.replace('{{count}}', '1'),
      deletion.counts.credentials_other.replace('{{count}}', '3'),
      deletion.counts.acceptanceTerms_one.replace('{{count}}', '1'),
      deletion.counts.scheduleItems_other.replace('{{count}}', '12'),
      deletion.counts.members_other.replace('{{count}}', '5'),
      deletion.counts.timeEntries_other.replace('{{count}}', '40'),
    ]);
    expect(dialog).toHaveTextContent(deletion.forGood);
    const confirm = within(dialog).getByRole('button', { name: deletion.confirm });
    const code = within(dialog).getByRole('textbox', {
      name: deletion.code.replace('{{code}}', 'ERP-FIN'),
    });
    expect(confirm).toBeDisabled();
    fireEvent.change(code, { target: { value: 'ERP' } });
    expect(confirm).toBeDisabled();
    fireEvent.change(code, { target: { value: ' erp-fin ' } });
    expect(confirm).toBeEnabled();
    fireEvent.click(confirm);

    expect(
      await screen.findByText(text.messages.deleted.replace('{{code}}', 'ERP-FIN')),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(ptBR.pages.projects.title);
    expect(requests).toEqual(['POST deletion erp-fin']);
  });

  it('Excluir só para o administrador', async () => {
    await openProject();

    const menu = await openActions();

    expect(
      within(menu).queryByRole('menuitem', { name: text.actions.delete }),
    ).not.toBeInTheDocument();
  });
});

// Spec: projetos, requirement "Pasta do projeto no Editar dados" (ADR-0122).
describe('Pasta do projeto no Editar dados', () => {
  const folderText = text.folder;

  beforeEach(() => {
    permissions = { ...permissions, canChooseFolder: true };
  });

  async function openEdit() {
    await openProject();
    const menu = await openActions();
    fireEvent.click(within(menu).getByRole('menuitem', { name: text.actions.edit }));
    return screen.findByRole('dialog', { name: text.form.editTitle });
  }

  it('Pasta que não muda mais', async () => {
    server.use(
      http.get('/api/projects/:id/folder', () =>
        HttpResponse.json({
          name: 'ERP-FIN - Implantação do ERP financeiro',
          chosen: false,
          hasFiles: true,
          canChange: false,
        }),
      ),
    );
    const dialog = await openEdit();

    const field = await within(dialog).findByRole('textbox', { name: folderText.field });
    expect(field).toHaveValue('ERP-FIN - Implantação do ERP financeiro');
    expect(field).toHaveAccessibleDescription(folderText.locked);
    expect(
      within(dialog).queryByRole('button', { name: folderText.choose }),
    ).not.toBeInTheDocument();
  });

  it('Voltar à pasta do sistema pelo Editar dados', async () => {
    server.use(
      http.get('/api/projects/:id/folder', () =>
        HttpResponse.json({ name: 'Alfa', chosen: true, hasFiles: false, canChange: true }),
      ),
      http.put('/api/projects/:id/folder', async ({ request }) => {
        const { folderId } = (await request.json()) as { folderId: string | null };
        requests.push(`PUT folder ${String(folderId)}`);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const dialog = await openEdit();

    const field = await within(dialog).findByRole('textbox', { name: folderText.field });
    expect(field).toHaveValue('Alfa');
    fireEvent.click(within(dialog).getByRole('button', { name: folderText.useSystem }));
    expect(field).toHaveValue('ERP-FIN - Implantação do ERP financeiro');
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.form.editTitle })).not.toBeInTheDocument();
    });
    // The folder changes first, so a folder refused leaves the data as they were.
    expect(requests).toEqual(['PUT folder null', 'PUT ERP-FIN Implantação do ERP financeiro']);
  });

  it('Pasta sem ação para quem não escolhe', async () => {
    permissions = { ...permissions, canChooseFolder: false };
    const dialog = await openEdit();

    expect(within(dialog).getByRole('textbox', { name: text.form.code })).toBeVisible();
    expect(
      within(dialog).queryByRole('textbox', { name: folderText.field }),
    ).not.toBeInTheDocument();
  });
});
