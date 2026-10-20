import DescriptionOutlined from '@mui/icons-material/DescriptionOutlined';
import ImageOutlined from '@mui/icons-material/ImageOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { Fragment, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Ticket } from '../api/client';
import { Avatar } from '../components/Avatar';
import { RichText } from '../components/RichText';
import { useDates } from '../format/useDates';
import {
  attachmentUrl,
  isImage,
  type MessageTone,
  type ParticipantRole,
  shownInteractions,
  sizeText,
  toneOf,
} from './conversation';

type Attachment = Ticket['attachments'][number];
type Participant = Ticket['participants'][number];

/** A file of the conversation (Message.Attachment): opens in a new tab, images and PDF in the browser. */
export function AttachmentChip({ number, attachment }: { number: number; attachment: Attachment }) {
  const Icon = isImage(attachment.contentType) ? ImageOutlined : DescriptionOutlined;
  return (
    <Box
      component="a"
      href={attachmentUrl(number, attachment.id)}
      target="_blank"
      rel="noopener noreferrer"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        maxWidth: '100%',
        boxSizing: 'border-box',
        height: 32,
        padding: '0 var(--space-3)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
        color: 'var(--text)',
        fontSize: 13,
        textDecoration: 'none',
        '&:hover': { backgroundColor: 'var(--bg-hover)' },
        '& > svg': { fontSize: 18, color: 'var(--text-secondary)', flex: 'none' },
      }}
    >
      <Icon aria-hidden />
      <Box
        component="span"
        sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
      >
        {attachment.name}
      </Box>
      <Box
        component="span"
        sx={{ flex: 'none', color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}
      >
        {sizeText(attachment.size)}
      </Box>
    </Box>
  );
}

/** The tag of a part (Message.Role): the color of the part and its name, so color is never alone (ADR-0065). */
export function RoleTag({ role }: { role: ParticipantRole }) {
  const { t } = useTranslation();
  const tone = toneOf[role];
  const neutral = tone === 'requester';
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        height: 20,
        padding: '0 6px',
        borderRadius: 'var(--radius-sm)',
        backgroundColor: neutral ? 'var(--bg-subtle)' : `var(--role-${tone}-bg)`,
        color: neutral ? 'var(--text-secondary)' : `var(--role-${tone}-text)`,
        fontSize: 12,
        lineHeight: '16px',
        fontWeight: 500,
        whiteSpace: 'nowrap',
      }}
    >
      {t(`tickets.roles.${role}`)}
    </Box>
  );
}

/** The avatar of a person in the color of the part (ADR-0069); the requester and the others in the neutral one. */
export function PersonAvatar({
  name,
  tone,
  size = 'md',
}: {
  name: string;
  tone: MessageTone;
  size?: 'sm' | 'md';
}) {
  return <Avatar name={name} size={size} tone={tone === 'requester' ? undefined : tone} />;
}

function Files({ number, files }: { number: number; files: readonly Attachment[] }) {
  return files.length === 0 ? null : (
    <Box
      sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)', marginTop: 'var(--space-3)' }}
    >
      {files.map((file) => (
        <AttachmentChip key={file.id} number={number} attachment={file} />
      ))}
    </Box>
  );
}

/**
 * The conversation of a ticket (canvas Chamado, ADR-0069): the description on top, then the messages, the requester
 * at the left and everyone else at the right, each in the color and with the tag of its part and "(você)" on the
 * person; a line for each day, and the older interactions folded above ten.
 */
export function TicketConversation({ ticket, me }: { ticket: Ticket; me: string }) {
  const { t } = useTranslation();
  const { timeZone, formatDateTime, formatTime } = useDates();
  const [unfolded, setUnfolded] = useState(false);
  // The day of today is read once, when the conversation opens.
  const [now] = useState(() => Date.now());
  const people = useMemo(
    () => new Map<string, Participant>(ticket.participants.map((person) => [person.id, person])),
    [ticket.participants],
  );
  const personOf = (id: string): Participant =>
    people.get(id) ?? { id, name: '', role: 'Other' as const };
  const requester = personOf(ticket.requester.id);
  const filesOf = (messageId: string | null) =>
    ticket.attachments.filter((attachment) => attachment.messageId === messageId);
  const hidden = unfolded ? 0 : Math.max(0, ticket.messages.length - shownInteractions);
  const shown = ticket.messages.slice(hidden);
  const dayOf = useMemo(() => {
    const format = new Intl.DateTimeFormat('pt-BR', {
      timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
    return (moment: string) => format.format(new Date(moment));
  }, [timeZone]);
  const dayText = useMemo(() => {
    const format = new Intl.DateTimeFormat('pt-BR', {
      timeZone,
      weekday: 'long',
      day: 'numeric',
      month: 'long',
    });
    const today = dayOf(new Date(now).toISOString());
    const yesterday = dayOf(new Date(now - 24 * 60 * 60 * 1000).toISOString());
    return (moment: string) => {
      const day = dayOf(moment);
      const text = format.format(new Date(moment));
      return day === today
        ? t('tickets.conversation.today', { day: text })
        : day === yesterday
          ? t('tickets.conversation.yesterday', { day: text })
          : text.charAt(0).toUpperCase() + text.slice(1);
    };
  }, [dayOf, now, t, timeZone]);

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-4)',
        maxWidth: 960,
        margin: '0 auto',
        width: '100%',
      }}
    >
      <Box
        component="section"
        aria-label={t('tickets.details.description')}
        sx={{
          padding: 'var(--space-4) var(--space-5)',
          backgroundColor: 'var(--bg-surface)',
          border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 'var(--space-2)',
            marginBottom: 'var(--space-3)',
            fontSize: 13,
            color: 'var(--text-secondary)',
          }}
        >
          <Avatar name={requester.name} size="sm" />
          <Box component="span" sx={{ fontSize: 14, fontWeight: 500, color: 'var(--text)' }}>
            {requester.name}
          </Box>
          <RoleTag role="Requester" />
          <span>{t('tickets.conversation.opened', { when: formatDateTime(ticket.openedAt) })}</span>
        </Box>
        {ticket.descriptionBody === null ? (
          <Box
            sx={{
              whiteSpace: 'pre-wrap',
              fontSize: 14,
              lineHeight: '22px',
              overflowWrap: 'anywhere',
            }}
          >
            {ticket.description}
          </Box>
        ) : (
          <RichText value={ticket.descriptionBody} />
        )}
        <Files number={ticket.number} files={filesOf(null)} />
      </Box>
      <Box
        component="ol"
        aria-label={t('tickets.conversation.messages')}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-3)',
          margin: 0,
          padding: 0,
          listStyle: 'none',
        }}
      >
        {hidden > 0 && (
          <li>
            <Button
              variant="text"
              fullWidth
              onClick={() => {
                setUnfolded(true);
              }}
              sx={{ border: '1px dashed var(--border-control)', color: 'var(--text-secondary)' }}
            >
              {t('tickets.conversation.older', { count: hidden })}
            </Button>
          </li>
        )}
        {shown.map((message, index) => {
          const author = personOf(message.authorId);
          const tone = toneOf[author.role];
          const left = author.role === 'Requester';
          const previous = shown[index - 1];
          const newDay = previous === undefined || dayOf(previous.sentAt) !== dayOf(message.sentAt);
          return (
            <Fragment key={message.id}>
              {newDay && (
                <Box
                  component="li"
                  aria-hidden={false}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 'var(--space-3)',
                    fontSize: 12,
                    color: 'var(--text-secondary)',
                    '&::before, &::after': {
                      content: '""',
                      flex: 1,
                      height: '1px',
                      backgroundColor: 'var(--border)',
                    },
                  }}
                >
                  {dayText(message.sentAt)}
                </Box>
              )}
              <Box
                component="li"
                data-side={left ? 'requester' : 'team'}
                data-tone={tone}
                sx={{
                  display: 'flex',
                  flexDirection: left ? 'row' : 'row-reverse',
                  alignItems: 'flex-start',
                  gap: 'var(--space-3)',
                  paddingLeft: left ? 0 : 'var(--space-8)',
                  paddingRight: left ? 'var(--space-8)' : 0,
                }}
              >
                <PersonAvatar name={author.name} tone={tone} />
                <Box
                  component="article"
                  aria-label={t('tickets.conversation.message', { name: author.name })}
                  sx={{
                    flex: 1,
                    minWidth: 0,
                    padding: 'var(--space-3) var(--space-4) var(--space-4)',
                    backgroundColor: `var(--msg-${tone}-bg)`,
                    border: `1px solid var(--msg-${tone}-border)`,
                    borderRadius: 'var(--radius-lg)',
                  }}
                >
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                      gap: 'var(--space-2)',
                      marginBottom: 'var(--space-2)',
                    }}
                  >
                    <Box component="span" sx={{ fontWeight: 500 }}>
                      {author.name}
                    </Box>
                    {author.id === me && (
                      <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
                        {t('tickets.conversation.you')}
                      </Box>
                    )}
                    <RoleTag role={author.role} />
                    <Box
                      component="time"
                      dateTime={message.sentAt}
                      sx={{
                        marginLeft: 'auto',
                        fontSize: 12,
                        color: 'var(--text-secondary)',
                        fontVariantNumeric: 'tabular-nums',
                      }}
                    >
                      {formatTime(message.sentAt)}
                    </Box>
                  </Box>
                  <RichText value={message.body} />
                  <Files number={ticket.number} files={filesOf(message.id)} />
                </Box>
              </Box>
            </Fragment>
          );
        })}
      </Box>
    </Box>
  );
}
