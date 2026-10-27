/**
 * The name of the folder the system creates for a project, "{code} - {name}", with "/" and "\" as "-", as the server
 * names it (Project.FolderNameOf, ADR-0121).
 */
export function folderNameOf(code: string, name: string) {
  return `${code} - ${name}`.replace(/[/\\]/g, '-');
}

/** A path of the file storage as the screens show it: "Projetos/Clientes" as "Projetos / Clientes". */
export function shownPath(path: string) {
  return path
    .split('/')
    .map((segment) => segment.trim())
    .filter((segment) => segment !== '')
    .join(' / ');
}

/**
 * Where the system creates the folder of a project: inside the folder of the projects of the installation, or at the
 * root without one (ADR-0121). Undefined while the code or the name is empty.
 */
export function systemFolderPath(projectsFolder: string, code: string, name: string) {
  if (code.trim() === '' || name.trim() === '') {
    return undefined;
  }
  return shownPath(`${projectsFolder}/${folderNameOf(code.trim().toUpperCase(), name.trim())}`);
}
