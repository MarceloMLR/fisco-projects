import LogoutOutlined from '@mui/icons-material/LogoutOutlined';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { SessionUser } from '../api/client';
import { Avatar } from '../components/Avatar';
import { useSession } from '../session/SessionContext';

/** Name and e-mail of the person in the session, next to the avatar (fp-appbar__who). */
export function SessionPerson({ user, mutedColor }: { user: SessionUser; mutedColor: string }) {
  return (
    <>
      <Avatar name={user.name} />
      <Box sx={{ minWidth: 0, overflow: 'hidden' }}>
        <Box sx={{ fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis' }}>
          {user.name}
        </Box>
        <Box
          sx={{
            fontSize: 12,
            lineHeight: '16px',
            color: mutedColor,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {user.email}
        </Box>
      </Box>
    </>
  );
}

/**
 * Avatar at the right of the header, opening the account menu (TopBar, fp-appbar__user and fp-appbar__menu):
 * who is in the session and sign out. The profile and the theme choice join it later.
 */
export function AccountMenu({ user }: { user: SessionUser }) {
  const { t } = useTranslation();
  const { signOut } = useSession();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const menuId = useId();
  const open = anchor !== null;

  return (
    <>
      <Box
        component="button"
        type="button"
        title={user.name}
        aria-label={t('shell.account', { name: user.name })}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        onClick={(event: React.MouseEvent<HTMLElement>) => {
          setAnchor(event.currentTarget);
        }}
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          width: 40,
          height: 40,
          padding: 0,
          border: 0,
          borderRadius: 'var(--radius-full)',
          backgroundColor: 'transparent',
          cursor: 'pointer',
          '& > span': { transition: 'box-shadow 150ms ease-out' },
          '&:hover > span, &[aria-expanded="true"] > span': {
            boxShadow: '0 0 0 2px var(--topbar-bg), 0 0 0 4px var(--border-strong)',
          },
        }}
      >
        <Avatar name={user.name} />
      </Box>
      <Menu
        id={menuId}
        anchorEl={anchor}
        open={open}
        onClose={() => {
          setAnchor(null);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 272, marginTop: '4px' } },
          list: { 'aria-label': t('shell.accountMenu'), sx: { padding: 'var(--space-1) 0' } },
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            color: 'var(--text)',
          }}
        >
          <SessionPerson user={user} mutedColor="var(--text-secondary)" />
        </Box>
        <Divider sx={{ margin: 'var(--space-1) 0', borderColor: 'var(--border)' }} />
        <MenuItem
          onClick={() => {
            setAnchor(null);
            void signOut();
          }}
          sx={{
            gap: 'var(--space-3)',
            minHeight: 36,
            height: 36,
            padding: '0 var(--space-3)',
            color: 'var(--text)',
            '&:hover, &.Mui-focusVisible': { backgroundColor: 'var(--bg-hover)' },
          }}
        >
          <LogoutOutlined
            aria-hidden
            sx={{ fontSize: 'var(--icon-md)', color: 'var(--text-secondary)' }}
          />
          {t('shell.signOut')}
        </MenuItem>
      </Menu>
    </>
  );
}
