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
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import {
  api,
  isClosedStatus,
  type Project,
  type ProjectPerson,
  type ProjectStatus,
} from '../api/client';
import { ApiError, projectQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Avatar } from '../components/Avatar';
import { Dialog } from '../components/Dialog';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ProjectDialog } from './ProjectDialog';
import { ProjectHeader } from './ProjectHeader';
import { projectStatusLook } from './projectStatus';

const openStatuses = [
  'Planning',
  'InProgress',
  'Paused',
] as const satisfies readonly ProjectStatus[];
const ReopenIcon = projectStatusLook.InProgress.icon;
const CompleteIcon = projectStatusLook.Completed.icon;
const CancelIcon = projectStatusLook.Cancelled.icon;

const cardSx = {
  boxSizing: 'border-box',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  overflow: 'hidden',
} as const;

const cardHeadSx = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: 'var(--space-3)',
  minHeight: 48,
  padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
  borderBottom: '1px solid var(--border)',
} as const;

const cardTitleSx = {
  margin: 0,
  fontSize: 15,
  lineHeight: '22px',
  fontWeight: 500,
  color: 'var(--text)',
} as const;

/** The teams of a project (ADR-0040, ADR-0068): Gestão, Funcional and Desenvolvimento. */
function teamsOf(members: readonly ProjectPerson[]) {
  const withRole = (...roles: ProjectPerson['role'][]) =>
    members.filter((member) => roles.includes(member.role));
  return [
    { key: 'management', people: withRole('Manager', 'TechLead') },
    { key: 'functional', people: withRole('Contributor') },
    { key: 'development', people: withRole('TechLead', 'Developer') },
  ] as const;
}

/**
 * The page of a project: the header (ProjectHeader) and the tab Visão geral, with the description and the team, until
 * the overview of Síntese-2. "Ações do projeto" offers to edit the data and to change the status, as the person may
 * (ADR-0043); a closed project says it is read-only (ADR-0016).
 */
export function ProjectPage() {
  const { t } = useTranslation();
  const { projectId = '' } = useParams();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const queryClient = useQueryClient();
  const { data: project, error, isPending } = useQuery(projectQuery(projectId));
  const [menu, setMenu] = useState<HTMLElement>();
  const [editing, setEditing] = useState(false);
  const [confirming, setConfirming] = useState<'Completed' | 'Cancelled'>();
  const [message, setMessage] = useState<string>();

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

  if (project === undefined) {
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
          id="project-panel-overview"
          aria-labelledby="project-tab-overview"
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
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: desktop ? 'minmax(0, 2fr) minmax(320px, 1fr)' : 'minmax(0, 1fr)',
              gap: 'var(--space-4)',
              alignItems: 'start',
            }}
          >
            <Box component="section" aria-labelledby="project-description" sx={cardSx}>
              <Box sx={cardHeadSx}>
                <Box component="h2" id="project-description" sx={cardTitleSx}>
                  {t('projects.overview.description')}
                </Box>
              </Box>
              <Box
                sx={{
                  padding: 'var(--space-4)',
                  whiteSpace: 'pre-wrap',
                  overflowWrap: 'anywhere',
                  color: project.description === null ? 'var(--text-secondary)' : 'var(--text)',
                }}
              >
                {project.description ?? t('projects.overview.noDescription')}
              </Box>
            </Box>
            <Box component="section" aria-labelledby="project-team" sx={cardSx}>
              <Box sx={cardHeadSx}>
                <Box component="h2" id="project-team" sx={cardTitleSx}>
                  {t('projects.overview.team')}
                </Box>
              </Box>
              <Box component="ul" sx={{ margin: 0, padding: 0, listStyle: 'none' }}>
                {teamsOf(project.members).map((team) => (
                  <Box
                    component="li"
                    key={team.key}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 'var(--space-3)',
                      padding: '10px var(--space-4)',
                      borderBottom: '1px solid var(--border)',
                      '&:last-of-type': { borderBottom: 0 },
                    }}
                  >
                    <Box sx={{ flex: 1, minWidth: 0 }}>
                      <Box sx={{ fontWeight: 500, color: 'var(--text)' }}>
                        {t(`projects.overview.teams.${team.key}`)}
                      </Box>
                      <Box
                        sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
                      >
                        {team.key === 'management' && team.people.length > 0
                          ? team.people.map((person) => person.name).join(', ')
                          : t('projects.overview.people', { count: team.people.length })}
                      </Box>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      {team.people.map((person, index) => (
                        <Box
                          key={person.id}
                          component="span"
                          sx={{
                            display: 'inline-flex',
                            marginLeft: index === 0 ? 0 : '-6px',
                            borderRadius: 'var(--radius-full)',
                            boxShadow: '0 0 0 2px var(--bg-surface)',
                          }}
                        >
                          <Avatar name={person.name} size="sm" />
                        </Box>
                      ))}
                    </Box>
                  </Box>
                ))}
              </Box>
            </Box>
          </Box>
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
