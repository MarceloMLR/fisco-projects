import type { TFunction } from 'i18next';
import { z } from 'zod';
import { acceptanceProblems, api } from '../api/client';
import { problemType } from '../projects/projectForm';

/** The messages of the fields of a term, as keys of the translations. */
export const acceptanceErrors = {
  milestoneRequired: 'acceptances.errors.milestoneRequired',
  recipientRequired: 'acceptances.errors.recipientRequired',
  titleRequired: 'acceptances.errors.titleRequired',
  messageTooLong: 'acceptances.errors.messageTooLong',
  fileRequired: 'acceptances.errors.fileRequired',
  fileNotPdf: 'acceptances.errors.fileNotPdf',
  reasonRequired: 'acceptances.errors.reasonRequired',
} as const;

export type AcceptanceError = (typeof acceptanceErrors)[keyof typeof acceptanceErrors];

export const isAcceptanceError = (message: string | undefined): message is AcceptanceError =>
  Object.values(acceptanceErrors).some((error) => error === message);

/** Whether the file is named as a PDF; the API also looks at its start (ADR-0035). */
export const isPdfName = (name: string) => name.toLowerCase().endsWith('.pdf');

const pdf = z.custom<File | null>().superRefine((file, context) => {
  if (file === null) {
    context.addIssue({ code: 'custom', message: acceptanceErrors.fileRequired });
  } else if (!isPdfName(file.name)) {
    context.addIssue({ code: 'custom', message: acceptanceErrors.fileNotPdf });
  }
});

/**
 * The rules of the fields of a request, the same as the API's (AcceptanceForm in the Documents module): the milestone,
 * the person, the title up to 120 characters, the message up to 2,000 and the term in PDF. A term attached in the name
 * of the person of a request has only the PDF.
 */
export const acceptanceSchema = (onlyTheFile: boolean) =>
  onlyTheFile
    ? z.object({
        milestoneId: z.string(),
        recipientId: z.string(),
        title: z.string(),
        message: z.string(),
        file: pdf,
      })
    : z.object({
        milestoneId: z.string().min(1, acceptanceErrors.milestoneRequired),
        recipientId: z.string().min(1, acceptanceErrors.recipientRequired),
        title: z
          .string()
          .trim()
          .min(1, acceptanceErrors.titleRequired)
          .max(120, acceptanceErrors.titleRequired),
        message: z.string().trim().max(2000, acceptanceErrors.messageTooLong),
        file: pdf,
      });

export interface AcceptanceValues {
  milestoneId: string;
  recipientId: string;
  title: string;
  message: string;
  file: File | null;
}

/** The reason of a refusal, up to 1,000 characters (AcceptanceTerm.ReasonOf in the Documents module). */
export const refusalSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, acceptanceErrors.reasonRequired)
    .max(1000, acceptanceErrors.reasonRequired),
});

/** The address of the term to sign of a request, which opens in a tab. */
export const acceptanceFileUrl = (projectId: string, termId: string) =>
  `/api/projects/${projectId}/acceptances/${termId}/file`;

/** The first name of a person, as "Lembrar Carla". */
export const firstName = (name: string) => name.trim().split(/\s+/)[0] ?? name;

/** What the API answered to a change of a term: done, the fields it refused, or the message of the refusal. */
export type AcceptanceAnswer =
  | { kind: 'done'; id?: string }
  | { kind: 'fields'; fields: string[] }
  | { kind: 'refused'; message: string };

function reasonOf(problem: unknown): string | undefined {
  const reason =
    typeof problem === 'object' && problem !== null && 'reason' in problem
      ? problem.reason
      : undefined;
  return reason === 'Empty' || reason === 'TooLarge' || reason === 'NotPdf' ? reason : undefined;
}

function fieldsOf(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/** The answer of the API in words: the fields refused, or why it was refused (closed project, answered, the file). */
export function answerOf(
  response: Response,
  problem: unknown,
  t: TFunction,
  id?: string,
): AcceptanceAnswer {
  if (response.ok) {
    return { kind: 'done', id };
  }
  const type = problemType(problem);
  if (type === acceptanceProblems.fileRefused) {
    const reason = reasonOf(problem);
    return {
      kind: 'refused',
      message:
        reason === undefined
          ? t('acceptances.messages.failed')
          : t(`acceptances.fileRefused.${reason as 'Empty' | 'TooLarge' | 'NotPdf'}`),
    };
  }
  if (response.status === 400) {
    return { kind: 'fields', fields: fieldsOf(problem) };
  }
  return {
    kind: 'refused',
    message:
      type === acceptanceProblems.projectClosed
        ? t('acceptances.messages.closed')
        : type === acceptanceProblems.answered
          ? t('acceptances.messages.answered')
          : t('acceptances.messages.failed'),
  };
}

/** A form with the fields given and the file, as the API takes a term (multipart). */
function formOf(fields: Record<string, string>, file: File): FormData {
  const form = new FormData();
  for (const [name, value] of Object.entries(fields)) {
    form.append(name, value);
  }
  form.append('file', file, file.name);
  return form;
}

/**
 * Sends a term: a request (with the message), a term attached without a request, the signed term of the person of a
 * request, or the signed term attached in the name of the person.
 */
export async function sendAcceptance(
  projectId: string,
  kind: 'request' | 'attach' | 'sign' | 'onBehalf',
  values: AcceptanceValues & { termId?: string },
  t: TFunction,
): Promise<AcceptanceAnswer> {
  const file = values.file;
  if (file === null) {
    return { kind: 'fields', fields: ['file'] };
  }
  try {
    if (kind === 'request' || kind === 'attach') {
      const fields: Record<string, string> = {
        milestoneId: values.milestoneId,
        recipientId: values.recipientId,
        title: values.title.trim(),
      };
      if (kind === 'request' && values.message.trim() !== '') {
        fields.message = values.message.trim();
      }
      const form = formOf(fields, file);
      const { data, error, response } =
        kind === 'request'
          ? await api.POST('/api/projects/{projectId}/acceptances', {
              params: { path: { projectId } },
              body: {},
              bodySerializer: () => form,
            })
          : await api.POST('/api/projects/{projectId}/acceptances/attached', {
              params: { path: { projectId } },
              body: {},
              bodySerializer: () => form,
            });
      return answerOf(response, error, t, data?.id);
    }
    const termId = values.termId ?? '';
    const form = formOf({}, file);
    const { error, response } =
      kind === 'sign'
        ? await api.POST('/api/projects/{projectId}/acceptances/{termId}/signed', {
            params: { path: { projectId, termId } },
            body: {},
            bodySerializer: () => form,
          })
        : await api.POST('/api/projects/{projectId}/acceptances/{termId}/attached', {
            params: { path: { projectId, termId } },
            body: {},
            bodySerializer: () => form,
          });
    return answerOf(response, error, t, termId);
  } catch {
    return { kind: 'refused', message: t('acceptances.messages.failed') };
  }
}
