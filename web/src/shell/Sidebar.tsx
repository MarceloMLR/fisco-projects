import PersonOutlined from '@mui/icons-material/PersonOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link, NavLink } from 'react-router';
import { myProjectsQuery } from '../api/queries';
import { InstallationLogo } from '../installation/InstallationLogo';
import { useInstallation } from '../installation/useInstallation';
import { useMainNavigation } from './pages';

const transition = 'background-color 150ms ease-out, color 150ms ease-out';

/** My profile at the end of the phone menu, as in the canvas; on a computer it is in the account menu. */
const profileItem = {
  key: 'profile',
  path: '/profile',
  labelKey: 'pages.profile.title',
  icon: PersonOutlined,
} as const;

/**
 * Sidebar from the design system (Sidebar, fp-sidebar): full height, the name of the installation on top after its
 * logo (or the badge, without one), then the main navigation. Without the brand it is the content of the phone menu,
 * which also has My profile.
 */
export function Sidebar({
  brand = true,
  withProfile = false,
  onNavigate,
}: {
  brand?: boolean;
  withProfile?: boolean;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation();
  const mainItems = useMainNavigation();
  const installation = useInstallation();
  const navigation = withProfile ? [...mainItems, profileItem] : mainItems;
  // The active projects the person is in, even the Administrator (ADR-0066).
  const myProjects = useQuery(myProjectsQuery()).data?.items ?? [];

  return (
    <Box
      component="nav"
      aria-label={t('shell.mainNavigation')}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-5)',
        boxSizing: 'border-box',
        width: 'var(--sidebar-width)',
        minHeight: 0,
        padding: brand ? '0 var(--space-3) var(--space-4)' : '8px var(--space-3) var(--space-4)',
        backgroundColor: 'var(--sidebar-bg)',
        color: 'var(--sidebar-text)',
        borderRight: brand ? '1px solid var(--sidebar-border)' : 'none',
        overflow: 'auto',
      }}
    >
      {brand && (
        <Box
          component={Link}
          to="/"
          onClick={onNavigate}
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            flex: 'none',
            boxSizing: 'border-box',
            height: 'var(--topbar-height)',
            margin: '0 0 calc(var(--space-2) * -1)',
            padding: '0 var(--space-2)',
            color: 'var(--text)',
            textDecoration: 'none',
          }}
        >
          {installation.logoUrl === null ? (
            <Box
              component="span"
              aria-hidden="true"
              sx={{
                display: 'inline-grid',
                placeItems: 'center',
                flex: 'none',
                width: 28,
                height: 28,
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--action)',
                color: 'var(--on-action)',
              }}
            >
              <ViewTimelineOutlined sx={{ fontSize: 18 }} />
            </Box>
          ) : (
            <InstallationLogo src={installation.logoUrl} maxHeight={28} />
          )}
          <Box
            component="span"
            sx={{
              minWidth: 0,
              fontSize: 16,
              lineHeight: '24px',
              fontWeight: 500,
              letterSpacing: '-0.01em',
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
            }}
          >
            {installation.name}
          </Box>
        </Box>
      )}
      <Box
        component="ul"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: '2px',
          margin: 0,
          padding: 0,
          listStyle: 'none',
        }}
      >
        {navigation.map(({ key, path, labelKey, icon: Icon }) => (
          <li key={key}>
            <Box
              component={NavLink}
              to={path}
              // Home is current only on its own address; the others also inside their pages (a project, a ticket).
              end={path === '/'}
              onClick={onNavigate}
              sx={{
                position: 'relative',
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-3)',
                height: 38,
                padding: '0 var(--space-3)',
                borderRadius: 'var(--radius-md)',
                color: 'var(--sidebar-text)',
                fontSize: 14,
                lineHeight: '20px',
                fontWeight: 500,
                textDecoration: 'none',
                transition,
                '& > svg': { fontSize: 'var(--icon-md)', color: 'var(--sidebar-muted)' },
                '&:hover': { backgroundColor: 'var(--sidebar-hover-bg)' },
                '&[aria-current="page"]': {
                  backgroundColor: 'var(--sidebar-active-bg)',
                  color: 'var(--sidebar-text-active)',
                  '& > svg': { color: 'var(--sidebar-indicator)' },
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    left: 0,
                    top: 9,
                    bottom: 9,
                    width: 3,
                    borderRadius: '0 2px 2px 0',
                    backgroundColor: 'var(--sidebar-indicator)',
                  },
                },
              }}
            >
              <Icon />
              <Box
                component="span"
                sx={{
                  flex: 1,
                  minWidth: 0,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {t(labelKey)}
              </Box>
            </Box>
          </li>
        ))}
      </Box>
      {myProjects.length > 0 && (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-1)',
            paddingTop: 'var(--space-4)',
            borderTop: '1px solid var(--sidebar-divider)',
          }}
        >
          <Box
            aria-hidden
            sx={{
              padding: '0 var(--space-3) var(--space-1)',
              fontSize: 12,
              lineHeight: '16px',
              fontWeight: 500,
              color: 'var(--sidebar-muted)',
            }}
          >
            {t('shell.myProjects')}
          </Box>
          <Box
            component="ul"
            aria-label={t('shell.myProjects')}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '2px',
              margin: 0,
              padding: 0,
              listStyle: 'none',
            }}
          >
            {myProjects.map((project) => (
              <li key={project.id}>
                <Box
                  component={NavLink}
                  to={`/projects/${project.id}`}
                  title={`${project.code} · ${project.name}`}
                  onClick={onNavigate}
                  sx={{
                    position: 'relative',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    minHeight: 42,
                    boxSizing: 'border-box',
                    padding: '4px var(--space-3)',
                    borderRadius: 'var(--radius-md)',
                    color: 'var(--sidebar-text)',
                    fontSize: 13,
                    lineHeight: '18px',
                    textDecoration: 'none',
                    transition,
                    '&:hover': { backgroundColor: 'var(--sidebar-hover-bg)' },
                    '&[aria-current="page"]': {
                      backgroundColor: 'var(--sidebar-active-bg)',
                      color: 'var(--sidebar-text-active)',
                      fontWeight: 500,
                      '& > span:last-of-type': { color: 'var(--sidebar-text-active)' },
                    },
                  }}
                >
                  <Box
                    component="span"
                    sx={{
                      minWidth: 0,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {project.name}
                  </Box>
                  <Box
                    component="span"
                    sx={{
                      fontFamily: 'var(--font-mono)',
                      fontSize: 11,
                      lineHeight: '16px',
                      fontWeight: 500,
                      color: 'var(--sidebar-muted)',
                    }}
                  >
                    {project.code}
                  </Box>
                </Box>
              </li>
            ))}
          </Box>
        </Box>
      )}
    </Box>
  );
}
