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

/** The manager of the projects, who builds their schedules. */
export const manager: DemoPerson = 'marina';

/** The people who get the role Project manager in the system (ADR-0040, ADR-0085). */
export const projectManagers: readonly DemoPerson[] = ['marina', 'beatriz'];

export const demoClients = ['Alfa Distribuidora', 'Beta Alimentos', 'Gama Têxtil'] as const;

export type DemoClient = (typeof demoClients)[number];

/** The vendors the team opens tickets with, fictitious, included by the Administrator (ADR-0002, ADR-0028). */
export const demoVendors = [
  { name: 'Emissor Exemplo', portalUrl: 'https://suporte.emissor.example' },
  { name: 'Nuvem Fiscal Exemplo', portalUrl: null },
] as const;

export type DemoVendor = (typeof demoVendors)[number]['name'];

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

/** A message of a ticket, by a person who sends messages in it (ADR-0026, ADR-0044). */
export interface DemoMessage {
  by: DemoPerson;
  text: string;
  /** Mentioned at the start of the message, who then follows the ticket. */
  mention?: DemoPerson;
  /** A file sent with the message. */
  file?: { name: string; content: string };
}

/** An action of the flow taken by a person who may take it, with the message when it asks for one (ADR-0024). */
export interface DemoStatusChange {
  by: DemoPerson;
  action: TicketAction;
  text?: string;
}

/**
 * A ticket opened at a vendor and registered by who serves our ticket on the day the data is loaded, with an update of
 * its status there (ADR-0028).
 */
export interface DemoVendorTicket {
  by: DemoPerson;
  vendor: DemoVendor;
  code: string;
  status: VendorTicketStatus;
  note?: string;
  update?: { by: DemoPerson; status: VendorTicketStatus; note: string };
}

/** A ticket opened by a person of the project, in the order of the plan (ADR-0029, ADR-0068). */
export interface DemoTicket {
  title: string;
  team: TicketTeam;
  priority: TicketPriority;
  /** Who opens it: anyone in the project but a viewer, or the Administrator (ADR-0044). */
  by: DemoPerson;
  description: string;
  /**
   * The conversation, in order (ADR-0069): the first message of who serves the team makes the person the assignee and
   * the ticket Em andamento.
   */
  messages?: readonly DemoMessage[];
  /** The actions of the flow taken after the conversation, in order. */
  changes?: readonly DemoStatusChange[];
  /** Assigned by the manager or the tech lead to someone who serves the team (ADR-0068). */
  assignee?: { by: DemoPerson; to: DemoPerson };
  /** Transferred to the other team by who serves it, left without an assignee (ADR-0068). */
  transfer?: { by: DemoPerson; to: TicketTeam };
  /** The tickets opened at vendors, registered after the conversation and the status (ADR-0028). */
  atVendors?: readonly DemoVendorTicket[];
}

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

/**
 * An acceptance term of a milestone (ADR-0035, ADR-0095), with PDFs made by the command: a request of the manager to a
 * person of the project, then the answer of the person, the signed term or the refusal; or, without a request, a term
 * signed outside the system that the manager attaches in the name of the person.
 */
export interface DemoAcceptance {
  milestone: string;
  title: string;
  /** The person of the term, of any role in the project. */
  to: DemoPerson;
  /** Who asks for it, or attaches it: the manager. */
  by: DemoPerson;
  message?: string;
  /** The PDF to sign of a request, or the signed term attached without one. */
  file: string;
  lines: readonly string[];
  /** Without a request: the signed term attached by the manager in the name of the person. */
  attached?: boolean;
  /** The answer of the person: the signed PDF, or the reason of the refusal. */
  answer?: { signed: string; lines: readonly string[] } | { refused: string };
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
  /** The documents sent once the schedule is built, before the project gets its status. */
  documents?: readonly DemoDocument[];
  /** The links included once the schedule is built, before the project gets its status. */
  links?: readonly DemoLink[];
  /** The credentials included once the schedule is built, before the project gets its status. */
  credentials?: readonly DemoCredential[];
  /** The acceptance terms asked for, answered and attached once the schedule is built, before the status. */
  acceptances?: readonly DemoAcceptance[];
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
        messages: [
          {
            by: 'diego',
            text: 'Consegui reproduzir na homologação com a nota 3381. A rejeição vem da falta da chave da nota original no grupo de referência.',
          },
          {
            by: 'rafael',
            text: 'Vou registrar no fornecedor do emissor. Diego, segue com um contorno enquanto eles não corrigem.',
          },
          {
            by: 'carla',
            mention: 'diego',
            text: 'segue a nota emitida com a natureza "Devolução de venda". Funcionou.',
            file: {
              name: 'nota-3401.xml',
              content: '<nfeProc versao="4.00"><NFe><infNFe Id="NFe-3401"/></NFe></nfeProc>',
            },
          },
          {
            by: 'marina',
            text: 'Subi a prioridade para Alta: o fechamento da filial é na segunda.',
          },
        ],
        atVendors: [
          {
            by: 'rafael',
            vendor: 'Emissor Exemplo',
            code: 'FX-20931',
            status: 'Open',
            note: 'Aberto com o XML da nota 3381 e o retorno da SEFAZ.',
            update: {
              by: 'rafael',
              status: 'InAnalysis',
              note: 'O fornecedor confirmou o erro no grupo de referência e está analisando.',
            },
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
        messages: [
          {
            by: 'rafael',
            text: 'Já vi o filtro do relatório. Corrijo junto com a próxima versão.',
          },
        ],
        changes: [{ by: 'rafael', action: 'Resolve' }],
      },
      {
        title: 'Liberar ambiente de homologação ao cliente',
        team: 'Development',
        priority: 'High',
        by: 'marina',
        description:
          'O cliente precisa de acesso ao ambiente de homologação para os testes da próxima semana.',
        transfer: { by: 'rafael', to: 'Functional' },
      },
      {
        title: 'Importar a tabela de NCM atualizada',
        team: 'Functional',
        priority: 'Medium',
        by: 'rafael',
        description:
          'A tabela de NCM mudou neste mês. Precisamos importar a versão nova antes dos testes.',
        messages: [{ by: 'carla', text: 'Importo hoje à tarde.' }],
        changes: [
          {
            by: 'carla',
            action: 'AskForInformation',
            text: 'Qual a data de vigência da tabela nova? O arquivo da Receita traz duas.',
          },
        ],
      },
      {
        title: 'Erro ao importar XML de entrada',
        team: 'Development',
        priority: 'Critical',
        by: 'carla',
        description:
          'Os XML de entrada de um fornecedor são recusados na importação, com a mensagem "Esquema inválido".',
        assignee: { by: 'marina', to: 'diego' },
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
        to: 'carla',
        by: 'marina',
        message:
          'Carla, o mapa de processos fiscais está fechado. Assine o termo e envie por aqui.',
        file: 'termo-mapa-de-processos.pdf',
        lines: [
          'Termo de aceite - Mapa de processos fiscais',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Declaro que o mapa de processos fiscais foi revisado e aprovado.',
          'Assinatura: ____________________',
        ],
        answer: {
          signed: 'termo-mapa-de-processos-assinado.pdf',
          lines: [
            'Termo de aceite - Mapa de processos fiscais',
            'Declaro que o mapa de processos fiscais foi revisado e aprovado.',
            'Assinado por Carla Mendes (assinatura fictícia de demonstração).',
          ],
        },
      },
      {
        milestone: 'Processos mapeados',
        title: 'Aceite do levantamento',
        to: 'beatriz',
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
        to: 'carla',
        by: 'marina',
        message: 'Carla, segue o roteiro dos testes integrados para o seu aceite.',
        file: 'termo-roteiro-de-testes.pdf',
        lines: [
          'Termo de aceite - Roteiro de testes integrados',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Assinatura: ____________________',
        ],
        answer: { refused: 'Falta incluir os testes de nota de devolução no roteiro.' },
      },
      {
        milestone: 'Aceite da homologação',
        title: 'Aceite da homologação',
        to: 'carla',
        by: 'marina',
        message:
          'Carla, quando os testes de homologação terminarem, assine o termo e envie por aqui.',
        file: 'termo-homologacao-erp-fin.pdf',
        lines: [
          'Termo de aceite - Homologação',
          'Projeto ERP-FIN - Implantação do ERP financeiro',
          'Declaro que a homologação foi concluída sem pendências.',
          'Assinatura: ____________________',
        ],
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
