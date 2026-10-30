import type { TimeEntry, TimeEntryList } from '../api/client';

// Hours of Carla Mendes in the week of 26/10/2026, as the canvas Horas do projeto shows them (ADR-0128): all fictitious.

/** The moment of the tests of Minhas horas: Thursday, 29/10/2026, at noon in America/Sao_Paulo. */
export const hoursNow = new Date('2026-10-29T15:00:00Z');

export const hoursProjects = [
  {
    id: '0199f5a2-7c1e-7d40-9a1b-e00000000001',
    code: 'ERP-FIN',
    name: 'Implantação do ERP financeiro',
  },
  {
    id: '0199f5a2-7c1e-7d40-9a1b-e00000000002',
    code: 'SPED-27',
    name: 'Adequação do SPED Fiscal 2027',
  },
] as const;

const [erp, sped] = hoursProjects;

/** Carla Mendes, who logged the hours of the week. */
export const hoursPerson = { id: '0199f5a2-7c1e-7d40-9a1b-e00000000031', name: 'Carla Mendes' };

export const hoursWork = {
  tasks: [
    { id: '0199f5a2-7c1e-7d40-9a1b-e00000000011', name: 'Configurar plano de contas' },
    { id: '0199f5a2-7c1e-7d40-9a1b-e00000000012', name: 'Roteiro de testes' },
  ],
  tickets: [
    {
      id: '0199f5a2-7c1e-7d40-9a1b-e00000000021',
      number: 104,
      title: 'Rejeição 539 ao emitir nota de devolução',
      isClosed: false,
    },
    {
      id: '0199f5a2-7c1e-7d40-9a1b-e00000000022',
      number: 101,
      title: 'Erro ao importar XML de entrada',
      isClosed: true,
    },
  ],
};

function entry(
  id: string,
  day: string,
  hours: number,
  description: string,
  project: { id: string; code: string; name: string },
  work: Pick<TimeEntry, 'task' | 'ticket'> = { task: null, ticket: null },
): TimeEntry {
  return {
    id,
    day,
    hours,
    description,
    person: { ...hoursPerson },
    project: { ...project },
    canChange: true,
    ...work,
  };
}

/** The entries of the week, the newest day first, as the API sends them. */
export function demoTimeEntries(): TimeEntry[] {
  return [
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000101',
      '2026-10-29',
      3.5,
      'Roteiro dos testes de emissão de NF-e',
      erp,
      {
        task: { id: '0199f5a2-7c1e-7d40-9a1b-e00000000012', name: 'Roteiro de testes' },
        ticket: null,
      },
    ),
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000102',
      '2026-10-28',
      5,
      'Comparação dos leiautes de 2026 e 2027',
      sped,
      {
        task: { id: '0199f5a2-7c1e-7d40-9a1b-e00000000013', name: null },
        ticket: null,
      },
    ),
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000103',
      '2026-10-28',
      3,
      'Treinamento dos usuários-chave',
      erp,
    ),
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000104',
      '2026-10-27',
      6,
      'Ajustes depois da revisão da contabilidade',
      erp,
      {
        task: { id: '0199f5a2-7c1e-7d40-9a1b-e00000000011', name: 'Configurar plano de contas' },
        ticket: null,
      },
    ),
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000105',
      '2026-10-27',
      1.5,
      'Teste da correção em homologação',
      erp,
      {
        task: null,
        ticket: {
          id: '0199f5a2-7c1e-7d40-9a1b-e00000000022',
          number: 101,
          title: 'Erro ao importar XML de entrada',
        },
      },
    ),
    entry(
      '0199f5a2-7c1e-7d40-9a1b-e00000000106',
      '2026-10-26',
      8,
      'Cadastro das contas de resultado',
      erp,
      {
        task: { id: '0199f5a2-7c1e-7d40-9a1b-e00000000011', name: 'Configurar plano de contas' },
        ticket: null,
      },
    ),
  ];
}

/** The list of a period, as the API answers it. */
export function timeEntryListOf(entries: readonly TimeEntry[], url: URL): TimeEntryList {
  const from = url.searchParams.get('from') ?? '';
  const to = url.searchParams.get('to') ?? '';
  const items = entries.filter((item) => item.day >= from && item.day <= to);
  return { items, total: items.reduce((sum, item) => sum + item.hours, 0) };
}
