import type { Ticket } from '../api/client';

export type ParticipantRole = Ticket['participants'][number]['role'];

/** The color of each part in the conversation (ADR-0069): the tokens msg-*, role-* and avatar-* of the design system. */
export type MessageTone = 'requester' | 'owner' | 'manager' | 'techlead' | 'dev' | 'functional';

export const toneOf: Record<ParticipantRole, MessageTone> = {
  Requester: 'requester',
  Owner: 'owner',
  Manager: 'manager',
  TechLead: 'techlead',
  Development: 'dev',
  Functional: 'functional',
  Administrator: 'requester',
  Other: 'requester',
};

/** Above this many interactions, the older ones are folded (ADR-0069). */
export const shownInteractions = 10;

/** The address of an attachment of a ticket. */
export const attachmentUrl = (number: number, id: string) =>
  `/api/tickets/${String(number)}/attachments/${id}`;

/** The address that downloads an attachment with its name, even an image or a PDF (Chamados-2.1). */
export const attachmentDownloadUrl = (number: number, id: string) =>
  `${attachmentUrl(number, id)}?download=true`;

/** The address of the .zip with all the attachments of a ticket (Chamados-2.1). */
export const attachmentsArchiveUrl = (number: number) =>
  `/api/tickets/${String(number)}/attachments/archive`;

/** Whether the file is an image, for its icon. */
export const isImage = (contentType: string) => contentType.startsWith('image/');

/** What the conversation shows (ADR-0069): everything, only the messages or only the history of the ticket. */
export type ConversationFilter = 'all' | 'messages' | 'history';

export type ConversationItem =
  | { kind: 'message'; at: string; message: Ticket['messages'][number] }
  | { kind: 'event'; at: string; event: Ticket['events'][number] };

/**
 * The messages and the events of a ticket in the order they happened; at the same moment, the message comes before the
 * change it made, as the first answer and the answer of the requester.
 */
export function conversationItems(
  ticket: Ticket,
  filter: ConversationFilter = 'all',
): ConversationItem[] {
  const items: ConversationItem[] = [
    ...(filter === 'history'
      ? []
      : ticket.messages.map((message) => ({
          kind: 'message' as const,
          at: message.sentAt,
          message,
        }))),
    ...(filter === 'messages'
      ? []
      : ticket.events.map((event) => ({ kind: 'event' as const, at: event.at, event }))),
  ];
  // The sort is stable: the messages, first in the list, stay before the events of the same moment.
  return items.sort((a, b) => Date.parse(a.at) - Date.parse(b.at));
}
