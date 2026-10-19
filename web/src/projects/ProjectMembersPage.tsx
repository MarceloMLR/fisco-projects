import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import PersonAddOutlined from '@mui/icons-material/PersonAddOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { useCallback, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import {
  api,
  type ProjectMember,
  type ProjectRole,
  projectProblems,
  projectRoles,
} from '../api/client';
import { ApiError, projectMembersQuery, queryKeys } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDates } from '../format/useDates';
import { useSession } from '../session/SessionContext';
import { theme } from '../theme/theme';
import { IncludePersonDialog } from './IncludePersonDialog';
import {
  isGroupRow,
  mayManage,
  type MemberRow,
  memberRows,
  searchMembers,
  teamsOfRole,
} from './members';
import { useProjectOutlet } from './projectOutlet';

/** Rows of a page of the list; the free Data Grid shows up to 100 (ADR-0050). */
const pageSize = 100;

type Grouping = 'team' | 'name';

const secondarySx = {
  display: 'block',
  marginTop: '2px',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  whiteSpace: 'nowrap',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
} as const;

/** The teams of a person in words, as "Gestão e Desenvolvimento" for the tech lead. */
function teamsText(role: ProjectRole, t: TFunction): string {
  const [first, second] = teamsOfRole(role).map((team) => t(`projects.members.teams.${team}`));
  return second === undefined
    ? (first ?? '')
    : t('projects.members.teamsOf', { first: first ?? '', second });
}

/**
 * The tab Membros of a project (canvas ProjetoMembros): the people by team (Gestão, Funcional, Desenvolvimento and
 * Leitores, the tech lead in Gestão and in Desenvolvimento) or by name, with the search, and "Incluir pessoa" for who
 * manages the people (ADR-0042, ADR-0043). Choosing a person opens the details in a drawer (ADR-0081), where the role
 * changes and the person leaves the project.
 */
export function ProjectMembersPage() {
  const { t } = useTranslation();
  const { project, notify } = useProjectOutlet();
  const { formatLastAccess } = useDates();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data: members = [], isFetching } = useQuery(projectMembersQuery(project.id));
  const [search, setSearch] = useState('');
  const [grouping, setGrouping] = useState<Grouping>('team');
  const [page, setPage] = useState(1);
  const [including, setIncluding] = useState(false);
  const [chosenId, setChosenId] = useState<string>();
  const chosen = members.find((member) => member.id === chosenId);
  const canInclude = project.permissions.canManagePeople;

  const found = useMemo(() => searchMembers(members, search), [members, search]);
  const rows = useMemo(() => memberRows(found, grouping === 'team'), [found, grouping]);
  const pageRows = useMemo(() => rows.slice((page - 1) * pageSize, page * pageSize), [rows, page]);
  const memberIds = useMemo(() => members.map((member) => member.id), [members]);

  const columns = useMemo<DataTableColumn<MemberRow>[]>(
    () => [
      {
        field: 'person',
        headerName: t('projects.members.columns.person'),
        flex: 1.6,
        minWidth: 220,
        // A group spans the whole row (fp-group-row).
        colSpan: (_value, row) => (row.kind === 'group' ? 3 : undefined),
        renderCell: ({ row }) =>
          row.kind === 'group' ? (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
              {t(`projects.members.teams.${row.team}`)}
              <Box
                component="span"
                sx={{
                  color: 'var(--text-secondary)',
                  fontWeight: 400,
                  fontVariantNumeric: 'tabular-nums',
                }}
              >
                {row.count}
              </Box>
            </Box>
          ) : (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0 }}>
              <Avatar name={row.member.name} />
              <Box sx={{ minWidth: 0 }}>
                <Box
                  component="span"
                  sx={{
                    display: 'block',
                    fontWeight: 500,
                    whiteSpace: 'nowrap',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                  }}
                >
                  {row.member.name}
                </Box>
                <Box component="span" sx={secondarySx}>
                  {row.member.email}
                </Box>
              </Box>
            </Box>
          ),
      },
      {
        field: 'role',
        headerName: t('projects.members.columns.role'),
        flex: 1,
        minWidth: 150,
        valueGetter: (_value, row) =>
          row.kind === 'person' ? t(`projects.roles.${row.member.role}`) : '',
      },
      {
        field: 'lastAccess',
        headerName: t('projects.members.columns.lastAccess'),
        width: 140,
        valueGetter: (_value, row) =>
          row.kind === 'person' ? formatLastAccess(row.member.lastSignInAt, t) : '',
        renderCell: ({ value }) => (
          <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
            {value as string}
          </Box>
        ),
      },
    ],
    [t, formatLastAccess],
  );
  const footer = useCallback(
    () => t('projects.members.total', { count: found.length }),
    [t, found.length],
  );
  const choose = useCallback((row: MemberRow) => {
    if (row.kind === 'person') {
      setChosenId(row.member.id);
    }
  }, []);
  const getRowId = useCallback((row: MemberRow) => row.id, []);

  const include = (fullWidth: boolean) =>
    canInclude && (
      <Button
        variant="contained"
        size="small"
        startIcon={<PersonAddOutlined />}
        onClick={() => {
          setIncluding(true);
        }}
        sx={fullWidth ? { width: '100%' } : { flex: 'none' }}
      >
        {t('projects.members.include')}
      </Button>
    );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
      }}
    >
      <DataTable<MemberRow>
        label={t('projects.members.table')}
        rows={pageRows}
        columns={columns}
        rowCount={rows.length}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        loading={isFetching}
        emptyLabel={t('projects.members.empty')}
        displayedRowsLabel={footer}
        onRowChoose={choose}
        isGroupRow={isGroupRow}
        getRowId={getRowId}
        selectedRowId={
          chosen === undefined
            ? undefined
            : grouping === 'team'
              ? `${teamsOfRole(chosen.role)[0] ?? 'viewers'}:${chosen.id}`
              : chosen.id
        }
        tools={
          <>
            <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
              <TextField
                label={t('projects.members.search')}
                hideLabel
                placeholder={t('projects.members.search')}
                type="search"
                size="sm"
                icon={SearchOutlined}
                width={desktop ? 260 : undefined}
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
              />
            </Box>
            <SegmentedControl<Grouping>
              label={t('projects.members.grouping')}
              value={grouping}
              options={[
                { value: 'team', label: t('projects.members.groupings.team') },
                { value: 'name', label: t('projects.members.groupings.name') },
              ]}
              onChange={(value) => {
                setGrouping(value);
                setPage(1);
              }}
            />
            {desktop && <Box sx={{ flex: 1 }} />}
            {desktop && include(false)}
          </>
        }
      />
      {!desktop && include(true)}
      {including && (
        <IncludePersonDialog
          project={project}
          memberIds={memberIds}
          onClose={() => {
            setIncluding(false);
          }}
          onIncluded={(message) => {
            setIncluding(false);
            notify(message);
          }}
        />
      )}
      <DetailsDrawer
        open={chosen !== undefined}
        onClose={() => {
          setChosenId(undefined);
        }}
        label={t('projects.members.details.label')}
      >
        {chosen !== undefined && (
          <MemberDetails
            member={chosen}
            onDone={(message, left) => {
              if (left) {
                setChosenId(undefined);
              }
              notify(message);
            }}
          />
        )}
      </DetailsDrawer>
    </Box>
  );
}

/**
 * The details of a person of the project (canvas ProjetoMembros, side panel, in the drawer of ADR-0081): the name and
 * the e-mail, the role, which changes on choosing another one, the team, since when the person is in the project and
 * the last access, and "Remover do projeto", with confirmation. Who removes themselves goes back to Projetos, without
 * the project.
 */
function MemberDetails({
  member,
  onDone,
}: {
  member: ProjectMember;
  /** A change was saved or refused; left when the person is no longer in the project. */
  onDone: (message: string, left: boolean) => void;
}) {
  const { t } = useTranslation();
  const { project } = useProjectOutlet();
  const { session } = useSession();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { formatLastAccess, formatDate } = useDates();
  const roleHelpId = useId();
  const [removing, setRemoving] = useState(false);
  const [roleError, setRoleError] = useState<string>();
  const { permissions } = project;
  const me = session.status === 'signed-in' ? session.user.id : undefined;
  const canChange = mayManage(permissions, member.role, null);
  const roleOptions = projectRoles
    .filter((role) => role === member.role || mayManage(permissions, member.role, role))
    .map((role) => ({
      value: role,
      label: t(`projects.roles.${role}`),
      disabled: role === 'Manager' && member.role !== 'Manager' && !member.canBeManager,
    }));

  const refusal = (error: unknown) =>
    error instanceof ApiError &&
    typeof error.problem === 'object' &&
    error.problem !== null &&
    'type' in error.problem &&
    error.problem.type === projectProblems.lastManager
      ? t('projects.members.errors.lastManager')
      : t('projects.members.messages.failed');

  const changeRole = useMutation({
    mutationFn: async (role: ProjectRole) => {
      const { error, response } = await api.PUT('/api/projects/{id}/members/{userId}/role', {
        params: { path: { id: project.id, userId: member.id } },
        body: { role },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onSuccess: (_result, role) => {
      setRoleError(undefined);
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
      onDone(
        t('projects.members.messages.roleChanged', {
          name: member.name,
          role: t(`projects.roles.${role}`),
        }),
        false,
      );
    },
    onError: (error) => {
      setRoleError(refusal(error));
    },
  });

  const remove = useMutation({
    mutationFn: async () => {
      const { error, response } = await api.DELETE('/api/projects/{id}/members/{userId}', {
        params: { path: { id: project.id, userId: member.id } },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onSuccess: async () => {
      setRemoving(false);
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
      if (member.id === me) {
        // Without the project: the list of projects, and the sidebar without it.
        queryClient.removeQueries({ queryKey: [...queryKeys.projects, project.id] });
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        await navigate('/projects');
        return;
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      onDone(t('projects.members.messages.removed', { name: member.name }), true);
    },
    onError: (error) => {
      setRemoving(false);
      onDone(refusal(error), false);
    },
  });

  return (
    <>
      <Box
        component="section"
        sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', minWidth: 0 }}>
          <Avatar name={member.name} size="lg" />
          <Box sx={{ minWidth: 0 }}>
            <Box
              component="h2"
              sx={{
                margin: 0,
                fontSize: 18,
                lineHeight: '26px',
                fontWeight: 500,
                color: 'var(--text)',
                overflowWrap: 'anywhere',
              }}
            >
              {member.name}
            </Box>
            <Box
              component="span"
              sx={{
                fontSize: 13,
                lineHeight: '18px',
                color: 'var(--text-secondary)',
                overflowWrap: 'anywhere',
              }}
            >
              {member.email}
            </Box>
          </Box>
        </Box>
        <FormField label={t('projects.members.details.role')} error={roleError} helpId={roleHelpId}>
          <Select<ProjectRole>
            label={t('projects.members.details.role')}
            value={member.role}
            options={roleOptions}
            disabled={!canChange || changeRole.isPending}
            invalid={roleError !== undefined}
            describedBy={roleError === undefined ? undefined : roleHelpId}
            onChange={(role) => {
              if (role !== member.role) {
                changeRole.mutate(role);
              }
            }}
          />
        </FormField>
        <Box
          component="dl"
          sx={{
            display: 'grid',
            gridTemplateColumns: '128px minmax(0, 1fr)',
            gap: 'var(--space-3)',
            margin: 0,
            fontSize: 14,
            lineHeight: '20px',
            '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
            '& dd': { margin: 0, minWidth: 0 },
          }}
        >
          <dt>{t('projects.members.details.team')}</dt>
          <dd>{teamsText(member.role, t)}</dd>
          <dt>{t('projects.members.details.since')}</dt>
          <dd>{formatDate(member.addedAt)}</dd>
          <dt>{t('projects.members.details.lastAccess')}</dt>
          <dd>{formatLastAccess(member.lastSignInAt, t)}</dd>
        </Box>
      </Box>
      {mayManage(permissions, member.role, null) && (
        <Box>
          <Button
            variant="text"
            size="small"
            startIcon={<DeleteOutlined />}
            onClick={() => {
              setRemoving(true);
            }}
            sx={{ color: 'var(--status-danger-text)' }}
          >
            {t('projects.members.details.remove')}
          </Button>
        </Box>
      )}
      <Dialog
        open={removing}
        alert
        title={t('projects.members.remove.title')}
        onClose={() => {
          setRemoving(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(false);
              }}
            >
              {t('projects.members.remove.back')}
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={remove.isPending}
              onClick={() => {
                remove.mutate();
              }}
            >
              {t('projects.members.remove.confirm')}
            </Button>
          </>
        }
      >
        {member.id === me
          ? t('projects.members.remove.self', { project: project.name })
          : t('projects.members.remove.text', { name: member.name, project: project.name })}
      </Dialog>
    </>
  );
}
