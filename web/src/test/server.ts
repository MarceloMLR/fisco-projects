import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { components } from '../api/schema';
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
  ProjectScheduleSummary,
  ProjectStatus,
  Schedule,
  ScheduleItem,
  SessionUser,
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

/** A user to choose. */
const user = (person: { id: string; name: string }): Person => ({
  ...person,
  directoryId: null,
  email: emailOf(person.name),
  canBeManager: managers.has(person.id),
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
  },
];

/** The people to choose as the API answers them: by name without accents, only the managers when asked. */
export function peopleOf(url: URL): Person[] {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const onlyManagers = url.searchParams.get('managers') === 'true';
  const limit = Number(url.searchParams.get('limit') ?? '20');
  return demoPeopleChoices
    .filter((person) => !onlyManagers || person.canBeManager)
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
    },
    scheduleStart: null,
    scheduleEnd: null,
    mine,
    schedule: null,
  };
}

/** A project with the numbers of its schedule, and its dates in the header. */
function withSchedule(project: DemoProject, schedule: ProjectScheduleSummary): DemoProject {
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
};

/** My profile of a person in the session, who signs in locally and last did so at 09:12 of October 19th. */
export function profileOf(person: SessionUser): Profile {
  return { ...person, lastSignInAt: '2026-10-19T12:12:00Z', signInMethods: ['local'] };
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

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
  http.get('/api/users', ({ request }) =>
    HttpResponse.json(usersPage(demoUsers, new URL(request.url))),
  ),
  http.get('/api/audit', ({ request }) =>
    HttpResponse.json(auditPage(demoAuditEntries, new URL(request.url))),
  ),
  http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn: true })),
  http.post('/api/session/sign-out', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/profile', () => HttpResponse.json(profileOf(marina))),
  http.put('/api/profile/theme', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/installation', () => HttpResponse.json(defaultInstallation)),
  http.get('/api/installation/rules', () => HttpResponse.json({ timeZone: 'America/Sao_Paulo' })),
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
  http.get('/api/people', () => HttpResponse.json([])),
  http.get('/api/projects/:id/members', () => HttpResponse.json([])),
  http.get('/api/projects/:id/schedule', () => HttpResponse.json(emptySchedule)),
];

export const server = setupServer(...handlers);

export function problem(status: number, type?: string) {
  return HttpResponse.json(
    { status, title: 'Problem', ...(type === undefined ? {} : { type }) },
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
