import { z } from 'zod';

/** The keys of the messages of the project fields, as the form and the API name the problems. */
export const projectFieldErrors = {
  codeInvalid: 'projects.errors.codeInvalid',
  nameRequired: 'projects.errors.nameRequired',
  nameTooLong: 'projects.errors.nameTooLong',
  descriptionTooLong: 'projects.errors.descriptionTooLong',
  clientInvalid: 'projects.errors.clientInvalid',
  managerRequired: 'projects.errors.managerRequired',
  managerInvalid: 'projects.errors.managerInvalid',
  techLeadInvalid: 'projects.errors.techLeadInvalid',
} as const;

export type ProjectFieldError = (typeof projectFieldErrors)[keyof typeof projectFieldErrors];

export const isProjectFieldError = (message: string | undefined): message is ProjectFieldError =>
  Object.values(projectFieldErrors).some((error) => error === message);

/** The value of the client field for a project without a client (an internal project). */
export const noClient = 'none';

/**
 * The code of a project as the API keeps it (ProjectCode in the Projects module): from 2 to 15 letters, digits and
 * hyphens, starting with a letter, without a hyphen at the end or two in a row, in capitals.
 */
export function isValidProjectCode(code: string): boolean {
  return code.length >= 2 && code.length <= 15 && /^[A-Z][A-Z0-9]*(-[A-Z0-9]+)*$/.test(code);
}

/** What is typed in the code field, in capitals, without spaces. */
export function maskTypedCode(text: string): string {
  return text.toUpperCase().replace(/\s/g, '').slice(0, 15);
}

/** The data of a project that both the creation and the editing ask, checked as the API checks them. */
export const projectDataSchema = z.object({
  code: z
    .string()
    .trim()
    .refine((value) => isValidProjectCode(value.toUpperCase()), {
      error: projectFieldErrors.codeInvalid,
    }),
  name: z
    .string()
    .trim()
    .min(1, { error: projectFieldErrors.nameRequired })
    .max(150, { error: projectFieldErrors.nameTooLong }),
  description: z.string().trim().max(4000, { error: projectFieldErrors.descriptionTooLong }),
  clientId: z.string(),
});

/** The fields a validation problem of the API names (RFC 9457 with the errors of ASP.NET Core). */
export function invalidFields(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/** A text the API sent with a problem, such as the name of the project that already has the code. */
export function problemText(problem: unknown, key: string): string {
  if (typeof problem !== 'object' || problem === null || !(key in problem)) {
    return '';
  }
  const value = (problem as Record<string, unknown>)[key];
  return typeof value === 'string' ? value : '';
}

/** The type of a problem of the API. */
export function problemType(problem: unknown): unknown {
  return typeof problem === 'object' && problem !== null && 'type' in problem
    ? problem.type
    : undefined;
}
