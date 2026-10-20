import CheckOutlined from '@mui/icons-material/CheckOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import ListSubheader from '@mui/material/ListSubheader';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Outlet, useMatch, useParams } from 'react-router';
import { api, isClosedStatus, type Project, type ProjectStatus } from '../api/client';
import { ApiError, projectQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { Page } from '../shell/Page';
import { ProjectDialog } from './ProjectDialog';
import { ProjectHeader } from './ProjectHeader';
import type { ProjectOutlet } from './projectOutlet';
import { projectStatusLook } from './projectStatus';

const openStatuses = [
  'Planning',
  'InProgress',
  'Paused',
] as const satisfies readonly ProjectStatus[];
const ReopenIcon = projectStatusLook.InProgress.icon;
const CompleteIcon = projectStatusLook.Completed.icon;
const CancelIcon = projectStatusLook.Cancelled.icon;

/**
 * The page of a project: the header (ProjectHeader) and the tab of the address, Visão geral, Cronograma, Chamados or Membros,
 * each at its own address. "Ações do projeto", in every tab, offers to edit the data and to change the status, as the person may
 * (ADR-0043); a closed project says it is read-only (ADR-0016). The tabs get the project and the message at the bottom
 * (useProjectOutlet).
 */
export function ProjectLayout() {
  const { t } = useTranslation();
  const { projectId = '' } = useParams();
  const onMembers = useMatch('/projects/:projectId/members') !== null;
  const onSchedule = useMatch('/projects/:projectId/schedule') !== null;
  const onTickets = useMatch('/projects/:projectId/tickets') !== null;
  const tab = onMembers ? 'members' : onSchedule ? 'schedule' : onTickets ? 'tickets' : 'overview';
  const queryClient = useQueryClient();
  const { data: project, error, isPending } = useQuery(projectQuery(projectId));
  const [menu, setMenu] = useState<HTMLElement>();
  const [editing, setEditing] = useState(false);
  const [confirming, setConfirming] = useState<'Completed' | 'Cancelled'>();
  const [message, setMessage] = useState<string>();
  const outlet = useMemo<ProjectOutlet | undefined>(
    () => (project === undefined ? undefined : { project, notify: setMessage }),
    [project],
  );

  const changeStatus = useMutation({
    mutationFn: async ({ status }: { status: ProjectStatus; project: Project }) => {
      const { response } = await api.PUT('/api/projects/{id}/status', {
        params: { path: { id: projectId } },
        body: { status },
      });
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
    },
    onSuccess: (_result, { status, project: changed }) => {
      setMessage(
        t('projects.messages.status', {
          name: changed.name,
          status: t(`projects.statuses.${status}`),
        }),
      );
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    onError: () => {
      setMessage(t('projects.messages.failed'));
    },
    onSettled: () => {
      setConfirming(undefined);
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
  const choose = (status: ProjectStatus) => {
    setMenu(undefined);
    if (status === 'Completed' || status === 'Cancelled') {
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
      <Page withTitle={false}>
        <Box
          role="tabpanel"
          id="project-panel"
          aria-labelledby={`project-tab-${tab}`}
          sx={{
            flex: 1,
            minHeight: 0,
            overflow: 'auto',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
          }}
        >
          {closed && (
            <Box sx={{ flex: 'none' }}>
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
              disabled={changeStatus.isPending}
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
        {t(
          confirming === 'Cancelled'
            ? 'projects.confirm.cancelText'
            : 'projects.confirm.completeText',
          {
            name: project.name,
          },
        )}
      </Dialog>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Box>
  );
}
