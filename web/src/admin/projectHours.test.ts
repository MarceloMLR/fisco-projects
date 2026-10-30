import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { balanceOf, closedProjectHours, openProjectHours } from '../test/demoProjectHours';
import {
  balanceLook,
  balanceText,
  consumptionDetail,
  hoursOrDash,
  projectHoursMeta,
} from './projectHours';

const t = i18n.t.bind(i18n);

// Spec: horas-do-projeto, "Saldo do projeto" and "Horas dos projetos pela tela" (ADR-0128).
describe('Saldo na tela', () => {
  it('Texto do saldo', () => {
    expect(
      [balanceOf(400, 40, 468), balanceOf(160, 0, 146), balanceOf(null, 8, 36)].map((balance) =>
        balanceText(balance, t),
      ),
    ).toEqual(['28 h acima', 'Restam 14 h', 'Sem horas contratadas']);
  });

  it('Ícone, cor e texto de cada estado', () => {
    expect(
      Object.entries(balanceLook).map(([state, look]) => [state, look.tone, look.progress]),
    ).toEqual([
      ['Over', 'danger', 'danger'],
      ['NearLimit', 'warning', 'warning'],
      ['Within', 'success', 'info'],
      ['NoContract', 'neutral', 'info'],
    ]);
  });

  it('Consumo ao lado da barra', () => {
    expect(consumptionDetail(balanceOf(400, 40, 468), t)).toBe('468 de 440 h (106%)');
    expect(consumptionDetail(balanceOf(160, 0, 146.5), t)).toBe('146,5 de 160 h (92%)');
    expect(consumptionDetail(balanceOf(null, 0, 36), t)).toBeUndefined();
  });

  it('Horas ou traço', () => {
    expect([hoursOrDash(400, t), hoursOrDash(0, t), hoursOrDash(null, t)]).toEqual([
      '400 h',
      '—',
      '—',
    ]);
  });

  it('Quantos projetos e quantos acima', () => {
    expect(projectHoursMeta(openProjectHours, 'Open', t)).toBe(
      '5 projetos em aberto · 1 acima das horas contratadas',
    );
    expect(projectHoursMeta(closedProjectHours, 'Closed', t)).toBe('1 projeto encerrado');
    expect(projectHoursMeta([], 'All', t)).toBe('0 projetos');
  });
});
