import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import i18n from '../i18n';
import {
  datesIn,
  daysBetween,
  defaultTimeZone,
  formatDayMonthText,
  formatPeriodText,
} from './dates';

const t = i18n.t.bind(i18n);
// 12:00:30 in Brasília, the time zone of the tests (vite.config.ts) and the default of the installations.
const now = new Date('2026-10-19T15:00:30Z');
const today = (time: string) => ptBR.dates.today.replace('{{time}}', time);
const yesterday = (time: string) => ptBR.dates.yesterday.replace('{{time}}', time);

const brasilia = datesIn(defaultTimeZone);
// Tokyo is 12 hours ahead of Brasília, and the browser of the tests is in Brasília.
const tokyo = datesIn('Asia/Tokyo');

describe('Último acesso', () => {
  it('Agora, hoje, ontem ou a data', () => {
    const { formatLastAccess } = brasilia;
    expect(formatLastAccess('2026-10-19T15:00:00Z', t, now)).toBe(ptBR.dates.now);
    expect(formatLastAccess('2026-10-19T12:12:00Z', t, now)).toBe(today('09:12'));
    expect(formatLastAccess('2026-10-19T03:05:00Z', t, now)).toBe(today('00:05'));
    expect(formatLastAccess('2026-10-19T02:55:00Z', t, now)).toBe(yesterday('23:55'));
    expect(formatLastAccess('2026-10-18T20:30:00Z', t, now)).toBe(yesterday('17:30'));
    expect(formatLastAccess('2026-08-28T18:00:00Z', t, now)).toBe('28/08/2026');
    expect(formatLastAccess(null, t, now)).toBe(ptBR.dates.never);
    expect(brasilia.formatDate('2026-10-19T02:55:00Z')).toBe('18/10/2026');
  });

  it('Hoje e ontem no fuso da instalação, não no do navegador', () => {
    // 00:00:30 of October 20th in Tokyo.
    expect(tokyo.formatLastAccess('2026-10-19T14:50:00Z', t, now)).toBe(yesterday('23:50'));
    expect(tokyo.formatLastAccess('2026-10-19T15:00:00Z', t, now)).toBe(ptBR.dates.now);
    expect(tokyo.formatLastAccess('2026-10-18T15:05:00Z', t, now)).toBe(yesterday('00:05'));
    expect(tokyo.formatLastAccess('2026-10-18T14:50:00Z', t, now)).toBe('18/10/2026');
  });
});

describe('Datas da auditoria', () => {
  it('Dia, hora e data com hora no fuso da instalação', () => {
    expect(brasilia.formatDayMonth('2026-10-16T12:58:00Z')).toBe('16/10');
    expect(brasilia.formatTime('2026-10-16T12:58:00Z')).toBe('09:58');
    expect(brasilia.formatDateTime('2026-10-17T02:30:00Z')).toBe('16/10/2026 23:30');

    expect(tokyo.formatDayMonth('2026-10-16T15:30:00Z')).toBe('17/10');
    expect(tokyo.formatTime('2026-10-16T12:58:00Z')).toBe('21:58');
    expect(tokyo.formatDateTime('2026-10-17T02:30:00Z')).toBe('17/10/2026 11:30');
  });

  it('Período dos últimos 30 dias, com o último dia inteiro', () => {
    const { lastDays, formatPeriod, formatDayValue, startOfDayValue, nextDayValue } = brasilia;
    const period = lastDays(30, now);

    expect(period).toEqual({ from: '2026-09-20', to: '2026-10-19' });
    expect(formatPeriod(period.from, period.to)).toBe('20/09 – 19/10');
    expect(formatDayValue(period.from)).toBe('20/09/2026');
    expect(startOfDayValue(period.from).toISOString()).toBe('2026-09-20T03:00:00.000Z');
    expect(startOfDayValue(nextDayValue(period.to)).toISOString()).toBe('2026-10-20T03:00:00.000Z');
    expect(nextDayValue('2026-10-31')).toBe('2026-11-01');
    expect(nextDayValue('2026-12-31')).toBe('2027-01-01');
  });

  it('Dias à meia-noite do fuso da instalação', () => {
    expect(tokyo.lastDays(1, now)).toEqual({ from: '2026-10-20', to: '2026-10-20' });
    expect(tokyo.startOfDayValue('2026-10-20').toISOString()).toBe('2026-10-19T15:00:00.000Z');
    expect(tokyo.toDayValue(new Date('2026-10-19T14:59:59Z'))).toBe('2026-10-19');
  });

  it('Meia-noite nos dias de troca do relógio', () => {
    const newYork = datesIn('America/New_York');
    // The clock goes back at 02:00 and forward at 02:00: midnight is the same moment as on any day.
    expect(newYork.startOfDayValue('2026-11-01').toISOString()).toBe('2026-11-01T04:00:00.000Z');
    expect(newYork.startOfDayValue('2026-11-02').toISOString()).toBe('2026-11-02T05:00:00.000Z');
    expect(newYork.startOfDayValue('2026-03-08').toISOString()).toBe('2026-03-08T05:00:00.000Z');
    expect(newYork.startOfDayValue('2026-03-09').toISOString()).toBe('2026-03-09T04:00:00.000Z');

    const santiago = datesIn('America/Santiago');
    // In Santiago the clock jumps from 00:00 to 01:00, so the day starts at 01:00; and goes back from 00:00 to 23:00.
    expect(santiago.startOfDayValue('2026-09-06').toISOString()).toBe('2026-09-06T04:00:00.000Z');
    expect(santiago.toDayValue(santiago.startOfDayValue('2026-09-06'))).toBe('2026-09-06');
    expect(santiago.startOfDayValue('2026-04-05').toISOString()).toBe('2026-04-05T04:00:00.000Z');
  });

  it('Mesmas funções para o mesmo fuso', () => {
    expect(datesIn('Asia/Tokyo')).toBe(tokyo);
    expect(datesIn(defaultTimeZone).formatTime).toBe(brasilia.formatTime);
  });
});

describe('Datas do cronograma', () => {
  it('Período no cabeçalho, com o ano uma vez no mesmo ano', () => {
    expect(formatPeriodText('2026-10-05', '2026-11-13')).toBe('05/10 – 13/11/2026');
    expect(formatPeriodText('2026-12-15', '2027-01-20')).toBe('15/12/2026 – 20/01/2027');
    expect(formatDayMonthText('2026-10-26')).toBe('26/10');
  });

  it('Dias corridos entre dois dias', () => {
    expect(daysBetween('2026-10-05', '2026-10-05')).toBe(0);
    expect(daysBetween('2026-10-30', '2026-11-02')).toBe(3);
    expect(daysBetween('2026-11-02', '2026-10-30')).toBe(-3);
    expect(daysBetween('2026-12-31', '2027-01-01')).toBe(1);
  });
});
