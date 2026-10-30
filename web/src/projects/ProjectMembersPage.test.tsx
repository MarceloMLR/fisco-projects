import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';
import { type Project, type ProjectRole, projectProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoPeopleChoices,
  type DemoProject,
  demoProjects,
  demoTickets,
  emailOf,
  marina,
  membersOf,
  peopleOf,
  projectsPage,
  server,
  signedInAs,
  ticketChoiceOf,
} from '../test/server';

const text = ptBR.projects.members;
const roles = ptBR.projects.roles;
const projectName = 'Implantação do ERP financeiro';

let project: DemoProject;
/** What the person in the session may do with the people, as the API answers it. */
let permissions: Pick<Project['permissions'], 'canManagePeople' | 'canManageManagers'>;
const requests: string[] = [];

const lastManager = () =>
  HttpResponse.json(
    { status: 409, title: 'Conflict', type: projectProblems.lastManager },
    { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
  );

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  // The team of the canvas ProjetoMembros: Marina manages, Rafael leads, Carla, Diego and Paulo work in it.
  erp.members = [
    ...erp.members,
    { ...demoPeople.carla, role: 'Contributor' },
    { ...demoPeople.diego, role: 'Developer' },
    { ...demoPeople.paulo, role: 'Viewer' },
  ];
  project = erp;
  permissions = { canManagePeople: true, canManageManagers: true };
  requests.length = 0;
  const current = erp;
  const managers = () => current.members.filter((member) => member.role === 'Manager');
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.get('/api/projects/:id', ({ params }) =>
      params.id === current.id
        ? HttpResponse.json({ ...current, permissions: { ...current.permissions, ...permissions } })
        : new HttpResponse(null, { status: 404 }),
    ),
    http.get('/api/projects/:id/members', () => HttpResponse.json(membersOf(current))),
    http.get('/api/people', ({ request }) => HttpResponse.json(peopleOf(new URL(request.url)))),
    http.post('/api/projects/:id/members', async ({ request }) => {
      const body = (await request.json()) as {
        userId: string | null;
        directoryId: string | null;
        role: ProjectRole;
      };
      requests.push(`POST ${String(body.userId)} ${String(body.directoryId)} ${body.role}`);
      const person = demoPeopleChoices.find((choice) =>
        body.userId === null ? choice.directoryId === body.directoryId : choice.id === body.userId,
      );
      const id = person?.id ?? '0199f5a2-7c1e-7d40-9a1b-000000000099';
      current.members.push({ id, name: person?.name ?? '', role: body.role });
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/projects/:id/members/:userId/role', async ({ request, params }) => {
      const { role } = (await request.json()) as { role: ProjectRole };
      requests.push(`PUT ${String(params.userId)} ${role}`);
      const member = current.members.find((candidate) => candidate.id === params.userId);
      if (member === undefined) {
        return new HttpResponse(null, { status: 404 });
      }
      if (member.role === 'Manager' && role !== 'Manager' && managers().length === 1) {
        return lastManager();
      }
      member.role = role;
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/projects/:id/members/:userId', ({ params }) => {
      requests.push(`DELETE ${String(params.userId)}`);
      const index = current.members.findIndex((candidate) => candidate.id === params.userId);
      const member = current.members[index];
      if (member?.role === 'Manager' && managers().length === 1) {
        return lastManager();
      }
      current.members.splice(index, 1);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

/** The lines of the list, the groups as "Gestão 2" and the people by name, as shown. */
function lines() {
  return within(table())
    .getAllByRole('row')
    .slice(1)
    .map((row) => {
      const cell = within(row).getAllByRole('gridcell')[0];
      if (row.classList.contains('row-group')) {
        return cell?.textContent ?? '';
      }
      const known = project.members.find((member) => cell?.textContent.includes(member.name));
      return known?.name ?? cell?.textContent ?? '';
    });
}

function rowOf(name: string) {
  const row = within(table())
    .getAllByRole('row')
    .find(
      (candidate) =>
        !candidate.classList.contains('row-group') && candidate.textContent.includes(name),
    );
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

async function openMembers(width?: number) {
  const view = await renderApp(`/projects/${project.id}/members`, width);
  await waitFor(() => {
    expect(lines()).toContain('Diego Torres');
  });
  return view;
}

async function choose(name: string) {
  fireEvent.click(within(rowOf(name)).getByText(name));
  return screen.findByRole('dialog', { name: text.details.label });
}

async function chooseRole(container: HTMLElement, role: string) {
  fireEvent.mouseDown(within(container).getByRole('combobox', { name: text.details.role }));
  fireEvent.click(await screen.findByRole('option', { name: role }));
}

// Spec: membros-do-projeto, requirement "Tela Membros".
describe('Tela Membros', () => {
  it('Membros pela tela', async () => {
    await openMembers();

    expect(lines()).toEqual([
      `${text.teams.management}2`,
      'Marina Costa',
      'Rafael Souza',
      `${text.teams.functional}1`,
      'Carla Mendes',
      `${text.teams.development}2`,
      'Diego Torres',
      'Rafael Souza',
      `${text.teams.viewers}1`,
      'Paulo Nogueira',
    ]);
    const cells = within(rowOf('Carla Mendes'))
      .getAllByRole('gridcell')
      .map((cell) => cell.textContent);
    expect(cells).toEqual([
      'CMCarla Mendescarla.mendes@empresa.example',
      roles.Contributor,
      ptBR.dates.never,
    ]);
    expect(screen.getByText(text.total_other.replace('{{count}}', '5'))).toBeInTheDocument();
    const tab = screen.getByRole('tab', { name: /Membros/ });
    expect(tab).toHaveAttribute('aria-selected', 'true');
    expect(tab).toHaveTextContent(`${ptBR.pages.projectMembers.title}5`);
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(
      within(trail)
        .getAllByRole('listitem')
        .map((level) => level.textContent),
    ).toEqual([ptBR.pages.projects.title, 'ERP-FIN', ptBR.pages.projectMembers.title]);
  });

  it('Buscar e agrupar por nome', async () => {
    await openMembers();

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'RAFA' },
    });
    await waitFor(() => {
      expect(lines()).toEqual([
        `${text.teams.management}1`,
        'Rafael Souza',
        `${text.teams.development}1`,
        'Rafael Souza',
      ]);
    });

    fireEvent.click(
      within(screen.getByRole('radiogroup', { name: text.grouping })).getByRole('radio', {
        name: text.groupings.name,
      }),
    );
    await waitFor(() => {
      expect(lines()).toEqual(['Rafael Souza']);
    });
    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: '' },
    });
    await waitFor(() => {
      expect(lines()).toEqual([
        'Carla Mendes',
        'Diego Torres',
        'Marina Costa',
        'Paulo Nogueira',
        'Rafael Souza',
      ]);
    });
  });

  it('Membros no celular', async () => {
    await openMembers(phoneWidth);

    expect(screen.getByRole('button', { name: text.include })).toBeVisible();
    expect(lines()).toContain('Carla Mendes');
  });
});

// Spec: membros-do-projeto, requirement "Incluir pessoa pela tela".
describe('Incluir pessoa pela tela', () => {
  it('Incluir pessoa pela tela', async () => {
    await openMembers();

    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    fireEvent.change(within(dialog).getByRole('combobox', { name: text.dialog.person }), {
      target: { value: 'lucas' },
    });
    const lucas = await screen.findByRole('option', { name: /Lucas Prado/ });
    expect(lucas).toHaveTextContent(`Lucas Prado${emailOf('Lucas Prado')}${ptBR.dates.never}`);
    fireEvent.click(lucas);
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.role }));
    fireEvent.click(await screen.findByRole('option', { name: roles.Contributor }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submit }));

    expect(
      await screen.findByText(text.messages.included.replace('{{name}}', 'Lucas Prado')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(lines().slice(3, 6)).toEqual([
        `${text.teams.functional}2`,
        'Carla Mendes',
        'Lucas Prado',
      ]);
    });
    expect(screen.getByRole('tab', { name: /Membros/ })).toHaveTextContent('6');
    expect(requests).toEqual(['POST null local:lucas.prado@empresa.example Contributor']);
  });

  it('Pessoas do projeto fora da busca', async () => {
    await openMembers();

    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    fireEvent.change(within(dialog).getByRole('combobox', { name: text.dialog.person }), {
      target: { value: 'a' },
    });

    const options = await screen.findAllByRole('option');
    // The name and, below, the e-mail; who never signed in says so.
    expect(options.map((option) => option.textContent)).toEqual([
      `Beatriz Lima${emailOf('Beatriz Lima')}`,
      `Lucas Prado${emailOf('Lucas Prado')}${ptBR.dates.never}`,
      `Sofia Martins${emailOf('Sofia Martins')}${ptBR.dates.never}`,
    ]);
  });

  it('Gerente só com o papel no sistema', async () => {
    await openMembers();

    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    fireEvent.change(within(dialog).getByRole('combobox', { name: text.dialog.person }), {
      target: { value: 'lucas' },
    });
    fireEvent.click(await screen.findByRole('option', { name: /Lucas Prado/ }));

    expect(
      within(dialog).getByRole('combobox', { name: text.dialog.role }),
    ).toHaveAccessibleDescription(text.dialog.notManagerNorTechLead);
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.role }));
    expect(await screen.findByRole('option', { name: roles.Manager })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
  });

  // Spec: membros-do-projeto, requirement "Incluir pessoa pela tela" (ADR-0097).
  it('Papel sugerido ao incluir', async () => {
    server.use(
      http.get('/api/people', () =>
        HttpResponse.json([
          {
            id: null,
            directoryId: 'local:lucas.prado@empresa.example',
            name: 'Lucas Prado',
            email: 'lucas.prado@empresa.example',
            canBeManager: false,
            canBeTechLead: false,
          },
          {
            id: '0199f5a2-7c1e-7d40-9a1b-000000000009',
            directoryId: null,
            name: 'Sofia Martins',
            email: 'sofia.martins@empresa.example',
            canBeManager: false,
            canBeTechLead: true,
          },
        ]),
      ),
    );
    await openMembers();
    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    const person = within(dialog).getByRole('combobox', { name: text.dialog.person });
    const role = () => within(dialog).getByRole('combobox', { name: text.dialog.role });

    // A tech lead of the system comes as tech lead.
    fireEvent.change(person, { target: { value: 'sofia' } });
    fireEvent.click(await screen.findByRole('option', { name: /Sofia Martins/ }));
    expect(role()).toHaveTextContent(roles.TechLead);
    expect(role()).not.toHaveAccessibleDescription(text.dialog.notTechLead);

    // Who may not lead a project technically does not get the role.
    fireEvent.change(person, { target: { value: 'lucas' } });
    fireEvent.click(await screen.findByRole('option', { name: /Lucas Prado/ }));
    expect(role()).toHaveTextContent(roles.Viewer);
    expect(role()).toHaveAccessibleDescription(text.dialog.notManagerNorTechLead);
    fireEvent.mouseDown(role());
    expect(await screen.findByRole('option', { name: roles.TechLead })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
  });

  // Spec: usuarios-e-papeis, requirement "E-mail na escolha de pessoa".
  it('Pessoas de mesmo nome na inclusão', async () => {
    const namesake = (id: string, email: string) => ({
      id,
      directoryId: null,
      name: 'Marina Costa',
      email,
      canBeManager: false,
      canBeTechLead: false,
    });
    server.use(
      http.get('/api/people', () =>
        HttpResponse.json([
          namesake('0199f5a2-7c1e-7d40-9a1b-000000000021', 'marina.costa@empresa.example'),
          namesake('0199f5a2-7c1e-7d40-9a1b-000000000022', 'marina.costa.ti@empresa.example'),
        ]),
      ),
    );
    await openMembers();
    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });

    fireEvent.change(within(dialog).getByRole('combobox', { name: text.dialog.person }), {
      target: { value: 'mar' },
    });

    const options = await screen.findAllByRole('option', { name: /Marina Costa/ });
    expect(options.map((option) => option.textContent)).toEqual([
      'Marina Costamarina.costa@empresa.example',
      'Marina Costamarina.costa.ti@empresa.example',
    ]);
    fireEvent.click(
      within(screen.getByRole('listbox')).getByText('marina.costa.ti@empresa.example'),
    );
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submit }));
    await waitFor(() => {
      expect(requests[0]).toMatch(/^POST 0199f5a2-7c1e-7d40-9a1b-000000000022 null /);
    });
  });

  it('Líder técnico sem a opção Gerente', async () => {
    permissions = { canManagePeople: true, canManageManagers: false };
    await openMembers();

    fireEvent.click(screen.getByRole('button', { name: text.include }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.role }));

    const options = await screen.findAllByRole('option');
    expect(options.map((option) => option.textContent)).toEqual([
      roles.TechLead,
      roles.Contributor,
      roles.Developer,
      roles.Viewer,
    ]);
  });

  it('Membros sem gerenciar', async () => {
    permissions = { canManagePeople: false, canManageManagers: false };
    await openMembers();

    expect(screen.queryByRole('button', { name: text.include })).not.toBeInTheDocument();
    const drawer = await choose('Diego Torres');
    expect(within(drawer).getByRole('combobox', { name: text.details.role })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
    expect(
      within(drawer).queryByRole('button', { name: text.details.remove }),
    ).not.toBeInTheDocument();
  });
});

// Spec: membros-do-projeto, requirement "Detalhe da pessoa pela tela".
describe('Detalhe da pessoa pela tela', () => {
  it('Detalhe da pessoa', async () => {
    // "Ontem" is the day before today: today is 19/10/2026, as in the demonstration data.
    vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
    onTestFinished(() => {
      vi.useRealTimers();
    });
    await openMembers();

    const drawer = await choose('Rafael Souza');

    expect(within(drawer).getByRole('heading', { level: 2, name: 'Rafael Souza' })).toBeVisible();
    expect(drawer).toHaveTextContent('rafael.souza@empresa.example');
    expect(within(drawer).getByRole('combobox', { name: text.details.role })).toHaveTextContent(
      roles.TechLead,
    );
    const values = within(drawer)
      .getAllByRole('definition')
      .map((value) => value.textContent);
    expect(values).toEqual([
      `${text.teams.management} e ${text.teams.development}`,
      '05/10/2026',
      ptBR.dates.yesterday.replace('{{time}}', '17:30'),
    ]);
  });

  it('Trocar o papel pela tela', async () => {
    await openMembers();

    const drawer = await choose('Diego Torres');
    await chooseRole(drawer, roles.Contributor);

    expect(
      await screen.findByText(
        text.messages.roleChanged
          .replace('{{name}}', 'Diego Torres')
          .replace('{{role}}', roles.Contributor),
      ),
    ).toBeInTheDocument();
    expect(within(drawer).getByRole('combobox', { name: text.details.role })).toHaveTextContent(
      roles.Contributor,
    );
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(lines().slice(3, 6)).toEqual([
        `${text.teams.functional}2`,
        'Carla Mendes',
        'Diego Torres',
      ]);
    });
    expect(requests).toEqual([`PUT ${demoPeople.diego.id} Contributor`]);
  });

  it('Último gerente pela tela', async () => {
    await openMembers();

    const drawer = await choose('Marina Costa');
    await chooseRole(drawer, roles.Developer);

    const role = within(drawer).getByRole('combobox', { name: text.details.role });
    await waitFor(() => {
      expect(role).toHaveAccessibleDescription(text.errors.lastManager);
    });
    expect(role).toHaveTextContent(roles.Manager);
    expect(project.members.find((member) => member.id === marina.id)?.role).toBe('Manager');
  });

  it('Remover do projeto pela tela', async () => {
    await openMembers();

    const drawer = await choose('Diego Torres');
    fireEvent.click(within(drawer).getByRole('button', { name: text.details.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    expect(confirm).toHaveAccessibleDescription(
      text.remove.text.replace('{{name}}', 'Diego Torres').replace('{{project}}', projectName),
    );
    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    expect(
      await screen.findByText(text.messages.removed.replace('{{name}}', 'Diego Torres')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(lines()).not.toContain('Diego Torres');
    });
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    expect(requests).toEqual([`DELETE ${demoPeople.diego.id}`]);
  });

  it('Remover a si mesmo pela tela', async () => {
    project.members.push({ ...demoPeople.beatriz, role: 'Manager' });
    const { router } = await openMembers();

    const drawer = await choose('Marina Costa');
    fireEvent.click(within(drawer).getByRole('button', { name: text.details.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    expect(confirm).toHaveAccessibleDescription(
      text.remove.self.replace('{{project}}', projectName),
    );
    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects');
    });
    expect(requests).toEqual([`DELETE ${marina.id}`]);
  });
});

// Spec: projetos, requirement "Cabeçalho do projeto".
describe('Abas do projeto', () => {
  it('Abas do projeto', async () => {
    const { router } = await renderApp(`/projects/${project.id}`);
    const members = await screen.findByRole('tab', { name: /Membros/ });
    expect(screen.getByRole('tab', { name: ptBR.pages.project.title })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    // Chamados with all the tickets of the project, any status (canvas ProjetoChamados), Documentos with the
    // documents not obsolete, Links with all the links, Acessos with all the credentials and, for the manager, Termos de
    // aceite with all the terms (canvas ProjetoCabecalho, ADR-0129).
    await waitFor(() => {
      expect(
        within(screen.getByRole('tablist', { name: ptBR.projects.header.tabs }))
          .getAllByRole('tab')
          .map((tab) => tab.textContent),
      ).toEqual([
        ptBR.pages.project.title,
        ptBR.pages.projectSchedule.title,
        'Chamados4',
        'Documentos4',
        'Links5',
        'Acessos4',
        'Termos de aceite0',
        'Membros5',
      ]);
    });

    fireEvent.click(members);

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(`/projects/${project.id}/members`);
    });
    expect(await screen.findByRole('grid', { name: text.table })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Membros/ })).toHaveAttribute('aria-selected', 'true');
    fireEvent.click(screen.getByRole('tab', { name: ptBR.pages.projectSchedule.title }));
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(`/projects/${project.id}/schedule`);
    });
    expect(screen.getByRole('tab', { name: ptBR.pages.projectSchedule.title })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    fireEvent.click(screen.getByRole('tab', { name: ptBR.pages.project.title }));
    expect(
      await screen.findByRole('region', { name: ptBR.projects.overview.summary }),
    ).toBeVisible();
  });
});

// Spec: membros-do-projeto, requirement "Chamados no detalhe da pessoa".
describe('Chamados no detalhe da pessoa', () => {
  const tickets = () => {
    const [first, second] = demoTickets();
    if (first === undefined || second === undefined) {
      throw new Error('No tickets.');
    }
    return [
      { ...first, assignee: demoPeople.diego, canChangeAssignee: true, canChangeRequester: true },
      { ...second, assignee: demoPeople.diego, canChangeAssignee: true, canChangeRequester: true },
    ];
  };

  it('Chamados da pessoa no detalhe', async () => {
    const asked: string[] = [];
    server.use(
      http.get('/api/tickets', ({ request }) => {
        asked.push(new URL(request.url).search);
        return HttpResponse.json({ items: tickets(), total: 2 });
      }),
    );
    await openMembers();

    const drawer = await choose('Diego Torres');

    const section = await within(drawer).findByRole('region', { name: /Chamados em aberto/ });
    const [first] = tickets();
    expect(
      await within(section).findByRole('link', {
        name: `#${String(first?.number)} ${String(first?.title)}`,
      }),
    ).toBeInTheDocument();
    expect(within(section).getAllByText(text.tickets.assignee)).toHaveLength(2);
    expect(asked.some((query) => query.includes(`personId=${demoPeople.diego.id}`))).toBe(true);
  });

  it('Passar pendências', async () => {
    let current = tickets();
    const saved: unknown[] = [];
    server.use(
      http.get('/api/tickets', () => HttpResponse.json({ items: current, total: current.length })),
      http.get('/api/tickets/assignee-choices', () =>
        HttpResponse.json([demoPeople.marina, demoPeople.rafael].map(ticketChoiceOf)),
      ),
      http.put('/api/tickets/assignee', async ({ request }) => {
        saved.push(await request.json());
        current = [];
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await openMembers();
    const drawer = await choose('Diego Torres');
    fireEvent.click(await within(drawer).findByRole('button', { name: text.tickets.handOver }));
    const dialog = await screen.findByRole('dialog', {
      name: text.tickets.handOverTitle.replace('{{name}}', 'Diego Torres'),
    });

    fireEvent.mouseDown(
      within(dialog).getByRole('combobox', { name: 'Novo responsável (2 chamados)' }),
    );
    fireEvent.click(await screen.findByRole('option', { name: /^Rafael Souza/ }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.tickets.handOver }));

    expect(
      await screen.findByText(text.tickets.done.replace('{{name}}', 'Diego Torres')),
    ).toBeInTheDocument();
    expect(saved).toEqual([
      { numbers: tickets().map((ticket) => ticket.number), assigneeId: demoPeople.rafael.id },
    ]);
    expect(await within(drawer).findByText(text.tickets.none)).toBeInTheDocument();
  });
});
