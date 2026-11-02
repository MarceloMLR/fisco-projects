import MenuOutlined from '@mui/icons-material/MenuOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { NotificationBell } from '../notifications/NotificationBell';

/** Phone top bar (fp-topbar): menu button, page title and the bell (ADR-0038), with a line below (ADR-0012). */
export function MobileTopBar({
  title,
  menuOpen,
  menuId,
  onOpenMenu,
}: {
  title: string;
  menuOpen: boolean;
  menuId: string;
  onOpenMenu: () => void;
}) {
  const { t } = useTranslation();

  return (
    <Box
      component="header"
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-1)',
        flex: 'none',
        boxSizing: 'border-box',
        height: 'var(--topbar-height)',
        padding: '0 var(--space-2) 0 var(--space-1)',
        backgroundColor: 'var(--topbar-bg)',
        color: 'var(--text)',
        // The border of the top bar, not of the sidebar: the sidebar may have a color of its own (ADR-0146).
        borderBottom: '1px solid var(--border)',
      }}
    >
      <Box
        component="button"
        type="button"
        aria-label={t('shell.openMenu')}
        aria-expanded={menuOpen}
        aria-controls={menuId}
        onClick={onOpenMenu}
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          width: 44,
          height: 44,
          padding: 0,
          border: 0,
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'transparent',
          color: 'var(--text-secondary)',
          cursor: 'pointer',
        }}
      >
        <MenuOutlined sx={{ fontSize: 'var(--icon-lg)' }} />
      </Box>
      <Box
        component="span"
        sx={{
          flex: 1,
          minWidth: 0,
          fontSize: 16,
          lineHeight: '24px',
          fontWeight: 500,
          whiteSpace: 'nowrap',
          overflow: 'hidden',
          textOverflow: 'ellipsis',
        }}
      >
        {title}
      </Box>
      <NotificationBell size={44} />
    </Box>
  );
}
