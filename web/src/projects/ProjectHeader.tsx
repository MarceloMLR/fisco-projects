import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import type { MouseEvent } from 'react';
import { useTranslation } from 'react-i18next';
import type { Project } from '../api/client';
import { initials } from '../components/initials';
import { theme } from '../theme/theme';
import { ProjectStatusBadge } from './ProjectStatusBadge';

/** The names of the people of the project with a role, joined. */
const namesWith = (project: Project, role: Project['members'][number]['role']) =>
  project.members
    .filter((member) => member.role === role)
    .map((member) => member.name)
    .join(', ');

/**
 * The header of a project in every tab (canvas ProjetoCabecalho, fp-proj): the logo of the client or the initials,
 * the name and the status, the code, the client, the manager and the tech lead (ADR-0072), the dates, "Ações do
 * projeto" and the tabs.
 */
export function ProjectHeader({
  project,
  onOpenActions,
}: {
  project: Project;
  /** Opens the menu of the actions; none when the person can do nothing in the project. */
  onOpenActions?: (event: MouseEvent<HTMLElement>) => void;
}) {
  const { t } = useTranslation();
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
            <span>{t('projects.noSchedule')}</span>
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
          margin: '0 calc(var(--space-3) * -1)',
          overflowX: 'auto',
        }}
      >
        <Box
          component="button"
          type="button"
          role="tab"
          id="project-tab-overview"
          aria-selected="true"
          aria-controls="project-panel-overview"
          tabIndex={0}
          sx={{
            position: 'relative',
            display: 'inline-flex',
            alignItems: 'center',
            height: 44,
            padding: '0 var(--space-3)',
            border: 0,
            backgroundColor: 'transparent',
            color: 'var(--action-text)',
            fontFamily: 'var(--font-sans)',
            fontSize: 14,
            lineHeight: '20px',
            fontWeight: 500,
            whiteSpace: 'nowrap',
            cursor: 'default',
            '&::after': {
              content: '""',
              position: 'absolute',
              left: 'var(--space-3)',
              right: 'var(--space-3)',
              bottom: -1,
              height: 2,
              backgroundColor: 'var(--action-text)',
              borderRadius: 'var(--radius-sm) var(--radius-sm) 0 0',
            },
          }}
        >
          {t('pages.project.title')}
        </Box>
      </Box>
    </Box>
  );
}
