import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDates } from '../format/useDates';

/**
 * The day of a moment in the time zone of the installation, and the text of the line of each day: "Hoje", "Ontem" or
 * the day itself. The day of today is read once, when the conversation opens.
 */
export function useConversationDays() {
  const { t } = useTranslation();
  const { timeZone } = useDates();
  const [now] = useState(() => Date.now());
  const dayOf = useMemo(() => {
    const format = new Intl.DateTimeFormat('pt-BR', {
      timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
    return (moment: string) => format.format(new Date(moment));
  }, [timeZone]);
  const dayText = useMemo(() => {
    const format = new Intl.DateTimeFormat('pt-BR', {
      timeZone,
      weekday: 'long',
      day: 'numeric',
      month: 'long',
    });
    const today = dayOf(new Date(now).toISOString());
    const yesterday = dayOf(new Date(now - 24 * 60 * 60 * 1000).toISOString());
    return (moment: string) => {
      const day = dayOf(moment);
      const text = format.format(new Date(moment));
      return day === today
        ? t('tickets.conversation.today', { day: text })
        : day === yesterday
          ? t('tickets.conversation.yesterday', { day: text })
          : text.charAt(0).toUpperCase() + text.slice(1);
    };
  }, [dayOf, now, t, timeZone]);
  return { dayOf, dayText };
}
