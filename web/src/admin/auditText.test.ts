import { describe, expect, it } from 'vitest';
import type { AuditEntry } from '../api/client';
import i18n from '../i18n';
import { describeAuditEntry } from './auditText';

const t = i18n.t.bind(i18n);

function entry(record: Partial<AuditEntry> & Pick<AuditEntry, 'action' | 'itemType'>): AuditEntry {
  return {
    id: '0199f5a2-7c1e-7d40-9a1b-100000000001',
    occurredAt: '2026-10-19T13:14:00Z',
    actor: { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' },
    itemId: null,
    itemLabel: null,
    detail: null,
    changes: [],
    ...record,
  };
}

// Spec: auditoria, requirement "Tela de Auditoria" (what each record says).
describe('Texto dos registros da auditoria', () => {
  it('Usuário criado na primeira entrada, por Sistema', () => {
    const created = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'User',
        actor: null,
        itemLabel: 'Rafael Souza',
        changes: [
          { field: 'Name', before: null, after: 'Rafael Souza', secret: false, reference: null },
          { field: 'Role', before: null, after: 'User', secret: false, reference: null },
          { field: 'IsActive', before: null, after: 'true', secret: false, reference: null },
        ],
      }),
      t,
    );

    expect(created).toEqual({
      title: 'Criou um usuário',
      summary: 'Rafael Souza',
      sentence: 'Sistema criou um usuário',
      properties: [{ label: 'Usuário', value: 'Rafael Souza' }],
      changes: [
        { field: 'Nome', before: '—', after: 'Rafael Souza' },
        { field: 'Papel no sistema', before: '—', after: 'Usuário' },
        { field: 'Situação', before: '—', after: 'Ativo' },
      ],
    });
  });

  it('Reativação e outra alteração do usuário', () => {
    const reactivated = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'User',
        itemLabel: 'Lívia Prado',
        changes: [
          { field: 'IsActive', before: 'false', after: 'true', secret: false, reference: null },
        ],
      }),
      t,
    );
    const renamed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'User',
        actor: null,
        itemLabel: 'Marina Costa Alves',
        changes: [
          {
            field: 'Name',
            before: 'Marina Costa',
            after: 'Marina Costa Alves',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );

    expect([reactivated.title, reactivated.summary]).toEqual([
      'Reativou um usuário',
      'Lívia Prado',
    ]);
    expect([renamed.sentence, renamed.summary]).toEqual([
      'Sistema alterou um usuário',
      'Marina Costa Alves · nome Marina Costa → Marina Costa Alves',
    ]);
  });

  it('Campo secreto sem o valor', () => {
    const secret = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'Credential',
        itemLabel: 'ERP · Homologação',
        changes: [{ field: 'Password', before: null, after: null, secret: true, reference: null }],
      }),
      t,
    );

    expect(secret.title).toBe('Alterou um item');
    expect(secret.summary).toBe('ERP · Homologação · password alterado');
    expect(secret.changes).toEqual([{ field: 'Password', before: '—', after: 'Alterado' }]);
    expect(secret.properties).toEqual([{ label: 'Item', value: 'ERP · Homologação' }]);
  });

  it('Entrada e entrada recusada', () => {
    const signIn = describeAuditEntry(
      entry({ action: 'SignedIn', itemType: 'SignIn', detail: 'local' }),
      t,
    );
    const refused = describeAuditEntry(
      entry({
        action: 'SignInRefused',
        itemType: 'SignIn',
        actor: null,
        itemLabel: 'marina.costa@empresa.example',
        detail: 'Inactive',
      }),
      t,
    );

    expect([signIn.sentence, signIn.summary]).toEqual([
      'Paulo Nogueira entrou no sistema',
      'Entrada local',
    ]);
    expect(signIn.properties).toEqual([{ label: 'Forma de entrada', value: 'Entrada local' }]);
    expect([refused.sentence, refused.summary]).toEqual([
      'Sistema recusou uma entrada',
      'marina.costa@empresa.example · conta desativada',
    ]);
    expect(refused.properties).toEqual([
      { label: 'Identidade', value: 'marina.costa@empresa.example' },
      { label: 'Motivo', value: 'conta desativada' },
    ]);
  });
});

// Spec: calendario, requirement "Mudanças do calendário na auditoria".
describe('Mudanças do calendário na auditoria', () => {
  it('Dias de trabalho na auditoria', () => {
    const changed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'WorkCalendar',
        changes: [
          {
            field: 'WorkingDays',
            before: 'Monday, Tuesday, Wednesday, Thursday, Friday',
            after: 'Monday, Tuesday, Wednesday, Thursday, Friday, Saturday',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );

    expect(changed).toEqual({
      title: 'Alterou os dias de trabalho',
      summary:
        'dias de trabalho Segunda, Terça, Quarta, Quinta, Sexta → Segunda, Terça, Quarta, Quinta, Sexta, Sábado',
      sentence: 'Paulo Nogueira alterou os dias de trabalho',
      properties: [{ label: 'Item', value: 'Calendário e feriados' }],
      changes: [
        {
          field: 'Dias de trabalho',
          before: 'Segunda, Terça, Quarta, Quinta, Sexta',
          after: 'Segunda, Terça, Quarta, Quinta, Sexta, Sábado',
        },
      ],
    });
  });

  it('Escolha de feriado na auditoria', () => {
    const considered = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'CalculatedHoliday',
        itemLabel: 'CarnivalMonday',
        changes: [
          { field: 'IsConsidered', before: 'false', after: 'true', secret: false, reference: null },
        ],
      }),
      t,
    );
    const disregarded = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'LocalHoliday',
        itemLabel: 'Data magna do estado',
        changes: [
          { field: 'IsConsidered', before: 'true', after: 'false', secret: false, reference: null },
        ],
      }),
      t,
    );

    expect(considered).toEqual({
      title: 'Considerou um feriado',
      summary: 'Segunda-feira de Carnaval',
      sentence: 'Paulo Nogueira considerou um feriado',
      properties: [{ label: 'Feriado', value: 'Segunda-feira de Carnaval' }],
      changes: [{ field: 'Considerado', before: 'Não', after: 'Sim' }],
    });
    expect(disregarded.title).toBe('Desconsiderou um feriado');
    expect(disregarded.summary).toBe('Data magna do estado');
  });

  it('Feriado local na auditoria', () => {
    const created = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'LocalHoliday',
        itemLabel: 'Emenda de Tiradentes',
        changes: [
          {
            field: 'Name',
            before: null,
            after: 'Emenda de Tiradentes',
            secret: false,
            reference: null,
          },
          { field: 'Date', before: null, after: '2026-04-20', secret: false, reference: null },
          {
            field: 'RepeatsEveryYear',
            before: null,
            after: 'false',
            secret: false,
            reference: null,
          },
          { field: 'IsConsidered', before: null, after: 'true', secret: false, reference: null },
        ],
      }),
      t,
    );
    const changed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'LocalHoliday',
        itemLabel: 'Emenda de Tiradentes',
        changes: [
          {
            field: 'Date',
            before: '2026-04-20',
            after: '2026-04-22',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );
    const removed = describeAuditEntry(
      entry({ action: 'Deleted', itemType: 'LocalHoliday', itemLabel: 'Emenda de Tiradentes' }),
      t,
    );

    expect(created.title).toBe('Incluiu um feriado');
    expect(created.changes).toEqual([
      { field: 'Nome', before: '—', after: 'Emenda de Tiradentes' },
      { field: 'Data', before: '—', after: '20/04/2026' },
      { field: 'Repete todo ano', before: '—', after: 'Não' },
      { field: 'Considerado', before: '—', after: 'Sim' },
    ]);
    expect(changed.title).toBe('Alterou um feriado');
    expect(changed.summary).toBe('Emenda de Tiradentes · data 20/04/2026 → 22/04/2026');
    expect(removed).toMatchObject({
      title: 'Excluiu um feriado',
      summary: 'Emenda de Tiradentes',
      properties: [{ label: 'Feriado', value: 'Emenda de Tiradentes' }],
    });
  });
});

// Spec: clientes, requirement "Mudanças dos clientes na auditoria" (what each record says).
describe('Mudanças dos clientes na auditoria', () => {
  it('Cliente incluído na auditoria', () => {
    const created = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'Client',
        actor: { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' },
        itemLabel: 'Kappa Energia',
        changes: [
          { field: 'Cnpj', before: null, after: '44555666000181', secret: false, reference: null },
          { field: 'IsActive', before: null, after: 'true', secret: false, reference: null },
          { field: 'Name', before: null, after: 'Kappa Energia', secret: false, reference: null },
        ],
      }),
      t,
    );

    expect(created).toEqual({
      title: 'Incluiu um cliente',
      summary: 'Kappa Energia',
      sentence: 'Marina Costa incluiu um cliente',
      properties: [{ label: 'Cliente', value: 'Kappa Energia' }],
      changes: [
        { field: 'CNPJ', before: '—', after: '44.555.666/0001-81' },
        { field: 'Situação', before: '—', after: 'Ativo' },
        { field: 'Nome', before: '—', after: 'Kappa Energia' },
      ],
    });
  });

  it('Cliente alterado na auditoria', () => {
    const cnpj = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'Client',
        itemLabel: 'Alfa Distribuidora',
        changes: [
          { field: 'Cnpj', before: null, after: '12ABC34501DE35', secret: false, reference: null },
        ],
      }),
      t,
    );
    const logo = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'Client',
        itemLabel: 'Alfa Distribuidora',
        changes: [
          {
            field: 'LogoFileName',
            before: null,
            after: 'alfa.png',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );

    expect(cnpj.title).toBe('Alterou um cliente');
    expect(cnpj.summary).toBe('Alfa Distribuidora · cnpj — → 12.ABC.345/01DE-35');
    expect(logo.changes).toEqual([{ field: 'Logo', before: '—', after: 'alfa.png' }]);
  });

  it('Cliente inativado na auditoria', () => {
    const changed = (before: string, after: string) =>
      describeAuditEntry(
        entry({
          action: 'Updated',
          itemType: 'Client',
          itemLabel: 'Épsilon Logística',
          changes: [{ field: 'IsActive', before, after, secret: false, reference: null }],
        }),
        t,
      );

    expect(changed('true', 'false')).toMatchObject({
      title: 'Inativou um cliente',
      summary: 'Épsilon Logística',
      changes: [{ field: 'Situação', before: 'Ativo', after: 'Inativo' }],
    });
    expect(changed('false', 'true').title).toBe('Reativou um cliente');
  });

  it('Cliente excluído na auditoria', () => {
    expect(
      describeAuditEntry(
        entry({ action: 'Deleted', itemType: 'Client', itemLabel: 'Gama Têxtil' }),
        t,
      ).title,
    ).toBe('Excluiu um cliente');
  });
});

// Spec: projetos, requirement "Mudanças dos projetos na auditoria".
describe('Mudanças dos projetos na auditoria', () => {
  it('Projeto criado na auditoria', () => {
    const created = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'Project',
        itemLabel: 'Implantação do ERP financeiro',
        changes: [
          {
            field: 'ClientId',
            before: null,
            after: 'Alfa Distribuidora',
            secret: false,
            reference: 'Client',
          },
          { field: 'Code', before: null, after: 'ERP-FIN', secret: false, reference: null },
          {
            field: 'Name',
            before: null,
            after: 'Implantação do ERP financeiro',
            secret: false,
            reference: null,
          },
          { field: 'Status', before: null, after: 'Planning', secret: false, reference: null },
        ],
      }),
      t,
    );
    const manager = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'ProjectMember',
        itemLabel: 'Marina Costa',
        changes: [
          {
            field: 'ProjectId',
            before: null,
            after: 'Implantação do ERP financeiro',
            secret: false,
            reference: 'Project',
          },
          { field: 'Role', before: null, after: 'Manager', secret: false, reference: null },
        ],
      }),
      t,
    );

    expect(created).toEqual({
      title: 'Criou um projeto',
      summary: 'Implantação do ERP financeiro',
      sentence: 'Paulo Nogueira criou um projeto',
      properties: [{ label: 'Projeto', value: 'Implantação do ERP financeiro' }],
      changes: [
        { field: 'Cliente', before: '—', after: 'Alfa Distribuidora' },
        { field: 'Código', before: '—', after: 'ERP-FIN' },
        { field: 'Nome', before: '—', after: 'Implantação do ERP financeiro' },
        { field: 'Status', before: '—', after: 'Planejamento' },
      ],
    });
    expect(manager).toEqual({
      title: 'Incluiu uma pessoa num projeto',
      summary: 'Marina Costa · Gerente de projeto · Implantação do ERP financeiro',
      sentence: 'Paulo Nogueira incluiu uma pessoa num projeto',
      properties: [
        { label: 'Pessoa', value: 'Marina Costa' },
        { label: 'Projeto', value: 'Implantação do ERP financeiro' },
      ],
      changes: [
        { field: 'Projeto', before: '—', after: 'Implantação do ERP financeiro' },
        { field: 'Papel no projeto', before: '—', after: 'Gerente de projeto' },
      ],
    });
  });

  it('Status na auditoria', () => {
    const status = (before: string, after: string) =>
      describeAuditEntry(
        entry({
          action: 'Updated',
          itemType: 'Project',
          itemLabel: 'Implantação do WMS',
          changes: [{ field: 'Status', before, after, secret: false, reference: null }],
        }),
        t,
      );

    expect(status('InProgress', 'Completed')).toMatchObject({
      title: 'Concluiu um projeto',
      changes: [{ field: 'Status', before: 'Em andamento', after: 'Concluído' }],
    });
    expect(status('Completed', 'InProgress')).toMatchObject({
      title: 'Reabriu um projeto',
      changes: [{ field: 'Status', before: 'Concluído', after: 'Em andamento' }],
    });
    expect(status('Planning', 'Cancelled').title).toBe('Cancelou um projeto');
    expect(status('InProgress', 'Paused').title).toBe('Mudou o status de um projeto');
  });

  it('Projeto alterado na auditoria', () => {
    const changed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'Project',
        itemLabel: 'Implantação do ERP financeiro e fiscal',
        changes: [
          {
            field: 'Name',
            before: 'Implantação do ERP financeiro',
            after: 'Implantação do ERP financeiro e fiscal',
            secret: false,
            reference: null,
          },
          {
            field: 'ClientId',
            before: 'Alfa Distribuidora',
            after: null,
            secret: false,
            reference: 'Client',
          },
        ],
      }),
      t,
    );

    expect(changed.title).toBe('Alterou um projeto');
    expect(changed.changes).toEqual([
      {
        field: 'Nome',
        before: 'Implantação do ERP financeiro',
        after: 'Implantação do ERP financeiro e fiscal',
      },
      { field: 'Cliente', before: 'Alfa Distribuidora', after: '—' },
    ]);
  });
});

// Spec: membros-do-projeto, requirement "Mudanças das pessoas na auditoria".
describe('Mudanças das pessoas na auditoria', () => {
  it('Papel trocado e pessoa removida na auditoria', () => {
    const project = 'Implantação do ERP financeiro';
    const changed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ProjectMember',
        actor: { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' },
        itemLabel: 'Diego Torres',
        changes: [
          {
            field: 'ProjectId',
            before: project,
            after: project,
            secret: false,
            reference: 'Project',
          },
          {
            field: 'Role',
            before: 'Developer',
            after: 'Contributor',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );
    const removed = describeAuditEntry(
      entry({
        action: 'Deleted',
        itemType: 'ProjectMember',
        itemLabel: 'Diego Torres',
        changes: [
          { field: 'ProjectId', before: project, after: null, secret: false, reference: 'Project' },
          { field: 'Role', before: 'Contributor', after: null, secret: false, reference: null },
          {
            field: 'UserId',
            before: 'Diego Torres',
            after: null,
            secret: false,
            reference: 'User',
          },
        ],
      }),
      t,
    );

    expect(changed).toEqual({
      title: 'Trocou o papel de uma pessoa num projeto',
      summary: `Diego Torres · Colaborador · ${project}`,
      sentence: 'Marina Costa trocou o papel de uma pessoa num projeto',
      properties: [
        { label: 'Pessoa', value: 'Diego Torres' },
        { label: 'Projeto', value: project },
      ],
      changes: [{ field: 'Papel no projeto', before: 'Desenvolvedor', after: 'Colaborador' }],
    });
    expect(removed.title).toBe('Removeu uma pessoa de um projeto');
    expect(removed.summary).toBe(`Diego Torres · Colaborador · ${project}`);
    expect(removed.changes).toEqual([
      { field: 'Projeto', before: project, after: '—' },
      { field: 'Papel no projeto', before: 'Colaborador', after: '—' },
      { field: 'Pessoa', before: 'Diego Torres', after: '—' },
    ]);
  });
});

// Spec: cronograma, requirement "Mudanças do cronograma na auditoria".
describe('Mudanças do cronograma na auditoria', () => {
  const project = 'Implantação do ERP financeiro';
  const marinaCosta = { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' };
  const context = (field: string, value: string, reference: string | null = null) => ({
    field,
    before: value,
    after: value,
    secret: false,
    reference,
  });

  it('Tarefa empurrada na auditoria', () => {
    const pushed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ScheduleItem',
        actor: marinaCosta,
        itemLabel: 'Aceite da homologação',
        // In the alphabetical order the API keeps the fields; the screen shows the start first.
        changes: [
          {
            field: 'End',
            before: '2026-11-03',
            after: '2026-11-04',
            secret: false,
            reference: null,
          },
          context('Kind', 'Milestone'),
          context('ProjectId', project, 'Project'),
          {
            field: 'Start',
            before: '2026-11-03',
            after: '2026-11-04',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );
    const task = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ScheduleItem',
        actor: marinaCosta,
        itemLabel: 'Validar cadastro de produtos',
        changes: [
          context('ProjectId', project, 'Project'),
          context('Kind', 'Task'),
          {
            field: 'OwnerId',
            before: 'Diego Torres',
            after: null,
            secret: false,
            reference: 'User',
          },
        ],
      }),
      t,
    );

    expect(pushed).toEqual({
      title: 'Alterou um marco',
      summary: 'Aceite da homologação · início 03/11/2026 → 04/11/2026',
      sentence: 'Marina Costa alterou um marco',
      properties: [
        { label: 'Tarefa ou marco', value: 'Aceite da homologação' },
        { label: 'Projeto', value: project },
      ],
      changes: [
        { field: 'Início', before: '03/11/2026', after: '04/11/2026' },
        { field: 'Fim', before: '03/11/2026', after: '04/11/2026' },
      ],
    });
    expect(task.title).toBe('Alterou uma tarefa');
    expect(task.changes).toEqual([{ field: 'Responsável', before: 'Diego Torres', after: '—' }]);
  });

  it('Tarefa incluída e excluída na auditoria', () => {
    const fields = (after: boolean) =>
      (
        [
          ['ProjectId', project, 'Project'],
          ['PhaseId', 'Configuração', 'SchedulePhase'],
          ['Kind', 'Task', null],
          ['Name', 'Parametrizar regras de ICMS', null],
          ['Start', '2026-10-13', null],
          ['Duration', '8', null],
          ['End', '2026-10-22', null],
          ['Progress', '0', null],
        ] satisfies [string, string, string | null][]
      ).map(([field, value, reference]) => ({
        field,
        before: after ? null : value,
        after: after ? value : null,
        secret: false,
        reference,
      }));
    const created = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'ScheduleItem',
        itemLabel: 'Parametrizar regras de ICMS',
        changes: fields(true),
      }),
      t,
    );
    const removed = describeAuditEntry(
      entry({
        action: 'Deleted',
        itemType: 'ScheduleItem',
        itemLabel: 'Parametrizar regras de ICMS',
        changes: fields(false),
      }),
      t,
    );

    expect(created.title).toBe('Incluiu uma tarefa');
    expect(created.summary).toBe(`Parametrizar regras de ICMS · ${project}`);
    expect(created.changes).toEqual([
      { field: 'Projeto', before: '—', after: project },
      { field: 'Fase', before: '—', after: 'Configuração' },
      { field: 'Tipo', before: '—', after: 'Tarefa' },
      { field: 'Nome', before: '—', after: 'Parametrizar regras de ICMS' },
      { field: 'Início', before: '—', after: '13/10/2026' },
      { field: 'Duração em dias úteis', before: '—', after: '8' },
      { field: 'Fim', before: '—', after: '22/10/2026' },
      { field: 'Progresso (%)', before: '—', after: '0' },
    ]);
    expect(removed.title).toBe('Excluiu uma tarefa');
  });

  // Spec: cronograma, requirement "Marco atingido".
  it('Marcar marco como atingido', () => {
    const reached = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ScheduleItem',
        actor: marinaCosta,
        itemLabel: 'Aceite da homologação',
        changes: [
          context('Kind', 'Milestone'),
          context('ProjectId', project, 'Project'),
          {
            field: 'ReachedOn',
            before: null,
            after: '2026-10-16',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );

    expect(reached.title).toBe('Alterou um marco');
    expect(reached.summary).toBe('Aceite da homologação · atingido em — → 16/10/2026');
    expect(reached.changes).toEqual([{ field: 'Atingido em', before: '—', after: '16/10/2026' }]);
  });

  it('Fase renomeada na auditoria', () => {
    const renamed = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'SchedulePhase',
        itemLabel: 'Diagnóstico',
        changes: [
          context('ProjectId', project, 'Project'),
          {
            field: 'Name',
            before: 'Levantamento',
            after: 'Diagnóstico',
            secret: false,
            reference: null,
          },
        ],
      }),
      t,
    );

    expect(renamed.title).toBe('Renomeou uma fase de um cronograma');
    expect(renamed.summary).toBe('Diagnóstico · nome Levantamento → Diagnóstico');
    expect(renamed.properties).toEqual([
      { label: 'Fase', value: 'Diagnóstico' },
      { label: 'Projeto', value: project },
    ]);
  });

  it('Dependência na auditoria', () => {
    const changes = (after: boolean) =>
      (
        [
          ['ProjectId', project, 'Project'],
          ['PredecessorId', 'Parametrizar regras de ICMS', 'ScheduleItem'],
          ['SuccessorId', 'Testes integrados', 'ScheduleItem'],
        ] satisfies [string, string, string][]
      ).map(([field, value, reference]) => ({
        field,
        before: after ? null : value,
        after: after ? value : null,
        secret: false,
        reference,
      }));
    const included = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'ScheduleDependency',
        itemLabel: 'Testes integrados',
        changes: changes(true),
      }),
      t,
    );
    const removed = describeAuditEntry(
      entry({
        action: 'Deleted',
        itemType: 'ScheduleDependency',
        itemLabel: 'Testes integrados',
        changes: changes(false),
      }),
      t,
    );

    expect(included.title).toBe('Incluiu uma dependência');
    expect(included.summary).toBe(
      `Testes integrados · depende de Parametrizar regras de ICMS · ${project}`,
    );
    expect(included.properties).toEqual([
      { label: 'Seguinte', value: 'Testes integrados' },
      { label: 'Projeto', value: project },
    ]);
    expect(included.changes).toEqual([
      { field: 'Projeto', before: '—', after: project },
      { field: 'Anterior', before: '—', after: 'Parametrizar regras de ICMS' },
      { field: 'Seguinte', before: '—', after: 'Testes integrados' },
    ]);
    expect(removed.title).toBe('Excluiu uma dependência');
    expect(removed.summary).toBe(
      `Testes integrados · depende de Parametrizar regras de ICMS · ${project}`,
    );
  });

  it('Linha de base na auditoria', () => {
    const field = (name: string, before: string | null, after: string) => ({
      field: name,
      before,
      after,
      secret: false,
      reference: null,
    });
    const saved = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'ScheduleBaseline',
        itemLabel: project,
        changes: [
          field('End', null, '2026-10-29'),
          field('SavedOn', null, '2026-10-09'),
          field('Start', null, '2026-10-05'),
        ],
      }),
      t,
    );
    const updated = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ScheduleBaseline',
        itemLabel: project,
        changes: [
          field('End', '2026-10-29', '2026-11-03'),
          field('SavedOn', '2026-10-09', '2026-10-16'),
        ],
      }),
      t,
    );

    expect(saved.title).toBe('Salvou a linha de base');
    expect(saved.summary).toBe(project);
    expect(saved.properties).toEqual([{ label: 'Projeto', value: project }]);
    expect(updated.title).toBe('Atualizou a linha de base');
    expect(updated.summary).toBe(`${project} · salva em 09/10/2026 → 16/10/2026`);
    // The day it was saved first, then the start and the end.
    expect(saved.changes.map((change) => change.field)).toEqual(['Salva em', 'Início', 'Fim']);
    expect(updated.changes).toEqual([
      { field: 'Salva em', before: '09/10/2026', after: '16/10/2026' },
      { field: 'Fim', before: '29/10/2026', after: '03/11/2026' },
    ]);
  });

  it('Modelo na auditoria', () => {
    const field = (name: string, before: string | null, after: string | null) => ({
      field: name,
      before,
      after,
      secret: false,
      reference: null,
    });
    const deactivated = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'ProjectTemplate',
        itemLabel: 'Implantação de ERP',
        changes: [field('IsActive', 'true', 'false')],
      }),
      t,
    );

    expect(deactivated.title).toBe('Alterou um modelo');
    expect(deactivated.summary).toBe('Implantação de ERP · situação Ativo → Inativo');
    expect(deactivated.properties).toEqual([{ label: 'Modelo', value: 'Implantação de ERP' }]);
  });
  // Spec: chamados, requirement "Chamados na auditoria".
  it('Abertura do chamado na auditoria', () => {
    const field = (name: string, after: string | null) => ({
      field: name,
      before: null,
      after,
      secret: false,
      reference: null,
    });
    const opened = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'Ticket',
        itemLabel: 'Rejeição 539 ao emitir nota de devolução',
        changes: [
          field('Number', '104'),
          field('ProjectId', 'Implantação do ERP financeiro'),
          field('Team', 'Development'),
          field('Priority', 'High'),
          field('Status', 'Open'),
        ],
      }),
      t,
    );

    expect(opened.title).toBe('Abriu um chamado');
    expect(opened.properties).toEqual([
      { label: 'Chamado', value: 'Rejeição 539 ao emitir nota de devolução' },
      { label: 'Projeto', value: 'Implantação do ERP financeiro' },
    ]);
    expect(opened.changes).toEqual(
      expect.arrayContaining([
        { field: 'Número', before: '—', after: '104' },
        { field: 'Time', before: '—', after: 'Desenvolvimento' },
        { field: 'Prioridade', before: '—', after: 'Alta' },
        { field: 'Status', before: '—', after: 'Aberto' },
      ]),
    );
  });
  // Spec: chamados, requirements "Fornecedores" and "Chamado no fornecedor".
  it('Fornecedores na auditoria', () => {
    const field = (name: string, before: string | null, after: string | null) => ({
      field: name,
      before,
      after,
      secret: false,
      reference: null,
    });
    const deactivated = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'Vendor',
        itemLabel: 'Emissor Exemplo',
        changes: [field('IsActive', 'true', 'false')],
      }),
      t,
    );
    const registered = describeAuditEntry(
      entry({
        action: 'Created',
        itemType: 'VendorTicket',
        itemLabel: 'FX-20931',
        changes: [
          field('TicketId', null, '#1 Rejeição 539 ao emitir nota de devolução'),
          field('VendorId', null, 'Emissor Exemplo'),
          field('Code', null, 'FX-20931'),
          field('OpenedOn', null, '2026-10-19'),
          field('Status', null, 'InAnalysis'),
        ],
      }),
      t,
    );
    const updated = describeAuditEntry(
      entry({
        action: 'Updated',
        itemType: 'VendorTicket',
        itemLabel: 'FX-20931',
        changes: [
          field('Status', 'InAnalysis', 'WaitingForUs'),
          field(
            'TicketId',
            '#1 Rejeição 539 ao emitir nota de devolução',
            '#1 Rejeição 539 ao emitir nota de devolução',
          ),
          field('VendorId', 'Emissor Exemplo', 'Emissor Exemplo'),
        ],
      }),
      t,
    );

    expect(deactivated.title).toBe('Inativou um fornecedor');
    expect(deactivated.summary).toBe('Emissor Exemplo');
    expect(deactivated.properties).toEqual([{ label: 'Fornecedor', value: 'Emissor Exemplo' }]);
    expect(registered.title).toBe('Registrou um chamado no fornecedor');
    expect(registered.summary).toBe(
      '#1 Rejeição 539 ao emitir nota de devolução · FX-20931 no Emissor Exemplo',
    );
    expect(registered.changes).toEqual(
      expect.arrayContaining([
        { field: 'Aberto em', before: '—', after: '19/10/2026' },
        { field: 'Status no fornecedor', before: '—', after: 'Em análise' },
      ]),
    );
    expect(updated.title).toBe('Atualizou um chamado no fornecedor');
    expect(updated.summary).toBe(
      'FX-20931 · status no fornecedor Em análise → Aguardando nosso retorno',
    );
    expect(updated.properties).toEqual([
      { label: 'Chamado no fornecedor', value: 'FX-20931' },
      { label: 'Fornecedor', value: 'Emissor Exemplo' },
      { label: 'Chamado', value: '#1 Rejeição 539 ao emitir nota de devolução' },
    ]);
  });
});
