/** The tabs of a project, each at its own address under the project, in the order of the canvas ProjetoCabecalho. */
export const projectTabs = [
  'overview',
  'schedule',
  'tickets',
  'documents',
  'links',
  'members',
] as const;

export type ProjectTab = (typeof projectTabs)[number];
