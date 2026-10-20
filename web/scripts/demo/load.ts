import createClient from 'openapi-fetch';
import type { paths } from '../../src/api/schema';
import { pdfOf } from './pdf.ts';
import {
  administrator,
  dayFrom,
  demoClients,
  type DemoDocument,
  type DemoMessage,
  demoNames,
  type DemoPerson,
  demoPeople,
  type DemoProject,
  demoProjects,
  demoVendors,
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

  const vendors = new Map<string, string>();
  for (const vendor of demoVendors) {
    const added = must(
      await admin.api.POST('/api/admin/vendors', { body: vendor }),
      `Fornecedor ${vendor.name}`,
    );
    vendors.set(vendor.name, added.id);
  }
  log(`Fornecedores: ${String(vendors.size)}.`);

  const firstDay = firstDayFor(found.today);
  let items = 0;
  for (const project of demoProjects) {
    items += await loadProject(
      project,
      firstDay,
      found.today,
      admin.api,
      clients,
      vendors,
      personOf,
    );
  }
  const tickets = demoProjects.reduce(
    (total, project) => total + (project.tickets?.length ?? 0),
    0,
  );
  const documents = demoProjects.reduce(
    (total, project) => total + (project.documents?.length ?? 0),
    0,
  );
  log(
    `Projetos: ${String(demoProjects.length)}, com ${String(items)} tarefas e marcos, a partir de ${firstDay}, ${String(tickets)} chamados e ${String(documents)} documentos.`,
  );
  log(screensLine);
  return { loaded: true, firstDay, projects: demoProjects.length, items };
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

/** Creates a project with its people, builds its schedule and puts it in its status; gives how many items it has. */
async function loadProject(
  project: DemoProject,
  firstDay: string,
  today: string,
  admin: Api,
  clients: ReadonlyMap<string, string>,
  vendors: ReadonlyMap<string, string>,
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
    // The conversation, each message by its person, with the mention and the file (ADR-0026).
    for (const message of ticket.messages ?? []) {
      const form = new FormData();
      form.append('body', JSON.stringify(documentOf(message, personOf)));
      if (message.file !== undefined) {
        form.append(
          'files',
          new Blob([message.file.content], { type: 'application/xml' }),
          message.file.name,
        );
      }
      done(
        await personOf(message.by).api.POST('/api/tickets/{number}/messages', {
          params: { path: { number: opened.number } },
          body: { files: [] },
          bodySerializer: () => form,
        }),
        `Mensagem no chamado ${ticket.title}`,
      );
    }
    // Then the status, each action by who may take it (ADR-0024).
    for (const change of ticket.changes ?? []) {
      done(
        await personOf(change.by).api.POST('/api/tickets/{number}/transitions', {
          params: { path: { number: opened.number } },
          body: {
            action: change.action,
            body:
              change.text === undefined
                ? null
                : JSON.stringify(documentOf({ by: change.by, text: change.text }, personOf)),
          },
        }),
        `Status do chamado ${ticket.title}`,
      );
    }
    // Then the tickets at vendors, registered on the day of the load and updated (ADR-0028).
    for (const atVendor of ticket.atVendors ?? []) {
      const registered = must(
        await personOf(atVendor.by).api.POST('/api/tickets/{number}/vendor-tickets', {
          params: { path: { number: opened.number } },
          body: {
            vendorId: vendors.get(atVendor.vendor) ?? null,
            code: atVendor.code,
            openedOn: today,
            status: atVendor.status,
            link: null,
            note: atVendor.note ?? null,
          },
        }),
        `Chamado ${atVendor.code} no fornecedor`,
      );
      if (atVendor.update !== undefined) {
        done(
          await personOf(atVendor.update.by).api.PUT('/api/tickets/{number}/vendor-tickets/{id}', {
            params: { path: { number: opened.number, id: registered.id } },
            body: { status: atVendor.update.status, note: atVendor.update.note },
          }),
          `Atualização do chamado ${atVendor.code} no fornecedor`,
        );
      }
    }
    // Then the people and the team, each by who may change them (ADR-0068).
    if (ticket.assignee !== undefined) {
      done(
        await personOf(ticket.assignee.by).api.PUT('/api/tickets/assignee', {
          body: { numbers: [opened.number], assigneeId: personOf(ticket.assignee.to).id },
        }),
        `Responsável do chamado ${ticket.title}`,
      );
    }
    if (ticket.transfer !== undefined) {
      done(
        await personOf(ticket.transfer.by).api.PUT('/api/tickets/{number}/team', {
          params: { path: { number: opened.number } },
          body: { team: ticket.transfer.to },
        }),
        `Time do chamado ${ticket.title}`,
      );
    }
  }

  if ((project.documents ?? []).length > 0) {
    await loadDocuments(project, id, ids, phaseIds, builder, personOf);
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

/** The content of a document of the plan: a PDF with the lines, or the lines as text (CSV, TXT). */
function fileOf(document: { name: string }, lines: readonly string[]): Blob {
  return document.name.toLowerCase().endsWith('.pdf')
    ? new Blob([pdfOf(lines)], { type: 'application/pdf' })
    : new Blob([lines.join('\n')], { type: 'text/plain' });
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
 * link with the schedule (ADR-0031, ADR-0045).
 */
async function loadDocuments(
  project: DemoProject,
  projectId: string,
  items: ReadonlyMap<string, string>,
  phases: ReadonlyMap<string, string>,
  builder: Api,
  personOf: (person: DemoPerson) => { api: Api; id: string },
): Promise<void> {
  const area = must(
    await builder.GET('/api/projects/{projectId}/documents/folders', {
      params: { path: { projectId } },
    }),
    `Pastas de ${project.code}`,
  );
  for (const document of project.documents ?? []) {
    const folderId = area.folders.find((folder) => folder.name === document.folder)?.id;
    if (folderId === undefined) {
      throw new Error(`${document.name} vai para ${document.folder}, que ${project.code} não tem.`);
    }
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
