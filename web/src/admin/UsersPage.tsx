import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import CheckOutlined from '@mui/icons-material/CheckOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
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
import { Select } from '../components/Select';
import { StatusBadge } from '../components/StatusBadge';
import { TextField } from '../components/TextField';
import { formatLastAccess } from '../format/dates';
import { useDebounced } from '../hooks/useDebounced';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { useAdminContext } from './adminContext';

const pageSize = 25;
const searchDelay = 300;

type RoleFilter = 'all' | SystemRole;
/** An inactive person shows dimmed in the list. */
const isInactive = (user: UserRow) => user.status === 'Inactive';
const noUsers = { items: [], total: 0 };
type StatusFilter = 'all' | UserRow['status'];

/**
 * Users of the Administration, as in the canvas (ADR-0008): people in alphabetical order, with search, filters by
 * role and status, and a menu per person to change the role and to deactivate or reactivate the access. Name and
 * e-mail come from the identity provider and are never edited here.
 */
export function UsersPage() {
  const { t } = useTranslation();
  const { usersTotal } = useAdminContext();
  const { session, refresh } = useSession();
  const [search, setSearch] = useState('');
  const [role, setRole] = useState<RoleFilter>('all');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [page, setPage] = useState(1);
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; user: UserRow }>();
  const debouncedSearch = useDebounced(search, searchDelay).trim();
  const queryClient = useQueryClient();

  // A list already seen in the session shows at once; another filter keeps the rows on screen until it arrives.
  const { data, isFetching, isPlaceholderData } = useQuery({
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
  const loading = isFetching && (data === undefined || isPlaceholderData);

  const act = useCallback(
    async (user: UserRow, action: 'role' | 'deactivate' | 'reactivate', newRole?: SystemRole) => {
      setMenu(undefined);
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
              : await api.POST('/api/users/{id}/reactivate', path);
        if (response.ok) {
          setMessage(
            action === 'role'
              ? t('users.messages.roleChanged', {
                  name: user.name,
                  role: t(`users.roles.${newRole ?? user.role}`),
                })
              : t(`users.messages.${action === 'deactivate' ? 'deactivated' : 'reactivated'}`, {
                  name: user.name,
                }),
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
            : t('users.messages.failed'),
        );
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
                {row.email}
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
        renderCell: ({ row }) => (
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
    [t],
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
        loading={loading}
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
          <MenuItem
            onClick={() => {
              void act(menu.user, 'reactivate');
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('users.menu.reactivate')}
          </MenuItem>
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
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Page>
  );
}
