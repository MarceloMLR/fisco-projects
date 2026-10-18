import type { TFunction } from 'i18next';

// Dates on screen (design system: 14/10/2026 and 09:42). Until the installation has its own time zone setting,
// they use the browser's.

const time = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' });
const date = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});

function startOfDay(moment: Date): number {
  return new Date(moment.getFullYear(), moment.getMonth(), moment.getDate()).getTime();
}

/** Last access of a person: now, today with the time, yesterday with the time, or the date (14/10/2026). */
export function formatLastAccess(isoUtc: string, t: TFunction, now: Date = new Date()): string {
  const moment = new Date(isoUtc);
  if (now.getTime() - moment.getTime() < 60_000) {
    return t('dates.now');
  }
  const days = Math.round((startOfDay(now) - startOfDay(moment)) / 86_400_000);
  if (days === 0) {
    return t('dates.today', { time: time.format(moment) });
  }
  if (days === 1) {
    return t('dates.yesterday', { time: time.format(moment) });
  }
  return date.format(moment);
}
