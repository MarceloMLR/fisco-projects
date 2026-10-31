import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { myProjectsQuery, upcomingMilestonesQuery } from '../api/queries';
import { Card, CodeChip } from '../components/Card';
import { DueDate } from '../components/DueDate';
import { ScheduleStrip } from '../components/ScheduleStrip';
import { formatDayOfMonthText, formatDayText, formatShortMonthText } from '../format/dates';
import { projectDue } from './myWork';
import { Button } from '../components/Button';

const listSx = { margin: 0, padding: 0, listStyle: 'none' } as const;

const itemSx = {
  borderBottom: '1px solid var(--border)',
  '&:last-of-type': { borderBottom: 0 },
} as const;

const smallSx = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

const emptySx = { padding: 'var(--space-4)', color: 'var(--text-secondary)' } as const;

/**
 * Meus projetos (ADR-0070, canvas Main): the open projects the person is in, as the side bar lists them, with the
 * strip of phases (ADR-0071), the progress and the delay of the most late item (ADR-0096).
 */
export function MyProjectsCard({ fill = false }: { fill?: boolean }) {
  const { t } = useTranslation();
  const { data } = useQuery(myProjectsQuery());
  const projects = data?.items ?? [];
  const today = data?.today ?? '';

  return (
    <Card
      title={t('myWork.projects.title')}
      count={data?.total}
      shrink={fill}
      actions={
        <Button component={Link} to="/projects" variant="text" size="small">
          {t('myWork.projects.all')}
        </Button>
      }
    >
      {data !== undefined && projects.length === 0 ? (
        <Box sx={emptySx}>{t('myWork.projects.empty')}</Box>
      ) : (
        <Box component="ul" sx={listSx}>
          {projects.map((project) => {
            const schedule = project.schedule;
            const state = projectDue(schedule, today, t);
            return (
              <Box
                component="li"
                key={project.id}
                sx={{
                  ...itemSx,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 'var(--space-2)',
                  padding: '12px var(--space-4)',
                }}
              >
                <Box
                  sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}
                >
                  <Box
                    component={Link}
                    to={`/projects/${project.id}`}
                    sx={{
                      flex: 1,
                      minWidth: 0,
                      fontWeight: 500,
                      color: 'var(--text)',
                      textDecoration: 'none',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      '&:hover': { textDecoration: 'underline' },
                    }}
                  >
                    {project.name}
                  </Box>
                  <CodeChip code={project.code} />
                </Box>
                {schedule !== null && (
                  <ScheduleStrip
                    start={schedule.start}
                    end={schedule.end}
                    phases={schedule.phases}
                    milestones={schedule.milestones}
                    today={today}
                    label={t('myWork.projects.strip', {
                      start: formatDayText(schedule.start),
                      end: formatDayText(schedule.end),
                    })}
                  />
                )}
                <Box
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: 'var(--space-2)',
                    flexWrap: 'wrap',
                  }}
                >
                  <Box component="span" sx={{ ...smallSx, fontVariantNumeric: 'tabular-nums' }}>
                    {schedule === null
                      ? t('myWork.projects.noSchedule')
                      : t('myWork.projects.progress', { progress: schedule.progress })}
                  </Box>
                  <DueDate state={state.state}>{state.text}</DueDate>
                </Box>
              </Box>
            );
          })}
        </Box>
      )}
    </Card>
  );
}

/**
 * Próximos marcos (ADR-0070, canvas Main): the next milestones of the projects of the person, with the day in a box,
 * the business days left and whether its acceptance was asked for (ADR-0096).
 */
export function UpcomingMilestonesCard({ fill = false }: { fill?: boolean }) {
  const { t } = useTranslation();
  const { data } = useQuery(upcomingMilestonesQuery());
  const milestones = data?.items ?? [];

  return (
    <Card title={t('myWork.milestones.title')} fill={fill}>
      {data !== undefined && milestones.length === 0 ? (
        <Box sx={emptySx}>{t('myWork.milestones.empty')}</Box>
      ) : (
        <Box component="ul" sx={listSx}>
          {milestones.map((milestone) => {
            const left =
              milestone.businessDaysLeft === 0
                ? t('myWork.milestones.today')
                : t('myWork.milestones.left', { count: milestone.businessDaysLeft });
            return (
              <Box
                component="li"
                key={milestone.id}
                sx={{
                  ...itemSx,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-3)',
                  minHeight: 52,
                  padding: 'var(--space-2) var(--space-4)',
                }}
              >
                <Box
                  aria-label={formatDayText(milestone.day)}
                  role="img"
                  sx={{
                    display: 'inline-flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flex: 'none',
                    boxSizing: 'border-box',
                    width: 44,
                    height: 48,
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-md)',
                    backgroundColor: 'var(--bg-surface)',
                  }}
                >
                  <Box
                    component="b"
                    aria-hidden
                    sx={{
                      fontSize: 17,
                      lineHeight: '20px',
                      fontWeight: 500,
                      fontVariantNumeric: 'tabular-nums',
                    }}
                  >
                    {formatDayOfMonthText(milestone.day)}
                  </Box>
                  <Box
                    component="span"
                    aria-hidden
                    sx={{ fontSize: 11, lineHeight: '14px', color: 'var(--text-secondary)' }}
                  >
                    {formatShortMonthText(milestone.day)}
                  </Box>
                </Box>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Box
                    sx={{
                      fontWeight: 500,
                      color: 'var(--text)',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}
                  >
                    {milestone.name}
                  </Box>
                  <Box sx={smallSx}>
                    {milestone.acceptanceRequested
                      ? t('myWork.milestones.requested', { left })
                      : left}
                  </Box>
                </Box>
                <CodeChip code={milestone.project.code} />
              </Box>
            );
          })}
        </Box>
      )}
    </Card>
  );
}
