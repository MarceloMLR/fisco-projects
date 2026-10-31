import AddOutlined from '@mui/icons-material/AddOutlined';
import ContentCopyOutlined from '@mui/icons-material/ContentCopyOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import VisibilityOffOutlined from '@mui/icons-material/VisibilityOffOutlined';
import VisibilityOutlined from '@mui/icons-material/VisibilityOutlined';
import Box from '@mui/material/Box';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Credential, credentialProblems } from '../api/client';
import { credentialsQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDebounced } from '../hooks/useDebounced';
import { shownAddress } from '../links/links';
import { problemType } from '../projects/projectForm';
import { useProjectOutlet } from '../projects/projectOutlet';
import { theme } from '../theme/theme';
import { CredentialDialog } from './CredentialDialog';
import { CredentialImportDialog } from './CredentialImportDialog';
import { credentialRows, type CredentialRow, isGroupRow } from './credentials';
import { Button, IconButton } from '../components/Button';
import type { NoticeOptions } from '../notices/notify';

/** A project has a few dozen credentials: they all fit in one page of the free Data Grid (ADR-0050). */
const pageSize = 100;
const searchDelay = 300;
const all = '*';

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const monoSx = { ...ellipsisSx, fontFamily: 'var(--font-mono)', fontSize: 13 } as const;
const subSx = {
  ...ellipsisSx,
  display: 'block',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;
const passwordSx = {
  fontFamily: 'var(--font-mono)',
  fontSize: 13,
  letterSpacing: '.12em',
  color: 'var(--text)',
  minWidth: 104,
  ...ellipsisSx,
} as const;
const iconButtonSx = {
  flex: 'none',
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

const getRowId = (row: CredentialRow) => row.id;

/**
 * The tab Acessos of a project (canvas ProjetoAcessos, ADR-0034): the credentials grouped by environment, with the
 * portal, the address, the user with "Copiar usuário", the password masked with "Mostrar senha" and "Copiar senha",
 * the notes and "⋮" with "Editar" and "Excluir", as the person may (ADR-0045); the search by portal and user, the
 * environments and "Novo acesso". The password comes from the API only when asked and stays only in the screen until
 * it is hidden or the person leaves the tab; it never goes to the cache of the queries. On the phone, the line shows
 * the portal with the user below and the password.
 */
export function ProjectCredentialsPage() {
  const { t } = useTranslation();
  const { project, notify } = useProjectOutlet();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [search, setSearch] = useState('');
  const [environment, setEnvironment] = useState<string>(all);
  const [shown, setShown] = useState<Readonly<Record<string, string>>>({});
  const [menu, setMenu] = useState<{ anchor: HTMLElement; credential: Credential }>();
  // A credential to change, or null for a new one.
  const [editing, setEditing] = useState<Credential | null>();
  const [removing, setRemoving] = useState<Credential>();
  const [importing, setImporting] = useState(false);
  const searched = useDebounced(search, searchDelay).trim();
  const { data, isFetching, error } = useQuery({
    ...credentialsQuery(project.id, {
      search: searched === '' ? undefined : searched,
      environment: environment === all ? undefined : environment,
    }),
    placeholderData: keepPreviousData,
  });
  const permissions = data?.permissions;
  const rows = useMemo(() => credentialRows(data?.items ?? []), [data?.items]);

  const changed = useCallback(
    (message: string, options?: NoticeOptions) => {
      notify(message, options);
      setShown({});
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.credentials, project.id] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [notify, project.id, queryClient],
  );

  const passwordOf = useCallback(
    async (credential: Credential): Promise<string | undefined> => {
      try {
        const { data: answer } = await api.GET(
          '/api/projects/{projectId}/credentials/{credentialId}/password',
          { params: { path: { projectId: project.id, credentialId: credential.id } } },
        );
        return answer?.password;
      } catch {
        return undefined;
      }
    },
    [project.id],
  );

  const show = useCallback(
    async (credential: Credential) => {
      const password = await passwordOf(credential);
      if (password === undefined) {
        notify(t('credentials.messages.passwordFailed'), { tone: 'error' });
        return;
      }
      setShown((current) => ({ ...current, [credential.id]: password }));
    },
    [notify, passwordOf, t],
  );

  const hide = useCallback((credential: Credential) => {
    setShown((current) =>
      Object.fromEntries(Object.entries(current).filter(([id]) => id !== credential.id)),
    );
  }, []);

  const copy = useCallback(
    async (text: string | undefined, done: string) => {
      try {
        if (text === undefined) {
          throw new Error('Nothing to copy.');
        }
        await navigator.clipboard.writeText(text);
        notify(done);
      } catch {
        notify(t('credentials.messages.copyFailed'), { tone: 'error' });
      }
    },
    [notify, t],
  );

  const copyPassword = useCallback(
    async (credential: Credential) => {
      await copy(
        shown[credential.id] ?? (await passwordOf(credential)),
        t('credentials.passwordCopied'),
      );
    },
    [copy, passwordOf, shown, t],
  );

  const openMenu = useCallback((event: MouseEvent<HTMLElement>, credential: Credential) => {
    setMenu({ anchor: event.currentTarget, credential });
  }, []);

  const remove = async (credential: Credential) => {
    setRemoving(undefined);
    try {
      const { response, error: problem } = await api.DELETE(
        '/api/projects/{projectId}/credentials/{credentialId}',
        { params: { path: { projectId: project.id, credentialId: credential.id } } },
      );
      if (response.ok) {
        changed(t('credentials.messages.removed', { portal: credential.portal }));
      } else {
        // The list is asked again: the project may have been closed meanwhile.
        changed(
          response.status === 409 && problemType(problem) === credentialProblems.projectClosed
            ? t('credentials.messages.closed')
            : t('credentials.messages.failed'),
          { tone: 'error' },
        );
      }
    } catch {
      notify(t('credentials.messages.failed'), { tone: 'error' });
    }
  };

  const hasMenu = permissions?.canEdit === true || permissions?.canDelete === true;

  const columns = useMemo<DataTableColumn<CredentialRow>[]>(() => {
    const password: DataTableColumn<CredentialRow> = {
      field: 'password',
      headerName: t('credentials.columns.password'),
      width: desktop ? 200 : 188,
      renderCell: ({ row }) => {
        if (row.kind !== 'credential') {
          return null;
        }
        const visible = shown[row.credential.id];
        const Eye = visible === undefined ? VisibilityOutlined : VisibilityOffOutlined;
        return (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '2px', minWidth: 0 }}>
            <Box component="span" sx={passwordSx} title={visible} data-testid="credential-password">
              {visible ?? t('credentials.mask')}
            </Box>
            <IconButton
              size="small"
              aria-label={t(visible === undefined ? 'credentials.show' : 'credentials.hide', {
                portal: row.credential.portal,
              })}
              onClick={() => {
                if (visible === undefined) {
                  return show(row.credential);
                }
                hide(row.credential);
                return undefined;
              }}
              sx={iconButtonSx}
            >
              <Eye sx={{ fontSize: 'var(--icon-sm)' }} />
            </IconButton>
            <IconButton
              size="small"
              aria-label={t('credentials.copyPassword', { portal: row.credential.portal })}
              onClick={() => copyPassword(row.credential)}
              sx={iconButtonSx}
            >
              <ContentCopyOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
            </IconButton>
          </Box>
        );
      },
    };
    const portal: DataTableColumn<CredentialRow> = {
      field: 'portal',
      headerName: t('credentials.columns.portal'),
      flex: 1,
      minWidth: desktop ? 170 : 140,
      // An environment spans the whole row (fp-group-row).
      colSpan: (_value, row) => (row.kind === 'group' ? (desktop ? 6 : 3) : undefined),
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
          <Box sx={{ minWidth: 0 }}>
            <Box component="span" sx={{ ...ellipsisSx, display: 'block', fontWeight: 500 }}>
              {row.credential.portal}
            </Box>
            {!desktop && (
              <Box component="span" sx={subSx}>
                {row.credential.username}
              </Box>
            )}
          </Box>
        ),
    };
    const actions: DataTableColumn<CredentialRow> = {
      field: 'actions',
      headerName: t('credentials.columns.actions'),
      renderHeader: () => null,
      width: 52,
      align: 'right',
      renderCell: ({ row }) =>
        row.kind === 'credential' &&
        hasMenu && (
          <IconButton
            size="small"
            aria-label={t('credentials.menu.button', { portal: row.credential.portal })}
            aria-haspopup="menu"
            onClick={(event) => {
              openMenu(event, row.credential);
            }}
            sx={iconButtonSx}
          >
            <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
        ),
    };
    if (!desktop) {
      return [portal, password, actions];
    }
    return [
      portal,
      {
        field: 'url',
        headerName: t('credentials.columns.url'),
        flex: 1.5,
        minWidth: 220,
        renderCell: ({ row }) =>
          row.kind === 'credential' &&
          row.credential.url !== null && (
            <Box
              component="a"
              href={row.credential.url}
              target="_blank"
              rel="noopener noreferrer"
              title={row.credential.url}
              sx={{
                ...monoSx,
                display: 'block',
                color: 'var(--action-text)',
                textDecoration: 'none',
                '&:hover': { textDecoration: 'underline' },
              }}
            >
              {shownAddress(row.credential.url)}
            </Box>
          ),
      },
      {
        field: 'username',
        headerName: t('credentials.columns.username'),
        flex: 1.1,
        minWidth: 190,
        renderCell: ({ row }) =>
          row.kind === 'credential' && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: '2px', minWidth: 0 }}>
              <Box component="span" sx={{ ...monoSx, color: 'var(--text)' }}>
                {row.credential.username}
              </Box>
              <IconButton
                size="small"
                aria-label={t('credentials.copyUsername', { portal: row.credential.portal })}
                // Copying sends nothing to the server, so the button is never busy (ADR-0131).
                onClick={() => {
                  void copy(row.credential.username, t('credentials.usernameCopied'));
                }}
                sx={iconButtonSx}
              >
                <ContentCopyOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
              </IconButton>
            </Box>
          ),
      },
      password,
      {
        field: 'notes',
        headerName: t('credentials.columns.notes'),
        flex: 1,
        minWidth: 170,
        renderCell: ({ row }) =>
          row.kind === 'credential' && (
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
              {row.credential.notes}
            </Box>
          ),
      },
      actions,
    ];
  }, [copy, copyPassword, desktop, hasMenu, hide, openMenu, show, shown, t]);

  const footer = useCallback(
    () => t('credentials.total', { count: data?.items.length ?? 0 }),
    [data?.items.length, t],
  );

  if (error !== null) {
    return <Alert tone="danger">{t('credentials.loadFailed')}</Alert>;
  }

  const environments = data?.environments ?? [];
  // An environment that left the project, with its last credential removed, goes back to all of them.
  const chosen = environments.some((item) => item.name === environment) ? environment : all;
  const options = [
    { value: all, label: t('credentials.all'), count: data?.total },
    ...environments.map((item) => ({ value: item.name, label: item.name, count: item.count })),
  ];

  // Who removes credentials imports a spreadsheet (ADR-0045).
  const importSheet = (block: boolean) =>
    permissions?.canDelete === true && (
      <Button
        variant="outlined"
        size="small"
        startIcon={<UploadOutlined />}
        onClick={() => {
          setImporting(true);
        }}
        sx={block ? { width: '100%', flex: 'none' } : { flex: 'none' }}
      >
        {t('credentials.import.open')}
      </Button>
    );

  const newCredential = (block: boolean) =>
    permissions?.canEdit === true && (
      <Button
        variant="contained"
        size="small"
        startIcon={<AddOutlined />}
        onClick={() => {
          setEditing(null);
        }}
        sx={block ? { width: '100%', flex: 'none' } : { flex: 'none' }}
      >
        {t('credentials.newCredential')}
      </Button>
    );

  const emptyLabel =
    searched !== ''
      ? t('credentials.emptySearch')
      : permissions?.canEdit === true
        ? t('credentials.empty')
        : t('credentials.emptyReadOnly');

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
      <DataTable<CredentialRow>
        label={t('credentials.table')}
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
                label={t('credentials.search')}
                hideLabel
                placeholder={t('credentials.search')}
                type="search"
                size="sm"
                icon={SearchOutlined}
                width={desktop ? 260 : undefined}
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                }}
              />
            </Box>
            {desktop ? (
              <SegmentedControl<string>
                label={t('credentials.environment')}
                value={chosen}
                options={options}
                onChange={setEnvironment}
              />
            ) : (
              <Box sx={{ flex: '1 1 100%', minWidth: 0 }}>
                <Select<string>
                  label={t('credentials.environment')}
                  size="sm"
                  value={chosen}
                  options={options.map(({ value, label, count }) => ({
                    value,
                    label: count === undefined ? label : `${label} (${String(count)})`,
                  }))}
                  onChange={setEnvironment}
                />
              </Box>
            )}
            {desktop && <Box sx={{ flex: 1 }} />}
            {desktop && importSheet(false)}
            {desktop && newCredential(false)}
          </>
        }
      />
      {!desktop && importSheet(true)}
      {!desktop && newCredential(true)}
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
              menu === undefined
                ? undefined
                : t('credentials.menu.button', { portal: menu.credential.portal }),
          },
        }}
      >
        {permissions?.canEdit === true && (
          <MenuItem
            onClick={() => {
              setEditing(menu?.credential);
              setMenu(undefined);
            }}
          >
            <EditOutlined aria-hidden />
            {t('credentials.menu.edit')}
          </MenuItem>
        )}
        {permissions?.canDelete === true && (
          <MenuItem
            onClick={() => {
              setRemoving(menu?.credential);
              setMenu(undefined);
            }}
          >
            <DeleteOutlined aria-hidden />
            {t('credentials.menu.remove')}
          </MenuItem>
        )}
      </Menu>
      {editing !== undefined && (
        <CredentialDialog
          projectId={project.id}
          credential={editing ?? undefined}
          environments={environments.map((item) => item.name)}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(message) => {
            setEditing(undefined);
            changed(message);
          }}
        />
      )}
      {importing && (
        <CredentialImportDialog
          projectId={project.id}
          onClose={() => {
            setImporting(false);
          }}
          onImported={(message) => {
            setImporting(false);
            changed(message);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('credentials.remove.title')}
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
              {t('credentials.remove.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => (removing === undefined ? undefined : remove(removing))}
            >
              {t('credentials.remove.confirm')}
            </Button>
          </>
        }
      >
        {t('credentials.remove.text', {
          portal: removing?.portal ?? '',
          environment: removing?.environment ?? '',
        })}
      </Dialog>
    </Box>
  );
}

function noPaging() {
  // Every credential of the project is in the one page.
}
