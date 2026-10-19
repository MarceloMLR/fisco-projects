import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type ClientRow, projectProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  clientChoicesOf,
  demoClients,
  demoPeopleChoices,
  type DemoProject,
  demoProjects,
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
  techLeadDirectoryId: string | null;
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
        `POST ${body.code} ${body.name} ${String(body.clientId)} ${String(body.managerId)} ${String(body.techLeadId)}${body.techLeadDirectoryId === null ? '' : ` ${body.techLeadDirectoryId}`}`,
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
          // A tech lead of the directory becomes a user (ADR-0005).
          ...(body.techLeadDirectoryId === null
            ? []
            : [
                {
                  id: '0199f5a2-7c1e-7d40-9a1b-000000000099',
                  name:
                    demoPeopleChoices.find(
                      (person) => person.directoryId === body.techLeadDirectoryId,
                    )?.name ?? '',
                  role: 'TechLead' as const,
                },
              ]),
        ],
        permissions: {
          canEdit: true,
          canChangeStatus: true,
          canManagePeople: true,
          canManageManagers: true,
          canEditSchedule: true,
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

async function choosePerson(label: string, search: string, name: string) {
  fireEvent.change(screen.getByRole('combobox', { name: label }), { target: { value: search } });
  fireEvent.click(await screen.findByRole('option', { name }));
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
    expect(options.map((option) => option.textContent)).toEqual([
      'Beatriz Lima',
      form.you.replace('{{name}}', marina.name),
      'Paulo Nogueira',
    ]);
  });

  // Spec: projetos, requirement "Gerente e líder técnico na criação" (ADR-0005).
  it('Líder técnico do diretório na criação', async () => {
    await openNewProject();

    fill(form.code, 'TRIB-MG');
    fill(form.name, 'Revisão tributária da filial MG');
    fireEvent.change(screen.getByRole('combobox', { name: form.techLead }), {
      target: { value: 'sofia' },
    });
    const sofia = await screen.findByRole('option', { name: /Sofia Martins/ });
    expect(sofia).toHaveTextContent(`Sofia Martins${ptBR.dates.never}`);
    fireEvent.click(sofia);
    create();

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Revisão tributária da filial MG' }),
    ).toBeVisible();
    expect(screen.getByTestId('project-meta')).toHaveTextContent(
      `${text.header.techLead} Sofia Martins`,
    );
    expect(requests).toEqual([
      `POST TRIB-MG Revisão tributária da filial MG null ${marina.id} null local:sofia.martins@empresa.example`,
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
