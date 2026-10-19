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
