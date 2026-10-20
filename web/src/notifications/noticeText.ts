import type { TFunction } from 'i18next';
import type { components } from '../api/schema';
import { type Dates, daysBetween, formatDayText } from '../format/dates';

export type BellNotification = components['schemas']['NotificationResponse'];
export type NoticeKind = components['schemas']['NoticeKind'];

/** The key of a kind in the texts of the screens: TicketOpened is ticketOpened. */
function keyOf(kind: NoticeKind) {
  return `${kind.charAt(0).toLowerCase()}${kind.slice(1)}` as Uncapitalize<NoticeKind>;
}

/** The value as the screen shows it: a code in words (a team, a status, a role) and a day as 21/10/2026. */
function shown(name: string, value: string, t: TFunction): string {
  switch (name) {
    case 'team':
      return t(`tickets.teams.${value}`, { defaultValue: value });
    case 'from':
    case 'to':
      return t(`tickets.statuses.${value}`, { defaultValue: value });
    case 'vendorStatus':
      return t(`tickets.vendors.statuses.${value}`, { defaultValue: value }).toLowerCase();
    case 'role':
      return t(`projects.roles.${value}`, { defaultValue: value });
    case 'status':
      return t(`projects.statuses.${value}`, { defaultValue: value });
    case 'deadline':
      return t(`notifications.deadlines.${value}`, { defaultValue: value });
    default:
      return /^\d{4}-\d{2}-\d{2}$/.test(value) ? formatDayText(value) : value;
  }
}

/**
 * What a notification of the bell says (ADR-0038), from its kind and data, with who did it by the current name; the
 * system, when nobody did it, as the automatic closing.
 */
export function noticeText(notification: BellNotification, t: TFunction): string {
  const value = (name: string) => {
    const raw = notification.values[name];
    return raw === undefined ? '' : shown(name, raw, t);
  };
  const values = {
    actor: notification.actor ?? t('notifications.system'),
    number: value('number'),
    project: value('project'),
    team: value('team'),
    from: value('from'),
    to: value('to'),
    deadline: value('deadline'),
    dueOn: value('dueOn'),
    vendor: value('vendor'),
    vendorStatus: value('vendorStatus'),
    role: value('role'),
    status: value('status'),
    item: value('item'),
    start: value('start'),
    end: value('end'),
  };
  if (notification.kind === 'TicketStatusChanged' && 'closedWithProject' in notification.values) {
    return t('notifications.kinds.ticketClosedWithProject', values);
  }
  return t(`notifications.kinds.${keyOf(notification.kind)}`, values);
}

/**
 * When a notification arrived, as the bell says it: "há 12 min" and "há 3 h" today, "ontem, 17:40", or "16/10, 09:05",
 * in the time zone of the installation (ADR-0013).
 */
export function noticeWhen(
  createdAt: string,
  dates: Dates,
  t: TFunction,
  now = new Date(),
): string {
  const moment = new Date(createdAt);
  const minutes = Math.floor((now.getTime() - moment.getTime()) / 60_000);
  const days = daysBetween(dates.toDayValue(moment), dates.toDayValue(now));
  if (days === 0 && minutes < 1) {
    return t('notifications.when.now');
  }
  if (days === 0 && minutes < 60) {
    return t('notifications.when.minutes', { count: minutes });
  }
  if (days === 0) {
    return t('notifications.when.hours', { count: Math.floor(minutes / 60) });
  }
  if (days === 1) {
    return t('notifications.when.yesterday', { time: dates.formatTime(createdAt) });
  }
  return t('notifications.when.before', {
    day: dates.formatDayMonth(createdAt),
    time: dates.formatTime(createdAt),
  });
}

/** The group of a notification in the panel: Hoje, Ontem, or the day (16/10). */
export function noticeGroup(
  createdAt: string,
  dates: Dates,
  t: TFunction,
  now = new Date(),
): string {
  const days = daysBetween(dates.toDayValue(new Date(createdAt)), dates.toDayValue(now));
  if (days === 0) {
    return t('notifications.groups.today');
  }
  return days === 1 ? t('notifications.groups.yesterday') : dates.formatDayMonth(createdAt);
}
