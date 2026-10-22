import LogoutOutlined from '@mui/icons-material/LogoutOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Snackbar from '@mui/material/Snackbar';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { profileQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { useDates } from '../format/useDates';
import { useSession } from '../session/SessionContext';
import { EmailNotices } from './EmailNotices';
import { ProfileCard, ProfileRow, singleColumn } from './ProfileCard';
import { ThemeOptions } from './ThemeOptions';
import { useThemeChoice } from './themeChoice';

const knownSignInMethods = ['local', 'entra'] as const;
type KnownSignInMethod = (typeof knownSignInMethods)[number];
const isKnown = (method: string): method is KnownSignInMethod =>
  (knownSignInMethods as readonly string[]).includes(method);

/**
 * My profile, as in the canvas: who the person is, the preferences (today the theme, ADR-0014), the e-mails
 * (ADR-0037) and, on the side, the account (role, how they sign in, last access) and Sign out. Name, e-mail and role show from the session at
 * once; the account comes from the profile.
 */
export function ProfilePage() {
  const { t } = useTranslation();
  const { formatLastAccess } = useDates();
  const { session, signOut } = useSession();
  const { theme, choose, message, dismiss } = useThemeChoice();
  const [notice, setNotice] = useState<string>();
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
        // One under the other, each as tall as its content, and the whole page scrolls; the account fills
        // what is left of the window.
        [singleColumn]: {
          gridTemplateColumns: 'minmax(0, 1fr)',
          gridTemplateRows: 'auto 1fr',
          overflow: 'auto',
        },
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
          [singleColumn]: { minHeight: 'auto', overflow: 'visible' },
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
        <ProfileCard id="profile-preferences" title={t('profile.preferences')}>
          <ProfileRow label={t('profile.theme')}>
            <ThemeOptions label={t('profile.theme')} value={theme} onChange={choose} />
          </ProfileRow>
        </ProfileCard>
        <EmailNotices onMessage={setNotice} />
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
            minHeight: 'auto',
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
        open={(message ?? notice) !== undefined}
        message={message ?? notice}
        autoHideDuration={6000}
        onClose={() => {
          dismiss();
          setNotice(undefined);
        }}
      />
    </Box>
  );
}
