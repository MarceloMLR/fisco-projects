import EventBusyOutlined from '@mui/icons-material/EventBusyOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { PersonLoad, PersonWeek, TeamLoadWeek } from '../api/client';
import { CodeChip } from '../components/Card';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { StatusBadge } from '../components/StatusBadge';
import { formatHours } from '../hours/hours';
import {
  absencePeriodText,
  loadLook,
  loadText,
  taskPeriodText,
  weekHoursText,
  weekText,
} from './teamLoad';

const sectionTitleSx = {
  margin: 0,
  fontSize: 14,
  lineHeight: '20px',
  fontWeight: 500,
  color: 'var(--text)',
} as const;
const secondarySx = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

/** A week of the person: the state and the load, the absences and the tasks that weigh on it, the most hours first. */
function WeekSection({ week, person }: { week: TeamLoadWeek; person: PersonWeek }) {
  const { t } = useTranslation();
  const { tone, icon } = loadLook[person.state];
  const headingId = `team-load-week-${week.start}`;
  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        paddingBottom: 'var(--space-4)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-3)',
          flexWrap: 'wrap',
        }}
      >
        <Box component="h3" id={headingId} sx={sectionTitleSx}>
          {t('teamLoad.details.week', { period: weekText(week) })}
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <Box component="span" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
            {loadText(person, t)}
          </Box>
          <StatusBadge tone={tone} icon={icon}>
            {t(`teamLoad.states.${person.state}`)}
          </StatusBadge>
        </Box>
      </Box>
      {person.absences.length > 0 && (
        <Box
          component="p"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-1)',
            margin: 0,
            ...secondarySx,
            fontSize: 13,
            lineHeight: '18px',
          }}
        >
          <EventBusyOutlined aria-hidden sx={{ fontSize: 16 }} />
          {t('teamLoad.details.away', {
            periods: person.absences.map((absence) => absencePeriodText(absence, t)).join(', '),
          })}
          {' · '}
          {t('teamLoad.absenceDays', { count: person.absenceDays })}
        </Box>
      )}
      {person.tasks.length === 0 ? (
        <Box component="span" sx={{ ...secondarySx, fontSize: 13 }}>
          {t('teamLoad.details.noTasks')}
        </Box>
      ) : (
        <Box
          component="ul"
          aria-labelledby={headingId}
          sx={{
            listStyle: 'none',
            margin: 0,
            padding: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-2)',
          }}
        >
          {person.tasks.map((task) => (
            <Box
              component="li"
              key={task.itemId}
              sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-2)' }}
            >
              <CodeChip code={task.projectCode} />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box
                  component={Link}
                  to={`/projects/${task.projectId}/schedule?view=list&item=${task.itemId}`}
                  aria-label={t('teamLoad.details.openTask', {
                    name: task.name,
                    code: task.projectCode,
                  })}
                  sx={{
                    display: 'block',
                    fontSize: 14,
                    lineHeight: '20px',
                    fontWeight: 500,
                    color: 'var(--text)',
                    textDecoration: 'none',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                    '&:hover': { textDecoration: 'underline' },
                  }}
                >
                  {task.name}
                </Box>
                <Box component="span" sx={{ ...secondarySx, display: 'block' }}>
                  {t('teamLoad.details.taskDetail', {
                    planned: formatHours(task.planned),
                    period: taskPeriodText(task, t),
                  })}
                </Box>
              </Box>
              <Box
                component="span"
                sx={{
                  flex: 'none',
                  fontSize: 14,
                  lineHeight: '20px',
                  fontVariantNumeric: 'tabular-nums',
                }}
              >
                {t('teamLoad.details.taskHours', { hours: weekHoursText(task.hours) })}
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
}

/**
 * The load of a person in the drawer (ADR-0081, ADR-0142): the name and the e-mail, then each week with the load, the
 * state, the absences and the tasks that weigh on it, each one leading to the schedule of its project with its
 * details open.
 */
export function TeamLoadDrawer({
  person,
  open,
  weeks,
  onClose,
}: {
  person: PersonLoad | undefined;
  open: boolean;
  weeks: readonly TeamLoadWeek[];
  onClose: () => void;
}) {
  const { t } = useTranslation();
  return (
    <DetailsDrawer open={open} onClose={onClose} label={t('teamLoad.details.label')}>
      {person === undefined ? (
        <Box component="span" sx={{ ...secondarySx, fontSize: 13 }}>
          {t('teamLoad.details.notFound')}
        </Box>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
          <Box>
            <Box
              component="h2"
              sx={{ margin: 0, fontSize: 20, lineHeight: '28px', fontWeight: 500 }}
            >
              {person.name}
            </Box>
            <Box component="span" sx={{ ...secondarySx, fontSize: 13 }}>
              {person.email}
            </Box>
          </Box>
          {weeks.map((week, index) => {
            const personWeek = person.weeks[index];
            return personWeek === undefined ? null : (
              <WeekSection key={week.start} week={week} person={personWeek} />
            );
          })}
        </Box>
      )}
    </DetailsDrawer>
  );
}
