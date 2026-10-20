import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type Project, projectProblems } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { invalidFields, problemType } from './projectForm';

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

/**
 * "Salvar como modelo" (ADR-0017): the name and, optionally, the description of the model of the schedule of the
 * project, which keeps its phases, tasks, milestones, dependencies and durations, without dates nor owners.
 */
export function SaveTemplateDialog({
  project,
  onClose,
  onSaved,
}: {
  project: Project;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: project.name, description: '' },
  });
  const errorText = (message: string | undefined) =>
    isTemplateError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    try {
      const { error, response } = await api.POST('/api/projects/{id}/schedule/template', {
        params: { path: { id: project.id } },
        body: {
          name: values.name.trim(),
          description: values.description.trim() === '' ? null : values.description.trim(),
        },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.templates });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onSaved(t('projects.templates.messages.saved', { name: values.name.trim() }));
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.templateNameInUse) {
        setError('name', { message: errors.nameInUse });
        return;
      }
      const fields = invalidFields(error);
      setError('root', {
        message: t(
          fields.includes('schedule')
            ? 'projects.templates.errors.noItems'
            : 'projects.templates.errors.failed',
        ),
      });
    } catch {
      setError('root', { message: t('projects.templates.errors.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('projects.templates.saveDialog.title')}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.templates.saveDialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('projects.templates.saveDialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {t('projects.templates.saveDialog.text')}
        </Box>
        <TextField
          label={t('projects.templates.saveDialog.name')}
          autoComplete="off"
          maxLength={160}
          error={errorText(fieldErrors.name?.message)}
          {...register('name')}
        />
        <TextArea
          label={`${t('projects.templates.saveDialog.description')} ${t('projects.templates.saveDialog.optional')}`}
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
