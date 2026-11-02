import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api } from '../api/client';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';

// Where the folders of the new projects go in the file storage; the server checks the names (ADR-0121).
const schema = z.object({ projectsFolder: z.string().trim() });

type Values = z.input<typeof schema>;

/**
 * "Pasta dos projetos", opened from Default folders (ADR-0121, ADR-0150): where the folders of the new projects go in
 * the file storage, with the folders separated by "/", or empty for its root. The server checks the names, and its
 * refusal comes back to the field.
 */
export function ProjectsFolderDialog({
  current,
  onClose,
  onSaved,
}: {
  /** The folder of the projects as saved, empty for the root of the storage. */
  current: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { projectsFolder: current },
  });

  const save = handleSubmit(async ({ projectsFolder }) => {
    try {
      const { response } = await api.PUT('/api/installation/projects-folder', {
        body: { projectsFolder: projectsFolder.trim() },
      });
      if (response.ok) {
        onSaved();
        return;
      }
      if (response.status === 400) {
        setError('projectsFolder', { message: t('defaultFolders.projectsFolder.invalid') });
        return;
      }
      setError('root', { message: t('defaultFolders.projectsFolder.failed') });
    } catch {
      setError('root', { message: t('defaultFolders.projectsFolder.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('defaultFolders.projectsFolder.title')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('defaultFolders.projectsFolder.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('defaultFolders.projectsFolder.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <TextField
          label={t('defaultFolders.projectsFolder.field')}
          autoComplete="off"
          spellCheck={false}
          helper={t('defaultFolders.projectsFolder.hint')}
          error={errors.projectsFolder?.message}
          {...register('projectsFolder')}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}
