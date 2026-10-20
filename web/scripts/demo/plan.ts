import type { components } from '../../src/api/schema';

/**
 * The fictitious demonstration data (ADR-0002, ADR-0089): the people of the demo directory of the local sign-in
 * (ADR-0078), the clients, the projects with their people, the schedules and the tickets. The days of the schedules count from the
 * first day of the main project, the Monday two weeks before the week the data is loaded; the server moves each start to
 * a business day (ADR-0018).
 */

type ProjectRole = components['schemas']['ProjectRole'];
type ProjectStatus = components['schemas']['ProjectStatus'];
type TicketTeam = components['schemas']['TicketTeam'];
type TicketPriority = components['schemas']['TicketPriority'];

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

/** A ticket opened by a person of the project, in the order of the plan (ADR-0029, ADR-0068). */
export interface DemoTicket {
  title: string;
  team: TicketTeam;
  priority: TicketPriority;
  /** Who opens it: anyone in the project but a viewer, or the Administrator (ADR-0044). */
  by: DemoPerson;
  description: string;
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
  /** The manager saves the schedule as a model of project, with this name and description (ADR-0017). */
  template?: { name: string; description: string };
  phases: readonly DemoPhase[];
  /** The tickets opened in the project once it is built, before it gets its status. */
  tickets?: readonly DemoTicket[];
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
    template: {
      name: 'Implantação de ERP',
      description:
        'Levantamento, configuração, homologação e entrada em produção de um ERP financeiro.',
    },
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
    tickets: [
      {
        title: 'Rejeição 539 ao emitir nota de devolução',
        team: 'Development',
        priority: 'High',
        by: 'carla',
        description:
          'Ao emitir a nota de devolução da venda 3381, a SEFAZ retorna a rejeição 539.\n\nAcontece com todas as devoluções da filial desde ontem. Esperava a autorização normal, como no mês passado.',
      },
      {
        title: 'Dúvida sobre o CFOP de transferência entre filiais',
        team: 'Functional',
        priority: 'Low',
        by: 'diego',
        description:
          'Qual CFOP usar na transferência de mercadoria entre a matriz e a filial de outro estado?',
      },
      {
        title: 'Relatório de ICMS-ST sem as notas de devolução',
        team: 'Development',
        priority: 'Medium',
        by: 'carla',
        description:
          'O relatório de ICMS-ST de outubro não traz as notas de devolução, e o total fica diferente da apuração.',
      },
      {
        title: 'Liberar ambiente de homologação ao cliente',
        team: 'Development',
        priority: 'High',
        by: 'marina',
        description:
          'O cliente precisa de acesso ao ambiente de homologação para os testes da próxima semana.',
      },
      {
        title: 'Importar a tabela de NCM atualizada',
        team: 'Functional',
        priority: 'Medium',
        by: 'rafael',
        description:
          'A tabela de NCM mudou neste mês. Precisamos importar a versão nova antes dos testes.',
      },
      {
        title: 'Erro ao importar XML de entrada',
        team: 'Development',
        priority: 'Critical',
        by: 'carla',
        description:
          'Os XML de entrada de um fornecedor são recusados na importação, com a mensagem "Esquema inválido".',
      },
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
    template: {
      name: 'Revisão tributária de filial',
      description: 'Diagnóstico, apuração, revisão e entrega da revisão tributária de uma filial.',
    },
    name: 'Revisão tributária da filial RS',
    client: 'Beta Alimentos',
    members: [['carla', 'Contributor']],
    tickets: [
      {
        title: 'Dúvida sobre o fechamento do SPED',
        team: 'Functional',
        priority: 'Medium',
        by: 'paulo',
        description: 'O fechamento do SPED da filial RS deve sair com os créditos já revisados?',
      },
      {
        title: 'Ajustar alíquota de ICMS-ST para RS',
        team: 'Functional',
        priority: 'Critical',
        by: 'marina',
        description:
          'A alíquota de ICMS-ST de autopeças mudou no RS. Precisamos ajustar o cadastro.',
      },
    ],
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
