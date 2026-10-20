import { api, documentProblems } from '../api/client';
import { problemType } from '../projects/projectForm';

/** Why a file was not sent: the reasons of the API (FileRefusal), someone sent it at the same time, or another failure. */
export type UploadRefusal = 'Empty' | 'TooLarge' | 'Dangerous' | 'Changed' | 'Failed';

/** What happened to the files sent: how many went and which were refused, with the reason. */
export interface UploadOutcome {
  sent: number;
  refused: { name: string; reason: UploadRefusal }[];
  /** The project was completed or cancelled in the meantime: read-only (ADR-0016). */
  closed: boolean;
}

const reasons: readonly string[] = ['Empty', 'TooLarge', 'Dangerous'];

function reasonOf(problem: unknown): UploadRefusal {
  const reason =
    typeof problem === 'object' && problem !== null && 'reason' in problem
      ? problem.reason
      : undefined;
  return typeof reason === 'string' && reasons.includes(reason)
    ? (reason as UploadRefusal)
    : 'Failed';
}

/**
 * Sends the files to a folder of the project, one at a time (ADR-0031), telling the progress before each one. A file
 * with the name of a document of the folder becomes its next version. Stops at a closed project.
 */
export async function uploadDocuments(
  projectId: string,
  folderId: string,
  files: readonly File[],
  onProgress: (current: number, total: number) => void,
): Promise<UploadOutcome> {
  const outcome: UploadOutcome = { sent: 0, refused: [], closed: false };
  for (const [index, file] of files.entries()) {
    onProgress(index + 1, files.length);
    const form = new FormData();
    form.append('folderId', folderId);
    form.append('file', file, file.name);
    try {
      const { error, response } = await api.POST('/api/projects/{projectId}/documents', {
        params: { path: { projectId } },
        body: { folderId, file: '' },
        bodySerializer: () => form,
      });
      if (response.ok) {
        outcome.sent += 1;
        continue;
      }
      const type = problemType(error);
      if (type === documentProblems.projectClosed) {
        outcome.closed = true;
        return outcome;
      }
      outcome.refused.push({
        name: file.name,
        reason:
          type === documentProblems.documentRefused
            ? reasonOf(error)
            : type === documentProblems.documentChanged
              ? 'Changed'
              : 'Failed',
      });
    } catch {
      outcome.refused.push({ name: file.name, reason: 'Failed' });
    }
  }
  return outcome;
}
