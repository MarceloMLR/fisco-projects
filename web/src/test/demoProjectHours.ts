import type {
  HoursBalance,
  ProjectHours,
  ProjectHoursDetail,
  ProjectTaskHours,
  TaskHours,
} from '../api/client';
import { demoPeople } from './server';

// The hours of the projects as the canvas Horas do projeto shows them (ADR-0128): all fictitious.

const projectId = (n: number) => `0199f5a2-7c1e-7d40-9a1b-f000000000${String(n).padStart(2, '0')}`;

/** The balance as the API computes it (HoursBalance in the TimeTracking module), for the simulated rows. */
export function balanceOf(
  contracted: number | null,
  additional: number,
  logged: number,
  isClosed = false,
): HoursBalance {
  if (contracted === null) {
    return {
      contracted: null,
      additional,
      logged,
      total: null,
      balance: null,
      consumption: null,
      state: 'NoContract',
    };
  }
  const total = contracted + additional;
  const state =
    logged > total ? 'Over' : !isClosed && logged * 100 >= total * 90 ? 'NearLimit' : 'Within';
  return {
    contracted,
    additional,
    logged,
    total,
    balance: total - logged,
    consumption: Math.round((logged * 100) / total),
    state,
  };
}

const row = (
  n: number,
  code: string,
  name: string,
  status: ProjectHours['status'],
  clientName: string | null,
  balance: HoursBalance,
): ProjectHours => ({ id: projectId(n), code, name, status, clientName, balance });

/** The project over its hours, the first of the list. */
export const erpProjectHours = row(
  1,
  'ERP-FIN',
  'Implantação do ERP financeiro',
  'InProgress',
  'Cliente Exemplo Alfa',
  balanceOf(400, 40, 468),
);

/** The open projects, in the order of the API: the most used first, the one without contracted hours last. */
export const openProjectHours: ProjectHours[] = [
  erpProjectHours,
  row(
    2,
    'SPED-27',
    'Adequação do SPED Fiscal 2027',
    'InProgress',
    'Cliente Exemplo Beta',
    balanceOf(160, 0, 146),
  ),
  row(
    3,
    'TMS-DELTA',
    'Implantação do TMS',
    'InProgress',
    'Cliente Exemplo Delta',
    balanceOf(320, 24, 158),
  ),
  row(
    4,
    'WMS-ALFA',
    'Implantação do WMS',
    'Planning',
    'Cliente Exemplo Alfa',
    balanceOf(240, 0, 12),
  ),
  row(
    5,
    'PORTAL-NF',
    'Portal de notas para fornecedores',
    'InProgress',
    null,
    balanceOf(null, 0, 36),
  ),
];

/** A closed project near its hours, which stays within: it takes no more hours. */
export const closedProjectHours: ProjectHours[] = [
  row(
    6,
    'TRIB-RS',
    'Revisão tributária da filial RS',
    'Completed',
    'Cliente Exemplo Beta',
    balanceOf(160, 0, 150, true),
  ),
];

/** The detail of the first project, as the canvas draws it. */
export function erpHoursDetail(project: ProjectHours = erpProjectHours) {
  return {
    project,
    firstDay: '2026-08-03',
    additional: [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-f00000000101',
        day: '2026-09-30',
        hours: 16,
        reason: 'Integração com um segundo banco, pedida pelo cliente',
        createdBy: demoPeople.paulo,
      },
      {
        id: '0199f5a2-7c1e-7d40-9a1b-f00000000102',
        day: '2026-08-12',
        hours: 24,
        reason: 'Relatório de comissões, fora do escopo inicial',
        createdBy: demoPeople.paulo,
      },
    ],
    people: [
      { person: demoPeople.carla, role: 'Contributor', hours: 182, share: 39 },
      { person: demoPeople.diego, role: 'Developer', hours: 150, share: 32 },
      { person: demoPeople.marina, role: 'Manager', hours: 96, share: 21 },
      { person: demoPeople.rafael, role: null, hours: 40, share: 9 },
    ],
  } satisfies ProjectHoursDetail;
}

/** A task with its planned and logged hours, as the API answers them (TaskHours in the TimeTracking module, ADR-0138). */
export function taskHoursRow(
  itemId: string,
  name: string,
  planned: number | null,
  logged: number,
  isDone = false,
): TaskHours {
  return { itemId, name, planned, logged, balance: balanceOf(planned, 0, logged, isDone) };
}

/** The hours of the tasks of a project, with the totals the API computes and the hours logged out of the tasks. */
export function taskHoursOf(items: TaskHours[], outside = 0): ProjectTaskHours {
  return {
    planned: items.reduce((total, item) => total + (item.planned ?? 0), 0),
    logged: items.reduce((total, item) => total + item.logged, 0),
    outside,
    items,
  };
}
