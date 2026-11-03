import LockOutlined from '@mui/icons-material/LockOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { Fragment, type ReactNode, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { TeamsTicket as Ticket } from '../api/client';
import { Avatar } from '../components/Avatar';
import { Button } from '../components/Button';
import { CodeChip } from '../components/Card';
import { RichText } from '../components/RichText';
import { useDates } from '../format/useDates';
import {
  type ConversationPerson,
  ConversationMessage,
  DaySeparator,
  EventLine,
  FileNameChip,
} from '../tickets/TicketConversation';
import { useConversationDays } from '../tickets/conversationDays';
import { TicketDue } from '../tickets/TicketDue';
import { PriorityTag, TicketStatusBadge } from '../tickets/TicketTags';

type Conversation = Ticket['conversation'];
type Item =
  | { kind: 'description'; at: string }
  | { kind: 'message'; at: string; message: Conversation['messages'][number] }
  | { kind: 'event'; at: string; event: Conversation['events'][number] };

const finished: readonly Ticket['status'][] = ['Resolved', 'Closed'];

const secondarySx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/** The interactions shown, in the order of the conversation: the description first, then by time (ADR-0069). */
function itemsOf(ticket: Ticket): Item[] {
  const { conversation } = ticket;
  const rest: Item[] = [
    ...conversation.messages.map((message) => ({
      kind: 'message' as const,
      at: message.sentAt,
      message,
    })),
    ...conversation.events.map((event) => ({ kind: 'event' as const, at: event.at, event })),
  ];
  // Stable: at the same moment, the message before the change it made, as in the page of the ticket.
  rest.sort((a, b) => Date.parse(a.at) - Date.parse(b.at));
  return conversation.description === null
    ? rest
    : [{ kind: 'description', at: ticket.openedAt }, ...rest];
}

/** The key of an interaction: the id of the message or of the event, or the kind of the description. */
const keyOf = (item: Item) =>
  item.kind === 'message' ? item.message.id : item.kind === 'event' ? item.event.id : item.kind;

/** "Responder no FiscoProjects": the ticket in the browser, where the person answers (ADR-0152). */
function ReplyButton({
  number,
  name,
  block = false,
}: {
  number: number;
  name: string;
  block?: boolean;
}) {
  const { t } = useTranslation();
  return (
    <Button
      variant="contained"
      endIcon={<OpenInNewOutlined />}
      href={`/tickets/${String(number)}`}
      target="_blank"
      rel="noopener noreferrer"
      fullWidth={block}
      sx={{ flex: 'none' }}
    >
      {t('teamsPage.ticket.reply', { name })}
    </Button>
  );
}

/** The last interactions of the conversation, with how many stay in FiscoProjects (Message.Gap, Message.Timeline). */
function TeamsConversation({
  ticket,
  name,
  narrow = false,
}: {
  ticket: Ticket;
  name: string;
  narrow?: boolean;
}) {
  const { t } = useTranslation();
  const { dayOf, dayText } = useConversationDays();
  const { conversation } = ticket;
  const people = useMemo(
    () => new Map(conversation.participants.map((person) => [person.id, person])),
    [conversation.participants],
  );
  const personOf = (id: string): ConversationPerson =>
    people.get(id) ?? { id, name: '', role: 'Other' };
  const filesOf = (messageId: string | null) => {
    const files = conversation.attachments.filter((file) => file.messageId === messageId);
    return files.length === 0 ? null : (
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 'var(--space-2)',
          marginTop: 'var(--space-3)',
        }}
      >
        {files.map((file, index) => (
          <FileNameChip key={`${file.name}-${String(index)}`} file={file} />
        ))}
      </Box>
    );
  };
  const items = itemsOf(ticket);
  const requester: ConversationPerson = {
    ...personOf(ticket.requester.id),
    name: ticket.requester.name,
    role: 'Requester',
  };
  return (
    <Box
      component="ol"
      aria-label={t('teamsPage.ticket.conversation')}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        margin: 0,
        padding: 0,
        listStyle: 'none',
      }}
    >
      {conversation.earlier > 0 && (
        <Box
          component="li"
          sx={{
            padding: 'var(--space-2) var(--space-3)',
            border: '1px dashed var(--border-control)',
            borderRadius: 'var(--radius-md)',
            textAlign: 'center',
            ...secondarySx,
          }}
        >
          {t('teamsPage.ticket.earlier', { count: conversation.earlier, name })}
        </Box>
      )}
      {items.map((item, index) => {
        const previous = items[index - 1];
        const separator = (previous === undefined || dayOf(previous.at) !== dayOf(item.at)) && (
          <DaySeparator>{dayText(item.at)}</DaySeparator>
        );
        let content: ReactNode;
        if (item.kind === 'event') {
          content = (
            <EventLine
              event={item.event}
              personOf={personOf}
              vendorTickets={conversation.vendorTickets}
            />
          );
        } else if (item.kind === 'message') {
          content = (
            <ConversationMessage
              at={item.message.sentAt}
              author={personOf(item.message.authorId)}
              me={ticket.viewerId}
              body={<RichText value={item.message.body} />}
              files={filesOf(item.message.id)}
              plainAvatar
              inset={narrow ? 'var(--space-6)' : undefined}
            />
          );
        } else {
          const description = conversation.description;
          content = (
            <ConversationMessage
              at={ticket.openedAt}
              author={requester}
              me={ticket.viewerId}
              body={
                description === null ? null : description.body === null ? (
                  <Box sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
                    {description.text}
                  </Box>
                ) : (
                  <RichText value={description.body} />
                )
              }
              files={filesOf(null)}
              plainAvatar
              inset={narrow ? 'var(--space-6)' : undefined}
            />
          );
        }
        return (
          <Fragment key={keyOf(item)}>
            {separator}
            {content}
          </Fragment>
        );
      })}
    </Box>
  );
}

/** A section of the column at the right (fp-props). */
function Section({ id, title, children }: { id: string; title: string; children: ReactNode }) {
  return (
    <Box
      component="section"
      aria-labelledby={id}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
    >
      <Box component="h2" id={id} sx={{ margin: 0, fontWeight: 500, ...secondarySx }}>
        {title}
      </Box>
      {children}
    </Box>
  );
}

/** Lines of a label and a value (fp-kv). */
function KeyValues({ rows }: { rows: readonly (readonly [string, ReactNode])[] }) {
  return (
    <Box
      component="dl"
      sx={{
        display: 'grid',
        gridTemplateColumns: 'max-content minmax(0, 1fr)',
        columnGap: 'var(--space-4)',
        rowGap: 'var(--space-2)',
        margin: 0,
        '& dt': secondarySx,
        '& dd': { margin: 0, minWidth: 0, overflowWrap: 'anywhere' },
      }}
    >
      {rows.map(([label, value]) => (
        <Fragment key={label}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </Fragment>
      ))}
    </Box>
  );
}

/** A person of the ticket with the avatar in the color of the part and the part under the name. */
function PersonLine({
  id,
  name,
  part,
  tone,
  you,
}: {
  id: string;
  name: string;
  part: string;
  tone: 'requester' | 'owner' | 'manager';
  you: boolean;
}) {
  const { t } = useTranslation();
  return (
    <Box data-person={id} sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)' }}>
      <Avatar name={name} size="sm" tone={tone} />
      <Box sx={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span>
          {name}
          {you && (
            <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
              {` ${t('teamsPage.ticket.you')}`}
            </Box>
          )}
        </span>
        <Box
          component="span"
          sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
        >
          {part}
        </Box>
      </Box>
    </Box>
  );
}

/** The deadlines of the ticket today (ADR-0025), as in the page of the ticket. */
function Deadlines({ ticket }: { ticket: Ticket }) {
  const { t } = useTranslation();
  const { formatDayMonth, formatTime } = useDates();
  const { deadlines } = ticket;
  if (deadlines === null) {
    return <Box sx={secondarySx}>{t('teamsPage.ticket.noDeadlines')}</Box>;
  }
  const { firstResponse, resolution, firstRespondedAt } = deadlines;
  return (
    <>
      {firstResponse !== null && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
          <span>{t('teamsPage.ticket.firstResponse')}</span>
          <TicketDue
            due={firstResponse}
            metText={
              firstRespondedAt === null
                ? undefined
                : t('tickets.due.firstMet', {
                    day: formatDayMonth(firstRespondedAt),
                    time: formatTime(firstRespondedAt),
                  })
            }
          />
        </Box>
      )}
      {resolution !== null && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
          <span>{t('teamsPage.ticket.resolution')}</span>
          <TicketDue due={resolution} finished={finished.includes(ticket.status)} />
        </Box>
      )}
    </>
  );
}

/**
 * The ticket of the notice in the page of Teams, only to read (canvas "Chamado no Teams", ADR-0152): on the computer, the
 * header with "Responder no FiscoProjects", the last interactions and the column with the people, the deadlines and the
 * ticket; up to 600px, one column, with the button at the foot. The page does not scroll; its parts do (ADR-0066).
 */
export function TeamsTicketView({ ticket, name }: { ticket: Ticket; name: string }) {
  const { t } = useTranslation();
  const { formatDateTime } = useDates();
  const phone = useMediaQuery('(max-width: 600px)', { noSsr: true });
  const you = (id: string) => id === ticket.viewerId;
  const resolution = ticket.deadlines?.resolution ?? null;
  const due = resolution !== null && !finished.includes(ticket.status) && (
    <TicketDue due={resolution} />
  );
  const team = t(`tickets.teams.${ticket.team}`);
  const number = t('tickets.number', { number: ticket.number });
  const attachments =
    ticket.attachmentCount === 0
      ? t('teamsPage.ticket.noAttachments')
      : t('teamsPage.ticket.attachmentCount', { count: ticket.attachmentCount });
  const scrolls = { minHeight: 0, overflow: 'auto' } as const;

  if (phone) {
    return (
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        <Box
          component="header"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-2)',
            padding: 'var(--space-4)',
            borderBottom: '1px solid var(--border)',
          }}
        >
          <Box
            sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', ...secondarySx }}
          >
            <CodeChip code={ticket.project.code} />
            <span>{number}</span>
          </Box>
          <Box
            component="h1"
            sx={{
              margin: 0,
              fontSize: 20,
              lineHeight: '28px',
              fontWeight: 500,
              overflowWrap: 'anywhere',
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
          </Box>
          {due}
        </Box>
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            ...scrolls,
            padding: 'var(--space-4)',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
          }}
        >
          <KeyValues
            rows={[
              [
                t('teamsPage.ticket.assignee'),
                ticket.assignee === null
                  ? t('teamsPage.ticket.noAssignee')
                  : `${ticket.assignee.name}${you(ticket.assignee.id) ? ` ${t('teamsPage.ticket.you')}` : ''}`,
              ],
              [
                t('teamsPage.ticket.requester'),
                `${ticket.requester.name}${you(ticket.requester.id) ? ` ${t('teamsPage.ticket.you')}` : ''}`,
              ],
              [t('teamsPage.ticket.team'), team],
            ]}
          />
          <TeamsConversation ticket={ticket} name={name} narrow />
        </Box>
        <Box
          component="footer"
          sx={{
            padding: 'var(--space-3) var(--space-4) var(--space-4)',
            borderTop: '1px solid var(--border)',
            backgroundColor: 'var(--bg-page)',
          }}
        >
          <ReplyButton number={ticket.number} name={name} block />
        </Box>
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Box
        component="header"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-2)',
          padding: 'var(--space-5) var(--space-6) var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            minWidth: 0,
            ...secondarySx,
          }}
        >
          <CodeChip code={ticket.project.code} />
          <Box
            component="span"
            sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
          >
            {ticket.project.name}
          </Box>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-4)' }}>
          <Box
            component="h1"
            sx={{
              flexGrow: 1,
              minWidth: 0,
              margin: 0,
              fontSize: 24,
              lineHeight: '32px',
              fontWeight: 500,
              overflowWrap: 'anywhere',
            }}
          >
            <Box component="span" sx={{ color: 'var(--text-secondary)', fontWeight: 400 }}>
              {number}
            </Box>{' '}
            {ticket.title}
          </Box>
          <ReplyButton number={ticket.number} name={name} />
        </Box>
        <Box
          sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 'var(--space-4)' }}
        >
          <TicketStatusBadge status={ticket.status} />
          <PriorityTag priority={ticket.priority} />
          {due}
        </Box>
      </Box>
      <Box sx={{ flexGrow: 1, minHeight: 0, display: 'flex' }}>
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            minWidth: 0,
            ...scrolls,
            padding: 'var(--space-6)',
          }}
        >
          <Box sx={{ maxWidth: 960, margin: '0 auto' }}>
            <TeamsConversation ticket={ticket} name={name} />
          </Box>
        </Box>
        <Box
          component="aside"
          sx={{
            flex: 'none',
            width: 320,
            boxSizing: 'border-box',
            ...scrolls,
            padding: 'var(--space-6)',
            backgroundColor: 'var(--bg-subtle)',
            borderLeft: '1px solid var(--border)',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-6)',
          }}
        >
          <Section id="teams-ticket-people" title={t('teamsPage.ticket.people')}>
            <PersonLine
              id={ticket.requester.id}
              name={ticket.requester.name}
              part={t('teamsPage.ticket.requester')}
              tone="requester"
              you={you(ticket.requester.id)}
            />
            {ticket.assignee === null ? (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)' }}>
                <Avatar size="sm" />
                <Box sx={secondarySx}>{t('teamsPage.ticket.noAssignee')}</Box>
              </Box>
            ) : (
              <PersonLine
                id={ticket.assignee.id}
                name={ticket.assignee.name}
                part={t('teamsPage.ticket.assignee')}
                tone="owner"
                you={you(ticket.assignee.id)}
              />
            )}
            {ticket.managers.map((manager) => (
              <PersonLine
                key={manager.id}
                id={manager.id}
                name={manager.name}
                part={t('teamsPage.ticket.manager')}
                tone="manager"
                you={you(manager.id)}
              />
            ))}
          </Section>
          <Section id="teams-ticket-deadlines" title={t('teamsPage.ticket.deadlines')}>
            <Deadlines ticket={ticket} />
          </Section>
          <Section id="teams-ticket-about" title={t('teamsPage.ticket.about')}>
            <KeyValues
              rows={[
                [t('teamsPage.ticket.team'), team],
                [t('teamsPage.ticket.openedAt'), formatDateTime(ticket.openedAt)],
                [t('teamsPage.ticket.attachments'), attachments],
              ]}
            />
          </Section>
        </Box>
      </Box>
    </Box>
  );
}

/** The ticket the person does not see, or that does not exist (canvas "Chamado que a pessoa não vê", ADR-0152). */
export function TeamsNoAccess({ path, name }: { path: string; name: string }) {
  const { t } = useTranslation();
  return (
    <Box
      sx={{
        minHeight: '100%',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 'var(--space-4)',
        padding: 'var(--space-6)',
        textAlign: 'center',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: 48,
          height: 48,
          borderRadius: '50%',
          backgroundColor: 'var(--bg-subtle)',
          color: 'var(--text-secondary)',
          '& svg': { fontSize: 24 },
        }}
      >
        <LockOutlined aria-hidden />
      </Box>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', maxWidth: 360 }}>
        <Box component="h1" sx={{ margin: 0, fontSize: 18, lineHeight: '26px', fontWeight: 500 }}>
          {t('teamsPage.noAccess.title')}
        </Box>
        <Box component="p" sx={{ margin: 0, ...secondarySx, fontSize: 14, lineHeight: '20px' }}>
          {t('teamsPage.noAccess.text')}
        </Box>
      </Box>
      <Button
        variant="outlined"
        endIcon={<OpenInNewOutlined />}
        href={path}
        target="_blank"
        rel="noopener noreferrer"
      >
        {t('teamsPage.open', { name })}
      </Button>
    </Box>
  );
}
