import Box from '@mui/material/Box';
import { type SyntheticEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type DocumentFolder, type FolderRole } from '../api/client';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { FolderVisibilityFields } from './FolderVisibilityFields';

/**
 * "Quem vê esta pasta" of the documents of a project (ADR-0148), only for the Administrator: the roles that see the
 * folder, besides the Administrator and the manager. The change goes to the audit with the project.
 */
export function FolderVisibilityDialog({
  projectId,
  folder,
  onClose,
  onSaved,
}: {
  projectId: string;
  folder: DocumentFolder;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const [roles, setRoles] = useState<FolderRole[]>(folder.visibleTo);
  const [failure, setFailure] = useState<string>();

  const save = async (event: SyntheticEvent) => {
    event.preventDefault();
    setFailure(undefined);
    try {
      const { response } = await api.PUT(
        '/api/projects/{projectId}/documents/folders/{folderId}/visibility',
        {
          params: { path: { projectId, folderId: folder.id } },
          body: { visibleTo: roles },
        },
      );
      if (response.ok) {
        onSaved(t('folderVisibility.messages.saved', { name: folder.name }));
        return;
      }
      setFailure(
        t(
          response.status === 409
            ? 'folderVisibility.messages.closed'
            : 'folderVisibility.messages.failed',
        ),
      );
    } catch {
      setFailure(t('folderVisibility.messages.failed'));
    }
  };

  return (
    <Dialog
      open
      title={t('folderVisibility.dialog.title')}
      subtitle={folder.name}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('folderVisibility.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('folderVisibility.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <FolderVisibilityFields value={roles} onChange={setRoles} />
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      </Box>
    </Dialog>
  );
}
