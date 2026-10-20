import Box from '@mui/material/Box';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { type ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useParams } from 'react-router';
import type { Ticket } from '../api/client';
import { ApiError, ticketQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Avatar } from '../components/Avatar';
import { useDates } from '../format/useDates';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { PriorityTag, TicketPersonTag, TicketStatusBadge } from './TicketTags';
import { ticketNumberOf } from './tickets';

const tagSx = {
  display: 'inline-flex',
  alignItems: 'center',
  height: 22,
  padding: '0 var(--space-2)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-sm)',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  whiteSpace: 'nowrap',
} as const;

const labelSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/** A line of the panel: the label at the left and the value. */
function PanelRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: '112px minmax(0, 1fr)',
        alignItems: 'center',
        gap: 'var(--space-3)',
        minHeight: 32,
      }}
    >
      <Box component="dt" sx={labelSx}>
        {label}
      </Box>
      <Box component="dd" sx={{ margin: 0, minWidth: 0, fontSize: 14, lineHeight: '20px' }}>
        {children}
      </Box>
    </Box>
  );
}

/** The people of the ticket, its project and its last update (canvas Chamado, at the right). */
function PeoplePanel({ ticket }: { ticket: Ticket }) {
  const { t } = useTranslation();
  const { formatDateTime } = useDates();
  const people = (list: Ticket['managers']) =>
    list.length === 0 ? (
      <Box component="span" sx={labelSx}>
        {t('tickets.details.none')}
      </Box>
    ) : (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
        {list.map((person) => (
          <TicketPersonTag key={person.id} name={person.name} />
        ))}
      </Box>
    );

  return (
    <Box
      component="aside"
      aria-labelledby="ticket-people"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-5)',
        padding: 'var(--space-5)',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        alignSelf: 'start',
      }}
    >
      <Box
        component="dl"
        sx={{ margin: 0, display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}
      >
        <Box
          component="h2"
          id="ticket-people"
          sx={{ margin: '0 0 var(--space-2)', fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
        >
          {t('tickets.details.people')}
        </Box>
        <PanelRow label={t('tickets.details.requester')}>
          <TicketPersonTag name={ticket.requester.name} />
        </PanelRow>
        <PanelRow label={t('tickets.details.assignee')}>
          <TicketPersonTag name={ticket.assignee?.name} />
        </PanelRow>
        <PanelRow label={t('tickets.details.team')}>{t(`tickets.teams.${ticket.team}`)}</PanelRow>
        <PanelRow label={t('tickets.details.manager')}>{people(ticket.managers)}</PanelRow>
        <PanelRow label={t('tickets.details.techLead')}>{people(ticket.techLeads)}</PanelRow>
      </Box>
      <Box
        component="dl"
        sx={{
          margin: 0,
          paddingTop: 'var(--space-4)',
          borderTop: '1px solid var(--border)',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-1)',
        }}
      >
        <PanelRow label={t('tickets.details.project')}>
          <Box
            component={Link}
            to={`/projects/${ticket.project.id}/tickets`}
            sx={{
              color: 'var(--action-text)',
              textDecoration: 'none',
              '&:hover': { textDecoration: 'underline' },
            }}
          >
            {`${ticket.project.code} · ${ticket.project.name}`}
          </Box>
        </PanelRow>
        <PanelRow label={t('tickets.details.updated')}>{formatDateTime(ticket.updatedAt)}</PanelRow>
      </Box>
    </Box>
  );
}

/**
 * The page of a ticket (canvas Chamado), at its number: the number with the code of the project and the team, the
 * title, the status, the priority and who opened it and when; the description, as the first message of the requester;
 * and the people beside it (ADR-0029, ADR-0044). The conversation joins it with its own change.
 */
export function TicketPage() {
  const { t } = useTranslation();
  const { number: param } = useParams();
  const location = useLocation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { formatDateTime } = useDates();
  const number = ticketNumberOf(param);
  const {
    data: ticket,
    error,
    isPending,
  } = useQuery({
    ...ticketQuery(number ?? 0),
    enabled: number !== null,
  });
  const state: unknown = location.state;
  const [message, setMessage] = useState<string | undefined>(
    typeof state === 'object' &&
      state !== null &&
      'message' in state &&
      typeof state.message === 'string'
      ? state.message
      : undefined,
  );

  if (ticket === undefined) {
    const missing = number === null || (error instanceof ApiError && error.status === 404);
    return (
      <Page>
        {(missing || !isPending) && (
          <Alert tone={missing ? 'info' : 'danger'}>
            {missing ? t('tickets.details.notFound') : t('tickets.details.loadFailed')}
          </Alert>
        )}
      </Page>
    );
  }

  return (
    <Page withTitle={false}>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          display: 'grid',
          gridTemplateColumns: desktop ? 'minmax(0, 1fr) 340px' : 'minmax(0, 1fr)',
          alignContent: 'start',
          gap: 'var(--space-5)',
        }}
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)', minWidth: 0 }}>
          <Box
            component="header"
            sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}
          >
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 'var(--space-2)',
              }}
            >
              <Box component="span" sx={{ ...labelSx, fontVariantNumeric: 'tabular-nums' }}>
                {t('tickets.number', { number: ticket.number })}
              </Box>
              <Box
                component="span"
                sx={{ ...tagSx, fontFamily: 'var(--font-mono)', fontWeight: 500 }}
              >
                {ticket.project.code}
              </Box>
              <Box component="span" sx={tagSx}>
                {t(`tickets.teams.${ticket.team}`)}
              </Box>
            </Box>
            <Box
              component="h1"
              sx={{
                margin: 0,
                fontSize: 22,
                lineHeight: '30px',
                fontWeight: 500,
                letterSpacing: '-0.01em',
              }}
            >
              {ticket.title}
            </Box>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 'var(--space-4)',
              }}
            >
              <TicketStatusBadge status={ticket.status} />
              <PriorityTag priority={ticket.priority} />
              <Box component="span" sx={labelSx}>
                {t('tickets.details.openedBy', {
                  name: ticket.requester.name,
                  when: formatDateTime(ticket.openedAt),
                })}
              </Box>
            </Box>
          </Box>
          <Box
            component="section"
            aria-label={t('tickets.details.description')}
            sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}
          >
            <Avatar name={ticket.requester.name} />
            <Box
              sx={{
                flex: 1,
                minWidth: 0,
                padding: 'var(--space-3) var(--space-4) var(--space-4)',
                backgroundColor: 'var(--bg-surface)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius-lg)',
              }}
            >
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-2)',
                  marginBottom: 'var(--space-2)',
                }}
              >
                <Box component="span" sx={{ fontWeight: 500 }}>
                  {ticket.requester.name}
                </Box>
                <Box
                  component="span"
                  sx={{ ...tagSx, height: 20, border: 0, backgroundColor: 'var(--bg-subtle)' }}
                >
                  {t('tickets.details.requesterTag')}
                </Box>
                <Box
                  component="span"
                  sx={{ ...labelSx, marginLeft: 'auto', fontVariantNumeric: 'tabular-nums' }}
                >
                  {formatDateTime(ticket.openedAt)}
                </Box>
              </Box>
              <Box
                sx={{
                  whiteSpace: 'pre-wrap',
                  overflowWrap: 'anywhere',
                  fontSize: 14,
                  lineHeight: '22px',
                }}
              >
                {ticket.description}
              </Box>
            </Box>
          </Box>
        </Box>
        <PeoplePanel ticket={ticket} />
      </Box>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Page>
  );
}
