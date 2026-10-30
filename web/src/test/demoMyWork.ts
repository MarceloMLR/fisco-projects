import type { MyWork, MyWorkItem, UpcomingMilestone } from '../api/client';

// Meu trabalho of Marina on Friday, 16/10/2026, as the canvas Main shows it (ADR-0070, ADR-0096).

const marinaId = '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70';
const diego = { id: '0199f5a2-7c1e-7d40-9a1b-0000000000d1', name: 'Diego Torres' };
const beatriz = { id: '0199f5a2-7c1e-7d40-9a1b-000000000003', name: 'Beatriz Lima' };
const carla = { id: '0199f5a2-7c1e-7d40-9a1b-0000000000c1', name: 'Carla Mendes' };
const marina = { id: marinaId, name: 'Marina Costa' };

const erp = { id: '0199f5a2-7c1e-7d40-9a1b-0000000000e1', code: 'ERP-FIN' };
const fisc = { id: '0199f5a2-7c1e-7d40-9a1b-0000000000e2', code: 'FISC-SP' };
const trib = { id: '0199f5a2-7c1e-7d40-9a1b-0000000000e3', code: 'TRIB-RS' };

function item(
  index: number,
  value: Partial<MyWorkItem> & Pick<MyWorkItem, 'kind' | 'title' | 'action'>,
): MyWorkItem {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-00000000a${String(index).padStart(3, '0')}`,
    project: erp,
    dueOn: null,
    daysLate: null,
    waitingSince: null,
    ticket: null,
    task: null,
    acceptance: null,
    ...value,
  };
}

export const demoMyWorkItems = {
  lateTask: item(1, {
    kind: 'Task',
    title: 'Validar cadastro de produtos',
    action: 'Complete',
    dueOn: '2026-10-15',
    daysLate: 1,
    task: { phase: 'Levantamento', progress: 80 },
  }),
  lateTicket: item(2, {
    kind: 'Ticket',
    title: 'Ajustar layout do DANFE simplificado',
    action: 'Open',
    project: fisc,
    dueOn: '2026-10-15',
    daysLate: 1,
    ticket: {
      number: 98,
      status: 'InProgress',
      team: 'Functional',
      requester: beatriz,
      assignee: marina,
      interactions: 6,
      deadline: 'Resolution',
    },
  }),
  waitingTicket: item(3, {
    kind: 'Ticket',
    title: 'Liberar ambiente de homologação ao cliente',
    action: 'Reply',
    waitingSince: '2026-10-14T13:00:00Z',
    ticket: {
      number: 107,
      status: 'WaitingForRequester',
      team: 'Development',
      requester: marina,
      assignee: diego,
      interactions: 4,
      deadline: null,
    },
  }),
  unassignedTicket: item(5, {
    kind: 'Ticket',
    title: 'Relatório de ICMS-ST sem as notas de devolução',
    action: 'Assign',
    dueOn: '2026-10-19',
    ticket: {
      number: 110,
      status: 'Open',
      team: 'Development',
      requester: carla,
      assignee: null,
      interactions: 1,
      deadline: 'FirstResponse',
    },
  }),
  todayTask: item(6, {
    kind: 'Task',
    title: 'Revisar cadastro de CFOP',
    action: 'Start',
    project: fisc,
    dueOn: '2026-10-16',
    task: { phase: 'Diagnóstico', progress: 0 },
  }),
  weekTask: item(7, {
    kind: 'Task',
    title: 'Configurar plano de contas',
    action: 'Update',
    dueOn: '2026-10-21',
    task: { phase: 'Configuração', progress: 70 },
  }),
  askedAcceptance: item(8, {
    kind: 'Acceptance',
    title: 'Aceite da homologação',
    action: 'Remind',
    dueOn: '2026-10-22',
    acceptance: {
      recipient: null,
      signers: ['Ana Souza', 'Bruno Lima'],
      requestedBy: marina,
      requestedAt: '2026-10-15T13:00:00Z',
      milestone: 'Homologação',
    },
  }),
  weekTicket: item(9, {
    kind: 'Ticket',
    title: 'Dúvida sobre o fechamento do SPED',
    action: 'Open',
    project: trib,
    dueOn: '2026-10-22',
    ticket: {
      number: 109,
      status: 'Triage',
      team: 'Functional',
      requester: carla,
      assignee: marina,
      interactions: 3,
      deadline: 'Resolution',
    },
  }),
  followedTicket: item(10, {
    kind: 'Ticket',
    title: 'Rejeição 539 ao emitir nota de devolução',
    action: 'Open',
    dueOn: '2026-10-19',
    ticket: {
      number: 104,
      status: 'InProgress',
      team: 'Development',
      requester: carla,
      assignee: diego,
      interactions: 50,
      deadline: 'Resolution',
    },
  }),
};

/** The queue of Marina on Friday, 16/10/2026: the next business day is Monday. */
export function demoMyWork(): MyWork {
  const items = demoMyWorkItems;
  return {
    today: '2026-10-16',
    nextBusinessDay: '2026-10-19',
    views: [
      {
        view: 'ForMe',
        count: 8,
        groups: [
          { group: 'Late', items: [items.lateTask, items.lateTicket] },
          { group: 'Waiting', items: [items.waitingTicket] },
          { group: 'Soon', items: [items.unassignedTicket, items.todayTask] },
          { group: 'Week', items: [items.weekTask, items.askedAcceptance, items.weekTicket] },
        ],
      },
      {
        view: 'OpenedByMe',
        count: 2,
        groups: [
          { group: 'Waiting', items: [items.waitingTicket] },
          { group: 'Week', items: [items.askedAcceptance] },
        ],
      },
      { view: 'Following', count: 1, groups: [{ group: 'Soon', items: [items.followedTicket] }] },
    ],
  };
}

/** Meu trabalho with nothing in it. */
export const emptyMyWork: MyWork = {
  today: '2026-10-16',
  nextBusinessDay: '2026-10-19',
  views: [
    { view: 'ForMe', count: 0, groups: [] },
    { view: 'OpenedByMe', count: 0, groups: [] },
    { view: 'Following', count: 0, groups: [] },
  ],
};

/** The next milestones of the projects of Marina on Friday, 16/10/2026. */
export function demoUpcomingMilestones(): UpcomingMilestone[] {
  const project = (code: string, name: string, id: string) => ({ id, code, name });
  return [
    {
      id: '0199f5a2-7c1e-7d40-9a1b-00000000b001',
      name: 'Entrega do diagnóstico',
      day: '2026-10-23',
      businessDaysLeft: 5,
      acceptanceRequested: false,
      project: project('FISC-SP', 'Revisão fiscal da filial SP', fisc.id),
    },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-00000000b002',
      name: 'Homologação',
      day: '2026-11-03',
      businessDaysLeft: 11,
      acceptanceRequested: true,
      project: project('ERP-FIN', 'Implantação do ERP financeiro', erp.id),
    },
  ];
}
