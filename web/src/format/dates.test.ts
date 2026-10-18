import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import i18n from '../i18n';
import {
  formatDateTime,
  formatDayMonth,
  formatPeriod,
  formatTime,
  lastDays,
  nextDayValue,
  formatLastAccess,
  startOfDayValue,
} from './dates';

const t = i18n.t.bind(i18n);
// 12:00:30 in the time zone of the tests (vite.config.ts).
const now = new Date('2026-10-19T15:00:30Z');
const today = (time: string) => ptBR.dates.today.replace('{{time}}', time);
const yesterday = (time: string) => ptBR.dates.yesterday.replace('{{time}}', time);

describe('Último acesso', () => {
  it('Agora, hoje, ontem ou a data', () => {
    expect(formatLastAccess('2026-10-19T15:00:00Z', t, now)).toBe(ptBR.dates.now);
    expect(formatLastAccess('2026-10-19T12:12:00Z', t, now)).toBe(today('09:12'));
    expect(formatLastAccess('2026-10-19T03:05:00Z', t, now)).toBe(today('00:05'));
    expect(formatLastAccess('2026-10-19T02:55:00Z', t, now)).toBe(yesterday('23:55'));
    expect(formatLastAccess('2026-10-18T20:30:00Z', t, now)).toBe(yesterday('17:30'));
    expect(formatLastAccess('2026-08-28T18:00:00Z', t, now)).toBe('28/08/2026');
  });
});

describe('Datas da auditoria', () => {
  it('Dia, hora e data com hora no fuso do navegador', () => {
    expect(formatDayMonth('2026-10-16T12:58:00Z')).toBe('16/10');
    expect(formatTime('2026-10-16T12:58:00Z')).toBe('09:58');
    expect(formatDateTime('2026-10-17T02:30:00Z')).toBe('16/10/2026 23:30');
  });

  it('Período dos últimos 30 dias, com o último dia inteiro', () => {
    const period = lastDays(30, now);

    expect(period).toEqual({ from: '2026-09-20', to: '2026-10-19' });
    expect(formatPeriod(period.from, period.to)).toBe('20/09 – 19/10');
    expect(startOfDayValue(period.from).toISOString()).toBe('2026-09-20T03:00:00.000Z');
    expect(startOfDayValue(nextDayValue(period.to)).toISOString()).toBe('2026-10-20T03:00:00.000Z');
    expect(nextDayValue('2026-10-31')).toBe('2026-11-01');
  });
});
