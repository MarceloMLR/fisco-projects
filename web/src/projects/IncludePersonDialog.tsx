import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQueryClient } from '@tanstack/react-query';
import { useId } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type Project, type ProjectRole, projectProblems, projectRoles } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { PersonField, type PersonChoice } from '../components/PersonField';
import { Select } from '../components/Select';
import { invalidFields, problemType } from './projectForm';

const errors = {
  personRequired: 'projects.members.errors.personRequired',
  personInvalid: 'projects.members.errors.personInvalid',
  roleInvalid: 'projects.members.errors.roleInvalid',
  exists: 'projects.members.errors.exists',
} as const;

type ErrorKey = (typeof errors)[keyof typeof errors];

const isErrorKey = (message: string | undefined): message is ErrorKey =>
  Object.values(errors).some((key) => key === message);

const form = z.object({
  person: z.custom<PersonChoice | null>().refine((person) => person !== null, {
    error: errors.personRequired,
  }),
  role: z.enum(projectRoles),
});

type FormValues = z.input<typeof form>;

/**
 * "Incluir pessoa" of the Members tab (ADR-0042): the person, searched in the system and in the company directory
 * (ADR-0005), without who is already in the project, and the role, Leitor by default, the least that is given. The tech
 * lead does not see the role Gerente de projeto, and a person who may not be a manager cannot get it (ADR-0085).
 */
export function IncludePersonDialog({
  project,
  memberIds,
  onClose,
  onIncluded,
}: {
  project: Project;
  /** The people already in the project, not offered. */
  memberIds: readonly string[];
  onClose: () => void;
  onIncluded: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const roleHelpId = useId();
  const {
    control,
    handleSubmit,
    setError,
    setValue,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: { person: null, role: 'Viewer' },
  });
  const person = useWatch({ control, name: 'person' });
  const { canManageManagers } = project.permissions;
  const notManager = canManageManagers && person !== null && !person.canBeManager;
  // Only who has the system role leads a project technically (ADR-0097).
  const notTechLead = person !== null && !person.canBeTechLead;
  const roleOptions = projectRoles
    .filter((role) => role !== 'Manager' || canManageManagers)
    .map((role) => ({
      value: role,
      label: t(`projects.roles.${role}`),
      disabled: (role === 'Manager' && notManager) || (role === 'TechLead' && notTechLead),
    }));
  const errorText = (message: string | undefined) => (isErrorKey(message) ? t(message) : undefined);

  const include = handleSubmit(async ({ person: chosen, role }) => {
    if (chosen === null) {
      return;
    }
    try {
      const { error, response } = await api.POST('/api/projects/{id}/members', {
        params: { path: { id: project.id } },
        body: { userId: chosen.id, directoryId: chosen.directoryId, role },
      });
      if (response.ok) {
        // The project, its people and, for a person of the directory, the user it became.
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.people });
        void queryClient.invalidateQueries({ queryKey: queryKeys.users });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onIncluded(t('projects.members.messages.included', { name: chosen.name }));
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.memberExists) {
        setError('person', { message: errors.exists });
        return;
      }
      const fields = invalidFields(error);
      if (fields.includes('person')) {
        setError('person', { message: errors.personInvalid });
      } else if (fields.includes('role')) {
        setError('role', { message: errors.roleInvalid });
      } else {
        setError('root', { message: t('projects.members.messages.failed') });
      }
    } catch {
      setError('root', { message: t('projects.members.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('projects.members.dialog.title')}
      onClose={onClose}
      onSubmit={(event) => {
        void include(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.members.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('projects.members.dialog.submit')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Controller
          control={control}
          name="person"
          render={({ field }) => (
            <PersonField
              label={t('projects.members.dialog.person')}
              value={field.value}
              onChange={(chosen) => {
                field.onChange(chosen);
                // A tech lead of the system comes as tech lead (ADR-0097); who may not be a manager does not keep
                // the role.
                if (chosen !== null && chosen.canBeTechLead && !chosen.canBeManager) {
                  setValue('role', 'TechLead' satisfies ProjectRole);
                } else if (chosen !== null && !chosen.canBeManager) {
                  setValue('role', 'Viewer' satisfies ProjectRole);
                }
              }}
              placeholder={t('projects.members.dialog.personPlaceholder')}
              exclude={memberIds}
              error={errorText(fieldErrors.person?.message)}
            />
          )}
        />
        <Controller
          control={control}
          name="role"
          render={({ field }) => {
            const roleError = errorText(fieldErrors.role?.message);
            const helper =
              notManager && notTechLead
                ? t('projects.members.dialog.notManagerNorTechLead')
                : notManager
                  ? t('projects.members.dialog.notManager')
                  : notTechLead
                    ? t('projects.members.dialog.notTechLead')
                    : undefined;
            return (
              <FormField
                label={t('projects.members.dialog.role')}
                error={roleError}
                helper={helper}
                helpId={roleHelpId}
              >
                <Select<ProjectRole>
                  label={t('projects.members.dialog.role')}
                  value={field.value}
                  options={roleOptions}
                  onChange={field.onChange}
                  invalid={roleError !== undefined}
                  describedBy={
                    roleError !== undefined || helper !== undefined ? roleHelpId : undefined
                  }
                />
              </FormField>
            );
          }}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
