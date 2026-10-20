import { z } from 'zod';

/** The messages of the fields of a credential, as keys of the translations. */
export const credentialErrors = {
  portalRequired: 'credentials.errors.portalRequired',
  environmentRequired: 'credentials.errors.environmentRequired',
  urlInvalid: 'credentials.errors.urlInvalid',
  usernameRequired: 'credentials.errors.usernameRequired',
  passwordRequired: 'credentials.errors.passwordRequired',
  passwordTooLong: 'credentials.errors.passwordTooLong',
  notesTooLong: 'credentials.errors.notesTooLong',
} as const;

export type CredentialError = (typeof credentialErrors)[keyof typeof credentialErrors];

export const isCredentialError = (message: string | undefined): message is CredentialError =>
  Object.values(credentialErrors).some((error) => error === message);

// An address of the web, as the API takes it (ADR-0034): empty, or http or https up to 2,000 characters.
const webAddress = /^https?:\/\/\S+$/i;

/**
 * The rules of the fields of a credential, the same as the API's (CredentialForm in the Credentials module), for the
 * dialog and for the lines of an imported spreadsheet: the password is needed only in a new one, and is never trimmed.
 */
export const credentialSchema = (passwordNeeded: boolean) =>
  z.object({
    portal: z
      .string()
      .trim()
      .min(1, credentialErrors.portalRequired)
      .max(120, credentialErrors.portalRequired),
    environment: z
      .string()
      .trim()
      .min(1, credentialErrors.environmentRequired)
      .max(60, credentialErrors.environmentRequired),
    url: z
      .string()
      .trim()
      .refine(
        (url) => url === '' || (url.length <= 2000 && webAddress.test(url)),
        credentialErrors.urlInvalid,
      ),
    username: z
      .string()
      .trim()
      .min(1, credentialErrors.usernameRequired)
      .max(200, credentialErrors.usernameRequired),
    password: z
      .string()
      .max(500, credentialErrors.passwordTooLong)
      .refine((password) => !passwordNeeded || password !== '', credentialErrors.passwordRequired),
    notes: z.string().trim().max(500, credentialErrors.notesTooLong),
  });

export type CredentialValues = z.input<ReturnType<typeof credentialSchema>>;

/** The fields of a credential, in the order of the dialog. */
export const credentialFields = [
  'portal',
  'environment',
  'url',
  'username',
  'password',
  'notes',
] as const satisfies readonly (keyof CredentialValues)[];

export type CredentialFieldName = (typeof credentialFields)[number];
