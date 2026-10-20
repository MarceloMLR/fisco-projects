import createClient from 'openapi-fetch';
import type { paths } from '../../src/api/schema';
import {
  administrator,
  dayFrom,
  demoClients,
  type DemoPerson,
  demoPeople,
  type DemoProject,
  demoProjects,
  firstDayFor,
  mainProject,
  manager,
  projectManagers,
} from './plan.ts';

/**
 * Loads the demonstration data through the API (ADR-0089), each action by the person who would do it on the screens:
 * the Administrator creates the clients and the projects, the manager builds the schedules and each person in charge
 * of a task updates its progress. It changes nothing when the main project is already there, and stops where the local
 * sign-in is off, as in production (ADR-0078). It ends saying where the screens open.
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
async function signIn(baseUrl: string, person: DemoPerson): Promise<{ api: Api; id: string }> {
  const api = apiFor(baseUrl);
  const signedIn = await api.POST('/api/session/local', { body: { email: demoPeople[person] } });
  if (!signedIn.response.ok) {
    throw new Error(
      `A entrada local recusou ${demoPeople[person]} (${String(signedIn.response.status)}): confira o diretório de demonstração (LocalSignIn:People).`,
    );
  }
  const session = must(await api.GET('/api/session'), `Sessão de ${demoPeople[person]}`);
  return { api, id: session.id };
}

export async function loadDemo(
  baseUrl: string,
  log: (line: string) => void = () => undefined,
): Promise<DemoOutcome> {
  const anonymous = apiFor(baseUrl);
  const options = await anonymous.GET('/api/session/sign-in-options');
  if (!options.response.ok || options.data?.localSignIn !== true) {
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
  const found = must(
    await admin.api.GET('/api/projects', {
      params: { query: { search: mainProject, situation: 'All', pageSize: 25 } },
    }),
    'Lista de projetos',
  );
  if (found.items.some((project) => project.code === mainProject)) {
    log(`Os dados de demonstração já estão carregados (${mainProject} existe). Nada mudou.`);
    log(screensLine);
    return { loaded: false, reason: 'already-loaded' };
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

  for (const person of projectManagers) {
    done(
      await admin.api.PUT('/api/users/{id}/role', {
        params: { path: { id: personOf(person).id } },
        body: { role: 'ProjectManager' },
      }),
      `Papel de ${demoPeople[person]}`,
    );
  }

  const clients = new Map<string, string>();
  for (const name of demoClients) {
    const created = must(
      await admin.api.POST('/api/clients', { body: { name, cnpj: null, observation: null } }),
      `Cliente ${name}`,
    );
    clients.set(name, created.id);
  }
  log(`Clientes: ${String(clients.size)}.`);

  const firstDay = firstDayFor(found.today);
  let items = 0;
  for (const project of demoProjects) {
    items += await loadProject(project, firstDay, admin.api, clients, personOf);
  }
  const tickets = demoProjects.reduce(
    (total, project) => total + (project.tickets?.length ?? 0),
    0,
  );
  log(
    `Projetos: ${String(demoProjects.length)}, com ${String(items)} tarefas e marcos, a partir de ${firstDay}, e ${String(tickets)} chamados.`,
  );
  log(screensLine);
  return { loaded: true, firstDay, projects: demoProjects.length, items };
}

/** Creates a project with its people, builds its schedule and puts it in its status; gives how many items it has. */
async function loadProject(
  project: DemoProject,
  firstDay: string,
  admin: Api,
  clients: ReadonlyMap<string, string>,
  personOf: (person: DemoPerson) => { api: Api; id: string },
): Promise<number> {
  const created = must(
    await admin.POST('/api/projects', {
      body: {
        code: project.code,
        name: project.name,
        description: null,
        clientId: project.client === null ? null : (clients.get(project.client) ?? null),
        managerId: personOf(manager).id,
        techLeadId: project.techLead === undefined ? null : personOf(project.techLead).id,
      },
    }),
    `Projeto ${project.code}`,
  );
  const id = created.id;
  for (const [person, role] of project.members ?? []) {
    done(
      await admin.POST('/api/projects/{id}/members', {
        params: { path: { id } },
        body: { userId: personOf(person).id, directoryId: null, role },
      }),
      `Membro ${demoPeople[person]} de ${project.code}`,
    );
  }

  const builder = personOf(manager).api;
  const ids = new Map<string, string>();
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
            progress: 0,
            predecessorIds: (item.after ?? []).map((name) => idOf(name, item.name)),
            parentId: item.inside === undefined ? null : idOf(item.inside, item.name),
          },
        }),
        `Item ${item.name} de ${project.code}`,
      ).id;
      ids.set(item.name, itemId);
      if (item.progress !== undefined) {
        // The person in charge updates the progress of the task; without one, the manager (ADR-0043).
        const who = personOf(item.owner ?? manager).api;
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

  // Each ticket opened by its person, while the project is open (ADR-0022).
  for (const ticket of project.tickets ?? []) {
    done(
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
  }

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
