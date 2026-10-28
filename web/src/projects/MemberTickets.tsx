import SwapHorizOutlined from '@mui/icons-material/SwapHorizOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { TicketRow } from '../api/client';
import { queryKeys, ticketPeopleChoicesQuery, ticketsQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { Select } from '../components/Select';
import { changePeople, ticketChangeProblemText } from '../tickets/ticketChanges';
import { TicketStatusBadge } from '../tickets/TicketTags';

interface Person {
  id: string;
  name: string;
}

const tagSx = {
  display: 'inline-flex',
  alignItems: 'center',
  height: 20,
  padding: '0 6px',
  borderRadius: 'var(--radius-sm)',
  backgroundColor: 'var(--bg-subtle)',
  color: 'var(--text-secondary)',
  fontSize: 12,
  lineHeight: '16px',
  fontWeight: 500,
} as const;

/** The choice of the new person for some of the tickets, with "Manter como está" first. */
function HandOverField({
  kind,
  numbers,
  label,
  value,
  onChange,
}: {
  kind: 'assignee' | 'requester';
  numbers: readonly number[];
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  const { t } = useTranslation();
  const { data: people } = useQuery(ticketPeopleChoicesQuery(kind, numbers));
  return (
    <Select<string>
      label={label}
      value={value}
      options={[
        { value: '', label: t('projects.members.tickets.keep') },
        ...(people ?? []).map((person) => ({
          value: person.id,
          label: person.name,
          detail: person.email ?? undefined,
        })),
      ]}
      onChange={onChange}
    />
  );
}

/**
 * "Passar pendências" (canvas ProjetoMembros, ADR-0042): the open tickets of the person go to the people chosen, as
 * the assignee and as the requester; what is left as it is stays with the person.
 */
function HandOverDialog({
  member,
  tickets,
  onClose,
  onDone,
}: {
  member: Person;
  tickets: readonly TicketRow[];
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const assigned = tickets
    .filter((ticket) => ticket.assignee?.id === member.id)
    .map((ticket) => ticket.number);
  const opened = tickets
    .filter((ticket) => ticket.requester.id === member.id)
    .map((ticket) => ticket.number);
  const [assignee, setAssignee] = useState('');
  const [requester, setRequester] = useState('');
  const [busy, setBusy] = useState(false);
  const [refusal, setRefusal] = useState<string>();

  const handOver = async () => {
    setBusy(true);
    setRefusal(undefined);
    try {
      for (const [kind, numbers, personId] of [
        ['assignee', assigned, assignee],
        ['requester', opened, requester],
      ] as const) {
        if (personId === '' || numbers.length === 0) {
          continue;
        }
        const result = await changePeople(kind, numbers, personId);
        if (!result.ok) {
          setRefusal(ticketChangeProblemText(result.status, result.problem, member.name, t));
          return;
        }
      }
      onDone(t('projects.members.tickets.done', { name: member.name }));
    } catch {
      setRefusal(t('projects.members.tickets.failed'));
    } finally {
      setBusy(false);
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
    }
  };

  return (
    <Dialog
      open
      title={t('projects.members.tickets.handOverTitle', { name: member.name })}
      onClose={onClose}
      onSubmit={(event) => {
        event.preventDefault();
        void handOver();
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.people.back')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={busy || (assignee === '' && requester === '')}
          >
            {t('projects.members.tickets.handOver')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {t('projects.members.tickets.handOverText', { name: member.name })}
        </Box>
        {assigned.length > 0 && (
          <HandOverField
            kind="assignee"
            numbers={assigned}
            label={t('projects.members.tickets.newAssignee', { count: assigned.length })}
            value={assignee}
            onChange={setAssignee}
          />
        )}
        {opened.length > 0 && (
          <HandOverField
            kind="requester"
            numbers={opened}
            label={t('projects.members.tickets.newRequester', { count: opened.length })}
            value={requester}
            onChange={setRequester}
          />
        )}
        {refusal !== undefined && <Alert tone="danger">{refusal}</Alert>}
      </Box>
    </Dialog>
  );
}

/**
 * The open tickets of a person in the project (canvas ProjetoMembros, ADR-0042), as the assignee or as the requester,
 * with "Passar pendências" for who manages the people.
 */
export function MemberTickets({
  projectId,
  member,
  canHandOver,
  onDone,
}: {
  projectId: string;
  member: Person;
  canHandOver: boolean;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const [handingOver, setHandingOver] = useState(false);
  const { data, isError } = useQuery(
    ticketsQuery({ projectId, personId: member.id, situation: 'Open', pageSize: 100 }),
  );
  const tickets = data?.items ?? [];

  return (
    <Box
      component="section"
      aria-labelledby="member-tickets"
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
    >
      <Box
        component="h3"
        id="member-tickets"
        sx={{ margin: 0, fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
      >
        {t('projects.members.tickets.title')}
        <Box
          component="span"
          sx={{ marginLeft: 'var(--space-2)', fontWeight: 400, color: 'var(--text-secondary)' }}
        >
          {data?.total ?? ''}
        </Box>
      </Box>
      {isError && <Alert tone="danger">{t('projects.members.tickets.loadFailed')}</Alert>}
      {data !== undefined && tickets.length === 0 && (
        <Box component="span" sx={{ fontSize: 13, color: 'var(--text-secondary)' }}>
          {t('projects.members.tickets.none')}
        </Box>
      )}
      {tickets.length > 0 && (
        <Box
          component="ul"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: '10px',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          {tickets.map((ticket) => (
            <li key={ticket.id}>
              <Box
                component={Link}
                to={`/tickets/${String(ticket.number)}`}
                sx={{
                  display: 'block',
                  fontSize: 14,
                  lineHeight: '20px',
                  fontWeight: 500,
                  color: 'var(--text)',
                  textDecoration: 'none',
                  '&:hover': { textDecoration: 'underline' },
                }}
              >
                {t('tickets.number', { number: ticket.number })} {ticket.title}
              </Box>
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: 'var(--space-2)',
                  marginTop: 'var(--space-1)',
                }}
              >
                <TicketStatusBadge status={ticket.status} />
                {ticket.assignee?.id === member.id && (
                  <Box component="span" sx={tagSx}>
                    {t('projects.members.tickets.assignee')}
                  </Box>
                )}
                {ticket.requester.id === member.id && (
                  <Box component="span" sx={tagSx}>
                    {t('projects.members.tickets.requester')}
                  </Box>
                )}
              </Box>
            </li>
          ))}
        </Box>
      )}
      {canHandOver && tickets.length > 0 && (
        <Box>
          <Button
            variant="outlined"
            size="small"
            startIcon={<SwapHorizOutlined />}
            onClick={() => {
              setHandingOver(true);
            }}
          >
            {t('projects.members.tickets.handOver')}
          </Button>
        </Box>
      )}
      {handingOver && (
        <HandOverDialog
          member={member}
          tickets={tickets}
          onClose={() => {
            setHandingOver(false);
          }}
          onDone={(message) => {
            setHandingOver(false);
            onDone(message);
          }}
        />
      )}
    </Box>
  );
}
