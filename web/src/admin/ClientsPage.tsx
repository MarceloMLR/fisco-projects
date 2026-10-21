import AddOutlined from '@mui/icons-material/AddOutlined';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, clientProblems, type ClientRow, type ClientStatus } from '../api/client';
import { ApiError, clientsQuery, queryKeys } from '../api/queries';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { LogoTile } from '../components/LogoTile';
import { SegmentedControl } from '../components/SegmentedControl';
import { StatusBadge } from '../components/StatusBadge';
import { TextField } from '../components/TextField';
import { formatCnpj } from '../format/cnpj';
import { useDebounced } from '../hooks/useDebounced';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { useAdminContext } from './adminContext';
import { ClientDialog } from './ClientDialog';

const pageSize = 25;
const searchDelay = 300;

type StatusFilter = 'all' | ClientStatus;
/** An inactive client shows dimmed in the list. */
const isInactive = (client: ClientRow) => client.status === 'Inactive';
const noClients = { items: [], total: 0 };

const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

const dangerItemSx = {
  color: 'var(--status-danger-text)',
  '& > .MuiSvgIcon-root': { color: 'var(--status-danger-text)' },
} as const;

/**
 * Clients of the Administration, as in the canvas (AdminClientes): the clients in alphabetical order, with the logo
 * or the initials, the CNPJ, the status and the observation; the search by name or CNPJ, the status filter and, at
 * the bottom, the clients and the active ones. Each client has a menu to edit, deactivate or reactivate and remove.
 */
export function ClientsPage() {
  const { t } = useTranslation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { clientsTotal } = useAdminContext();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [page, setPage] = useState(1);
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; client: ClientRow }>();
  const [editing, setEditing] = useState<{ client?: ClientRow }>();
  const [removing, setRemoving] = useState<ClientRow>();
  const debouncedSearch = useDebounced(search, searchDelay).trim();
  const queryClient = useQueryClient();
  const searched = debouncedSearch === '' ? undefined : debouncedSearch;

  // A list already seen in the session shows at once; another filter keeps the rows on screen until it arrives.
  const { data, isFetching } = useQuery({
    ...clientsQuery({
      search: searched,
      status: status === 'all' ? undefined : status,
      page,
      pageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const clients = data ?? noClients;
  // The active ones of the search, for the footer; with a status chosen, the list itself tells it.
  const activeOfSearch = useQuery({
    ...clientsQuery({ search: searched, status: 'Active', pageSize: 1 }),
    enabled: status === 'all',
    placeholderData: keepPreviousData,
  }).data?.total;
  const activeCount =
    status === 'Active' ? clients.total : status === 'Inactive' ? 0 : (activeOfSearch ?? 0);

  const changed = useCallback(
    (text: string) => {
      setMessage(text);
      // The lists that show the client, here and in the Audit, are asked again.
      void queryClient.invalidateQueries({ queryKey: queryKeys.clients });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient],
  );

  const changeStatus = useCallback(
    async (client: ClientRow, action: 'deactivate' | 'reactivate') => {
      setMenu(undefined);
      const path = { params: { path: { id: client.id } } };
      try {
        const { response } =
          action === 'deactivate'
            ? await api.POST('/api/clients/{id}/deactivate', path)
            : await api.POST('/api/clients/{id}/reactivate', path);
        if (response.ok) {
          changed(
            t(`clients.messages.${action === 'deactivate' ? 'deactivated' : 'reactivated'}`, {
              name: client.name,
            }),
          );
          return;
        }
        setMessage(t('clients.messages.failed'));
      } catch {
        setMessage(t('clients.messages.failed'));
      }
    },
    [changed, t],
  );

  const remove = useMutation({
    mutationFn: async (client: ClientRow) => {
      const { response, error } = await api.DELETE('/api/clients/{id}', {
        params: { path: { id: client.id } },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onSuccess: (_result, client) => {
      changed(t('clients.messages.removed', { name: client.name }));
    },
    onError: (error, client) => {
      // A client with projects is only made inactive (ADR-0015).
      const problem = error instanceof ApiError ? error.problem : undefined;
      setMessage(
        typeof problem === 'object' &&
          problem !== null &&
          'type' in problem &&
          problem.type === clientProblems.hasProjects
          ? t('clients.messages.hasProjects', { name: client.name })
          : t('clients.messages.failed'),
      );
    },
    onSettled: () => {
      setRemoving(undefined);
    },
  });

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, client: ClientRow) => {
    setMenu({ anchor: event.currentTarget, client });
  }, []);

  const columns = useMemo<DataTableColumn<ClientRow>[]>(
    () => [
      {
        field: 'name',
        headerName: t('clients.columns.client'),
        flex: 1.6,
        minWidth: 240,
        renderCell: ({ row }) => (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0 }}>
            <LogoTile name={row.name} src={row.logoUrl} />
            <Box
              component="span"
              sx={{
                fontWeight: 500,
                color: 'inherit',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {row.name}
            </Box>
          </Box>
        ),
      },
      {
        field: 'cnpj',
        headerName: t('clients.columns.cnpj'),
        width: 190,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              fontFamily: 'var(--font-mono)',
              fontSize: 13,
              color: 'inherit',
              whiteSpace: 'nowrap',
            }}
          >
            {row.cnpj === null ? t('audit.details.none') : formatCnpj(row.cnpj)}
          </Box>
        ),
      },
      {
        field: 'status',
        headerName: t('clients.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.status === 'Active' ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('clients.statuses.Active')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('clients.statuses.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'observation',
        headerName: t('clients.columns.observation'),
        flex: 1.2,
        minWidth: 180,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              display: '-webkit-box',
              WebkitBoxOrient: 'vertical',
              WebkitLineClamp: 2,
              overflow: 'hidden',
              whiteSpace: 'normal',
              color: 'var(--text-secondary)',
            }}
          >
            {row.observation ?? t('audit.details.none')}
          </Box>
        ),
      },
      {
        field: 'actions',
        headerName: t('clients.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) => (
          <IconButton
            size="small"
            aria-label={t('clients.menu.button', { name: row.name })}
            aria-haspopup="menu"
            onClick={(event) => {
              openMenu(event, row);
            }}
            sx={iconButtonSx}
          >
            <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
        ),
      },
    ],
    [t, openMenu],
  );

  // "10 clientes · 9 ativos"; with more than a page, the clients shown come first: "1–25 de 30 clientes · 28 ativos".
  const footer = useCallback(
    ({ from, to, count }: { from: number; to: number; count: number }) => {
      const total = t('clients.total', { count });
      const shown = count > pageSize ? t('clients.range', { from, to, total }) : total;
      return `${shown} · ${t('clients.active', { count: activeCount })}`;
    },
    [t, activeCount],
  );

  const newClient = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing({});
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('clients.newClient')}
    </Button>
  );

  return (
    <Page count={clientsTotal ?? undefined} actions={newClient(false)}>
      <DataTable
        label={t('clients.table')}
        rows={clients.items}
        columns={columns}
        rowCount={clients.total}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        // Any wait, a new filter or a refresh of the rows on screen, shows the bar if it lasts (DataTable).
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('clients.empty')}
        displayedRowsLabel={footer}
        tools={
          <>
            {/* On the phone the search takes the whole row, and the status goes below it. */}
            <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
              <TextField
                label={t('clients.search')}
                hideLabel
                placeholder={t('clients.search')}
                type="search"
                size="sm"
                icon={SearchOutlined}
                width={desktop ? 280 : undefined}
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
              />
            </Box>
            <SegmentedControl<StatusFilter>
              label={t('clients.status')}
              value={status}
              options={[
                { value: 'all', label: t('clients.filters.all') },
                { value: 'Active', label: t('clients.filters.Active') },
                { value: 'Inactive', label: t('clients.filters.Inactive') },
              ]}
              onChange={(value) => {
                setStatus(value);
                setPage(1);
              }}
            />
          </>
        }
      />
      {!desktop && newClient(true)}
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 200, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined ? undefined : t('clients.menu.button', { name: menu.client.name }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing({ client: menu?.client });
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('clients.menu.edit')}
        </MenuItem>
        {menu?.client.status === 'Inactive' ? (
          <MenuItem
            onClick={() => {
              void changeStatus(menu.client, 'reactivate');
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('clients.menu.reactivate')}
          </MenuItem>
        ) : (
          <MenuItem
            onClick={() => {
              if (menu !== undefined) {
                void changeStatus(menu.client, 'deactivate');
              }
            }}
          >
            <BlockOutlined aria-hidden />
            {t('clients.menu.deactivate')}
          </MenuItem>
        )}
        <Divider sx={{ margin: 'var(--space-1) 0' }} />
        <MenuItem
          onClick={() => {
            setRemoving(menu?.client);
            setMenu(undefined);
          }}
          sx={dangerItemSx}
        >
          <DeleteOutlined aria-hidden />
          {t('clients.menu.remove')}
        </MenuItem>
      </Menu>
      {editing !== undefined && (
        <ClientDialog
          client={editing.client}
          canSendLogo
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(saved) => {
            setEditing(undefined);
            setMessage(saved);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('clients.remove.title')}
        onClose={() => {
          setRemoving(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(undefined);
              }}
            >
              {t('clients.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={remove.isPending}
              onClick={() => {
                if (removing !== undefined) {
                  remove.mutate(removing);
                }
              }}
            >
              {t('clients.remove.confirm')}
            </Button>
          </>
        }
      >
        {removing !== undefined && t('clients.remove.text', { name: removing.name })}
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
