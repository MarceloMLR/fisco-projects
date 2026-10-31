import type { TFunction } from 'i18next';
import { z } from 'zod';
import {
  acceptanceProblems,
  api,
  type Acceptance,
  type AcceptanceSignerStatus,
  type Project,
  type SessionUser,
} from '../api/client';
import { problemType } from '../projects/projectForm';

/** The messages of the fields of a term, as keys of the translations. */
export const acceptanceErrors = {
  milestoneRequired: 'acceptances.errors.milestoneRequired',
  titleRequired: 'acceptances.errors.titleRequired',
  messageTooLong: 'acceptances.errors.messageTooLong',
  fileRequired: 'acceptances.errors.fileRequired',
  fileNotPdf: 'acceptances.errors.fileNotPdf',
  reasonRequired: 'acceptances.errors.reasonRequired',
  signersRequired: 'acceptances.errors.signersRequired',
  signerNameRequired: 'acceptances.errors.signerNameRequired',
  signerEmailInvalid: 'acceptances.errors.signerEmailInvalid',
  signerEmailRepeated: 'acceptances.errors.signerEmailRepeated',
} as const;

export type AcceptanceError = (typeof acceptanceErrors)[keyof typeof acceptanceErrors];

export const isAcceptanceError = (message: string | undefined): message is AcceptanceError =>
  Object.values(acceptanceErrors).some((error) => error === message);

/** At most 10 signers in a request, all signing at the same time (AcceptanceSignerForm in the Documents module, ADR-0129). */
export const maxSigners = 10;

/**
 * Whether the person manages the terms of the project, the only one who sees them (ADR-0129): the Administrator, and
 * the manager of the project. The API decides; this only keeps the tab away from who would get 403.
 */
export function managesTerms(project: Project, user: SessionUser | undefined): boolean {
  return (
    user !== undefined &&
    (user.role === 'Administrator' ||
      project.members.some((member) => member.id === user.id && member.role === 'Manager'))
  );
}

/** Whether the file is named as a PDF; the API also looks at its start (ADR-0035). */
export const isPdfName = (name: string) => name.toLowerCase().endsWith('.pdf');

const pdf = z.custom<File | null>().superRefine((file, context) => {
  if (file === null) {
    context.addIssue({ code: 'custom', message: acceptanceErrors.fileRequired });
  } else if (!isPdfName(file.name)) {
    context.addIssue({ code: 'custom', message: acceptanceErrors.fileNotPdf });
  }
});

/** An e-mail as the API takes it: a name, "@" and a domain with a dot, without spaces. */
const emailPattern = /^[^\s@<>()]+@[^\s@<>()]+\.[^\s@<>().]+$/;

const signer = z.object({
  name: z
    .string()
    .trim()
    .min(1, acceptanceErrors.signerNameRequired)
    .max(120, acceptanceErrors.signerNameRequired),
  email: z
    .string()
    .trim()
    .max(254, acceptanceErrors.signerEmailInvalid)
    .regex(emailPattern, acceptanceErrors.signerEmailInvalid),
});

/** The signers of a request: 1 to 10, each with the name and the e-mail, without the same e-mail twice. */
const signers = z
  .array(signer)
  .min(1, acceptanceErrors.signersRequired)
  .max(maxSigners, acceptanceErrors.signersRequired)
  .superRefine((list, context) => {
    const seen = new Set<string>();
    list.forEach((item, index) => {
      const email = item.email.trim().toLowerCase();
      if (email !== '' && seen.has(email)) {
        context.addIssue({
          code: 'custom',
          message: acceptanceErrors.signerEmailRepeated,
          path: [index, 'email'],
        });
      }
      seen.add(email);
    });
  });

const milestoneId = z.string().min(1, acceptanceErrors.milestoneRequired);
const title = z
  .string()
  .trim()
  .min(1, acceptanceErrors.titleRequired)
  .max(120, acceptanceErrors.titleRequired);

/**
 * The rules of the fields of a term, the same as the API's (AcceptanceForm and AcceptanceSignerForm in the Documents
 * module): a request has the milestone, the title up to 120 characters, the message up to 2,000, the term in PDF and
 * the signers; a term attached without a request, the milestone, the title and the PDF; and a term attached to a
 * request, only the PDF.
 */
export const acceptanceSchema = (mode: AcceptanceMode) =>
  mode === 'onBehalf'
    ? z.object({
        milestoneId: z.string(),
        title: z.string(),
        message: z.string(),
        signers: z.array(z.object({ name: z.string(), email: z.string() })),
        file: pdf,
      })
    : z.object({
        milestoneId,
        title,
        message: z.string().trim().max(2000, acceptanceErrors.messageTooLong),
        signers:
          mode === 'request' ? signers : z.array(z.object({ name: z.string(), email: z.string() })),
        file: pdf,
      });

/** A request to the provider, a term attached without a request, or a signed term attached to a pending request. */
export type AcceptanceMode = 'request' | 'attach' | 'onBehalf';

export interface AcceptanceValues {
  milestoneId: string;
  title: string;
  message: string;
  signers: { name: string; email: string }[];
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

/** The names joined as the language says them: "Ana Souza", "Ana Souza e Bruno Lima", "A, B e C". */
export const namesText = (names: readonly string[]) =>
  new Intl.ListFormat('pt-BR', { style: 'long', type: 'conjunction' }).format(names);

/** How many signers of the list signed. */
export const signedCount = (list: readonly { status: AcceptanceSignerStatus }[]) =>
  list.filter((item) => item.status === 'Signed').length;

/**
 * Who a term was asked of, as the list tells it: "2 de 3 assinaram", or who refused, with the names of the signers; the
 * person of a request asked before the provider (ADR-0129); or a term signed outside the system.
 */
export function signersSummary(term: Acceptance, t: TFunction): { main: string; detail: string } {
  // A refusal of one signer refuses the term, and says who (ADR-0129).
  const refusal = term.signers.find((item) => item.status === 'Refused');
  if (refusal !== undefined) {
    return {
      main: t('acceptances.signers.refused', { name: refusal.name }),
      detail: namesText(term.signers.map((item) => item.name)),
    };
  }
  if (term.signers.length > 0) {
    return {
      main: t('acceptances.signers.count', {
        count: signedCount(term.signers),
        total: term.signers.length,
      }),
      detail: namesText(term.signers.map((item) => item.name)),
    };
  }
  if (term.recipient !== null) {
    return { main: term.recipient.name, detail: t('acceptances.signers.before') };
  }
  return { main: t('acceptances.signers.outside'), detail: t('acceptances.signers.outsideDetail') };
}

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

/**
 * The answer of the API in words: the fields refused, or why it was refused (closed project, answered, the file, no
 * provider of signatures, the provider failing).
 */
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
  const messages = {
    [acceptanceProblems.projectClosed]: 'acceptances.messages.closed',
    [acceptanceProblems.answered]: 'acceptances.messages.answered',
    [acceptanceProblems.noProvider]: 'acceptances.messages.noProvider',
    [acceptanceProblems.providerFailed]: 'acceptances.messages.providerFailed',
    [acceptanceProblems.remindedRecently]: 'acceptances.messages.remindedRecently',
  } as const;
  const key = type === undefined ? undefined : messages[type as keyof typeof messages];
  return { kind: 'refused', message: t(key ?? 'acceptances.messages.failed') };
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
 * Sends a term: a request of signature to the provider (with the message and the signers), a term attached without
 * a request, or the signed term attached to a pending request.
 */
export async function sendAcceptance(
  projectId: string,
  mode: AcceptanceMode,
  values: AcceptanceValues & { termId?: string },
  t: TFunction,
): Promise<AcceptanceAnswer> {
  const file = values.file;
  if (file === null) {
    return { kind: 'fields', fields: ['file'] };
  }
  try {
    if (mode === 'onBehalf') {
      const termId = values.termId ?? '';
      const { error, response } = await api.POST(
        '/api/projects/{projectId}/acceptances/{termId}/attached',
        {
          params: { path: { projectId, termId } },
          body: {},
          bodySerializer: () => formOf({}, file),
        },
      );
      return answerOf(response, error, t, termId);
    }
    const fields: Record<string, string> = {
      milestoneId: values.milestoneId,
      title: values.title.trim(),
    };
    if (mode === 'attach') {
      const { data, error, response } = await api.POST(
        '/api/projects/{projectId}/acceptances/attached',
        {
          params: { path: { projectId } },
          body: {},
          bodySerializer: () => formOf(fields, file),
        },
      );
      return answerOf(response, error, t, data?.id);
    }
    if (values.message.trim() !== '') {
      fields.message = values.message.trim();
    }
    values.signers.forEach((item, index) => {
      fields[`signers[${String(index)}].name`] = item.name.trim();
      fields[`signers[${String(index)}].email`] = item.email.trim();
    });
    const { data, error, response } = await api.POST('/api/projects/{projectId}/acceptances', {
      params: { path: { projectId } },
      body: {},
      bodySerializer: () => formOf(fields, file),
    });
    return answerOf(response, error, t, data?.id);
  } catch {
    return { kind: 'refused', message: t('acceptances.messages.failed') };
  }
}
