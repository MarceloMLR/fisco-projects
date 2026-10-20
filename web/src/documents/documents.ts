import type { DocumentStatus } from '../api/client';

const images = ['.png', '.jpg', '.jpeg', '.gif', '.webp'];
const texts = ['.txt', '.csv'];

/** The extension of a file name, in lower case, with the dot; empty without one. */
function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.');
  return dot < 0 ? '' : name.slice(dot).toLowerCase();
}

/** The kind of a file for its icon, as in the canvas: PDF, image or a document. */
export function fileKind(name: string): 'pdf' | 'image' | 'document' {
  const extension = extensionOf(name);
  if (extension === '.pdf') {
    return 'pdf';
  }
  return images.includes(extension) ? 'image' : 'document';
}

/** Whether the browser shows the file in a tab: PDF, images, CSV and text (ADR-0031); the rest is downloaded. */
export function opensInBrowser(name: string): boolean {
  const extension = extensionOf(name);
  return extension === '.pdf' || images.includes(extension) || texts.includes(extension);
}

/** The address of a document's file: the current version, or another one; downloaded when asked. */
export function documentFileUrl(
  projectId: string,
  documentId: string,
  options: { version?: number; download?: boolean } = {},
): string {
  const query = new URLSearchParams();
  if (options.version !== undefined) {
    query.set('version', String(options.version));
  }
  if (options.download === true) {
    query.set('download', 'true');
  }
  const text = query.toString();
  return `/api/projects/${projectId}/documents/${documentId}/content${text === '' ? '' : `?${text}`}`;
}

/** The statuses a person may set: the common ones for who sends documents, and Aprovado for who approves (ADR-0045). */
export function statusesFor(permissions: {
  canUpload: boolean;
  canApprove: boolean;
}): DocumentStatus[] {
  if (!permissions.canUpload) {
    return [];
  }
  return permissions.canApprove
    ? ['Draft', 'InReview', 'Approved', 'Obsolete']
    : ['Draft', 'InReview', 'Obsolete'];
}
