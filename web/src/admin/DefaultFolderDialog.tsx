import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type DefaultFolder, documentProblems } from '../api/client';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';
import { Button } from '../components/Button';
import { FolderVisibilityFields } from '../documents/FolderVisibilityFields';
import { folderRoles } from '../documents/folderVisibility';

const errors = {
  nameRequired: 'defaultFolders.errors.nameRequired',
  nameInUse: 'defaultFolders.errors.nameInUse',
} as const;

type FolderError = (typeof errors)[keyof typeof errors];
const isFolderError = (message: string | undefined): message is FolderError =>
  Object.values(errors).some((error) => error === message);

// The names SharePoint takes for a folder (ADR-0030), as the API checks them.
const refused = /["*:<>?/\\|]/;

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, errors.nameRequired)
    .max(120, errors.nameRequired)
    .refine((name) => !refused.test(name) && !name.endsWith('.'), errors.nameRequired),
  visibleTo: z.array(z.enum(folderRoles)),
});

type Values = z.input<typeof schema>;

/**
 * "Nova pasta padrão" and "Editar pasta padrão" (ADR-0031, ADR-0046): the name, which no other default folder has, and
 * who sees the folder in the new projects (ADR-0148), the four roles for a new one. A new folder is active, and every
 * new project gets it. The folder of the acceptance terms is only of the managers.
 */
export function DefaultFolderDialog({
  folder,
  onClose,
  onSaved,
}: {
  /** The folder to change; none for a new one. */
  folder?: DefaultFolder;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: folder?.name ?? '', visibleTo: folder?.visibleTo ?? [...folderRoles] },
  });
  const isAcceptance = folder?.isAcceptance === true;
  const errorText = (message: string | undefined) =>
    isFolderError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    // The folder of the terms takes no roles: it stays only of the managers.
    const visibleTo = isAcceptance ? undefined : values.visibleTo;
    try {
      const { error, response } =
        folder === undefined
          ? await api.POST('/api/admin/document-folders', { body: { name, visibleTo } })
          : await api.PUT('/api/admin/document-folders/{id}', {
              params: { path: { id: folder.id } },
              body: { name, isActive: folder.isActive, visibleTo },
            });
      if (response.ok) {
        onSaved(
          t(
            folder === undefined
              ? 'defaultFolders.messages.added'
              : 'defaultFolders.messages.saved',
            {
              name,
            },
          ),
        );
        return;
      }
      if (response.status === 409 && problemType(error) === documentProblems.folderNameInUse) {
        setError('name', { message: errors.nameInUse });
        return;
      }
      if (response.status === 400) {
        setError('name', { message: errors.nameRequired });
        return;
      }
      setError('root', { message: t('defaultFolders.messages.failed') });
    } catch {
      setError('root', { message: t('defaultFolders.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(
        folder === undefined ? 'defaultFolders.dialog.newTitle' : 'defaultFolders.dialog.editTitle',
      )}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('defaultFolders.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(folder === undefined ? 'defaultFolders.dialog.add' : 'defaultFolders.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <TextField
          label={t('defaultFolders.dialog.name')}
          autoComplete="off"
          maxLength={130}
          helper={t('defaultFolders.dialog.nameHelp')}
          error={errorText(fieldErrors.name?.message)}
          {...register('name')}
        />
        <Controller
          control={control}
          name="visibleTo"
          render={({ field }) => (
            <FolderVisibilityFields
              value={field.value}
              onChange={field.onChange}
              isAcceptance={isAcceptance}
            />
          )}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
