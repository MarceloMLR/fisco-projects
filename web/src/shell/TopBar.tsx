import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { SessionUser } from '../api/client';
import { AccountMenu } from './AccountMenu';

const crumb = {
  display: 'inline-flex',
  alignItems: 'center',
  height: 32,
  padding: '0 var(--space-2)',
  borderRadius: 'var(--radius-md)',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
} as const;

/**
 * Invisible header over the content (TopBar, fp-appbar): same color as the content, no line below, with the
 * page trail (the areas above the page lead to their addresses) and, at the right, the account menu. The bell
 * joins it with the notifications.
 */
export function TopBar({
  title,
  trail = [],
  user,
}: {
  title: string;
  trail?: readonly { title: string; path: string }[];
  user: SessionUser;
}) {
  const { t } = useTranslation();

  return (
    <Box
      component="header"
      sx={{
        gridColumn: 2,
        gridRow: 1,
        position: 'relative',
        zIndex: 20,
        display: 'flex',
        alignItems: 'center',
        boxSizing: 'border-box',
        height: 'var(--topbar-height)',
        padding: '0 var(--space-4) 0 var(--space-2)',
        backgroundColor: 'var(--topbar-bg)',
        color: 'var(--text)',
      }}
    >
      <Box
        component="nav"
        aria-label={t('shell.breadcrumb')}
        sx={{ flex: 1, minWidth: 0, paddingLeft: 'var(--space-2)' }}
      >
        <Box
          component="ol"
          sx={{
            display: 'flex',
            alignItems: 'center',
            margin: 0,
            padding: 0,
            listStyle: 'none',
            fontSize: 14,
            lineHeight: '20px',
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            '& li + li::before': {
              content: '"/"',
              padding: '0 2px',
              color: 'var(--text-disabled)',
            },
          }}
        >
          {trail.map((level) => (
            <Box
              component="li"
              key={level.path}
              sx={{ display: 'flex', alignItems: 'center', minWidth: 0 }}
            >
              <Box
                component={Link}
                to={level.path}
                sx={{
                  ...crumb,
                  color: 'var(--text-secondary)',
                  textDecoration: 'none',
                  '&:hover': { backgroundColor: 'var(--bg-hover)', color: 'var(--text)' },
                }}
              >
                {level.title}
              </Box>
            </Box>
          ))}
          <Box component="li" sx={{ display: 'flex', alignItems: 'center', minWidth: 0 }}>
            <Box
              component="span"
              aria-current="page"
              sx={{ ...crumb, color: 'var(--text)', fontWeight: 500 }}
            >
              {title}
            </Box>
          </Box>
        </Box>
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-1)',
          marginLeft: 'var(--space-3)',
        }}
      >
        <AccountMenu user={user} />
      </Box>
    </Box>
  );
}
