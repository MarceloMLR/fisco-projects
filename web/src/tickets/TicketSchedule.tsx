import AccountTreeOutlined from '@mui/icons-material/AccountTreeOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import LinkOutlined from '@mui/icons-material/LinkOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { type SubmitEvent, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { api, type Ticket, ticketProblems } from '../api/client';
import { projectScheduleQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { MultiSelect } from '../components/MultiSelect';
import { formatDayText, formatPeriodText } from '../format/dates';
import { useNotify } from '../notices/notify';
import { itemsOf } from '../projects/schedule';
import { problemField } from './ticketChanges';

/** Up to 10 tasks and milestones for a ticket (ADR-0164). */
const maxImpacts = 10;

type ScheduleSection = Ticket['schedule'];
type LinkedItem = ScheduleSection['items'][number];

const labelSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

const linkSx = {
  color: 'var(--action-text)',
  textDecoration: 'none',
  overflowWrap: 'anywhere',
  '&:hover': { textDecoration: 'underline' },
  '&:focus-visible': { outline: '2px solid var(--focus-ring)', borderRadius: 'var(--radius-sm)' },
} as const;

/** Where a task or milestone of the project opens, with its details (Cronograma-3). */
function scheduleItemPath(projectId: string, itemId: string): string {
  return `/projects/${projectId}/schedule?item=${itemId}`;
}

/** Why the API refused the items, in words of the screen. */
function refusalText(status: number, problem: unknown, t: TFunction): string {
  const type = problemField(problem, 'type');
  if (type === ticketProblems.impactRefused) {
    return t('tickets.schedule.messages.refused');
  }
  if (type === ticketProblems.ticketClosed) {
    return t('tickets.schedule.messages.closed');
  }
  if (type === ticketProblems.ticketChanged) {
    return t('tickets.schedule.messages.changed');
  }
  if (status === 403 || status === 404) {
    return t('tickets.schedule.messages.forbidden');
  }
  return t('tickets.schedule.messages.failed');
}

/** How a linked item stands: done, late by the business days the API counts, or its dates. */
function ItemState({ item }: { item: LinkedItem }) {
  const { t } = useTranslation();
  if (item.isDone) {
    return (
      <DueDate state="met">
        {t(item.type === 'Milestone' ? 'tickets.schedule.reached' : 'tickets.schedule.done')}
      </DueDate>
    );
  }
  if (item.lateDays > 0) {
    return (
      <DueDate state="overdue">{t('projects.schedule.lateDays', { count: item.lateDays })}</DueDate>
    );
  }
  return (
    <Box component="span" sx={{ ...labelSx, fontVariantNumeric: 'tabular-nums' }}>
      {item.type === 'Milestone'
        ? t('tickets.schedule.milestoneDay', { day: formatDayText(item.start) })
        : formatPeriodText(item.start, item.end)}
    </Box>
  );
}

/**
 * "Ligar ao cronograma" (ADR-0164): the tasks, also the summaries and the subtasks, and the milestones of the schedule of
 * the project, in its order and under their phases, up to 10; none unlinks the ticket.
 */
function ImpactsDialog({
  ticket,
  onClose,
  onDone,
}: {
  ticket: Ticket;
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: schedule, isError } = useQuery(projectScheduleQuery(ticket.project.id));
  const [chosen, setChosen] = useState<string[]>(() =>
    ticket.schedule.items.map((item) => item.id),
  );
  const [error, setError] = useState<string>();
  const options = useMemo(() => {
    if (schedule === undefined) {
      return [];
    }
    const phases = new Map(schedule.phases.map((phase) => [phase.id, phase.name]));
    return itemsOf(schedule).map((item) => ({
      value: item.id,
      label: item.name,
      group: phases.get(item.phaseId),
    }));
  }, [schedule]);
  const tooMany = chosen.length > maxImpacts;

  const save = async (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (tooMany) {
      return;
    }
    setError(undefined);
    try {
      const { response, error: problem } = await api.PUT('/api/tickets/{number}/impacts', {
        params: { path: { number: ticket.number } },
        body: { itemIds: chosen },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
        // The schedule and the overview of the project show the ticket on its items and milestones.
        void queryClient.invalidateQueries({
          queryKey: [...queryKeys.projects, ticket.project.id],
        });
        onDone(
          t(
            chosen.length === 0
              ? 'tickets.schedule.messages.unlinked'
              : 'tickets.schedule.messages.saved',
            {
              number: ticket.number,
            },
          ),
        );
        return;
      }
      setError(refusalText(response.status, problem, t));
    } catch {
      setError(t('tickets.schedule.messages.failed'));
    }
  };

  return (
    <Dialog
      open
      title={t('tickets.schedule.dialog.title')}
      subtitle={t('tickets.schedule.dialog.subtitle', { number: ticket.number })}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.schedule.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={schedule === undefined}>
            {t('tickets.schedule.dialog.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {isError && <Alert tone="danger">{t('tickets.schedule.messages.loadFailed')}</Alert>}
        <MultiSelect
          label={t('tickets.schedule.dialog.items')}
          value={chosen}
          options={options}
          onChange={setChosen}
          noOptionsText={t('tickets.schedule.dialog.noItems')}
          helper={tooMany ? undefined : t('tickets.schedule.dialog.help', { count: maxImpacts })}
          error={tooMany ? t('tickets.schedule.dialog.tooMany', { count: maxImpacts }) : undefined}
          disabled={schedule === undefined}
        />
        {error !== undefined && <Alert tone="danger">{error}</Alert>}
      </Box>
    </Dialog>
  );
}

/**
 * "No cronograma" (ADR-0164), after "No fornecedor": the tasks and milestones the ticket impacts, each with its kind, its
 * dates and its delay, leading to its details in the schedule; the milestones not reached that depend on them
 * ("Ameaça"), with their days; and the warning of the critical path. Who serves the ticket links it ("Ligar ao
 * cronograma") or changes the items ("Alterar") until it is closed; nothing changes a date or a deadline.
 */
export function ScheduleSection({ ticket }: { ticket: Ticket }) {
  const { t } = useTranslation();
  const notify = useNotify();
  const [changing, setChanging] = useState(false);
  const { items, milestones, onCriticalPath, canChange } = ticket.schedule;

  return (
    <Box
      component="section"
      aria-labelledby="ticket-schedule"
      sx={{ paddingTop: 'var(--space-4)', borderTop: '1px solid var(--border)' }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-2)',
          minHeight: 28,
          marginBottom: 'var(--space-2)',
        }}
      >
        <Box
          component="h2"
          id="ticket-schedule"
          sx={{ margin: 0, fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
        >
          {t('tickets.schedule.title')}
        </Box>
        {canChange && (
          <Button
            variant="text"
            size="small"
            startIcon={items.length === 0 ? <LinkOutlined /> : <EditOutlined />}
            onClick={() => {
              setChanging(true);
            }}
          >
            {items.length === 0 ? t('tickets.schedule.link') : t('tickets.schedule.change')}
          </Button>
        )}
      </Box>
      {items.length === 0 ? (
        <Box component="span" sx={labelSx}>
          {t('tickets.schedule.none')}
        </Box>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          <Box
            component="ul"
            aria-label={t('tickets.schedule.items')}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-2)',
              margin: 0,
              padding: 0,
              listStyle: 'none',
            }}
          >
            {items.map((item) => {
              const Icon = item.type === 'Milestone' ? FlagOutlined : ViewTimelineOutlined;
              return (
                <Box
                  component="li"
                  key={item.id}
                  sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-2)' }}
                >
                  <Icon
                    role="img"
                    aria-hidden={false}
                    aria-label={t(
                      `tickets.schedule.kinds.${item.type === 'Milestone' ? 'Milestone' : 'Task'}`,
                    )}
                    sx={{
                      fontSize: 16,
                      marginTop: '2px',
                      color: 'var(--text-secondary)',
                      flex: 'none',
                    }}
                  />
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: '2px', minWidth: 0 }}>
                    <Box
                      component={Link}
                      to={scheduleItemPath(ticket.project.id, item.id)}
                      sx={{ ...linkSx, fontSize: 14, lineHeight: '20px', fontWeight: 500 }}
                    >
                      {item.name}
                    </Box>
                    <ItemState item={item} />
                  </Box>
                </Box>
              );
            })}
          </Box>
          {milestones.length > 0 && (
            <Box>
              <Box
                component="h3"
                sx={{ ...labelSx, margin: '0 0 var(--space-1)', fontWeight: 500 }}
              >
                {t('tickets.schedule.threatens')}
              </Box>
              <Box
                component="ul"
                aria-label={t('tickets.schedule.threatens')}
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 'var(--space-1)',
                  margin: 0,
                  padding: 0,
                  listStyle: 'none',
                }}
              >
                {milestones.map((milestone) => (
                  <Box
                    component="li"
                    key={milestone.id}
                    sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-2)' }}
                  >
                    <FlagOutlined
                      aria-hidden
                      sx={{
                        fontSize: 16,
                        marginTop: '2px',
                        color: 'var(--text-secondary)',
                        flex: 'none',
                      }}
                    />
                    <Box
                      sx={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        columnGap: 'var(--space-2)',
                        minWidth: 0,
                      }}
                    >
                      <Box
                        component={Link}
                        to={scheduleItemPath(ticket.project.id, milestone.id)}
                        sx={{ ...linkSx, fontSize: 13, lineHeight: '18px' }}
                      >
                        {milestone.name}
                      </Box>
                      {milestone.lateDays > 0 ? (
                        <DueDate state="overdue">
                          {t('projects.schedule.lateDays', { count: milestone.lateDays })}
                        </DueDate>
                      ) : (
                        <Box
                          component="span"
                          sx={{ ...labelSx, fontVariantNumeric: 'tabular-nums' }}
                        >
                          {formatDayText(milestone.day)}
                        </Box>
                      )}
                    </Box>
                  </Box>
                ))}
              </Box>
            </Box>
          )}
          {onCriticalPath && (
            <Box
              component="p"
              data-state="critical"
              sx={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: 'var(--space-1)',
                margin: 0,
                fontSize: 13,
                lineHeight: '18px',
                fontWeight: 500,
                color: 'var(--status-warning-text)',
              }}
            >
              <AccountTreeOutlined
                aria-hidden
                sx={{ fontSize: 16, marginTop: '1px', flex: 'none' }}
              />
              {t('tickets.schedule.critical')}
            </Box>
          )}
        </Box>
      )}
      {changing && (
        <ImpactsDialog
          ticket={ticket}
          onClose={() => {
            setChanging(false);
          }}
          onDone={(message) => {
            setChanging(false);
            notify(message);
          }}
        />
      )}
    </Box>
  );
}
