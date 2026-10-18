import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import i18n from '../i18n';
import { formatLastAccess } from './dates';

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
