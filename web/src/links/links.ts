import type { ProjectLink } from '../api/client';

/** A line of the list of links: the name of a category, with how many links it has, or a link. */
export type LinkRow =
  | { kind: 'group'; id: string; name: string; count: number }
  | { kind: 'link'; id: string; link: ProjectLink };

export const isGroupRow = (row: LinkRow) => row.kind === 'group';

/**
 * The lines of the list: each category with its name and its links, in the order the API sends them (the category
 * and then the title), a category without links left out (canvas ProjetoLinks, fp-group-row).
 */
export function linkRows(links: readonly ProjectLink[]): LinkRow[] {
  const rows: LinkRow[] = [];
  for (const link of links) {
    const group = `group:${link.categoryId}`;
    if (!rows.some((row) => row.id === group)) {
      rows.push({
        kind: 'group',
        id: group,
        name: link.categoryName,
        count: links.filter((item) => item.categoryId === link.categoryId).length,
      });
    }
    rows.push({ kind: 'link', id: link.id, link });
  }
  return rows;
}

/**
 * The letter of the square beside a link: of the last part of a title made of parts, as "ERP · Homologação" → "H",
 * so the environments of one system tell apart; of the title otherwise.
 */
export function initialOf(title: string): string {
  const parts = title.split(' · ');
  const last = (parts[parts.length - 1] ?? title).trim();
  const letter = /[\p{L}\p{N}]/u.exec(last === '' ? title : last)?.[0];
  return (letter ?? '').toLocaleUpperCase('pt-BR');
}

/** The address as the list shows it: without the protocol, as "hml-erp.cliente.example/qa". */
export function shownAddress(url: string): string {
  return url.replace(/^https?:\/\//i, '').replace(/\/$/, '');
}
