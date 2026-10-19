import LogoutOutlined from '@mui/icons-material/LogoutOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Snackbar from '@mui/material/Snackbar';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { profileQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { formatLastAccess } from '../format/dates';
import { useSession } from '../session/SessionContext';
import { ThemeOptions } from './ThemeOptions';
import { useThemeChoice } from './themeChoice';

/** Below this width the account goes under the preferences, as fp-split in the design system. */
const singleColumn = '@media (max-width: 1100px)';

const knownSignInMethods = ['local', 'entra'] as const;
type KnownSignInMethod = (typeof knownSignInMethods)[number];
const isKnown = (method: string): method is KnownSignInMethod =>
  (knownSignInMethods as readonly string[]).includes(method);

/**
 * My profile, as in the canvas: who the person is, the preferences (today the theme, ADR-0014) and, on the side,
 * the account (role, how they sign in, last access) and Sign out. Name, e-mail and role show from the session at
 * once; the account comes from the profile.
 */
export function ProfilePage() {
  const { t } = useTranslation();
  const { session, signOut } = useSession();
  const { theme, choose, message, dismiss } = useThemeChoice();
  const profile = useQuery(profileQuery()).data;

  if (session.status !== 'signed-in') {
    return null;
  }
  const { user } = session;
  const role = t(`users.roles.${user.role}`);
  const signInMethods = profile?.signInMethods
    .map((method) => (isKnown(method) ? t(`profile.signInMethods.${method}`) : method))
    .join(', ');

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 1fr) 400px',
        overflow: 'hidden',
        [singleColumn]: { gridTemplateColumns: 'minmax(0, 1fr)', overflow: 'auto' },
      }}
    >
      <Box
        sx={{
          minWidth: 0,
          minHeight: 0,
          boxSizing: 'border-box',
          display: 'flex',
          flexDirection: 'column',
          gap: '20px',
          padding: 'var(--space-5) var(--space-6) var(--space-6)',
          overflow: 'auto',
          [singleColumn]: { overflow: 'visible' },
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-4)' }}>
          <Avatar name={user.name} size="lg" />
          <Box sx={{ minWidth: 0 }}>
            <Box
              component="h1"
              sx={{
                margin: 0,
                fontSize: 26,
                lineHeight: '34px',
                fontWeight: 500,
                letterSpacing: '-0.015em',
                color: 'var(--text)',
              }}
            >
              {user.name}
            </Box>
            <Box
              sx={{
                marginTop: '2px',
                fontSize: 13,
                lineHeight: '18px',
                color: 'var(--text-secondary)',
                overflowWrap: 'anywhere',
              }}
            >
              {t('profile.subtitle', { email: user.email, role })}
            </Box>
          </Box>
        </Box>
        <Box
          component="section"
          aria-labelledby="profile-preferences"
          sx={{
            boxSizing: 'border-box',
            backgroundColor: 'var(--bg-surface)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-lg)',
          }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              minHeight: 48,
              boxSizing: 'border-box',
              padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
              borderBottom: '1px solid var(--border)',
            }}
          >
            <Box
              component="h2"
              id="profile-preferences"
              sx={{
                margin: 0,
                fontSize: 15,
                lineHeight: '22px',
                fontWeight: 500,
                color: 'var(--text)',
              }}
            >
              {t('profile.preferences')}
            </Box>
          </Box>
          <Box sx={{ padding: '0 var(--space-4)' }}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: '200px minmax(0, 1fr)',
                gap: 'var(--space-6)',
                padding: 'var(--space-5) 0',
                [singleColumn]: { gridTemplateColumns: 'minmax(0, 1fr)', gap: 'var(--space-3)' },
              }}
            >
              <Box component="span" sx={{ fontWeight: 500, color: 'var(--text)' }}>
                {t('profile.theme')}
              </Box>
              <ThemeOptions label={t('profile.theme')} value={theme} onChange={choose} />
            </Box>
          </Box>
        </Box>
      </Box>
      <Box
        component="aside"
        aria-label={t('profile.account')}
        sx={{
          minWidth: 0,
          minHeight: 0,
          boxSizing: 'border-box',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-5)',
          padding: 'var(--space-5) var(--space-5) var(--space-6)',
          backgroundColor: 'var(--bg-subtle)',
          borderLeft: '1px solid var(--border)',
          overflow: 'auto',
          [singleColumn]: {
            borderLeft: 0,
            borderTop: '1px solid var(--border)',
            overflow: 'visible',
          },
        }}
      >
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-3)',
            paddingBottom: 'var(--space-5)',
            borderBottom: '1px solid var(--border)',
          }}
        >
          <Box
            component="h2"
            sx={{
              margin: 0,
              fontSize: 12,
              lineHeight: '16px',
              fontWeight: 500,
              color: 'var(--text-secondary)',
            }}
          >
            {t('profile.account')}
          </Box>
          <Box
            component="dl"
            sx={{
              display: 'grid',
              gridTemplateColumns: '136px minmax(0, 1fr)',
              gap: 'var(--space-3)',
              alignItems: 'center',
              margin: 0,
              '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
              '& dd': { margin: 0, minWidth: 0, color: 'var(--text)' },
            }}
          >
            <dt>{t('profile.role')}</dt>
            <dd>{role}</dd>
            <dt>{t('profile.signIn')}</dt>
            <dd>{signInMethods ?? t('profile.none')}</dd>
            <dt>{t('profile.lastAccess')}</dt>
            <dd>
              {profile === undefined
                ? t('profile.none')
                : formatLastAccess(profile.lastSignInAt, t)}
            </dd>
          </Box>
        </Box>
        <Box>
          <Button
            variant="outlined"
            size="small"
            startIcon={<LogoutOutlined />}
            onClick={() => {
              void signOut();
            }}
          >
            {t('shell.signOut')}
          </Button>
        </Box>
      </Box>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={dismiss}
      />
    </Box>
  );
}
