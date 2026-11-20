import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import Box from '@mui/material/Box';
import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { ScheduleItem } from '../api/client';
import { TicketDue } from '../tickets/TicketDue';

type ScheduleTicket = ScheduleItem['tickets'][number];

const labelSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

const listSx = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-2)',
  margin: 0,
  padding: 0,
  listStyle: 'none',
} as const;

/** A ticket on the schedule: its number and title, leading to its page, and its deadline now (ADR-0025, ADR-0164). */
function TicketLine({ ticket, through }: { ticket: ScheduleTicket; through?: string }) {
  const { t } = useTranslation();
  return (
    <Box component="li" sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-2)' }}>
      <ConfirmationNumberOutlined
        aria-hidden
        sx={{ fontSize: 16, marginTop: '2px', color: 'var(--text-secondary)', flex: 'none' }}
      />
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: '2px', minWidth: 0 }}>
        <Box
          component={Link}
          to={`/tickets/${String(ticket.number)}`}
          sx={{
            fontSize: 14,
            lineHeight: '20px',
            color: 'var(--action-text)',
            textDecoration: 'none',
            overflowWrap: 'anywhere',
            '&:hover': { textDecoration: 'underline' },
            '&:focus-visible': {
              outline: '2px solid var(--focus-ring)',
              borderRadius: 'var(--radius-sm)',
            },
          }}
        >
          {t('projects.schedule.tickets.ticket', { number: ticket.number, title: ticket.title })}
        </Box>
        {ticket.dueState !== null && (
          <TicketDue
            due={{ state: ticket.dueState, dueOn: ticket.dueOn, daysLate: ticket.daysLate }}
          />
        )}
        {through !== undefined && (
          <Box component="span" sx={labelSx}>
            {t('projects.schedule.tickets.through', { items: through })}
          </Box>
        )}
      </Box>
    </Box>
  );
}

/**
 * "Chamados" in the details of a task or milestone (ADR-0164), before the comments, for everyone who sees the schedule:
 * the open tickets linked to the item and, on a milestone not reached, "Ameaçado por", the open tickets that reach it
 * through the dependencies, with the items linked to them that lead there. Nothing without tickets.
 */
export function ItemTickets({
  item,
}: {
  item: Pick<ScheduleItem, 'name' | 'tickets' | 'threats'>;
}) {
  const { t } = useTranslation();
  const headingId = useId();
  if (item.tickets.length === 0 && item.threats.length === 0) {
    return null;
  }
  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        paddingTop: 'var(--space-4)',
        borderTop: '1px solid var(--border)',
      }}
    >
      <Box component="h3" id={headingId} sx={{ ...labelSx, margin: 0, fontWeight: 500 }}>
        {t('projects.schedule.tickets.title')}
      </Box>
      {item.tickets.length > 0 && (
        <Box
          component="ul"
          aria-label={t('projects.schedule.tickets.list', { name: item.name })}
          sx={listSx}
        >
          {item.tickets.map((ticket) => (
            <TicketLine key={ticket.number} ticket={ticket} />
          ))}
        </Box>
      )}
      {item.threats.length > 0 && (
        <Box>
          <Box component="h4" sx={{ ...labelSx, margin: '0 0 var(--space-2)', fontWeight: 500 }}>
            {t('projects.schedule.tickets.threatenedBy')}
          </Box>
          <Box component="ul" aria-label={t('projects.schedule.tickets.threatenedBy')} sx={listSx}>
            {item.threats.map((threat) => (
              <TicketLine
                key={threat.ticket.number}
                ticket={threat.ticket}
                through={threat.through.map((linked) => linked.name).join(', ')}
              />
            ))}
          </Box>
        </Box>
      )}
    </Box>
  );
}
