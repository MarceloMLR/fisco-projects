import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type TicketTeam, ticketProblems } from '../api/client';
import { queryKeys, ticketPeopleChoicesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { Select } from '../components/Select';
import {
  changePeople,
  type PeopleChange,
  problemField,
  ticketChangeProblemText,
} from './ticketChanges';
import { Button } from '../components/Button';

/**
 * A new assignee or requester for one ticket or many (ADR-0027, ADR-0068): the choices come from the API, already
 * filtered to the people who serve all the tickets.
 */
export function PeopleChangeDialog({
  kind,
  numbers,
  onClose,
  onDone,
}: {
  kind: PeopleChange;
  numbers: readonly number[];
  onClose: () => void;
  /** The change was saved, with the message to show. */
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: people, isError } = useQuery(ticketPeopleChoicesQuery(kind, numbers));
  const [personId, setPersonId] = useState('');
  const [refusal, setRefusal] = useState<string>();
  const count = numbers.length;
  const chosen = people?.find((person) => person.id === personId);

  const save = async () => {
    if (chosen === undefined) {
      return;
    }
    setRefusal(undefined);
    try {
      const result = await changePeople(kind, numbers, chosen.id);
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
      if (result.ok) {
        onDone(
          kind === 'assignee'
            ? t('tickets.people.assigned', { count, name: chosen.name })
            : t('tickets.people.requesterChanged', { count, name: chosen.name }),
        );
      } else {
        setRefusal(ticketChangeProblemText(result.status, result.problem, chosen.name, t));
      }
    } catch {
      setRefusal(t('tickets.people.failed'));
    }
  };

  return (
    <Dialog
      open
      title={
        kind === 'assignee'
          ? t('tickets.people.assignTitle', { count })
          : t('tickets.people.requesterTitle', { count })
      }
      onClose={onClose}
      onSubmit={(event) => {
        event.preventDefault();
        return save();
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.people.back')}
          </Button>
          <Button type="submit" variant="contained" disabled={chosen === undefined}>
            {t('tickets.people.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {kind === 'assignee' ? t('tickets.people.assignText') : t('tickets.people.requesterText')}
        </Box>
        {isError && <Alert tone="danger">{t('tickets.people.loadFailed')}</Alert>}
        {people?.length === 0 && <Alert tone="info">{t('tickets.people.none')}</Alert>}
        {people !== undefined && people.length > 0 && (
          <Select<string>
            label={
              kind === 'assignee' ? t('tickets.people.assignee') : t('tickets.people.requester')
            }
            value={personId}
            options={[
              { value: '', label: t('tickets.people.choose'), disabled: true },
              ...people.map((person) => ({
                value: person.id,
                label: person.name,
                detail: person.email ?? undefined,
              })),
            ]}
            onChange={setPersonId}
          />
        )}
        {refusal !== undefined && <Alert tone="danger">{refusal}</Alert>}
      </Box>
    </Dialog>
  );
}

/**
 * Transfers the ticket to the other team (ADR-0068): it is left without an assignee until someone of the team
 * answers.
 */
export function TransferDialog({
  number,
  team,
  onClose,
  onDone,
}: {
  number: number;
  /** The team it goes to. */
  team: TicketTeam;
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [refusal, setRefusal] = useState<string>();
  const name = t(`tickets.teams.${team}`);

  const transfer = async () => {
    setRefusal(undefined);
    try {
      const { response, error } = await api.PUT('/api/tickets/{number}/team', {
        params: { path: { number } },
        body: { team },
      });
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
      if (response.ok) {
        onDone(t('tickets.transfer.done', { team: name }));
      } else {
        setRefusal(
          problemField(error, 'type') === ticketProblems.personRefused
            ? t('tickets.transfer.refused', { team: name })
            : ticketChangeProblemText(response.status, error, name, t),
        );
      }
    } catch {
      setRefusal(t('tickets.transfer.failed'));
    }
  };

  return (
    <Dialog
      open
      title={t('tickets.transfer.title', { team: name })}
      onClose={onClose}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.transfer.back')}
          </Button>
          <Button variant="contained" onClick={() => transfer()}>
            {t('tickets.transfer.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {t('tickets.transfer.text', { team: name })}
        </Box>
        {refusal !== undefined && <Alert tone="danger">{refusal}</Alert>}
      </Box>
    </Dialog>
  );
}
