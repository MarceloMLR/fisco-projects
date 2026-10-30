import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { api, type Project } from '../api/client';
import { ApiError, projectDeletionQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';

/** The kinds of items the summary counts, in the order the dialog lists them. */
const kinds = [
  'tickets',
  'documents',
  'links',
  'credentials',
  'acceptanceTerms',
  'scheduleItems',
  'members',
  'timeEntries',
] as const;

/** Whether the code typed is the code of the project, as the API checks it: without spaces around, in any case. */
const typedCodeMatches = (typed: string, code: string) =>
  typed.trim().toUpperCase() === code.toUpperCase();

/**
 * "Excluir projeto", only for the Administrator (ADR-0041, ADR-0111): what the project has, counted when the dialog
 * opens, the warning that it cannot be undone and the code of the project typed to confirm. Once deleted, the screen
 * goes to Projetos with the confirmation, and what was kept of the project is dropped.
 */
export function DeleteProjectDialog({
  project,
  onClose,
  onFailed,
}: {
  project: Project;
  onClose: () => void;
  onFailed: (message: string) => void;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [typed, setTyped] = useState('');
  const { data: summary, isError } = useQuery(projectDeletionQuery(project.id));
  const matches = typedCodeMatches(typed, project.code);

  const remove = useMutation({
    mutationFn: async () => {
      const { response, error } = await api.POST('/api/projects/{id}/deletion', {
        params: { path: { id: project.id } },
        body: { code: typed.trim() },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onSuccess: async () => {
      // The project and everything of it is gone: its queries leave the cache, and the lists that showed it are asked again.
      queryClient.removeQueries({ queryKey: [...queryKeys.projects, project.id] });
      for (const key of [
        queryKeys.projects,
        queryKeys.tickets,
        queryKeys.documents,
        queryKeys.links,
        queryKeys.credentials,
        queryKeys.acceptances,
        queryKeys.myWork,
        queryKeys.notifications,
        queryKeys.audit,
      ]) {
        void queryClient.invalidateQueries({ queryKey: key });
      }
      await navigate('/projects', {
        replace: true,
        state: { message: t('projects.messages.deleted', { code: project.code }) },
      });
    },
    onError: () => {
      onClose();
      onFailed(t('projects.messages.deleteFailed'));
    },
  });

  return (
    <Dialog
      open
      alert
      title={t('projects.deletion.title', { code: project.code })}
      onClose={onClose}
      onSubmit={(event) => {
        event.preventDefault();
        if (matches && !remove.isPending) {
          remove.mutate();
        }
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.deletion.cancel')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            color="error"
            disabled={!matches || summary === undefined || remove.isPending}
          >
            {t('projects.deletion.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
        <span>{t('projects.deletion.text', { name: project.name })}</span>
        {summary === undefined ? (
          isError ? (
            <Alert tone="danger">{t('projects.deletion.loadFailed')}</Alert>
          ) : (
            <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
              {t('projects.deletion.loading')}
            </Box>
          )
        ) : (
          <Box
            component="ul"
            aria-label={t('projects.deletion.text', { name: project.name })}
            sx={{ margin: 0, paddingLeft: 'var(--space-5)' }}
          >
            {kinds
              .filter((kind) => summary[kind] > 0)
              .map((kind) => (
                <li key={kind}>
                  {t(`projects.deletion.counts.${kind}`, { count: summary[kind] })}
                </li>
              ))}
          </Box>
        )}
        <Alert tone="danger">{t('projects.deletion.forGood')}</Alert>
        <TextField
          label={t('projects.deletion.code', { code: project.code })}
          value={typed}
          autoComplete="off"
          spellCheck={false}
          onChange={(event) => {
            setTyped(event.target.value);
          }}
        />
      </Box>
    </Dialog>
  );
}
