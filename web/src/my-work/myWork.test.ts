import { describe, expect, it } from 'vitest';
import type { ProjectScheduleSummary } from '../api/client';
import i18n from '../i18n';
import { datesIn } from '../format/dates';
import { demoMyWorkItems } from '../test/demoMyWork';
import { dueOf, groupLabel, itemPath, itemTitle, metaText, projectDue } from './myWork';

const t = i18n.t.bind(i18n);
const dates = datesIn('America/Sao_Paulo');
const marinaId = '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70';

// Spec: meu-trabalho, requirements "Grupos da fila" (the names of the groups) and "Meu trabalho na tela inicial"
// (the detail and the deadline of each line, ADR-0065).
describe('Textos da fila', () => {
  it('Hoje e o próximo dia útil', () => {
    expect(groupLabel('Soon', '2026-10-19', '2026-10-20', t)).toBe('Hoje e amanhã');
    expect(groupLabel('Soon', '2026-10-16', '2026-10-19', t)).toBe('Hoje e segunda');
    expect(groupLabel('Soon', '2026-10-30', '2026-11-03', t)).toBe('Hoje e terça');
    expect(groupLabel('Late', '2026-10-19', '2026-10-20', t)).toBe('Atrasado');
  });

  it('Prazo de cada linha', () => {
    const items = demoMyWorkItems;
    expect(dueOf(items.lateTask, 'Late', '2026-10-16', t)).toEqual({
      state: 'overdue',
      text: 'Venceu há 1 dia útil',
    });
    expect(dueOf({ ...items.lateTask, daysLate: 0 }, 'Late', '2026-10-17', t)).toEqual({
      state: 'overdue',
      text: 'Venceu quinta',
    });
    expect(dueOf(items.waitingTicket, 'Waiting', '2026-10-16', t)).toEqual({
      state: 'paused',
      text: 'Prazo pausado',
    });
    expect(dueOf(items.unassignedTicket, 'Soon', '2026-10-19', t).text).toBe(
      '1ª resposta até hoje',
    );
    expect(dueOf({ ...items.unassignedTicket, daysLate: 2 }, 'Late', '2026-10-21', t).text).toBe(
      '1ª resposta venceu há 2 dias úteis',
    );
    expect(dueOf(items.todayTask, 'Soon', '2026-10-16', t)).toEqual({
      state: 'soon',
      text: 'Vence hoje',
    });
    expect(dueOf(items.askedAcceptance, 'Week', '2026-10-16', t)).toEqual({
      state: 'onTrack',
      text: 'Marco em 22/10',
    });
    expect(dueOf(items.askedAcceptance, 'Soon', '2026-10-21', t).text).toBe('Marco amanhã');
    expect(dueOf(items.weekTask, 'Soon', '2026-10-20', t).text).toBe('Vence amanhã');
    expect(dueOf(items.weekTask, 'Week', '2026-10-16', t).text).toBe('Vence quarta');
    expect(dueOf(items.weekTask, 'Week', '2026-10-14', t).text).toBe('Vence 21/10');
  });

  it('Detalhe de cada linha', () => {
    const items = demoMyWorkItems;
    const now = new Date('2026-10-14T15:00:00Z');
    expect(metaText(items.waitingTicket, marinaId, dates, t, now)).toBe(
      'Diego Torres aguarda sua resposta há 2 h',
    );
    expect(
      metaText(items.waitingTicket, marinaId, dates, t, new Date('2026-10-16T15:00:00Z')),
    ).toBe('Diego Torres aguarda sua resposta desde 14/10');
    expect(metaText(items.followedTicket, marinaId, dates, t)).toBe(
      'Diego Torres é responsável · 50 interações',
    );
    expect(metaText(items.weekTask, marinaId, dates, t)).toBe('Tarefa · Configuração · 70%');
  });

  it('Endereço e título de cada linha', () => {
    const items = demoMyWorkItems;
    expect(itemPath(items.followedTicket)).toBe('/tickets/104');
    expect(itemTitle(items.followedTicket)).toBe(
      `#${String(104)} Rejeição 539 ao emitir nota de devolução`,
    );
    expect(itemTitle(items.weekTask)).toBe('Configurar plano de contas');
  });
});

// Spec: meu-trabalho, requirement "Meus projetos pela tela".
describe('Situação de cada projeto', () => {
  const schedule: ProjectScheduleSummary = {
    start: '2026-10-05',
    end: '2026-11-13',
    progress: 62,
    lateTasks: 1,
    lateMilestones: 1,
    lateDays: 2,
    currentPhase: 'Configuração',
    completedOn: null,
    phases: [],
    milestones: [],
  };

  it('Atraso, início, conclusão e sem cronograma', () => {
    expect(projectDue(schedule, '2026-10-16', t)).toEqual({
      state: 'overdue',
      text: '2 dias úteis de atraso',
    });
    expect(projectDue({ ...schedule, lateDays: 0 }, '2026-10-16', t).text).toBe('Atrasado');
    expect(
      projectDue({ ...schedule, lateDays: 0, lateTasks: 0, lateMilestones: 0 }, '2026-10-16', t)
        .text,
    ).toBe('No prazo');
    expect(projectDue({ ...schedule, start: '2026-10-26' }, '2026-10-16', t).text).toBe(
      'Começa 26/10',
    );
    expect(projectDue({ ...schedule, completedOn: '2026-11-12' }, '2026-11-13', t)).toEqual({
      state: 'met',
      text: 'Concluído em 12/11',
    });
    expect(projectDue(null, '2026-10-16', t)).toEqual({ state: 'paused', text: 'Sem data' });
  });
});
