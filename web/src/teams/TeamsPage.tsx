import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Button } from '../components/Button';
import { InstallationLogo } from '../installation/InstallationLogo';
import { useInstallation } from '../installation/useInstallation';
import { safeItemPath, schemeOfTeams } from './teamsPage';

/**
 * The page of the installation in Teams (ADR-0149), outside the structure of the screens and without a session: the
 * brand of the installation and "Abrir no {name}", which opens in the browser the item of the notice the person
 * chose in the tab Activity (the path in "item", only of the application itself) or the home page. The theme follows
 * the one of Teams, in "theme".
 */
export function TeamsPage() {
  const { t } = useTranslation();
  const [search] = useSearchParams();
  const { name, logoUrl } = useInstallation();
  const scheme = schemeOfTeams(search.get('theme'));
  const path = safeItemPath(search.get('item'));

  return (
    <Box
      data-theme={scheme}
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'var(--bg-page)',
        color: 'var(--text)',
        fontFamily: 'var(--font-sans)',
        paddingBlock: 'var(--space-6)',
        paddingInline: 'var(--space-4)',
      }}
    >
      <Box
        component="main"
        sx={{
          width: '100%',
          maxWidth: 400,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 'var(--space-5)',
          padding: 'var(--space-6)',
          border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)',
          backgroundColor: 'var(--bg-surface)',
          textAlign: 'center',
        }}
      >
        <Box
          component="h1"
          sx={{
            margin: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 'var(--space-3)',
            fontSize: 18,
            lineHeight: '26px',
            fontWeight: 500,
            overflowWrap: 'anywhere',
          }}
        >
          {logoUrl !== null && <InstallationLogo src={logoUrl} maxHeight={40} />}
          {name}
        </Box>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 'var(--space-2)',
          }}
        >
          <Button
            variant="contained"
            startIcon={<OpenInNewOutlined />}
            href={path}
            target="_blank"
            rel="noopener noreferrer"
          >
            {t('teamsPage.open', { name })}
          </Button>
          <Box
            component="span"
            sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
          >
            {t('teamsPage.inBrowser')}
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
