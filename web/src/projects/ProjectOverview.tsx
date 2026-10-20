import ArrowForwardOutlined from '@mui/icons-material/ArrowForwardOutlined';
import BusinessOutlined from '@mui/icons-material/BusinessOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import InboxOutlined from '@mui/icons-material/InboxOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { OverviewTeam, ProjectOverview as Overview } from '../api/client';
import { projectOverviewQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { Card } from '../components/Card';
import { DueDate } from '../components/DueDate';
import { formatDayMonthText } from '../format/dates';
import { useDates } from '../format/useDates';
import { theme } from '../theme/theme';
import { type AttentionLine, attentionLines, deviationDue, milestoneText } from './overview';
import { OverviewTimeline } from './OverviewTimeline';
import { useProjectOutlet } from './projectOutlet';

const listSx = { margin: 0, padding: 0, listStyle: 'none' } as const;

const itemSx = {
  borderBottom: '1px solid var(--border)',
  '&:last-of-type': { borderBottom: 0 },
} as const;

const metaSx = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

const emptySx = { padding: 'var(--space-4)', color: 'var(--text-secondary)' } as const;

/** A number of the strip of the overview (fp-kpi): its label, its value and what goes under it. */
function Kpi({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: '6px',
        minWidth: 0,
        padding: '14px var(--space-5)',
        borderLeft: '1px solid var(--border)',
        '&:first-of-type': { borderLeft: 0 },
        '@media (max-width: 1100px)': {
          '&:nth-of-type(3)': { borderLeft: 0 },
          '&:nth-of-type(n+3)': { borderTop: '1px solid var(--border)' },
        },
        [theme.breakpoints.down('sm')]: { padding: '12px var(--space-4)' },
      }}
    >
      <Box
        component="span"
        sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
      >
        {label}
      </Box>
      {children}
    </Box>
  );
}

const valueSx = {
  display: 'flex',
  alignItems: 'baseline',
  flexWrap: 'wrap',
  columnGap: 'var(--space-2)',
  fontSize: 26,
  lineHeight: '32px',
  fontWeight: 500,
  letterSpacing: '-0.01em',
  color: 'var(--text)',
  fontVariantNumeric: 'tabular-nums',
  '& small': {
    fontSize: 14,
    lineHeight: '20px',
    fontWeight: 400,
    color: 'var(--text-secondary)',
    letterSpacing: 0,
  },
} as const;

/** A value in words, such as the name of the next milestone or "Sem cronograma", smaller than a number. */
const wordsSx = {
  ...valueSx,
  display: 'block',
  fontSize: 20,
  lineHeight: '34px',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

const footSx = {
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  flexWrap: 'wrap',
  fontSize: 13,
  lineHeight: '18px',
  color: 'var(--text-secondary)',
  fontVariantNumeric: 'tabular-nums',
  // A long deadline breaks the line in a narrow number, as on the phone, instead of running out of it.
  '& > [data-state]': { whiteSpace: 'normal', minWidth: 0 },
} as const;

/** The progress against the planned for today (fp-planbar): the part done in green and the mark of the planned. */
function PlanBar({ actual, planned }: { actual: number; planned: number | null }) {
  const { t } = useTranslation();
  return (
    <Box
      role="img"
      aria-label={
        planned === null
          ? t('projects.overview.progress.barNoPlan', { actual })
          : t('projects.overview.progress.bar', { actual, planned })
      }
      sx={{
        position: 'relative',
        height: 8,
        borderRadius: 'var(--radius-sm)',
        backgroundColor: 'var(--progress-track)',
      }}
    >
      <Box
        component="span"
        sx={{
          position: 'absolute',
          left: 0,
          top: 0,
          bottom: 0,
          width: `${String(actual)}%`,
          borderRadius: 'var(--radius-sm)',
          backgroundColor: 'var(--success)',
        }}
      />
      {planned !== null && (
        <Box
          component="span"
          data-planned={planned}
          sx={{
            position: 'absolute',
            top: '-4px',
            bottom: '-4px',
            left: `${String(planned)}%`,
            width: 2,
            marginLeft: '-1px',
            borderRadius: '1px',
            backgroundColor: 'var(--text)',
          }}
        />
      )}
    </Box>
  );
}

/** The four numbers (fp-kpis): the progress, the final deadline, the tickets and the next milestone (ADR-0073). */
function Numbers({ overview }: { overview: Overview }) {
  const { t } = useTranslation();
  const { progress, deadline, tickets, nextMilestone } = overview;
  const deviation = deadline === null ? undefined : deviationDue(deadline, t);
  const noSchedule = (
    <Box component="span" sx={wordsSx}>
      {t('projects.overview.noSchedule')}
    </Box>
  );

  return (
    <Box
      component="section"
      aria-label={t('projects.overview.summary')}
      sx={{
        flex: 'none',
        display: 'grid',
        gridTemplateColumns: 'repeat(4, minmax(0, 1fr))',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        '@media (max-width: 1100px)': { gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' },
      }}
    >
      <Kpi label={t('projects.overview.progress.label')}>
        {progress === null ? (
          noSchedule
        ) : (
          <>
            <Box component="span" sx={valueSx}>
              {`${String(progress.actual)}%`}
              {progress.planned !== null && (
                <small>
                  {t('projects.overview.progress.planned', { planned: progress.planned })}
                </small>
              )}
            </Box>
            <PlanBar actual={progress.actual} planned={progress.planned} />
          </>
        )}
      </Kpi>
      <Kpi label={t('projects.overview.deadline.label')}>
        {deadline === null ? (
          noSchedule
        ) : (
          <>
            <Box component="span" sx={valueSx}>
              {formatDayMonthText(deadline.end)}
              <small>{deadline.end.slice(0, 4)}</small>
            </Box>
            <Box component="span" sx={footSx}>
              {deviation !== undefined && (
                <DueDate state={deviation.state}>{deviation.text}</DueDate>
              )}
              <span>
                {deadline.baselineEnd === null
                  ? t('projects.overview.deadline.noBaseline')
                  : t('projects.overview.deadline.baseline', {
                      day: formatDayMonthText(deadline.baselineEnd),
                    })}
              </span>
            </Box>
          </>
        )}
      </Kpi>
      <Kpi label={t('projects.overview.tickets.label')}>
        <Box component="span" sx={valueSx}>
          {String(tickets.open)}
          <small>{t('projects.overview.tickets.of', { total: tickets.total })}</small>
        </Box>
        <Box component="span" sx={footSx}>
          {tickets.overdue > 0 ? (
            <DueDate state="overdue">
              {t('projects.overview.tickets.overdue', { count: tickets.overdue })}
            </DueDate>
          ) : (
            <DueDate state="met">{t('projects.overview.tickets.noneOverdue')}</DueDate>
          )}
          {tickets.waitingForRequester > 0 && (
            <span>
              {t('projects.overview.tickets.waiting', { count: tickets.waitingForRequester })}
            </span>
          )}
        </Box>
      </Kpi>
      <Kpi label={t('projects.overview.milestone.label')}>
        {progress === null ? (
          noSchedule
        ) : nextMilestone === null ? (
          <Box component="span" sx={{ ...wordsSx, color: 'var(--text-secondary)' }}>
            {t('projects.overview.milestone.none')}
          </Box>
        ) : (
          <>
            <Box component="span" title={nextMilestone.name} sx={wordsSx}>
              {nextMilestone.name}
            </Box>
            <Box component="span" sx={footSx}>
              {nextMilestone.daysLate === null ? (
                milestoneText(nextMilestone, t)
              ) : (
                <DueDate state="overdue">{milestoneText(nextMilestone, t)}</DueDate>
              )}
            </Box>
          </>
        )}
      </Kpi>
    </Box>
  );
}

const icons = {
  task: ViewTimelineOutlined,
  ticket: ConfirmationNumberOutlined,
  unassigned: InboxOutlined,
  acceptance: TaskAltOutlined,
  vendor: BusinessOutlined,
} as const;

function AttentionItem({ line }: { line: AttentionLine }) {
  const Icon = icons[line.kind];
  return (
    <Box
      component="li"
      sx={{
        ...itemSx,
        display: 'flex',
        alignItems: 'flex-start',
        gap: '12px',
        padding: '12px var(--space-4)',
      }}
    >
      <Box
        component="span"
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          flex: 'none',
          width: 28,
          height: 28,
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'var(--bg-subtle)',
          color: 'var(--text-secondary)',
        }}
      >
        <Icon aria-hidden sx={{ fontSize: 18 }} />
      </Box>
      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <Box
          component={Link}
          to={line.path}
          sx={{
            fontWeight: 500,
            color: 'var(--text)',
            textDecoration: 'none',
            overflowWrap: 'anywhere',
            '&:hover': { color: 'var(--action-text)', textDecoration: 'underline' },
          }}
        >
          {line.title}
        </Box>
        <Box sx={metaSx}>{line.meta}</Box>
        <span>
          <DueDate state={line.due.state}>{line.due.text}</DueDate>
        </span>
      </Box>
    </Box>
  );
}

/** Precisa de atenção (ADR-0073): the late tasks, the tickets, the pending terms and the stale tickets at vendors. */
function AttentionCard({ overview, fill }: { overview: Overview; fill: boolean }) {
  const { t } = useTranslation();
  const dates = useDates();
  const { project } = useProjectOutlet();
  const lines = attentionLines(project.id, overview.attention, overview.today, dates, t);
  return (
    <Card title={t('projects.overview.attention.title')} count={lines.length} fill={fill}>
      {lines.length === 0 ? (
        <Box sx={emptySx}>{t('projects.overview.attention.empty')}</Box>
      ) : (
        <Box component="ul" sx={listSx}>
          {lines.map((line) => (
            <AttentionItem key={line.key} line={line} />
          ))}
        </Box>
      )}
    </Card>
  );
}

/** The color of a person in the team, by the role in the project (avatar-*, ADR-0069). */
const toneOf = {
  Manager: 'manager',
  TechLead: 'techlead',
  Developer: 'dev',
  Contributor: 'functional',
  Viewer: undefined,
} as const;

const teamKeys = {
  Management: 'management',
  Functional: 'functional',
  Development: 'development',
} as const;

/** Equipe (ADR-0068): Gestão, Funcional and Desenvolvimento, with the people and the open tickets of each team. */
function TeamCard({ teams, fill }: { teams: OverviewTeam[]; fill: boolean }) {
  const { t } = useTranslation();
  const { project } = useProjectOutlet();
  return (
    <Card
      title={t('projects.overview.team')}
      fill={fill}
      actions={
        <Button component={Link} to={`/projects/${project.id}/members`} variant="text" size="small">
          {t('projects.overview.members')}
        </Button>
      }
    >
      <Box component="ul" sx={listSx}>
        {teams.map((team) => {
          const people = t('projects.overview.people', { count: team.people.length });
          return (
            <Box
              component="li"
              key={team.team}
              sx={{
                ...itemSx,
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-3)',
                padding: '10px var(--space-4)',
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ fontWeight: 500, color: 'var(--text)' }}>
                  {t(`projects.overview.teams.${teamKeys[team.team]}`)}
                </Box>
                <Box sx={metaSx}>
                  {team.team === 'Management'
                    ? t('projects.overview.managementMeta')
                    : t('projects.overview.teamMeta', {
                        people,
                        tickets: t('projects.overview.openTickets', {
                          count: team.openTickets ?? 0,
                        }),
                      })}
                </Box>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', flex: 'none' }}>
                {team.people.map((person, index) => (
                  <Box
                    key={person.id}
                    component="span"
                    title={person.name}
                    sx={{
                      display: 'inline-flex',
                      marginLeft: index === 0 ? 0 : '-6px',
                      borderRadius: 'var(--radius-full)',
                      boxShadow: '0 0 0 2px var(--bg-surface)',
                    }}
                  >
                    <Avatar name={person.name} size="sm" tone={toneOf[person.role]} />
                  </Box>
                ))}
              </Box>
            </Box>
          );
        })}
      </Box>
    </Card>
  );
}

/** Fases e marcos (ADR-0071): the smaller timeline, or the message of a project without a schedule. */
function TimelineCard({
  overview,
  listWidth,
  maxHeight,
}: {
  overview: Overview;
  listWidth: number;
  maxHeight?: number;
}) {
  const { t } = useTranslation();
  const { project } = useProjectOutlet();
  return (
    <Card
      title={t('projects.overview.timeline.title')}
      actions={
        <Button
          component={Link}
          to={`/projects/${project.id}/schedule`}
          variant="text"
          size="small"
          endIcon={<ArrowForwardOutlined />}
        >
          {t('projects.overview.timeline.open')}
        </Button>
      }
    >
      {overview.timeline === null ? (
        <Box sx={emptySx}>{t('projects.overview.timeline.empty')}</Box>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', maxHeight }}>
          <OverviewTimeline
            timeline={overview.timeline}
            today={overview.today}
            listWidth={listWidth}
          />
        </Box>
      )}
    </Card>
  );
}

/**
 * The tab Visão geral of a project (ADR-0073, canvas ProjetoVisaoGeral), which answers "está no prazo?": the four
 * numbers; the phases and the milestones; the team; and what needs attention. In the height of the window on the
 * computer, with the list of attention scrolling inside (ADR-0066); one under the other on the phone.
 */
export function ProjectOverview() {
  const { project } = useProjectOutlet();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const wide = useMediaQuery(theme.breakpoints.up('lg'), { noSsr: true });
  const { data: overview } = useQuery(projectOverviewQuery(project.id));

  if (overview === undefined) {
    return null;
  }

  if (!desktop) {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)', flex: 'none' }}>
        <Numbers overview={overview} />
        <AttentionCard overview={overview} fill={false} />
        <TimelineCard overview={overview} listWidth={148} />
        <TeamCard teams={overview.teams} fill={false} />
      </Box>
    );
  }

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-4)',
      }}
    >
      <Numbers overview={overview} />
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'grid',
          gridTemplateColumns: wide
            ? 'minmax(0, 2fr) minmax(360px, 1fr)'
            : 'minmax(0, 2fr) minmax(320px, 1fr)',
          gap: 'var(--space-4)',
        }}
      >
        <Box
          sx={{
            minWidth: 0,
            minHeight: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
          }}
        >
          <TimelineCard overview={overview} listWidth={wide ? 264 : 180} maxHeight={230} />
          <TeamCard teams={overview.teams} fill />
        </Box>
        <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
          <AttentionCard overview={overview} fill />
        </Box>
      </Box>
    </Box>
  );
}
