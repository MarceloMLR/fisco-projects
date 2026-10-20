import { useTranslation } from 'react-i18next';
import type { TicketDue as Due, TicketDueState } from '../api/client';
import { DueDate, type DueState } from '../components/DueDate';
import { formatWeekdayDayText } from '../format/dates';

const looks: Record<TicketDueState, DueState> = {
  OnTrack: 'onTrack',
  Soon: 'soon',
  Overdue: 'overdue',
  Paused: 'paused',
  Met: 'met',
};

/**
 * A deadline of a ticket (ADR-0025) by the DueDate of the design system: the icon, the color and the text of its state,
 * as "Vence segunda, 26/10", "Venceu há 2 dias úteis" or "Prazo pausado" (ADR-0065).
 */
export function TicketDue({
  due,
  metText,
  finished = false,
}: {
  due: Due;
  metText?: string;
  /** The ticket was resolved: a late deadline says by how much it was late, not that it is late now. */
  finished?: boolean;
}) {
  const { t } = useTranslation();
  const day = due.dueOn === null ? '' : formatWeekdayDayText(due.dueOn);
  const text = {
    OnTrack: () => t('tickets.due.onTrack', { day }),
    Soon: () => t('tickets.due.soon', { day }),
    Overdue: () =>
      t(finished ? 'tickets.due.lateResolved' : 'tickets.due.overdue', {
        count: due.daysLate ?? 1,
      }),
    Paused: () => t('tickets.due.paused'),
    Met: () => metText ?? t('tickets.due.met'),
  }[due.state]();
  return <DueDate state={looks[due.state]}>{text}</DueDate>;
}
