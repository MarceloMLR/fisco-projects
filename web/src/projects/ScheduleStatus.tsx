import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { ScheduleItem } from '../api/client';
import { DueDate, type DueState } from '../components/DueDate';
import { formatDayMonthText, formatDayText } from '../format/dates';
import { type MilestoneStatus as Status, milestoneStatus } from './schedule';

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
