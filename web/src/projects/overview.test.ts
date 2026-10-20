import { describe, expect, it } from 'vitest';
import { datesIn } from '../format/dates';
import i18n from '../i18n';
import { demoOverview, demoOverviewTaskId, demoOverviewTermId } from '../test/demoOverview';
import { attentionLines, deviationDue, milestoneText } from './overview';

const t = i18n.t.bind(i18n);
const dates = datesIn('America/Sao_Paulo');
const projectId = '0199f5a2-7c1e-7d40-9a1b-400000000001';
const ticket = (number: number) => `#${String(number)}`;

// Spec: visao-geral-do-projeto, requirement "Prazo final contra a linha de base" (ADR-0021, ADR-0065).
describe('Prazo final contra a linha de base', () => {
  it('Desvio do prazo final', () => {
    expect(deviationDue({ end: '2026-11-13', baselineEnd: '2026-11-11', deviation: 2 }, t)).toEqual(
      { state: 'soon', text: '+2 dias úteis' },
    );
    expect(deviationDue({ end: '2026-11-12', baselineEnd: '2026-11-11', deviation: 1 }, t)).toEqual(
      { state: 'soon', text: '+1 dia útil' },
    );
    expect(
      deviationDue({ end: '2026-11-10', baselineEnd: '2026-11-11', deviation: -1 }, t),
    ).toEqual({ state: 'met', text: '1 dia útil antes' });
    expect(deviationDue({ end: '2026-11-11', baselineEnd: '2026-11-11', deviation: 0 }, t)).toEqual(
      {
        state: 'met',
        text: 'Na data da linha de base',
      },
    );
  });

  it('Prazo sem linha de base', () => {
    expect(
      deviationDue({ end: '2026-11-13', baselineEnd: null, deviation: null }, t),
    ).toBeUndefined();
  });
});

// Spec: visao-geral-do-projeto, requirement "Chamados e próximo marco na visão geral".
describe('Chamados e próximo marco na visão geral', () => {
  const milestone = {
    id: '0199f5a2-7c1e-7d40-9a1b-0000000f0004',
    name: 'Homologação',
    day: '2026-11-03',
    businessDaysLeft: 10,
    daysLate: null,
  };

  it('Próximo marco', () => {
    expect(milestoneText(milestone, t)).toBe('03/11 · em 10 dias úteis');
    expect(milestoneText({ ...milestone, businessDaysLeft: 1 }, t)).toBe('03/11 · em 1 dia útil');
    expect(milestoneText({ ...milestone, businessDaysLeft: 0 }, t)).toBe('03/11 · hoje');
    expect(milestoneText({ ...milestone, businessDaysLeft: 0, daysLate: 2 }, t)).toBe(
      '03/11 · 2 dias úteis de atraso',
    );
    expect(milestoneText({ ...milestone, businessDaysLeft: 0, daysLate: 0 }, t)).toBe(
      '03/11 · atrasado',
    );
  });
});

// Spec: visao-geral-do-projeto, requirement "Precisa de atenção" (the texts and the addresses of the lines).
describe('Precisa de atenção', () => {
  it('Tarefa atrasada na atenção', () => {
    const overview = demoOverview();
    const [task] = attentionLines(projectId, overview.attention, overview.today, dates, t);
    expect(task).toEqual({
      key: `task-${demoOverviewTaskId}`,
      kind: 'task',
      title: 'Validar cadastro de produtos',
      path: `/projects/${projectId}/schedule?item=${demoOverviewTaskId}`,
      meta: 'Tarefa de Diego Torres · 80% · bloqueia Configurar plano de contas',
      due: { state: 'overdue', text: 'Venceu há 1 dia útil' },
    });

    const attention = {
      ...overview.attention,
      tasks: [
        {
          id: demoOverviewTaskId,
          name: 'Migrar saldos',
          owner: null,
          progress: 0,
          end: '2026-10-15',
          daysLate: 0,
          blocks: ['Conferir saldos', 'Virada para produção'],
        },
      ],
    };
    const [other] = attentionLines(projectId, attention, '2026-10-16', dates, t);
    expect(other?.meta).toBe('Tarefa sem responsável · 0% · bloqueia Conferir saldos e mais 1');
    expect(other?.due).toEqual({ state: 'overdue', text: 'Venceu quinta' });
  });

  it('Chamados na atenção', () => {
    const overview = demoOverview();
    const lines = attentionLines(projectId, overview.attention, overview.today, dates, t).filter(
      (line) => line.kind === 'ticket' || line.kind === 'unassigned',
    );
    expect(lines.map((line) => [line.title, line.path, line.meta, line.due])).toEqual([
      [
        `${ticket(104)} Rejeição 539 ao emitir nota de devolução`,
        '/tickets/104',
        'Desenvolvimento · Diego Torres · 50 interações',
        { state: 'soon', text: 'Vence segunda' },
      ],
      [
        `${ticket(101)} Erro ao importar XML de entrada`,
        '/tickets/101',
        'Desenvolvimento · Lucas Prado · 14 interações',
        { state: 'soon', text: 'Vence segunda' },
      ],
      [
        `${ticket(110)} Relatório de ICMS-ST sem as notas de devolução`,
        '/tickets/110',
        'Desenvolvimento · sem responsável',
        { state: 'soon', text: '1ª resposta até segunda' },
      ],
    ]);
    expect(lines.map((line) => line.kind)).toEqual(['ticket', 'ticket', 'unassigned']);

    const [late, paused] = attentionLines(
      projectId,
      {
        ...overview.attention,
        tasks: [],
        acceptances: [],
        vendors: [],
        tickets: [
          {
            number: 98,
            title: 'Divergência no SPED',
            team: 'Functional',
            assignee: null,
            interactions: 3,
            deadline: 'FirstResponse',
            dueState: 'Overdue',
            dueOn: '2026-10-13',
            daysLate: 2,
          },
          {
            number: 97,
            title: 'Cadastro de CFOP',
            team: 'Functional',
            assignee: null,
            interactions: 2,
            deadline: null,
            dueState: null,
            dueOn: null,
            daysLate: null,
          },
        ],
      },
      overview.today,
      dates,
      t,
    );
    expect(late?.meta).toBe('Funcional · sem responsável');
    expect(late?.due).toEqual({ state: 'overdue', text: '1ª resposta venceu há 2 dias úteis' });
    expect(paused?.due).toEqual({ state: 'paused', text: 'Prazo pausado' });
  });

  it('Aceites e fornecedores na atenção', () => {
    const overview = demoOverview();
    const requested = {
      id: demoOverviewTermId,
      title: 'Aceite da homologação',
      recipient: { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' },
      requestedAt: '2026-10-15T13:00:00Z',
      milestone: 'Aceite da homologação',
      milestoneDay: '2026-11-03',
    };
    const [, , , , term, vendor] = attentionLines(
      projectId,
      overview.attention,
      overview.today,
      dates,
      t,
    );
    expect(term).toEqual({
      key: `acceptance-${demoOverviewTermId}`,
      kind: 'acceptance',
      title: 'Aceite da homologação',
      path: `/projects/${projectId}/acceptances?term=${demoOverviewTermId}`,
      meta: 'Termo pedido a Carla Mendes em 15/10',
      due: { state: 'onTrack', text: 'Marco em 03/11' },
    });
    expect(vendor).toEqual({
      key: 'vendor-104-FX-20931',
      kind: 'vendor',
      title: 'Emissor Exemplo · FX-20931',
      path: '/tickets/104',
      meta: `Chamado no fornecedor ligado ao ${ticket(104)}`,
      due: { state: 'soon', text: 'Sem retorno há 6 dias úteis' },
    });

    const [past, without] = attentionLines(
      projectId,
      {
        ...overview.attention,
        tasks: [],
        tickets: [],
        vendors: [],
        acceptances: [
          { ...requested, milestoneDay: '2026-10-14' },
          {
            ...requested,
            id: '0199f5a2-7c1e-7d40-9a1b-0000000f2002',
            milestone: null,
            milestoneDay: null,
          },
        ],
      },
      overview.today,
      dates,
      t,
    );
    expect(past?.due).toEqual({ state: 'overdue', text: 'Marco em 14/10' });
    expect(without?.due).toEqual({ state: 'onTrack', text: 'Pedido em 15/10' });
  });
});
