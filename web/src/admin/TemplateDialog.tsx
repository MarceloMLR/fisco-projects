import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, projectProblems, type ProjectTemplate } from '../api/client';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';
import { Button } from '../components/Button';

const errors = {
  nameRequired: 'projects.templates.errors.nameRequired',
  descriptionTooLong: 'projects.templates.errors.descriptionTooLong',
  nameInUse: 'projects.templates.errors.nameInUse',
} as const;

type TemplateError = (typeof errors)[keyof typeof errors];
const isTemplateError = (message: string | undefined): message is TemplateError =>
  Object.values(errors).some((error) => error === message);

const schema = z.object({
  name: z.string().trim().min(1, errors.nameRequired).max(150, errors.nameRequired),
  description: z.string().max(500, errors.descriptionTooLong),
});

type Values = z.input<typeof schema>;

/** "Editar modelo": the name and the description of a model of project, by the Administrator (ADR-0017, ADR-0046). */
export function TemplateDialog({
  template,
  onClose,
  onSaved,
}: {
  template: ProjectTemplate;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: template.name, description: template.description ?? '' },
  });
  const errorText = (message: string | undefined) =>
    isTemplateError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    try {
      const { error, response } = await api.PUT('/api/admin/project-templates/{id}', {
        params: { path: { id: template.id } },
        body: {
          name: values.name.trim(),
          description: values.description.trim() === '' ? null : values.description.trim(),
          isActive: template.isActive,
        },
      });
      if (response.ok) {
        onSaved(t('templates.messages.saved', { name: values.name.trim() }));
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.templateNameInUse) {
        setError('name', { message: errors.nameInUse });
        return;
      }
      setError('root', { message: t('templates.messages.failed') });
    } catch {
      setError('root', { message: t('templates.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('templates.dialog.title')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('templates.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('templates.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <TextField
          label={t('templates.dialog.name')}
          autoComplete="off"
          maxLength={160}
          error={errorText(fieldErrors.name?.message)}
          {...register('name')}
        />
        <TextArea
          label={`${t('templates.dialog.description')} ${t('templates.dialog.optional')}`}
          rows={3}
          maxLength={520}
          error={errorText(fieldErrors.description?.message)}
          {...register('description')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
