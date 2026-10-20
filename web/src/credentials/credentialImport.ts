import {
  type CredentialFieldName,
  credentialFields,
  credentialSchema,
  type CredentialValues,
} from './credentialForm';

/** A line of the spreadsheet read by the API: its number in the spreadsheet and the text of each column. */
export interface SheetRow {
  number: number;
  cells: readonly string[];
}

/** Where each field of a credential comes from: the index of a column, or none. */
export type ImportMapping = Record<CredentialFieldName, number | null>;

/**
 * The mapping of the columns and, when the environment is the same for every line ("Mesmo para todas"), the text
 * typed for it; null takes the environment from its column.
 */
export interface ImportChoice {
  mapping: ImportMapping;
  environment: string | null;
}

/** The names of the header that suggest each field, without accents and in lowercase. */
const hints: Record<CredentialFieldName, readonly string[]> = {
  portal: ['portal', 'sistema', 'site'],
  environment: ['ambiente', 'environment'],
  url: ['endereco', 'url', 'link'],
  username: ['usuario', 'login', 'user', 'e-mail', 'email'],
  password: ['senha', 'password'],
  notes: ['observacao', 'observacoes', 'obs', 'notas', 'nota'],
};

const simplify = (text: string) => text.normalize('NFD').replace(/[̀-ͯ]/g, '').trim().toLowerCase();

/**
 * The mapping suggested by the header: each field takes the first column whose name is or starts with one of its
 * names, each column for one field only; the others stay without a column.
 */
export function suggestMapping(columns: readonly string[]): ImportMapping {
  const used = new Set<number>();
  const mapping = Object.fromEntries(
    credentialFields.map((field) => [field, null]),
  ) as ImportMapping;
  for (const field of credentialFields) {
    const index = columns.findIndex(
      (column, position) =>
        !used.has(position) && hints[field].some((hint) => simplify(column).startsWith(hint)),
    );
    if (index >= 0) {
      used.add(index);
      mapping[field] = index;
    }
  }
  return mapping;
}

/** The credential a line becomes with the mapping: the text of each mapped column, the typed environment otherwise. */
export function credentialOf(row: SheetRow, choice: ImportChoice): CredentialValues {
  const cell = (field: CredentialFieldName) => {
    const index = choice.mapping[field];
    return index === null ? '' : (row.cells[index] ?? '');
  };
  return {
    portal: cell('portal'),
    environment: choice.environment ?? cell('environment'),
    url: cell('url'),
    username: cell('username'),
    password: cell('password'),
    notes: cell('notes'),
  };
}

/** What keeps a line out of the import, as "Sem usuário"; none for a line that is a valid credential. */
export type RowProblem =
  | { kind: 'missing'; field: CredentialFieldName }
  | { kind: 'tooLong'; field: CredentialFieldName }
  | { kind: 'urlInvalid' };

const schema = credentialSchema(true);

/** The first problem of the credential of a line, by the rules of the dialog (credentialSchema). */
export function problemOf(values: CredentialValues): RowProblem | undefined {
  const parsed = schema.safeParse(values);
  if (parsed.success) {
    return undefined;
  }
  const fields = parsed.error.issues.map((issue) => issue.path[0]);
  const field = credentialFields.find((name) => fields.includes(name));
  if (field === undefined) {
    return undefined;
  }
  if (field === 'url') {
    return { kind: 'urlInvalid' };
  }
  const value = field === 'password' ? values[field] : values[field].trim();
  return value === '' ? { kind: 'missing', field } : { kind: 'tooLong', field };
}

/** Whether a line has the text in any of its columns, without telling case or accents apart. */
export function rowHas(row: SheetRow, search: string): boolean {
  const text = simplify(search);
  return text === '' || row.cells.some((cell) => simplify(cell).includes(text));
}
