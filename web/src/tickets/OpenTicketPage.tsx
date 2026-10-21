import { zodResolver } from '@hookform/resolvers/zod';
import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { visuallyHidden } from '@mui/utils';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { lazy, type ReactNode, Suspense, useEffect, useId, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { z } from 'zod';
import {
  api,
  projectProblems,
  type TicketPriority,
  type TicketTeam,
  ticketPriorities,
} from '../api/client';
import { queryKeys, ticketChoicesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { invalidFields, problemType } from '../projects/projectForm';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ChooseFiles, ChosenFiles } from './TicketReply';
import { PriorityTag } from './TicketTags';
import { ticketProblemText } from './ticketProblems';
import { teamIcons } from './tickets';

// The editor only loads on the screens of the tickets.
const RichTextEditor = lazy(async () => ({
  default: (await import('../components/RichTextEditor')).RichTextEditor,
}));

const errors = {
  projectRequired: 'tickets.errors.projectRequired',
  teamRequired: 'tickets.errors.teamRequired',
  titleRequired: 'tickets.errors.titleRequired',
  titleTooLong: 'tickets.errors.titleTooLong',
  descriptionRequired: 'tickets.errors.descriptionRequired',
  descriptionTooLong: 'tickets.errors.descriptionTooLong',
} as const;
type TicketFieldError = (typeof errors)[keyof typeof errors];
const isTicketFieldError = (message: string | undefined): message is TicketFieldError =>
  Object.values(errors).some((error) => error === message);

const form = z.object({
  projectId: z.string().min(1, { error: errors.projectRequired }),
  team: z.string().min(1, { error: errors.teamRequired }),
  priority: z.enum(ticketPriorities),
  title: z
    .string()
    .trim()
    .min(1, { error: errors.titleRequired })
    .max(200, { error: errors.titleTooLong }),
  // The document of the editor, and whether it has any text (ADR-0026).
  description: z.string(),
  descriptionEmpty: z.boolean().refine((empty) => !empty, { error: errors.descriptionRequired }),
});

type FormValues = z.input<typeof form>;

/** One option of a small choice in cards (fp-opt fp-opt--inline): the radio, an icon and the text. */
function ChoiceCard({
  name,
  value,
  checked,
  onChange,
  children,
}: {
  name: string;
  value: string;
  checked: boolean;
  onChange: (value: string) => void;
  children: ReactNode;
}) {
  return (
    <Box
      component="label"
      data-checked={checked}
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        boxSizing: 'border-box',
        height: 36,
        padding: '0 var(--space-3)',
        border: '1px solid var(--border-control)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
        fontSize: 14,
        lineHeight: '20px',
        fontWeight: 500,
        cursor: 'pointer',
        whiteSpace: 'nowrap',
        '&:hover': { borderColor: 'var(--text-secondary)' },
        '&:focus-within': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
        '&[data-checked="true"]': {
          borderColor: 'var(--control-selected)',
          boxShadow: 'inset 0 0 0 1px var(--control-selected)',
          backgroundColor: 'var(--bg-selected)',
        },
      }}
    >
      <Box
        component="input"
        type="radio"
        name={name}
        value={value}
        checked={checked}
        onChange={() => {
          onChange(value);
        }}
        sx={visuallyHidden}
      />
      <Box
        component="span"
        aria-hidden
        sx={{
          flex: 'none',
          width: 16,
          height: 16,
          boxSizing: 'border-box',
          border: checked ? '5px solid var(--control-selected)' : '2px solid var(--border-control)',
          borderRadius: 'var(--radius-full)',
          backgroundColor: 'var(--bg-surface)',
        }}
      />
      {children}
    </Box>
  );
}

function TeamChoice({ team, icon: Icon }: { team: string; icon: typeof SvgIcon }) {
  return (
    <>
      <Icon aria-hidden sx={{ fontSize: 18, color: 'var(--text-secondary)' }} />
      {team}
    </>
  );
}

/**
 * Abrir chamado (canvas AbrirChamado): the project, the team, the priority, the title and the description, without an
 * assignee (ADR-0068). From the tab of a project, the project comes filled in. Only the projects where the person may
 * open tickets are offered, and only the teams with someone to serve them; the priority starts at Média. The
 * attachments join it with the conversation.
 */
export function OpenTicketPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { projectId: fromProject } = useParams();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const ids = useId();
  const choices = useQuery(ticketChoicesQuery());
  const projects = (choices.data ?? []).filter((choice) => choice.canOpen);

  const {
    register,
    control,
    handleSubmit,
    setError,
    setValue,
    getValues,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: {
      projectId: fromProject ?? '',
      team: '',
      priority: 'Medium',
      title: '',
      description: '',
      descriptionEmpty: true,
    },
  });
  const [files, setFiles] = useState<File[]>([]);
  const projectId = useWatch({ control, name: 'projectId' });
  const project = projects.find((choice) => choice.id === projectId);
  const teams = project?.teams ?? [];

  // A project with one team chooses it; a team the project does not have is left out.
  useEffect(() => {
    const team = getValues('team');
    if (project === undefined) {
      return;
    }
    if (project.teams.length === 1 && team === '') {
      setValue('team', project.teams[0] ?? '');
    } else if (team !== '' && !project.teams.includes(team as TicketTeam)) {
      setValue('team', '');
    }
  }, [project, getValues, setValue]);

  const errorText = (message: string | undefined) =>
    isTicketFieldError(message) ? t(message) : undefined;
  const back = fromProject === undefined ? '/tickets' : `/projects/${fromProject}/tickets`;

  const open = handleSubmit(async (values) => {
    try {
      const { data, error, response } = await api.POST('/api/tickets', {
        body: {
          projectId: values.projectId,
          team: values.team as TicketTeam,
          priority: values.priority,
          title: values.title.trim(),
          description: null,
          descriptionBody: values.description,
        },
      });
      if (data !== undefined) {
        // The files of the description go right after the ticket exists (ADR-0069).
        let message = t('tickets.opened', { number: data.number });
        if (files.length > 0) {
          const form = new FormData();
          for (const file of files) {
            form.append('files', file, file.name);
          }
          const sent = await api
            .POST('/api/tickets/{number}/attachments', {
              params: { path: { number: data.number } },
              body: { files: [] },
              bodySerializer: () => form,
            })
            .catch(() => undefined);
          if (sent?.response.ok !== true) {
            message = t('tickets.errors.attachmentsFailed', {
              number: data.number,
              reason: ticketProblemText(sent?.response.status ?? 0, sent?.error, t),
            });
          }
        }
        void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        // The form is done: Back from the new ticket goes to where the form was opened from (ADR-0105).
        await navigate(`/tickets/${String(data.number)}`, { replace: true, state: { message } });
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.closed) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        setError('root', { message: t('tickets.errors.projectClosed') });
        return;
      }
      if (response.status === 403 || response.status === 404) {
        setError('root', { message: t('tickets.errors.forbidden') });
        return;
      }
      const fields = invalidFields(error);
      if (fields.includes('team')) {
        // The team lost the people who served it: the choices are asked again.
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        setError('team', { message: errors.teamRequired });
      }
      if (fields.includes('projectId')) {
        setError('projectId', { message: errors.projectRequired });
      }
      if (fields.includes('description')) {
        setError('description', { message: errors.descriptionTooLong });
      }
      if (
        fields.length === 0 ||
        fields.some((field) => !['team', 'projectId', 'description'].includes(field))
      ) {
        setError('root', { message: t('tickets.errors.failed') });
      }
    } catch {
      setError('root', { message: t('tickets.errors.failed') });
    }
  });

  const projectOptions = [
    { value: '', label: t('tickets.form.projectPlaceholder') },
    ...projects.map((choice) => ({ value: choice.id, label: `${choice.code} · ${choice.name}` })),
  ];

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page>
        <Box
          component="form"
          id="open-ticket"
          noValidate
          onSubmit={(event) => {
            void open(event);
          }}
          sx={{
            flex: 1,
            minHeight: 0,
            overflow: 'auto',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
          }}
        >
          {choices.isSuccess && projects.length === 0 && (
            <Alert tone="info">{t('tickets.form.noProjects')}</Alert>
          )}
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: desktop ? 'minmax(0, 1fr) auto auto' : 'minmax(0, 1fr)',
              alignItems: 'start',
              gap: 'var(--space-4)',
            }}
          >
            <FormField
              label={t('tickets.form.project')}
              error={errorText(fieldErrors.projectId?.message)}
              helpId={`${ids}-project-help`}
            >
              <Controller
                control={control}
                name="projectId"
                render={({ field }) => (
                  <Select
                    label={t('tickets.form.project')}
                    value={field.value}
                    options={projectOptions}
                    invalid={fieldErrors.projectId !== undefined}
                    describedBy={`${ids}-project-help`}
                    onChange={field.onChange}
                  />
                )}
              />
            </FormField>
            <FormField label={t('tickets.form.team')} error={errorText(fieldErrors.team?.message)}>
              <Controller
                control={control}
                name="team"
                render={({ field }) => (
                  <Box
                    role="radiogroup"
                    aria-label={t('tickets.form.team')}
                    sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)' }}
                  >
                    {teams.map((team) => (
                      <ChoiceCard
                        key={team}
                        name={`${ids}-team`}
                        value={team}
                        checked={field.value === team}
                        onChange={field.onChange}
                      >
                        <TeamChoice team={t(`tickets.teams.${team}`)} icon={teamIcons[team]} />
                      </ChoiceCard>
                    ))}
                  </Box>
                )}
              />
            </FormField>
            <FormField label={t('tickets.form.priority')}>
              <Controller
                control={control}
                name="priority"
                render={({ field }) => (
                  <Box
                    role="radiogroup"
                    aria-label={t('tickets.form.priority')}
                    sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)' }}
                  >
                    {ticketPriorities.map((priority: TicketPriority) => (
                      <ChoiceCard
                        key={priority}
                        name={`${ids}-priority`}
                        value={priority}
                        checked={field.value === priority}
                        onChange={field.onChange}
                      >
                        <PriorityTag priority={priority} />
                      </ChoiceCard>
                    ))}
                  </Box>
                )}
              />
            </FormField>
          </Box>
          {project !== undefined && teams.length === 0 && (
            <Alert tone="info">{t('tickets.form.noTeams')}</Alert>
          )}
          <TextField
            label={t('tickets.form.title')}
            error={errorText(fieldErrors.title?.message)}
            maxLength={200}
            {...register('title')}
          />
          <FormField
            label={t('tickets.form.description')}
            error={errorText(
              fieldErrors.descriptionEmpty?.message ?? fieldErrors.description?.message,
            )}
          >
            <Suspense fallback={<Box sx={{ height: 280 }} />}>
              <Controller
                control={control}
                name="description"
                render={({ field }) => (
                  <RichTextEditor
                    label={t('tickets.form.description')}
                    minHeight={240}
                    invalid={
                      fieldErrors.descriptionEmpty !== undefined ||
                      fieldErrors.description !== undefined
                    }
                    onChange={(json, empty) => {
                      field.onChange(json);
                      setValue('descriptionEmpty', empty, {
                        shouldValidate: fieldErrors.descriptionEmpty !== undefined,
                      });
                    }}
                  />
                )}
              />
            </Suspense>
          </FormField>
          <Box
            sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 'var(--space-2)' }}
          >
            <Box component="span" sx={{ fontSize: 13, fontWeight: 500 }}>
              {t('tickets.form.attachments')}
              <Box
                component="span"
                sx={{
                  marginLeft: 'var(--space-1)',
                  fontWeight: 400,
                  color: 'var(--text-secondary)',
                }}
              >
                {t('tickets.form.optional')}
              </Box>
            </Box>
            <ChooseFiles
              onChoose={(chosen) => {
                setFiles((current) => [...current, ...chosen]);
              }}
            />
            <ChosenFiles
              files={files}
              onRemove={(index) => {
                setFiles((current) => current.filter((_file, place) => place !== index));
              }}
            />
          </Box>
          {fieldErrors.root?.message !== undefined && (
            <Alert tone="danger">{fieldErrors.root.message}</Alert>
          )}
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
            void navigate(back, { replace: true });
          }}
        >
          {t('tickets.form.cancel')}
        </Button>
        <Button variant="contained" type="submit" form="open-ticket" disabled={isSubmitting}>
          {t('tickets.form.submit')}
        </Button>
      </Box>
    </Box>
  );
}
