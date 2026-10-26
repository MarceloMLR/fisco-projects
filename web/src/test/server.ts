import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { components } from '../api/schema';
import {
  demoDefaultFolders,
  demoDocumentArea,
  demoDocumentDetails,
  demoDocuments,
  documentsPage,
} from './demoDocuments';
import { demoLinkCategories, demoLinks, linksList } from './demoLinks';
import { credentialsList, demoCredentials } from './demoCredentials';
import { emptyMyWork } from './demoMyWork';
import { emptyOverview } from './demoOverview';
import { cleanCnpj } from '../format/cnpj';
import type {
  CalendarDay,
  CalendarDayKind,
  CalendarYear,
  ClientChoice,
  ClientRow,
  DayOfWeek,
  Installation,
  Person,
  Profile,
  Project,
  ProjectMember,
  ProjectRow,
  ProjectTemplate,
  ProjectScheduleSummary,
  ProjectStatus,
  Schedule,
  ScheduleItem,
  SessionUser,
  Ticket,
  TicketChoice,
  TicketCounts,
  TicketFlow,
  TicketRow,
  Vendor,
} from '../api/client';

type UserResponse = components['schemas']['UserResponse'];
type AuditEntryResponse = components['schemas']['AuditEntryResponse'];

// The API, simulated for the screen tests (ADR-0056). By default Marina is signed in and the local sign-in is on;
// each test changes only what it needs with server.use(...).

export const marina: SessionUser = {
  id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70',
  name: 'Marina Costa',
  email: 'marina.costa@empresa.example',
  role: 'User',
  theme: 'Automatic',
};

/**
 * People of the simulated installation, as the Users screen of the canvas shows them. The tests run in the time
 * zone set in vite.config.ts, where 2026-10-19T15:00:00Z is 12:00.
 */
export const demoUsers: UserResponse[] = [
  person(1, 'Paulo Nogueira', 'Administrator', 'Active', '2026-10-19T15:00:00Z'),
  person(2, 'Marina Costa', 'ProjectManager', 'Active', '2026-10-19T12:12:00Z'),
  person(3, 'Beatriz Lima', 'ProjectManager', 'Active', '2026-10-19T11:47:00Z'),
  person(4, 'Rafael Souza', 'User', 'Active', '2026-10-18T20:30:00Z'),
  person(5, 'Lívia Prado', 'User', 'Inactive', '2026-08-28T18:00:00Z'),
];

function person(
  index: number,
  name: string,
  role: UserResponse['role'],
  status: UserResponse['status'],
  lastSignInAt: string,
): UserResponse {
  const email = `${name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(' ', '.')}@empresa.example`;
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-00000000000${String(index)}`,
    name,
    email,
    role,
    status,
    lastSignInAt,
  };
}

const withoutAccents = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();

/** The list of users as the API answers it: search without accents, filters and pages, in alphabetical order. */
export function usersPage(users: readonly UserResponse[], url: URL) {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const role = url.searchParams.get('role');
  const status = url.searchParams.get('status');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = users
    .filter(
      (user) =>
        withoutAccents(user.name).includes(search) || withoutAccents(user.email).includes(search),
    )
    .filter((user) => role === null || user.role === role)
    .filter((user) => status === null || user.status === status)
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

function client(
  index: number,
  name: string,
  cnpj: string | null,
  observation: string | null,
  status: ClientRow['status'] = 'Active',
): ClientRow {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-30000000000${String(index)}`,
    name,
    cnpj,
    logoUrl: null,
    observation,
    status,
  };
}

/** Clients of the simulated installation, as the Clients screen of the canvas shows them; only Alfa has a logo. */
export function demoClients(): ClientRow[] {
  return [
    {
      ...client(1, 'Alfa Distribuidora', '11222333000181', 'Matriz em Campinas'),
      logoUrl: '/api/clients/0199f5a2-7c1e-7d40-9a1b-300000000001/logo?v=1',
    },
    client(2, 'Beta Alimentos', '22333444000181', 'Filiais SP e RS'),
    client(3, 'Épsilon Logística', null, 'Contrato encerrado em 2025', 'Inactive'),
    client(4, 'Gama Têxtil', '12ABC34501DE35', null),
    client(5, 'Kappa Energia', null, 'Cliente novo, sem projeto ainda'),
  ];
}

/** The list of clients as the API answers it: search by name without accents or by CNPJ, status and pages. */
export function clientsPage(clients: readonly ClientRow[], url: URL) {
  const search = (url.searchParams.get('search') ?? '').trim();
  const cnpj = cleanCnpj(search);
  const status = url.searchParams.get('status');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = clients
    .filter(
      (row) =>
        withoutAccents(row.name).includes(withoutAccents(search)) ||
        (cnpj !== '' && (row.cnpj ?? '').includes(cnpj)),
    )
    .filter((row) => status === null || row.status === status)
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

/** The clients to choose, as the API answers them: the name, the logo and the status, without the CNPJ. */
export function clientChoicesOf(clients: readonly ClientRow[]): ClientChoice[] {
  return clients
    .map(({ id, name, logoUrl, status }) => ({ id, name, logoUrl, status }))
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
}

/** A project of the simulated installation, with the people in it and what Marina may do. */
export interface DemoProject extends Project {
  /** Whether Marina is in the project, so it shows in her list. */
  mine: boolean;
  /** The numbers of its schedule in the list of projects; none without tasks and milestones. */
  schedule: ProjectScheduleSummary | null;
}

/** Today in the simulated installation, the day the schedules are seen on (as in the canvas). */
export const demoToday = '2026-10-16';

/** The people of the projects; Marina is the person in the session. */
export const demoPeople = {
  paulo: { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' },
  marina: { id: marina.id, name: marina.name },
  beatriz: { id: '0199f5a2-7c1e-7d40-9a1b-000000000003', name: 'Beatriz Lima' },
  rafael: { id: '0199f5a2-7c1e-7d40-9a1b-000000000004', name: 'Rafael Souza' },
  carla: { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' },
  diego: { id: '0199f5a2-7c1e-7d40-9a1b-000000000006', name: 'Diego Torres' },
} as const;

/** The e-mail of a person of the demonstration, from the name. */
export const emailOf = (name: string) =>
  `${withoutAccents(name).replace(' ', '.')}@empresa.example`;

/** When the people last signed in; who is not here never did. */
const lastSignIns: Record<string, string> = {
  [demoPeople.paulo.id]: '2026-10-19T15:00:00Z',
  [demoPeople.marina.id]: '2026-10-19T12:12:00Z',
  [demoPeople.beatriz.id]: '2026-10-19T11:47:00Z',
  [demoPeople.rafael.id]: '2026-10-18T20:30:00Z',
  [demoPeople.diego.id]: '2026-10-14T13:00:00Z',
};

const managers = new Set<string>([
  demoPeople.paulo.id,
  demoPeople.marina.id,
  demoPeople.beatriz.id,
]);

/** Who may lead a project technically: the Administrator and the Tech lead of the system (ADR-0097). */
const techLeads = new Set<string>([demoPeople.paulo.id, demoPeople.rafael.id]);

/** A user to choose. */
const user = (person: { id: string; name: string }): Person => ({
  ...person,
  directoryId: null,
  email: emailOf(person.name),
  canBeManager: managers.has(person.id),
  canBeTechLead: techLeads.has(person.id),
});

/**
 * The people to choose, as the API answers them: the users and, in the directory, Lucas Prado and Sofia Martins, who
 * never signed in.
 */
export const demoPeopleChoices: Person[] = [
  user(demoPeople.beatriz),
  user(demoPeople.carla),
  user(demoPeople.diego),
  {
    id: null,
    directoryId: 'local:lucas.prado@empresa.example',
    name: 'Lucas Prado',
    email: 'lucas.prado@empresa.example',
    canBeManager: false,
    canBeTechLead: false,
  },
  user(demoPeople.marina),
  user(demoPeople.paulo),
  user(demoPeople.rafael),
  {
    id: null,
    directoryId: 'local:sofia.martins@empresa.example',
    name: 'Sofia Martins',
    email: 'sofia.martins@empresa.example',
    canBeManager: false,
    canBeTechLead: false,
  },
];

/**
 * The people to choose as the API answers them: by name without accents, only the managers or the tech leads when
 * asked.
 */
export function peopleOf(url: URL): Person[] {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const onlyManagers = url.searchParams.get('managers') === 'true';
  const onlyTechLeads = url.searchParams.get('techLeads') === 'true';
  const limit = Number(url.searchParams.get('limit') ?? '20');
  return demoPeopleChoices
    .filter((person) => !onlyManagers || person.canBeManager)
    .filter((person) => !onlyTechLeads || person.canBeTechLead)
    .filter(
      (person) => withoutAccents(person.name).includes(search) || person.email.includes(search),
    )
    .slice(0, limit);
}

/**
 * The people of a project as the Members tab reads them (GET /api/projects/{id}/members): by name, with the e-mail, when
 * they joined, the last access and whether they may be managers.
 */
export function membersOf(project: Project): ProjectMember[] {
  return project.members
    .map((member) => ({
      ...member,
      email: emailOf(member.name),
      addedAt: '2026-10-05T12:00:00Z',
      lastSignInAt: lastSignIns[member.id] ?? null,
      canBeManager: managers.has(member.id),
      canBeTechLead: techLeads.has(member.id),
    }))
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
}

function demoProject(
  index: number,
  code: string,
  name: string,
  client: ClientRow | null,
  status: ProjectStatus,
  members: Project['members'],
  mine = true,
): DemoProject {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-40000000000${String(index)}`,
    code,
    name,
    description: null,
    client: client === null ? null : { id: client.id, name: client.name, logoUrl: client.logoUrl },
    status,
    members,
    permissions: {
      canEdit: status !== 'Completed' && status !== 'Cancelled',
      canChangeStatus: true,
      canManagePeople: status !== 'Completed' && status !== 'Cancelled',
      canManageManagers: status !== 'Completed' && status !== 'Cancelled',
      canEditSchedule: status !== 'Completed' && status !== 'Cancelled',
      canDelete: false,
    },
    scheduleStart: null,
    scheduleEnd: null,
    mine,
    schedule: null,
  };
}

/** A project with the numbers of its schedule, and its dates in the header. */
function withSchedule(
  project: DemoProject,
  summary: Omit<ProjectScheduleSummary, 'lateDays'> &
    Partial<Pick<ProjectScheduleSummary, 'lateDays'>>,
): DemoProject {
  const schedule = { lateDays: 0, ...summary };
  return { ...project, schedule, scheduleStart: schedule.start, scheduleEnd: schedule.end };
}

/**
 * A task or milestone of the demonstration schedule, as the manager sees it: the progress of every task may be
 * updated.
 */
function demoItem(
  index: number,
  phaseId: string,
  item: Pick<ScheduleItem, 'name' | 'start' | 'end'> & Partial<ScheduleItem>,
): ScheduleItem {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-6000000000${String(index).padStart(2, '0')}`,
    kind: 'Task',
    phaseId,
    duration: 1,
    progress: 0,
    completedOn: null,
    reachedOn: null,
    late: false,
    lateDays: 0,
    critical: false,
    baselineStart: null,
    baselineEnd: null,
    baselineDeviation: null,
    canUpdateProgress: item.kind !== 'Milestone',
    owner: null,
    ownedByClient: false,
    predecessorIds: [],
    parentId: null,
    hasSubtasks: false,
    ...item,
  };
}

/**
 * The schedule as a person who does not edit it sees it (ADR-0043): the progress may be updated only in the tasks the
 * person is in charge of, and in none when the person is a viewer.
 */
export function scheduleSeenBy(schedule: Schedule, personId: string, viewer = false): Schedule {
  return {
    ...schedule,
    phases: schedule.phases.map((phase) => ({
      ...phase,
      items: phase.items.map((item) => ({
        ...item,
        canUpdateProgress: !viewer && item.kind === 'Task' && item.owner?.id === personId,
      })),
    })),
  };
}

/**
 * The non-business days the API sends with the demonstration schedule: the weekends from September to December 2026,
 * and the holidays of 12/10 and 02/11.
 */
function demoNonWorkingDays(): string[] {
  const days: string[] = [];
  for (
    let day = new Date(Date.UTC(2026, 8, 1));
    day.getUTCFullYear() === 2026;
    day.setUTCDate(day.getUTCDate() + 1)
  ) {
    const text = day.toISOString().slice(0, 10);
    if (
      day.getUTCDay() === 0 ||
      day.getUTCDay() === 6 ||
      text === '2026-10-12' ||
      text === '2026-11-02'
    ) {
      days.push(text);
    }
  }
  return days;
}

const phaseIds = {
  survey: '0199f5a2-7c1e-7d40-9a1b-500000000001',
  configuration: '0199f5a2-7c1e-7d40-9a1b-500000000002',
  approval: '0199f5a2-7c1e-7d40-9a1b-500000000003',
  production: '0199f5a2-7c1e-7d40-9a1b-500000000004',
} as const;

/**
 * The schedule of ERP-FIN on 16/10/2026, as the canvas ProjetoCronograma, in business days with 12/10 and 02/11 as
 * holidays: Levantamento, late by Validar cadastro de produtos; Configuração; Homologação, with the milestone; and
 * Entrada em produção, still without items. The critical path goes from the opening to the acceptance, through
 * Parametrizar regras de ICMS and Testes integrados.
 */
export function demoSchedule(): Schedule {
  const marinaOwner = { id: demoPeople.marina.id, name: demoPeople.marina.name };
  const rafaelOwner = { id: demoPeople.rafael.id, name: demoPeople.rafael.name };
  const opening = demoItem(1, phaseIds.survey, {
    critical: true,
    name: 'Reunião de abertura',
    start: '2026-10-05',
    end: '2026-10-05',
    progress: 100,
    completedOn: '2026-10-05',
    owner: marinaOwner,
  });
  const mapping = demoItem(2, phaseIds.survey, {
    critical: true,
    name: 'Mapear processos fiscais',
    start: '2026-10-06',
    end: '2026-10-09',
    duration: 4,
    progress: 100,
    completedOn: '2026-10-09',
    owner: rafaelOwner,
    predecessorIds: [opening.id],
  });
  const validating = demoItem(3, phaseIds.survey, {
    name: 'Validar cadastro de produtos',
    start: '2026-10-13',
    end: '2026-10-15',
    duration: 3,
    progress: 80,
    late: true,
    lateDays: 1,
    owner: rafaelOwner,
    predecessorIds: [mapping.id],
  });
  const icms = demoItem(4, phaseIds.configuration, {
    critical: true,
    name: 'Parametrizar regras de ICMS',
    start: '2026-10-13',
    end: '2026-10-22',
    duration: 8,
    progress: 50,
    owner: rafaelOwner,
    predecessorIds: [mapping.id],
  });
  const invoices = demoItem(5, phaseIds.configuration, {
    name: 'Integrar emissão de NF-e',
    start: '2026-10-19',
    end: '2026-10-28',
    duration: 8,
    progress: 20,
  });
  const tests = demoItem(6, phaseIds.configuration, {
    critical: true,
    name: 'Testes integrados',
    start: '2026-10-23',
    end: '2026-10-29',
    duration: 5,
    owner: marinaOwner,
    predecessorIds: [icms.id],
  });
  const clientTests = demoItem(7, phaseIds.approval, {
    critical: true,
    name: 'Testes com a equipe do cliente',
    start: '2026-10-30',
    end: '2026-11-03',
    duration: 2,
    predecessorIds: [tests.id],
  });
  const acceptance = demoItem(8, phaseIds.approval, {
    critical: true,
    kind: 'Milestone',
    name: 'Aceite da homologação',
    start: '2026-11-03',
    end: '2026-11-03',
    duration: 0,
    predecessorIds: [clientTests.id],
  });
  return {
    today: demoToday,
    start: '2026-10-05',
    end: '2026-11-03',
    progress: 42,
    lateTasks: 1,
    lateMilestones: 0,
    nonWorkingDays: demoNonWorkingDays(),
    baseline: null,
    undo: null,
    redo: null,
    phases: [
      {
        id: phaseIds.survey,
        name: 'Levantamento',
        start: '2026-10-05',
        end: '2026-10-15',
        progress: 93,
        late: true,
        items: [opening, mapping, validating],
      },
      {
        id: phaseIds.configuration,
        name: 'Configuração',
        start: '2026-10-13',
        end: '2026-10-29',
        progress: 27,
        late: false,
        items: [icms, invoices, tests],
      },
      {
        id: phaseIds.approval,
        name: 'Homologação',
        start: '2026-10-30',
        end: '2026-11-03',
        progress: 0,
        late: false,
        items: [clientTests, acceptance],
      },
      {
        id: phaseIds.production,
        name: 'Entrada em produção',
        start: null,
        end: null,
        progress: 0,
        late: false,
        items: [],
      },
    ],
  };
}

/**
 * The schedule of ERP-FIN with the baseline the manager saved on 09/10/2026 (ADR-0021), before Parametrizar regras de
 * ICMS took two business days more: it and the items after it end 2 business days after the baseline, as the project
 * does; the others are on it.
 */
export function demoScheduleWithBaseline(): Schedule {
  const schedule = demoSchedule();
  const slipped = new Map([
    ['Parametrizar regras de ICMS', ['2026-10-13', '2026-10-20']],
    ['Testes integrados', ['2026-10-21', '2026-10-27']],
    ['Testes com a equipe do cliente', ['2026-10-28', '2026-10-29']],
    ['Aceite da homologação', ['2026-10-29', '2026-10-29']],
  ]);
  for (const phase of schedule.phases) {
    phase.items = phase.items.map((item) => {
      const [start, end] = slipped.get(item.name) ?? [item.start, item.end];
      return {
        ...item,
        baselineStart: start ?? item.start,
        baselineEnd: end ?? item.end,
        baselineDeviation: slipped.has(item.name) ? 2 : 0,
      };
    });
  }
  return {
    ...schedule,
    baseline: { savedOn: '2026-10-09', start: '2026-10-05', end: '2026-10-29', deviation: 2 },
  };
}

/**
 * The schedule of ERP-FIN with "Testes integrados" broken into subtasks (ADR-0088): "Roteiro de testes" took the
 * dependency on "Parametrizar regras de ICMS", "Executar testes" follows it, and the tests with the client wait for the
 * last one. The task is a summary, with the dates of its subtasks, and nobody updates its progress.
 */
export function demoScheduleWithSubtasks(): Schedule {
  const schedule = demoSchedule();
  const configuration = schedule.phases[1];
  const approval = schedule.phases[2];
  const [icms, invoices, tests] = configuration?.items ?? [];
  const clientTests = approval?.items[0];
  if (
    configuration === undefined ||
    approval === undefined ||
    icms === undefined ||
    invoices === undefined ||
    tests === undefined ||
    clientTests === undefined
  ) {
    throw new Error('No Configuração or Homologação.');
  }
  const script = demoItem(9, configuration.id, {
    name: 'Roteiro de testes',
    start: '2026-10-23',
    end: '2026-10-26',
    duration: 2,
    owner: { id: demoPeople.carla.id, name: demoPeople.carla.name },
    predecessorIds: [icms.id],
    parentId: tests.id,
  });
  const run = demoItem(10, configuration.id, {
    name: 'Executar testes',
    start: '2026-10-27',
    end: '2026-10-29',
    duration: 3,
    predecessorIds: [script.id],
    parentId: tests.id,
  });
  configuration.items = [
    icms,
    invoices,
    { ...tests, predecessorIds: [], hasSubtasks: true, canUpdateProgress: false },
    script,
    run,
  ];
  approval.items = [{ ...clientTests, predecessorIds: [run.id] }, ...approval.items.slice(1)];
  return schedule;
}

/** A schedule without phases, as a new project has. */
export const emptySchedule: Schedule = {
  today: demoToday,
  start: null,
  end: null,
  progress: 0,
  lateTasks: 0,
  lateMilestones: 0,
  phases: [],
  nonWorkingDays: [],
  baseline: null,
  undo: null,
  redo: null,
};

/**
 * Projects of the simulated installation, as the Projects screen of the canvas shows them: Marina manages most of them;
 * the last one is Beatriz's, which Marina does not see.
 */
export function demoProjects(): DemoProject[] {
  const [alfa, beta, , gama] = demoClients();
  const manager = (person: { id: string; name: string }) => ({
    ...person,
    role: 'Manager' as const,
  });
  const techLead = (person: { id: string; name: string }) => ({
    ...person,
    role: 'TechLead' as const,
  });
  return [
    withSchedule(
      {
        ...demoProject(1, 'ERP-FIN', 'Implantação do ERP financeiro', alfa ?? null, 'InProgress', [
          manager(demoPeople.marina),
          techLead(demoPeople.rafael),
        ]),
        description: 'Implantação do ERP na matriz, com o financeiro e a contabilidade.',
      },
      {
        start: '2026-10-05',
        end: '2026-11-03',
        progress: 42,
        lateTasks: 1,
        lateMilestones: 0,
        lateDays: 2,
        currentPhase: 'Levantamento',
        completedOn: null,
        phases: [
          {
            name: 'Levantamento',
            start: '2026-10-05',
            end: '2026-10-15',
            progress: 93,
            late: true,
          },
          {
            name: 'Configuração',
            start: '2026-10-13',
            end: '2026-10-29',
            progress: 27,
            late: false,
          },
          { name: 'Homologação', start: '2026-10-30', end: '2026-11-03', progress: 0, late: false },
        ],
        milestones: ['2026-11-03'],
      },
    ),
    withSchedule(
      demoProject(2, 'TRIB-RS', 'Revisão tributária da filial RS', beta ?? null, 'Planning', [
        manager(demoPeople.marina),
      ]),
      {
        start: '2026-10-26',
        end: '2027-01-29',
        progress: 0,
        lateTasks: 0,
        lateMilestones: 0,
        currentPhase: null,
        completedOn: null,
        phases: [
          { name: 'Diagnóstico', start: '2026-10-26', end: '2026-11-20', progress: 0, late: false },
          { name: 'Apuração', start: '2026-11-23', end: '2026-12-30', progress: 0, late: false },
          { name: 'Entrega', start: '2027-01-04', end: '2027-01-29', progress: 0, late: false },
        ],
        milestones: ['2027-01-29'],
      },
    ),
    demoProject(3, 'PORTAL-NF', 'Portal de notas para fornecedores', null, 'Paused', [
      manager(demoPeople.marina),
    ]),
    withSchedule(
      demoProject(4, 'WMS-ALFA', 'Implantação do WMS', alfa ?? null, 'Completed', [
        manager(demoPeople.marina),
      ]),
      {
        start: '2026-06-01',
        end: '2026-08-28',
        progress: 100,
        lateTasks: 0,
        lateMilestones: 0,
        currentPhase: 'Entrada em produção',
        completedOn: '2026-08-28',
        phases: [
          {
            name: 'Levantamento',
            start: '2026-06-01',
            end: '2026-06-26',
            progress: 100,
            late: false,
          },
          {
            name: 'Configuração',
            start: '2026-06-29',
            end: '2026-08-07',
            progress: 100,
            late: false,
          },
          {
            name: 'Entrada em produção',
            start: '2026-08-10',
            end: '2026-08-28',
            progress: 100,
            late: false,
          },
        ],
        milestones: ['2026-08-28'],
      },
    ),
    demoProject(5, 'CONC-BAN', 'Conciliação bancária automática', gama ?? null, 'Cancelled', [
      manager(demoPeople.marina),
    ]),
    demoProject(
      6,
      'FISC-SP',
      'Revisão fiscal da filial SP',
      beta ?? null,
      'InProgress',
      [manager(demoPeople.beatriz)],
      false,
    ),
  ];
}

/** The list of projects as the API answers it for Marina: her projects, by search, client and situation. */
export function projectsPage(projects: readonly DemoProject[], url: URL) {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const clientId = url.searchParams.get('clientId');
  const situation = url.searchParams.get('situation') ?? 'Active';
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const closed = (status: ProjectStatus) => status === 'Completed' || status === 'Cancelled';
  const matching = projects
    .filter((project) => project.mine)
    .filter(
      (project) =>
        withoutAccents(project.name).includes(search) ||
        withoutAccents(project.code).includes(search),
    )
    .filter((project) => clientId === null || project.client?.id === clientId)
    .filter((project) => situation === 'All' || (situation === 'Closed') === closed(project.status))
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
  const rows: ProjectRow[] = matching.map(
    ({ id, code, name, client, status, members, schedule }) => ({
      id,
      code,
      name,
      client,
      status,
      managers: members.filter((member) => member.role === 'Manager'),
      schedule,
    }),
  );
  return {
    items: rows.slice((page - 1) * pageSize, page * pageSize),
    total: rows.length,
    today: demoToday,
  };
}

const paulo = { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' };
const marinaActor = { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' };

function auditEntry(
  index: number,
  occurredAt: string,
  actor: AuditEntryResponse['actor'],
  record: Pick<AuditEntryResponse, 'action' | 'itemType'> & Partial<AuditEntryResponse>,
): AuditEntryResponse {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-1000000000${String(index).padStart(2, '0')}`,
    occurredAt,
    actor,
    itemId: null,
    itemLabel: null,
    detail: null,
    changes: [],
    ...record,
  };
}

/**
 * Audit records of the simulated installation, newest first, as in the Audit screen of the canvas. In the time
 * zone of the tests (vite.config.ts), 2026-10-19T13:14:00Z is 10:14 of October 19th.
 */
export const demoAuditEntries: AuditEntryResponse[] = [
  auditEntry(1, '2026-10-19T13:14:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000003',
    itemLabel: 'Beatriz Lima',
    changes: [
      { field: 'Role', before: 'User', after: 'ProjectManager', secret: false, reference: null },
    ],
  }),
  auditEntry(2, '2026-10-19T12:58:00Z', marinaActor, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: marinaActor.id,
    detail: 'local',
  }),
  auditEntry(3, '2026-10-19T11:05:00Z', null, {
    action: 'SignInRefused',
    itemType: 'SignIn',
    itemLabel: 'joao.silva@parceiro.example',
    detail: 'NotInDirectory',
  }),
  auditEntry(4, '2026-10-18T14:47:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000005',
    itemLabel: 'Lívia Prado',
    changes: [
      { field: 'IsActive', before: 'true', after: 'false', secret: false, reference: null },
    ],
  }),
  auditEntry(5, '2026-10-17T12:02:00Z', null, {
    action: 'Created',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000004',
    itemLabel: 'Rafael Souza',
    changes: [
      {
        field: 'Email',
        before: null,
        after: 'rafael.souza@empresa.example',
        secret: false,
        reference: null,
      },
      { field: 'IsActive', before: null, after: 'true', secret: false, reference: null },
      { field: 'Name', before: null, after: 'Rafael Souza', secret: false, reference: null },
      { field: 'Role', before: null, after: 'User', secret: false, reference: null },
    ],
  }),
  auditEntry(6, '2026-09-10T12:00:00Z', paulo, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: paulo.id,
    detail: 'local',
  }),
];

/** The audit list as the API answers it: by author, item type and period (end excluded), newest first, in pages. */
export function auditPage(entries: readonly AuditEntryResponse[], url: URL) {
  const actorId = url.searchParams.get('actorId');
  const itemType = url.searchParams.get('itemType');
  const from = url.searchParams.get('from');
  const to = url.searchParams.get('to');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = entries
    .filter((entry) => actorId === null || entry.actor?.id === actorId)
    .filter((entry) => itemType === null || entry.itemType === itemType)
    .filter((entry) => from === null || Date.parse(entry.occurredAt) >= Date.parse(from))
    .filter((entry) => to === null || Date.parse(entry.occurredAt) < Date.parse(to))
    .toSorted((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

/** The brand of the simulated installation: the name nobody changed yet, and no logo. */
export const defaultInstallation: Installation = {
  name: 'FiscoProjects',
  logoUrl: null,
  mainColor: null,
  darkLogoUrl: null,
};

/** My profile of a person in the session, who signs in locally and last did so at 09:12 of October 19th. */
export function profileOf(person: SessionUser): Profile {
  return {
    ...person,
    lastSignInAt: '2026-10-19T12:12:00Z',
    signInMethods: ['local'],
    receivesDailySummary: true,
  };
}

/** A local holiday of the simulated calendar, as the Calendar module keeps it. */
export interface DemoLocalHoliday {
  id: string;
  name: string;
  date: string;
  repeatsEveryYear: boolean;
  isConsidered: boolean;
}

/** The calendar of the simulated installation, changed by the requests of a test as the real one would be. */
export interface DemoCalendar {
  workingDays: DayOfWeek[];
  /** The calculated holidays considered, by key. */
  considered: Set<string>;
  local: DemoLocalHoliday[];
}

const calculatedRules: [
  string,
  CalendarDayKind,
  (year: number, easter: number) => number,
  number?,
][] = [
  ['NewYearsDay', 'NationalHoliday', (year) => Date.UTC(year, 0, 1)],
  ['CarnivalMonday', 'OptionalHoliday', (_year, easter) => easter - 48 * 86_400_000],
  ['CarnivalTuesday', 'OptionalHoliday', (_year, easter) => easter - 47 * 86_400_000],
  ['AshWednesday', 'OptionalHoliday', (_year, easter) => easter - 46 * 86_400_000],
  ['GoodFriday', 'NationalHoliday', (_year, easter) => easter - 2 * 86_400_000],
  ['Tiradentes', 'NationalHoliday', (year) => Date.UTC(year, 3, 21)],
  ['LabourDay', 'NationalHoliday', (year) => Date.UTC(year, 4, 1)],
  ['CorpusChristi', 'OptionalHoliday', (_year, easter) => easter + 60 * 86_400_000],
  ['IndependenceDay', 'NationalHoliday', (year) => Date.UTC(year, 8, 7)],
  ['OurLadyOfAparecida', 'NationalHoliday', (year) => Date.UTC(year, 9, 12)],
  ['PublicServantsDay', 'OptionalHoliday', (year) => Date.UTC(year, 9, 28)],
  ['AllSoulsDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 2)],
  ['RepublicDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 15)],
  ['BlackConsciousnessDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 20), 2024],
  ['ChristmasEve', 'OptionalHoliday', (year) => Date.UTC(year, 11, 24)],
  ['ChristmasDay', 'NationalHoliday', (year) => Date.UTC(year, 11, 25)],
  ['NewYearsEve', 'OptionalHoliday', (year) => Date.UTC(year, 11, 31)],
];

/** Easter Sunday of the Gregorian calendar, at midnight UTC (the same rule as the Calendar module). */
function easterOf(year: number): number {
  const a = year % 19;
  const b = Math.floor(year / 100);
  const c = year % 100;
  const h =
    (19 * a + b - Math.floor(b / 4) - Math.floor((b - Math.floor((b + 8) / 25) + 1) / 3) + 15) % 30;
  const l = (32 + 2 * (b % 4) + 2 * Math.floor(c / 4) - h - (c % 4)) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const month = Math.floor((h + l - 7 * m + 114) / 31);
  const day = ((h + l - 7 * m + 114) % 31) + 1;
  return Date.UTC(year, month - 1, day);
}

/** A new simulated calendar: Monday to Friday, the national holidays considered and two local holidays of 2026. */
export function demoCalendar(): DemoCalendar {
  return {
    workingDays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
    considered: new Set(
      calculatedRules.filter(([, kind]) => kind === 'NationalHoliday').map(([key]) => key),
    ),
    local: [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000001',
        name: 'Aniversário da cidade',
        date: '2026-01-25',
        repeatsEveryYear: true,
        isConsidered: true,
      },
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000002',
        name: 'Emenda de Tiradentes',
        date: '2026-04-20',
        repeatsEveryYear: false,
        isConsidered: true,
      },
    ],
  };
}

/** The calendar of a year as the API answers it: the calculated holidays of the year and the local ones, in order. */
export function calendarYearOf(calendar: DemoCalendar, year: number): CalendarYear {
  const easter = easterOf(year);
  const calculated: CalendarDay[] = calculatedRules
    .filter(([, , , since]) => since === undefined || year >= since)
    .map(([key, kind, dateOf]) => ({
      date: new Date(dateOf(year, easter)).toISOString().slice(0, 10),
      kind,
      key,
      id: null,
      name: null,
      repeatsEveryYear: true,
      isConsidered: calendar.considered.has(key),
    }));
  const local: CalendarDay[] = calendar.local.flatMap((holiday) => {
    const holidayYear = Number(holiday.date.slice(0, 4));
    const date =
      holidayYear === year
        ? holiday.date
        : holiday.repeatsEveryYear && year > holidayYear
          ? `${String(year)}${holiday.date.slice(4)}`
          : undefined;
    return date === undefined
      ? []
      : [
          {
            date,
            kind: 'LocalHoliday' as const,
            key: null,
            id: holiday.id,
            name: holiday.name,
            repeatsEveryYear: holiday.repeatsEveryYear,
            isConsidered: holiday.isConsidered,
          },
        ];
  });
  return {
    year,
    workingDays: calendar.workingDays,
    days: [...calculated, ...local].toSorted((first, second) =>
      first.date.localeCompare(second.date),
    ),
  };
}

/**
 * The vendors of the simulated installation (ADR-0028): two active, one with the support portal, and one inactive.
 */
export function demoVendors(): Vendor[] {
  // In the order of the API: by name.
  return [
    {
      id: '0199f5a2-7c1e-7d40-9a1b-900000000003',
      name: 'Arquivo Fiscal Exemplo',
      portalUrl: 'https://arquivo.example',
      isActive: false,
    },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-900000000001',
      name: 'Emissor Exemplo',
      portalUrl: 'https://suporte.emissor.example',
      isActive: true,
    },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-900000000002',
      name: 'Nuvem Fiscal Exemplo',
      portalUrl: null,
      isActive: true,
    },
  ];
}

/**
 * The models of project of the demonstration (ADR-0017): "Implantação de ERP", active, made from ERP-FIN, and "Revisão
 * tributária de filial", inactive.
 */
export function demoTemplates(): ProjectTemplate[] {
  const task = (name: string, startOffset: number, duration: number, isSubtask = false) => ({
    kind: 'Task' as const,
    name,
    startOffset,
    duration,
    isSubtask,
  });
  const milestone = (name: string, startOffset: number) => ({
    kind: 'Milestone' as const,
    name,
    startOffset,
    duration: 0,
    isSubtask: false,
  });
  return [
    {
      id: '0199f5a2-7c1e-7d40-9a1b-700000000001',
      name: 'Implantação de ERP',
      description: 'Fases de uma implantação de ERP financeiro',
      isActive: true,
      phases: [
        {
          name: 'Levantamento',
          items: [task('Reunião de abertura', 0, 1), task('Mapear processos fiscais', 1, 4)],
        },
        {
          name: 'Configuração',
          items: [
            task('Parametrizar regras de ICMS', 5, 8),
            task('Testes integrados', 13, 5),
            task('Roteiro de testes', 13, 2, true),
          ],
        },
        { name: 'Homologação', items: [milestone('Aceite da homologação', 18)] },
      ],
    },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-700000000002',
      name: 'Revisão tributária de filial',
      description: null,
      isActive: false,
      phases: [{ name: 'Diagnóstico', items: [task('Levantar obrigações da filial', 0, 5)] }],
    },
  ];
}

const erpFin = {
  id: '0199f5a2-7c1e-7d40-9a1b-400000000001',
  code: 'ERP-FIN',
  name: 'Implantação do ERP financeiro',
};
const tribRs = {
  id: '0199f5a2-7c1e-7d40-9a1b-400000000002',
  code: 'TRIB-RS',
  name: 'Revisão tributária da filial RS',
};

function demoTicketRow(
  number: number,
  title: string,
  project: typeof erpFin,
  team: TicketRow['team'],
  priority: TicketRow['priority'],
  requester: { id: string; name: string },
  openedAt: string,
): TicketRow {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-7000000001${String(number).padStart(2, '0')}`,
    number,
    title,
    project,
    team,
    priority,
    status: 'Open',
    requester,
    assignee: null,
    openedAt,
    updatedAt: openedAt,
    interactions: 0,
    resolution: { state: 'OnTrack', dueOn: '2026-10-26', daysLate: null },
    canChangeAssignee: false,
    canChangeRequester: false,
    vendor: null,
  };
}

/**
 * Tickets of the simulated installation, as the Chamados screen of the canvas shows them, in the order of the list:
 * the highest priority first and, in each, the oldest first. All Open and without an assignee (Chamados-1).
 */
export function demoTickets(): TicketRow[] {
  return [
    demoTicketRow(
      3,
      'Ajustar alíquota de ICMS-ST para SP',
      tribRs,
      'Functional',
      'Critical',
      demoPeople.carla,
      '2026-10-13T12:30:00Z',
    ),
    demoTicketRow(
      1,
      'Rejeição 539 ao emitir nota de devolução',
      erpFin,
      'Development',
      'High',
      demoPeople.carla,
      '2026-10-14T12:42:00Z',
    ),
    demoTicketRow(
      4,
      'Liberar ambiente de homologação ao cliente',
      erpFin,
      'Development',
      'High',
      demoPeople.marina,
      '2026-10-15T13:10:00Z',
    ),
    demoTicketRow(
      2,
      'Dúvida sobre o CFOP de transferência entre filiais',
      erpFin,
      'Functional',
      'Low',
      demoPeople.diego,
      '2026-10-15T17:05:00Z',
    ),
  ];
}

/** The projects of the filters and of the opening, as Marina sees them. */
export function demoTicketChoices(): TicketChoice[] {
  return [
    { ...erpFin, isClosed: false, canOpen: true, teams: ['Functional', 'Development'] },
    { ...tribRs, isClosed: false, canOpen: true, teams: ['Functional'] },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-400000000004',
      code: 'WMS-ALFA',
      name: 'Implantação do WMS',
      isClosed: true,
      canOpen: false,
      teams: [],
    },
  ];
}

/** The counts of the views of Marina, the manager of both projects. */
export const demoTicketCounts: TicketCounts = {
  all: 4,
  open: 4,
  unassigned: 4,
  unassignedInMyTeams: 4,
  assignedToMe: 0,
  openedByMe: 1,
  following: 1,
  withVendor: 0,
};

/** A ticket of the list as its page shows it. */
export function demoTicket(row: TicketRow): Ticket {
  return {
    id: row.id,
    number: row.number,
    title: row.title,
    description:
      'A SEFAZ retorna a rejeição 539 na devolução da venda 3381.\nAcontece com todas as devoluções da filial.',
    project: row.project,
    team: row.team,
    priority: row.priority,
    status: row.status,
    requester: row.requester,
    assignee: row.assignee,
    managers: [demoPeople.marina],
    techLeads: row.project.id === erpFin.id ? [demoPeople.rafael] : [],
    openedAt: row.openedAt,
    updatedAt: row.updatedAt,
    descriptionBody: null,
    messages: [],
    events: [],
    attachments: [],
    followers: [row.requester.id],
    mentionable: [
      demoPeople.marina.id,
      demoPeople.rafael.id,
      row.requester.id,
      demoPeople.diego.id,
    ],
    participants: [
      { ...row.requester, email: emailOf(row.requester.name), role: 'Requester' },
      {
        ...demoPeople.marina,
        email: emailOf(demoPeople.marina.name),
        role: row.requester.id === demoPeople.marina.id ? 'Requester' : 'Manager',
      },
      { ...demoPeople.rafael, email: emailOf(demoPeople.rafael.name), role: 'TechLead' },
      {
        ...demoPeople.diego,
        email: emailOf(demoPeople.diego.name),
        role: row.requester.id === demoPeople.diego.id ? 'Requester' : 'Development',
      },
    ],
    permissions: {
      canSend: true,
      canAttachToDescription: false,
      actions: [],
      canChangePriority: false,
      canChangeAssignee: false,
      canChangeRequester: false,
      canTransfer: false,
      canRegisterVendorTicket: false,
    },
    following: row.requester.id === marina.id,
    deadlines: {
      firstResponse: { state: 'Soon', dueOn: '2026-10-20', daysLate: null },
      firstRespondedAt: null,
      resolution: row.resolution ?? { state: 'OnTrack', dueOn: '2026-10-26', daysLate: null },
      usedDays: 0,
      totalDays: 5,
      pausedDays: 0,
    },
    vendorTickets: [],
  };
}

/** The flow of the MVP (ADR-0024), as GET /api/tickets/flow answers it. */
export function demoTicketFlow(): TicketFlow {
  const attendant = ['Attendant'];
  const closers = ['Requester', 'Manager', 'Administrator'];
  return {
    statuses: ['Open', 'Triage', 'InProgress', 'WaitingForRequester', 'Resolved', 'Closed'],
    autoCloseBusinessDays: 5,
    transitions: [
      {
        from: 'Open',
        to: 'Triage',
        action: 'StartTriage',
        actors: attendant,
        requiresMessage: false,
        automatic: false,
      },
      {
        from: 'Open',
        to: 'InProgress',
        action: 'FirstResponse',
        actors: attendant,
        requiresMessage: true,
        automatic: true,
      },
      {
        from: 'Open',
        to: 'Closed',
        action: 'Cancel',
        actors: closers,
        requiresMessage: true,
        automatic: false,
      },
      {
        from: 'Triage',
        to: 'InProgress',
        action: 'StartWork',
        actors: attendant,
        requiresMessage: false,
        automatic: false,
      },
      {
        from: 'Triage',
        to: 'Closed',
        action: 'Cancel',
        actors: closers,
        requiresMessage: true,
        automatic: false,
      },
      {
        from: 'InProgress',
        to: 'WaitingForRequester',
        action: 'AskForInformation',
        actors: attendant,
        requiresMessage: true,
        automatic: false,
      },
      {
        from: 'InProgress',
        to: 'Resolved',
        action: 'Resolve',
        actors: attendant,
        requiresMessage: false,
        automatic: false,
      },
      {
        from: 'WaitingForRequester',
        to: 'InProgress',
        action: 'Reply',
        actors: ['Attendant', 'Requester'],
        requiresMessage: true,
        automatic: true,
      },
      {
        from: 'Resolved',
        to: 'Closed',
        action: 'ConfirmAndClose',
        actors: closers,
        requiresMessage: false,
        automatic: false,
      },
      {
        from: 'Resolved',
        to: 'Closed',
        action: 'AutoClose',
        actors: ['System'],
        requiresMessage: false,
        automatic: true,
      },
      {
        from: 'Resolved',
        to: 'InProgress',
        action: 'Reopen',
        actors: ['Requester'],
        requiresMessage: true,
        automatic: false,
      },
    ],
  };
}

/** A page of tickets with the filters of the address, as the API answers them (only what the tests use). */
export function ticketsPage(tickets: TicketRow[], url: URL) {
  const projectId = url.searchParams.get('projectId');
  const team = url.searchParams.get('team');
  const view = url.searchParams.get('view');
  const search = url.searchParams.get('search')?.toLowerCase();
  const items = tickets.filter(
    (ticket) =>
      (projectId === null || ticket.project.id === projectId) &&
      (team === null || ticket.team === team) &&
      (view !== 'OpenedByMe' || ticket.requester.id === marina.id) &&
      (view !== 'AssignedToMe' || ticket.assignee?.id === marina.id) &&
      (view !== 'Following' || ticket.requester.id === marina.id) &&
      (search === undefined ||
        ticket.title.toLowerCase().includes(search) ||
        `#${String(ticket.number)}` === search),
  );
  return { items, total: items.length };
}

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
  http.get('/api/users', ({ request }) =>
    HttpResponse.json(usersPage(demoUsers, new URL(request.url))),
  ),
  http.get('/api/audit', ({ request }) =>
    HttpResponse.json(auditPage(demoAuditEntries, new URL(request.url))),
  ),
  http.get('/api/session/sign-in-options', () =>
    HttpResponse.json({ localSignIn: true, entraSignIn: false }),
  ),
  http.post('/api/session/sign-out', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/profile', () => HttpResponse.json(profileOf(marina))),
  http.put('/api/profile/theme', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/installation', () => HttpResponse.json(defaultInstallation)),
  http.get('/api/installation/rules', () =>
    HttpResponse.json({
      timeZone: 'America/Sao_Paulo',
      dailySummaryAt: '07:30:00',
      notificationRetentionDays: 90,
      auditRetentionYears: 5,
    }),
  ),
  http.put('/api/profile/daily-summary', () => new HttpResponse(null, { status: 204 })),
  // A bell without notifications.
  http.get('/api/notifications/unread-count', () => HttpResponse.json({ unread: 0 })),
  http.get('/api/notifications', () => HttpResponse.json({ items: [], unread: 0, hasMore: false })),
  // The mailbox of the demonstration, never tested.
  http.get('/api/admin/email', () =>
    HttpResponse.json({ mailbox: 'projetos@empresa.example', lastTest: null }),
  ),
  // No Syncfusion license in the tests (ADR-0087).
  http.get('/api/client-settings', () => HttpResponse.json({ timelineLicense: null })),
  http.get('/api/calendar/:year', ({ params }) =>
    HttpResponse.json(calendarYearOf(demoCalendar(), Number(params.year))),
  ),
  http.get('/api/clients', ({ request }) =>
    HttpResponse.json(clientsPage(demoClients(), new URL(request.url))),
  ),
  http.get('/api/clients/choices', () => HttpResponse.json(clientChoicesOf(demoClients()))),
  // Without projects by default: the screens of projects simulate their own.
  http.get('/api/projects', () => HttpResponse.json({ items: [], total: 0, today: demoToday })),
  http.get('/api/projects/upcoming-milestones', () =>
    HttpResponse.json({ items: [], today: demoToday }),
  ),
  http.get('/api/my-work', () => HttpResponse.json(emptyMyWork)),
  http.get('/api/people', () => HttpResponse.json([])),
  // The card of a person (ADR-0118): the users of the Users screen and the people of the tickets and projects.
  http.get('/api/people/:id', ({ params }) => {
    const id = String(params.id);
    const known =
      demoUsers.find((row) => row.id === id) ??
      Object.values(demoPeople).find((person) => person.id === id);
    if (known === undefined) {
      return problem(404);
    }
    const email = 'email' in known ? known.email : emailOf(known.name);
    return HttpResponse.json({ id, name: known.name, email: email === '' ? null : email });
  }),
  http.get('/api/projects/:id/members', () => HttpResponse.json([])),
  http.get('/api/projects/:id/schedule', () => HttpResponse.json(emptySchedule)),
  http.get('/api/projects/:id/overview', () => HttpResponse.json(emptyOverview)),
  http.get('/api/projects/:id/closing', () =>
    HttpResponse.json({ openTickets: 0, incompleteTasks: 0, pendingAcceptances: 0 }),
  ),
  http.get('/api/project-templates', () =>
    HttpResponse.json(demoTemplates().filter((template) => template.isActive)),
  ),
  http.get('/api/admin/project-templates', () => HttpResponse.json(demoTemplates())),
  http.get('/api/admin/vendors', () => HttpResponse.json(demoVendors())),
  // The documents of a project, as Marina sees them (ADR-0031).
  http.get('/api/admin/document-folders', () => HttpResponse.json(demoDefaultFolders())),
  http.get('/api/projects/:id/documents/folders', () => HttpResponse.json(demoDocumentArea())),
  http.get('/api/projects/:id/documents', ({ request }) =>
    HttpResponse.json(documentsPage(demoDocuments(), new URL(request.url))),
  ),
  http.get('/api/projects/:id/documents/:documentId', ({ params }) => {
    const row = demoDocuments().find((document) => document.id === params.documentId);
    return row === undefined
      ? new HttpResponse(null, { status: 404 })
      : HttpResponse.json(demoDocumentDetails(row));
  }),
  // The links of a project and the categories (ADR-0032).
  http.get('/api/projects/:id/links', ({ request }) =>
    HttpResponse.json(linksList(demoLinks(), new URL(request.url))),
  ),
  http.get('/api/link-categories', () =>
    HttpResponse.json(demoLinkCategories().filter((category) => category.isActive)),
  ),
  http.get('/api/admin/link-categories', () => HttpResponse.json(demoLinkCategories())),
  // The acceptance terms of a project: none, and the person does not manage them (ADR-0045).
  http.get('/api/projects/:id/acceptances', () =>
    HttpResponse.json({
      items: [],
      counts: { all: 0, pending: 0, signed: 0, refused: 0 },
      canManage: false,
      canRequest: false,
    }),
  ),
  // The credentials of a project, without the passwords (ADR-0034).
  http.get('/api/projects/:id/credentials', ({ request }) =>
    HttpResponse.json(credentialsList(demoCredentials(), new URL(request.url))),
  ),
  http.get('/api/tickets', ({ request }) =>
    HttpResponse.json(ticketsPage(demoTickets(), new URL(request.url))),
  ),
  http.get('/api/tickets/counts', () => HttpResponse.json(demoTicketCounts)),
  http.get('/api/tickets/choices', () => HttpResponse.json(demoTicketChoices())),
  http.get('/api/tickets/flow', () => HttpResponse.json(demoTicketFlow())),
  http.get('/api/tickets/vendor-choices', () =>
    HttpResponse.json(demoVendors().filter((vendor) => vendor.isActive)),
  ),
  http.get('/api/tickets/:number', ({ params }) => {
    const row = demoTickets().find((ticket) => String(ticket.number) === params.number);
    return row === undefined
      ? new HttpResponse(null, { status: 404 })
      : HttpResponse.json(demoTicket(row));
  }),
];

export const server = setupServer(...handlers);

export function problem(status: number, type?: string, extensions: Record<string, unknown> = {}) {
  return HttpResponse.json(
    { status, title: 'Problem', ...(type === undefined ? {} : { type }), ...extensions },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  );
}

/** Marina is signed in with the given role in the system. */
export function signedInAs(role: SessionUser['role']): void {
  server.use(http.get('/api/session', () => HttpResponse.json({ ...marina, role })));
}

/** Nobody is signed in: the API answers 401 to "who is in the session". */
export function signedOut(): void {
  server.use(http.get('/api/session', () => problem(401)));
}
