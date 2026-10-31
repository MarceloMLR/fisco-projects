import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { z } from 'zod';
import { api, type Project, projectProblems } from '../api/client';
import { generalRulesQuery, projectFolderQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { ClientSelectField } from './ClientSelectField';
import type { ChosenFolder } from './ProjectFolderDialog';
import { ProjectFolderField } from './ProjectFolderField';
import { systemFolderPath } from './projectFolder';
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
import { Button } from '../components/Button';

type FormValues = z.input<typeof projectDataSchema>;

/**
 * "Editar dados" of a project: the code, the name, the client and the description (ADR-0016). The administrator, the
 * manager and the tech lead change them while the project is open (ADR-0043). The administrator and the manager also
 * see the folder of the files, which changes until the project stores a file (ADR-0122).
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
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(projectDataSchema),
    defaultValues: {
      code: project.code,
      name: project.name,
      description: project.description ?? '',
      clientId: project.client?.id ?? noClient,
    },
  });

  const canChooseFolder = project.permissions.canChooseFolder;
  const folderInfo = useQuery({ ...projectFolderQuery(project.id), enabled: canChooseFolder });
  const rules = useQuery({ ...generalRulesQuery(), enabled: canChooseFolder });
  // Undefined while the folder stays as it is; null goes back to the folder the system creates.
  const [folder, setFolder] = useState<ChosenFolder | null>();
  const [folderError, setFolderError] = useState<string>();
  const typedCode = useWatch({ control, name: 'code' });
  const typedName = useWatch({ control, name: 'name' });
  const current = folderInfo.data;
  const shownFolder =
    folder !== undefined
      ? folder
      : current?.chosen === true
        ? { id: '', path: current.name }
        : null;
  const systemText =
    current !== undefined && !current.chosen && folder === undefined
      ? current.name
      : (systemFolderPath(rules.data?.projectsFolder ?? '', typedCode, typedName) ??
        t('projects.folder.pending'));

  /** Changes the folder first, so nothing is saved when the folder is refused; true when the data may follow. */
  const changeFolder = async () => {
    if (folder === undefined) {
      return true;
    }
    const { error, response } = await api.PUT('/api/projects/{id}/folder', {
      params: { path: { id: project.id } },
      body: { folderId: folder?.id ?? null },
    });
    if (response.ok) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      return true;
    }
    if (response.status === 409 && problemType(error) === projectProblems.hasFiles) {
      setFolderError(t('projects.folder.locked'));
    } else if (response.status === 400) {
      setFolderError(t('projects.folder.invalid'));
    } else {
      setError('root', { message: t('projects.messages.failed') });
    }
    return false;
  };

  const errorText = (message: string | undefined) =>
    isProjectFieldError(message) ? t(message) : undefined;
  const codeError =
    errors.code?.type === 'codeInUse'
      ? t('projects.errors.codeInUse', { name: errors.code.message ?? '' })
      : errorText(errors.code?.message);

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    try {
      if (!(await changeFolder())) {
        return;
      }
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
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.form.cancel')}
          </Button>
          <Button type="submit" variant="contained">
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
      {canChooseFolder && current !== undefined && (
        <ProjectFolderField
          value={shownFolder}
          onChange={(chosen) => {
            setFolder(chosen);
            setFolderError(undefined);
          }}
          systemText={systemText}
          systemHelper={t('projects.folder.systemHelper')}
          chosenHelper={t('projects.folder.chosenHelper')}
          projectId={project.id}
          locked={current.hasFiles ? t('projects.folder.locked') : undefined}
          error={folderError}
        />
      )}
      {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
    </Dialog>
  );
}
