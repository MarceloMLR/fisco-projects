import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { ScheduleItem } from '../api/client';
import { DueDate, type DueState } from '../components/DueDate';
import { formatDayMonthText, formatDayText } from '../format/dates';
import {
  deadlineState,
  deadlineText,
  type MilestoneStatus as Status,
  milestoneStatus,
} from './schedule';

const dueStates = { reached: 'met', late: 'overdue', pending: 'onTrack' } as const satisfies Record<
  Status,
  DueState
>;

/**
 * The state of a milestone by icon, color and text (ADR-0065, ADR-0086): "Atingido em 09/10", "Pendente" or
 * "Atrasado"; with the year in the details.
 */
export function MilestoneStatus({
  item,
  withYear = false,
}: {
  item: ScheduleItem;
  withYear?: boolean;
}) {
  const { t } = useTranslation();
  const status = milestoneStatus(item);
  return (
    <DueDate state={dueStates[status]}>
      {status === 'reached' && item.reachedOn !== null
        ? t('projects.schedule.milestone.reached', {
            day: withYear ? formatDayText(item.reachedOn) : formatDayMonthText(item.reachedOn),
          })
        : t(`projects.schedule.milestone.${status === 'late' ? 'late' : 'pending'}`)}
    </DueDate>
  );
}

/**
 * How the deadline of a task or milestone is going, by icon, color and text (GanttA-2, ADR-0065): "Passou 1 dia útil
 * da data-limite" while it is missed, or how far before it the item ends; nothing without a deadline.
 */
export function DeadlineStatus({ item }: { item: ScheduleItem }) {
  const { t } = useTranslation();
  const state = deadlineState(item);
  return state === undefined ? null : <DueDate state={state}>{deadlineText(item, t)}</DueDate>;
}

/** How many open tasks and milestones went past their deadline, when one did: "1 data-limite estourada". */
export function MissedDeadlines({ count }: { count: number }) {
  const { t } = useTranslation();
  return count === 0 ? null : (
    <DueDate state="overdue">{t('projects.schedule.missedDeadlines', { count })}</DueDate>
  );
}

/**
 * How many tasks and milestones are late, each when there is one (ADR-0020, ADR-0086): "2 tarefas atrasadas" and
 * "1 marco atrasado", side by side or one under the other.
 */
export function LateCounts({
  tasks,
  milestones,
  stacked = false,
}: {
  tasks: number;
  milestones: number;
  stacked?: boolean;
}) {
  const { t } = useTranslation();
  if (tasks === 0 && milestones === 0) {
    return null;
  }
  return (
    <Box
      component="span"
      sx={{
        display: 'flex',
        flexDirection: stacked ? 'column' : 'row',
        alignItems: 'flex-start',
        flexWrap: 'wrap',
        gap: stacked ? 0 : 'var(--space-2) var(--space-4)',
      }}
    >
      {tasks > 0 && (
        <DueDate state="overdue">{t('projects.schedule.lateTasks', { count: tasks })}</DueDate>
      )}
      {milestones > 0 && (
        <DueDate state="overdue">
          {t('projects.schedule.lateMilestones', { count: milestones })}
        </DueDate>
      )}
    </Box>
  );
}
