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

/** The size of a file as the attachments show it: "184 KB", "1,2 MB". */
export function sizeText(bytes: number): string {
  if (bytes < 1024 * 1024) {
    return `${String(Math.max(1, Math.round(bytes / 1024)))} KB`;
  }
  return `${(bytes / (1024 * 1024)).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB`;
}

/** The address of an attachment of a ticket. */
export const attachmentUrl = (number: number, id: string) =>
  `/api/tickets/${String(number)}/attachments/${id}`;

/** Whether the file is an image, for its icon. */
export const isImage = (contentType: string) => contentType.startsWith('image/');
