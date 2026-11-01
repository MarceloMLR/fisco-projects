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
type TicketAction = components['schemas']['TicketAction'];
type VendorTicketStatus = components['schemas']['VendorTicketStatus'];
type DocumentStatus = components['schemas']['DocumentStatus'];

/**
 * The people who sign in; Lucas Prado and Sofia Martins stay out, as people who never signed in (ADR-0078). Sofia
 * Martins signs in once, as the colleague who left the company (`formerColleague`).
 */
export const demoPeople = {
  paulo: 'paulo.nogueira@empresa.example',
  marina: 'marina.costa@empresa.example',
  rafael: 'rafael.souza@empresa.example',
  beatriz: 'beatriz.lima@empresa.example',
  carla: 'carla.mendes@empresa.example',
  diego: 'diego.torres@empresa.example',
} as const;

/**
 * The signers of the terms, people of the clients outside the company and the system, with fictitious e-mails
 * (ADR-0002, ADR-0129).
 */
export const clientSigners = {
  ana: { name: 'Ana Souza', email: 'ana.souza@cliente.example' },
  bruno: { name: 'Bruno Lima', email: 'bruno.lima@cliente.example' },
  otavio: { name: 'Otávio Ramos', email: 'otavio.ramos@cliente.example' },
  paula: { name: 'Paula Freitas', email: 'paula.freitas@cliente.example' },
} satisfies Record<string, DemoSigner>;

export type DemoPerson = keyof typeof demoPeople;

/** The names of the people, as the demo directory has them, for the mentions. */
export const demoNames: Record<DemoPerson, string> = {
  paulo: 'Paulo Nogueira',
  marina: 'Marina Costa',
  rafael: 'Rafael Souza',
  beatriz: 'Beatriz Lima',
  carla: 'Carla Mendes',
  diego: 'Diego Torres',
};

/** The first Administrator (`Users:FirstAdministrator` of the development settings). */
export const administrator: DemoPerson = 'paulo';

/** The manager of the projects without one of their own, who builds their schedules. */
export const manager: DemoPerson = 'marina';

/** The people who get the role Project manager in the system (ADR-0040, ADR-0085). */
export const projectManagers: readonly DemoPerson[] = ['marina', 'beatriz'];

/** The people who get the role Tech lead in the system, to lead projects technically (ADR-0097). */
export const techLeads: readonly DemoPerson[] = ['rafael'];

export const demoClients = [
  'Alfa Distribuidora',
  'Beta Alimentos',
  'Gama Têxtil',
  'Delta Logística',
  'Épsilon Farma',
  'Zeta Agronegócio',
] as const;

export type DemoClient = (typeof demoClients)[number];

/** The vendors the team opens tickets with, fictitious, included by the Administrator (ADR-0002, ADR-0028). */
export const demoVendors = [
  { name: 'Emissor Exemplo', portalUrl: 'https://suporte.emissor.example' },
  { name: 'Nuvem Fiscal Exemplo', portalUrl: null },
] as const;

export type DemoVendor = (typeof demoVendors)[number]['name'];

/**
 * A colleague who left the company (ADR-0058, ADR-0110): she signs in, joins the main project, opens a ticket and is
 * mentioned in the answer; then the Administrator deactivates and anonymizes her, and the screens show her by the
 * identifier of the installation ("Pessoa removida nº 1"). Only while the installation has nobody anonymized, so a new
 * load does not repeat it; the demo directory keeps her, as someone who never signed in.
 */
export const formerColleague = {
  email: 'sofia.martins@empresa.example',
  role: 'Developer',
  ticket: {
    title: 'Conciliação sem as filiais novas',
    team: 'Development',
    priority: 'Medium',
    description:
      'O relatório de conciliação bancária não traz as duas filiais incluídas em setembro. As outras aparecem normalmente.',
  },
  answer: {
    by: 'rafael',
    text: 'obrigado pelo aviso. O filtro das filiais estava fixo; ajustei e o relatório já traz as novas.',
  },
} as const satisfies {
  email: string;
  role: ProjectRole;
  ticket: { title: string; team: TicketTeam; priority: TicketPriority; description: string };
  answer: { by: DemoPerson; text: string };
};

/** The category of links that takes the links of a category the Administrator deactivated (ADR-0032). */
export const fallbackCategory = 'Outros';

export interface DemoItem {
  kind: 'Task' | 'Milestone';
  name: string;
  /** Days from the first day of the main project. */
  day: number;
  /** Business days of a task; zero for a milestone. */
  duration: number;
  owner?: DemoPerson;
  /** The client of the project is in charge of the task, as its tests (ADR-0102). */
  byClient?: boolean;
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
  /** Days from the first day of the main project of its deadline, which never moves the dates (GanttA-2). */
  deadline?: number;
}

export interface DemoPhase {
  name: string;
  items: readonly DemoItem[];
}

/**
 * A file of the demonstration, made by the command from its lines when it runs (ADR-0089): a PDF with the lines, a PNG
 * that stands for a screen capture, or the lines as text (XML, CSV, TXT). Nothing real is kept.
 */
export interface DemoFile {
  name: string;
  lines: readonly string[];
}

/** A message of a ticket, by a person who sends messages in it (ADR-0026, ADR-0044). */
export interface DemoMessage {
  by: DemoPerson;
  text: string;
  /** Mentioned at the start of the message, who then follows the ticket. */
  mention?: DemoPerson;
  /** A file sent with the message. */
  file?: DemoFile;
}

/**
 * What happens to a ticket after it is opened, in order, each by the person who may do it:
 * - a message (ADR-0026, ADR-0069): the first of who serves the team makes the person the assignee and the ticket Em
 *   andamento, and the answer of the requester in Aguardando solicitante takes it back;
 * - an action of the flow, with the message when it asks for one (ADR-0024);
 * - a new priority, by who serves the ticket (ADR-0025);
 * - a person who starts following it (ADR-0026);
 * - the assignee chosen by the manager or the tech lead, or the ticket sent to the other team (ADR-0068);
 * - a ticket opened at a vendor, registered on the day the data is loaded, and its updates there (ADR-0028).
 */
export type DemoTicketStep =
  | ({ kind: 'message' } & DemoMessage)
  | { kind: 'action'; by: DemoPerson; action: TicketAction; text?: string }
  | { kind: 'priority'; by: DemoPerson; to: TicketPriority }
  | { kind: 'follow'; by: DemoPerson }
  | { kind: 'assign'; by: DemoPerson; to: DemoPerson }
  | { kind: 'transfer'; by: DemoPerson; to: TicketTeam }
  | {
      kind: 'vendor';
      by: DemoPerson;
      vendor: DemoVendor;
      code: string;
      status: VendorTicketStatus;
      link?: string;
      note?: string;
    }
  | {
      kind: 'vendorUpdate';
      by: DemoPerson;
      code: string;
      status: VendorTicketStatus;
      note: string;
    };

/** A ticket opened by a person of the project, in the order of the plan (ADR-0029, ADR-0068). */
export interface DemoTicket {
  title: string;
  team: TicketTeam;
  priority: TicketPriority;
  /** Who opens it: anyone in the project but a viewer, or the Administrator (ADR-0044). */
  by: DemoPerson;
  description: string;
  /** Files attached to the description by who opens it, before any message (ADR-0026). */
  files?: readonly DemoFile[];
  steps?: readonly DemoTicketStep[];
}

/** A message in the steps of a ticket. */
export const say = (
  by: DemoPerson,
  text: string,
  more: Partial<DemoMessage> = {},
): DemoTicketStep => ({
  kind: 'message',
  by,
  text,
  ...more,
});

/** An action of the flow in the steps of a ticket. */
export const act = (by: DemoPerson, action: TicketAction, text?: string): DemoTicketStep =>
  text === undefined ? { kind: 'action', by, action } : { kind: 'action', by, action, text };

/**
 * A document sent to a folder of the project by a person who sends documents (ADR-0031, ADR-0045): the content is
 * fictitious, made by the command (a PDF with the lines, or the text of a CSV or TXT); then the new versions, the
 * status and the link with the schedule, each by its person.
 */
export interface DemoDocument {
  folder: string;
  name: string;
  by: DemoPerson;
  lines: readonly string[];
  /** Sent again with the same name: the next versions (ADR-0093). */
  versions?: readonly { by: DemoPerson; lines: readonly string[] }[];
  status?: { by: DemoPerson; to: DocumentStatus };
  /** The name of a phase, task or milestone of the project. */
  link?: { by: DemoPerson; to: string };
}

/** A link of the project, included by a person who keeps links (ADR-0032, ADR-0045), in a category of the migration. */
export interface DemoLink {
  title: string;
  url: string;
  category: string;
  description?: string;
  by: DemoPerson;
}

/**
 * A credential of a portal of the project, included by a person who keeps credentials (ADR-0034, ADR-0045); its
 * password is made by the command when it runs, fictitious and different each time, never written here.
 */
export interface DemoCredential {
  portal: string;
  environment: string;
  url?: string;
  username: string;
  notes?: string;
  by: DemoPerson;
}

/** A signer of a request of signature, outside the company and the system, with a fictitious e-mail (ADR-0129). */
export interface DemoSigner {
  name: string;
  email: `${string}@cliente.example`;
}

/** What a signer does on the page of the local provider: sign, or refuse with the reason. */
export type DemoSignerAnswer =
  { signer: number; signs: true } | { signer: number; refuses: string };

/**
 * An acceptance term of a milestone (ADR-0129), with the PDF made by the command: a request of signature by the
 * manager to signers of the client, at the local provider, then what each signer does on the local page, in order;
 * or, without a request, a term signed outside the system that the manager attaches.
 */
export interface DemoAcceptance {
  milestone: string;
  title: string;
  /** Who asks for it, or attaches it: the manager. */
  by: DemoPerson;
  message?: string;
  /** The PDF to sign of a request, or the signed term attached without one. */
  file: string;
  lines: readonly string[];
  /** The signers of a request, in order. */
  signers?: readonly DemoSigner[];
  /** Without a request: the signed term attached by the manager. */
  attached?: boolean;
  /** What the signers do, in order; the term is signed when all sign, and refused by one refusal. */
  answers?: readonly DemoSignerAnswer[];
}

/**
 * Hours a person of the project worked (ADR-0128), on a working day before today (0 is today), on the project only or
 * on a task of its schedule or a ticket of it, by the name of the task or the title of the ticket; never by a viewer,
 * and never more than 24 hours of a person in a day.
 */
export interface DemoTimeEntry {
  by: DemoPerson;
  /** Working days (Monday to Friday) before today. */
  daysAgo: number;
  hours: number;
  description: string;
  task?: string;
  ticket?: string;
}

/** Hours added to a project for a request out of the scope, registered by the Administrator (ADR-0128). */
export interface DemoAdditionalHours {
  /** Working days (Monday to Friday) before today. */
  daysAgo: number;
  hours: number;
  reason: string;
}

export interface DemoProject {
  code: string;
  name: string;
  client: DemoClient | null;
  /** The manager, who builds the schedule and changes the status; Marina Costa when none (ADR-0040). */
  manager?: DemoPerson;
  /**
   * Created from a model of an earlier project of the plan, from this day: the server makes the schedule (ADR-0017);
   * the project then has no phases of its own in the plan.
   */
  fromTemplate?: { name: string; start: number };
  /**
   * Created from the XML of the MS Project that the export gives of an earlier project of the plan, from this day: the
   * server converts it, and each resource goes to the person of the demo with its name, with the role of the members
   * of the plan (ADR-0135); the project then has no phases of its own in the plan.
   */
  fromMsProject?: { code: string; start: number };
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
  /** The documents sent once the schedule is built, before the project gets its status. */
  documents?: readonly DemoDocument[];
  /** The links included once the schedule is built, before the project gets its status. */
  links?: readonly DemoLink[];
  /** The credentials included once the schedule is built, before the project gets its status. */
  credentials?: readonly DemoCredential[];
  /** The acceptance terms asked for, answered and attached once the schedule is built, before the status. */
  acceptances?: readonly DemoAcceptance[];
  /** The hours logged by the people of the project, before it gets its status (ADR-0128). */
  hours?: readonly DemoTimeEntry[];
  /** The hours sold with the project, registered by the Administrator; none leaves it without a balance (ADR-0128). */
  contractedHours?: number;
  /** The hours added for requests out of the scope, registered by the Administrator (ADR-0128). */
  additionalHours?: readonly DemoAdditionalHours[];
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
    // Hours of the people of the project in the last two weeks (ADR-0128).
    // Past the hours: 68,5 logged of 56 contracted and 8 additional (ADR-0128).
    contractedHours: 56,
    additionalHours: [
      {
        daysAgo: 6,
        hours: 8,
        reason: 'Relatório de comissões, fora do escopo inicial, pedido pelo cliente',
      },
    ],
    hours: [
      {
        by: 'marina',
        daysAgo: 0,
        hours: 2,
        description: 'Reunião de acompanhamento com o cliente',
        task: 'Revisar parâmetros com o cliente',
      },
      { by: 'marina', daysAgo: 1, hours: 3, description: 'Revisão do cronograma e dos riscos' },
      {
        by: 'marina',
        daysAgo: 3,
        hours: 4,
        description: 'Alinhamento do escopo com o cliente',
        task: 'Aprovar o escopo com o cliente',
      },
      { by: 'marina', daysAgo: 6, hours: 2.5, description: 'Reunião semanal com o cliente' },
      {
        by: 'carla',
        daysAgo: 0,
        hours: 3.5,
        description: 'Roteiro dos testes de emissão de NF-e',
        task: 'Roteiro de testes',
      },
      { by: 'carla', daysAgo: 1, hours: 3, description: 'Treinamento dos usuários-chave' },
      {
        by: 'carla',
        daysAgo: 2,
        hours: 6,
        description: 'Ajustes depois da revisão da contabilidade',
        task: 'Configurar plano de contas',
      },
      {
        by: 'carla',
        daysAgo: 2,
        hours: 1.5,
        description: 'Teste da correção em homologação',
        ticket: 'Erro ao importar XML de entrada',
      },
      {
        by: 'carla',
        daysAgo: 3,
        hours: 4,
        description: 'Cadastro das contas de resultado',
        task: 'Configurar plano de contas',
      },
      {
        by: 'carla',
        daysAgo: 3,
        hours: 1.5,
        description: 'Análise da rejeição com o time técnico',
        ticket: 'Rejeição 539 ao emitir nota de devolução',
      },
      {
        by: 'carla',
        daysAgo: 5,
        hours: 6,
        description: 'Parametrização das regras de SP e RS',
        task: 'Parametrizar regras de ICMS',
      },
      {
        by: 'carla',
        daysAgo: 7,
        hours: 8,
        description: 'Entrevistas com a área fiscal',
        task: 'Mapear processos fiscais',
      },
      {
        by: 'diego',
        daysAgo: 0,
        hours: 5,
        description: 'Integração com o emissor',
        task: 'Integrar emissão de NF-e',
      },
      {
        by: 'diego',
        daysAgo: 1,
        hours: 6,
        description: 'Análise dos tempos da integração',
        ticket: 'Integração com o banco retorna tempo esgotado',
      },
      {
        by: 'diego',
        daysAgo: 4,
        hours: 7.5,
        description: 'Montagem do XML de envio',
        task: 'Integrar emissão de NF-e',
      },
      {
        by: 'rafael',
        daysAgo: 2,
        hours: 2,
        description: 'Revisão da correção da rejeição',
        ticket: 'Rejeição 539 ao emitir nota de devolução',
      },
      { by: 'rafael', daysAgo: 5, hours: 3, description: 'Revisão técnica da integração bancária' },
    ],
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
          // Late in the week it is loaded, in the queue of Meu trabalho of the manager (ADR-0070).
          task('Aprovar o escopo com o cliente', 7, 3, {
            owner: 'marina',
            progress: 60,
            after: ['Reunião de abertura'],
          }),
          // Late on the Monday it is loaded: it ended on the Friday before, at 80%, past its deadline (GanttA-2).
          task('Validar cadastro de produtos', 8, 4, {
            owner: 'diego',
            progress: 80,
            after: ['Mapear processos fiscais'],
            deadline: 10,
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
          // Of the week it is loaded, not started: "Iniciar" in Meu trabalho of the manager.
          task('Revisar parâmetros com o cliente', 14, 3, { owner: 'marina' }),
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
          // The tests on the side of the client, in its charge (ADR-0102).
          task('Testes com a equipe do cliente', 25, 2, {
            byClient: true,
            after: ['Executar testes'],
          }),
          // The date the client fixed for the acceptance, two days after the planned one.
          milestone('Aceite da homologação', 29, {
            after: ['Testes com a equipe do cliente'],
            deadline: 31,
          }),
        ],
      },
      {
        name: 'Entrada em produção',
        items: [
          // The legal deadline of the balances, as an obligation has (GanttA-2).
          task('Migrar saldos', 30, 2, {
            owner: 'diego',
            after: ['Aceite da homologação'],
            deadline: 35,
          }),
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
        priority: 'Medium',
        by: 'carla',
        description:
          'Ao emitir a nota de devolução da venda 3381, a SEFAZ retorna a rejeição 539.\n\nAcontece com todas as devoluções da filial desde ontem. Esperava a autorização normal, como no mês passado.',
        steps: [
          say(
            'diego',
            'Consegui reproduzir na homologação com a nota 3381. A rejeição vem da falta da chave da nota original no grupo de referência.',
          ),
          say(
            'rafael',
            'Vou registrar no fornecedor do emissor. Diego, segue com um contorno enquanto eles não corrigem.',
          ),
          {
            kind: 'vendor',
            by: 'rafael',
            vendor: 'Emissor Exemplo',
            code: 'FX-20931',
            status: 'Open',
            link: 'https://suporte.emissor.example/chamados/FX-20931',
            note: 'Aberto com o XML da nota 3381 e o retorno da SEFAZ.',
          },
          say('carla', 'segue a nota emitida com a natureza "Devolução de venda". Funcionou.', {
            mention: 'diego',
            file: {
              name: 'nota-3401.xml',
              lines: ['<nfeProc versao="4.00"><NFe><infNFe Id="NFe-3401"/></NFe></nfeProc>'],
            },
          }),
          { kind: 'priority', by: 'marina', to: 'High' },
          say('marina', 'Subi a prioridade para Alta: o fechamento da filial é na segunda.'),
          {
            kind: 'vendorUpdate',
            by: 'rafael',
            code: 'FX-20931',
            status: 'InAnalysis',
            note: 'O fornecedor confirmou o erro no grupo de referência e está analisando.',
          },
          say(
            'diego',
            'Contorno publicado na homologação: a devolução sai com a referência preenchida à mão. Segue a tela.',
            {
              file: {
                name: 'contorno-homologacao.png',
                lines: ['Nota de devolução 3401', 'Referência: chave da nota 3381', 'Autorizada'],
              },
            },
          ),
          {
            kind: 'vendorUpdate',
            by: 'rafael',
            code: 'FX-20931',
            status: 'WaitingForUs',
            note: 'Pediram o log completo da transmissão da nota 3381.',
          },
          say('rafael', 'o fornecedor pediu o log da transmissão. Consegue gerar hoje?', {
            mention: 'diego',
          }),
          say('diego', 'Gerei e enviei pelo portal do fornecedor. Deixo aqui também.', {
            file: {
              name: 'log-transmissao-3381.pdf',
              lines: [
                'Log da transmissão da nota 3381',
                '10:02:11 Envio do lote 552',
                '10:02:14 Retorno 539: chave da nota referenciada ausente',
              ],
            },
          }),
          {
            kind: 'vendorUpdate',
            by: 'rafael',
            code: 'FX-20931',
            status: 'InAnalysis',
            note: 'Log recebido. Correção prevista para a versão da semana que vem.',
          },
        ],
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
        steps: [
          say('rafael', 'Já vi o filtro do relatório. Corrijo junto com a próxima versão.'),
          act('rafael', 'Resolve'),
        ],
      },
      {
        title: 'Liberar ambiente de homologação ao cliente',
        team: 'Development',
        priority: 'High',
        by: 'marina',
        description:
          'O cliente precisa de acesso ao ambiente de homologação para os testes da próxima semana.',
        steps: [{ kind: 'transfer', by: 'rafael', to: 'Functional' }],
      },
      {
        title: 'Importar a tabela de NCM atualizada',
        team: 'Functional',
        priority: 'Medium',
        by: 'rafael',
        description:
          'A tabela de NCM mudou neste mês. Precisamos importar a versão nova antes dos testes.',
        steps: [
          say('carla', 'Importo hoje à tarde.'),
          act(
            'carla',
            'AskForInformation',
            'Qual a data de vigência da tabela nova? O arquivo da Receita traz duas.',
          ),
        ],
      },
      {
        title: 'Erro ao importar XML de entrada',
        team: 'Development',
        priority: 'Critical',
        by: 'carla',
        description:
          'Os XML de entrada de um fornecedor são recusados na importação, com a mensagem "Esquema inválido".',
        steps: [{ kind: 'assign', by: 'marina', to: 'diego' }],
      },
      {
        title: 'Lançamentos duplicados na conciliação bancária',
        team: 'Development',
        priority: 'High',
        by: 'carla',
        description:
          'Na conciliação de 15/10, os lançamentos do extrato aparecem duas vezes. Anexei o extrato com as linhas repetidas.',
        files: [
          {
            name: 'extrato-15-10.csv',
            lines: [
              'data;histórico;valor',
              '15/10/2026;TED recebida cliente 0412;1.250,00',
              '15/10/2026;TED recebida cliente 0412;1.250,00',
              '15/10/2026;Tarifa bancária;-12,90',
            ],
          },
        ],
        steps: [
          act('rafael', 'StartTriage'),
          say('carla', 'Aconteceu de novo hoje cedo, com o extrato de 16/10.'),
        ],
      },
      {
        title: 'Ajuste no leiaute do boleto',
        team: 'Functional',
        priority: 'Medium',
        by: 'rafael',
        description: 'O boleto precisa do logo novo do cliente e do campo do PIX.',
        steps: [
          say('carla', 'Ajustei o leiaute com o logo novo e o campo do PIX. Segue a prévia.', {
            file: {
              name: 'previa-boleto.png',
              lines: ['Logo do cliente', 'Linha digitável', 'QR Code do PIX', 'Instruções'],
            },
          }),
          say('rafael', 'Ficou certo. Pode publicar.'),
          act('carla', 'Resolve'),
          act('rafael', 'ConfirmAndClose'),
        ],
      },
      {
        title: 'Conciliação do cartão corporativo',
        team: 'Functional',
        priority: 'Low',
        by: 'diego',
        description: 'As faturas do cartão corporativo não entram na conciliação.',
        steps: [
          act(
            'diego',
            'Cancel',
            'Abri em duplicidade: o assunto segue no chamado da conciliação bancária.',
          ),
        ],
      },
      {
        title: 'Integração com o banco retorna tempo esgotado',
        team: 'Development',
        priority: 'Critical',
        by: 'carla',
        description:
          'A remessa de pagamentos para o banco falha com "tempo esgotado" desde a manhã. Os pagamentos do dia estão parados.',
        steps: [
          { kind: 'follow', by: 'paulo' },
          say('diego', 'Reproduzi: o banco demora mais de 30 segundos para responder às 9h.'),
          say('diego', 'Aumentei o tempo de espera e coloquei uma nova tentativa automática.'),
          act('diego', 'Resolve'),
          act('carla', 'Reopen', 'Voltou a acontecer hoje às 9h10, com o mesmo erro.'),
          say(
            'diego',
            'o banco trocou o certificado do canal. Preciso de ajuda para atualizar no servidor.',
            {
              mention: 'rafael',
              file: {
                name: 'erro-integracao.pdf',
                lines: [
                  'Erro da integração bancária',
                  '09:10:02 Conexão com o canal de pagamentos',
                  '09:10:32 Tempo esgotado: certificado do servidor não reconhecido',
                ],
              },
            },
          ),
          {
            kind: 'vendor',
            by: 'rafael',
            vendor: 'Nuvem Fiscal Exemplo',
            code: 'NF-88412',
            status: 'Open',
            note: 'Pedido o certificado novo do canal bancário.',
          },
          {
            kind: 'vendorUpdate',
            by: 'rafael',
            code: 'NF-88412',
            status: 'Resolved',
            note: 'Certificado novo enviado pelo fornecedor.',
          },
          say('rafael', 'Atualizo o certificado no fim do dia, fora do horário de uso.'),
        ],
      },
    ],
    documents: [
      {
        folder: '01 Gestão do projeto',
        name: 'Plano do projeto.pdf',
        by: 'marina',
        lines: [
          'Plano do projeto ERP-FIN',
          'Escopo: financeiro e contabilidade da matriz.',
          'Fases: levantamento, configuração, homologação e entrada em produção.',
        ],
        status: { by: 'marina', to: 'Approved' },
        link: { by: 'marina', to: 'Levantamento' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Especificação das regras de ICMS-ST.pdf',
        by: 'rafael',
        lines: ['Regras de ICMS-ST', 'Versão inicial com as operações de venda.'],
        versions: [
          {
            by: 'carla',
            lines: [
              'Regras de ICMS-ST',
              'Versão revista com as operações de venda e de devolução.',
            ],
          },
        ],
        status: { by: 'rafael', to: 'Approved' },
        link: { by: 'carla', to: 'Parametrizar regras de ICMS' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Mapa de processos fiscais.pdf',
        by: 'carla',
        lines: ['Mapa de processos fiscais', 'Compras, vendas, devoluções e transferências.'],
        status: { by: 'marina', to: 'Approved' },
        link: { by: 'carla', to: 'Mapear processos fiscais' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Regras de CFOP por operação.csv',
        by: 'carla',
        lines: [
          'Operação;CFOP;Descrição',
          'Venda dentro do estado;5102;Venda de mercadoria',
          'Devolução de venda;1202;Devolução de venda de mercadoria',
        ],
        status: { by: 'carla', to: 'InReview' },
        link: { by: 'carla', to: 'Parametrizar regras de ICMS' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Especificação de relatórios fiscais v1.txt',
        by: 'rafael',
        lines: ['Relatórios fiscais', 'Substituída pela especificação do relatório de ICMS-ST.'],
        status: { by: 'rafael', to: 'Obsolete' },
      },
      {
        folder: '04 Atas de reunião',
        name: 'Ata da reunião de abertura.txt',
        by: 'marina',
        lines: [
          'Ata da reunião de abertura',
          'Presentes: equipe do projeto e equipe do cliente.',
          'Decidido: homologação com a equipe do cliente antes da virada.',
        ],
        link: { by: 'marina', to: 'Reunião de abertura' },
      },
      {
        folder: '05 Entregas e manuais',
        name: 'Plano de testes integrados.txt',
        by: 'diego',
        lines: ['Plano de testes integrados', 'Roteiros de compras, vendas e apuração.'],
        link: { by: 'diego', to: 'Testes integrados' },
      },
    ],
    links: [
      {
        title: 'ERP · Homologação',
        url: 'https://hml-erp.alfadistribuidora.example',
        category: 'Ambientes',
        description: 'Usado nos testes com a equipe do cliente.',
        by: 'rafael',
      },
      {
        title: 'ERP · Produção',
        url: 'https://erp.alfadistribuidora.example',
        category: 'Ambientes',
        description: 'Somente leitura até a virada para produção.',
        by: 'rafael',
      },
      {
        title: 'Emissor de NF-e',
        url: 'https://nfe.emissor-exemplo.example/painel',
        category: 'Sistemas',
        description: 'Painel do emissor usado pela filial.',
        by: 'diego',
      },
      {
        title: 'Portal de suporte do emissor',
        url: 'https://suporte.emissor-exemplo.example',
        category: 'Sistemas',
        description: 'Onde registramos os chamados no fornecedor.',
        by: 'marina',
      },
      {
        title: 'Qualidade dos cadastros',
        url: 'https://bi.alfadistribuidora.example/cadastros',
        category: 'Painéis',
        description: 'Produtos e clientes com cadastro incompleto.',
        by: 'carla',
      },
      {
        title: 'Layouts de integração',
        url: 'https://git.empresa.example/erp-fin/layouts',
        category: 'Repositórios',
        description: 'Layouts de arquivo trocados com o emissor.',
        by: 'diego',
      },
    ],
    credentials: [
      {
        portal: 'ERP',
        environment: 'Produção',
        url: 'https://erp.alfadistribuidora.example',
        username: 'consultoria.leitura',
        notes: 'Somente leitura.',
        by: 'rafael',
      },
      {
        portal: 'SEFAZ',
        environment: 'Produção',
        url: 'https://nfe.sefaz.example',
        username: '12.345.678/0001-90',
        notes: 'Certificado A1 no cofre.',
        by: 'marina',
      },
      {
        portal: 'ERP',
        environment: 'Homologação',
        url: 'https://hml-erp.alfadistribuidora.example',
        username: 'consultoria.fiscal',
        notes: 'Perfil de administrador do módulo fiscal.',
        by: 'carla',
      },
      {
        portal: 'ERP · banco de dados',
        environment: 'Homologação',
        url: 'https://hml-db.alfadistribuidora.example',
        username: 'migracao_app',
        notes: 'Usado pelos scripts de carga.',
        by: 'diego',
      },
      {
        portal: 'Portal de suporte',
        environment: 'Fornecedores',
        url: 'https://suporte.emissor-exemplo.example',
        username: 'marina.costa',
        notes: 'Registro de chamados no fornecedor.',
        by: 'marina',
      },
    ],
    acceptances: [
      {
        milestone: 'Processos mapeados',
        title: 'Aceite do mapa de processos',
        by: 'marina',
        message: 'O mapa de processos fiscais está fechado. Por favor, assinem o termo.',
        file: 'termo-mapa-de-processos.pdf',
        lines: [
          'Termo de aceite - Mapa de processos fiscais',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Declaro que o mapa de processos fiscais foi revisado e aprovado.',
          'Assinatura: ____________________',
        ],
        signers: [clientSigners.ana, clientSigners.bruno],
        answers: [
          { signer: 0, signs: true },
          { signer: 1, signs: true },
        ],
      },
      {
        milestone: 'Processos mapeados',
        title: 'Aceite do levantamento',
        by: 'marina',
        file: 'termo-levantamento-assinado.pdf',
        lines: [
          'Termo de aceite - Levantamento',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Recebido assinado por e-mail (assinatura fictícia de demonstração).',
        ],
        attached: true,
      },
      {
        milestone: 'Roteiro aprovado',
        title: 'Aceite do roteiro de testes',
        by: 'marina',
        message: 'Segue o roteiro dos testes integrados para o aceite de vocês.',
        file: 'termo-roteiro-de-testes.pdf',
        lines: [
          'Termo de aceite - Roteiro de testes integrados',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Assinatura: ____________________',
        ],
        signers: [clientSigners.ana],
        answers: [
          { signer: 0, refuses: 'Falta incluir os testes de nota de devolução no roteiro.' },
        ],
      },
      {
        milestone: 'Aceite da homologação',
        title: 'Aceite da homologação',
        by: 'marina',
        message: 'Os testes de homologação terminaram sem pendências. Por favor, assinem o termo.',
        file: 'termo-homologacao-erp-fin.pdf',
        lines: [
          'Termo de aceite - Homologação',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Declaro que a homologação foi concluída sem pendências.',
          'Assinatura: ____________________',
        ],
        signers: [clientSigners.ana, clientSigners.bruno],
        answers: [{ signer: 0, signs: true }],
      },
    ],
  },
  {
    code: 'SPED-27',
    name: 'Adequação do SPED Fiscal 2027',
    client: 'Gama Têxtil',
    status: 'InProgress',
    // Hours of the people of the project in the last two weeks (ADR-0128).
    // Near the limit: 15 logged of 16 contracted.
    contractedHours: 16,
    hours: [
      { by: 'marina', daysAgo: 0, hours: 1.5, description: 'Planejamento da entrega de outubro' },
      {
        by: 'marina',
        daysAgo: 2,
        hours: 2,
        description: 'Conferência do arquivo com o cliente',
        ticket: 'Validar o arquivo no PVA antes da entrega',
      },
      { by: 'carla', daysAgo: 1, hours: 2.5, description: 'Reunião semanal com o cliente' },
      {
        by: 'carla',
        daysAgo: 4,
        hours: 5,
        description: 'Comparação dos leiautes de 2026 e 2027',
        task: 'Levantar mudanças do leiaute',
      },
      {
        by: 'diego',
        daysAgo: 2,
        hours: 4,
        description: 'Correção do registro K200',
        task: 'Ajustar registros do bloco K',
      },
    ],
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
    ],
    tickets: [
      {
        title: 'Registro K200 com saldo negativo',
        team: 'Development',
        priority: 'High',
        by: 'marina',
        description: 'O registro K200 de setembro sai com saldo negativo em três itens.',
        steps: [
          say('diego', 'O saldo negativo vem das ordens de produção abertas em setembro.'),
          say('diego', 'Corrigi o fechamento das ordens e gerei o arquivo de novo.'),
          act('diego', 'Resolve'),
          act('marina', 'ConfirmAndClose'),
        ],
      },
      {
        title: 'Validar o arquivo no PVA antes da entrega',
        team: 'Functional',
        priority: 'Medium',
        by: 'marina',
        description: 'Precisamos validar o arquivo de outubro no PVA antes de mandar ao cliente.',
        steps: [act('carla', 'StartTriage')],
      },
    ],
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
        // Waits for the answer of the manager: "Aguardando você" in her Meu trabalho (ADR-0070).
        steps: [
          say('carla', 'Vou ajustar o cadastro ainda hoje.'),
          act(
            'carla',
            'AskForInformation',
            'A alíquota nova vale para todas as autopeças ou só para algumas NCM?',
          ),
        ],
      },
      {
        title: 'Créditos de ICMS sem a nota de entrada',
        team: 'Functional',
        priority: 'High',
        by: 'marina',
        description:
          'Alguns créditos de ICMS da filial RS não têm a nota de entrada correspondente no sistema.',
        steps: [
          say('carla', 'Encontrei 12 notas sem o XML de entrada. Segue a planilha.', {
            file: {
              name: 'notas-sem-xml.csv',
              lines: [
                'nota;fornecedor;valor do crédito',
                '10233;Fornecedor A;318,40',
                '10251;Fornecedor B;97,15',
                '10302;Fornecedor A;1.204,90',
              ],
            },
          }),
          say('marina', 'pode pedir os XML ao fiscal da filial?', { mention: 'carla' }),
          say('carla', 'Pedi. Ele envia até quinta.'),
        ],
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
  {
    code: 'PORTAL-NF',
    name: 'Portal de notas para fornecedores',
    client: null,
    phases: [],
    // Someone serves the Funcional team, which then takes tickets (ADR-0068).
    members: [['carla', 'Contributor']],
    tickets: [
      {
        title: 'Definir o escopo do portal',
        team: 'Functional',
        priority: 'Low',
        by: 'marina',
        description: 'Quais notas os fornecedores vão poder consultar no portal?',
        // The contributor starts the triage and then the work by the buttons, and becomes the assignee (ADR-0098).
        steps: [act('carla', 'StartTriage'), act('carla', 'StartWork')],
      },
    ],
  },
  {
    code: 'WMS-ALFA',
    name: 'Implantação do WMS',
    client: 'Alfa Distribuidora',
    status: 'Completed',
    // A closed project within its hours, in the view Encerrados.
    contractedHours: 80,
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
    ],
    // Closed before the project is completed, as the conclusion asks (ADR-0043).
    tickets: [
      {
        title: 'Etiquetas de endereçamento saindo cortadas',
        team: 'Development',
        priority: 'Medium',
        by: 'carla',
        description: 'As etiquetas do armazém 2 saem cortadas na impressora térmica.',
        steps: [
          say('diego', 'Ajustei a margem da impressora térmica do armazém 2.'),
          act('diego', 'Resolve'),
          act('carla', 'ConfirmAndClose'),
        ],
      },
      {
        title: 'Treinamento extra para o turno da noite',
        team: 'Functional',
        priority: 'Low',
        by: 'marina',
        description: 'O turno da noite pediu mais um treinamento do recebimento.',
        steps: [
          act('marina', 'Cancel', 'O treinamento entrou no plano do acompanhamento assistido.'),
        ],
      },
    ],
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
  // The projects below came in Cronograma-3.3: other clients, the other statuses, a second manager and a model.
  {
    code: 'TMS-DELTA',
    name: 'Implantação do TMS',
    client: 'Delta Logística',
    manager: 'beatriz',
    techLead: 'rafael',
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
      ['marina', 'Viewer'],
    ],
    status: 'InProgress',
    // Hours of the people of the project in the last two weeks (ADR-0128).
    // Within: 10 logged of 120 contracted and 16 additional.
    contractedHours: 120,
    additionalHours: [
      { daysAgo: 3, hours: 16, reason: 'Integração com a transportadora parceira, fora do escopo' },
    ],
    hours: [
      {
        by: 'carla',
        daysAgo: 6,
        hours: 4,
        description: 'Revisão das tabelas de frete',
        task: 'Mapear rotas e tabelas de frete',
      },
      {
        by: 'diego',
        daysAgo: 3,
        hours: 6,
        description: 'Integração do CT-e',
        task: 'Integrar CT-e com o TMS',
      },
    ],
    baseline: true,
    phases: [
      {
        name: 'Levantamento',
        items: [
          task('Kickoff com a transportadora', 0, 1, { owner: 'beatriz', progress: 100 }),
          task('Mapear rotas e tabelas de frete', 1, 5, {
            owner: 'carla',
            progress: 100,
            after: ['Kickoff com a transportadora'],
          }),
          milestone('Rotas mapeadas', 6, {
            after: ['Mapear rotas e tabelas de frete'],
            reached: true,
          }),
        ],
      },
      {
        name: 'Configuração',
        items: [
          // Late in the week it is loaded: it should have ended the day before.
          task('Cadastrar tabelas de frete', 7, 6, {
            owner: 'carla',
            progress: 70,
            after: ['Rotas mapeadas'],
          }),
          // One day later than planned, after the baseline.
          task('Integrar CT-e com o TMS', 8, 10, {
            owner: 'diego',
            progress: 40,
            after: ['Rotas mapeadas'],
            movedTo: 9,
          }),
          // Late in the Meu trabalho of the second manager, and one of her week.
          task('Validar cálculo de frete com o cliente', 10, 3, {
            owner: 'beatriz',
            progress: 30,
          }),
          task('Revisar regras de ICMS do frete', 16, 3, { owner: 'beatriz' }),
          milestone('Fretes configurados', 21, {
            after: ['Cadastrar tabelas de frete', 'Integrar CT-e com o TMS'],
          }),
        ],
      },
      {
        name: 'Homologação',
        items: [
          task('Testes de emissão de CT-e', 22, 5, {
            owner: 'diego',
            after: ['Fretes configurados'],
          }),
          task('Testes da equipe do cliente', 29, 3, {
            byClient: true,
            after: ['Testes de emissão de CT-e'],
          }),
          milestone('Homologação aprovada', 34, { after: ['Testes da equipe do cliente'] }),
        ],
      },
      {
        name: 'Entrada em produção',
        items: [
          task('Virada do TMS', 35, 2, { owner: 'rafael', after: ['Homologação aprovada'] }),
          milestone('TMS em produção', 37, { after: ['Virada do TMS'] }),
        ],
      },
    ],
    tickets: [
      {
        title: 'CT-e rejeitado por divergência no valor do frete',
        team: 'Development',
        priority: 'High',
        by: 'carla',
        description:
          'O CT-e 4410 foi rejeitado pela SEFAZ: o valor total do frete não bate com a soma dos componentes.\n\nAs cargas da rota sul de amanhã dependem dele.',
        steps: [
          say(
            'diego',
            'Reproduzi com o CT-e 4410: o arredondamento do pedágio gera um centavo de diferença.',
          ),
          { kind: 'priority', by: 'beatriz', to: 'Critical' },
          say('beatriz', 'Subi para Crítica: sem o CT-e, as cargas de amanhã não saem.'),
          {
            kind: 'vendor',
            by: 'diego',
            vendor: 'Emissor Exemplo',
            code: 'FX-21107',
            status: 'Open',
            link: 'https://suporte.emissor.example/chamados/FX-21107',
            note: 'Aberto com o XML do CT-e 4410.',
          },
          say('carla', 'segue o XML do CT-e recusado.', {
            mention: 'diego',
            file: {
              name: 'cte-4410.xml',
              lines: ['<cteProc versao="4.00"><CTe><infCte Id="CTe-4410"/></CTe></cteProc>'],
            },
          }),
          {
            kind: 'vendorUpdate',
            by: 'diego',
            code: 'FX-21107',
            status: 'InAnalysis',
            note: 'O fornecedor confirmou o arredondamento do pedágio e está corrigindo.',
          },
          say(
            'diego',
            'Enquanto isso, ajustei o arredondamento na homologação. Segue a tela do CT-e autorizado.',
            {
              file: {
                name: 'cte-autorizado.png',
                lines: ['CT-e 4411', 'Componentes: frete-peso, pedágio, GRIS', 'Autorizado'],
              },
            },
          ),
        ],
      },
      {
        title: 'Tabela de frete fracionado com faixas erradas',
        team: 'Functional',
        priority: 'Medium',
        by: 'beatriz',
        description:
          'Na tabela de frete fracionado, as faixas acima de 500 kg estão com o valor por quilo da tabela antiga.',
        // Waits for the answer of the second manager: "Aguardando você" in her Meu trabalho (ADR-0070).
        steps: [
          say('carla', 'Confirmei: as faixas acima de 500 kg ficaram com o valor de 2025.'),
          act(
            'carla',
            'AskForInformation',
            'Qual valor vale acima de 500 kg: o da proposta nova ou o do contrato atual?',
          ),
        ],
      },
      {
        title: 'Liberar usuário do cliente no TMS de homologação',
        team: 'Functional',
        priority: 'Low',
        by: 'diego',
        description: 'A equipe de expedição do cliente precisa de dois usuários para os testes.',
        steps: [{ kind: 'assign', by: 'beatriz', to: 'carla' }],
      },
      {
        title: 'Lentidão ao gerar o manifesto (MDF-e)',
        team: 'Development',
        priority: 'Critical',
        by: 'beatriz',
        description:
          'Gerar o MDF-e de uma carga com mais de 50 notas leva mais de dois minutos e trava a expedição.',
        steps: [
          say('rafael', 'Vou olhar os logs do servidor de manifesto.'),
          say('diego', 'Medi cada etapa: a assinatura do XML leva quase todo o tempo.', {
            file: {
              name: 'tempo-mdfe.csv',
              lines: ['etapa;segundos', 'montagem do XML;3', 'assinatura;118', 'transmissão;4'],
            },
          }),
          say(
            'rafael',
            'Troquei a assinatura para usar o certificado em memória. Caiu para 6 segundos.',
          ),
          act('rafael', 'Resolve'),
        ],
      },
      {
        title: 'Dúvida sobre o DACTE simplificado',
        team: 'Functional',
        priority: 'Low',
        by: 'diego',
        description: 'Podemos imprimir o DACTE simplificado nas entregas da rota sul?',
        steps: [
          say('carla', 'Pode, desde que o CT-e seja de carga fracionada com até 50 entregas.'),
          act('carla', 'Resolve'),
          act('diego', 'ConfirmAndClose'),
        ],
      },
    ],
    documents: [
      {
        folder: '02 Contratos e compras',
        name: 'Proposta comercial TMS.pdf',
        by: 'beatriz',
        lines: [
          'Proposta comercial - Implantação do TMS',
          'Escopo: fretes, CT-e, MDF-e e rastreamento.',
          'Valores fictícios de demonstração.',
        ],
        status: { by: 'beatriz', to: 'Approved' },
      },
      {
        folder: '01 Gestão do projeto',
        name: 'Cronograma aprovado TMS.pdf',
        by: 'beatriz',
        lines: ['Cronograma aprovado do TMS-DELTA', 'Virada prevista para o fim do próximo mês.'],
        status: { by: 'beatriz', to: 'Approved' },
        link: { by: 'beatriz', to: 'Levantamento' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Tabelas de frete 2026.csv',
        by: 'carla',
        lines: ['rota;faixa;valor por kg', 'Sul;até 100 kg;1,90', 'Sul;acima de 500 kg;1,10'],
        versions: [
          {
            by: 'carla',
            lines: [
              'rota;faixa;valor por kg',
              'Sul;até 100 kg;1,90',
              'Sul;de 100 a 500 kg;1,45',
              'Sul;acima de 500 kg;1,05',
            ],
          },
        ],
        status: { by: 'carla', to: 'InReview' },
        link: { by: 'carla', to: 'Cadastrar tabelas de frete' },
      },
      {
        folder: '04 Atas de reunião',
        name: 'Ata do kickoff TMS.txt',
        by: 'beatriz',
        lines: [
          'Ata do kickoff do TMS',
          'Presentes: equipe do projeto e operação da transportadora.',
          'Decidido: começar pela rota sul.',
        ],
        link: { by: 'beatriz', to: 'Kickoff com a transportadora' },
      },
    ],
    links: [
      {
        title: 'TMS · Homologação',
        url: 'https://hml-tms.deltalogistica.example',
        category: 'Ambientes',
        description: 'Usado nos testes de frete e de CT-e.',
        by: 'diego',
      },
      {
        title: 'Rastreamento de cargas',
        url: 'https://rastreio.deltalogistica.example',
        category: 'Sistemas',
        description: 'Onde a expedição acompanha as entregas.',
        by: 'carla',
      },
      {
        title: 'Entregas no prazo',
        url: 'https://bi.deltalogistica.example/entregas',
        category: 'Painéis',
        description: 'Indicador de entregas por rota.',
        by: 'beatriz',
      },
      {
        title: 'Manual do TMS',
        url: 'https://docs.tms-exemplo.example/manual',
        category: 'Outros',
        by: 'carla',
      },
    ],
    credentials: [
      {
        portal: 'TMS',
        environment: 'Homologação',
        url: 'https://hml-tms.deltalogistica.example',
        username: 'consultoria.tms',
        notes: 'Perfil de configuração de fretes.',
        by: 'diego',
      },
      {
        portal: 'SEFAZ · CT-e',
        environment: 'Produção',
        url: 'https://cte.sefaz.example',
        username: 'transmissao.cte',
        notes: 'Certificado A1 da matriz no cofre.',
        by: 'beatriz',
      },
      {
        portal: 'Portal da transportadora parceira',
        environment: 'Fornecedores',
        url: 'https://parceiros.rotaexemplo.example',
        username: 'delta.logistica',
        by: 'carla',
      },
    ],
    acceptances: [
      {
        milestone: 'Rotas mapeadas',
        title: 'Aceite do mapeamento de rotas',
        by: 'beatriz',
        message: 'O mapeamento das rotas está fechado. Por favor, assine o termo.',
        file: 'termo-rotas.pdf',
        lines: [
          'Termo de aceite - Mapeamento de rotas',
          'Projeto TMS-DELTA - Implantação do TMS',
          'Assinatura: ____________________',
        ],
        signers: [clientSigners.otavio],
        answers: [{ signer: 0, signs: true }],
      },
      {
        milestone: 'Homologação aprovada',
        title: 'Aceite da homologação do TMS',
        by: 'beatriz',
        message: 'Quando os testes da homologação terminarem, assinem o termo, por favor.',
        file: 'termo-homologacao-tms.pdf',
        lines: [
          'Termo de aceite - Homologação do TMS',
          'Projeto TMS-DELTA - Implantação do TMS',
          'Assinatura: ____________________',
        ],
        signers: [clientSigners.otavio, clientSigners.paula],
      },
    ],
  },
  {
    code: 'FARMA-REINF',
    name: 'EFD-Reinf e DCTFWeb',
    client: 'Épsilon Farma',
    techLead: 'rafael',
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
    ],
    status: 'InProgress',
    phases: [
      {
        name: 'Diagnóstico',
        items: [
          task('Levantar eventos da EFD-Reinf', -7, 5, { owner: 'carla', progress: 100 }),
          milestone('Diagnóstico entregue', -1, {
            after: ['Levantar eventos da EFD-Reinf'],
            reached: true,
          }),
        ],
      },
      {
        name: 'Parametrização',
        items: [
          task('Parametrizar retenções de serviços', 0, 10, {
            owner: 'carla',
            progress: 80,
            after: ['Diagnóstico entregue'],
          }),
          task('Gerar eventos R-4000', 7, 8, { owner: 'diego', progress: 50 }),
          task('Conferir a DCTFWeb com o cliente', 15, 4, { owner: 'marina' }),
        ],
      },
      {
        name: 'Entrega',
        items: [
          milestone('Primeira transmissão', 25, {
            after: ['Gerar eventos R-4000', 'Conferir a DCTFWeb com o cliente'],
          }),
        ],
      },
    ],
    tickets: [
      {
        title: 'Evento R-4020 rejeitado por CNPJ do beneficiário',
        team: 'Development',
        priority: 'High',
        by: 'carla',
        description:
          'Os eventos R-4020 de setembro voltam rejeitados com "CNPJ do beneficiário inválido".',
        steps: [
          say('diego', 'O CNPJ do beneficiário sai sem os zeros à esquerda no evento.'),
          say('diego', 'Segue o retorno do ambiente nacional.', {
            file: {
              name: 'retorno-r4020.xml',
              lines: ['<retornoEvento><cdResposta>2</cdResposta></retornoEvento>'],
            },
          }),
          {
            kind: 'vendor',
            by: 'diego',
            vendor: 'Nuvem Fiscal Exemplo',
            code: 'NF-90127',
            status: 'WaitingForUs',
            note: 'Pediram um exemplo do evento com o CNPJ completo.',
          },
          say('rafael', 'manda o exemplo pedido pelo fornecedor ainda hoje.', {
            mention: 'diego',
          }),
        ],
      },
      {
        title: 'Retenção de INSS em nota sem destaque',
        team: 'Functional',
        priority: 'Medium',
        by: 'diego',
        description: 'Notas de serviço sem o destaque do INSS devem gerar a retenção no R-2010?',
        steps: [act('carla', 'StartTriage')],
      },
      {
        title: 'Prazo da DCTFWeb de novembro',
        team: 'Functional',
        priority: 'Low',
        by: 'marina',
        description: 'Qual o prazo da DCTFWeb de novembro com o feriado de 20/11?',
        steps: [
          say('carla', 'O prazo não muda: é o dia 15 do mês seguinte, junto com o pagamento.'),
          act('carla', 'Resolve'),
          act(
            'marina',
            'Reopen',
            'Faltou confirmar se as retenções da filial entram na mesma declaração.',
          ),
          say('carla', 'Entram, sim: a declaração é pelo CNPJ raiz.'),
        ],
      },
      {
        title: 'Acesso ao e-CAC da filial',
        team: 'Functional',
        priority: 'Medium',
        by: 'rafael',
        description: 'Precisamos da procuração eletrônica da filial para consultar a DCTFWeb.',
        steps: [act('marina', 'Cancel', 'O próprio cliente cadastrou a procuração.')],
      },
    ],
    documents: [
      {
        folder: '03 Requisitos e especificações',
        name: 'Diagnóstico da EFD-Reinf.pdf',
        by: 'carla',
        lines: [
          'Diagnóstico da EFD-Reinf',
          'Eventos da série R-4000 e retenções de serviços tomados.',
        ],
        status: { by: 'marina', to: 'Approved' },
        link: { by: 'marina', to: 'Diagnóstico entregue' },
      },
      {
        folder: '03 Requisitos e especificações',
        name: 'Naturezas de rendimento.csv',
        by: 'diego',
        lines: ['código;descrição', '15001;Serviços prestados por pessoa jurídica'],
      },
    ],
    links: [
      {
        title: 'Manual da EFD-Reinf',
        url: 'https://manuais.reinf.example/efd-reinf',
        category: 'Outros',
        by: 'carla',
      },
      {
        title: 'Retenções por fornecedor',
        url: 'https://bi.epsilonfarma.example/retencoes',
        category: 'Painéis',
        description: 'Retenções do mês, por fornecedor de serviço.',
        by: 'marina',
      },
    ],
  },
  {
    code: 'AGRO-NFE',
    name: 'NF-e de produtor rural',
    client: 'Zeta Agronegócio',
    manager: 'beatriz',
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
    ],
    // Paused until the harvest ends.
    status: 'Paused',
    phases: [
      {
        name: 'Levantamento',
        items: [
          task('Levantar operações do produtor rural', -21, 5, { owner: 'carla', progress: 100 }),
          milestone('Operações levantadas', -15, {
            after: ['Levantar operações do produtor rural'],
            reached: true,
          }),
        ],
      },
      {
        name: 'Desenvolvimento',
        items: [
          task('Adaptar a emissão para o produtor rural', -14, 15, {
            owner: 'diego',
            progress: 40,
            after: ['Operações levantadas'],
          }),
        ],
      },
      {
        name: 'Implantação',
        items: [
          milestone('Emissão liberada', 30, {
            after: ['Adaptar a emissão para o produtor rural'],
          }),
        ],
      },
    ],
    tickets: [
      {
        title: 'Inscrição estadual do produtor não validada',
        team: 'Development',
        priority: 'Medium',
        by: 'carla',
        description: 'A emissão recusa a inscrição estadual de produtor rural da fazenda 12.',
        steps: [
          say(
            'diego',
            'A validação usava a regra de empresa; a do produtor rural tem outro formato.',
          ),
          act('diego', 'Resolve'),
        ],
      },
      {
        title: 'Retomar o projeto depois da colheita',
        team: 'Functional',
        priority: 'Low',
        by: 'beatriz',
        description: 'Combinar com o cliente a data de retomada, depois da colheita da soja.',
      },
    ],
  },
  {
    code: 'MIGRA-ALFA',
    name: 'Migração do servidor fiscal',
    client: 'Alfa Distribuidora',
    members: [['diego', 'Developer']],
    // Cancelled with tickets still open: the cancellation closes them (ADR-0043).
    status: 'Cancelled',
    phases: [
      {
        name: 'Planejamento',
        items: [
          task('Inventariar rotinas do servidor fiscal', -10, 6, { owner: 'diego', progress: 60 }),
        ],
      },
    ],
    tickets: [
      {
        title: 'Backup das bases fiscais antes da migração',
        team: 'Development',
        priority: 'High',
        by: 'marina',
        description:
          'Fazer o backup completo das bases fiscais antes de desligar o servidor antigo.',
        steps: [say('diego', 'Começo o backup hoje à noite, fora do horário de uso.')],
      },
      {
        title: 'Licenças do novo servidor',
        team: 'Development',
        priority: 'Low',
        by: 'diego',
        description: 'Confirmar as licenças do banco de dados do servidor novo.',
      },
    ],
  },
  {
    code: 'ERP-EPS',
    name: 'Implantação do ERP na Épsilon',
    client: 'Épsilon Farma',
    manager: 'beatriz',
    techLead: 'rafael',
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
    ],
    // From the model saved from the ERP-FIN, starting in four weeks; it stays in Planejamento.
    fromTemplate: { name: 'Implantação de ERP', start: 28 },
    phases: [],
    tickets: [
      {
        title: 'Agendar a reunião de abertura',
        team: 'Functional',
        priority: 'Low',
        by: 'beatriz',
        description: 'Agendar a reunião de abertura com a diretoria financeira do cliente.',
        steps: [act('carla', 'StartTriage')],
      },
    ],
    documents: [
      {
        folder: '02 Contratos e compras',
        name: 'Proposta comercial ERP Épsilon.pdf',
        by: 'beatriz',
        lines: [
          'Proposta comercial - Implantação do ERP',
          'Escopo: financeiro, fiscal e contabilidade.',
          'Valores fictícios de demonstração.',
        ],
        status: { by: 'beatriz', to: 'Approved' },
      },
    ],
  },
  {
    code: 'REFORMA-26',
    name: 'Preparação para a Reforma Tributária',
    client: null,
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
      ['beatriz', 'Contributor'],
    ],
    status: 'InProgress',
    phases: [
      {
        name: 'Estudo',
        items: [
          task('Estudar as regras do IBS e da CBS', -5, 10, { owner: 'carla', progress: 100 }),
          task('Mapear impactos nos clientes', 5, 8, {
            owner: 'beatriz',
            progress: 50,
            after: ['Estudar as regras do IBS e da CBS'],
          }),
        ],
      },
      {
        name: 'Ferramentas',
        items: [
          task('Adaptar o cálculo para a CBS', 12, 8, {
            owner: 'diego',
            after: ['Estudar as regras do IBS e da CBS'],
          }),
          // Broken into subtasks (ADR-0088): the first takes the dependency of the task.
          task('Ajustar o cadastro de NCM', 12, 4, {
            owner: 'diego',
            progress: 50,
            inside: 'Adaptar o cálculo para a CBS',
          }),
          task('Testar as alíquotas de teste', 18, 4, {
            owner: 'carla',
            inside: 'Adaptar o cálculo para a CBS',
            after: ['Ajustar o cadastro de NCM'],
          }),
        ],
      },
      {
        name: 'Treinamento',
        items: [
          task('Treinar a equipe', 26, 3, {
            owner: 'marina',
            after: ['Testar as alíquotas de teste'],
          }),
          milestone('Equipe preparada', 30, { after: ['Treinar a equipe'] }),
        ],
      },
    ],
    tickets: [
      {
        title: 'Alíquota de teste da CBS no cálculo',
        team: 'Development',
        priority: 'Medium',
        by: 'beatriz',
        description: 'O cálculo precisa destacar a CBS e o IBS com as alíquotas do ano de teste.',
        steps: [
          say('diego', 'O cálculo já destaca 0,9% de CBS e 0,1% de IBS nas notas de teste.'),
          act('diego', 'Resolve'),
          act('beatriz', 'ConfirmAndClose'),
        ],
      },
      {
        title: 'Material do treinamento de IBS e CBS',
        team: 'Functional',
        priority: 'Low',
        by: 'diego',
        description: 'Montar o material do treinamento interno sobre o IBS e a CBS.',
        steps: [say('carla', 'Monto o material até sexta, com exemplos de notas de venda.')],
      },
    ],
    documents: [
      {
        folder: '05 Entregas e manuais',
        name: 'Guia rápido do IBS e da CBS.pdf',
        by: 'carla',
        lines: ['Guia rápido do IBS e da CBS', 'Ano de teste, alíquotas e destaque nas notas.'],
        versions: [
          {
            by: 'beatriz',
            lines: [
              'Guia rápido do IBS e da CBS',
              'Ano de teste, alíquotas, destaque nas notas e impactos por cliente.',
            ],
          },
        ],
        status: { by: 'carla', to: 'InReview' },
      },
    ],
    links: [
      {
        title: 'Portal da Reforma Tributária',
        url: 'https://reforma.example/ibs-cbs',
        category: 'Outros',
        description: 'Leis, regulamentos e perguntas frequentes.',
        by: 'carla',
      },
      {
        title: 'Simulador de alíquotas',
        url: 'https://simulador.empresa.example/cbs',
        category: 'Sistemas',
        by: 'diego',
      },
    ],
  },
  // A project made in a training, to show the deletion by the Administrator (ADR-0041): "Excluir projeto" in
  // "Ações do projeto", with what it has and the code to confirm.
  {
    code: 'REFORMA-RS',
    name: 'Reforma Tributária na filial RS',
    client: null,
    // Planned in the MS Project by the filial: the XML of the REFORMA-26, on the same days, with its progress and the
    // people of its tasks (ADR-0135).
    fromMsProject: { code: 'REFORMA-26', start: -5 },
    members: [
      ['carla', 'Contributor'],
      ['diego', 'Developer'],
      ['beatriz', 'Contributor'],
    ],
    status: 'InProgress',
    phases: [],
    tickets: [
      {
        title: 'Alíquotas da filial RS no cálculo',
        team: 'Functional',
        priority: 'Medium',
        by: 'beatriz',
        description:
          'Conferir se o cálculo da filial RS usa as mesmas alíquotas de teste da matriz.',
        steps: [act('carla', 'StartTriage')],
      },
    ],
  },
  {
    code: 'TESTE-01',
    name: 'Projeto de teste do treinamento',
    client: null,
    members: [['carla', 'Contributor']],
    status: 'Paused',
    phases: [
      {
        name: 'Treinamento',
        items: [
          task('Cadastrar o projeto de exemplo', 0, 2, { owner: 'carla', progress: 100 }),
          milestone('Treinamento concluído', 3, { after: ['Cadastrar o projeto de exemplo'] }),
        ],
      },
    ],
    tickets: [
      {
        title: 'Chamado de exemplo do treinamento',
        team: 'Functional',
        priority: 'Low',
        by: 'carla',
        description:
          'Chamado aberto no treinamento para mostrar a abertura. Pode ser apagado com o projeto.',
      },
    ],
    links: [
      {
        title: 'Roteiro do treinamento',
        url: 'https://treinamento.empresa.example/roteiro',
        category: 'Outros',
        by: 'carla',
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

/** The day some working days (Monday to Friday) before today, today itself for 0, as the hours of the plan say it. */
export function workDaysBefore(today: string, days: number): string {
  let time = utcOf(today);
  let left = days;
  while (left > 0) {
    time -= dayInMilliseconds;
    const weekday = new Date(time).getUTCDay();
    if (weekday !== 0 && weekday !== 6) {
      left -= 1;
    }
  }
  return dayText(time);
}
