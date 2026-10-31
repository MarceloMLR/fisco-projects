import AddOutlined from '@mui/icons-material/AddOutlined';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import Box from '@mui/material/Box';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Vendor } from '../api/client';
import { allVendorsQuery, queryKeys } from '../api/queries';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { StatusBadge } from '../components/StatusBadge';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { VendorDialog } from './VendorDialog';
import { Button, IconButton } from '../components/Button';

/** The vendors fit in one page: an installation keeps a few of them. */
const pageSize = 100;
const noVendors: Vendor[] = [];
const isInactive = (vendor: Vendor) => !vendor.isActive;

const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

/**
 * "Fornecedores" of the Administration (ADR-0028, ADR-0046), drawn as the models of project, since the canvas has no
 * screen for it yet: the vendors by name, with the support portal and the situation, and "Novo fornecedor". Each one
 * has a menu to edit, deactivate or reactivate; a vendor is never removed, since the tickets keep it, and an inactive
 * one is not offered for new registrations.
 */
export function VendorsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data, isFetching } = useQuery(allVendorsQuery());
  const vendors = data ?? noVendors;
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; vendor: Vendor }>();
  // A vendor to edit, or null for a new one.
  const [editing, setEditing] = useState<Vendor | null>();

  const changed = useCallback(
    (text: string) => {
      setMessage(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.vendors });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient],
  );

  const setActive = useCallback(
    async (vendor: Vendor, active: boolean) => {
      setMenu(undefined);
      try {
        const { response } = await api.PUT('/api/admin/vendors/{id}', {
          params: { path: { id: vendor.id } },
          body: { name: vendor.name, portalUrl: vendor.portalUrl, isActive: active },
        });
        changed(
          response.ok
            ? t(active ? 'vendors.messages.reactivated' : 'vendors.messages.deactivated', {
                name: vendor.name,
              })
            : t('vendors.messages.failed'),
        );
      } catch {
        setMessage(t('vendors.messages.failed'));
      }
    },
    [changed, t],
  );

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, vendor: Vendor) => {
    setMenu({ anchor: event.currentTarget, vendor });
  }, []);

  const columns = useMemo<DataTableColumn<Vendor>[]>(
    () => [
      {
        field: 'name',
        headerName: t('vendors.columns.name'),
        flex: 1,
        minWidth: 200,
        renderCell: ({ row }) => (
          <Box component="span" sx={{ ...ellipsisSx, fontWeight: 500 }}>
            {row.name}
          </Box>
        ),
      },
      {
        field: 'portalUrl',
        headerName: t('vendors.columns.portal'),
        flex: 1.2,
        minWidth: 220,
        renderCell: ({ row }) =>
          row.portalUrl === null ? (
            <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
              {t('vendors.noPortal')}
            </Box>
          ) : (
            <Box
              component="a"
              href={row.portalUrl}
              target="_blank"
              rel="noopener noreferrer"
              sx={{
                ...ellipsisSx,
                display: 'block',
                color: 'var(--action-text)',
                textDecoration: 'none',
                '&:hover': { textDecoration: 'underline' },
              }}
            >
              {row.portalUrl}
            </Box>
          ),
      },
      {
        field: 'status',
        headerName: t('vendors.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.isActive ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('vendors.statuses.Active')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('vendors.statuses.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'actions',
        headerName: t('vendors.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) => (
          <IconButton
            size="small"
            aria-label={t('vendors.menu.button', { name: row.name })}
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

  const footer = useCallback(({ count }: { count: number }) => t('vendors.total', { count }), [t]);

  const newVendor = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing(null);
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('vendors.newVendor')}
    </Button>
  );

  return (
    <Page count={data === undefined ? undefined : vendors.length} actions={newVendor(false)}>
      <DataTable
        label={t('vendors.table')}
        rows={vendors}
        columns={columns}
        rowCount={vendors.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('vendors.empty')}
        displayedRowsLabel={footer}
      />
      {!desktop && newVendor(true)}
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
              menu === undefined ? undefined : t('vendors.menu.button', { name: menu.vendor.name }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.vendor);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('vendors.menu.edit')}
        </MenuItem>
        {menu?.vendor.isActive === false ? (
          <MenuItem
            onClick={() => {
              void setActive(menu.vendor, true);
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('vendors.menu.reactivate')}
          </MenuItem>
        ) : (
          <MenuItem
            onClick={() => {
              if (menu !== undefined) {
                void setActive(menu.vendor, false);
              }
            }}
          >
            <BlockOutlined aria-hidden />
            {t('vendors.menu.deactivate')}
          </MenuItem>
        )}
      </Menu>
      {editing !== undefined && (
        <VendorDialog
          vendor={editing ?? undefined}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(saved) => {
            setEditing(undefined);
            changed(saved);
          }}
        />
      )}
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

function noPaging() {
  // Every vendor is in the one page.
}
