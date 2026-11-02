import type { TFunction } from 'i18next';
import type { FolderRole } from '../api/client';

// Who sees a folder of the documents (ADR-0148): the roles chosen, besides the Administrator and the manager, who see
// every folder. The folder of the acceptance terms is only theirs.

/** The roles a folder may leave out, in the order of the screens. */
export const folderRoles = [
  'TechLead',
  'Contributor',
  'Developer',
  'Viewer',
] as const satisfies readonly FolderRole[];

const isFolderRole = (value: string): value is FolderRole =>
  (folderRoles as readonly string[]).includes(value);

interface Visibility {
  isAcceptance: boolean;
  visibleTo: readonly FolderRole[];
}

/** Whether some role of the project does not see the folder, as the folder of the terms. */
export function isRestricted(folder: Visibility): boolean {
  return folder.isAcceptance || folderRoles.some((role) => !folder.visibleTo.includes(role));
}

/** The roles in the order of the screens, without repetitions. */
export function inOrder(roles: readonly FolderRole[]): FolderRole[] {
  return folderRoles.filter((role) => roles.includes(role));
}

/** Who sees the folder in words: "Todos do projeto", "Só a gestão" or "Gestão, Líder técnico e Leitor". */
export function visibilityText(folder: Visibility, t: TFunction): string {
  const roles = inOrder(folder.visibleTo);
  if (folder.isAcceptance || roles.length === 0) {
    return t('folderVisibility.onlyManagement');
  }
  if (roles.length === folderRoles.length) {
    return t('folderVisibility.everyone');
  }
  return new Intl.ListFormat('pt-BR', { type: 'conjunction' }).format([
    t('folderVisibility.management'),
    ...roles.map((role) => t(`projects.roles.${role}`)),
  ]);
}

/** The roles of a value of the audit: "TechLead, Viewer", "Everyone" or "None". */
export function rolesOfAudit(value: string): FolderRole[] {
  if (value === 'Everyone') {
    return [...folderRoles];
  }
  return value
    .split(',')
    .map((part) => part.trim())
    .filter(isFolderRole);
}
