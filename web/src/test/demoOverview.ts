import type { OverviewRow, ProjectOverview } from '../api/client';

// The overview of the ERP-FIN on Friday, 16/10/2026, as the canvas ProjetoVisaoGeral shows it (ADR-0073).

const marina = { id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70', name: 'Marina Costa' };
const beatriz = { id: '0199f5a2-7c1e-7d40-9a1b-000000000003', name: 'Beatriz Lima' };
const rafael = { id: '0199f5a2-7c1e-7d40-9a1b-000000000004', name: 'Rafael Souza' };
const carla = { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' };
const diego = { id: '0199f5a2-7c1e-7d40-9a1b-000000000006', name: 'Diego Torres' };
const lucas = { id: '0199f5a2-7c1e-7d40-9a1b-000000000007', name: 'Lucas Prado' };

function row(
  index: number,
  value: Partial<OverviewRow> & Pick<OverviewRow, 'name' | 'start' | 'end'>,
): OverviewRow {
  return {
    kind: 'Phase',
    id: `0199f5a2-7c1e-7d40-9a1b-0000000f0${String(index).padStart(3, '0')}`,
    progress: 0,
    late: false,
    critical: false,
    reached: false,
    baselineStart: null,
    baselineEnd: null,
    ...value,
  };
}

/** The weekends between two days, besides the holidays of the calendar of the demonstration. */
function nonWorkingDays(from: string, to: string, holidays: string[]): string[] {
  const days: string[] = [];
  for (let day = new Date(`${from}T00:00:00Z`); day <= new Date(`${to}T00:00:00Z`);) {
    const text = day.toISOString().slice(0, 10);
    if (day.getUTCDay() === 0 || day.getUTCDay() === 6 || holidays.includes(text)) {
      days.push(text);
    }
    day = new Date(day.getTime() + 86_400_000);
  }
  return days;
}

export const demoOverviewTaskId = '0199f5a2-7c1e-7d40-9a1b-0000000f1001';
export const demoOverviewTermId = '0199f5a2-7c1e-7d40-9a1b-0000000f2001';

export function demoOverview(): ProjectOverview {
  return {
    today: '2026-10-16',
    progress: { actual: 62, planned: 70, byBaseline: true },
    deadline: { end: '2026-11-13', baselineEnd: '2026-11-11', deviation: 2 },
    tickets: { open: 7, total: 9, overdue: 1, waitingForRequester: 2 },
    nextMilestone: {
      id: '0199f5a2-7c1e-7d40-9a1b-0000000f0004',
      name: 'Aceite da homologação',
      day: '2026-11-03',
      businessDaysLeft: 11,
      daysLate: null,
    },
    timeline: {
      start: '2026-10-05',
      end: '2026-11-13',
      nonWorkingDays: nonWorkingDays('2026-10-05', '2026-11-13', ['2026-10-12', '2026-11-02']),
      rows: [
        row(1, {
          name: 'Levantamento',
          start: '2026-10-05',
          end: '2026-10-15',
          progress: 96,
          late: true,
        }),
        row(2, {
          name: 'Configuração',
          start: '2026-10-13',
          end: '2026-10-29',
          progress: 38,
          critical: true,
          baselineStart: '2026-10-13',
          baselineEnd: '2026-10-27',
        }),
        row(3, { name: 'Homologação', start: '2026-10-30', end: '2026-11-03', critical: true }),
        row(4, {
          kind: 'Milestone',
          name: 'Aceite da homologação',
          start: '2026-11-03',
          end: '2026-11-03',
          critical: true,
        }),
        row(5, { name: 'Entrada em produção', start: '2026-11-04', end: '2026-11-13' }),
        row(6, {
          kind: 'Milestone',
          name: 'Virada para produção',
          start: '2026-11-13',
          end: '2026-11-13',
        }),
      ],
    },
    attention: {
      tasks: [
        {
          id: demoOverviewTaskId,
          name: 'Validar cadastro de produtos',
          owner: diego,
          progress: 80,
          end: '2026-10-15',
          daysLate: 1,
          blocks: ['Configurar plano de contas'],
        },
      ],
      tickets: [
        {
          number: 104,
          title: 'Rejeição 539 ao emitir nota de devolução',
          team: 'Development',
          assignee: diego,
          interactions: 50,
          deadline: 'Resolution',
          dueState: 'Soon',
          dueOn: '2026-10-19',
          daysLate: null,
        },
        {
          number: 101,
          title: 'Erro ao importar XML de entrada',
          team: 'Development',
          assignee: lucas,
          interactions: 14,
          deadline: 'Resolution',
          dueState: 'Soon',
          dueOn: '2026-10-19',
          daysLate: null,
        },
        {
          number: 110,
          title: 'Relatório de ICMS-ST sem as notas de devolução',
          team: 'Development',
          assignee: null,
          interactions: 1,
          deadline: 'FirstResponse',
          dueState: 'Soon',
          dueOn: '2026-10-19',
          daysLate: null,
        },
      ],
      acceptances: [
        {
          id: demoOverviewTermId,
          title: 'Aceite da homologação',
          recipient: carla,
          requestedAt: '2026-10-15T13:00:00Z',
          milestone: 'Aceite da homologação',
          milestoneDay: '2026-11-03',
        },
      ],
      vendors: [{ ticketNumber: 104, vendor: 'Emissor Exemplo', code: 'FX-20931', staleDays: 6 }],
    },
    teams: [
      {
        team: 'Management',
        people: [
          { ...marina, role: 'Manager' },
          { ...rafael, role: 'TechLead' },
        ],
        openTickets: null,
      },
      {
        team: 'Functional',
        people: [
          { ...beatriz, role: 'Contributor' },
          { ...carla, role: 'Contributor' },
        ],
        openTickets: 3,
      },
      {
        team: 'Development',
        people: [
          { ...rafael, role: 'TechLead' },
          { ...diego, role: 'Developer' },
          { ...lucas, role: 'Developer' },
        ],
        openTickets: 4,
      },
    ],
  };
}

/** The overview of a project without a schedule, tickets nor people: the default of the screen tests. */
export const emptyOverview: ProjectOverview = {
  today: '2026-10-16',
  progress: null,
  deadline: null,
  tickets: { open: 0, total: 0, overdue: 0, waitingForRequester: 0 },
  nextMilestone: null,
  timeline: null,
  attention: { tasks: [], tickets: [], acceptances: [], vendors: [] },
  teams: [
    { team: 'Management', people: [], openTickets: null },
    { team: 'Functional', people: [], openTickets: 0 },
    { team: 'Development', people: [], openTickets: 0 },
  ],
};
