/**
 * The CNPJ of a client, numeric or alphanumeric (IN RFB nº 2.229/2024): 12 letters or digits and 2 check digits.
 * The API keeps it without punctuation and checks it with the same rule (Cnpj in the Projects module); the screen
 * checks it only to warn before saving.
 */
export const cnpjLength = 14;

const firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
const secondWeights = [6, ...firstWeights];

/** The letters and digits of a typed CNPJ, in capitals: what the API keeps. */
export function cleanCnpj(text: string): string {
  return text.toUpperCase().replace(/[^0-9A-Z]/g, '');
}

/** A check digit by modulo 11, with each character worth its ASCII code minus 48. */
function checkDigit(characters: string, weights: readonly number[]): number {
  const sum = weights.reduce(
    (total, weight, index) => total + (characters.charCodeAt(index) - 48) * weight,
    0,
  );
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

/**
 * What is wrong with a CNPJ without punctuation, for the field to say: fewer or more than 14 characters
 * (`incomplete`), all of them the same (`repeated`), or the last two not the check digits of the others (`digits`);
 * null when it follows the rule.
 */
export type CnpjProblem = 'incomplete' | 'repeated' | 'digits';

export function cnpjProblem(cnpj: string): CnpjProblem | null {
  if (cnpj.length !== cnpjLength) {
    return 'incomplete';
  }
  if (/^(.)\1*$/.test(cnpj)) {
    return 'repeated';
  }
  if (!/^[0-9A-Z]{12}[0-9]{2}$/.test(cnpj)) {
    return 'digits';
  }
  const first = checkDigit(cnpj.slice(0, 12), firstWeights);
  const second = checkDigit(cnpj.slice(0, 12) + String(first), secondWeights);
  return cnpj.slice(12) === `${String(first)}${String(second)}` ? null : 'digits';
}

/** Whether a CNPJ without punctuation follows the rule: the format and both check digits. */
export function isValidCnpj(cnpj: string): boolean {
  return cnpjProblem(cnpj) === null;
}

/** A CNPJ as the screens show it: XX.XXX.XXX/XXXX-XX. Anything else stays as it is. */
export function formatCnpj(cnpj: string): string {
  return cnpj.length === cnpjLength ? maskTypedCnpj(cnpj) : cnpj;
}

/** What is typed in the CNPJ field, in capitals and with the punctuation as it grows: "11.222.333/0001-81". */
export function maskTypedCnpj(text: string): string {
  const clean = cleanCnpj(text).slice(0, cnpjLength);
  const parts = [
    clean.slice(0, 2),
    clean.slice(2, 5),
    clean.slice(5, 8),
    clean.slice(8, 12),
    clean.slice(12),
  ];
  const separators = ['', '.', '.', '/', '-'];
  return parts.reduce(
    (masked, part, index) => (part === '' ? masked : `${masked}${separators[index] ?? ''}${part}`),
    '',
  );
}
