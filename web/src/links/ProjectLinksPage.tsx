import AddOutlined from '@mui/icons-material/AddOutlined';
import ContentCopyOutlined from '@mui/icons-material/ContentCopyOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, documentProblems, type ProjectLink } from '../api/client';
import { linksQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDates } from '../format/useDates';
import { useDebounced } from '../hooks/useDebounced';
import { problemType } from '../projects/projectForm';
import { useProjectOutlet } from '../projects/projectOutlet';
import { theme } from '../theme/theme';
import { LinkDialog } from './LinkDialog';
import { initialOf, isGroupRow, type LinkRow, linkRows, shownAddress } from './links';

/** A project has a few links: they all fit in one page of the free Data Grid (ADR-0050). */
const pageSize = 100;
const searchDelay = 300;
const all = 'all';

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const subSx = {
  ...ellipsisSx,
  display: 'block',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;
const addressSx = {
  ...ellipsisSx,
  display: 'block',
  fontSize: 13,
  color: 'var(--action-text)',
  textDecoration: 'none',
  '&:hover': { textDecoration: 'underline' },
} as const;
const tileSx = {
  display: 'inline-grid',
  placeItems: 'center',
  flex: 'none',
  boxSizing: 'border-box',
  width: 28,
  height: 28,
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-md)',
  backgroundColor: 'var(--bg-subtle)',
  color: 'var(--text-secondary)',
  fontSize: 12,
  fontWeight: 500,
} as const;
const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

const getRowId = (row: LinkRow) => row.id;

/**
 * The tab Links of a project (canvas ProjetoLinks, ADR-0032): the links grouped by category, with the square of the
 * initial and the title, the address without the protocol, which opens in a new tab, what it is for and who included
 * it; the search by title and address and the categories the links use; "Novo link" and, on each line, "Copiar
 * endereço", "Abrir em nova aba" and "⋮" with "Editar" and "Excluir", for everyone but the viewer (ADR-0045). On the
 * phone, the line shows the title with the address below, and the categories are a field.
 */
export function ProjectLinksPage() {
  const { t } = useTranslation();
  const { formatDate } = useDates();
  const { project, notify } = useProjectOutlet();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState<string>(all);
  const [menu, setMenu] = useState<{ anchor: HTMLElement; link: ProjectLink }>();
  // A link to change, or null for a new one.
  const [editing, setEditing] = useState<ProjectLink | null>();
  const [removing, setRemoving] = useState<ProjectLink>();
  const searched = useDebounced(search, searchDelay).trim();
  const { data, isFetching, error } = useQuery({
    ...linksQuery(project.id, {
      search: searched === '' ? undefined : searched,
      categoryId: category === all ? undefined : category,
    }),
    placeholderData: keepPreviousData,
  });
  const canEdit = data?.canEdit === true;
  const rows = useMemo(() => linkRows(data?.items ?? []), [data?.items]);

  const changed = useCallback(
    (message: string) => {
      notify(message);
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.links, project.id] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [notify, project.id, queryClient],
  );

  const copy = useCallback(
    async (link: ProjectLink) => {
      try {
        await navigator.clipboard.writeText(link.url);
        notify(t('links.copied'));
      } catch {
        notify(t('links.copyFailed'));
      }
    },
    [notify, t],
  );

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, link: ProjectLink) => {
    setMenu({ anchor: event.currentTarget, link });
  }, []);

  const remove = async (link: ProjectLink) => {
    setRemoving(undefined);
    try {
      const { response, error: problem } = await api.DELETE(
        '/api/projects/{projectId}/links/{linkId}',
        { params: { path: { projectId: project.id, linkId: link.id } } },
      );
      changed(
        response.ok
          ? t('links.messages.removed', { title: link.title })
          : response.status === 409 && problemType(problem) === documentProblems.projectClosed
            ? t('links.messages.closed')
            : t('links.messages.failed'),
      );
    } catch {
      notify(t('links.messages.failed'));
    }
  };

  const columns = useMemo<DataTableColumn<LinkRow>[]>(() => {
    const title: DataTableColumn<LinkRow> = {
      field: 'title',
      headerName: t('links.columns.link'),
      flex: 1.3,
      minWidth: desktop ? 240 : 180,
      // A category spans the whole row (fp-group-row).
      colSpan: (_value, row) => (row.kind === 'group' ? (desktop ? 5 : 2) : undefined),
      renderCell: ({ row }) =>
        row.kind === 'group' ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
            {row.name}
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
            <Box component="span" aria-hidden sx={tileSx}>
              {initialOf(row.link.title)}
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Box
                component="a"
                href={row.link.url}
                target="_blank"
                rel="noopener noreferrer"
                sx={{
                  ...ellipsisSx,
                  display: 'block',
                  fontWeight: 500,
                  color: 'var(--text)',
                  textDecoration: 'none',
                  '&:hover': { textDecoration: 'underline' },
                }}
              >
                {row.link.title}
              </Box>
              {!desktop && (
                <Box component="span" sx={subSx}>
                  {shownAddress(row.link.url)}
                </Box>
              )}
            </Box>
          </Box>
        ),
    };
    const actions: DataTableColumn<LinkRow> = {
      field: 'actions',
      headerName: t('links.columns.actions'),
      renderHeader: () => null,
      width: canEdit ? 120 : 84,
      align: 'right',
      renderCell: ({ row }) =>
        row.kind === 'link' && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '2px' }}>
            <IconButton
              size="small"
              aria-label={t('links.copy', { title: row.link.title })}
              onClick={() => {
                void copy(row.link);
              }}
              sx={iconButtonSx}
            >
              <ContentCopyOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
            </IconButton>
            <IconButton
              size="small"
              component="a"
              href={row.link.url}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={t('links.open', { title: row.link.title })}
              sx={iconButtonSx}
            >
              <OpenInNewOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
            </IconButton>
            {canEdit && (
              <IconButton
                size="small"
                aria-label={t('links.menu.button', { title: row.link.title })}
                aria-haspopup="menu"
                onClick={(event) => {
                  openMenu(event, row.link);
                }}
                sx={iconButtonSx}
              >
                <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
              </IconButton>
            )}
          </Box>
        ),
    };
    if (!desktop) {
      return [title, actions];
    }
    return [
      title,
      {
        field: 'url',
        headerName: t('links.columns.url'),
        flex: 1.4,
        minWidth: 220,
        renderCell: ({ row }) =>
          row.kind === 'link' && (
            <Box
              component="a"
              href={row.link.url}
              target="_blank"
              rel="noopener noreferrer"
              title={row.link.url}
              sx={addressSx}
            >
              {shownAddress(row.link.url)}
            </Box>
          ),
      },
      {
        field: 'description',
        headerName: t('links.columns.description'),
        flex: 1.4,
        minWidth: 200,
        renderCell: ({ row }) =>
          row.kind === 'link' && (
            <Box
              component="span"
              sx={{
                color: 'var(--text-secondary)',
                whiteSpace: 'normal',
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
                overflow: 'hidden',
                lineHeight: '18px',
              }}
            >
              {row.link.description}
            </Box>
          ),
      },
      {
        field: 'created',
        headerName: t('links.columns.created'),
        width: 160,
        renderCell: ({ row }) =>
          row.kind === 'link' && (
            <Box sx={{ minWidth: 0 }}>
              <Box component="span" sx={{ ...ellipsisSx, display: 'block' }}>
                {row.link.created.name}
              </Box>
              <Box component="span" sx={subSx}>
                {formatDate(row.link.created.at)}
              </Box>
            </Box>
          ),
      },
      actions,
    ];
  }, [canEdit, copy, desktop, formatDate, openMenu, t]);

  const footer = useCallback(
    () => t('links.total', { count: data?.items.length ?? 0 }),
    [data?.items.length, t],
  );

  if (error !== null) {
    return <Alert tone="danger">{t('links.loadFailed')}</Alert>;
  }

  const categories = data?.categories ?? [];
  // A category that left the project, with its last link removed, goes back to all of them.
  const chosen = categories.some((item) => item.id === category) ? category : all;
  const options = [
    { value: all, label: t('links.all'), count: data?.total },
    ...categories.map((item) => ({ value: item.id, label: item.name, count: item.count })),
  ];

  const newLink = (block: boolean) =>
    canEdit && (
      <Button
        variant="contained"
        size="small"
        startIcon={<AddOutlined />}
        onClick={() => {
          setEditing(null);
        }}
        sx={block ? { width: '100%', flex: 'none' } : { flex: 'none' }}
      >
        {t('links.newLink')}
      </Button>
    );

  const emptyLabel =
    searched !== ''
      ? t('links.emptySearch')
      : canEdit
        ? t('links.empty')
        : t('links.emptyReadOnly');

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
      <DataTable<LinkRow>
        label={t('links.table')}
        rows={rows}
        columns={columns}
        rowCount={rows.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        rowHeight={desktop ? 52 : 60}
        emptyLabel={emptyLabel}
        displayedRowsLabel={footer}
        isGroupRow={isGroupRow}
        getRowId={getRowId}
        tools={
          <>
            <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
              <TextField
                label={t('links.search')}
                hideLabel
                placeholder={t('links.search')}
                type="search"
                size="sm"
                icon={SearchOutlined}
                width={desktop ? 280 : undefined}
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                }}
              />
            </Box>
            {desktop ? (
              <SegmentedControl<string>
                label={t('links.category')}
                value={chosen}
                options={options}
                onChange={setCategory}
              />
            ) : (
              <Box sx={{ flex: '1 1 100%', minWidth: 0 }}>
                <Select<string>
                  label={t('links.category')}
                  size="sm"
                  value={chosen}
                  options={options.map(({ value, label, count }) => ({
                    value,
                    label: count === undefined ? label : `${label} (${String(count)})`,
                  }))}
                  onChange={setCategory}
                />
              </Box>
            )}
            {desktop && <Box sx={{ flex: 1 }} />}
            {desktop && newLink(false)}
          </>
        }
      />
      {!desktop && newLink(true)}
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 180, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined ? undefined : t('links.menu.button', { title: menu.link.title }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.link);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('links.menu.edit')}
        </MenuItem>
        <MenuItem
          onClick={() => {
            setRemoving(menu?.link);
            setMenu(undefined);
          }}
        >
          <DeleteOutlined aria-hidden />
          {t('links.menu.remove')}
        </MenuItem>
      </Menu>
      {editing !== undefined && (
        <LinkDialog
          projectId={project.id}
          link={editing ?? undefined}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(message) => {
            setEditing(undefined);
            changed(message);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('links.remove.title')}
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
              {t('links.remove.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => {
                if (removing !== undefined) {
                  void remove(removing);
                }
              }}
            >
              {t('links.remove.confirm')}
            </Button>
          </>
        }
      >
        {t('links.remove.text', { title: removing?.title ?? '' })}
      </Dialog>
    </Box>
  );
}

function noPaging() {
  // Every link of the project is in the one page.
}
