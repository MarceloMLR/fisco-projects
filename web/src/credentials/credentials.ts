import type { Credential } from '../api/client';

/** A line of the list of credentials: the name of an environment, with how many credentials it has, or a credential. */
export type CredentialRow =
  | { kind: 'group'; id: string; name: string; count: number }
  | { kind: 'credential'; id: string; credential: Credential };

export const isGroupRow = (row: CredentialRow) => row.kind === 'group';

/**
 * The lines of the list: each environment with its name and its credentials, in the order the API sends them (the
 * environment and then the portal), an environment without credentials left out (canvas ProjetoAcessos).
 */
export function credentialRows(credentials: readonly Credential[]): CredentialRow[] {
  const counts = new Map<string, number>();
  for (const credential of credentials) {
    counts.set(credential.environment, (counts.get(credential.environment) ?? 0) + 1);
  }
  const rows: CredentialRow[] = [];
  for (const credential of credentials) {
    const group = `group:${credential.environment}`;
    if (!rows.some((row) => row.id === group)) {
      rows.push({
        kind: 'group',
        id: group,
        name: credential.environment,
        count: counts.get(credential.environment) ?? 0,
      });
    }
    rows.push({ kind: 'credential', id: credential.id, credential });
  }
  return rows;
}
