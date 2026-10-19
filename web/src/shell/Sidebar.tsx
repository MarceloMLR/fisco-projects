import PersonOutlined from '@mui/icons-material/PersonOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { Link, NavLink } from 'react-router';
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
 * Sidebar from the design system (Sidebar, fp-sidebar): full height, product name on top, then the main
 * navigation. Without the brand it is the content of the phone menu, which also has My profile.
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
  const navigation = withProfile ? [...mainItems, profileItem] : mainItems;

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
            {t('app.name')}
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
    </Box>
  );
}
