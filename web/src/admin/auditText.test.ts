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
          { field: 'Name', before: null, after: 'Rafael Souza', secret: false },
          { field: 'Role', before: null, after: 'User', secret: false },
          { field: 'IsActive', before: null, after: 'true', secret: false },
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
        changes: [{ field: 'IsActive', before: 'false', after: 'true', secret: false }],
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
          { field: 'Name', before: 'Marina Costa', after: 'Marina Costa Alves', secret: false },
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
        changes: [{ field: 'Password', before: null, after: null, secret: true }],
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
