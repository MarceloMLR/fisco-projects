import createClient from 'openapi-fetch';
import type { paths } from '../../src/api/schema';
import { pdfOf } from './pdf.ts';
import { pngOf } from './png.ts';
import {
  administrator,
  dayFrom,
  demoClients,
  type DemoDocument,
  type DemoFile,
  type DemoMessage,
  demoNames,
  type DemoPerson,
  demoPeople,
  type DemoProject,
  demoProjects,
  demoVendors,
  fallbackCategory,
  firstDayFor,
  formerColleague,
  mainProject,
  manager,
  projectManagers,
  techLeads,
  workDaysBefore,
} from './plan.ts';

/**
 * Loads the demonstration data through the API (ADR-0089), each action by the person who would do it on the screens:
 * the Administrator creates the clients and the projects, the manager of each project builds its schedule and each
 * person in charge of a task updates its progress. Only the projects of the plan that are not there yet are loaded,
 * with the clients and vendors still missing, so a demonstration already loaded gets what the plan added since; the
 * colleague who left the company is anonymized while nobody is (ADR-0110). It stops where the local sign-in is off, as
 * in production (ADR-0078). It ends saying where the screens open.
 */

type Api = ReturnType<typeof createClient<paths>>;

/**
 * Where the screens open in the development (`npm run dev`), always with the code as it is now; the API, in 5175,
 * serves the last `npm run build`, which may be older.
 */
export const screensAddress = 'http://localhost:5173';

const screensLine = `Abra as telas em ${screensAddress} (npm run dev).`;

export type DemoOutcome =
  | { loaded: true; firstDay: string; projects: number; items: number }
  | { loaded: false; reason: 'already-loaded' };

/** What the API answered to a call: an error, or a response that is not a success. */
interface Answer<T> {
  data?: T;
  error?: unknown;
  response: Response;
}

/** The data of a call that must work, or an error that says which call failed and how. */
function must<T>(answer: Answer<T>, what: string): T {
  if (!answer.response.ok || answer.data === undefined) {
    throw new Error(`${what}: a API respondeu ${String(answer.response.status)}.`);
  }
  return answer.data;
}

function done(answer: { response: Response }, what: string): void {
  if (!answer.response.ok) {
    throw new Error(`${what}: a API respondeu ${String(answer.response.status)}.`);
  }
}

/** A client of the API with the session of one person: the cookie it got, and the X-CSRF of every change (ADR-0054). */
function apiFor(baseUrl: string): Api {
  const cookies = new Map<string, string>();
  return createClient<paths>({
    baseUrl,
    fetch: async (request: Request) => {
      if (cookies.size > 0) {
        request.headers.set(
          'Cookie',
          [...cookies].map(([name, value]) => `${name}=${value}`).join('; '),
        );
      }
      request.headers.set('X-CSRF', '1');
      const response = await fetch(request);
      for (const header of response.headers.getSetCookie()) {
        const [pair = ''] = header.split(';');
        const separator = pair.indexOf('=');
        if (separator > 0) {
          cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
        }
      }
      return response;
    },
  });
}

/** Signs a person in by the local sign-in and gives the id of the user (ADR-0078). */
function signIn(baseUrl: string, person: DemoPerson): Promise<{ api: Api; id: string }> {
  return signInWith(baseUrl, demoPeople[person]);
}

/** Signs in the person of the demo directory with the e-mail, with the id and the name of the user (ADR-0078). */
async function signInWith(
  baseUrl: string,
  email: string,
): Promise<{ api: Api; id: string; name: string }> {
  const api = apiFor(baseUrl);
  const signedIn = await api.POST('/api/session/local', { body: { email } });
  if (!signedIn.response.ok) {
    throw new Error(
      `A entrada local recusou ${email} (${String(signedIn.response.status)}): confira o diretório de demonstração (LocalSignIn:People).`,
    );
  }
  const session = must(await api.GET('/api/session'), `Sessão de ${email}`);
  return { api, id: session.id, name: session.name };
}

export async function loadDemo(
  baseUrl: string,
  log: (line: string) => void = () => undefined,
): Promise<DemoOutcome> {
  const anonymous = apiFor(baseUrl);
  const options = await anonymous.GET('/api/session/sign-in-options');
  if (!options.response.ok) {
    throw new Error(
      `A API não respondeu às formas de entrada (${String(options.response.status)}); ela pode estar reiniciando.`,
    );
  }
  if (options.data?.localSignIn !== true) {
    throw new Error(
      'A entrada local está desligada nesta API: os dados de demonstração só entram no desenvolvimento (ADR-0078).',
    );
  }

  const admin = await signIn(baseUrl, administrator);
  const session = must(await admin.api.GET('/api/session'), 'Sessão do administrador');
  if (session.role !== 'Administrator') {
    throw new Error(
      `${demoPeople[administrator]} não é administrador: confira Users:FirstAdministrator no appsettings.Development.json.`,
    );
  }
  const { codes, today } = await existingProjects(admin.api);
  const missing = demoProjects.filter((project) => !codes.has(project.code));
  // The colleague who left the company, while nobody is anonymized (ADR-0110).
  const colleagueLeaves =
    must(
      await admin.api.GET('/api/users', {
        params: { query: { status: 'Anonymized', pageSize: 1 } },
      }),
      'Pessoas anonimizadas',
    ).total === 0;
  if (missing.length === 0 && !colleagueLeaves) {
    log(
      `Os dados de demonstração já estão carregados (${mainProject} e os demais projetos do plano existem). Nada mudou.`,
    );
    log(
      'Para carregar de novo, apague o banco local (docker compose down -v e docker compose up -d --wait) e os arquivos de src/FiscoProjects.Api/App_Data, suba a API e rode npm run demo:data.',
    );
    log(screensLine);
    return { loaded: false, reason: 'already-loaded' };
  }
  const kept = demoProjects.filter((project) => codes.has(project.code));
  if (kept.length > 0) {
    log(
      `Projetos que já existiam, mantidos como estão: ${kept.map((project) => project.code).join(', ')}.`,
    );
  }

  const people = new Map<DemoPerson, { api: Api; id: string }>([[administrator, admin]]);
  for (const person of Object.keys(demoPeople) as DemoPerson[]) {
    if (!people.has(person)) {
      people.set(person, await signIn(baseUrl, person));
    }
  }
  const personOf = (person: DemoPerson) => {
    const signedIn = people.get(person);
    if (signedIn === undefined) {
      throw new Error(`Sem sessão de ${demoPeople[person]}.`);
    }
    return signedIn;
  };
  log(`Pessoas: ${String(people.size)}.`);

  for (const [person, role] of [
    ...projectManagers.map((person) => [person, 'ProjectManager'] as const),
    ...techLeads.map((person) => [person, 'TechLead'] as const),
  ]) {
    done(
      await admin.api.PUT('/api/users/{id}/role', {
        params: { path: { id: personOf(person).id } },
        body: { role },
      }),
      `Papel de ${demoPeople[person]}`,
    );
  }

  // The clients and the vendors of the plan that are not there yet, by name.
  const clients = new Map<string, string>(
    must(
      await admin.api.GET('/api/clients', { params: { query: { pageSize: 100 } } }),
      'Lista de clientes',
    ).items.map((client) => [client.name, client.id]),
  );
  let newClients = 0;
  for (const name of demoClients.filter((client) => !clients.has(client))) {
    const created = must(
      await admin.api.POST('/api/clients', { body: { name, cnpj: null, observation: null } }),
      `Cliente ${name}`,
    );
    clients.set(name, created.id);
    newClients += 1;
  }
  log(`Clientes: ${String(newClients)} novos.`);

  const vendors = new Map<string, string>(
    must(await admin.api.GET('/api/admin/vendors'), 'Lista de fornecedores').map((vendor) => [
      vendor.name,
      vendor.id,
    ]),
  );
  let newVendors = 0;
  for (const vendor of demoVendors.filter((item) => !vendors.has(item.name))) {
    const added = must(
      await admin.api.POST('/api/admin/vendors', { body: vendor }),
      `Fornecedor ${vendor.name}`,
    );
    vendors.set(vendor.name, added.id);
    newVendors += 1;
  }
  log(`Fornecedores: ${String(newVendors)} novos.`);

  const firstDay = firstDayFor(today);
  let items = 0;
  for (const project of missing) {
    items += await loadProject(
      project,
      firstDay,
      today,
      admin.api,
      clients,
      vendors,
      personOf,
      anonymous,
      log,
    );
  }
  if (colleagueLeaves) {
    await leaveTheCompany(baseUrl, admin.api, personOf, log);
  }
  const count = (of: (project: DemoProject) => readonly unknown[] | undefined) =>
    missing.reduce((total, project) => total + (of(project)?.length ?? 0), 0);
  if (missing.length > 0) {
    log(
      `Projetos novos: ${String(missing.length)} (${missing.map((project) => project.code).join(', ')}), com ${String(items)} tarefas e marcos, a partir de ${firstDay}, ${String(count((project) => project.tickets))} chamados, ${String(count((project) => project.documents))} documentos, ${String(count((project) => project.links))} links, ${String(count((project) => project.credentials))} acessos, ${String(count((project) => project.acceptances))} termos de aceite, ${String(count((project) => project.hours))} apontamentos de horas e ${String(missing.filter((project) => project.contractedHours !== undefined).length)} com horas contratadas.`,
    );
  }
  log(screensLine);
  return { loaded: true, firstDay, projects: missing.length, items };
}

/**
 * The projects already there, of every situation, by code, with the id, and today in the time zone of the
 * installation.
 */
async function existingProjects(
  admin: Api,
): Promise<{ codes: Map<string, string>; today: string }> {
  const codes = new Map<string, string>();
  const pageSize = 100;
  for (let page = 1; ; page += 1) {
    const found = must(
      await admin.GET('/api/projects', { params: { query: { situation: 'All', page, pageSize } } }),
      'Lista de projetos',
    );
    for (const project of found.items) {
      codes.set(project.code, project.id);
    }
    if (page * pageSize >= found.total) {
      return { codes, today: found.today };
    }
  }
}

/**
 * The colleague who left the company (ADR-0058, ADR-0110): she signs in, the Administrator puts her in the main
 * project, she opens a ticket and the tech lead answers mentioning her; then the Administrator deactivates and
 * anonymizes her. Without the main project, which the Administrator may have removed, nothing happens.
 */
async function leaveTheCompany(
  baseUrl: string,
  admin: Api,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  log: (line: string) => void,
): Promise<void> {
  const projectId = (await existingProjects(admin)).codes.get(mainProject);
  if (projectId === undefined) {
    log(`Ninguém saiu da empresa: ${mainProject} não existe.`);
    return;
  }
  const colleague = await signInWith(baseUrl, formerColleague.email);
  // Already in the project, from a load that stopped in the middle, is fine.
  const joined = await admin.POST('/api/projects/{id}/members', {
    params: { path: { id: projectId } },
    body: { userId: colleague.id, directoryId: null, role: formerColleague.role },
  });
  if (!joined.response.ok && joined.response.status !== 409) {
    done(joined, `Membro ${formerColleague.email} de ${mainProject}`);
  }
  const { ticket, answer } = formerColleague;
  const opened = must(
    await colleague.api.POST('/api/tickets', { body: { projectId, ...ticket } }),
    `Chamado ${ticket.title}`,
  );
  const form = new FormData();
  form.append(
    'body',
    JSON.stringify({
      type: 'doc',
      content: [
        {
          type: 'paragraph',
          content: [
            { type: 'mention', attrs: { id: colleague.id, label: colleague.name } },
            { type: 'text', text: `, ${answer.text}` },
          ],
        },
      ],
    }),
  );
  done(
    await personOf(answer.by).api.POST('/api/tickets/{number}/messages', {
      params: { path: { number: opened.number } },
      body: { files: [] },
      bodySerializer: () => form,
    }),
    `Mensagem no chamado ${ticket.title}`,
  );
  const user = { params: { path: { id: colleague.id } } };
  done(await admin.POST('/api/users/{id}/deactivate', user), `Desativar ${formerColleague.email}`);
  done(
    await admin.POST('/api/users/{id}/anonymization', user),
    `Anonimizar ${formerColleague.email}`,
  );
  log(
    `${colleague.name} saiu da empresa: abriu o chamado #${String(opened.number)} no ${mainProject} e foi desativada e anonimizada.`,
  );
}

/**
 * The model and the start of a project created from a model of an earlier project of the plan (ADR-0017); without the
 * model, which the Administrator may have renamed or removed, the project is created blank.
 */
async function fromTemplate(
  project: DemoProject,
  firstDay: string,
  admin: Api,
  log: (line: string) => void,
): Promise<{ templateId?: string; scheduleStart?: string }> {
  if (project.fromTemplate === undefined) {
    return {};
  }
  const { name, start } = project.fromTemplate;
  const template = must(await admin.GET('/api/project-templates'), 'Modelos de projeto').find(
    (model) => model.name === name,
  );
  if (template === undefined) {
    log(`${project.code} foi criado em branco: o modelo ${name} não está entre os modelos ativos.`);
    return {};
  }
  return { templateId: template.id, scheduleStart: dayFrom(firstDay, start) };
}

/**
 * The schedule of a project created from the XML of the MS Project that the export gives of an earlier project of the
 * plan (ADR-0135): the Administrator exports it, reads it in the preview and links each resource to the person of the
 * demo with its name, with the role of the members of the plan, or to the client; gives the people it put in the
 * project. Without the earlier project, which the Administrator may have removed, the project is created blank.
 */
async function fromMsProject(
  project: DemoProject,
  firstDay: string,
  clientId: string | null,
  admin: Api,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  log: (line: string) => void,
): Promise<{ body: Record<string, unknown>; people: ReadonlySet<DemoPerson> }> {
  if (project.fromMsProject === undefined) {
    return { body: {}, people: new Set() };
  }
  const { code, start } = project.fromMsProject;
  const sourceId = (await existingProjects(admin)).codes.get(code);
  if (sourceId === undefined) {
    log(
      `${project.code} foi criado em branco: ${code} não existe para exportar o XML do MS Project.`,
    );
    return { body: {}, people: new Set() };
  }
  const xml = must(
    await admin.GET('/api/projects/{id}/schedule/ms-project', {
      params: { path: { id: sourceId } },
      parseAs: 'text',
    }),
    `XML do MS Project de ${code}`,
  );
  const scheduleStart = dayFrom(firstDay, start);
  const form = new FormData();
  form.append('file', new Blob([xml], { type: 'application/xml' }), `${code}.xml`);
  form.append('start', scheduleStart);
  if (clientId !== null) {
    form.append('clientId', clientId);
  }
  const preview = must(
    await admin.POST('/api/projects/ms-project-preview', {
      body: { file: '', start: scheduleStart, ...(clientId === null ? {} : { clientId }) },
      bodySerializer: () => form,
    }),
    `Prévia do XML de ${code}`,
  );
  const roles = new Map(project.members ?? []);
  const people = new Set<DemoPerson>();
  const resources = preview.resources.map((resource) => {
    const person = (Object.keys(demoNames) as DemoPerson[]).find(
      (candidate) => demoNames[candidate] === resource.name,
    );
    if (person === undefined) {
      return { userId: null, directoryId: null, client: resource.client, role: null };
    }
    people.add(person);
    const role = roles.get(person);
    return {
      userId: personOf(person).id,
      directoryId: null,
      client: false,
      role: role === 'Developer' ? 'Developer' : 'Contributor',
    };
  });
  return {
    body: {
      scheduleStart,
      importedSchedule: { phases: preview.plan.phases, items: preview.plan.items, resources },
    },
    people,
  };
}

/** The text of a message as the editor keeps it: a paragraph, with the mention at the start (ADR-0026). */
function documentOf(message: DemoMessage, personOf: (person: DemoPerson) => { id: string }) {
  const mention =
    message.mention === undefined
      ? []
      : [
          {
            type: 'mention',
            attrs: { id: personOf(message.mention).id, label: demoNames[message.mention] },
          },
          { type: 'text', text: ', ' },
        ];
  return {
    type: 'doc',
    content: [{ type: 'paragraph', content: [...mention, { type: 'text', text: message.text }] }],
  };
}

/**
 * Creates a project with its people, builds its schedule (or has the server make it from a model) and puts it in its
 * status; gives how many items of the plan it has.
 */
async function loadProject(
  project: DemoProject,
  firstDay: string,
  today: string,
  admin: Api,
  clients: ReadonlyMap<string, string>,
  vendors: ReadonlyMap<string, string>,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  anonymous: Api,
  log: (line: string) => void,
): Promise<number> {
  const leader = project.manager ?? manager;
  const clientId = project.client === null ? null : (clients.get(project.client) ?? null);
  const imported = await fromMsProject(project, firstDay, clientId, admin, personOf, log);
  const created = must(
    await admin.POST('/api/projects', {
      body: {
        code: project.code,
        name: project.name,
        description: null,
        clientId,
        managerId: personOf(leader).id,
        techLeadId: project.techLead === undefined ? null : personOf(project.techLead).id,
        ...(await fromTemplate(project, firstDay, admin, log)),
        ...imported.body,
      },
    }),
    `Projeto ${project.code}`,
  );
  const id = created.id;
  // The people of the resources of the file are already in the project, with their roles (ADR-0135).
  for (const [person, role] of (project.members ?? []).filter(
    ([person]) => !imported.people.has(person),
  )) {
    done(
      await admin.POST('/api/projects/{id}/members', {
        params: { path: { id } },
        body: { userId: personOf(person).id, directoryId: null, role },
      }),
      `Membro ${demoPeople[person]} de ${project.code}`,
    );
  }

  const builder = personOf(leader).api;
  const ids = new Map<string, string>();
  const phaseIds = new Map<string, string>();
  const idOf = (name: string, by: string) => {
    const found = ids.get(name);
    if (found === undefined) {
      throw new Error(`${by} usa ${name}, que ainda não existe em ${project.code}.`);
    }
    return found;
  };
  for (const phase of project.phases) {
    const phaseId = must(
      await builder.POST('/api/projects/{id}/schedule/phases', {
        params: { path: { id } },
        body: { name: phase.name },
      }),
      `Fase ${phase.name} de ${project.code}`,
    ).id;
    phaseIds.set(phase.name, phaseId);
    for (const item of phase.items) {
      const itemId = must(
        await builder.POST('/api/projects/{id}/schedule/items', {
          params: { path: { id } },
          body: {
            kind: item.kind,
            phaseId,
            name: item.name,
            start: dayFrom(firstDay, item.day),
            duration: item.duration,
            ownerId: item.owner === undefined ? null : personOf(item.owner).id,
            ownedByClient: item.byClient === true,
            progress: 0,
            predecessorIds: (item.after ?? []).map((name) => idOf(name, item.name)),
            links: (item.after ?? []).map((name) => ({
              predecessorId: idOf(name, item.name),
              type: item.links?.[name]?.type ?? ('FinishToStart' as const),
              lag: item.links?.[name]?.lag ?? 0,
            })),
            parentId: item.inside === undefined ? null : idOf(item.inside, item.name),
            deadline: item.deadline === undefined ? null : dayFrom(firstDay, item.deadline),
          },
        }),
        `Item ${item.name} de ${project.code}`,
      ).id;
      ids.set(item.name, itemId);
      if (item.progress !== undefined) {
        // The person in charge updates the progress of the task; without one, the manager (ADR-0043).
        const who = personOf(item.owner ?? leader).api;
        done(
          await who.PUT('/api/projects/{id}/schedule/items/{itemId}/progress', {
            params: { path: { id, itemId } },
            body: { progress: item.progress },
          }),
          `Progresso de ${item.name}`,
        );
      }
      if (item.reached === true) {
        done(
          await builder.PUT('/api/projects/{id}/schedule/items/{itemId}/reached', {
            params: { path: { id, itemId } },
            body: { reached: true },
          }),
          `Marco ${item.name}`,
        );
      }
    }
  }

  if (project.baseline === true) {
    done(
      await builder.PUT('/api/projects/{id}/schedule/baseline', { params: { path: { id } } }),
      `Linha de base de ${project.code}`,
    );
  }
  for (const item of project.phases.flatMap((phase) => phase.items)) {
    if (item.movedTo !== undefined) {
      done(
        await builder.PUT('/api/projects/{id}/schedule/items/{itemId}/dates', {
          params: { path: { id, itemId: idOf(item.name, 'movedTo') } },
          body: { start: dayFrom(firstDay, item.movedTo), end: null },
        }),
        `Datas de ${item.name}`,
      );
    }
  }

  if (project.template !== undefined) {
    done(
      await builder.POST('/api/projects/{id}/schedule/template', {
        params: { path: { id } },
        body: project.template,
      }),
      `Modelo ${project.template.name}`,
    );
  }

  // The ids of the tickets by title, for the hours worked on them (ADR-0128).
  const ticketIds = new Map<string, string>();
  // Each ticket opened by its person, while the project is open (ADR-0022), then what happens to it, in order.
  for (const ticket of project.tickets ?? []) {
    const opened = must(
      await personOf(ticket.by).api.POST('/api/tickets', {
        body: {
          projectId: id,
          team: ticket.team,
          priority: ticket.priority,
          title: ticket.title,
          description: ticket.description,
        },
      }),
      `Chamado ${ticket.title}`,
    );
    ticketIds.set(ticket.title, opened.id);
    const path = { params: { path: { number: opened.number } } };
    // The files of the description, by who opened it, before any message (ADR-0026).
    if ((ticket.files ?? []).length > 0) {
      const form = new FormData();
      for (const file of ticket.files ?? []) {
        form.append('files', ticketFileOf(file), file.name);
      }
      done(
        await personOf(ticket.by).api.POST('/api/tickets/{number}/attachments', {
          ...path,
          body: { files: [] },
          bodySerializer: () => form,
        }),
        `Anexos do chamado ${ticket.title}`,
      );
    }
    const atVendors = new Map<string, string>();
    for (const step of ticket.steps ?? []) {
      const api = personOf(step.by).api;
      switch (step.kind) {
        case 'message': {
          // Each message by its person, with the mention and the file (ADR-0026).
          const form = new FormData();
          form.append('body', JSON.stringify(documentOf(step, personOf)));
          if (step.file !== undefined) {
            form.append('files', ticketFileOf(step.file), step.file.name);
          }
          done(
            await api.POST('/api/tickets/{number}/messages', {
              ...path,
              body: { files: [] },
              bodySerializer: () => form,
            }),
            `Mensagem no chamado ${ticket.title}`,
          );
          break;
        }
        case 'action':
          // An action of the flow by who may take it (ADR-0024).
          done(
            await api.POST('/api/tickets/{number}/transitions', {
              ...path,
              body: {
                action: step.action,
                body:
                  step.text === undefined
                    ? null
                    : JSON.stringify(documentOf({ by: step.by, text: step.text }, personOf)),
              },
            }),
            `Status do chamado ${ticket.title}`,
          );
          break;
        case 'priority':
          done(
            await api.PUT('/api/tickets/{number}/priority', {
              ...path,
              body: { priority: step.to },
            }),
            `Prioridade do chamado ${ticket.title}`,
          );
          break;
        case 'follow':
          done(
            await api.PUT('/api/tickets/{number}/following', path),
            `Acompanhar o chamado ${ticket.title}`,
          );
          break;
        case 'assign':
          // The people and the team, each by who may change them (ADR-0068).
          done(
            await api.PUT('/api/tickets/assignee', {
              body: { numbers: [opened.number], assigneeId: personOf(step.to).id },
            }),
            `Responsável do chamado ${ticket.title}`,
          );
          break;
        case 'transfer':
          done(
            await api.PUT('/api/tickets/{number}/team', { ...path, body: { team: step.to } }),
            `Time do chamado ${ticket.title}`,
          );
          break;
        case 'vendor': {
          // A ticket at a vendor, registered on the day of the load (ADR-0028).
          const registered = must(
            await api.POST('/api/tickets/{number}/vendor-tickets', {
              ...path,
              body: {
                vendorId: vendors.get(step.vendor) ?? null,
                code: step.code,
                openedOn: today,
                status: step.status,
                link: step.link ?? null,
                note: step.note ?? null,
              },
            }),
            `Chamado ${step.code} no fornecedor`,
          );
          atVendors.set(step.code, registered.id);
          break;
        }
        case 'vendorUpdate':
          done(
            await api.PUT('/api/tickets/{number}/vendor-tickets/{id}', {
              params: { path: { number: opened.number, id: atVendors.get(step.code) ?? '' } },
              body: { status: step.status, note: step.note },
            }),
            `Atualização do chamado ${step.code} no fornecedor`,
          );
          break;
      }
    }
  }

  if ((project.documents ?? []).length > 0) {
    await loadDocuments(project, id, ids, phaseIds, builder, personOf, log);
  }

  if ((project.links ?? []).length > 0) {
    await loadLinks(project, id, builder, personOf, log);
  }

  if ((project.credentials ?? []).length > 0) {
    await loadCredentials(project, id, personOf);
  }

  if ((project.acceptances ?? []).length > 0) {
    await loadAcceptances(project, id, ids, personOf, anonymous);
  }

  if ((project.hours ?? []).length > 0) {
    await loadHours(project, id, today, ids, ticketIds, personOf);
  }

  await loadBalance(project, id, today, admin);

  if (project.status !== undefined) {
    done(
      await builder.PUT('/api/projects/{id}/status', {
        params: { path: { id } },
        body: { status: project.status },
      }),
      `Status de ${project.code}`,
    );
  }
  return ids.size;
}

/** The content of a document of the plan: a PDF with the lines, or the lines as text (CSV, TXT). */
function fileOf(document: { name: string }, lines: readonly string[]): Blob {
  return document.name.toLowerCase().endsWith('.pdf')
    ? new Blob([pdfOf(lines)], { type: 'application/pdf' })
    : new Blob([lines.join('\n')], { type: 'text/plain' });
}

/** A file of a ticket, made from its lines by its extension (ADR-0089): PDF, PNG, or the lines as text. */
function ticketFileOf(file: DemoFile): Blob {
  const name = file.name.toLowerCase();
  if (name.endsWith('.pdf')) {
    return new Blob([pdfOf(file.lines)], { type: 'application/pdf' });
  }
  if (name.endsWith('.png')) {
    return new Blob([pngOf(file.lines.length)], { type: 'image/png' });
  }
  const type = name.endsWith('.xml')
    ? 'application/xml'
    : name.endsWith('.csv')
      ? 'text/csv'
      : 'text/plain';
  return new Blob([file.lines.join('\n')], { type });
}

/** Sends a file to a folder, as the person who sends it (ADR-0031); gives the id of the document. */
async function sendDocument(
  api: Api,
  projectId: string,
  folderId: string,
  document: DemoDocument,
  lines: readonly string[],
): Promise<string> {
  const form = new FormData();
  form.append('folderId', folderId);
  form.append('file', fileOf(document, lines), document.name);
  return must(
    await api.POST('/api/projects/{projectId}/documents', {
      params: { path: { projectId } },
      body: { folderId, file: '' },
      bodySerializer: () => form,
    }),
    `Documento ${document.name}`,
  ).id;
}

/**
 * Sends the documents of the project to their folders, each by its person, then the new versions, the status and the
 * link with the schedule (ADR-0031, ADR-0045). A folder the project lacks, because the Administrator deactivated or
 * renamed its default folder, gives way to the first folder that is not the one of the terms.
 */
async function loadDocuments(
  project: DemoProject,
  projectId: string,
  items: ReadonlyMap<string, string>,
  phases: ReadonlyMap<string, string>,
  builder: Api,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  log: (line: string) => void,
): Promise<void> {
  const area = must(
    await builder.GET('/api/projects/{projectId}/documents/folders', {
      params: { path: { projectId } },
    }),
    `Pastas de ${project.code}`,
  );
  for (const document of project.documents ?? []) {
    const folder =
      area.folders.find((item) => item.name === document.folder) ??
      area.folders.find((item) => !item.isAcceptance);
    if (folder === undefined) {
      throw new Error(`${project.code} não tem pastas para receber ${document.name}.`);
    }
    if (folder.name !== document.folder) {
      log(
        `${document.name} foi para ${folder.name}: ${project.code} não tem a pasta ${document.folder}.`,
      );
    }
    const folderId = folder.id;
    const documentId = await sendDocument(
      personOf(document.by).api,
      projectId,
      folderId,
      document,
      document.lines,
    );
    for (const version of document.versions ?? []) {
      await sendDocument(personOf(version.by).api, projectId, folderId, document, version.lines);
    }
    const path = { params: { path: { projectId, documentId } } };
    if (document.status !== undefined) {
      done(
        await personOf(document.status.by).api.PUT(
          '/api/projects/{projectId}/documents/{documentId}/status',
          { ...path, body: { status: document.status.to } },
        ),
        `Status de ${document.name}`,
      );
    }
    if (document.link !== undefined) {
      const phaseId = phases.get(document.link.to) ?? null;
      const itemId = phaseId === null ? (items.get(document.link.to) ?? null) : null;
      if (phaseId === null && itemId === null) {
        throw new Error(
          `${document.name} se liga a ${document.link.to}, que ${project.code} não tem.`,
        );
      }
      done(
        await personOf(document.link.by).api.PUT(
          '/api/projects/{projectId}/documents/{documentId}/link',
          { ...path, body: { phaseId, itemId } },
        ),
        `Vínculo de ${document.name}`,
      );
    }
  }
}

/**
 * Includes the links of the project, each by its person, in the categories of the migration (ADR-0032). A category the
 * Administrator deactivated or renamed gives way to the fallback one of the plan, or to the first active one.
 */
async function loadLinks(
  project: DemoProject,
  projectId: string,
  builder: Api,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  log: (line: string) => void,
): Promise<void> {
  const categories = must(await builder.GET('/api/link-categories'), 'Categorias de links');
  for (const link of project.links ?? []) {
    const category =
      categories.find((item) => item.name === link.category) ??
      categories.find((item) => item.name === fallbackCategory) ??
      categories[0];
    if (category === undefined) {
      throw new Error(`Não há categorias de links ativas para ${link.title}.`);
    }
    if (category.name !== link.category) {
      log(
        `O link ${link.title} foi para ${category.name}: ${link.category} não é uma categoria ativa.`,
      );
    }
    const categoryId = category.id;
    must(
      await personOf(link.by).api.POST('/api/projects/{projectId}/links', {
        params: { path: { projectId } },
        body: {
          title: link.title,
          url: link.url,
          categoryId,
          description: link.description ?? null,
        },
      }),
      `Link ${link.title}`,
    );
  }
}

/** A fictitious password, different each time the command runs (ADR-0002): never a password of the plan. */
export function demoPassword(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(12));
  return `Demo-${Buffer.from(bytes).toString('base64url')}`;
}

/** Includes the credentials of the project, each by its person, with a fictitious password (ADR-0033, ADR-0034). */
async function loadCredentials(
  project: DemoProject,
  projectId: string,
  personOf: (person: DemoPerson) => { api: Api; id: string },
): Promise<void> {
  for (const credential of project.credentials ?? []) {
    must(
      await personOf(credential.by).api.POST('/api/projects/{projectId}/credentials', {
        params: { path: { projectId } },
        body: {
          portal: credential.portal,
          environment: credential.environment,
          url: credential.url ?? null,
          username: credential.username,
          password: demoPassword(),
          notes: credential.notes ?? null,
        },
      }),
      `Acesso ${credential.portal} de ${credential.environment}`,
    );
  }
}

/**
 * The hours of the project (ADR-0128), each logged by its person on its working day, on the project only or on the
 * task or the ticket of the plan.
 */
async function loadHours(
  project: DemoProject,
  projectId: string,
  today: string,
  items: ReadonlyMap<string, string>,
  tickets: ReadonlyMap<string, string>,
  personOf: (person: DemoPerson) => { api: Api; id: string },
): Promise<void> {
  for (const entry of project.hours ?? []) {
    must(
      await personOf(entry.by).api.POST('/api/time-entries', {
        body: {
          projectId,
          day: workDaysBefore(today, entry.daysAgo),
          hours: entry.hours,
          description: entry.description,
          scheduleItemId: entry.task === undefined ? null : (items.get(entry.task) ?? null),
          ticketId: entry.ticket === undefined ? null : (tickets.get(entry.ticket) ?? null),
        },
      }),
      `Horas de ${entry.by} em ${project.code}`,
    );
  }
}

/**
 * The contracted and the additional hours of the project, registered by the Administrator after the hours logged
 * (ADR-0128), so the hours of the projects show each state of the balance.
 */
async function loadBalance(
  project: DemoProject,
  projectId: string,
  today: string,
  admin: Api,
): Promise<void> {
  if (project.contractedHours !== undefined) {
    done(
      await admin.PUT('/api/admin/project-hours/{projectId}/contracted', {
        params: { path: { projectId } },
        body: { hours: project.contractedHours },
      }),
      `Horas contratadas de ${project.code}`,
    );
  }
  for (const additional of project.additionalHours ?? []) {
    must(
      await admin.POST('/api/admin/project-hours/{projectId}/additional', {
        params: { path: { projectId } },
        body: {
          day: workDaysBefore(today, additional.daysAgo),
          hours: additional.hours,
          reason: additional.reason,
        },
      }),
      `Horas adicionais de ${project.code}`,
    );
  }
}

/** A PDF of the plan, as the form of a term takes it, with the other fields (ADR-0035). */
function termForm(
  fields: Record<string, string>,
  name: string,
  lines: readonly string[],
): FormData {
  const form = new FormData();
  for (const [field, value] of Object.entries(fields)) {
    form.append(field, value);
  }
  form.append('file', new Blob([pdfOf(lines)], { type: 'application/pdf' }), name);
  return form;
}

/**
 * The acceptance terms of the project (ADR-0129): each request of signature by the manager to the signers of the
 * client, at the local provider, then what each signer does on the local page, by the link the drawer of the term
 * shows, without a session; and the terms signed outside the system, attached by the manager.
 */
async function loadAcceptances(
  project: DemoProject,
  projectId: string,
  items: ReadonlyMap<string, string>,
  personOf: (person: DemoPerson) => { api: Api; id: string },
  anonymous: Api,
): Promise<void> {
  for (const term of project.acceptances ?? []) {
    const milestoneId = items.get(term.milestone);
    if (milestoneId === undefined) {
      throw new Error(`${term.title} aceita ${term.milestone}, que ${project.code} não tem.`);
    }
    const fields: Record<string, string> = { milestoneId, title: term.title };
    const by = personOf(term.by).api;
    if (term.attached === true) {
      const form = termForm(fields, term.file, term.lines);
      must(
        await by.POST('/api/projects/{projectId}/acceptances/attached', {
          params: { path: { projectId } },
          body: {},
          bodySerializer: () => form,
        }),
        `Termo ${term.title}`,
      );
      continue;
    }
    if (term.message !== undefined) {
      fields.message = term.message;
    }
    (term.signers ?? []).forEach((signer, index) => {
      fields[`signers[${String(index)}].name`] = signer.name;
      fields[`signers[${String(index)}].email`] = signer.email;
    });
    const request = termForm(fields, term.file, term.lines);
    const termId = must(
      await by.POST('/api/projects/{projectId}/acceptances', {
        params: { path: { projectId } },
        body: {},
        bodySerializer: () => request,
      }),
      `Pedido de assinatura ${term.title}`,
    ).id;
    const answers = term.answers ?? [];
    if (answers.length === 0) {
      continue;
    }
    // The links of the signers, which only the local provider gives (Signature:Provider = Local).
    const details = must(
      await by.GET('/api/projects/{projectId}/acceptances/{termId}', {
        params: { path: { projectId, termId } },
      }),
      `Termo ${term.title}`,
    );
    for (const answer of answers) {
      const link = details.signers[answer.signer]?.signingLink;
      if (link === null || link === undefined) {
        throw new Error(
          `${term.title}: a API não deu o link do signatário ${String(answer.signer + 1)}; confira o provedor local de assinatura (Signature:Provider).`,
        );
      }
      const path = { params: { path: { token: link.slice('/sign/'.length) } } };
      done(
        'refuses' in answer
          ? await anonymous.POST('/api/local-signatures/{token}/refuse', {
              ...path,
              body: { reason: answer.refuses },
            })
          : await anonymous.POST('/api/local-signatures/{token}/sign', path),
        `Resposta do signatário de ${term.title}`,
      );
    }
  }
}
