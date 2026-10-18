import GroupOutlined from '@mui/icons-material/GroupOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink, Outlet } from 'react-router';
import { api } from '../api/client';
import { theme } from '../theme/theme';
import type { AdminContext } from './adminContext';

const sections = [
  {
    labelKey: 'admin.sections.people',
    items: [{ path: '/admin/users', labelKey: 'pages.users.title', icon: GroupOutlined }],
  },
] as const;

/**
 * Administration area, as in the Users screen of the canvas: the sections in a column on the left (fp-nav2 in a
 * fp-pane--nav) and the section on the right. Each section joins it with its own change. On the phone the
 * column is left out, since there is a single section.
 */
export function AdminLayout() {
  const { t } = useTranslation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [usersTotal, setUsersTotal] = useState<number | null>(null);

  useEffect(() => {
    let active = true;
    void api
      .GET('/api/users', { params: { query: { pageSize: 1 } } })
      .then(({ data }) => {
        if (active && data !== undefined) {
          setUsersTotal(data.total);
        }
      })
      .catch(() => undefined);
    return () => {
      active = false;
    };
  }, []);

  const context: AdminContext = { usersTotal };

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: desktop ? '240px minmax(0, 1fr)' : 'minmax(0, 1fr)',
        overflow: 'hidden',
      }}
    >
      {desktop && (
        <Box
          component="nav"
          aria-label={t('admin.sections.label')}
          sx={{
            minHeight: 0,
            boxSizing: 'border-box',
            padding: 'var(--space-5) var(--space-3)',
            backgroundColor: 'var(--bg-subtle)',
            borderRight: '1px solid var(--border)',
            overflow: 'auto',
          }}
        >
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
            {sections.map((section) => [
              <Box
                component="li"
                key={section.labelKey}
                aria-hidden="true"
                sx={{
                  padding: 'var(--space-4) var(--space-3) var(--space-1)',
                  fontSize: 12,
                  lineHeight: '16px',
                  fontWeight: 500,
                  color: 'var(--text-secondary)',
                  '&:first-of-type': { paddingTop: 0 },
                }}
              >
                {t(section.labelKey)}
              </Box>,
              ...section.items.map(({ path, labelKey, icon: Icon }) => (
                <li key={path}>
                  <Box
                    component={NavLink}
                    to={path}
                    sx={{
                      display: 'flex',
                      boxSizing: 'border-box',
                      alignItems: 'center',
                      gap: 'var(--space-2)',
                      minHeight: 36,
                      padding: '0 var(--space-3)',
                      borderRadius: 'var(--radius-md)',
                      color: 'var(--text)',
                      textDecoration: 'none',
                      '& > svg': { fontSize: 18, color: 'var(--text-secondary)' },
                      '&:hover': { backgroundColor: 'var(--bg-hover)' },
                      '&[aria-current="page"]': {
                        backgroundColor: 'var(--bg-surface)',
                        boxShadow: '0 0 0 1px var(--border)',
                        fontWeight: 500,
                        '& > svg': { color: 'var(--action-text)' },
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
                    {usersTotal !== null && (
                      <Box
                        component="span"
                        sx={{
                          fontSize: 12,
                          lineHeight: '16px',
                          fontWeight: 400,
                          color: 'var(--text-secondary)',
                          fontVariantNumeric: 'tabular-nums',
                        }}
                      >
                        {usersTotal}
                      </Box>
                    )}
                  </Box>
                </li>
              )),
            ])}
          </Box>
        </Box>
      )}
      <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <Outlet context={context} />
      </Box>
    </Box>
  );
}
