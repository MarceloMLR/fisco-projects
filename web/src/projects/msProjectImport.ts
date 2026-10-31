import {
  api,
  type MsProjectPreview,
  type MsProjectResource,
  type ProjectRole,
} from '../api/client';
import type { PersonChoice } from '../components/PersonField';
import { problemText, problemType } from './projectForm';

/** The problem type of a file the preview does not read (ADR-0135). */
export const msProjectRefused = 'urn:fiscoprojects:problem:ms-project-file-refused';

/** Why the API did not read the file, as it says in the extension reason. */
export const msProjectRefusals = [
  'NotMsProjectXml',
  'TooLarge',
  'TooManyTasks',
  'NoTasks',
  'OutOfRange',
] as const;

export type MsProjectRefusal = (typeof msProjectRefusals)[number];

const isRefusal = (reason: string): reason is MsProjectRefusal =>
  (msProjectRefusals as readonly string[]).includes(reason);

/** The roles a person of the file gets in the project, when not the manager nor the tech lead (ADR-0135). */
export const rolesOfNewPeople = [
  'Contributor',
  'Developer',
] as const satisfies readonly ProjectRole[];

export type RoleOfNewPerson = (typeof rolesOfNewPeople)[number];

/** Who a resource of the file becomes: a person of the company with the role, the client of the project, or nobody. */
export type ResourceOwner =
  | { kind: 'person'; person: PersonChoice | null; role: RoleOfNewPerson }
  | { kind: 'client' }
  | { kind: 'nobody' };

/** What reading the file gave: the preview, the reason it was refused, or a failure on the way. */
export type MsProjectReading =
  { preview: MsProjectPreview } | { refusal: MsProjectRefusal } | { failed: true };

/** Reads the file on the server, keeping nothing (ADR-0135), from the start chosen and with the client of the form. */
export async function readMsProject(
  file: File,
  start: string | undefined,
  clientId: string | undefined,
): Promise<MsProjectReading> {
  const form = new FormData();
  form.append('file', file, file.name);
  if (start !== undefined) {
    form.append('start', start);
  }
  if (clientId !== undefined) {
    form.append('clientId', clientId);
  }
  try {
    const { data, error, response } = await api.POST('/api/projects/ms-project-preview', {
      body: {
        file: '',
        ...(start === undefined ? {} : { start }),
        ...(clientId === undefined ? {} : { clientId }),
      },
      bodySerializer: () => form,
    });
    if (data !== undefined) {
      return { preview: data };
    }
    const reason = problemText(error, 'reason');
    if (response.status === 400 && problemType(error) === msProjectRefused) {
      return { refusal: isRefusal(reason) ? reason : 'NotMsProjectXml' };
    }
    return { failed: true };
  } catch {
    return { failed: true };
  }
}

/** Who each resource starts as: the person the e-mail found, the client its name names, or nobody. */
export function suggestedOwners(preview: MsProjectPreview): ResourceOwner[] {
  return preview.resources.map((resource): ResourceOwner => {
    if (resource.person !== null) {
      const { id, directoryId, name, email } = resource.person;
      return {
        kind: 'person',
        person: { id, directoryId, name, email, canBeManager: false, canBeTechLead: false },
        role: 'Contributor',
      };
    }
    return resource.client ? { kind: 'client' } : { kind: 'nobody' };
  });
}

/** Whether a resource gives its tasks an owner: a person chosen, or the client in a project with one (ADR-0135). */
export function ownsTasks(owner: ResourceOwner | undefined, hasClient: boolean): boolean {
  return owner?.kind === 'person' ? owner.person !== null : owner?.kind === 'client' && hasClient;
}

/** The resources whose tasks stay without an owner, by who each one is in "Responsáveis do arquivo" (ADR-0135). */
export function ownerlessResources(
  preview: MsProjectPreview,
  owners: readonly ResourceOwner[],
  hasClient: boolean,
): MsProjectResource[] {
  return preview.resources.filter((_resource, index) => !ownsTasks(owners[index], hasClient));
}

/** How many lines the box of the conversion lists: each warning of the server, and the tasks without an owner. */
export function adjustmentsOf(
  preview: MsProjectPreview,
  owners: readonly ResourceOwner[],
  hasClient: boolean,
): number {
  return (
    preview.warnings.length + (ownerlessResources(preview, owners, hasClient).length > 0 ? 1 : 0)
  );
}

/**
 * The schedule the preview converted, to send back on the creation, with who each resource is: a person without a
 * choice, or the client without a client in the form, is nobody.
 */
export function importedScheduleOf(
  preview: MsProjectPreview,
  owners: readonly ResourceOwner[],
  hasClient: boolean,
) {
  return {
    phases: preview.plan.phases,
    items: preview.plan.items,
    resources: preview.resources.map((_resource, index) => {
      const owner = owners[index];
      if (owner?.kind === 'person' && owner.person !== null) {
        return {
          userId: owner.person.id,
          directoryId: owner.person.id === null ? owner.person.directoryId : null,
          client: false,
          role: owner.role,
        };
      }
      return {
        userId: null,
        directoryId: null,
        client: ownsTasks(owner, hasClient),
        role: null,
      };
    }),
  };
}

/** How many names a line of the box shows before "e mais N". */
export const namesShown = 5;

/** The names of a line of the box, the first ones and how many more. */
export function namesOf(names: readonly string[]): { names: string; more: number } {
  return {
    names: names.slice(0, namesShown).join(', '),
    more: Math.max(0, names.length - namesShown),
  };
}
