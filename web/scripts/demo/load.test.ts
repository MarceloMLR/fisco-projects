// @vitest-environment node
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../src/test/server';
import { loadDemo, screensAddress } from './load.ts';
import { demoPeople, demoProjects } from './plan.ts';

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
let ids = 0;
let numbers = 0;

const personOfEmail = (email: string) =>
  Object.entries(demoPeople).find(([, value]) => value === email)?.[0];
const idOf = (person: string) => `0199f5a2-0000-7000-8000-${person.padStart(12, '0').slice(-12)}`;
const nextId = () => `0199f5a2-1111-7000-8000-${String(++ids).padStart(12, '0')}`;

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
  ids = 0;
  numbers = 0;
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
    http.get(`${base}/api/projects`, () =>
      HttpResponse.json({
        items: existing.map((code) => ({ code })),
        total: existing.length,
        today: '2026-10-19',
      }),
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
        ].map((name, index) => ({
          id: `0199f5a2-2222-7000-8000-${String(index + 1).padStart(12, '0')}`,
          name,
          isAcceptance: index === 5,
          count: 0,
        })),
        total: 0,
        permissions: { canUpload: true, canApprove: true, canDelete: true },
        maxBytes: 25 * 1024 * 1024,
      }),
    ),
    // The categories of links of the migration (ADR-0032).
    http.get(`${base}/api/link-categories`, () =>
      HttpResponse.json(
        ['Ambientes', 'Outros', 'Painéis', 'Repositórios', 'Sistemas'].map((name, index) => ({
          id: `0199f5a2-3333-7000-8000-${String(index + 1).padStart(12, '0')}`,
          name,
          isActive: true,
        })),
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
  it('Carregar dados de demonstração', async () => {
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    const items = demoProjects.flatMap((project) => project.phases).flatMap((phase) => phase.items);
    expect(outcome).toEqual({
      loaded: true,
      firstDay: '2026-10-05',
      projects: 5,
      items: items.length,
    });
    // Everyone of the demo directory signs in, but the people who never signed in (ADR-0078).
    expect(people(/POST \/api\/session\/local$/)).toEqual(['anonymous']);
    expect(
      (bodies.get('/api/session/local') ?? [])
        .map((body) => (body as { email: string }).email)
        .sort(),
    ).toEqual(Object.values(demoPeople).sort());
    // The Administrator gives the roles and creates the clients, the projects and their members.
    expect(callsBy(/^paulo PUT \/api\/users\/.+\/role$/)).toEqual([
      `paulo PUT /api/users/${idOf('marina')}/role`,
      `paulo PUT /api/users/${idOf('beatriz')}/role`,
    ]);
    expect(callsBy(/^paulo POST \/api\/clients$/)).toHaveLength(3);
    expect(callsBy(/^paulo POST \/api\/projects$/)).toHaveLength(5);
    expect(people(/POST \/api\/projects\/[^/]+\/members$/)).toEqual(['paulo']);
    // The manager builds the schedules, marks the milestones and changes the status.
    expect(people(/\/schedule\/(phases|items)$/)).toEqual(['marina']);
    expect(people(/\/reached$/)).toEqual(['marina']);
    expect(people(/\/projects\/[^/]+\/status$/)).toEqual(['marina']);
    // Each person in charge updates the progress of the task; the manager, of the tasks of no one.
    expect(people(/\/progress$/)).toEqual(['carla', 'diego', 'marina', 'rafael']);
    // The dates follow the day it runs: 19/10/2026 starts the main project on 05/10/2026.
    const first = [...bodies.entries()].find(([path]) => path.endsWith('/schedule/items'))?.[1][0];
    expect(first).toMatchObject({
      name: 'Reunião de abertura',
      start: '2026-10-05',
      ownerId: idOf('marina'),
    });
    expect(lines.at(-2)).toContain('2026-10-05');
    // And where to open the screens, with the code as it is now.
    expect(screensAddress).toBe('http://localhost:5173');
    expect(lines.at(-1)).toBe('Abra as telas em http://localhost:5173 (npm run dev).');
  });

  it('Dados já carregados', async () => {
    existing = ['ERP-FIN'];
    const lines: string[] = [];

    const outcome = await loadDemo(base, (line) => lines.push(line));

    expect(outcome).toEqual({ loaded: false, reason: 'already-loaded' });
    expect(calls.filter((call) => !call.includes(' GET '))).toEqual([
      'anonymous POST /api/session/local',
    ]);
    expect(lines).toEqual([
      expect.stringContaining('já estão carregados'),
      'Abra as telas em http://localhost:5173 (npm run dev).',
    ]);
  });

  it('Sem entrada local', async () => {
    localSignIn = false;

    await expect(loadDemo(base)).rejects.toThrow('A entrada local está desligada');
    expect(calls).toEqual(['anonymous GET /api/session/sign-in-options']);
  });
});

// Spec: dados-de-demonstracao, requirement "Linha de base na demonstração".
describe('Linha de base na demonstração', () => {
  it('Linha de base na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager saves the baseline of ERP-FIN and then puts off Parametrizar regras de ICMS, in the same project.
    const changes = callsBy(/ PUT .*\/schedule\/(baseline|items\/[^/]+\/dates)$/);
    expect(changes).toHaveLength(2);
    const [saved, moved] = changes;
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

// Spec: dados-de-demonstracao, requirement "Chamados na demonstração".
describe('Chamados na demonstração', () => {
  it('Chamados na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Each ticket opened by its person, before the projects get their status.
    expect(people(/ POST \/api\/tickets$/)).toEqual([
      'carla',
      'diego',
      'marina',
      'paulo',
      'rafael',
    ]);
    const opened = (bodies.get('/api/tickets') ?? []) as { title: string; team: string }[];
    const planned = demoProjects.flatMap((project) => project.tickets ?? []);
    expect(opened.map((ticket) => ticket.title)).toEqual(planned.map((ticket) => ticket.title));
    expect(new Set(opened.map((ticket) => ticket.team))).toEqual(
      new Set(['Functional', 'Development']),
    );
    const firstStatus = calls.findIndex((call) => / PUT .*\/status$/.test(call));
    expect(calls.findIndex((call) => call.endsWith('POST /api/tickets'))).toBeLessThan(firstStatus);
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

    // Each message by its person, in the first ticket: the developer, the tech lead, the requester and the manager.
    const first = callsBy(/ POST \/api\/tickets\/1\/messages$/).map((call) => call.split(' ')[0]);
    expect(first).toEqual(['diego', 'rafael', 'carla', 'marina']);
    const forms = (bodies.get('/api/tickets/1/messages') ?? []) as string[];
    // The requester mentions the developer and sends the XML.
    expect(forms[2]).toContain(`"id":"${idOf('diego')}"`);
    expect(forms[2]).toContain('filename="nota-3401.xml"');
    expect(callsBy(/ POST \/api\/tickets\/3\/messages$/)).toHaveLength(1);
  });
});

// Spec: dados-de-demonstracao, requirement "Status na demonstração".
describe('Status na demonstração', () => {
  it('Status na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The tech lead resolves the third ticket he answered; the contributor asks for information in the fifth.
    expect(callsBy(/ POST \/api\/tickets\/\d+\/transitions$/)).toEqual([
      'rafael POST /api/tickets/3/transitions',
      'carla POST /api/tickets/5/transitions',
    ]);
    expect(bodies.get('/api/tickets/3/transitions')).toEqual([{ action: 'Resolve', body: null }]);
    const [ask] = (bodies.get('/api/tickets/5/transitions') ?? []) as {
      action: string;
      body: string;
    }[];
    expect(ask?.action).toBe('AskForInformation');
    expect(ask?.body).toContain('data de vigência');
    // Each change after the conversation of its ticket.
    const fifth = callsBy(/ POST \/api\/tickets\/5\//);
    expect(fifth).toEqual([
      'carla POST /api/tickets/5/messages',
      'carla POST /api/tickets/5/transitions',
    ]);
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
    // And one waits for the requester, with its deadline paused.
    const asked = demoProjects
      .flatMap((project) => project.tickets ?? [])
      .filter((ticket) => ticket.changes?.some((change) => change.action === 'AskForInformation'));
    expect(asked).toHaveLength(1);
  });
});

// Spec: dados-de-demonstracao, requirement "Trocas na demonstração".
describe('Trocas na demonstração', () => {
  it('Trocas na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // The manager assigns the sixth ticket to the developer; the tech lead sends the fourth to Funcional.
    expect(callsBy(/ PUT \/api\/tickets\/assignee$/)).toEqual(['marina PUT /api/tickets/assignee']);
    expect(bodies.get('/api/tickets/assignee')).toEqual([
      { numbers: [6], assigneeId: idOf('diego') },
    ]);
    expect(callsBy(/ PUT \/api\/tickets\/\d+\/team$/)).toEqual(['rafael PUT /api/tickets/4/team']);
    expect(bodies.get('/api/tickets/4/team')).toEqual([{ team: 'Functional' }]);
  });
});

// Spec: dados-de-demonstracao, requirement "Fornecedores na demonstração".
describe('Fornecedores na demonstração', () => {
  it('Fornecedores na demonstração', async () => {
    await loadDemo(base, () => undefined);

    // Two fictitious vendors, by the Administrator; the tech lead registers the first ticket at one and updates it.
    expect(callsBy(/ POST \/api\/admin\/vendors$/)).toEqual([
      'paulo POST /api/admin/vendors',
      'paulo POST /api/admin/vendors',
    ]);
    const vendors = (bodies.get('/api/admin/vendors') ?? []) as { name: string }[];
    expect(vendors.map((vendor) => vendor.name)).toEqual([
      'Emissor Exemplo',
      'Nuvem Fiscal Exemplo',
    ]);
    expect(callsBy(/\/vendor-tickets/)).toEqual([
      'rafael POST /api/tickets/1/vendor-tickets',
      expect.stringMatching(/^rafael PUT \/api\/tickets\/1\/vendor-tickets\/[0-9a-f-]+$/),
    ]);
    expect(bodies.get('/api/tickets/1/vendor-tickets')).toEqual([
      expect.objectContaining({ code: 'FX-20931', openedOn: '2026-10-19', status: 'Open' }),
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
    const versions = documents.reduce(
      (total, document) => total + (document.versions?.length ?? 0),
      0,
    );
    expect(sent).toHaveLength(documents.length + versions);
    expect(people(/ POST \/api\/projects\/[^/]+\/documents$/)).toEqual([
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
    expect(people(/ PUT .*\/documents\/[^/]+\/status$/)).toEqual(['carla', 'marina', 'rafael']);
    expect(callsBy(/ PUT .*\/documents\/[^/]+\/link$/)).toHaveLength(
      documents.filter((document) => document.link !== undefined).length,
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
    expect(callsBy(/ POST \/api\/projects\/[^/]+\/links$/)).toHaveLength(links.length);
    expect(people(/ POST \/api\/projects\/[^/]+\/links$/)).toEqual([
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
    expect(new Set(sent.map((link) => link.categoryId)).size).toBe(4);
    // Before the project gets its status.
    const lastLink = calls.findLastIndex((call) => call.endsWith('/links'));
    const status = calls.findLastIndex((call) => /PUT \/api\/projects\/[^/]+\/status$/.test(call));
    expect(lastLink).toBeLessThan(status);
  });
});
