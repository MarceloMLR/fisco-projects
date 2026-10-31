import AddOutlined from '@mui/icons-material/AddOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { Schedule, ScheduleItem } from '../api/client';
import { DueDate } from '../components/DueDate';
import { itemsOf, lateText, showsDelay } from './schedule';
import { DetailsSection } from './ScheduleItemProgress';
import { Button } from '../components/Button';

/**
 * The Subtarefas of a task right in the phase, in its details (ADR-0088): each subtask, which opens its own details,
 * with its delay when it is late, and, for who edits the schedule, "Nova subtarefa".
 */
export function SubtaskSection({
  item,
  schedule,
  onOpen,
  onNew,
}: {
  item: ScheduleItem;
  schedule: Schedule;
  /** Opens the details of a subtask. */
  onOpen: (itemId: string) => void;
  /** "Nova subtarefa"; none for who does not edit the schedule. */
  onNew?: () => void;
}) {
  const { t } = useTranslation();
  const subtasks = itemsOf(schedule).filter((other) => other.parentId === item.id);
  return (
    <DetailsSection title={t('projects.schedule.details.subtasks')}>
      {subtasks.length > 0 && (
        <Box
          component="ul"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignSelf: 'stretch',
            gap: 'var(--space-1)',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          {subtasks.map((subtask) => (
            <Box
              component="li"
              key={subtask.id}
              sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}
            >
              <Button
                variant="text"
                size="small"
                startIcon={
                  subtask.kind === 'Milestone' ? (
                    <FlagOutlined titleAccess={t('projects.schedule.kinds.Milestone')} />
                  ) : undefined
                }
                onClick={() => {
                  onOpen(subtask.id);
                }}
                sx={{ minWidth: 0, justifyContent: 'flex-start', textAlign: 'left' }}
              >
                <Box
                  component="span"
                  sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                >
                  {subtask.name}
                </Box>
              </Button>
              {showsDelay(subtask) && <DueDate state="overdue">{lateText(subtask, t)}</DueDate>}
            </Box>
          ))}
        </Box>
      )}
      {onNew !== undefined && (
        <Button variant="outlined" size="small" startIcon={<AddOutlined />} onClick={onNew}>
          {t('projects.schedule.form.newSubtask')}
        </Button>
      )}
    </DetailsSection>
  );
}
