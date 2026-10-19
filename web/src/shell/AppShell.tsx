import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Outlet, useParams } from 'react-router';
import { generalRulesQuery, projectQuery } from '../api/queries';
import { SessionPending } from '../session/RequireSession';
import { useSession } from '../session/SessionContext';
import { theme } from '../theme/theme';
import { SessionPerson } from './AccountMenu';
import { MobileTopBar } from './MobileTopBar';
import { useCurrentPage } from './pages';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

const menuId = 'main-menu';

/** The project of the address (/projects/:projectId), as its page already read it. */
function useProjectOfTheAddress() {
  const { projectId } = useParams();
  const { data } = useQuery({ ...projectQuery(projectId ?? ''), enabled: projectId !== undefined });
  return projectId === undefined ? undefined : data;
}

/**
 * Structure around every screen (ADR-0066): it fills the window, which never scrolls; the content scrolls inside.
 * From 900px, sidebar and invisible header (fp-frame); below it, the phone top bar and menu (ADR-0012).
 */
export function AppShell() {
  const { t } = useTranslation();
  const page = useCurrentPage();
  const { session } = useSession();
  const title = t(page.titleKey);
  const project = useProjectOfTheAddress();
  const trail = (page.trail ?? []).flatMap((level) => {
    if (!('project' in level)) {
      return [{ title: t(level.titleKey), path: level.path }];
    }
    // The project of the address shows once it is read.
    return project === undefined
      ? []
      : [{ title: project.code, path: `/projects/${project.id}`, code: true }];
  });
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [menuOpen, setMenuOpen] = useState(false);
  // No screen shows a date before the time zone of the installation is known (ADR-0013).
  const rules = useQuery(generalRulesQuery());

  // The shell is only reached through RequireSession.
  if (session.status !== 'signed-in') {
    return null;
  }
  // Only the first answer, or the first failure, is waited for: a later reading never takes the screen away.
  if (!rules.isFetched) {
    return <SessionPending />;
  }
  const { user } = session;

  if (desktop) {
    return (
      <Box
        data-testid="app-frame"
        sx={{
          display: 'grid',
          gridTemplateColumns: 'var(--sidebar-width) minmax(0, 1fr)',
          gridTemplateRows: 'var(--topbar-height) minmax(0, 1fr)',
          height: '100dvh',
          boxSizing: 'border-box',
          backgroundColor: 'var(--bg-page)',
          color: 'var(--text)',
          overflow: 'hidden',
          '& > nav': { gridColumn: 1, gridRow: '1 / -1' },
        }}
      >
        <TopBar title={title} trail={trail} user={user} />
        <Sidebar />
        <Box
          component="main"
          sx={{
            gridColumn: 2,
            gridRow: 2,
            display: 'flex',
            flexDirection: 'column',
            minWidth: 0,
            minHeight: 0,
            boxSizing: 'border-box',
            backgroundColor: 'var(--bg-page)',
            overflow: 'auto',
          }}
        >
          <Outlet />
        </Box>
      </Box>
    );
  }

  const closeMenu = () => {
    setMenuOpen(false);
  };

  return (
    <Box
      data-testid="app-frame"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '100dvh',
        boxSizing: 'border-box',
        backgroundColor: 'var(--bg-page)',
        color: 'var(--text)',
        overflow: 'hidden',
      }}
    >
      <MobileTopBar
        title={title}
        menuOpen={menuOpen}
        menuId={menuId}
        onOpenMenu={() => {
          setMenuOpen(true);
        }}
      />
      <Drawer
        id={menuId}
        variant="temporary"
        open={menuOpen}
        onClose={closeMenu}
        slotProps={{
          paper: {
            role: 'dialog',
            'aria-modal': true,
            'aria-label': t('shell.menu'),
            sx: {
              width: 'var(--sidebar-width)',
              backgroundColor: 'var(--sidebar-bg)',
              boxShadow: 'var(--shadow-dialog)',
              border: 0,
              '& > nav': { width: '100%', flex: 1 },
            },
          },
          backdrop: { sx: { backgroundColor: 'var(--overlay)' } },
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            flex: 'none',
            padding: '16px 16px 8px',
            color: 'var(--sidebar-text-active)',
          }}
        >
          <SessionPerson user={user} mutedColor="var(--sidebar-muted)" />
        </Box>
        <Sidebar brand={false} withProfile onNavigate={closeMenu} />
      </Drawer>
      <Box
        component="main"
        sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column', overflow: 'auto' }}
      >
        <Outlet />
      </Box>
    </Box>
  );
}
