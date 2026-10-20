import type { components } from '../../src/api/schema';

/**
 * The fictitious demonstration data (ADR-0002, ADR-0089): the people of the demo directory of the local sign-in
 * (ADR-0078), the clients, the projects with their people and the schedules. The days of the schedules count from the
 * first day of the main project, the Monday two weeks before the week the data is loaded; the server moves each start to
 * a business day (ADR-0018).
 */

type ProjectRole = components['schemas']['ProjectRole'];
type ProjectStatus = components['schemas']['ProjectStatus'];

/** The people who sign in; Lucas Prado and Sofia Martins stay out, as people who never signed in (ADR-0078). */
export const demoPeople = {
  paulo: 'paulo.nogueira@empresa.example',
  marina: 'marina.costa@empresa.example',
  rafael: 'rafael.souza@empresa.example',
  beatriz: 'beatriz.lima@empresa.example',
  carla: 'carla.mendes@empresa.example',
  diego: 'diego.torres@empresa.example',
} as const;

export type DemoPerson = keyof typeof demoPeople;

/** The first Administrator (`Users:FirstAdministrator` of the development settings). */
export const administrator: DemoPerson = 'paulo';

/** The manager of the projects, who builds their schedules. */
export const manager: DemoPerson = 'marina';

/** The people who get the role Project manager in the system (ADR-0040, ADR-0085). */
export const projectManagers: readonly DemoPerson[] = ['marina', 'beatriz'];

export const demoClients = ['Alfa Distribuidora', 'Beta Alimentos', 'Gama Têxtil'] as const;

export type DemoClient = (typeof demoClients)[number];

export interface DemoItem {
  kind: 'Task' | 'Milestone';
  name: string;
  /** Days from the first day of the main project. */
  day: number;
  /** Business days of a task; zero for a milestone. */
  duration: number;
  owner?: DemoPerson;
  progress?: number;
  /** The names of the items it depends on, of the same project. */
  after?: readonly string[];
  reached?: boolean;
  /** The name of the task it is inside, of the same phase and before it in the plan (ADR-0088). */
  inside?: string;
  /**
   * Days from the first day of the main project where the manager puts its start after saving the baseline, which
   * pushes the items after it (ADR-0021).
   */
  movedTo?: number;
}

export interface DemoPhase {
  name: string;
  items: readonly DemoItem[];
}

export interface DemoProject {
  code: string;
  name: string;
  client: DemoClient | null;
  techLead?: DemoPerson;
  members?: readonly (readonly [DemoPerson, ProjectRole])[];
  /** The status it ends in; a new project starts in Planning. */
  status?: ProjectStatus;
  /** The manager saves the baseline once the schedule is built, before moving the items with movedTo (ADR-0021). */
  baseline?: boolean;
  phases: readonly DemoPhase[];
}

/** The project whose presence says the data was already loaded. */
export const mainProject = 'ERP-FIN';

const task = (
  name: string,
  day: number,
  duration: number,
  more: Partial<DemoItem> = {},
): DemoItem => ({
  kind: 'Task',
  name,
  day,
  duration,
  ...more,
});

const milestone = (name: string, day: number, more: Partial<DemoItem> = {}): DemoItem => ({
  kind: 'Milestone',
  name,
  day,
  duration: 0,
  ...more,
});

export const demoProjects: readonly DemoProject[] = [
  {
    code: mainProject,
    name: 'Implantação do ERP financeiro',
    client: 'Alfa Distribuidora',
    techLead: 'rafael',
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
      ['beatriz', 'Viewer'],
    ],
    status: 'InProgress',
    baseline: true,
    phases: [
      {
        name: 'Levantamento',
        items: [
          task('Reunião de abertura', 0, 1, { owner: 'marina', progress: 100 }),
          task('Mapear processos fiscais', 1, 4, {
            owner: 'carla',
            progress: 100,
            after: ['Reunião de abertura'],
          }),
          milestone('Processos mapeados', 4, {
            after: ['Mapear processos fiscais'],
            reached: true,
          }),
          // Late on the Monday it is loaded: it ended on the Friday before, at 80%.
          task('Validar cadastro de produtos', 8, 4, {
            owner: 'diego',
            progress: 80,
            after: ['Mapear processos fiscais'],
          }),
          milestone('Cadastro validado', 11, { after: ['Validar cadastro de produtos'] }),
        ],
      },
      {
        name: 'Configuração',
        items: [
          // Two days later than planned, after the baseline: everything after it and the project end later.
          task('Parametrizar regras de ICMS', 8, 8, {
            owner: 'rafael',
            progress: 50,
            after: ['Mapear processos fiscais'],
            movedTo: 10,
          }),
          task('Configurar plano de contas', 9, 6, { owner: 'carla', progress: 70 }),
          task('Integrar emissão de NF-e', 14, 8, { owner: 'diego', progress: 30 }),
          task('Testes integrados', 18, 5, {
            owner: 'rafael',
            after: ['Parametrizar regras de ICMS'],
          }),
          // Broken into subtasks (ADR-0088): the first takes the dependency of the task.
          task('Roteiro de testes', 18, 2, { owner: 'carla', inside: 'Testes integrados' }),
          milestone('Roteiro aprovado', 20, {
            inside: 'Testes integrados',
            after: ['Roteiro de testes'],
          }),
          task('Executar testes', 20, 3, {
            owner: 'rafael',
            inside: 'Testes integrados',
            after: ['Roteiro aprovado'],
          }),
        ],
      },
      {
        name: 'Homologação',
        items: [
          task('Testes com a equipe do cliente', 25, 2, {
            owner: 'carla',
            after: ['Executar testes'],
          }),
          milestone('Aceite da homologação', 29, { after: ['Testes com a equipe do cliente'] }),
        ],
      },
      {
        name: 'Entrada em produção',
        items: [
          task('Migrar saldos', 30, 2, { owner: 'diego', after: ['Aceite da homologação'] }),
          task('Treinar usuários', 31, 4, { owner: 'marina' }),
          task('Acompanhamento assistido', 35, 5, { owner: 'rafael', after: ['Migrar saldos'] }),
          milestone('Virada para produção', 39, { after: ['Acompanhamento assistido'] }),
        ],
      },
      { name: 'Encerramento', items: [] },
    ],
  },
  {
    code: 'SPED-27',
    name: 'Adequação do SPED Fiscal 2027',
    client: 'Gama Têxtil',
    status: 'InProgress',
    phases: [
      { name: 'Análise', items: [task('Levantar mudanças do leiaute', -14, 8, { progress: 100 })] },
      {
        name: 'Ajustes',
        items: [
          task('Ajustar registros do bloco K', -4, 18, {
            progress: 90,
            after: ['Levantar mudanças do leiaute'],
          }),
        ],
      },
      {
        name: 'Validação',
        items: [
          task('Validar no PVA', 22, 6, { after: ['Ajustar registros do bloco K'] }),
          milestone('Arquivo validado', 29, { after: ['Validar no PVA'] }),
        ],
      },
    ],
  },
  {
    code: 'TRIB-RS',
    name: 'Revisão tributária da filial RS',
    client: 'Beta Alimentos',
    phases: [
      { name: 'Diagnóstico', items: [task('Levantar créditos de ICMS', 21, 15)] },
      {
        name: 'Apuração',
        items: [task('Apurar diferenças', 42, 20, { after: ['Levantar créditos de ICMS'] })],
      },
      {
        name: 'Entrega',
        items: [milestone('Relatório entregue', 74, { after: ['Apurar diferenças'] })],
      },
    ],
  },
  { code: 'PORTAL-NF', name: 'Portal de notas para fornecedores', client: null, phases: [] },
  {
    code: 'WMS-ALFA',
    name: 'Implantação do WMS',
    client: 'Alfa Distribuidora',
    status: 'Completed',
    phases: [
      { name: 'Levantamento', items: [task('Mapear armazéns', -126, 15, { progress: 100 })] },
      {
        name: 'Configuração',
        items: [
          task('Configurar endereçamento', -105, 30, { progress: 100, after: ['Mapear armazéns'] }),
        ],
      },
      {
        name: 'Entrada em produção',
        items: [
          milestone('Operação no WMS', -38, { after: ['Configurar endereçamento'], reached: true }),
        ],
      },
    ],
  },
];

const dayInMilliseconds = 24 * 60 * 60 * 1000;

function utcOf(day: string): number {
  const [year, month, date] = day.split('-').map(Number);
  return Date.UTC(year ?? 0, (month ?? 1) - 1, date ?? 1);
}

function dayText(time: number): string {
  return new Date(time).toISOString().slice(0, 10);
}

/** The first day of the main project: the Monday two weeks before the week of the day. */
export function firstDayFor(today: string): string {
  const time = utcOf(today);
  const sinceMonday = (new Date(time).getUTCDay() + 6) % 7;
  return dayText(time - (sinceMonday + 14) * dayInMilliseconds);
}

/** The day some days after (or before) the first day. */
export function dayFrom(first: string, days: number): string {
  return dayText(utcOf(first) + days * dayInMilliseconds);
}
