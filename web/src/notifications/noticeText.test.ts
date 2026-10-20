import { describe, expect, it } from 'vitest';
import { datesIn } from '../format/dates';
import i18n from '../i18n';
import { type BellNotification, noticeGroup, noticeText, noticeWhen } from './noticeText';

const t = i18n.t.bind(i18n);
const dates = datesIn('America/Sao_Paulo');

function notification(
  kind: BellNotification['kind'],
  values: Record<string, string>,
  actor: string | null = 'Diego Torres',
): BellNotification {
  return {
    id: '0199f5a2-0000-7000-8000-000000000001',
    kind,
    actor,
    path: '/tickets/14',
    values,
    createdAt: '2026-10-20T13:00:00Z',
    read: false,
  };
}

describe('Texto das notificações', () => {
  it('Chamado aberto, com o time em palavras', () => {
    expect(noticeText(notification('TicketOpened', { number: '14', team: 'Development' }), t)).toBe(
      'Diego Torres abriu o chamado #14 para o time Desenvolvimento',
    );
  });

  it('Prazo vencendo, com o dia', () => {
    expect(
      noticeText(
        notification(
          'TicketDueSoon',
          { number: '14', deadline: 'Resolution', dueOn: '2026-10-21' },
          null,
        ),
        t,
      ),
    ).toBe('O prazo de resolução do chamado #14 vence em 21/10/2026');
  });

  it('Mudança de status e fechamento com o projeto', () => {
    expect(
      noticeText(
        notification('TicketStatusChanged', { number: '14', from: 'Open', to: 'Triage' }),
        t,
      ),
    ).toBe('O chamado #14 passou para Em triagem');
    expect(
      noticeText(
        notification('TicketStatusChanged', {
          number: '14',
          from: 'Open',
          to: 'Closed',
          closedWithProject: 'true',
        }),
        t,
      ),
    ).toBe('O chamado #14 foi fechado com o cancelamento do projeto');
  });

  it('Inclusão no projeto, com o papel', () => {
    expect(
      noticeText(
        notification(
          'ProjectMemberAdded',
          { project: 'ERP-FIN', role: 'TechLead' },
          'Marina Costa',
        ),
        t,
      ),
    ).toBe('Marina Costa incluiu você no projeto ERP-FIN como Líder técnico');
  });

  it('Fornecedor, com o status em minúsculas', () => {
    expect(
      noticeText(
        notification('VendorTicketUpdated', {
          number: '14',
          vendor: 'Emissor Exemplo',
          vendorStatus: 'InAnalysis',
        }),
        t,
      ),
    ).toBe('O chamado #14 está em análise no Emissor Exemplo');
  });
});

describe('Quando chegou', () => {
  const now = new Date('2026-10-20T15:00:00Z');

  it('Hoje, em minutos e horas', () => {
    expect(noticeWhen('2026-10-20T14:48:00Z', dates, t, now)).toBe('há 12 min');
    expect(noticeWhen('2026-10-20T12:00:00Z', dates, t, now)).toBe('há 3 h');
    expect(noticeGroup('2026-10-20T12:00:00Z', dates, t, now)).toBe('Hoje');
  });

  it('Ontem e antes, no fuso da instalação', () => {
    // 20:40 of October 19th in Brasília.
    expect(noticeWhen('2026-10-19T23:40:00Z', dates, t, now)).toBe('ontem, 20:40');
    expect(noticeGroup('2026-10-19T23:40:00Z', dates, t, now)).toBe('Ontem');
    expect(noticeWhen('2026-10-16T12:05:00Z', dates, t, now)).toBe('16/10, 09:05');
    expect(noticeGroup('2026-10-16T12:05:00Z', dates, t, now)).toBe('16/10');
  });
});
