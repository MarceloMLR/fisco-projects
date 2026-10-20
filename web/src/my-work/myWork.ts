import type { TFunction } from 'i18next';
import type { MyWorkGroup, MyWorkItem, ProjectScheduleSummary } from '../api/client';
import type { DueState } from '../components/DueDate';
import {
  type Dates,
  type DayValue,
  daysBetween,
  formatDayMonthText,
  formatShortWeekdayText,
} from '../format/dates';

/**
 * The name of a group of the queue (ADR-0070): "Hoje e amanhã", or "Hoje e segunda" when the next business day is not
 * tomorrow, as on a Friday or the eve of a holiday.
 */
export function groupLabel(
  group: MyWorkGroup,
  today: DayValue,
  nextBusinessDay: DayValue,
  t: TFunction,
): string {
  if (group !== 'Soon') {
    return t(`myWork.groups.${group}`);
  }
  return daysBetween(today, nextBusinessDay) === 1
    ? t('myWork.groups.SoonTomorrow')
    : t('myWork.groups.Soon', { weekday: formatShortWeekdayText(nextBusinessDay) });
}

/** Where the title of a line leads: the ticket, the details of the task in the schedule or the drawer of the term. */
export function itemPath(item: MyWorkItem): string {
  if (item.ticket !== null) {
    return `/tickets/${String(item.ticket.number)}`;
  }
  return item.kind === 'Task'
    ? `/projects/${item.project.id}/schedule?item=${item.id}`
    : `/projects/${item.project.id}/acceptances?term=${item.id}`;
}

/** The title of a line: a ticket with "#" and its number before the title, as on the page of the ticket. */
export function itemTitle(item: MyWorkItem): string {
  return item.ticket === null ? item.title : `#${String(item.ticket.number)} ${item.title}`;
}

const looks: Record<MyWorkGroup, DueState> = {
  Late: 'overdue',
  Waiting: 'paused',
  Soon: 'soon',
  Week: 'onTrack',
};

/**
 * A day of the window of the queue in a word: "hoje", "amanhã", the day of the week up to 6 days ahead ("quarta") or
 * the day and the month (21/10).
 */
function dayWord(day: DayValue, today: DayValue, t: TFunction): string {
  const days = daysBetween(today, day);
  if (days === 0) {
    return t('myWork.days.today');
  }
  if (days === 1) {
    return t('myWork.days.tomorrow');
  }
  return days > 1 && days < 7 ? formatShortWeekdayText(day) : formatDayMonthText(day);
}

/**
 * The deadline of a line, with the state of its icon and color (ADR-0065): late by business days, due on a day, the
 * first answer of a ticket, the paused deadline of a ticket waiting for the person, the day a request was made for the
 * person, and the milestone of a request the person made.
 */
export function dueOf(
  item: MyWorkItem,
  group: MyWorkGroup,
  today: DayValue,
  dates: Dates,
  t: TFunction,
): { state: DueState; text: string } {
  const acceptance = item.acceptance;
  if (acceptance?.forYou === true) {
    return {
      state: 'onTrack',
      text: t('myWork.due.requested', { day: dates.formatDayMonth(acceptance.requestedAt) }),
    };
  }
  if (group === 'Waiting' || item.dueOn === null) {
    return { state: 'paused', text: t('myWork.due.paused') };
  }
  const state = looks[group];
  const due = item.dueOn;
  if (acceptance !== null) {
    const days = daysBetween(today, due);
    return {
      state,
      text:
        days === 0
          ? t('myWork.due.milestoneToday')
          : days === 1
            ? t('myWork.due.milestoneTomorrow')
            : t('myWork.due.milestone', { day: formatDayMonthText(due) }),
    };
  }
  const first = item.ticket?.deadline === 'FirstResponse';
  if (group === 'Late') {
    const daysLate = item.daysLate ?? 0;
    return {
      state,
      text:
        daysLate > 0
          ? t(first ? 'myWork.due.firstResponseLate' : 'myWork.due.late', { count: daysLate })
          : t(first ? 'myWork.due.firstResponseLateOn' : 'myWork.due.lateOn', {
              day: formatShortWeekdayText(due),
            }),
    };
  }
  return {
    state,
    text: t(first ? 'myWork.due.firstResponse' : 'myWork.due.on', {
      day: dayWord(due, today, t),
    }),
  };
}

/** Since when a ticket waits for the person: "há 12 min" and "há 2 h" today, "desde 16/10" before. */
function waitingFor(since: string, dates: Dates, t: TFunction, now: Date): string {
  const minutes = Math.max(1, Math.floor((now.getTime() - new Date(since).getTime()) / 60_000));
  const days = daysBetween(dates.toDayValue(new Date(since)), dates.toDayValue(now));
  if (days > 0) {
    return t('myWork.waiting.since', { day: dates.formatDayMonth(since) });
  }
  return minutes < 60
    ? t('myWork.waiting.minutes', { count: minutes })
    : t('myWork.waiting.hours', { count: Math.floor(minutes / 60) });
}

/**
 * The detail of a line, under the title: the phase and the progress of a task; who is in charge of a ticket, who waits
 * for the answer of the person or that nobody took it yet; who asked for a term, or to whom the person asked it.
 */
export function metaText(
  item: MyWorkItem,
  userId: string,
  dates: Dates,
  t: TFunction,
  now = new Date(),
): string {
  if (item.task !== null) {
    return item.task.progress === 0
      ? t('myWork.meta.taskNotStarted', { phase: item.task.phase })
      : t('myWork.meta.task', { phase: item.task.phase, progress: item.task.progress });
  }
  if (item.ticket !== null) {
    const { assignee, interactions, team } = item.ticket;
    if (item.waitingSince !== null) {
      return t('myWork.meta.waiting', {
        name: assignee?.name ?? t('myWork.meta.attendant'),
        when: waitingFor(item.waitingSince, dates, t, now),
      });
    }
    if (assignee === null) {
      return t('myWork.meta.unassigned', { team: t(`tickets.teams.${team}`) });
    }
    return assignee.id === userId
      ? t('myWork.meta.yours', { count: interactions })
      : t('myWork.meta.assignee', { name: assignee.name, count: interactions });
  }
  if (item.acceptance !== null) {
    return item.acceptance.forYou
      ? t('myWork.meta.acceptanceFor', { name: item.acceptance.requestedBy.name })
      : t('myWork.meta.acceptanceAsked', {
          name: item.acceptance.recipient.name,
          day: dates.formatDayMonth(item.acceptance.requestedAt),
        });
  }
  return '';
}

/**
 * Where a project stands in Meu trabalho (ADR-0096): without a schedule, no date; concluded on a day, starting on a
 * day, late by the business days of its most late item, or on track.
 */
export function projectDue(
  schedule: ProjectScheduleSummary | null,
  today: DayValue,
  t: TFunction,
): { state: DueState; text: string } {
  if (schedule === null) {
    return { state: 'paused', text: t('myWork.projects.noDate') };
  }
  if (schedule.completedOn !== null) {
    return {
      state: 'met',
      text: t('myWork.projects.completed', { day: formatDayMonthText(schedule.completedOn) }),
    };
  }
  if (schedule.start > today) {
    return {
      state: 'onTrack',
      text: t('myWork.projects.starts', { day: formatDayMonthText(schedule.start) }),
    };
  }
  if (schedule.lateDays > 0) {
    return { state: 'overdue', text: t('myWork.projects.late', { count: schedule.lateDays }) };
  }
  if (schedule.lateTasks + schedule.lateMilestones > 0) {
    return { state: 'overdue', text: t('myWork.projects.lateNoDays') };
  }
  return { state: 'onTrack', text: t('myWork.projects.onTrack') };
}
