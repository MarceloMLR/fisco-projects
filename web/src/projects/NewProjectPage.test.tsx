import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { type ClientRow, projectProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { expectBusy, heldAnswer } from '../test/busy';
import { demoMsProjectPreview } from '../test/demoMsProject';
import { simulateDevice } from '../test/media';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  clientChoicesOf,
  demoClients,
  demoPeople,
  demoPeopleChoices,
  type DemoProject,
  demoProjects,
  emailOf,
  marina,
  peopleOf,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;
const form = text.form;
const newId = '0199f5a2-7c1e-7d40-9a1b-400000000009';

interface ProjectBody {
  code: string;
  name: string;
  description: string | null;
  clientId: string | null;
  managerId: string | null;
  techLeadId: string | null;
}

let projects: DemoProject[];
let clients: ClientRow[];
const requests: string[] = [];

const nameOf = (id: string | null) =>
  id === marina.id
    ? marina.name
    : (demoPeopleChoices.find((person) => person.id === id)?.name ?? '');

beforeEach(() => {
  projects = demoProjects();
  clients = demoClients();
  requests.length = 0;
  const current = projects;
  const currentClients = clients;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/people', ({ request }) => HttpResponse.json(peopleOf(new URL(request.url)))),
    http.get('/api/clients/choices', () => HttpResponse.json(clientChoicesOf(currentClients))),
    http.post('/api/clients', async ({ request }) => {
      const body = (await request.json()) as Pick<ClientRow, 'name' | 'cnpj' | 'observation'>;
      requests.push(`POST client ${body.name}`);
      const id = '0199f5a2-7c1e-7d40-9a1b-300000000009';
      currentClients.push({ id, ...body, logoUrl: null, status: 'Active' });
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.post('/api/projects', async ({ request }) => {
      const body = (await request.json()) as ProjectBody;
      requests.push(
        `POST ${body.code} ${body.name} ${String(body.clientId)} ${String(body.managerId)} ${String(body.techLeadId)}`,
      );
      const owner = current.find((project) => project.code === body.code);
      if (owner !== undefined) {
        return HttpResponse.json(
          {
            status: 409,
            title: 'Conflict',
            type: projectProblems.codeInUse,
            projectName: owner.name,
          },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        );
      }
      const client = currentClients.find((candidate) => candidate.id === body.clientId);
      current.push({
        id: newId,
        code: body.code,
        name: body.name,
        description: body.description,
        client: client === undefined ? null : { id: client.id, name: client.name, logoUrl: null },
        status: 'Planning',
        members: [
          ...(body.managerId === null
            ? []
            : [{ id: body.managerId, name: nameOf(body.managerId), role: 'Manager' as const }]),
          ...(body.techLeadId === null
            ? []
            : [{ id: body.techLeadId, name: nameOf(body.techLeadId), role: 'TechLead' as const }]),
        ],
        permissions: {
          canEdit: true,
          canChangeStatus: true,
          canManagePeople: true,
          canManageManagers: true,
          canEditSchedule: true,
          canDelete: false,
          canChooseFolder: false,
          canExportSchedule: true,
        },
        scheduleStart: null,
        scheduleEnd: null,
        mine: true,
        schedule: null,
      });
      return HttpResponse.json({ id: newId }, { status: 201 });
    }),
    http.get('/api/projects/:id', ({ params }) => {
      const project = current.find((candidate) => candidate.id === params.id);
      return project === undefined
        ? new HttpResponse(null, { status: 404 })
        : HttpResponse.json(project);
    }),
  );
});

async function openNewProject(width?: number) {
  const view = await renderApp('/projects/new', width);
  await screen.findByRole('heading', { level: 1, name: ptBR.pages.newProject.title });
  return view;
}

function fill(label: string, value: string) {
  fireEvent.change(screen.getByRole('textbox', { name: label }), { target: { value } });
}

/** Chooses a person by the name, the first line of the option, above the e-mail. */
async function choosePerson(label: string, search: string, name: string) {
  fireEvent.change(screen.getByRole('combobox', { name: label }), { target: { value: search } });
  fireEvent.click(await within(await screen.findByRole('listbox')).findByText(name));
}

async function chooseClient(name: string) {
  fireEvent.mouseDown(screen.getByRole('combobox', { name: form.client }));
  fireEvent.click(await screen.findByRole('option', { name }));
}

function create() {
  fireEvent.click(screen.getByRole('button', { name: form.create }));
}

// Spec: projetos, requirement "Tela Novo projeto".
describe('Tela Novo projeto', () => {
  it('Novo projeto pela tela', async () => {
    const { router } = await openNewProject();

    expect(screen.getByRole('heading', { level: 2, name: form.data })).toBeVisible();
    fill(form.code, 'trib-mg');
    expect(screen.getByRole('textbox', { name: form.code })).toHaveValue('TRIB-MG');
    fill(form.name, 'Revisão tributária da filial MG');
    await chooseClient('Beta Alimentos');
    expect(screen.getByRole('combobox', { name: form.manager })).toHaveValue(
      form.you.replace('{{name}}', marina.name),
    );
    await choosePerson(form.techLead, 'raf', 'Rafael Souza');
    fill(`${form.description} ${form.optional}`, 'Revisão das apurações de ICMS da filial.');
    create();

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(`/projects/${newId}`);
    });
    // The project takes the place of the form, so Back does not return to it (ADR-0105).
    expect(router.state.historyAction).toBe('REPLACE');
    expect(
      await screen.findByRole('heading', { level: 1, name: 'Revisão tributária da filial MG' }),
    ).toBeVisible();
    const meta = screen.getByTestId('project-meta');
    expect(meta).toHaveTextContent(`${text.header.manager} ${marina.name}`);
    expect(meta).toHaveTextContent(`${text.header.techLead} Rafael Souza`);
    expect(requests).toEqual([
      `POST TRIB-MG Revisão tributária da filial MG 0199f5a2-7c1e-7d40-9a1b-300000000002 ${marina.id} 0199f5a2-7c1e-7d40-9a1b-000000000004`,
    ]);
  });

  it('Projeto interno pela tela', async () => {
    await openNewProject();

    expect(screen.getByRole('combobox', { name: form.client })).toHaveTextContent(form.noClient);
    fill(form.code, 'PORTAL-RH');
    fill(form.name, 'Portal do RH');
    create();

    await screen.findByRole('heading', { level: 1, name: 'Portal do RH' });
    expect(screen.getByTestId('project-meta')).toHaveTextContent(text.internal);
    expect(requests).toEqual([`POST PORTAL-RH Portal do RH null ${marina.id} null`]);
  });

  it('Código inválido pela tela', async () => {
    await openNewProject();

    fill(form.code, '1ERP');
    fill(form.name, 'ERP');
    create();

    const code = screen.getByRole('textbox', { name: form.code });
    await waitFor(() => {
      expect(code).toHaveAccessibleDescription(text.errors.codeInvalid);
    });
    expect(code).toBeInvalid();
    expect(requests).toEqual([]);
  });

  it('Código repetido pela tela', async () => {
    const { router } = await openNewProject();

    fill(form.code, 'ERP-FIN');
    fill(form.name, 'Outro ERP');
    create();

    const code = screen.getByRole('textbox', { name: form.code });
    await waitFor(() => {
      expect(code).toHaveAccessibleDescription(
        text.errors.codeInUse.replace('{{name}}', 'Implantação do ERP financeiro'),
      );
    });
    expect(router.state.location.pathname).toBe('/projects/new');
  });

  it('Nome obrigatório pela tela', async () => {
    await openNewProject();

    fill(form.code, 'ERP-MG');
    create();

    await waitFor(() => {
      expect(screen.getByRole('textbox', { name: form.name })).toHaveAccessibleDescription(
        text.errors.nameRequired,
      );
    });
    expect(requests).toEqual([]);
  });

  it('Cancelar o Novo projeto', async () => {
    const { router } = await openNewProject();

    fireEvent.click(screen.getByRole('button', { name: form.cancel }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects');
    });
    expect(router.state.historyAction).toBe('REPLACE');
  });

  it('Novo projeto no celular', async () => {
    await openNewProject(phoneWidth);

    expect(screen.getByRole('textbox', { name: form.code })).toBeVisible();
    expect(screen.getByRole('button', { name: form.create })).toBeVisible();
  });
});

// Spec: projetos, requirement "Gerente e cliente no Novo projeto".
describe('Gerente e cliente no Novo projeto', () => {
  it('Cadastrar cliente na criação', async () => {
    await openNewProject();

    fireEvent.click(screen.getByRole('button', { name: form.newClient }));
    const dialog = await screen.findByRole('dialog', { name: ptBR.clients.newClient });
    // The logo is the Administrator's (ADR-0046).
    expect(within(dialog).queryByTestId('client-logo-file')).not.toBeInTheDocument();
    fireEvent.change(within(dialog).getByRole('textbox', { name: ptBR.clients.name }), {
      target: { value: 'Zeta Mineração' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: ptBR.clients.include }));

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: form.client })).toHaveTextContent(
        'Zeta Mineração',
      );
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(requests).toEqual(['POST client Zeta Mineração']);
  });

  it('Trocar o gerente na criação', async () => {
    signedInAs('Administrator');
    await openNewProject();

    expect(screen.queryByText(form.notMember)).not.toBeInTheDocument();
    await choosePerson(form.manager, 'bea', 'Beatriz Lima');

    expect(screen.getByRole('combobox', { name: form.manager })).toHaveValue('Beatriz Lima');
    expect(screen.getByRole('combobox', { name: form.manager })).toHaveAccessibleDescription(
      form.notMember,
    );
  });

  it('Só gerentes para escolher como gerente', async () => {
    await openNewProject();

    fireEvent.change(screen.getByRole('combobox', { name: form.manager }), {
      target: { value: 'a' },
    });

    const options = await screen.findAllByRole('option');
    // The name and, below, the e-mail.
    expect(options.map((option) => option.textContent)).toEqual([
      `Beatriz Lima${emailOf('Beatriz Lima')}`,
      `${form.you.replace('{{name}}', marina.name)}${marina.email}`,
      `Paulo Nogueira${emailOf('Paulo Nogueira')}`,
    ]);
  });

  // Spec: projetos, requirement "Gerente e líder técnico na criação" (ADR-0097).
  it('Só líderes técnicos para escolher como líder técnico', async () => {
    await openNewProject();

    fireEvent.change(screen.getByRole('combobox', { name: form.techLead }), {
      target: { value: 'a' },
    });

    // The Administrator and the tech lead of the system, without the people of the directory.
    const options = await screen.findAllByRole('option');
    expect(options.map((option) => option.textContent)).toEqual([
      `Paulo Nogueira${emailOf('Paulo Nogueira')}`,
      `Rafael Souza${emailOf('Rafael Souza')}`,
    ]);
  });

  it('Gerente e líder técnico são pessoas diferentes', async () => {
    await openNewProject();

    fireEvent.change(screen.getByRole('combobox', { name: form.techLead }), {
      target: { value: 'marina' },
    });

    expect(await screen.findByText(form.noPeople)).toBeVisible();
  });
});

// Spec: modelos-de-projeto, requirement "Modelos pela tela".
describe('Novo projeto a partir de um modelo', () => {
  it('Novo projeto a partir de um modelo pela tela', async () => {
    const bodies: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects', async ({ request }) => {
        bodies.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');

    // "Em branco" at first; the model comes with the first active one.
    expect(screen.getByRole('radio', { name: new RegExp(form.blank) })).toBeChecked();
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromTemplate) }));
    const preview = await screen.findByRole('complementary', { name: form.preview });
    expect(within(preview).getByRole('heading', { name: 'Implantação de ERP' })).toBeVisible();
    expect(preview).toHaveTextContent('5 tarefas · 1 marco');
    expect(within(preview).getByText('Marco: Aceite da homologação')).toBeVisible();
    // The start is needed.
    create();
    expect(await screen.findByText(text.errors.scheduleStartRequired)).toBeVisible();
    expect(bodies).toEqual([]);
    fireEvent.change(screen.getByLabelText(form.scheduleStart), {
      target: { value: '2026-11-03' },
    });
    create();

    await waitFor(() => {
      expect(bodies).toHaveLength(1);
    });
    expect(bodies[0]).toMatchObject({
      code: 'ERP-RH',
      templateId: '0199f5a2-7c1e-7d40-9a1b-700000000001',
      scheduleStart: '2026-11-03',
    });
  });

  it('Projeto em branco pela tela', async () => {
    const bodies: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects', async ({ request }) => {
        bodies.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');
    create();

    await waitFor(() => {
      expect(bodies).toHaveLength(1);
    });
    expect(bodies[0]).toMatchObject({ templateId: null, scheduleStart: null });
    expect(screen.queryByRole('complementary', { name: form.preview })).not.toBeInTheDocument();
  });
});

// Spec: projetos, requirement "Pasta do projeto na tela" (ADR-0122).
describe('Pasta do projeto na tela', () => {
  const folderText = text.folder;
  const folders = {
    clientes: '01clientes',
    alfa: '01alfa',
    projetos: '01projetos',
    erpFin: '01erpfin',
  };

  beforeEach(() => {
    server.use(
      http.get('/api/installation/rules', () =>
        HttpResponse.json({
          timeZone: 'America/Sao_Paulo',
          dailySummaryAt: '07:30:00',
          notificationRetentionDays: 90,
          auditRetentionYears: 5,
          projectsFolder: 'Projetos',
          workHoursPerDay: 8,
        }),
      ),
      http.get('/api/project-folders', ({ request }) => {
        const parentId = new URL(request.url).searchParams.get('parentId') ?? '';
        const folder = (
          id: string,
          name: string,
          refusal: string | null = null,
          code?: string,
        ) => ({
          id,
          name,
          refusal,
          projectCode: code ?? null,
        });
        const lists: Record<
          string,
          { current: ReturnType<typeof folder> | null; folders: ReturnType<typeof folder>[] }
        > = {
          '': {
            current: null,
            folders: [
              folder(folders.clientes, 'Clientes'),
              folder(folders.projetos, 'Projetos', 'ProjectsFolder'),
            ],
          },
          [folders.clientes]: {
            current: folder(folders.clientes, 'Clientes'),
            folders: [folder(folders.alfa, 'Alfa')],
          },
          [folders.alfa]: { current: folder(folders.alfa, 'Alfa'), folders: [] },
          [folders.projetos]: {
            current: folder(folders.projetos, 'Projetos', 'ProjectsFolder'),
            folders: [folder(folders.erpFin, 'ERP-FIN - Implantação', 'InUse', 'ERP-FIN')],
          },
        };
        return HttpResponse.json(lists[parentId]);
      }),
    );
  });

  async function openFolders() {
    fireEvent.click(screen.getByRole('button', { name: folderText.choose }));
    return screen.findByRole('dialog', { name: folderText.dialogTitle });
  }

  it('Escolher a pasta no Novo projeto', async () => {
    const bodies: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects', async ({ request }) => {
        bodies.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    await openNewProject();
    fill(form.code, 'trib-mg');
    fill(form.name, 'Revisão tributária');
    // Until someone chooses another, the folder is the one the system creates in the folder of the projects.
    expect(screen.getByRole('textbox', { name: folderText.field })).toHaveValue(
      'Projetos / TRIB-MG - Revisão tributária',
    );
    expect(screen.queryByRole('button', { name: folderText.useSystem })).not.toBeInTheDocument();

    const dialog = await openFolders();
    // At the root, nothing is chosen yet.
    expect(await within(dialog).findByText(folderText.openOne)).toBeVisible();
    expect(within(dialog).getByRole('button', { name: folderText.use })).toBeDisabled();
    fireEvent.click(await within(dialog).findByRole('button', { name: /Clientes/ }));
    fireEvent.click(await within(dialog).findByRole('button', { name: /Alfa/ }));
    await within(dialog).findByText(folderText.empty);
    const path = within(dialog).getByRole('navigation', { name: folderText.path });
    expect(within(path).getByRole('button', { name: 'Alfa' })).toHaveAttribute(
      'aria-current',
      'location',
    );
    fireEvent.click(within(dialog).getByRole('button', { name: folderText.use }));

    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: folderText.dialogTitle }),
      ).not.toBeInTheDocument();
    });
    expect(screen.getByRole('textbox', { name: folderText.field })).toHaveValue('Clientes / Alfa');
    expect(screen.getByText(folderText.chosenHelper)).toBeVisible();
    expect(screen.getByRole('button', { name: folderText.useSystem })).toBeVisible();
    create();

    await waitFor(() => {
      expect(bodies).toHaveLength(1);
    });
    expect(bodies[0]).toMatchObject({ code: 'TRIB-MG', folderId: folders.alfa });
  });

  it('Pasta que não pode ser escolhida no diálogo', async () => {
    await openNewProject();

    const dialog = await openFolders();
    fireEvent.click(await within(dialog).findByRole('button', { name: /Projetos/ }));
    await within(dialog).findByText('ERP-FIN - Implantação');
    // The folder of another project shows its code and does not open.
    const list = within(dialog).getByRole('list', { name: folderText.folders });
    expect(within(list).queryByRole('button')).not.toBeInTheDocument();
    expect(within(list).getByText('ERP-FIN')).toBeVisible();
    expect(
      within(list).getByText(folderText.refusals.InUse.replace('{{code}}', 'ERP-FIN')),
    ).toBeVisible();
    // The folder of the projects is never the folder of a project, and the dialog says why.
    const use = within(dialog).getByRole('button', { name: folderText.use });
    expect(use).toBeDisabled();
    expect(use).toHaveAccessibleDescription(folderText.why.ProjectsFolder);
  });

  it('Pasta recusada pelo servidor na criação', async () => {
    server.use(
      http.post('/api/projects', () =>
        HttpResponse.json(
          {
            status: 400,
            title: 'Bad Request',
            errors: { folderId: ['Another project uses the folder.'] },
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openNewProject();
    fill(form.code, 'trib-mg');
    fill(form.name, 'Revisão tributária');
    const dialog = await openFolders();
    fireEvent.click(await within(dialog).findByRole('button', { name: /Clientes/ }));
    await within(dialog).findByRole('button', { name: /Alfa/ });
    fireEvent.click(within(dialog).getByRole('button', { name: folderText.use }));
    create();

    expect(await screen.findByText(folderText.invalid)).toBeVisible();
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
describe('Botões em andamento no Novo projeto', () => {
  it('Formulário em andamento no Novo projeto', async () => {
    const held = heldAnswer();
    // Held first, then the creation of the simulated API.
    server.use(
      http.post('/api/projects', async () => {
        await held.until;
      }),
    );
    await openNewProject();
    fill(form.code, 'PORTAL-RH');
    fill(form.name, 'Portal do RH');
    // The button sits in the header, outside the form, and is busy as well.
    const submit = screen.getByRole('button', { name: form.create });

    create();

    await waitFor(() => {
      expectBusy(submit, form.create);
    });
    create();
    held.release();
    await screen.findByRole('heading', { level: 1, name: 'Portal do RH' });
    expect(requests).toEqual([`POST PORTAL-RH Portal do RH null ${marina.id} null`]);
  });
});

// Spec: cronograma, requirement "Importar do MS Project pela tela" (ADR-0135).
describe('Novo projeto a partir do MS Project', () => {
  const msProject = form.msProject;
  const owners = msProject.owners;
  const xml = () =>
    new File(['<Project xmlns="http://schemas.microsoft.com/project" />'], 'cronograma.xml', {
      type: 'text/xml',
    });

  /** The start each reading of the file sent, none for the first. */
  let starts: (string | undefined)[];

  beforeEach(() => {
    starts = [];
    server.use(
      http.post('/api/projects/ms-project-preview', async ({ request }) => {
        const start = /name="start"\r\n\r\n([\d-]+)/.exec(await request.text())?.[1];
        starts.push(start);
        return HttpResponse.json(demoMsProjectPreview(start), {
          headers: { 'Cache-Control': 'no-store' },
        });
      }),
    );
  });

  function chooseFile(file: File) {
    fireEvent.change(screen.getByTestId('ms-project-file'), { target: { files: [file] } });
  }

  async function chooseOption(label: string, name: string) {
    fireEvent.mouseDown(screen.getByRole('combobox', { name: label }));
    fireEvent.click(await screen.findByRole('option', { name }));
  }

  it('Novo projeto a partir do MS Project', async () => {
    const bodies: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects', async ({ request }) => {
        bodies.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    const { router } = await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());

    // The preview, with the dates the server computed, and what changed in the conversion.
    const preview = await screen.findByRole('complementary', { name: form.preview });
    expect(within(preview).getByRole('heading', { name: 'Implantação do ERP' })).toBeVisible();
    expect(preview).toHaveTextContent('2 tarefas · 1 marco');
    expect(within(preview).getByText('Marco: Aceite da configuração')).toBeVisible();
    expect(within(preview).getByText('13/10 – 15/10/2026')).toBeVisible();
    expect(screen.getByLabelText(form.scheduleStart)).toHaveValue('2026-10-13');
    expect(screen.getByTestId('ms-project-file-name')).toHaveTextContent('cronograma.xml');
    const warnings = screen.getByRole('status');
    expect(warnings).toHaveTextContent(msProject.warnings);
    expect(warnings).toHaveTextContent(
      '1 tarefa com mais de um recurso ficou com o primeiro: Treinar usuários.',
    );

    // Carla comes found by her e-mail; the consultant goes to a person of the company, as a developer.
    const people = screen.getByRole('region', { name: owners.title });
    const carla = within(people).getByRole('listitem', { name: /Carla Mendes/ });
    expect(within(carla).getByRole('combobox', { name: 'Pessoa de Carla Mendes' })).toHaveValue(
      'Carla Mendes',
    );
    expect(
      within(people).queryByRole('combobox', { name: 'Pessoa de Consultor externo' }),
    ).toBeNull();
    await chooseOption('Responsável por Consultor externo', owners.person);
    await choosePerson('Pessoa de Consultor externo', 'Diego', 'Diego Torres');
    await chooseOption('Papel de Consultor externo', text.roles.Developer);

    // Another start reads the file again.
    fireEvent.change(screen.getByLabelText(form.scheduleStart), {
      target: { value: '2026-10-19' },
    });
    expect(await within(preview).findByText('19/10 – 21/10/2026')).toBeVisible();
    expect(starts).toEqual([undefined, '2026-10-19']);
    create();

    await waitFor(() => {
      expect(bodies).toHaveLength(1);
    });
    const sent = demoMsProjectPreview('2026-10-19');
    expect(bodies[0]).toMatchObject({
      code: 'ERP-RH',
      templateId: null,
      scheduleStart: '2026-10-19',
      importedSchedule: {
        phases: sent.plan.phases,
        items: sent.plan.items,
        resources: [
          { userId: demoPeople.carla.id, directoryId: null, client: false, role: 'Contributor' },
          { userId: demoPeople.diego.id, directoryId: null, client: false, role: 'Developer' },
        ],
      },
    });
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(`/projects/${newId}`);
    });
  });

  it('Quadro da conversão na tela', async () => {
    const scroll = vi.spyOn(Element.prototype, 'scrollIntoView');
    await openNewProject();
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());

    // The page goes to the box, which may be below the visible part of the form.
    const box = await screen.findByTestId('ms-project-conversion');
    await waitFor(() => {
      expect(scroll.mock.contexts).toContain(box);
    });
    expect(scroll).toHaveBeenLastCalledWith({ block: 'nearest', behavior: 'smooth' });
    const status = within(box).getByRole('status');
    expect(status).toHaveTextContent('4 de 4 tarefas e 2 de 2 recursos vieram do arquivo.');
    expect(
      within(status)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual([
      '1 dependência ficou sem o intervalo: Parametrizar regras de ICMS → Aceite da configuração.',
      '1 tarefa com mais de um recurso ficou com o primeiro: Treinar usuários.',
      'O recurso Consultor externo está sem pessoa, e a tarefa dele fica sem responsável.',
    ]);

    // Another start reads the file again without moving the page.
    const calls = scroll.mock.calls.length;
    fireEvent.change(screen.getByLabelText(form.scheduleStart), {
      target: { value: '2026-10-19' },
    });
    const preview = screen.getByRole('complementary', { name: form.preview });
    expect(await within(preview).findByText('19/10 – 21/10/2026')).toBeVisible();
    expect(scroll.mock.calls).toHaveLength(calls);

    // The shortcut of the preview takes to the box, with the focus.
    fireEvent.click(within(preview).getByRole('button', { name: '3 ajustes na conversão' }));
    expect(box).toHaveFocus();
    expect(scroll.mock.contexts.at(-1)).toBe(box);
    scroll.mockRestore();
  });

  it('Arquivo sem ajustes na tela', async () => {
    simulateDevice({ reducedMotion: true });
    const scroll = vi.spyOn(Element.prototype, 'scrollIntoView');
    const whole = demoMsProjectPreview();
    server.use(
      http.post('/api/projects/ms-project-preview', () =>
        HttpResponse.json({
          ...whole,
          resources: whole.resources.map((resource) => ({
            ...resource,
            person: resource.person ?? {
              id: demoPeople.diego.id,
              directoryId: null,
              name: 'Diego Torres',
              email: 'diego.torres@empresa.example',
            },
          })),
          warnings: [],
        }),
      ),
    );
    await openNewProject();
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());

    const box = await screen.findByTestId('ms-project-conversion');
    const status = within(box).getByRole('status');
    expect(status).toHaveTextContent('4 de 4 tarefas e 2 de 2 recursos vieram do arquivo.');
    expect(status).toHaveTextContent(msProject.noWarnings);
    expect(within(status).queryByRole('listitem')).toBeNull();
    const preview = screen.getByRole('complementary', { name: form.preview });
    expect(within(preview).queryByRole('button', { name: /ajuste/ })).toBeNull();
    // With less motion, the page goes there at once.
    await waitFor(() => {
      expect(scroll).toHaveBeenLastCalledWith({ block: 'nearest', behavior: 'auto' });
    });
    scroll.mockRestore();
  });

  it('Tarefas sem responsável na tela', async () => {
    await openNewProject();
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());
    const status = within(await screen.findByTestId('ms-project-conversion')).getByRole('status');
    const preview = screen.getByRole('complementary', { name: form.preview });
    const people = screen.getByRole('region', { name: owners.title });
    const consultant = within(people).getByRole('listitem', { name: /Consultor externo/ });
    const ownerless =
      'O recurso Consultor externo está sem pessoa, e a tarefa dele fica sem responsável.';

    // Without a person, the row and the box say the task stays without an owner; Carla, found, owns hers.
    expect(status).toHaveTextContent(ownerless);
    expect(
      within(consultant).getByRole('combobox', { name: 'Responsável por Consultor externo' }),
    ).toHaveAccessibleDescription('A tarefa fica sem responsável.');
    const carla = within(people).getByRole('listitem', { name: /Carla Mendes/ });
    expect(within(carla).queryByText(/fica sem responsável/)).toBeNull();

    // A person of the company with nobody chosen still leaves it without an owner.
    await chooseOption('Responsável por Consultor externo', owners.person);
    expect(status).toHaveTextContent(ownerless);
    await choosePerson('Pessoa de Consultor externo', 'Diego', 'Diego Torres');

    expect(status).not.toHaveTextContent(/sem responsável/);
    expect(within(consultant).queryByText(/fica sem responsável/)).toBeNull();
    expect(within(preview).getByRole('button', { name: '2 ajustes na conversão' })).toBeVisible();
  });

  it('Recursos sem pessoa no quadro', async () => {
    const [carla, consultant] = demoMsProjectPreview().resources;
    if (carla === undefined || consultant === undefined) {
      throw new Error('The preview has two resources.');
    }
    server.use(
      http.post('/api/projects/ms-project-preview', () =>
        HttpResponse.json({
          ...demoMsProjectPreview(),
          resources: [
            { ...carla, person: null, tasks: 2 },
            { ...consultant, tasks: 1 },
          ],
          warnings: [{ kind: 'BlankLines', names: [], count: 2 }],
        }),
      ),
    );
    await openNewProject();
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());

    const status = within(await screen.findByTestId('ms-project-conversion')).getByRole('status');
    expect(
      within(status)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual([
      '2 linhas em branco ou sem nome ficaram de fora.',
      '2 recursos estão sem pessoa, e as tarefas deles ficam sem responsável: Carla Mendes (2 tarefas), Consultor externo (1 tarefa).',
    ]);
  });

  it('Arquivo recusado pela tela', async () => {
    const bodies: unknown[] = [];
    server.use(
      http.post('/api/projects/ms-project-preview', () =>
        HttpResponse.json(
          {
            status: 400,
            type: 'urn:fiscoprojects:problem:ms-project-file-refused',
            reason: 'NotMsProjectXml',
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
      http.post('/api/projects', async ({ request }) => {
        bodies.push(await request.json());
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(new File([new Uint8Array([0xd0, 0xcf, 0x11, 0xe0])], 'cronograma.mpp'));

    expect(await screen.findByText(msProject.refused.NotMsProjectXml)).toBeVisible();
    expect(screen.queryByRole('complementary', { name: form.preview })).not.toBeInTheDocument();
    create();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: form.create })).not.toHaveAttribute('aria-busy');
    });
    expect(bodies).toEqual([]);
    expect(screen.getByText(msProject.refused.NotMsProjectXml)).toBeVisible();
  });

  it('Sem arquivo do MS Project pela tela', async () => {
    await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    create();

    expect(await screen.findByText(text.errors.msProjectFileRequired)).toBeVisible();
    expect(screen.getByLabelText(form.scheduleStart)).toBeDisabled();
  });

  it('Cliente como responsável do arquivo', async () => {
    const bodies: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects', async ({ request }) => {
        bodies.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: newId }, { status: 201 });
      }),
    );
    await openNewProject();
    fill(form.code, 'erp-rh');
    fill(form.name, 'Implantação do ERP de RH');
    const [client] = clients;
    if (client === undefined) {
      throw new Error('The simulated API has clients.');
    }
    await chooseClient(client.name);
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());
    await screen.findByRole('complementary', { name: form.preview });

    await chooseOption('Responsável por Consultor externo', `${client.name} (cliente)`);
    create();

    await waitFor(() => {
      expect(bodies).toHaveLength(1);
    });
    expect(bodies[0]).toMatchObject({
      clientId: client.id,
      importedSchedule: {
        resources: [
          { userId: demoPeople.carla.id, client: false },
          { userId: null, directoryId: null, client: true, role: null },
        ],
      },
    });
  });

  it('MS Project no celular e no tema escuro', async () => {
    simulateDevice({ systemTheme: 'dark' });
    await openNewProject(phoneWidth);
    fireEvent.click(screen.getByRole('radio', { name: new RegExp(form.fromMsProject) }));
    chooseFile(xml());

    // On the phone, the preview comes below the form, after the people of the file.
    const preview = await screen.findByRole('complementary', { name: form.preview });
    const people = screen.getByRole('region', { name: owners.title });
    expect(people.compareDocumentPosition(preview) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(people).getAllByRole('listitem')).toHaveLength(2);
  });
});
