import type { ProjectMember, ProjectRole } from '../api/client';

/** The teams of a project on the Members tab (ADR-0040, ADR-0068), in the order of the canvas ProjetoMembros. */
export const memberTeams = ['management', 'functional', 'development', 'viewers'] as const;

export type MemberTeam = (typeof memberTeams)[number];

/** The teams of a role: the tech lead is in Gestão and in Desenvolvimento. */
export function teamsOfRole(role: ProjectRole): readonly MemberTeam[] {
  switch (role) {
    case 'Manager':
      return ['management'];
    case 'TechLead':
      return ['management', 'development'];
    case 'Contributor':
      return ['functional'];
    case 'Developer':
      return ['development'];
    case 'Viewer':
      return ['viewers'];
  }
}

/** A line of the list of the people: the name of a team, with how many are in it, or a person. */
export type MemberRow =
  | { kind: 'group'; id: string; team: MemberTeam; count: number }
  | { kind: 'person'; id: string; member: ProjectMember };

export const isGroupRow = (row: MemberRow) => row.kind === 'group';

const simplify = (text: string) => text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

/** The people whose name or e-mail has the text, without telling case or accents apart. */
export function searchMembers(members: readonly ProjectMember[], search: string): ProjectMember[] {
  const text = simplify(search.trim());
  return members.filter(
    (member) => simplify(member.name).includes(text) || simplify(member.email).includes(text),
  );
}

/**
 * The lines of the list: by team, each team with its name and its people, a team without people left out, or by
 * name, without the teams. The people come in alphabetical order from the API.
 */
export function memberRows(members: readonly ProjectMember[], byTeam: boolean): MemberRow[] {
  if (!byTeam) {
    return members.map((member) => ({ kind: 'person', id: member.id, member }));
  }
  return memberTeams.flatMap((team) => {
    const people = members.filter((member) => teamsOfRole(member.role).includes(team));
    return people.length === 0
      ? []
      : [
          { kind: 'group', id: `group:${team}`, team, count: people.length } as const,
          ...people.map(
            (member) => ({ kind: 'person', id: `${team}:${member.id}`, member }) as const,
          ),
        ];
  });
}

/**
 * Whether the person in the session may include (no current role), change or remove (no new role) a person of the
 * project: the tech lead does it with everyone but the managers, and never makes someone a manager (ADR-0042). The
 * same rule as ProjectPermissions.MayManage in the Projects module, from the permissions the API gives.
 */
export function mayManage(
  permissions: { canManagePeople: boolean; canManageManagers: boolean },
  current: ProjectRole | null,
  next: ProjectRole | null,
): boolean {
  return (
    permissions.canManagePeople &&
    (permissions.canManageManagers || (current !== 'Manager' && next !== 'Manager'))
  );
}
