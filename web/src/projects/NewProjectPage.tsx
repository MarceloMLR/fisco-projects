import { zodResolver } from '@hookform/resolvers/zod';
import AddOutlined from '@mui/icons-material/AddOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { z } from 'zod';
import { api, projectProblems } from '../api/client';
import { queryKeys, templatesQuery } from '../api/queries';
import { ClientDialog } from '../admin/ClientDialog';
import { Alert } from '../components/Alert';
import { FormField } from '../components/FormField';
import { PersonField, type PersonChoice } from '../components/PersonField';
import { Select } from '../components/Select';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ClientSelectField } from './ClientSelectField';
import { ScheduleOriginChoice, TemplatePreview } from './TemplateChoice';
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

const form = projectDataSchema
  .extend({
    manager: z.custom<PersonChoice | null>().refine((person) => person !== null, {
      error: projectFieldErrors.managerRequired,
    }),
    techLead: z.custom<PersonChoice | null>(),
    origin: z.enum(['blank', 'template']),
    templateId: z.string(),
    scheduleStart: z.string(),
  })
  .superRefine((values, context) => {
    // From a model, the model and the start are needed (ADR-0017).
    if (values.origin === 'template' && values.templateId === '') {
      context.addIssue({
        code: 'custom',
        path: ['templateId'],
        message: projectFieldErrors.templateRequired,
      });
    }
    if (values.origin === 'template' && values.scheduleStart === '') {
      context.addIssue({
        code: 'custom',
        path: ['scheduleStart'],
        message: projectFieldErrors.scheduleStartRequired,
      });
    }
  });

type FormValues = z.input<typeof form>;

/** A section of the form (fp-form-section) and its title (fp-h2). */
const sectionSx = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-4)',
  padding: 'var(--space-6)',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
} as const;

const sectionTitleSx = {
  margin: 0,
  fontSize: 18,
  lineHeight: '26px',
  fontWeight: 500,
  letterSpacing: '-0.005em',
  color: 'var(--text)',
} as const;

// Below 700 px the fields of a row stack (fp-form-row).
const narrowForm = '@media (max-width: 700px)';

/**
 * Novo projeto (canvas NovoProjeto): the section "Dados do projeto" and the section "Cronograma", "Em branco" or "A
 * partir de um modelo", with the model, the start and its preview beside the form (ADR-0017). The
 * manager comes with the person who creates the project, marked "(você)"; with another manager, the screen says the
 * person does not join it (ADR-0072). "Cadastrar cliente" includes a client and chooses it.
 */
export function NewProjectPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [newClient, setNewClient] = useState(false);
  const me = session.status === 'signed-in' ? session.user : undefined;
  const isAdministrator = me?.role === 'Administrator';

  const {
    register,
    control,
    handleSubmit,
    setError,
    setValue,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: {
      code: '',
      name: '',
      description: '',
      clientId: noClient,
      manager:
        me === undefined
          ? null
          : { id: me.id, directoryId: null, name: me.name, canBeManager: true },
      techLead: null,
      origin: 'blank',
      templateId: '',
      scheduleStart: '',
    },
  });
  const templates = useQuery(templatesQuery());
  const activeTemplates = templates.data ?? [];
  const origin = useWatch({ control, name: 'origin' });
  const templateId = useWatch({ control, name: 'templateId' });
  const chosenTemplate =
    origin === 'template'
      ? activeTemplates.find((template) => template.id === templateId)
      : undefined;

  const manager = useWatch({ control, name: 'manager' });
  const otherManager = manager !== null && me !== undefined && manager.id !== me.id;
  const techLead = useWatch({ control, name: 'techLead' });
  const errorText = (message: string | undefined) =>
    isProjectFieldError(message) ? t(message) : undefined;
  const codeError =
    errors.code?.type === 'codeInUse'
      ? t('projects.errors.codeInUse', { name: errors.code.message ?? '' })
      : errorText(errors.code?.message);

  const create = handleSubmit(async (values) => {
    try {
      const { data, error, response } = await api.POST('/api/projects', {
        body: {
          code: values.code.trim().toUpperCase(),
          name: values.name.trim(),
          description: values.description.trim() === '' ? null : values.description.trim(),
          clientId: values.clientId === noClient ? null : values.clientId,
          managerId: values.manager?.id ?? null,
          // The tech lead may be a person of the directory who never signed in (ADR-0005).
          techLeadId: values.techLead?.id ?? null,
          techLeadDirectoryId: values.techLead?.directoryId ?? null,
          templateId: values.origin === 'template' ? values.templateId : null,
          scheduleStart: values.origin === 'template' ? values.scheduleStart : null,
        },
      });
      if (data !== undefined) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        await navigate(`/projects/${data.id}`);
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
        managerId: ['manager', projectFieldErrors.managerInvalid],
        techLeadId: ['techLead', projectFieldErrors.techLeadInvalid],
        techLeadDirectoryId: ['techLead', projectFieldErrors.techLeadInvalid],
        templateId: ['templateId', projectFieldErrors.templateInvalid],
        scheduleStart: ['scheduleStart', projectFieldErrors.scheduleStartRequired],
      } as const;
      let known = false;
      for (const [field, [name, message]] of Object.entries(byField)) {
        if (fields.includes(field)) {
          setError(name, { message });
          known = true;
        }
      }
      if (!known) {
        setError('root', { message: t('projects.messages.failed') });
      }
    } catch {
      setError('root', { message: t('projects.messages.failed') });
    }
  });

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page>
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gridTemplateColumns:
              chosenTemplate !== undefined && desktop ? 'minmax(0, 1fr) 400px' : 'minmax(0, 1fr)',
            gap: 'var(--space-4)',
          }}
        >
          <Box
            component="form"
            id="new-project"
            noValidate
            onSubmit={(event) => {
              void create(event);
            }}
            sx={{ minHeight: 0, overflow: 'auto' }}
          >
            <Box component="section" aria-labelledby="new-project-data" sx={sectionSx}>
              <Box component="h2" id="new-project-data" sx={sectionTitleSx}>
                {t('projects.form.data')}
              </Box>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: '200px minmax(0, 1fr)',
                  gap: 'var(--space-4)',
                  [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
                }}
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
              </Box>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(3, minmax(0, 1fr))',
                  gap: 'var(--space-4)',
                  alignItems: 'start',
                  [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
                }}
              >
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: '6px', minWidth: 0 }}>
                  <Controller
                    control={control}
                    name="clientId"
                    render={({ field }) => (
                      <ClientSelectField
                        value={field.value}
                        onChange={field.onChange}
                        error={errorText(errors.clientId?.message)}
                      />
                    )}
                  />
                  <Box>
                    <Button
                      variant="text"
                      size="small"
                      startIcon={<AddOutlined />}
                      onClick={() => {
                        setNewClient(true);
                      }}
                    >
                      {t('projects.form.newClient')}
                    </Button>
                  </Box>
                </Box>
                <Controller
                  control={control}
                  name="manager"
                  render={({ field }) => (
                    <PersonField
                      label={t('projects.form.manager')}
                      value={field.value}
                      onChange={field.onChange}
                      managersOnly
                      youId={me?.id}
                      // The manager and the tech lead are two people (ADR-0072).
                      exclude={techLead?.id == null ? undefined : [techLead.id]}
                      error={errorText(errors.manager?.message)}
                      helper={otherManager ? t('projects.form.notMember') : undefined}
                    />
                  )}
                />
                <Controller
                  control={control}
                  name="techLead"
                  render={({ field }) => (
                    <PersonField
                      label={t('projects.form.techLead')}
                      optional={t('projects.form.optional')}
                      value={field.value}
                      onChange={field.onChange}
                      placeholder={t('projects.form.noTechLead')}
                      youId={me?.id}
                      exclude={manager?.id == null ? undefined : [manager.id]}
                      error={errorText(errors.techLead?.message)}
                    />
                  )}
                />
              </Box>
              <TextArea
                label={`${t('projects.form.description')} ${t('projects.form.optional')}`}
                rows={4}
                maxLength={4500}
                error={errorText(errors.description?.message)}
                {...register('description')}
              />
            </Box>
            <Box
              component="section"
              aria-labelledby="new-project-schedule"
              sx={{ ...sectionSx, marginTop: 'var(--space-4)' }}
            >
              <Box component="h2" id="new-project-schedule" sx={sectionTitleSx}>
                {t('projects.form.schedule')}
              </Box>
              <Controller
                control={control}
                name="origin"
                render={({ field }) => (
                  <ScheduleOriginChoice
                    value={field.value}
                    hasTemplates={activeTemplates.length > 0}
                    labelledBy="new-project-schedule"
                    onChange={(value) => {
                      field.onChange(value);
                      // The first model comes chosen.
                      const first = activeTemplates[0];
                      if (
                        value === 'template' &&
                        getValues('templateId') === '' &&
                        first !== undefined
                      ) {
                        setValue('templateId', first.id);
                      }
                    }}
                  />
                )}
              />
              {origin === 'template' && (
                <Box
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: 'minmax(0, 1fr) 200px',
                    gap: 'var(--space-4)',
                    alignItems: 'start',
                    [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
                  }}
                >
                  <Controller
                    control={control}
                    name="templateId"
                    render={({ field }) => (
                      <FormField
                        label={t('projects.form.template')}
                        error={errorText(errors.templateId?.message)}
                        helpId="new-project-template-help"
                      >
                        <Select
                          label={t('projects.form.template')}
                          value={field.value}
                          onChange={field.onChange}
                          options={activeTemplates.map((template) => ({
                            value: template.id,
                            label: template.name,
                          }))}
                          invalid={errors.templateId !== undefined}
                          describedBy={
                            errors.templateId === undefined
                              ? undefined
                              : 'new-project-template-help'
                          }
                        />
                      </FormField>
                    )}
                  />
                  <TextField
                    label={t('projects.form.scheduleStart')}
                    type="date"
                    error={errorText(errors.scheduleStart?.message)}
                    {...register('scheduleStart')}
                  />
                </Box>
              )}
              {errors.root?.message !== undefined && (
                <Alert tone="danger">{errors.root.message}</Alert>
              )}
            </Box>
            {chosenTemplate !== undefined && !desktop && (
              <Box sx={{ marginTop: 'var(--space-4)' }}>
                <TemplatePreview template={chosenTemplate} />
              </Box>
            )}
          </Box>
          {chosenTemplate !== undefined && desktop && <TemplatePreview template={chosenTemplate} />}
        </Box>
      </Page>
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          gap: 'var(--space-2)',
          padding: desktop ? 'var(--space-3) var(--space-6)' : 'var(--space-3)',
          backgroundColor: 'var(--bg-page)',
          borderTop: '1px solid var(--border)',
        }}
      >
        <Button
          variant="outlined"
          onClick={() => {
            void navigate('/projects');
          }}
        >
          {t('projects.form.cancel')}
        </Button>
        <Button variant="contained" type="submit" form="new-project" disabled={isSubmitting}>
          {t('projects.form.create')}
        </Button>
      </Box>
      {newClient && (
        <ClientDialog
          // The logo is the Administrator's (ADR-0046).
          canSendLogo={isAdministrator}
          onClose={() => {
            setNewClient(false);
          }}
          onSaved={(_message, id) => {
            setNewClient(false);
            void queryClient.invalidateQueries({ queryKey: queryKeys.clients });
            setValue('clientId', id === '' ? noClient : id);
          }}
        />
      )}
    </Box>
  );
}
