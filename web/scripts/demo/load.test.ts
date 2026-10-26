// @vitest-environment node
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../src/test/server';
import { loadDemo, screensAddress } from './load.ts';
import {
  demoClients,
  type DemoTicket,
  demoPeople,
  demoProjects,
  demoVendors,
  formerColleague,
  mainProject,
} from './plan.ts';

/**
 * The API of a development installation, simulated: the local sign-in sets a session cookie with the person, and every
 * call is kept as "person METHOD path", so the tests see who did what.
 */
const base = 'http://api.test';
const cookieName = '__Host-fiscoprojects-session';
const calls: string[] = [];
const bodies = new Map<string, unknown[]>();
let localSignIn = true;
let existing: string[] = [];
let existingClients: string[] = [];
let existingVendors: string[] = [];
/** What the Administrator turned off: categories of links, default folders and models (ADR-0032, ADR-0092). */
let inactiveCategories: string[] = [];
let missingFolders: string[] = [];
let missingTemplates: string[] = [];
let ids = 0;
let numbers = 0;
/** People anonymized before the load (ADR-0110). */
let anonymized = 0;
/** The projects the load created, with their ids. */
let created: { code: string; id: string }[] = [];

const personOfEmail = (email: string) =>
  email === formerColleague.email
    ? 'sofia'
    : Object.entries(demoPeople).find(([, value]) => value === email)?.[0];
const idOf = (person: string) => `0199f5a2-0000-7000-8000-${person.padStart(12, '0').slice(-12)}`;
const nextId = () => `0199f5a2-1111-7000-8000-${String(++ids).padStart(12, '0')}`;
const listedId = (kind: string, index: number) =>
  `0199f5a2-${kind}-7000-8000-${String(index + 1).padStart(12, '0')}`;
/** The model the project made from a model takes, saved from the ERP-FIN earlier in the load. */
const erpTemplateId = listedId('4444', 0);

function whoCalls(request: Request): string {
  const cookie = request.headers.get('cookie') ?? '';
  return new RegExp(`${cookieName}=([a-z]+)`).exec(cookie)?.[1] ?? 'anonymous';
}

async function keep(request: Request): Promise<void> {
  const path = new URL(request.url).pathname;
  calls.push(`${whoCalls(request)} ${request.method} ${path}`);
  if (request.method !== 'GET') {
    const text = await request.clone().text();
    let body: unknown;
    try {
      body = text === '' ? null : JSON.parse(text);
    } catch {
      // A form, as a message with its files: kept as it came.
      body = text;
    }
    bodies.set(path, [...(bodies.get(path) ?? []), body]);
  }
}

beforeEach(() => {
  calls.length = 0;
  bodies.clear();
  localSignIn = true;
  existing = [];
  existingClients = [];
  existingVendors = [];
  inactiveCategories = [];
  missingFolders = [];
  missingTemplates = [];
  ids = 0;
  numbers = 0;
  anonymized = 0;
  created = [];
  server.use(
    http.all(`${base}/api/*`, async ({ request }) => {
      await keep(request);
      return undefined;
    }),
    http.get(`${base}/api/session/sign-in-options`, () => HttpResponse.json({ localSignIn })),
    http.post(`${base}/api/session/local`, async ({ request }) => {
      const { email } = (await request.json()) as { email: string };
      const person = personOfEmail(email);
      if (!localSignIn || person === undefined) {
        return new HttpResponse(null, { status: 404 });
      }
      return new HttpResponse(null, {
        status: 204,
        headers: {
          'Set-Cookie': `${cookieName}=${person}; path=/; secure; samesite=strict; httponly`,
        },
      });
    }),
    http.get(`${base}/api/session`, ({ request }) => {
      const person = whoCalls(request);
      return HttpResponse.json({
        id: idOf(person),
        name: person,
        email: `${person}@empresa.example`,
        role: person === 'paulo' ? 'Administrator' : 'User',
        theme: 'System',
      });
    }),
    http.get(`${base}/api/projects`, () => {
      const items = [
        ...existing.map((code, index) => ({ code, id: listedId('7777', index) })),
        ...created,
      ];
      return HttpResponse.json({ items, total: items.length, today: '2026-10-19' });
    }),
    http.post(`${base}/api/projects`, async ({ request }) => {
      const { code } = (await request.json()) as { code: string };
      const id = nextId();
      created.push({ code, id });
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.get(`${base}/api/users`, () => HttpResponse.json({ items: [], total: anonymized })),
    http.get(`${base}/api/clients`, () =>
      HttpResponse.json({
        items: existingClients.map((name, index) => ({
          id: listedId('5555', index),
          name,
          cnpj: null,
          logoUrl: null,
          observation: null,
          status: 'Active',
        })),
        total: existingClients.length,
      }),
    ),
    http.get(`${base}/api/admin/vendors`, () =>
      HttpResponse.json(
        existingVendors.map((name, index) => ({
          id: listedId('6666', index),
          name,
          portalUrl: null,
          isActive: true,
        })),
      ),
    ),
    // The models saved by the manager of the ERP-FIN and of the TRIB-RS (ADR-0017).
    http.get(`${base}/api/project-templates`, () =>
      HttpResponse.json(
        ['Implantação de ERP', 'Revisão tributária de filial']
          .map((name, index) => ({
            id: listedId('4444', index),
            name,
            description: null,
            isActive: true,
            phases: [],
          }))
          .filter((template) => !missingTemplates.includes(template.name)),
      ),
    ),
    // The six default folders every new project gets (ADR-0031).
    http.get(`${base}/api/projects/:id/documents/folders`, () =>
      HttpResponse.json({
        folders: [
          '01 Gestão do projeto',
          '02 Contratos e compras',
          '03 Requisitos e especificações',
          '04 Atas de reunião',
          '05 Entregas e manuais',
          '06 Termos de aceite',
        ]
          .map((name, index) => ({
            id: `0199f5a2-2222-7000-8000-${String(index + 1).padStart(12, '0')}`,
            name,
            isAcceptance: index === 5,
            count: 0,
          }))
          .filter((folder) => !missingFolders.includes(folder.name)),
        total: 0,
        permissions: { canUpload: true, canApprove: true, canDelete: true },
        maxBytes: 25 * 1024 * 1024,
      }),
    ),
    // The categories of links of the migration (ADR-0032).
    http.get(`${base}/api/link-categories`, () =>
      HttpResponse.json(
        ['Ambientes', 'Outros', 'Painéis', 'Repositórios', 'Sistemas']
          .map((name, index) => ({
            id: `0199f5a2-3333-7000-8000-${String(index + 1).padStart(12, '0')}`,
            name,
            isActive: true,
          }))
          .filter((category) => !inactiveCategories.includes(category.name)),
      ),
    ),
    http.post(`${base}/api/tickets`, () =>
      HttpResponse.json({ id: nextId(), number: ++numbers }, { status: 201 }),
    ),
    http.post(`${base}/api/*`, () => HttpResponse.json({ id: nextId() }, { status: 201 })),
    http.put(`${base}/api/*`, () => new HttpResponse(null, { status: 204 })),
  );
});

const callsBy = (pattern: RegExp) => calls.filter((call) => pattern.test(call));
const people = (pattern: RegExp) =>
  [...new Set(callsBy(pattern).map((call) => call.split(' ')[0]))].sort();

// Spec: dados-de-demonstracao, requirement "Carga dos dados de demonstração".
describe('Carga dos dados de demonstração', () => {
  // Spec: dados-pessoais, requirement "Pessoa anonimizada em todo o sistema" (ADR-0110).
  it('Pessoa que saiu da empresa', async () => {
    const lines: string[] = [];

    await loadDemo(base, (line) => lines.push(line));

    const sofia = idOf('sofia');
    const main = created.find((project) => project.code === mainProject)?.id ?? '';
    expect(bodies.get(`/api/projects/${main}/members`)).toContainEqual({
      userId: sofia,
      directoryId: null,
      role: 'Developer',
    });
    expect(callsBy(/^sofia POST /)).toEqual(['sofia POST /api/tickets']);
    expect((bodies.get('/api/tickets') ?? []).at(-1)).toEqual({
      projectId: main,
      ...formerColleague.ticket,
    });
    const number = String(numbers);
    // The tech lead answers mentioning her; then the Administrator deactivates and anonymizes her.
    const answer = (bodies.get(`/api/tickets/${number}/messages`) ?? []).at(-1) as string;
    expect(answer).toContain(`"id":"${sofia}"`);
    expect(calls.filter((call) => /sofia|messages$/.test(call)).slice(-3)).toEqual([
      `rafael POST /api/tickets/${number}/messages`,
      `paulo POST /api/users/${sofia}/deactivate`,
      `paulo POST /api/users/${sofia}/anonymization`,
    ]);
    expect(lines).toContain(
      `sofia saiu da empresa: abriu o chamado #${number} no ${mainProject} e foi desativada e anonimizada.`,
    );
  });

  it('Só a pessoa que saiu da empresa', async () => {
    existing = demoProjects.map((project) => project.code);
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    expect(outcome).toMatchObject({ loaded: true, projects: 0, items: 0 });
    expect(callsBy(/ POST \/api\/projects$/)).toEqual([]);
    expect(callsBy(/^paulo POST \/api\/users\/.+\/anonymization$/)).toHaveLength(1);
    expect(bodies.get(`/api/projects/${listedId('7777', 0)}/members`)).toEqual([
      { userId: idOf('sofia'), directoryId: null, role: 'Developer' },
    ]);
    expect(lines.at(-1)).toBe('Abra as telas em http://localhost:5173 (npm run dev).');
  });

  it('Ninguém mais sai da empresa', async () => {
    anonymized = 1;

    await loadDemo(base);

    expect(
      (bodies.get('/api/session/local') ?? []).map((body) => (body as { email: string }).email),
    ).not.toContain(formerColleague.email);
    expect(callsBy(/anonymization$/)).toEqual([]);
  });

  it('Carregar dados de demonstração', async () => {
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    const items = demoProjects.flatMap((project) => project.phases).flatMap((phase) => phase.items);
    expect(outcome).toEqual({
      loaded: true,
      firstDay: '2026-10-05',
      projects: demoProjects.length,
      items: items.length,
    });
    // Everyone of the demo directory signs in, but the people who never signed in (ADR-0078).
    expect(people(/POST \/api\/session\/local$/)).toEqual(['anonymous']);
    // And the colleague who left the company, once (ADR-0110).
    expect(
      (bodies.get('/api/session/local') ?? [])
        .map((body) => (body as { email: string }).email)
        .sort(),
    ).toEqual([...Object.values(demoPeople), formerColleague.email].sort());
    // The Administrator gives the roles and creates the clients, the projects and their members.
    expect(callsBy(/^paulo PUT \/api\/users\/.+\/role$/)).toEqual([
      `paulo PUT /api/users/${idOf('marina')}/role`,
      `paulo PUT /api/users/${idOf('beatriz')}/role`,
      `paulo PUT /api/users/${idOf('rafael')}/role`,
    ]);
    // The tech lead gets the role Tech lead in the system before the projects (ADR-0097).
    expect((bodies.get(`/api/users/${idOf('rafael')}/role`) ?? [])[0]).toEqual({
      role: 'TechLead',
    });
    expect(callsBy(/^paulo POST \/api\/clients$/)).toHaveLength(demoClients.length);
    expect(callsBy(/^paulo POST \/api\/projects$/)).toHaveLength(demoProjects.length);
    expect(people(/POST \/api\/projects\/[^/]+\/members$/)).toEqual(['paulo']);
    // Each project with its manager, Marina Costa when the plan names none.
    const created = (bodies.get('/api/projects') ?? []) as { code: string; managerId: string }[];
    expect(created.map((project) => [project.code, project.managerId])).toEqual(
      demoProjects.map((project) => [project.code, idOf(project.manager ?? 'marina')]),
    );
    // The managers build the schedules, mark the milestones and change the status.
    expect(people(/\/schedule\/(phases|items)$/)).toEqual(['beatriz', 'marina']);
    expect(people(/\/reached$/)).toEqual(['beatriz', 'marina']);
    expect(people(/\/projects\/[^/]+\/status$/)).toEqual(['beatriz', 'marina']);
    // Each person in charge updates the progress of the task; the manager, of the tasks of no one.
    expect(people(/\/progress$/)).toEqual(['beatriz', 'carla', 'diego', 'marina', 'rafael']);
    // The dates follow the day it runs: 19/10/2026 starts the main project on 05/10/2026.
    const first = [...bodies.entries()].find(([path]) => path.endsWith('/schedule/items'))?.[1][0];
    expect(first).toMatchObject({
      name: 'Reunião de abertura',
      start: '2026-10-05',
      ownerId: idOf('marina'),
    });
    expect(lines.at(-2)).toContain('2026-10-05');
    // The tests of the client are in its charge, without a person (ADR-0102).
    const postedItems = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/schedule/items'))
      .flatMap(([, posted]) => posted);
    expect(
      postedItems.find(
        (item) => (item as { name?: string }).name === 'Testes com a equipe do cliente',
      ),
    ).toMatchObject({
      ownerId: null,
      ownedByClient: true,
    });
    // And where to open the screens, with the code as it is now.
    expect(screensAddress).toBe('http://localhost:5173');
    expect(lines.at(-1)).toBe('Abra as telas em http://localhost:5173 (npm run dev).');
  });

  it('Só os dados que faltam', async () => {
    // A demonstration loaded before Cronograma-3.3: the first five projects, their clients and the vendors.
    const before = demoProjects.slice(0, 5).map((project) => project.code);
    existing = before;
    existingClients = demoClients.slice(0, 3);
    existingVendors = demoVendors.map((vendor) => vendor.name);
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    const missing = demoProjects.slice(5);
    expect(outcome).toMatchObject({ loaded: true, projects: missing.length });
    const created = (bodies.get('/api/projects') ?? []) as { code: string; clientId: string }[];
    expect(created.map((project) => project.code)).toEqual(missing.map((project) => project.code));
    // Only the new clients, and a new project of an old client goes to the one already there.
    expect(
      ((bodies.get('/api/clients') ?? []) as { name: string }[]).map((client) => client.name),
    ).toEqual(demoClients.slice(3));
    expect(created.find((project) => project.code === 'MIGRA-ALFA')?.clientId).toBe(
      listedId('5555', 0),
    );
    expect(callsBy(/ POST \/api\/admin\/vendors$/)).toEqual([]);
    expect(lines).toContain(`Projetos que já existiam, mantidos como estão: ${before.join(', ')}.`);
    expect(lines.at(-1)).toBe('Abra as telas em http://localhost:5173 (npm run dev).');
  });

  it('Categoria, pasta ou modelo desativado', async () => {
    inactiveCategories = ['Painéis'];
    missingFolders = ['04 Atas de reunião'];
    missingTemplates = ['Implantação de ERP'];
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    expect(outcome).toMatchObject({ loaded: true, projects: demoProjects.length });
    // The links of Painéis go to Outros, the minutes to the first folder, and the project of the model is blank.
    const links = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/links'))
      .flatMap(([, saved]) => saved) as { title: string; categoryId: string }[];
    expect(links.find((link) => link.title === 'Qualidade dos cadastros')?.categoryId).toBe(
      '0199f5a2-3333-7000-8000-000000000002',
    );
    expect(lines).toContain(
      'O link Qualidade dos cadastros foi para Outros: Painéis não é uma categoria ativa.',
    );
    expect(lines).toContain(
      'Ata da reunião de abertura.txt foi para 01 Gestão do projeto: ERP-FIN não tem a pasta 04 Atas de reunião.',
    );
    const created = (bodies.get('/api/projects') ?? []) as { code: string; templateId?: string }[];
    expect(created.find((project) => project.code === 'ERP-EPS')?.templateId).toBeUndefined();
    expect(lines).toContain(
      'ERP-EPS foi criado em branco: o modelo Implantação de ERP não está entre os modelos ativos.',
    );
  });

  it('Dados já carregados', async () => {
    existing = demoProjects.map((project) => project.code);
    anonymized = 1;
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    expect(outcome).toEqual({ loaded: false, reason: 'already-loaded' });
    expect(calls.filter((call) => !call.includes(' GET '))).toEqual([
      'anonymous POST /api/session/local',
    ]);
    expect(lines).toEqual([
      expect.stringContaining('já estão carregados'),
      expect.stringContaining('docker compose down -v'),
      'Abra as telas em http://localhost:5173 (npm run dev).',
    ]);
  });

  it('Sem entrada local', async () => {
    localSignIn = false;

    await expect(loadDemo(base)).rejects.toThrow('A entrada local está desligada');
    expect(calls).toEqual(['anonymous GET /api/session/sign-in-options']);
  });

  it('API reiniciando', async () => {
    server.use(
      http.get(
        `${base}/api/session/sign-in-options`,
        () => new HttpResponse(null, { status: 503 }),
      ),
    );

    await expect(loadDemo(base)).rejects.toThrow(
      'A API não respondeu às formas de entrada (503); ela pode estar reiniciando.',
    );
  });
});

// Spec: dados-de-demonstracao, requirement "Linha de base na demonstração".
describe('Linha de base na demonstração', () => {
  it('Linha de base na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager saves the baseline of ERP-FIN and then puts off Parametrizar regras de ICMS, in the same project.
    const changes = callsBy(/ PUT .*\/schedule\/(baseline|items\/[^/]+\/dates)$/);
    expect(changes).toHaveLength(4);
    const [saved, moved, savedTms, movedTms] = changes;
    // The TMS-DELTA too, by its manager.
    expect(savedTms).toMatch(/^beatriz PUT \/api\/projects\/[^/]+\/schedule\/baseline$/);
    expect(movedTms?.startsWith(savedTms?.replace(/\/baseline$/, '') ?? '')).toBe(true);
    expect(saved).toMatch(/^marina PUT \/api\/projects\/[^/]+\/schedule\/baseline$/);
    expect(moved).toMatch(/^marina PUT \/api\/projects\/[^/]+\/schedule\/items\/[^/]+\/dates$/);
    expect(moved?.startsWith(saved?.replace(/\/baseline$/, '') ?? '')).toBe(true);
    // Two days later than planned, from 13/10/2026 to 15/10/2026: the API pushes the items after it.
    const dates = [...bodies.entries()].find(([path]) => path.endsWith('/dates'))?.[1];
    expect(dates).toEqual([{ start: '2026-10-15', end: null }]);
    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    expect(main?.baseline).toBe(true);
    expect(
      main?.phases.flatMap((phase) => phase.items).filter((item) => item.movedTo !== undefined),
    ).toMatchObject([{ name: 'Parametrizar regras de ICMS', day: 8, movedTo: 10 }]);
  });
});

// Spec: dados-de-demonstracao, requirement "Modelos na demonstração".
describe('Modelos na demonstração', () => {
  it('Modelos na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager saves the two models, from ERP-FIN and from TRIB-RS.
    expect(people(/ POST .*\/schedule\/template$/)).toEqual(['marina']);
    const names = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/schedule/template'))
      .flatMap(([, saved]) => saved.map((body) => (body as { name: string }).name));
    expect(names.sort()).toEqual(['Implantação de ERP', 'Revisão tributária de filial']);
  });
});

const plannedTickets = () => demoProjects.flatMap((project) => project.tickets ?? []);

/** The number a ticket of the plan gets, loaded in order into an empty installation. */
function numberOf(title: string): number {
  const index = plannedTickets().findIndex((ticket) => ticket.title === title);
  if (index < 0) {
    throw new Error(`No ticket ${title} in the plan.`);
  }
  return index + 1;
}

/** The status a ticket of the plan ends in, by the flow of the MVP (ADR-0024): a cancelled ticket apart. */
function finalStatus(ticket: DemoTicket): string {
  let status = 'Open';
  const moves: Record<string, Record<string, string>> = {
    StartTriage: { Open: 'Triage' },
    StartWork: { Triage: 'InProgress' },
    AskForInformation: { InProgress: 'WaitingForRequester' },
    Reply: { WaitingForRequester: 'InProgress' },
    Resolve: { InProgress: 'Resolved' },
    ConfirmAndClose: { Resolved: 'Closed' },
    Reopen: { Resolved: 'InProgress' },
    Cancel: { Open: 'Cancelled', Triage: 'Cancelled' },
  };
  for (const step of ticket.steps ?? []) {
    if (step.kind === 'message') {
      // The first answer of who serves takes an open ticket to Em andamento; the answer of the requester takes back
      // one waiting for it.
      if (status === 'Open' && step.by !== ticket.by) {
        status = 'InProgress';
      } else if (status === 'WaitingForRequester' && step.by === ticket.by) {
        status = 'InProgress';
      }
    } else if (step.kind === 'action') {
      const next = moves[step.action]?.[status];
      expect(next, `${ticket.title}: ${step.action} em ${status}`).toBeDefined();
      status = next ?? status;
    }
  }
  return status;
}

/** The calls a ticket of the plan makes, in order, as "person METHOD path" (the assignee goes by the list). */
function expectedCalls(ticket: DemoTicket, number: number): string[] {
  const base = `/api/tickets/${String(number)}`;
  const files = (ticket.files ?? []).length > 0 ? [`${ticket.by} POST ${base}/attachments`] : [];
  return [
    ...files,
    ...(ticket.steps ?? []).flatMap((step) => {
      switch (step.kind) {
        case 'message':
          return [`${step.by} POST ${base}/messages`];
        case 'action':
          return [`${step.by} POST ${base}/transitions`];
        case 'priority':
          return [`${step.by} PUT ${base}/priority`];
        case 'follow':
          return [`${step.by} PUT ${base}/following`];
        case 'assign':
          return [`${step.by} PUT /api/tickets/assignee`];
        case 'transfer':
          return [`${step.by} PUT ${base}/team`];
        case 'vendor':
          return [`${step.by} POST ${base}/vendor-tickets`];
        case 'vendorUpdate':
          return [`${step.by} PUT ${base}/vendor-tickets/id`];
      }
    }),
  ];
}

// Spec: dados-de-demonstracao, requirement "Chamados na demonstração".
describe('Chamados na demonstração', () => {
  it('Chamados na demonstração', async () => {
    // Only the tickets of the projects: the colleague who left the company has her own test.
    anonymized = 1;
    await loadDemo(base, () => undefined);

    // Each ticket opened by its person, before the projects get their status.
    expect(people(/ POST \/api\/tickets$/)).toEqual([
      'beatriz',
      'carla',
      'diego',
      'marina',
      'paulo',
      'rafael',
    ]);
    const opened = (bodies.get('/api/tickets') ?? []) as { title: string; team: string }[];
    expect(opened.map((ticket) => ticket.title)).toEqual(
      plannedTickets().map((ticket) => ticket.title),
    );
    expect(new Set(opened.map((ticket) => ticket.team))).toEqual(
      new Set(['Functional', 'Development']),
    );
    const firstStatus = calls.findIndex((call) => / PUT .*\/status$/.test(call));
    expect(calls.findIndex((call) => call.endsWith('POST /api/tickets'))).toBeLessThan(firstStatus);
    // Every project has tickets, the completed one with them all closed (ADR-0043).
    expect(demoProjects.filter((project) => (project.tickets ?? []).length === 0)).toEqual([]);
    for (const project of demoProjects.filter((item) => item.status === 'Completed')) {
      for (const status of (project.tickets ?? []).map(finalStatus)) {
        expect(['Closed', 'Cancelled']).toContain(status);
      }
    }
    // Nobody opens a ticket as a viewer (ADR-0044).
    for (const project of demoProjects) {
      const viewers = (project.members ?? [])
        .filter(([, role]) => role === 'Viewer')
        .map(([person]) => person);
      expect((project.tickets ?? []).filter((ticket) => viewers.includes(ticket.by))).toEqual([]);
    }
  });
});

// Spec: dados-de-demonstracao, requirement "Conversa na demonstração".
describe('Conversa na demonstração', () => {
  it('Conversa na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // A long conversation in the first ticket: the developer, the tech lead, the requester and the manager.
    const first = callsBy(/ POST \/api\/tickets\/1\/messages$/).map((call) => call.split(' ')[0]);
    expect(first).toEqual(['diego', 'rafael', 'carla', 'marina', 'diego', 'rafael', 'diego']);
    const forms = (bodies.get('/api/tickets/1/messages') ?? []) as string[];
    // The requester mentions the developer and sends the XML; then a capture and a PDF.
    expect(forms[2]).toContain(`"id":"${idOf('diego')}"`);
    expect(forms[2]).toContain('filename="nota-3401.xml"');
    expect(forms[4]).toContain('filename="contorno-homologacao.png"');
    expect(forms[4]).toContain('PNG');
    expect(forms[6]).toContain('filename="log-transmissao-3381.pdf"');
    expect(forms[6]).toContain('%PDF-1.4');
    // Other tickets with conversations too, and files in the description, before any message.
    const conversations = plannedTickets().filter(
      (ticket) => (ticket.steps ?? []).filter((step) => step.kind === 'message').length >= 2,
    );
    expect(conversations.length).toBeGreaterThanOrEqual(5);
    expect(callsBy(/ POST \/api\/tickets\/7\/attachments$/)).toEqual([
      'carla POST /api/tickets/7/attachments',
    ]);
    expect((bodies.get('/api/tickets/7/attachments') ?? [])[0]).toContain(
      'filename="extrato-15-10.csv"',
    );
  });
});

// Spec: dados-de-demonstracao, requirement "Status na demonstração".
describe('Status na demonstração', () => {
  it('Status na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // What happens to each ticket, in the order of the plan, each by its person.
    const assignees = (bodies.get('/api/tickets/assignee') ?? []) as { numbers: number[] }[];
    plannedTickets().forEach((ticket, index) => {
      const number = index + 1;
      // Each change of assignee goes with its own body, in the order of the calls.
      let assignee = -1;
      const made = calls
        .filter((call) => {
          if (call.endsWith('PUT /api/tickets/assignee')) {
            assignee += 1;
            return assignees[assignee]?.numbers.includes(number) ?? false;
          }
          return call.includes(`/api/tickets/${String(number)}/`);
        })
        .map((call) => call.replace(/vendor-tickets\/[0-9a-f-]+$/, 'vendor-tickets/id'));
      expect(made, ticket.title).toEqual(expectedCalls(ticket, number));
    });
    // Every status of the flow, a cancelled ticket and a reopened one (ADR-0024).
    expect(new Set(plannedTickets().map(finalStatus))).toEqual(
      new Set([
        'Open',
        'Triage',
        'InProgress',
        'WaitingForRequester',
        'Resolved',
        'Closed',
        'Cancelled',
      ]),
    );
    expect(
      plannedTickets().some((ticket) =>
        ticket.steps?.some((step) => step.kind === 'action' && step.action === 'Reopen'),
      ),
    ).toBe(true);
    const [ask] = (bodies.get('/api/tickets/5/transitions') ?? []) as {
      action: string;
      body: string;
    }[];
    expect(ask?.action).toBe('AskForInformation');
    expect(ask?.body).toContain('data de vigência');
  });
});

// Spec: dados-de-demonstracao, requirement "Atendimento iniciado na demonstração" (ADR-0098).
describe('Atendimento iniciado na demonstração', () => {
  it('Atendimento iniciado na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // A ticket goes to triage and then to work by the buttons, by who serves its team, with no message before.
    const started = plannedTickets().filter((ticket) =>
      (ticket.steps ?? []).some((step) => step.kind === 'action' && step.action === 'StartWork'),
    );
    expect(started.map((ticket) => ticket.title)).toEqual(['Definir o escopo do portal']);
    const [first] = started;
    if (first === undefined) {
      throw new Error('No ticket with the work started.');
    }
    const number = plannedTickets().indexOf(first) + 1;
    expect(
      (
        (bodies.get(`/api/tickets/${String(number)}/transitions`) ?? []) as { action: string }[]
      ).map((body) => body.action),
    ).toEqual(['StartTriage', 'StartWork']);
    expect(callsBy(new RegExp(`/api/tickets/${String(number)}/transitions$`))).toEqual([
      `carla POST /api/tickets/${String(number)}/transitions`,
      `carla POST /api/tickets/${String(number)}/transitions`,
    ]);
    expect(finalStatus(first)).toBe('InProgress');
  });
});

// Spec: dados-de-demonstracao, requirement "Prazos na demonstração".
describe('Prazos na demonstração', () => {
  it('Prazos na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Opened on the day of the load: the critical ones are due on the next business day, the others later.
    const opened = (bodies.get('/api/tickets') ?? []) as { priority: string }[];
    expect(opened.map((ticket) => ticket.priority)).toEqual(
      expect.arrayContaining(['Critical', 'High', 'Medium', 'Low']),
    );
    // And some wait for the requester, with their deadlines paused.
    expect(
      plannedTickets().filter((ticket) => finalStatus(ticket) === 'WaitingForRequester').length,
    ).toBeGreaterThanOrEqual(2);
  });
});

// Spec: dados-de-demonstracao, requirement "Trocas na demonstração".
describe('Trocas na demonstração', () => {
  it('Trocas na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager assigns the sixth ticket to the developer, and the manager of the TMS-DELTA one of hers to the
    // contributor; the tech lead sends the fourth to Funcional.
    expect(callsBy(/ PUT \/api\/tickets\/assignee$/)).toEqual([
      'marina PUT /api/tickets/assignee',
      'beatriz PUT /api/tickets/assignee',
    ]);
    expect(bodies.get('/api/tickets/assignee')).toEqual([
      { numbers: [6], assigneeId: idOf('diego') },
      {
        numbers: [numberOf('Liberar usuário do cliente no TMS de homologação')],
        assigneeId: idOf('carla'),
      },
    ]);
    expect(callsBy(/ PUT \/api\/tickets\/\d+\/team$/)).toEqual(['rafael PUT /api/tickets/4/team']);
    expect(bodies.get('/api/tickets/4/team')).toEqual([{ team: 'Functional' }]);
  });
});

// Spec: dados-de-demonstracao, requirement "Prioridade e acompanhamento na demonstração".
describe('Prioridade e acompanhamento na demonstração', () => {
  it('Prioridade e acompanhamento na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager raises the priority of the first ticket, opened as Média; the Administrator follows the tenth.
    expect((bodies.get('/api/tickets') ?? [])[0]).toMatchObject({ priority: 'Medium' });
    const cte = numberOf('CT-e rejeitado por divergência no valor do frete');
    expect(callsBy(/\/priority$/)).toEqual([
      'marina PUT /api/tickets/1/priority',
      `beatriz PUT /api/tickets/${String(cte)}/priority`,
    ]);
    expect(bodies.get('/api/tickets/1/priority')).toEqual([{ priority: 'High' }]);
    expect(callsBy(/\/following$/)).toEqual(['paulo PUT /api/tickets/10/following']);
  });
});

// Spec: dados-de-demonstracao, requirement "Fornecedores na demonstração".
describe('Fornecedores na demonstração', () => {
  it('Fornecedores na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Two fictitious vendors, by the Administrator; the tech lead and the developer register tickets at them.
    const cte = numberOf('CT-e rejeitado por divergência no valor do frete');
    const reinf = numberOf('Evento R-4020 rejeitado por CNPJ do beneficiário');
    expect(callsBy(/ POST \/api\/admin\/vendors$/)).toEqual([
      'paulo POST /api/admin/vendors',
      'paulo POST /api/admin/vendors',
    ]);
    const vendors = (bodies.get('/api/admin/vendors') ?? []) as { name: string }[];
    expect(vendors.map((vendor) => vendor.name)).toEqual([
      'Emissor Exemplo',
      'Nuvem Fiscal Exemplo',
    ]);
    const update = (number: number) =>
      `rafael PUT /api/tickets/${String(number)}/vendor-tickets/id`;
    expect(
      callsBy(/\/vendor-tickets/).map((call) =>
        call.replace(/vendor-tickets\/[0-9a-f-]+$/, 'vendor-tickets/id'),
      ),
    ).toEqual([
      'rafael POST /api/tickets/1/vendor-tickets',
      update(1),
      update(1),
      update(1),
      'rafael POST /api/tickets/10/vendor-tickets',
      update(10),
      `diego POST /api/tickets/${String(cte)}/vendor-tickets`,
      `diego PUT /api/tickets/${String(cte)}/vendor-tickets/id`,
      `diego POST /api/tickets/${String(reinf)}/vendor-tickets`,
    ]);
    expect(bodies.get('/api/tickets/1/vendor-tickets')).toEqual([
      expect.objectContaining({
        code: 'FX-20931',
        openedOn: '2026-10-19',
        status: 'Open',
        link: 'https://suporte.emissor.example/chamados/FX-20931',
      }),
    ]);
  });
});

// Spec: dados-de-demonstracao, requirement "Documentos na demonstração".
describe('Documentos na demonstração', () => {
  it('Documentos na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Each document sent by a person of the project who sends documents, never the viewer (ADR-0045).
    const sent = callsBy(/ POST \/api\/projects\/[^/]+\/documents$/);
    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    const documents = main?.documents ?? [];
    const everyDocument = demoProjects.flatMap((project) => project.documents ?? []);
    const versions = everyDocument.reduce(
      (total, document) => total + (document.versions?.length ?? 0),
      0,
    );
    expect(sent).toHaveLength(everyDocument.length + versions);
    expect(people(/ POST \/api\/projects\/[^/]+\/documents$/)).toEqual([
      'beatriz',
      'carla',
      'diego',
      'marina',
      'rafael',
    ]);
    // In more than one folder, one of them in version 2, with statuses and links.
    expect(new Set(documents.map((document) => document.folder)).size).toBeGreaterThan(1);
    expect(documents.filter((document) => (document.versions?.length ?? 0) > 0)).toHaveLength(1);
    expect(new Set(documents.map((document) => document.status?.to))).toEqual(
      new Set(['Approved', 'InReview', 'Obsolete', undefined]),
    );
    const forms = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/documents'))
      .flatMap(([, saved]) => saved) as string[];
    expect(
      forms.some(
        (form) => form.includes('filename="Plano do projeto.pdf"') && form.includes('%PDF-1.4'),
      ),
    ).toBe(true);
    // The approvals by the manager and the tech lead; the links after the documents.
    expect(people(/ PUT .*\/documents\/[^/]+\/status$/)).toEqual([
      'beatriz',
      'carla',
      'marina',
      'rafael',
    ]);
    expect(callsBy(/ PUT .*\/documents\/[^/]+\/link$/)).toHaveLength(
      everyDocument.filter((document) => document.link !== undefined).length,
    );
    const links = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/link'))
      .flatMap(([, saved]) => saved) as { phaseId: string | null; itemId: string | null }[];
    expect(links.some((link) => link.phaseId !== null)).toBe(true);
    expect(links.some((link) => link.itemId !== null)).toBe(true);
    // Before the project gets its status.
    const lastDocument = calls.findLastIndex((call) => call.includes('/documents'));
    const status = calls.findLastIndex((call) => /PUT \/api\/projects\/[^/]+\/status$/.test(call));
    expect(lastDocument).toBeLessThan(status);
  });
});

// Spec: dados-de-demonstracao, requirement "Links na demonstração".
describe('Links na demonstração', () => {
  it('Links na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Each link included by a person of the project who keeps links, never the viewer (ADR-0045).
    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    const links = main?.links ?? [];
    const everyLink = demoProjects.flatMap((project) => project.links ?? []);
    expect(callsBy(/ POST \/api\/projects\/[^/]+\/links$/)).toHaveLength(everyLink.length);
    expect(people(/ POST \/api\/projects\/[^/]+\/links$/)).toEqual([
      'beatriz',
      'carla',
      'diego',
      'marina',
      'rafael',
    ]);
    expect(new Set(links.map((link) => link.category))).toEqual(
      new Set(['Ambientes', 'Sistemas', 'Painéis', 'Repositórios']),
    );
    const sent = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/links'))
      .flatMap(([, saved]) => saved) as { title: string; url: string; categoryId: string }[];
    expect(sent.every((link) => link.url.startsWith('https://'))).toBe(true);
    expect(new Set(sent.map((link) => link.categoryId)).size).toBe(
      new Set(everyLink.map((link) => link.category)).size,
    );
    // Before the project gets its status.
    const lastLink = calls.findLastIndex((call) => call.endsWith('/links'));
    const status = calls.findLastIndex((call) => /PUT \/api\/projects\/[^/]+\/status$/.test(call));
    expect(lastLink).toBeLessThan(status);
  });
});

// Spec: dados-de-demonstracao, requirement "Acessos na demonstração".
describe('Acessos na demonstração', () => {
  it('Acessos na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Each credential included by a person of the project who keeps credentials, never the viewer (ADR-0045).
    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    const credentials = main?.credentials ?? [];
    expect(callsBy(/ POST \/api\/projects\/[^/]+\/credentials$/)).toHaveLength(
      demoProjects.flatMap((project) => project.credentials ?? []).length,
    );
    expect(people(/ POST \/api\/projects\/[^/]+\/credentials$/)).toEqual([
      'beatriz',
      'carla',
      'diego',
      'marina',
      'rafael',
    ]);
    expect(new Set(credentials.map((credential) => credential.environment))).toEqual(
      new Set(['Produção', 'Homologação', 'Fornecedores']),
    );
    // Fictitious passwords made when it runs, each one different.
    const sent = [...bodies.entries()]
      .filter(([path]) => path.endsWith('/credentials'))
      .flatMap(([, saved]) => saved) as { password: string }[];
    expect(sent.every((credential) => credential.password.startsWith('Demo-'))).toBe(true);
    expect(new Set(sent.map((credential) => credential.password)).size).toBe(sent.length);
    expect(JSON.stringify(demoProjects)).not.toContain('password');
  });
});

// Spec: dados-de-demonstracao, requirement "Termos na demonstração".
describe('Termos na demonstração', () => {
  it('Termos na demonstração', async () => {
    await loadDemo(base, () => undefined);

    const main = demoProjects.find((project) => project.code === 'ERP-FIN');
    const terms = main?.acceptances ?? [];
    const requests = demoProjects
      .flatMap((project) => project.acceptances ?? [])
      .filter((term) => term.attached !== true);
    // The requests and the attached term by the managers; the answers by the person of each request (ADR-0045).
    expect(callsBy(/ POST \/api\/projects\/[^/]+\/acceptances$/)).toHaveLength(requests.length);
    expect(people(/ POST \/api\/projects\/[^/]+\/acceptances$/)).toEqual(['beatriz', 'marina']);
    expect(people(/ POST \/api\/projects\/[^/]+\/acceptances\/attached$/)).toEqual(['marina']);
    expect(people(/ POST .*\/acceptances\/[^/]+\/signed$/)).toEqual(['carla']);
    expect(people(/ POST .*\/acceptances\/[^/]+\/refusal$/)).toEqual(['carla']);
    // Signed, attached, refused and pending, each with a PDF made by the command.
    expect(terms.some((term) => term.attached === true)).toBe(true);
    expect(terms.some((term) => term.answer !== undefined && 'signed' in term.answer)).toBe(true);
    expect(terms.some((term) => term.answer !== undefined && 'refused' in term.answer)).toBe(true);
    expect(terms.some((term) => term.attached !== true && term.answer === undefined)).toBe(true);
    const forms = [...bodies.entries()]
      .filter(([path]) => /\/acceptances(\/attached|\/[^/]+\/signed)?$/.test(path))
      .flatMap(([, saved]) => saved) as string[];
    expect(forms.every((form) => form.includes('%PDF-1.4'))).toBe(true);
    // Before the project gets its status.
    const lastTerm = calls.findLastIndex((call) => call.includes('/acceptances'));
    const status = calls.findLastIndex((call) => /PUT \/api\/projects\/[^/]+\/status$/.test(call));
    expect(lastTerm).toBeLessThan(status);
  });
});

// Spec: dados-de-demonstracao, requirement "Demonstração mais rica".
describe('Demonstração mais rica', () => {
  it('Projetos em todos os status', async () => {
    await loadDemo(base, () => undefined);

    // A new project starts in Planejamento; the others go to their status at the end, by their managers.
    expect(new Set(demoProjects.map((project) => project.status ?? 'Planning'))).toEqual(
      new Set(['Planning', 'InProgress', 'Paused', 'Completed', 'Cancelled']),
    );
    const statuses = [...bodies.entries()]
      .filter(([path]) => /\/api\/projects\/[^/]+\/status$/.test(path))
      .flatMap(([, saved]) => saved as { status: string }[])
      .map((body) => body.status);
    expect(new Set(statuses)).toEqual(new Set(['InProgress', 'Paused', 'Completed', 'Cancelled']));
    // Six clients, internal projects too, and every project with tickets.
    expect(new Set(demoProjects.map((project) => project.client))).toEqual(
      new Set([...demoClients, null]),
    );
    expect(demoProjects.filter((project) => (project.tickets ?? []).length === 0)).toEqual([]);
    // Documents, links, credentials and terms outside the main project too.
    const others = demoProjects.filter((project) => project.code !== 'ERP-FIN');
    for (const of of [
      (project: (typeof demoProjects)[number]) => project.documents,
      (project: (typeof demoProjects)[number]) => project.links,
      (project: (typeof demoProjects)[number]) => project.credentials,
      (project: (typeof demoProjects)[number]) => project.acceptances,
    ]) {
      expect(others.some((project) => (of(project) ?? []).length > 0)).toBe(true);
    }
  });

  it('Projeto a partir de um modelo', async () => {
    await loadDemo(base, () => undefined);

    // The Administrator creates the project from the model saved from the ERP-FIN earlier in the load.
    const fromModel = demoProjects.filter((project) => project.fromTemplate !== undefined);
    expect(fromModel.map((project) => project.fromTemplate?.name)).toEqual(['Implantação de ERP']);
    expect(fromModel.every((project) => project.phases.length === 0)).toBe(true);
    const created = (bodies.get('/api/projects') ?? []) as {
      code: string;
      templateId?: string;
      scheduleStart?: string;
    }[];
    expect(created.find((project) => project.code === fromModel[0]?.code)).toMatchObject({
      templateId: erpTemplateId,
      scheduleStart: '2026-11-02',
    });
    expect(created.filter((project) => project.templateId !== undefined)).toHaveLength(1);
    const saved = calls.findIndex((call) => /^marina POST .*\/schedule\/template$/.test(call));
    expect(calls.indexOf('paulo GET /api/project-templates')).toBeGreaterThan(saved);
    // Its schedule comes from the server: the command posts no phase of its own for it.
    expect(callsBy(/ POST .*\/schedule\/phases$/)).toHaveLength(
      demoProjects.reduce((total, project) => total + project.phases.length, 0),
    );
  });

  it('Projeto cancelado com chamados', async () => {
    await loadDemo(base, () => undefined);

    // Its tickets are still open when the manager cancels it, at the end: the API closes them (ADR-0043).
    const cancelled = demoProjects.find((project) => project.status === 'Cancelled');
    const open = (cancelled?.tickets ?? []).map(finalStatus);
    expect(open.length).toBeGreaterThan(0);
    expect(open.filter((status) => status === 'Closed' || status === 'Cancelled')).toEqual([]);
    const [path] =
      [...bodies.entries()].find(
        ([key, saved]) =>
          key.endsWith('/status') &&
          (saved as { status: string }[]).some((body) => body.status === 'Cancelled'),
      ) ?? [];
    const cancel = calls.indexOf(`marina PUT ${String(path)}`);
    const numbers = (cancelled?.tickets ?? []).map((ticket) => numberOf(ticket.title));
    const lastTicket = calls.findLastIndex((call) =>
      numbers.some((number) => call.includes(`/api/tickets/${String(number)}/`)),
    );
    expect(lastTicket).toBeGreaterThanOrEqual(0);
    expect(cancel).toBeGreaterThan(lastTicket);
  });

  it('Segunda gerente', async () => {
    await loadDemo(base, () => undefined);

    // Beatriz Lima manages projects of her own and builds their schedules.
    const hers = demoProjects.filter((project) => project.manager === 'beatriz');
    expect(hers.length).toBeGreaterThanOrEqual(2);
    expect(callsBy(/^beatriz POST .*\/schedule\/phases$/)).toHaveLength(
      hers.reduce((total, project) => total + project.phases.length, 0),
    );
    // Her Meu trabalho, loaded on a Monday, the day 14: a late task, one of the week and a ticket waiting for her.
    const tasks = demoProjects
      .flatMap((project) => project.phases)
      .flatMap((phase) => phase.items)
      .filter(
        (item) => item.kind === 'Task' && item.owner === 'beatriz' && (item.progress ?? 0) < 100,
      );
    expect(tasks.some((item) => item.day + item.duration < 14)).toBe(true);
    expect(tasks.some((item) => item.day >= 14 && item.day <= 21)).toBe(true);
    expect(
      plannedTickets().some(
        (ticket) => ticket.by === 'beatriz' && finalStatus(ticket) === 'WaitingForRequester',
      ),
    ).toBe(true);
  });
});
