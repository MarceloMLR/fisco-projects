import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { type MouseEvent, useLayoutEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { managesTerms } from '../acceptances/acceptances';
import type { Project } from '../api/client';
import {
  acceptancesQuery,
  credentialsQuery,
  documentFoldersQuery,
  linksQuery,
  ticketCountsQuery,
} from '../api/queries';
import { initials } from '../components/initials';
import { formatPeriodText } from '../format/dates';
import { useSession } from '../session/SessionContext';
import { fade, motion, play, reducesMotion } from '../theme/motion';
import { theme } from '../theme/theme';
import { ProjectStatusBadge } from './ProjectStatusBadge';
import type { ProjectTab } from './projectTabs';
import { Button } from '../components/Button';

/** The names of the people of the project with a role, joined. */
const namesWith = (project: Project, role: Project['members'][number]['role']) =>
  project.members
    .filter((member) => member.role === role)
    .map((member) => member.name)
    .join(', ');

const tabSx = {
  position: 'relative',
  display: 'inline-flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  height: 44,
  padding: '0 var(--space-3)',
  color: 'var(--text-secondary)',
  fontFamily: 'var(--font-sans)',
  fontSize: 14,
  lineHeight: '20px',
  fontWeight: 500,
  whiteSpace: 'nowrap',
  textDecoration: 'none',
  '&:hover': { color: 'var(--text)' },
  '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: -2 },
  '&[aria-selected="true"]': {
    color: 'var(--action-text)',
    '&::after': {
      content: '""',
      position: 'absolute',
      left: 'var(--space-3)',
      right: 'var(--space-3)',
      bottom: 0,
      height: 2,
      backgroundColor: 'var(--action-text)',
      borderRadius: 'var(--radius-sm) var(--radius-sm) 0 0',
    },
  },
} as const;

/** The count beside the name of a tab (fp-count). */
const countSx = {
  display: 'inline-grid',
  placeItems: 'center',
  minWidth: 20,
  height: 20,
  padding: '0 6px',
  boxSizing: 'border-box',
  borderRadius: 'var(--radius-full)',
  backgroundColor: 'var(--bg-subtle)',
  color: 'var(--text-secondary)',
  fontSize: 12,
  lineHeight: '16px',
  fontWeight: 500,
  fontVariantNumeric: 'tabular-nums',
} as const;

/**
 * The count of a tab (fp-count): its place is there from the start, empty while the counts come, and the number fades
 * in with the others (ADR-0133). A count that failed leaves its place empty.
 */
function TabCount({ value }: { value: number | undefined }) {
  const number = useRef<HTMLSpanElement>(null);
  const shown = value !== undefined;
  useLayoutEffect(() => {
    if (shown) {
      void play(
        number.current,
        fade(true),
        reducesMotion() ? motion.reduced : motion.count,
        motion.enter,
      );
    }
  }, [shown]);
  return (
    <Box component="span" sx={countSx} aria-hidden={shown ? undefined : true}>
      {shown && (
        <Box component="span" ref={number}>
          {value}
        </Box>
      )}
    </Box>
  );
}

/**
 * The header of a project in every tab (canvas ProjetoCabecalho, fp-proj): the logo of the client or the initials,
 * the name and the status, the code, the client, the manager and the tech lead (ADR-0072), the dates of the schedule
 * ("Sem cronograma" without tasks and milestones), "Ações do projeto" and the tabs, each a link to its address, Membros
 * with the number of people.
 */
export function ProjectHeader({
  project,
  tab,
  onOpenActions,
}: {
  project: Project;
  tab: ProjectTab;
  /** Opens the menu of the actions; none when the person can do nothing in the project. */
  onOpenActions?: (event: MouseEvent<HTMLElement>) => void;
}) {
  const { t } = useTranslation();
  // The count of the tab Chamados: all the tickets of the project, any status (canvas ProjetoChamados).
  const ticketCounts = useQuery(ticketCountsQuery(project.id));
  // The count of the tab Documentos: the documents not obsolete in the folders the person sees.
  const documentFolders = useQuery(documentFoldersQuery(project.id));
  // The count of the tab Links: all the links of the project.
  const projectLinks = useQuery(linksQuery(project.id));
  // The count of the tab Acessos: all the credentials of the project.
  const projectCredentials = useQuery(credentialsQuery(project.id));
  // The tab Termos de aceite, only for who manages the terms (ADR-0129), with all the terms of the project.
  const { session } = useSession();
  const seesAcceptances = managesTerms(
    project,
    session.status === 'signed-in' ? session.user : undefined,
  );
  const projectAcceptances = useQuery({
    ...acceptancesQuery(project.id),
    enabled: seesAcceptances,
  });
  // The numbers come in all together, once every count answered, so the tabs do not move one by one (ADR-0133).
  const counted =
    [ticketCounts, documentFolders, projectLinks, projectCredentials].every(
      (query) => !query.isPending,
    ) &&
    (!seesAcceptances || !projectAcceptances.isPending);
  const count = (value: number | undefined) => <TabCount value={counted ? value : undefined} />;
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const managers = namesWith(project, 'Manager');
  const techLeads = namesWith(project, 'TechLead');
  const logo = project.client?.logoUrl ?? null;

  return (
    <Box
      component="header"
      sx={{
        flex: 'none',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        padding: desktop ? 'var(--space-4) var(--space-6) 0' : 'var(--space-3) var(--space-3) 0',
        borderBottom: '1px solid var(--border)',
        backgroundColor: 'var(--bg-page)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: desktop ? 'var(--space-4)' : 'var(--space-3)',
        }}
      >
        <Box
          component="span"
          aria-hidden
          data-testid="project-logo"
          sx={{
            display: 'inline-grid',
            placeItems: 'center',
            flex: 'none',
            boxSizing: 'border-box',
            width: 44,
            height: 44,
            padding: logo === null ? 0 : '4px',
            overflow: 'hidden',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--bg-surface)',
            color: 'var(--text-secondary)',
            fontSize: 14,
            fontWeight: 500,
          }}
        >
          {logo === null ? (
            initials(project.client?.name ?? project.name)
          ) : (
            <Box
              component="img"
              src={logo}
              alt=""
              sx={{ display: 'block', maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
            />
          )}
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Box
            sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 'var(--space-2)' }}
          >
            <Box
              component="h1"
              sx={{
                margin: 0,
                fontSize: desktop ? 22 : 18,
                lineHeight: desktop ? '30px' : '26px',
                fontWeight: 500,
                letterSpacing: '-0.01em',
                color: 'var(--text)',
                minWidth: 0,
                overflowWrap: 'anywhere',
              }}
            >
              {project.name}
            </Box>
            <ProjectStatusBadge status={project.status} />
          </Box>
          <Box
            data-testid="project-meta"
            sx={{
              display: 'flex',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 'var(--space-1) var(--space-4)',
              marginTop: '2px',
              fontSize: 13,
              lineHeight: '18px',
              color: 'var(--text-secondary)',
              '& b': { fontWeight: 500, color: 'var(--text)' },
            }}
          >
            <Box
              component="span"
              sx={{
                fontFamily: 'var(--font-mono)',
                fontSize: 12,
                lineHeight: '16px',
                fontWeight: 500,
              }}
            >
              {project.code}
            </Box>
            <span>{project.client?.name ?? t('projects.internal')}</span>
            {managers !== '' && (
              <span>
                {t('projects.header.manager')} <b>{managers}</b>
              </span>
            )}
            {techLeads !== '' && (
              <span>
                {t('projects.header.techLead')} <b>{techLeads}</b>
              </span>
            )}
            {project.scheduleStart === null || project.scheduleEnd === null ? (
              <span>{t('projects.noSchedule')}</span>
            ) : (
              <Box component="span" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                {formatPeriodText(project.scheduleStart, project.scheduleEnd)}
              </Box>
            )}
          </Box>
        </Box>
        {desktop && onOpenActions !== undefined && (
          <Button
            variant="outlined"
            size="small"
            aria-haspopup="menu"
            endIcon={<ExpandMoreOutlined />}
            onClick={onOpenActions}
            sx={{ flex: 'none' }}
          >
            {t('projects.actions.button')}
          </Button>
        )}
      </Box>
      {!desktop && onOpenActions !== undefined && (
        <Button
          variant="outlined"
          size="small"
          aria-haspopup="menu"
          endIcon={<ExpandMoreOutlined />}
          onClick={onOpenActions}
          sx={{ alignSelf: 'flex-start' }}
        >
          {t('projects.actions.button')}
        </Button>
      )}
      <Box
        role="tablist"
        aria-label={t('projects.header.tabs')}
        sx={{
          display: 'flex',
          gap: 'var(--space-1)',
          // Over the line of the header by 1 px, with the mark of the chosen tab on it; only sideways there is
          // something to scroll, on a narrow screen.
          marginX: 'calc(var(--space-3) * -1)',
          marginBottom: '-1px',
          overflowX: 'auto',
          overflowY: 'hidden',
        }}
      >
        <Box
          component={Link}
          to={`/projects/${project.id}`}
          role="tab"
          id="project-tab-overview"
          aria-selected={tab === 'overview'}
          aria-controls={tab === 'overview' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.project.title')}
        </Box>
        <Box
          component={Link}
          to={`/projects/${project.id}/schedule`}
          role="tab"
          id="project-tab-schedule"
          aria-selected={tab === 'schedule'}
          aria-controls={tab === 'schedule' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectSchedule.title')}
        </Box>
        <Box
          component={Link}
          to={`/projects/${project.id}/tickets`}
          role="tab"
          id="project-tab-tickets"
          aria-selected={tab === 'tickets'}
          aria-controls={tab === 'tickets' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectTickets.title')}
          {count(ticketCounts.data?.all)}
        </Box>
        <Box
          component={Link}
          to={`/projects/${project.id}/documents`}
          role="tab"
          id="project-tab-documents"
          aria-selected={tab === 'documents'}
          aria-controls={tab === 'documents' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectDocuments.title')}
          {count(documentFolders.data?.total)}
        </Box>
        <Box
          component={Link}
          to={`/projects/${project.id}/links`}
          role="tab"
          id="project-tab-links"
          aria-selected={tab === 'links'}
          aria-controls={tab === 'links' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectLinks.title')}
          {count(projectLinks.data?.total)}
        </Box>
        <Box
          component={Link}
          to={`/projects/${project.id}/credentials`}
          role="tab"
          id="project-tab-credentials"
          aria-selected={tab === 'credentials'}
          aria-controls={tab === 'credentials' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectCredentials.title')}
          {count(projectCredentials.data?.total)}
        </Box>
        {(seesAcceptances || tab === 'acceptances') && (
          <Box
            component={Link}
            to={`/projects/${project.id}/acceptances`}
            role="tab"
            id="project-tab-acceptances"
            aria-selected={tab === 'acceptances'}
            aria-controls={tab === 'acceptances' ? 'project-panel' : undefined}
            sx={tabSx}
          >
            {t('pages.projectAcceptances.title')}
            {count(projectAcceptances.data?.counts.all)}
          </Box>
        )}
        <Box
          component={Link}
          to={`/projects/${project.id}/members`}
          role="tab"
          id="project-tab-members"
          aria-selected={tab === 'members'}
          aria-controls={tab === 'members' ? 'project-panel' : undefined}
          sx={tabSx}
        >
          {t('pages.projectMembers.title')}
          {count(project.members.length)}
        </Box>
      </Box>
    </Box>
  );
}
