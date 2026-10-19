import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTranslation } from 'react-i18next';
import type { ProjectPerson } from '../api/client';
import { Avatar } from '../components/Avatar';
import { theme } from '../theme/theme';
import { useProjectOutlet } from './projectOutlet';

const cardSx = {
  boxSizing: 'border-box',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  overflow: 'hidden',
} as const;

const cardHeadSx = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: 'var(--space-3)',
  minHeight: 48,
  padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
  borderBottom: '1px solid var(--border)',
} as const;

const cardTitleSx = {
  margin: 0,
  fontSize: 15,
  lineHeight: '22px',
  fontWeight: 500,
  color: 'var(--text)',
} as const;

/** The teams of a project (ADR-0040, ADR-0068): Gestão, Funcional and Desenvolvimento. */
function teamsOf(members: readonly ProjectPerson[]) {
  const withRole = (...roles: ProjectPerson['role'][]) =>
    members.filter((member) => roles.includes(member.role));
  return [
    { key: 'management', people: withRole('Manager', 'TechLead') },
    { key: 'functional', people: withRole('Contributor') },
    { key: 'development', people: withRole('TechLead', 'Developer') },
  ] as const;
}

/** The tab Visão geral of a project: the description and the team, until the overview of Síntese-2. */
export function ProjectOverview() {
  const { t } = useTranslation();
  const { project } = useProjectOutlet();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: desktop ? 'minmax(0, 2fr) minmax(320px, 1fr)' : 'minmax(0, 1fr)',
        gap: 'var(--space-4)',
        alignItems: 'start',
      }}
    >
      <Box component="section" aria-labelledby="project-description" sx={cardSx}>
        <Box sx={cardHeadSx}>
          <Box component="h2" id="project-description" sx={cardTitleSx}>
            {t('projects.overview.description')}
          </Box>
        </Box>
        <Box
          sx={{
            padding: 'var(--space-4)',
            whiteSpace: 'pre-wrap',
            overflowWrap: 'anywhere',
            color: project.description === null ? 'var(--text-secondary)' : 'var(--text)',
          }}
        >
          {project.description ?? t('projects.overview.noDescription')}
        </Box>
      </Box>
      <Box component="section" aria-labelledby="project-team" sx={cardSx}>
        <Box sx={cardHeadSx}>
          <Box component="h2" id="project-team" sx={cardTitleSx}>
            {t('projects.overview.team')}
          </Box>
        </Box>
        <Box component="ul" sx={{ margin: 0, padding: 0, listStyle: 'none' }}>
          {teamsOf(project.members).map((team) => (
            <Box
              component="li"
              key={team.key}
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-3)',
                padding: '10px var(--space-4)',
                borderBottom: '1px solid var(--border)',
                '&:last-of-type': { borderBottom: 0 },
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ fontWeight: 500, color: 'var(--text)' }}>
                  {t(`projects.overview.teams.${team.key}`)}
                </Box>
                <Box sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}>
                  {team.key === 'management' && team.people.length > 0
                    ? team.people.map((person) => person.name).join(', ')
                    : t('projects.overview.people', { count: team.people.length })}
                </Box>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                {team.people.map((person, index) => (
                  <Box
                    key={person.id}
                    component="span"
                    sx={{
                      display: 'inline-flex',
                      marginLeft: index === 0 ? 0 : '-6px',
                      borderRadius: 'var(--radius-full)',
                      boxShadow: '0 0 0 2px var(--bg-surface)',
                    }}
                  >
                    <Avatar name={person.name} size="sm" />
                  </Box>
                ))}
              </Box>
            </Box>
          ))}
        </Box>
      </Box>
    </Box>
  );
}
