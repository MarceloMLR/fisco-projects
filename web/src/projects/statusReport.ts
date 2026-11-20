import { z } from 'zod';
import type { RiskLevel, StatusReportDraft, StatusReportTicket } from '../api/client';
import type { MultiSelectOption } from '../components/MultiSelect';

/** The limits of the status report (ADR-0165), the same as the server's. */
export const statusReportLimits = { text: 1000, attention: 500, risks: 10 } as const;

/** The levels a risk may have, from the most serious. */
export const riskLevels: readonly RiskLevel[] = ['High', 'Medium', 'Low'];

/** The keys of the texts of the errors of the form, checked by the types. */
export const statusReportErrors = {
  textTooLong: 'projects.statusReport.errors.textTooLong',
  attentionTooLong: 'projects.statusReport.errors.attentionTooLong',
  risks: 'projects.statusReport.errors.risks',
} as const;

export type StatusReportError = (typeof statusReportErrors)[keyof typeof statusReportErrors];

export function isStatusReportError(message: string | undefined): message is StatusReportError {
  return Object.values<string>(statusReportErrors).includes(message ?? '');
}

const text = z.string().max(statusReportLimits.text, statusReportErrors.textTooLong);

/** What the person writes and chooses: the four texts, the risks with their level and point of attention, and the hours. */
export const statusReportSchema = z.object({
  summary: text,
  focus: text,
  nextSteps: text,
  clientNeeds: text,
  risks: z
    .array(
      z.object({
        number: z.number(),
        level: z.enum(['High', 'Medium', 'Low']),
        attention: z
          .string()
          .max(statusReportLimits.attention, statusReportErrors.attentionTooLong),
      }),
    )
    .max(statusReportLimits.risks, statusReportErrors.risks),
  showHours: z.boolean(),
});

export type StatusReportValues = z.infer<typeof statusReportSchema>;

/** The form filled by the draft: the texts of the last report and the risks suggested; the hours never (ADR-0166). */
export function valuesOf(draft: StatusReportDraft): StatusReportValues {
  return {
    summary: draft.summary,
    focus: draft.focus,
    nextSteps: draft.nextSteps,
    clientNeeds: draft.clientNeeds,
    risks: draft.risks.map((risk) => ({ ...risk })),
    showHours: false,
  };
}

/** The open tickets to choose as risks, the ones that threaten milestones first, each as "#539 · título". */
export function ticketOptions(
  tickets: readonly StatusReportTicket[],
  groups: { threatens: string; others: string },
): MultiSelectOption[] {
  return [...tickets]
    .sort((a, b) => Number(b.threatens) - Number(a.threatens) || a.number - b.number)
    .map((ticket) => ({
      value: String(ticket.number),
      label: ticketLabel(ticket),
      group: ticket.threatens ? groups.threatens : groups.others,
    }));
}

export function ticketLabel(ticket: Pick<StatusReportTicket, 'number' | 'title'>): string {
  return `#${String(ticket.number)} · ${ticket.title}`;
}

/**
 * The risks after the person changed the tickets chosen: the ones kept stay as they were, in their order, and a new one
 * comes with the level Medium and no point of attention.
 */
export function risksAfter(
  risks: StatusReportValues['risks'],
  chosen: readonly string[],
): StatusReportValues['risks'] {
  const numbers = chosen.map(Number);
  const kept = risks.filter((risk) => numbers.includes(risk.number));
  const added = numbers
    .filter((number) => !kept.some((risk) => risk.number === number))
    .map((number) => ({ number, level: 'Medium' as const, attention: '' }));
  return [...kept, ...added];
}
