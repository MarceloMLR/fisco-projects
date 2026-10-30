/** The color of a person by the role in the project (avatar-*, ADR-0069); a viewer has none. */
export const memberToneOf = {
  Manager: 'manager',
  TechLead: 'techlead',
  Developer: 'dev',
  Contributor: 'functional',
  Viewer: undefined,
} as const;
