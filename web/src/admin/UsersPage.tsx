import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import CheckOutlined from '@mui/icons-material/CheckOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import PersonOffOutlined from '@mui/icons-material/PersonOffOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import ListSubheader from '@mui/material/ListSubheader';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, systemRoles, type SystemRole, type UserRow, userProblems } from '../api/client';
import { queryKeys, usersQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { Select } from '../components/Select';
import { StatusBadge } from '../components/StatusBadge';
import { TextField } from '../components/TextField';
import { useDates } from '../format/useDates';
import { useDebounced } from '../hooks/useDebounced';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { useAdminContext } from './adminContext';

const pageSize = 25;
const searchDelay = 300;

type RoleFilter = 'all' | SystemRole;
type UserAction = 'role' | 'deactivate' | 'reactivate' | 'anonymize';
/** A person without access, deactivated or anonymized, shows dimmed in the list. */
const isInactive = (user: UserRow) => user.status !== 'Active';
const noUsers = { items: [], total: 0 };
/** The confirmation of each action on the access of a person. */
const doneMessages = {
  deactivate: 'deactivated',
  reactivate: 'reactivated',
  anonymize: 'anonymized',
} as const;
type StatusFilter = 'all' | UserRow['status'];

/**
 * Users of the Administration, as in the canvas (ADR-0008): people in alphabetical order, with search, filters by
 * role and status, and a menu per person to change the role and to deactivate or reactivate the access. Name and
 * e-mail come from the identity provider and are never edited here. A deactivated person can be anonymized, for
 * good, after a confirmation (ADR-0058, ADR-0110); an anonymized one has no menu.
 */
export function UsersPage() {
  const { t } = useTranslation();
  const { formatLastAccess } = useDates();
  const { usersTotal } = useAdminContext();
  const { session, refresh } = useSession();
  const [search, setSearch] = useState('');
  const [role, setRole] = useState<RoleFilter>('all');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [page, setPage] = useState(1);
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; user: UserRow }>();
  const [anonymizing, setAnonymizing] = useState<UserRow>();
  const debouncedSearch = useDebounced(search, searchDelay).trim();
  const queryClient = useQueryClient();

  // A list already seen in the session shows at once; another filter keeps the rows on screen until it arrives.
  const { data, isFetching } = useQuery({
    ...usersQuery({
      search: debouncedSearch === '' ? undefined : debouncedSearch,
      role: role === 'all' ? undefined : role,
      status: status === 'all' ? undefined : status,
      page,
      pageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const users = data ?? noUsers;

  const act = useCallback(
    async (user: UserRow, action: UserAction, newRole?: SystemRole) => {
      setMenu(undefined);
      setAnonymizing(undefined);
      const path = { params: { path: { id: user.id } } };
      try {
        const { response, error } =
          action === 'role'
            ? await api.PUT('/api/users/{id}/role', {
                ...path,
                body: { role: newRole ?? user.role },
              })
            : action === 'deactivate'
              ? await api.POST('/api/users/{id}/deactivate', path)
              : action === 'reactivate'
                ? await api.POST('/api/users/{id}/reactivate', path)
                : await api.POST('/api/users/{id}/anonymization', path);
        if (response.ok) {
          setMessage(
            action === 'role'
              ? t('users.messages.roleChanged', {
                  name: user.name,
                  role: t(`users.roles.${newRole ?? user.role}`),
                })
              : t(`users.messages.${doneMessages[action]}`, { name: user.name }),
          );
          // The lists that show the person, here and in the Audit, are asked again.
          void queryClient.invalidateQueries({ queryKey: queryKeys.users });
          void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
          // A change of one's own access shows at once: the Administration may no longer be theirs.
          if (session.status === 'signed-in' && session.user.id === user.id) {
            await refresh();
          }
          return;
        }
        const problemType = (error as { type?: string | null } | undefined)?.type;
        setMessage(
          problemType === userProblems.lastAdministrator
            ? t('users.messages.lastAdministrator')
            : problemType === userProblems.anonymized
              ? t('users.messages.anonymizedAlready')
              : problemType === userProblems.active
                ? t('users.messages.activeNotAnonymized')
                : t('users.messages.failed'),
        );
        // Someone else may have changed the person meanwhile.
        void queryClient.invalidateQueries({ queryKey: queryKeys.users });
      } catch {
        setMessage(t('users.messages.failed'));
      }
    },
    [queryClient, refresh, session, t],
  );

  const columns = useMemo<DataTableColumn<UserRow>[]>(
    () => [
      {
        field: 'name',
        headerName: t('users.columns.person'),
        flex: 2,
        minWidth: 240,
        renderCell: ({ row }) => (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0 }}>
            <Avatar name={row.name} />
            <Box sx={{ minWidth: 0 }}>
              <Box
                component="span"
                sx={{
                  display: 'block',
                  fontWeight: 500,
                  color: 'inherit',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {row.name}
              </Box>
              <Box
                component="span"
                sx={{
                  display: 'block',
                  marginTop: '2px',
                  fontSize: 12,
                  lineHeight: '16px',
                  color: 'var(--text-secondary)',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {row.status === 'Anonymized' ? t('users.anonymizedData') : row.email}
              </Box>
            </Box>
          </Box>
        ),
      },
      {
        field: 'role',
        headerName: t('users.columns.role'),
        width: 180,
        valueGetter: (_value, row) => t(`users.roles.${row.role}`),
      },
      {
        field: 'status',
        headerName: t('users.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.status === 'Active' ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('users.status.Active')}
            </StatusBadge>
          ) : row.status === 'Anonymized' ? (
            <StatusBadge tone="neutral" icon={PersonOffOutlined}>
              {t('users.status.Anonymized')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('users.status.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'lastSignInAt',
        headerName: t('users.columns.lastAccess'),
        width: 140,
        valueGetter: (_value, row) => formatLastAccess(row.lastSignInAt, t),
      },
      {
        field: 'actions',
        headerName: t('users.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        // An anonymized person stays as is, for good (ADR-0110).
        renderCell: ({ row }) =>
          row.status === 'Anonymized' ? null : (
            <IconButton
              size="small"
              aria-label={t('users.menu.button', { name: row.name })}
              aria-haspopup="menu"
              onClick={(event) => {
                setMenu({ anchor: event.currentTarget, user: row });
              }}
              sx={{
                width: 'var(--control-sm)',
                height: 'var(--control-sm)',
                borderRadius: 'var(--radius-md)',
                color: 'var(--text-secondary)',
                '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
              }}
            >
              <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
            </IconButton>
          ),
      },
    ],
    [t, formatLastAccess],
  );

  return (
    <Page count={usersTotal ?? undefined}>
      <DataTable
        label={t('users.table')}
        rows={users.items}
        columns={columns}
        rowCount={users.total}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        // Any wait, a new filter or a refresh of the rows on screen, shows the bar if it lasts (DataTable).
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('users.empty')}
        tools={
          <>
            <TextField
              label={t('users.search')}
              hideLabel
              placeholder={t('users.search')}
              type="search"
              size="sm"
              icon={SearchOutlined}
              width={280}
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
            <Select<RoleFilter>
              label={t('users.filters.role')}
              size="sm"
              width={200}
              value={role}
              options={[
                { value: 'all', label: t('users.filters.allRoles') },
                ...systemRoles.map((value) => ({ value, label: t(`users.roles.${value}`) })),
              ]}
              onChange={(value) => {
                setRole(value);
                setPage(1);
              }}
            />
            <Select<StatusFilter>
              label={t('users.filters.status')}
              size="sm"
              width={180}
              value={status}
              options={[
                { value: 'all', label: t('users.filters.allStatuses') },
                { value: 'Active', label: t('users.status.Active') },
                { value: 'Inactive', label: t('users.status.Inactive') },
                { value: 'Anonymized', label: t('users.status.Anonymized') },
              ]}
              onChange={(value) => {
                setStatus(value);
                setPage(1);
              }}
            />
          </>
        }
      />
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 260, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined ? undefined : t('users.menu.button', { name: menu.user.name }),
          },
        }}
      >
        <ListSubheader>{t('users.menu.role')}</ListSubheader>
        {systemRoles.map((value) => {
          const current = menu?.user.role === value;
          return (
            <MenuItem
              key={value}
              selected={current}
              aria-checked={current}
              role="menuitemradio"
              onClick={() => {
                if (menu !== undefined && !current) {
                  void act(menu.user, 'role', value);
                } else {
                  setMenu(undefined);
                }
              }}
            >
              <Box component="span" sx={{ flex: 1 }}>
                {t(`users.roles.${value}`)}
              </Box>
              {current && (
                <CheckOutlined aria-hidden sx={{ color: 'var(--action-text) !important' }} />
              )}
            </MenuItem>
          );
        })}
        <Divider sx={{ margin: 'var(--space-1) 0' }} />
        {menu?.user.status === 'Inactive' ? (
          [
            <MenuItem
              key="reactivate"
              onClick={() => {
                void act(menu.user, 'reactivate');
              }}
            >
              <CheckCircleOutlined aria-hidden />
              {t('users.menu.reactivate')}
            </MenuItem>,
            <MenuItem
              key="anonymize"
              onClick={() => {
                setAnonymizing(menu.user);
                setMenu(undefined);
              }}
              sx={{
                color: 'var(--status-danger-text)',
                '& > .MuiSvgIcon-root': { color: 'var(--status-danger-text)' },
              }}
            >
              <PersonOffOutlined aria-hidden />
              {t('users.menu.anonymize')}
            </MenuItem>,
          ]
        ) : (
          <MenuItem
            onClick={() => {
              if (menu !== undefined) {
                void act(menu.user, 'deactivate');
              }
            }}
            sx={{
              color: 'var(--status-danger-text)',
              '& > .MuiSvgIcon-root': { color: 'var(--status-danger-text)' },
            }}
          >
            <BlockOutlined aria-hidden />
            {t('users.menu.deactivate')}
          </MenuItem>
        )}
      </Menu>
      <Dialog
        open={anonymizing !== undefined}
        alert
        title={t('users.anonymize.title', { name: anonymizing?.name ?? '' })}
        onClose={() => {
          setAnonymizing(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setAnonymizing(undefined);
              }}
            >
              {t('users.anonymize.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => {
                if (anonymizing !== undefined) {
                  void act(anonymizing, 'anonymize');
                }
              }}
            >
              {t('users.anonymize.confirm')}
            </Button>
          </>
        }
      >
        <Box component="p" sx={{ margin: '0 0 var(--space-3)' }}>
          {t('users.anonymize.text', {
            name: anonymizing?.name ?? '',
            email: anonymizing?.email ?? '',
          })}
        </Box>
        <Box component="p" sx={{ margin: 0 }}>
          {t('users.anonymize.forGood')}
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
    </Page>
  );
}
