import type { TFunction } from 'i18next';
import type { ProjectOverview } from '../api/client';
import { namesText } from '../acceptances/acceptances';
import type { DueState } from '../components/DueDate';
import {
  type Dates,
  type DayValue,
  formatDayMonthText,
  formatShortWeekdayText,
} from '../format/dates';
import { dayWord } from '../my-work/myWork';

type Deadline = NonNullable<ProjectOverview['deadline']>;
type Milestone = NonNullable<ProjectOverview['nextMilestone']>;
type Attention = ProjectOverview['attention'];

/** A deadline in words, with the state of its icon and color (ADR-0065). */
export interface Due {
  state: DueState;
  text: string;
}

/**
 * The end of the schedule against the baseline (ADR-0021): after it, a warning, as the canvas ProjetoVisaoGeral shows
 * it; before or on it, on time.
 */
export function deviationDue(deadline: Deadline, t: TFunction): Due | undefined {
  const deviation = deadline.deviation;
  if (deviation === null) {
    return undefined;
  }
  if (deviation > 0) {
    return { state: 'soon', text: t('projects.overview.deadline.late', { count: deviation }) };
  }
  return deviation < 0
    ? { state: 'met', text: t('projects.overview.deadline.early', { count: -deviation }) }
    : { state: 'met', text: t('projects.overview.deadline.onTime') };
}

/** The day of the next milestone and the business days left, or how late it is. */
export function milestoneText(milestone: Milestone, t: TFunction): string {
  const day = formatDayMonthText(milestone.day);
  if (milestone.daysLate !== null) {
    return milestone.daysLate > 0
      ? t('projects.overview.milestone.late', { day, count: milestone.daysLate })
      : t('projects.overview.milestone.lateNoDays', { day });
  }
  return milestone.businessDaysLeft === 0
    ? t('projects.overview.milestone.today', { day })
    : t('projects.overview.milestone.left', { day, count: milestone.businessDaysLeft });
}

/** A line of "Precisa de atenção": what it is, where it leads, its detail and its deadline. */
export interface AttentionLine {
  key: string;
  kind: 'task' | 'deadline' | 'ticket' | 'unassigned' | 'acceptance' | 'vendor';
  title: string;
  path: string;
  meta: string;
  due: Due;
}

const late = (daysLate: number, day: DayValue, first: boolean, t: TFunction): string =>
  daysLate > 0
    ? t(first ? 'myWork.due.firstResponseLate' : 'myWork.due.late', { count: daysLate })
    : t(first ? 'myWork.due.firstResponseLateOn' : 'myWork.due.lateOn', {
        day: formatShortWeekdayText(day),
      });

/**
 * What needs attention in the project (ADR-0073), in the order of the API: the late tasks, the items past their
 * deadline (GanttA-2), the tickets, the pending requests of acceptance and the stale tickets at vendors.
 */
export function attentionLines(
  projectId: string,
  attention: Attention,
  today: DayValue,
  dates: Dates,
  t: TFunction,
): AttentionLine[] {
  const tasks = attention.tasks.map((task): AttentionLine => {
    const [first, ...others] = task.blocks;
    const owner =
      task.owner === null
        ? t('projects.overview.attention.taskNoOwner', { progress: task.progress })
        : t('projects.overview.attention.task', { name: task.owner.name, progress: task.progress });
    const blocks =
      first === undefined
        ? undefined
        : others.length === 0
          ? t('projects.overview.attention.blocks', { name: first })
          : t('projects.overview.attention.blocksMore', { name: first, count: others.length });
    return {
      key: `task-${task.id}`,
      kind: 'task',
      title: task.name,
      path: `/projects/${projectId}/schedule?item=${task.id}`,
      meta: blocks === undefined ? owner : `${owner} · ${blocks}`,
      due: { state: 'overdue', text: late(task.daysLate, task.end, false, t) },
    };
  });
  const deadlines = attention.deadlines.map((item): AttentionLine => ({
    key: `deadline-${item.id}`,
    kind: 'deadline',
    title: item.name,
    path: `/projects/${projectId}/schedule?item=${item.id}`,
    meta: t(`projects.overview.attention.deadline.${item.kind}`, {
      day: formatDayMonthText(item.deadline),
    }),
    due: {
      state: 'overdue',
      text: t('projects.overview.attention.deadlinePast', { count: item.daysPast }),
    },
  }));
  const tickets = attention.tickets.map((ticket): AttentionLine => {
    const team = t(`tickets.teams.${ticket.team}`);
    const first = ticket.deadline === 'FirstResponse';
    let due: Due;
    if (ticket.dueOn === null || ticket.dueState === null) {
      due = { state: 'paused', text: t('myWork.due.paused') };
    } else if (ticket.dueState === 'Overdue') {
      due = { state: 'overdue', text: late(ticket.daysLate ?? 0, ticket.dueOn, first, t) };
    } else {
      due = {
        state: ticket.dueState === 'Soon' ? 'soon' : 'onTrack',
        text: t(first ? 'myWork.due.firstResponse' : 'myWork.due.on', {
          day: dayWord(ticket.dueOn, today, t),
        }),
      };
    }
    return {
      key: `ticket-${String(ticket.number)}`,
      kind: ticket.assignee === null ? 'unassigned' : 'ticket',
      title: `#${String(ticket.number)} ${ticket.title}`,
      path: `/tickets/${String(ticket.number)}`,
      meta:
        ticket.assignee === null
          ? t('projects.overview.attention.ticketNoAssignee', { team })
          : t('projects.overview.attention.ticket', {
              team,
              name: ticket.assignee.name,
              count: ticket.interactions,
            }),
      due,
    };
  });
  const acceptances = attention.acceptances.map((term): AttentionLine => ({
    key: `acceptance-${term.id}`,
    kind: 'acceptance',
    title: term.title,
    path: `/projects/${projectId}/acceptances?term=${term.id}`,
    meta: t('projects.overview.attention.acceptance', {
      name: term.recipient?.name ?? namesText(term.signers),
      day: dates.formatDayMonth(term.requestedAt),
    }),
    due:
      term.milestoneDay === null
        ? {
            state: 'onTrack',
            text: t('projects.overview.attention.noMilestone', {
              day: dates.formatDayMonth(term.requestedAt),
            }),
          }
        : {
            state: term.milestoneDay < today ? 'overdue' : 'onTrack',
            text: t('projects.overview.attention.milestone', {
              day: formatDayMonthText(term.milestoneDay),
            }),
          },
  }));
  const vendors = attention.vendors.map((vendor): AttentionLine => ({
    key: `vendor-${String(vendor.ticketNumber)}-${vendor.code}`,
    kind: 'vendor',
    title: `${vendor.vendor} · ${vendor.code}`,
    path: `/tickets/${String(vendor.ticketNumber)}`,
    meta: t('projects.overview.attention.vendor', { number: vendor.ticketNumber }),
    due: {
      state: 'soon',
      text: t('projects.overview.attention.stale', { count: vendor.staleDays }),
    },
  }));
  return [...tasks, ...deadlines, ...tickets, ...acceptances, ...vendors];
}
