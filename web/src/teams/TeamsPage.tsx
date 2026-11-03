import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import Box from '@mui/material/Box';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Button } from '../components/Button';
import { InstallationLogo } from '../installation/InstallationLogo';
import { useInstallation } from '../installation/useInstallation';
import type { TeamsScheme } from './teamsHost';
import { safeItemPath, schemeOfTeams } from './teamsPage';
import { TeamsNoAccess, TeamsTicketView } from './TeamsTicket';
import { ticketNumberOf, useTeamsTicket } from './teamsTicket';

/**
 * The brand of the installation and "Abrir no {name}", which opens the item in the browser (ADR-0149): the page when
 * the ticket is not shown, with a line while the ticket is read or when its reading failed.
 */
function BrandCard({ path, status }: { path: string; status: string | null }) {
  const { t } = useTranslation();
  const { name, logoUrl } = useInstallation();
  return (
    <Box
      sx={{
        minHeight: '100%',
        boxSizing: 'border-box',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
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
        {status !== null && (
          <Box
            role="status"
            sx={{ margin: 0, fontSize: 14, lineHeight: '20px', color: 'var(--text-secondary)' }}
          >
            {status}
          </Box>
        )}
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

/**
 * The page of the installation in Teams (ADR-0149, ADR-0152), outside the structure of the screens and without a
 * session. With the notice of a ticket, inside Teams, it shows the ticket only to read, as the person of the token
 * Teams gives the page, with "Responder no FiscoProjects"; the ticket the person does not see says so. Otherwise (outside
 * Teams, another item, no item, the token refused or a failure), the brand and "Abrir no {name}", which opens the item
 * (the path in "item", only of the application itself) or the home page in the browser. The theme follows the one of
 * Teams, from "theme" and from Teams itself.
 */
export function TeamsPage() {
  const { t } = useTranslation();
  const [search] = useSearchParams();
  const { name } = useInstallation();
  const [scheme, setScheme] = useState<TeamsScheme>(() => schemeOfTeams(search.get('theme')));
  const path = safeItemPath(search.get('item'));
  const state = useTeamsTicket(ticketNumberOf(path), setScheme);

  return (
    <Box
      data-theme={scheme}
      sx={{
        height: '100vh',
        overflow: 'auto',
        backgroundColor: 'var(--bg-page)',
        color: 'var(--text)',
        fontFamily: 'var(--font-sans)',
        fontSize: 14,
        lineHeight: '20px',
      }}
    >
      {state.kind === 'ticket' ? (
        <TeamsTicketView ticket={state.ticket} name={name} />
      ) : state.kind === 'noAccess' ? (
        <TeamsNoAccess path={path} name={name} />
      ) : (
        <BrandCard
          path={path}
          status={
            state.kind === 'loading'
              ? t('teamsPage.opening')
              : state.failed
                ? t('teamsPage.failed')
                : null
          }
        />
      )}
    </Box>
  );
}
