import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useParams } from 'react-router';
import { api, type Ticket } from '../api/client';
import { ApiError, queryKeys, ticketQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { useDates } from '../format/useDates';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { toneOf } from './conversation';
import { AttachmentChip, PersonAvatar, TicketConversation } from './TicketConversation';
import { TicketReply } from './TicketReply';
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

/** A line of the panel: the label at the left and the value (fp-props). */
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

function SectionTitle({ id, children }: { id: string; children: ReactNode }) {
  return (
    <Box
      component="h2"
      id={id}
      sx={{ margin: '0 0 var(--space-2)', fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
    >
      {children}
    </Box>
  );
}

/**
 * The details beside the conversation (canvas Chamado, fp-pane--side): the people, in the colors of their parts and with
 * who follows the ticket and "Acompanhar" (ADR-0026, ADR-0069); the attachments of the conversation; the project and
 * the last update.
 */
function SidePanel({
  ticket,
  onMessage,
}: {
  ticket: Ticket;
  onMessage: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { formatDateTime } = useDates();
  const queryClient = useQueryClient();
  const [busy, setBusy] = useState(false);
  const roles = new Map(ticket.participants.map((person) => [person.id, person]));
  const toned = (id: string, name: string) => {
    const person = roles.get(id);
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
        <PersonAvatar
          name={name}
          tone={person === undefined ? 'requester' : toneOf[person.role]}
          size="sm"
        />
        <Box
          component="span"
          sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
        >
          {name}
        </Box>
      </Box>
    );
  };
  const people = (list: Ticket['managers']) =>
    list.length === 0 ? (
      <Box component="span" sx={labelSx}>
        {t('tickets.details.none')}
      </Box>
    ) : (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
        {list.map((person) => (
          <Box key={person.id}>{toned(person.id, person.name)}</Box>
        ))}
      </Box>
    );
  const followers = ticket.followers
    .map((id) => roles.get(id))
    .filter((person) => person !== undefined);

  const follow = async () => {
    setBusy(true);
    try {
      const path = { params: { path: { number: ticket.number } } };
      const { response } = ticket.following
        ? await api.DELETE('/api/tickets/{number}/following', path)
        : await api.PUT('/api/tickets/{number}/following', path);
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
    } catch {
      onMessage(t('tickets.panel.followFailed'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box
      component="aside"
      aria-label={t('tickets.details.people')}
      sx={{
        minHeight: 0,
        overflow: 'auto',
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-5)',
        padding: 'var(--space-5)',
        borderLeft: '1px solid var(--border)',
        backgroundColor: 'var(--bg-page)',
      }}
    >
      <Box component="section" aria-labelledby="ticket-people">
        <SectionTitle id="ticket-people">{t('tickets.details.people')}</SectionTitle>
        <Box
          component="dl"
          sx={{ margin: 0, display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}
        >
          <PanelRow label={t('tickets.details.requester')}>
            {toned(ticket.requester.id, ticket.requester.name)}
          </PanelRow>
          <PanelRow label={t('tickets.details.assignee')}>
            {ticket.assignee === null ? (
              <TicketPersonTag name={undefined} />
            ) : (
              toned(ticket.assignee.id, ticket.assignee.name)
            )}
          </PanelRow>
          <PanelRow label={t('tickets.details.team')}>{t(`tickets.teams.${ticket.team}`)}</PanelRow>
          <PanelRow label={t('tickets.details.manager')}>{people(ticket.managers)}</PanelRow>
          <PanelRow label={t('tickets.details.techLead')}>{people(ticket.techLeads)}</PanelRow>
          <PanelRow label={t('tickets.panel.followers')}>
            {followers.length === 0 ? (
              <Box component="span" sx={labelSx}>
                {t('tickets.panel.nobodyFollows')}
              </Box>
            ) : (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
                {followers.map((person) => (
                  <Box key={person.id}>{toned(person.id, person.name)}</Box>
                ))}
              </Box>
            )}
          </PanelRow>
        </Box>
        <Button
          variant="outlined"
          size="small"
          disabled={busy}
          onClick={() => {
            void follow();
          }}
          sx={{ marginTop: 'var(--space-3)' }}
        >
          {ticket.following ? t('tickets.panel.unfollow') : t('tickets.panel.follow')}
        </Button>
      </Box>
      <Box
        component="section"
        aria-labelledby="ticket-attachments"
        sx={{ paddingTop: 'var(--space-4)', borderTop: '1px solid var(--border)' }}
      >
        <SectionTitle id="ticket-attachments">
          {t('tickets.panel.attachments')}
          <Box
            component="span"
            sx={{ marginLeft: 'var(--space-2)', fontWeight: 400, color: 'var(--text-secondary)' }}
          >
            {ticket.attachments.length}
          </Box>
        </SectionTitle>
        {ticket.attachments.length === 0 ? (
          <Box component="span" sx={labelSx}>
            {t('tickets.panel.noAttachments')}
          </Box>
        ) : (
          <Box
            sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: '6px' }}
          >
            {ticket.attachments.map((attachment) => (
              <AttachmentChip key={attachment.id} number={ticket.number} attachment={attachment} />
            ))}
          </Box>
        )}
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
 * The page of a ticket (canvas Chamado), at its number: the header with the number, the code of the project, the team,
 * the title, the status, the priority and who opened it; the conversation, which scrolls inside, with the answer box at
 * the bottom for who sends messages (ADR-0044, ADR-0069); and the details beside it. On the phone the details go under
 * the conversation.
 */
export function TicketPage() {
  const { t } = useTranslation();
  const { number: param } = useParams();
  const location = useLocation();
  const { session } = useSession();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { formatDateTime } = useDates();
  const number = ticketNumberOf(param);
  const me = session.status === 'signed-in' ? session.user.id : '';
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

  const header = (
    <Box
      component="header"
      sx={{
        flex: 'none',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        padding: desktop ? 'var(--space-4) var(--space-6) 14px' : 'var(--space-3)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 'var(--space-2)' }}>
        <Box component="span" sx={{ ...labelSx, fontFamily: 'var(--font-mono)', fontWeight: 500 }}>
          {t('tickets.number', { number: ticket.number })}
        </Box>
        <Box component="span" sx={{ ...tagSx, fontFamily: 'var(--font-mono)', fontWeight: 500 }}>
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
          gap: 'var(--space-2) var(--space-4)',
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
  );

  const conversation = (
    <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      {header}
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-3)',
          padding: desktop ? 'var(--space-2) var(--space-6)' : 'var(--space-2) var(--space-3)',
          borderBottom: '1px solid var(--border)',
          backgroundColor: 'var(--bg-subtle)',
        }}
      >
        <Box component="h2" sx={{ margin: 0, fontSize: 14, lineHeight: '20px', fontWeight: 500 }}>
          {t('tickets.conversation.title')}
          <Box
            component="span"
            sx={{ marginLeft: 'var(--space-2)', fontWeight: 400, color: 'var(--text-secondary)' }}
          >
            {t('tickets.conversation.interactions', { count: ticket.messages.length })}
          </Box>
        </Box>
      </Box>
      <Box
        tabIndex={0}
        aria-label={t('tickets.conversation.title')}
        sx={{
          flex: 1,
          minHeight: desktop ? 0 : 320,
          overflow: 'auto',
          padding: desktop ? 'var(--space-5) var(--space-6)' : 'var(--space-3)',
          '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: -2 },
        }}
      >
        <TicketConversation ticket={ticket} me={me} />
      </Box>
      {ticket.permissions.canSend && (
        <Box
          sx={{
            flex: 'none',
            padding: desktop ? 'var(--space-3) var(--space-6)' : 'var(--space-3)',
            borderTop: '1px solid var(--border)',
            backgroundColor: 'var(--bg-page)',
          }}
        >
          <TicketReply ticket={ticket} me={me} />
        </Box>
      )}
    </Box>
  );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: desktop ? 'grid' : 'flex',
        flexDirection: 'column',
        gridTemplateColumns: 'minmax(0, 1fr) 340px',
        overflow: desktop ? 'hidden' : 'auto',
      }}
    >
      {conversation}
      <SidePanel ticket={ticket} onMessage={setMessage} />
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Box>
  );
}
