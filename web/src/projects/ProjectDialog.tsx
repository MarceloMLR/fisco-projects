import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import { useQueryClient } from '@tanstack/react-query';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { z } from 'zod';
import { api, type Project, projectProblems } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { ClientSelectField } from './ClientSelectField';
import {
  invalidFields,
  isProjectFieldError,
  maskTypedCode,
  noClient,
  problemText,
  problemType,
  projectDataSchema,
  projectFieldErrors,
} from './projectForm';

type FormValues = z.input<typeof projectDataSchema>;

/**
 * "Editar dados" of a project: the code, the name, the client and the description (ADR-0016). The administrator, the
 * manager and the tech lead change them while the project is open (ADR-0043).
 */
export function ProjectDialog({
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
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(projectDataSchema),
    defaultValues: {
      code: project.code,
      name: project.name,
      description: project.description ?? '',
      clientId: project.client?.id ?? noClient,
    },
  });

  const errorText = (message: string | undefined) =>
    isProjectFieldError(message) ? t(message) : undefined;
  const codeError =
    errors.code?.type === 'codeInUse'
      ? t('projects.errors.codeInUse', { name: errors.code.message ?? '' })
      : errorText(errors.code?.message);

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    try {
      const { error, response } = await api.PUT('/api/projects/{id}', {
        params: { path: { id: project.id } },
        body: {
          code: values.code.trim().toUpperCase(),
          name,
          description: values.description.trim() === '' ? null : values.description.trim(),
          clientId: values.clientId === noClient ? null : values.clientId,
        },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onSaved(t('projects.messages.changed', { name }));
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.codeInUse) {
        setError('code', { type: 'codeInUse', message: problemText(error, 'projectName') });
        return;
      }
      const fields = invalidFields(error);
      const byField = {
        code: ['code', projectFieldErrors.codeInvalid],
        name: ['name', projectFieldErrors.nameRequired],
        description: ['description', projectFieldErrors.descriptionTooLong],
        clientId: ['clientId', projectFieldErrors.clientInvalid],
      } as const;
      const known = Object.entries(byField).filter(([field]) => fields.includes(field));
      for (const [, [field, message]] of known) {
        setError(field, { message });
      }
      if (known.length === 0) {
        setError('root', { message: t('projects.messages.failed') });
      }
    } catch {
      setError('root', { message: t('projects.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('projects.form.editTitle')}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.form.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('projects.form.save')}
          </Button>
        </>
      }
    >
      <Controller
        control={control}
        name="code"
        render={({ field }) => (
          <TextField
            label={t('projects.form.code')}
            helper={t('projects.form.codeHelper')}
            autoComplete="off"
            spellCheck={false}
            width={200}
            error={codeError}
            name={field.name}
            ref={field.ref}
            value={field.value}
            onBlur={field.onBlur}
            onChange={(event) => {
              field.onChange(maskTypedCode(event.target.value));
            }}
          />
        )}
      />
      <TextField
        label={t('projects.form.name')}
        autoComplete="off"
        maxLength={200}
        error={errorText(errors.name?.message)}
        {...register('name')}
      />
      <Controller
        control={control}
        name="clientId"
        render={({ field }) => (
          <ClientSelectField
            value={field.value}
            onChange={field.onChange}
            currentClientId={project.client?.id}
            error={errorText(errors.clientId?.message)}
          />
        )}
      />
      <TextArea
        label={`${t('projects.form.description')} ${t('projects.form.optional')}`}
        rows={4}
        maxLength={4500}
        error={errorText(errors.description?.message)}
        {...register('description')}
      />
      {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
    </Dialog>
  );
}
