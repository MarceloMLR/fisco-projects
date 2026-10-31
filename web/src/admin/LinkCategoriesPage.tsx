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
import { api, type LinkCategory } from '../api/client';
import { linkCategoriesQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { StatusBadge } from '../components/StatusBadge';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { LinkCategoryDialog } from './LinkCategoryDialog';
import { Button, IconButton } from '../components/Button';

/** The categories fit in one page: an installation keeps a few of them. */
const pageSize = 100;
const noCategories: LinkCategory[] = [];
const isInactive = (category: LinkCategory) => !category.isActive;

const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

/**
 * "Categorias de links" of the Administration (ADR-0032, ADR-0046), drawn as "Pastas padrão", since the canvas has no
 * screen for it: the categories of the links of the projects, in the order of the name, with the situation and "Nova
 * categoria". Each one has a menu to rename, deactivate or reactivate; an inactive one is not offered for new links.
 */
export function LinkCategoriesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data, isFetching } = useQuery(linkCategoriesQuery());
  const categories = data ?? noCategories;
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; category: LinkCategory }>();
  // A category to rename, or null for a new one.
  const [editing, setEditing] = useState<LinkCategory | null>();

  const changed = useCallback(
    (text: string) => {
      setMessage(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.linkCategories });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient],
  );

  const setActive = useCallback(
    async (category: LinkCategory, active: boolean) => {
      setMenu(undefined);
      try {
        const { response } = await api.PUT('/api/admin/link-categories/{id}', {
          params: { path: { id: category.id } },
          body: { name: category.name, isActive: active },
        });
        changed(
          response.ok
            ? t(
                active
                  ? 'linkCategories.messages.reactivated'
                  : 'linkCategories.messages.deactivated',
                {
                  name: category.name,
                },
              )
            : t('linkCategories.messages.failed'),
        );
      } catch {
        setMessage(t('linkCategories.messages.failed'));
      }
    },
    [changed, t],
  );

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, category: LinkCategory) => {
    setMenu({ anchor: event.currentTarget, category });
  }, []);

  const columns = useMemo<DataTableColumn<LinkCategory>[]>(
    () => [
      {
        field: 'name',
        headerName: t('linkCategories.columns.name'),
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
        field: 'status',
        headerName: t('linkCategories.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.isActive ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('linkCategories.statuses.Active')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('linkCategories.statuses.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'actions',
        headerName: t('linkCategories.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) => (
          <IconButton
            size="small"
            aria-label={t('linkCategories.menu.button', { name: row.name })}
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
    ({ count }: { count: number }) => t('linkCategories.total', { count }),
    [t],
  );

  const newCategory = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing(null);
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('linkCategories.newCategory')}
    </Button>
  );

  return (
    <Page count={data === undefined ? undefined : categories.length} actions={newCategory(false)}>
      <Box sx={{ flex: 'none' }}>
        <Alert tone="info">{t('linkCategories.note')}</Alert>
      </Box>
      <DataTable
        label={t('linkCategories.table')}
        rows={categories}
        columns={columns}
        rowCount={categories.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('linkCategories.empty')}
        displayedRowsLabel={footer}
      />
      {!desktop && newCategory(true)}
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
                : t('linkCategories.menu.button', { name: menu.category.name }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.category);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('linkCategories.menu.edit')}
        </MenuItem>
        {menu?.category.isActive === false && (
          <MenuItem
            onClick={() => {
              void setActive(menu.category, true);
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('linkCategories.menu.reactivate')}
          </MenuItem>
        )}
        {menu?.category.isActive === true && (
          <MenuItem
            onClick={() => {
              void setActive(menu.category, false);
            }}
          >
            <BlockOutlined aria-hidden />
            {t('linkCategories.menu.deactivate')}
          </MenuItem>
        )}
      </Menu>
      {editing !== undefined && (
        <LinkCategoryDialog
          category={editing ?? undefined}
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
  // Every category is in the one page.
}
