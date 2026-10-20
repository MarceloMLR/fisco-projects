import type { TFunction } from 'i18next';
import { api, ticketProblems } from '../api/client';

export type PeopleChange = 'assignee' | 'requester';

export function problemField(problem: unknown, name: string): unknown {
  return typeof problem === 'object' && problem !== null && name in problem
    ? (problem as Record<string, unknown>)[name]
    : undefined;
}

/** The numbers of a problem, as "#3, #5". */
function numbersOf(problem: unknown): string {
  const numbers = problemField(problem, 'numbers');
  return Array.isArray(numbers) ? numbers.map((number) => `#${String(number)}`).join(', ') : '';
}

/** Why a change of people or of team was refused, in words of the screen (TicketProblems in the Tickets module). */
export function ticketChangeProblemText(
  status: number,
  problem: unknown,
  name: string,
  t: TFunction,
): string {
  const type = problemField(problem, 'type');
  if (type === ticketProblems.personRefused) {
    return t('tickets.people.refused', { name, numbers: numbersOf(problem) });
  }
  if (type === ticketProblems.ticketFinished) {
    return t('tickets.people.finished', { numbers: numbersOf(problem) });
  }
  if (type === ticketProblems.ticketChanged) {
    return t('tickets.reply.changed');
  }
  if (status === 403) {
    return t('tickets.people.forbidden');
  }
  return t('tickets.people.failed');
}

/** Saves a new assignee or requester for the tickets; the problem of the API when it refuses. */
export async function changePeople(
  kind: PeopleChange,
  numbers: readonly number[],
  personId: string,
): Promise<{ ok: true } | { ok: false; status: number; problem: unknown }> {
  const { response, error } =
    kind === 'assignee'
      ? await api.PUT('/api/tickets/assignee', {
          body: { numbers: [...numbers], assigneeId: personId },
        })
      : await api.PUT('/api/tickets/requester', {
          body: { numbers: [...numbers], requesterId: personId },
        });
  return response.ok ? { ok: true } : { ok: false, status: response.status, problem: error };
}
