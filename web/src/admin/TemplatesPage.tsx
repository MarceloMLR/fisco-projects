import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type ProjectTemplate } from '../api/client';
import { allTemplatesQuery, queryKeys } from '../api/queries';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { StatusBadge } from '../components/StatusBadge';
import { Page } from '../shell/Page';
import { TemplateDialog } from './TemplateDialog';
import { Button, IconButton } from '../components/Button';
import { useNotify } from '../notices/notify';

/** The models fit in one page: an installation keeps a few of them. */
const pageSize = 100;
const noTemplates: ProjectTemplate[] = [];
const isInactive = (template: ProjectTemplate) => !template.isActive;

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

const countItems = (template: ProjectTemplate) =>
  template.phases.reduce((total, phase) => total + phase.items.length, 0);

/**
 * "Modelos de projeto" of the Administration (ADR-0017, ADR-0046), drawn as the Clients, since the canvas has no screen
 * for it yet: the models by name, with the description, how many phases and items, and the situation. Each one has a
 * menu to edit the name and the description, deactivate or reactivate, and remove. New models come from "Salvar como
 * modelo" in the schedule of a project.
 */
export function TemplatesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data, isFetching } = useQuery(allTemplatesQuery());
  const templates = data ?? noTemplates;
  const notify = useNotify();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; template: ProjectTemplate }>();
  const [editing, setEditing] = useState<ProjectTemplate>();
  const [removing, setRemoving] = useState<ProjectTemplate>();
  const [pending, setPending] = useState(false);

  const changed = useCallback(
    (text: string) => {
      notify(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.templates });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [notify, queryClient],
  );

  const setActive = useCallback(
    async (template: ProjectTemplate, active: boolean) => {
      setMenu(undefined);
      try {
        const { response } = await api.PUT('/api/admin/project-templates/{id}', {
          params: { path: { id: template.id } },
          body: { name: template.name, description: template.description, isActive: active },
        });
        if (response.ok) {
          changed(
            t(active ? 'templates.messages.reactivated' : 'templates.messages.deactivated', {
              name: template.name,
            }),
          );
        } else {
          notify(t('templates.messages.failed'), { tone: 'error' });
        }
      } catch {
        notify(t('templates.messages.failed'), { tone: 'error' });
      }
    },
    [notify, changed, t],
  );

  const remove = async (template: ProjectTemplate) => {
    setPending(true);
    try {
      const { response } = await api.DELETE('/api/admin/project-templates/{id}', {
        params: { path: { id: template.id } },
      });
      if (response.ok) {
        changed(t('templates.messages.removed', { name: template.name }));
      } else {
        notify(t('templates.messages.failed'), { tone: 'error' });
      }
    } catch {
      notify(t('templates.messages.failed'), { tone: 'error' });
    } finally {
      setPending(false);
      setRemoving(undefined);
    }
  };

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, template: ProjectTemplate) => {
    setMenu({ anchor: event.currentTarget, template });
  }, []);

  const columns = useMemo<DataTableColumn<ProjectTemplate>[]>(
    () => [
      {
        field: 'name',
        headerName: t('templates.columns.name'),
        flex: 1.4,
        minWidth: 220,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              fontWeight: 500,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {row.name}
          </Box>
        ),
      },
      {
        field: 'structure',
        headerName: t('templates.columns.structure'),
        width: 180,
        valueGetter: (_value, row) =>
          t('templates.structure', {
            phases: t('templates.phases', { count: row.phases.length }),
            items: t('templates.items', { count: countItems(row) }),
          }),
      },
      {
        field: 'status',
        headerName: t('templates.columns.status'),
        width: 120,
        renderCell: ({ row }) =>
          row.isActive ? (
            <StatusBadge tone="success" icon={CheckCircleOutlined}>
              {t('templates.statuses.Active')}
            </StatusBadge>
          ) : (
            <StatusBadge tone="neutral" icon={BlockOutlined}>
              {t('templates.statuses.Inactive')}
            </StatusBadge>
          ),
      },
      {
        field: 'description',
        headerName: t('templates.columns.description'),
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
            {row.description ?? t('audit.details.none')}
          </Box>
        ),
      },
      {
        field: 'actions',
        headerName: t('templates.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) => (
          <IconButton
            size="small"
            aria-label={t('templates.menu.button', { name: row.name })}
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
    ({ count }: { count: number }) => t('templates.total', { count }),
    [t],
  );

  return (
    <Page count={data === undefined ? undefined : templates.length}>
      <DataTable
        label={t('templates.table')}
        rows={templates}
        columns={columns}
        rowCount={templates.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        isRowMuted={isInactive}
        emptyLabel={t('templates.empty')}
        displayedRowsLabel={footer}
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
          paper: { sx: { width: 200, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined
                ? undefined
                : t('templates.menu.button', { name: menu.template.name }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.template);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('templates.menu.edit')}
        </MenuItem>
        {menu?.template.isActive === false ? (
          <MenuItem
            onClick={() => {
              void setActive(menu.template, true);
            }}
          >
            <CheckCircleOutlined aria-hidden />
            {t('templates.menu.reactivate')}
          </MenuItem>
        ) : (
          <MenuItem
            onClick={() => {
              if (menu !== undefined) {
                void setActive(menu.template, false);
              }
            }}
          >
            <BlockOutlined aria-hidden />
            {t('templates.menu.deactivate')}
          </MenuItem>
        )}
        <Divider sx={{ margin: 'var(--space-1) 0' }} />
        <MenuItem
          onClick={() => {
            setRemoving(menu?.template);
            setMenu(undefined);
          }}
          sx={dangerItemSx}
        >
          <DeleteOutlined aria-hidden />
          {t('templates.menu.remove')}
        </MenuItem>
      </Menu>
      {editing !== undefined && (
        <TemplateDialog
          template={editing}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(saved) => {
            setEditing(undefined);
            changed(saved);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('templates.remove.title')}
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
              {t('templates.remove.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              loading={pending}
              onClick={() => (removing === undefined ? undefined : remove(removing))}
            >
              {t('templates.remove.confirm')}
            </Button>
          </>
        }
      >
        {removing !== undefined && t('templates.remove.text', { name: removing.name })}
      </Dialog>
    </Page>
  );
}

function noPaging() {
  // Every model is in the one page.
}
