import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type LinkCategory, linkProblems } from '../api/client';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';

const errors = {
  nameRequired: 'linkCategories.errors.nameRequired',
  nameInUse: 'linkCategories.errors.nameInUse',
} as const;

type CategoryError = (typeof errors)[keyof typeof errors];
const isCategoryError = (message: string | undefined): message is CategoryError =>
  Object.values(errors).some((error) => error === message);

const schema = z.object({
  name: z.string().trim().min(1, errors.nameRequired).max(80, errors.nameRequired),
});

type Values = z.input<typeof schema>;

/**
 * "Nova categoria de links" and "Renomear categoria de links" (ADR-0032, ADR-0046): the name, which no other category
 * has. A new category is active and is offered for new links.
 */
export function LinkCategoryDialog({
  category,
  onClose,
  onSaved,
}: {
  /** The category to rename; none for a new one. */
  category?: LinkCategory;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: category?.name ?? '' },
  });
  const errorText = (message: string | undefined) =>
    isCategoryError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    try {
      const { error, response } =
        category === undefined
          ? await api.POST('/api/admin/link-categories', { body: { name } })
          : await api.PUT('/api/admin/link-categories/{id}', {
              params: { path: { id: category.id } },
              body: { name, isActive: category.isActive },
            });
      if (response.ok) {
        onSaved(
          t(
            category === undefined
              ? 'linkCategories.messages.added'
              : 'linkCategories.messages.saved',
            {
              name,
            },
          ),
        );
        return;
      }
      if (response.status === 409 && problemType(error) === linkProblems.categoryNameInUse) {
        setError('name', { message: errors.nameInUse });
        return;
      }
      if (response.status === 400) {
        setError('name', { message: errors.nameRequired });
        return;
      }
      setError('root', { message: t('linkCategories.messages.failed') });
    } catch {
      setError('root', { message: t('linkCategories.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(
        category === undefined
          ? 'linkCategories.dialog.newTitle'
          : 'linkCategories.dialog.editTitle',
      )}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('linkCategories.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t(category === undefined ? 'linkCategories.dialog.add' : 'linkCategories.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <TextField
          label={t('linkCategories.dialog.name')}
          autoComplete="off"
          maxLength={90}
          error={errorText(fieldErrors.name?.message)}
          {...register('name')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
