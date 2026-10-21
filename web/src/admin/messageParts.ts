import type { ParticipantRole } from '../tickets/conversation';
import type { MessagePart } from '../theme/brandColors';

/** The part of the conversation of each color of the messages (ADR-0069, ADR-0101). */
export const roleOfPart: Record<MessagePart, ParticipantRole> = {
  requester: 'Requester',
  owner: 'Owner',
  manager: 'Manager',
  techLead: 'TechLead',
  development: 'Development',
  functional: 'Functional',
};
