import type { TFunction } from 'i18next';
import { ticketProblems } from '../api/client';

function field(problem: unknown, name: string): unknown {
  return typeof problem === 'object' && problem !== null && name in problem
    ? (problem as Record<string, unknown>)[name]
    : undefined;
}

/** Why the API refused a message or files, in words of the screen (TicketProblems in the Tickets module). */
export function ticketProblemText(status: number, problem: unknown, t: TFunction): string {
  const type = field(problem, 'type');
  if (status === 413) {
    return t('tickets.reply.refused.TooLarge', { name: '' });
  }
  if (type === ticketProblems.attachmentRefused) {
    const reason = field(problem, 'reason');
    const name = field(problem, 'fileName');
    const known =
      reason === 'TooLarge' || reason === 'Empty' || reason === 'Dangerous' ? reason : 'TooLarge';
    return t(`tickets.reply.refused.${known}`, { name: typeof name === 'string' ? name : '' });
  }
  if (type === ticketProblems.mentionRefused) {
    return t('tickets.reply.mentionRefused');
  }
  if (type === ticketProblems.ticketChanged) {
    return t('tickets.reply.changed');
  }
  if (type === ticketProblems.ticketClosed) {
    return t('tickets.reply.closed');
  }
  if (status === 400) {
    return t('tickets.reply.invalid');
  }
  if (status === 403 || status === 404) {
    return t('tickets.reply.forbidden');
  }
  return t('tickets.reply.failed');
}
