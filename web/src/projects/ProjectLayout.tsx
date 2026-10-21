import CheckOutlined from '@mui/icons-material/CheckOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import ListSubheader from '@mui/material/ListSubheader';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Outlet, useMatch, useParams } from 'react-router';
import {
  api,
  isClosedStatus,
  type Project,
  projectProblems,
  type ProjectStatus,
} from '../api/client';
import { prefetchProject } from '../api/prefetch';
import { ApiError, projectClosingQuery, projectQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { Page } from '../shell/Page';
import { ProjectDialog } from './ProjectDialog';
import { ProjectHeader } from './ProjectHeader';
import type { ProjectOutlet } from './projectOutlet';
import { projectTabs } from './projectTabs';
import { projectStatusLook } from './projectStatus';
import { preloadTimeline } from './timelinePreload';
import { theme } from '../theme/theme';

const openStatuses = [
  'Planning',
  'InProgress',
  'Paused',
] as const satisfies readonly ProjectStatus[];
const ReopenIcon = projectStatusLook.InProgress.icon;
const CompleteIcon = projectStatusLook.Completed.icon;
const CancelIcon = projectStatusLook.Cancelled.icon;

/** A field of a problem the API returned, such as its type or the open tickets. */
function problemField(problem: unknown, name: string): unknown {
  return typeof problem === 'object' && problem !== null && name in problem
    ? (problem as Record<string, unknown>)[name]
    : undefined;
}

/**
 * The page of a project: the header (ProjectHeader) and the tab of the address, Visão geral, Cronograma, Chamados, Documentos or Membros,
 * each at its own address. "Ações do projeto", in every tab, offers to edit the data and to change the status, as the person may
 * (ADR-0043); a closed project says it is read-only (ADR-0016). The tabs get the project and the message at the bottom
 * (useProjectOutlet).
 */
export function ProjectLayout() {
  const { t } = useTranslation();
  const { projectId = '' } = useParams();
  const segment = useMatch('/projects/:projectId/:tab')?.params.tab;
  const tab = projectTabs.find((item) => item === segment) ?? 'overview';
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  // The documents are split in panes with margins of their own, as in the canvas (fp-split).
  const flush = desktop && tab === 'documents';
  const queryClient = useQueryClient();
  const { data: project, error, isPending } = useQuery(projectQuery(projectId));
  // The tab and the counts of the header are asked together with the project, not after it (ADR-0106).
  useEffect(() => {
    prefetchProject(
      queryClient,
      projectId,
      tab === 'overview' || tab === 'schedule' || tab === 'members' ? tab : 'other',
    );
  }, [queryClient, projectId, tab]);
  // The code of the Linha do tempo, while the person looks at the project (on a computer, where it shows).
  useEffect(() => {
    if (desktop) {
      preloadTimeline();
    }
  }, [desktop]);
  const [menu, setMenu] = useState<HTMLElement>();
  const [editing, setEditing] = useState(false);
  const [confirming, setConfirming] = useState<'Completed' | 'Cancelled'>();
  // The tickets not closed the API found when it refused to complete the project (ADR-0043).
  const [refusedTickets, setRefusedTickets] = useState<number>();
  const [message, setMessage] = useState<string>();
  // What closing the project would leave behind, read when the confirmation opens (ADR-0043).
  const { data: closing } = useQuery({
    ...projectClosingQuery(projectId),
    enabled: confirming !== undefined,
  });
  const outlet = useMemo<ProjectOutlet | undefined>(
    () => (project === undefined ? undefined : { project, notify: setMessage }),
    [project],
  );

  const changeStatus = useMutation({
    mutationFn: async ({ status }: { status: ProjectStatus; project: Project }) => {
      const { response, error } = await api.PUT('/api/projects/{id}/status', {
        params: { path: { id: projectId } },
        body: { status },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onSuccess: (_result, { status, project: changed }) => {
      setConfirming(undefined);
      setMessage(
        t('projects.messages.status', {
          name: changed.name,
          status: t(`projects.statuses.${status}`),
        }),
      );
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
      void queryClient.invalidateQueries({ queryKey: queryKeys.documents });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    onError: (error) => {
      const problem = error instanceof ApiError ? error.problem : undefined;
      const open = problemField(problem, 'openTickets');
      if (
        problemField(problem, 'type') === projectProblems.hasOpenTickets &&
        typeof open === 'number'
      ) {
        // A ticket opened while the confirmation was open: the dialog stays, saying it.
        setRefusedTickets(open);
        return;
      }
      setConfirming(undefined);
      setMessage(
        problemField(problem, 'type') === projectProblems.ticketsChanged
          ? t('projects.messages.ticketsChanged')
          : t('projects.messages.failed'),
      );
    },
  });

  if (project === undefined || outlet === undefined) {
    return (
      <Page>
        {!isPending && (
          <Alert tone={error instanceof ApiError && error.status === 404 ? 'info' : 'danger'}>
            {error instanceof ApiError && error.status === 404
              ? t('projects.notFound')
              : t('projects.loadFailed')}
          </Alert>
        )}
      </Page>
    );
  }

  const { permissions } = project;
  const closed = isClosedStatus(project.status);
  const hasActions = permissions.canEdit || permissions.canChangeStatus;
  // The tickets not closed keep the project from being completed; the cancellation closes them (ADR-0043).
  const openTickets = refusedTickets ?? closing?.openTickets ?? 0;
  const blocked = confirming === 'Completed' && openTickets > 0;
  const choose = (status: ProjectStatus) => {
    setMenu(undefined);
    if (status === 'Completed' || status === 'Cancelled') {
      setRefusedTickets(undefined);
      setConfirming(status);
    } else if (status !== project.status) {
      changeStatus.mutate({ status, project });
    }
  };

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <ProjectHeader
        project={project}
        tab={tab}
        onOpenActions={
          hasActions
            ? (event) => {
                setMenu(event.currentTarget);
              }
            : undefined
        }
      />
      <Page withTitle={false} flush={flush}>
        <Box
          role="tabpanel"
          id="project-panel"
          aria-labelledby={`project-tab-${tab}`}
          sx={{
            flex: 1,
            minHeight: 0,
            overflow: flush ? 'hidden' : 'auto',
            display: 'flex',
            flexDirection: 'column',
            gap: flush ? 0 : 'var(--space-4)',
          }}
        >
          {closed && (
            <Box sx={{ flex: 'none', padding: flush ? 'var(--space-4) var(--space-6) 0' : 0 }}>
              <Alert tone="info">
                {t(`projects.closed.${project.status as 'Completed' | 'Cancelled'}`)}
              </Alert>
            </Box>
          )}
          <Outlet context={outlet} />
        </Box>
      </Page>
      <Menu
        anchorEl={menu}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 240, marginTop: 'var(--space-1)' } },
          list: { 'aria-label': t('projects.actions.button') },
        }}
      >
        {permissions.canEdit && (
          <MenuItem
            onClick={() => {
              setMenu(undefined);
              setEditing(true);
            }}
          >
            <EditOutlined aria-hidden />
            {t('projects.actions.edit')}
          </MenuItem>
        )}
        {permissions.canEdit && permissions.canChangeStatus && (
          <Divider sx={{ margin: 'var(--space-1) 0' }} />
        )}
        {permissions.canChangeStatus && closed && (
          <MenuItem
            onClick={() => {
              choose('InProgress');
            }}
          >
            <ReopenIcon aria-hidden />
            {t('projects.actions.reopen')}
          </MenuItem>
        )}
        {permissions.canChangeStatus && !closed && (
          <ListSubheader>{t('projects.actions.status')}</ListSubheader>
        )}
        {permissions.canChangeStatus &&
          !closed &&
          openStatuses.map((status) => {
            const current = project.status === status;
            return (
              <MenuItem
                key={status}
                role="menuitemradio"
                aria-checked={current}
                selected={current}
                onClick={() => {
                  choose(status);
                }}
              >
                <Box component="span" sx={{ flex: 1 }}>
                  {t(`projects.statuses.${status}`)}
                </Box>
                {current && (
                  <CheckOutlined aria-hidden sx={{ color: 'var(--action-text) !important' }} />
                )}
              </MenuItem>
            );
          })}
        {permissions.canChangeStatus && !closed && <Divider sx={{ margin: 'var(--space-1) 0' }} />}
        {permissions.canChangeStatus && !closed && (
          <MenuItem
            onClick={() => {
              choose('Completed');
            }}
          >
            <CompleteIcon aria-hidden />
            {t('projects.actions.complete')}
          </MenuItem>
        )}
        {permissions.canChangeStatus && !closed && (
          <MenuItem
            onClick={() => {
              choose('Cancelled');
            }}
            sx={{
              color: 'var(--status-danger-text)',
              '& > .MuiSvgIcon-root': { color: 'var(--status-danger-text)' },
            }}
          >
            <CancelIcon aria-hidden />
            {t('projects.actions.cancel')}
          </MenuItem>
        )}
      </Menu>
      {editing && (
        <ProjectDialog
          project={project}
          onClose={() => {
            setEditing(false);
          }}
          onSaved={(saved) => {
            setEditing(false);
            setMessage(saved);
          }}
        />
      )}
      <Dialog
        open={confirming !== undefined}
        alert
        title={t(
          confirming === 'Cancelled'
            ? 'projects.confirm.cancelTitle'
            : 'projects.confirm.completeTitle',
        )}
        onClose={() => {
          setConfirming(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setConfirming(undefined);
              }}
            >
              {t('projects.confirm.back')}
            </Button>
            <Button
              variant="contained"
              color={confirming === 'Cancelled' ? 'error' : 'primary'}
              disabled={changeStatus.isPending || blocked}
              onClick={() => {
                if (confirming !== undefined) {
                  changeStatus.mutate({ status: confirming, project });
                }
              }}
            >
              {t(
                confirming === 'Cancelled'
                  ? 'projects.confirm.cancelAction'
                  : 'projects.confirm.completeAction',
              )}
            </Button>
          </>
        }
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          <span>
            {t(
              confirming === 'Cancelled'
                ? 'projects.confirm.cancelText'
                : 'projects.confirm.completeText',
              {
                name: project.name,
              },
            )}
          </span>
          {blocked && (
            <Alert tone="danger">
              {t('projects.confirm.openTickets', { count: openTickets })}{' '}
              <Box
                component={Link}
                to={`/projects/${projectId}/tickets`}
                onClick={() => {
                  setConfirming(undefined);
                }}
                sx={{ color: 'inherit', fontWeight: 500 }}
              >
                {t('projects.confirm.seeTickets')}
              </Box>
            </Alert>
          )}
          {confirming === 'Completed' && closing !== undefined && closing.incompleteTasks > 0 && (
            <Alert tone="warning">
              {t('projects.confirm.incompleteTasks', { count: closing.incompleteTasks })}
            </Alert>
          )}
          {confirming === 'Completed' &&
            closing !== undefined &&
            closing.pendingAcceptances > 0 && (
              <Alert tone="warning">
                {t('projects.confirm.pendingAcceptances', { count: closing.pendingAcceptances })}{' '}
                <Box
                  component={Link}
                  to={`/projects/${projectId}/acceptances`}
                  onClick={() => {
                    setConfirming(undefined);
                  }}
                  sx={{ color: 'inherit', fontWeight: 500 }}
                >
                  {t('projects.confirm.seeAcceptances')}
                </Box>
              </Alert>
            )}
          {confirming === 'Cancelled' && openTickets > 0 && (
            <Alert tone="warning">
              {t('projects.confirm.ticketsClosed', { count: openTickets })}
            </Alert>
          )}
        </Box>
      </Dialog>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Box>
  );
}
