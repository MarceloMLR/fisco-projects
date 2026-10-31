import AddOutlined from '@mui/icons-material/AddOutlined';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import Box from '@mui/material/Box';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type DefaultFolder } from '../api/client';
import { defaultFoldersQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { StatusBadge } from '../components/StatusBadge';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { DefaultFolderDialog } from './DefaultFolderDialog';
import { Button, IconButton } from '../components/Button';
import { useNotify } from '../notices/notify';

/** The default folders fit in one page: an installation keeps a few of them. */
const pageSize = 100;
const noFolders: DefaultFolder[] = [];
const isInactive = (folder: DefaultFolder) => !folder.isActive;

const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

/**
 * "Pastas padrão" of the Administration (ADR-0031, ADR-0046), drawn as "Fornecedores", since the canvas has no screen
 * for it: the folders every new project gets, in the order of the name, with the one of the acceptance terms marked
 * (ADR-0093), the situation and "Nova pasta". Each one has a menu to rename, deactivate or reactivate; the folder of
 * the terms is always active. The changes are for the new projects.
 */
export function DefaultFoldersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data, isFetching } = useQuery(defaultFoldersQuery());
  const folders = data ?? noFolders;
  const notify = useNotify();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; folder: DefaultFolder }>();
  // A folder to rename, or null for a new one.
  const [editing, setEditing] = useState<DefaultFolder | null>();

  const changed = useCallback(
    (text: string) => {
      notify(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.defaultFolders });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [notify, queryClient],
  );

  const setActive = useCallback(
    async (folder: DefaultFolder, active: boolean) => {
      setMenu(undefined);
      try {
        const { response } = await api.PUT('/api/admin/document-folders/{id}', {
          params: { path: { id: folder.id } },
          body: { name: folder.name, isActive: active },
        });
        if (response.ok) {
          changed(
            t(
              active
                ? 'defaultFolders.messages.reactivated'
                : 'defaultFolders.messages.deactivated',
              {
                name: folder.name,
              },
            ),
          );
        } else {
          notify(t('defaultFolders.messages.failed'), { tone: 'error' });
        }
      } catch {
        notify(t('defaultFolders.messages.failed'), { tone: 'error' });
      }
    },
    [notify, changed, t],
  );

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, folder: DefaultFolder) => {
    setMenu({ anchor: event.currentTarget, folder });
  }, []);

  const columns = useMemo<DataTableColumn<DefaultFolder>[]>(
    () => [
      {
        field: 'name',
        headerName: t('defaultFolders.columns.name'),
        flex: 1,
        minWidth: 220,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              fontWeight: 500,
            }}
          >
            {row.name}
          </Box>
        ),
      },
      {
        field: 'use',
        headerName: t('defaultFolders.columns.use'),
        width: 180,
        renderCell: ({ row }) =>
          row.isAcceptance ? (
            <Box
              component="span"
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 'var(--space-1)',
                color: 'var(--text)',
              }}
            >
              <TaskAltOutlined
                aria-hidden
                sx={{ fontSize: 'var(--icon-sm)', color: 'var(--text-secondary)' }}
              />
              {t('defaultFolders.acceptance')}
            </Box>
          ) : (
            <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
              {t('defaultFolders.regular')}
            </Box>
          ),
      },
      {
        field: 'status',
        headerName: t('defaultFolders.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.isActive ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('defaultFolders.statuses.Active')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('defaultFolders.statuses.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'actions',
        headerName: t('defaultFolders.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) => (
          <IconButton
            size="small"
            aria-label={t('defaultFolders.menu.button', { name: row.name })}
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

  const footer = useCallback(
    ({ count }: { count: number }) => t('defaultFolders.total', { count }),
    [t],
  );

  const newFolder = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing(null);
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('defaultFolders.newFolder')}
    </Button>
  );

  return (
    <Page count={data === undefined ? undefined : folders.length} actions={newFolder(false)}>
      <Box sx={{ flex: 'none' }}>
        <Alert tone="info">{t('defaultFolders.note')}</Alert>
      </Box>
      <DataTable
        label={t('defaultFolders.table')}
        rows={folders}
        columns={columns}
        rowCount={folders.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('defaultFolders.empty')}
        displayedRowsLabel={footer}
      />
      {!desktop && newFolder(true)}
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
              menu === undefined
                ? undefined
                : t('defaultFolders.menu.button', { name: menu.folder.name }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.folder);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('defaultFolders.menu.edit')}
        </MenuItem>
        {menu?.folder.isActive === false && (
          <MenuItem
            onClick={() => {
              void setActive(menu.folder, true);
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('defaultFolders.menu.reactivate')}
          </MenuItem>
        )}
        {menu?.folder.isActive === true && !menu.folder.isAcceptance && (
          <MenuItem
            onClick={() => {
              void setActive(menu.folder, false);
            }}
          >
            <BlockOutlined aria-hidden />
            {t('defaultFolders.menu.deactivate')}
          </MenuItem>
        )}
      </Menu>
      {editing !== undefined && (
        <DefaultFolderDialog
          folder={editing ?? undefined}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(saved) => {
            setEditing(undefined);
            changed(saved);
          }}
        />
      )}
    </Page>
  );
}

function noPaging() {
  // Every default folder is in the one page.
}
