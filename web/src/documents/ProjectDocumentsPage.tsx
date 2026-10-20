import FolderOutlined from '@mui/icons-material/FolderOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { type DragEvent, useCallback, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { DocumentFolder, DocumentRow, DocumentStatusFilter } from '../api/client';
import { documentFoldersQuery, documentsQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Select } from '../components/Select';
import { SideNav } from '../components/SideNav';
import { TextField } from '../components/TextField';
import { sizeText } from '../format/size';
import { useDates } from '../format/useDates';
import { useDebounced } from '../hooks/useDebounced';
import { useProjectOutlet } from '../projects/projectOutlet';
import { theme } from '../theme/theme';
import { DocumentDrawer } from './DocumentDrawer';
import { DocumentStatusBadge } from './DocumentStatusBadge';
import { FileIcon } from './FileIcon';
import { uploadDocuments, type UploadOutcome } from './uploadDocuments';

const pageSize = 50;
const searchDelay = 300;
const noDocuments = { items: [], total: 0 };
const noFolders: DocumentFolder[] = [];

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const subSx = {
  ...ellipsisSx,
  display: 'block',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;

/** Whether a drag carries files from the computer, not a piece of the page. */
const carriesFiles = (event: DragEvent) => event.dataTransfer.types.includes('Files');

/**
 * The tab Documentos of a project (canvas ProjetoDocumentos, ADR-0031): the folders in a column, with how many
 * documents not obsolete each has; the documents of the folder, or the ones the search finds in the whole project,
 * with the status filter; "Enviar arquivos" and dropping files on the list, one at a time, for who sends documents
 * (ADR-0045); and the details of the document chosen in a drawer (ADR-0081). On the phone the folders are a field.
 */
export function ProjectDocumentsPage() {
  const { t } = useTranslation();
  const { formatDate } = useDates();
  const { project, notify } = useProjectOutlet();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data: area, error } = useQuery(documentFoldersQuery(project.id));
  const folders = area?.folders ?? noFolders;
  const [chosenFolder, setChosenFolder] = useState<string>();
  // The first folder that takes documents, until the person chooses another.
  const folder =
    folders.find((item) => item.id === chosenFolder) ??
    folders.find((item) => !item.isAcceptance) ??
    folders[0];
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<DocumentStatusFilter>('Active');
  const [page, setPage] = useState(1);
  const [chosen, setChosen] = useState<string>();
  const [progress, setProgress] = useState<{ current: number; total: number }>();
  const [dragging, setDragging] = useState(false);
  const input = useRef<HTMLInputElement>(null);
  const searched = useDebounced(search, searchDelay).trim();
  const searching = searched !== '';

  const { data, isFetching } = useQuery({
    ...documentsQuery(project.id, {
      folderId: searching ? undefined : folder?.id,
      search: searching ? searched : undefined,
      status,
      page,
      pageSize,
    }),
    enabled: folder !== undefined || searching,
    placeholderData: keepPreviousData,
  });
  const documents = data ?? noDocuments;
  const permissions = area?.permissions;
  const takesFiles =
    permissions?.canUpload === true && folder !== undefined && !folder.isAcceptance && !searching;

  const tell = useCallback(
    (outcome: UploadOutcome) => {
      if (outcome.closed) {
        notify(t('documents.messages.closed'));
        return;
      }
      const size = sizeText(area?.maxBytes ?? 0);
      const parts = [
        ...(outcome.sent > 0 ? [t('documents.uploaded', { count: outcome.sent })] : []),
        ...outcome.refused.map(({ name, reason }) =>
          t(`documents.refused.${reason}`, { name, size }),
        ),
      ];
      notify(parts.join(' '));
    },
    [area?.maxBytes, notify, t],
  );

  const send = useCallback(
    async (files: readonly File[]) => {
      if (folder === undefined || files.length === 0) {
        return;
      }
      const outcome = await uploadDocuments(project.id, folder.id, files, (current, total) => {
        setProgress({ current, total });
      });
      setProgress(undefined);
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.documents, project.id] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
      tell(outcome);
    },
    [folder, project.id, queryClient, tell],
  );

  const columns = useMemo<DataTableColumn<DocumentRow>[]>(() => {
    const name: DataTableColumn<DocumentRow> = {
      field: 'name',
      headerName: t('documents.columns.name'),
      flex: 2,
      minWidth: desktop ? 220 : 180,
      renderCell: ({ row }) => {
        const details = [
          ...(searching ? [row.folderName] : []),
          t('documents.version', { version: row.version }),
          row.link?.name ?? t('documents.noLink'),
        ].join(' · ');
        return (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', minWidth: 0 }}>
            <FileIcon name={row.name} size={20} />
            <Box sx={{ minWidth: 0 }}>
              <Box component="span" sx={{ ...ellipsisSx, display: 'block', fontWeight: 500 }}>
                {row.name}
              </Box>
              <Box component="span" sx={subSx}>
                {details}
              </Box>
            </Box>
          </Box>
        );
      },
    };
    const statusColumn: DataTableColumn<DocumentRow> = {
      field: 'status',
      headerName: t('documents.columns.status'),
      width: 132,
      renderCell: ({ row }) => <DocumentStatusBadge status={row.status} />,
    };
    if (!desktop) {
      return [name, statusColumn];
    }
    return [
      name,
      statusColumn,
      {
        field: 'updated',
        headerName: t('documents.columns.updated'),
        width: 150,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0 }}>
            <Box component="span" sx={{ display: 'block' }}>
              {formatDate(row.updated.at)}
            </Box>
            <Box component="span" sx={subSx}>
              {row.updated.name}
            </Box>
          </Box>
        ),
      },
      {
        field: 'size',
        headerName: t('documents.columns.size'),
        width: 96,
        align: 'right',
        headerAlign: 'right',
        renderCell: ({ row }) => sizeText(row.size),
      },
    ];
  }, [desktop, formatDate, searching, t]);

  const footer = useCallback(
    ({ count }: { count: number }) => t('documents.total', { count }),
    [t],
  );
  const choose = useCallback((row: DocumentRow) => {
    setChosen(row.id);
  }, []);

  if (error !== null) {
    return <Alert tone="danger">{t('documents.loadFailed')}</Alert>;
  }

  const chooseFolder = (id: string) => {
    setChosenFolder(id);
    setSearch('');
    setPage(1);
  };

  const emptyLabel = searching
    ? t('documents.emptySearch')
    : folder?.isAcceptance === true
      ? t('documents.emptyTerms')
      : takesFiles
        ? t('documents.empty')
        : t('documents.emptyReadOnly');

  const upload = takesFiles ? (
    <>
      <Button
        variant="contained"
        startIcon={<UploadOutlined />}
        disabled={progress !== undefined}
        fullWidth={!desktop}
        onClick={() => input.current?.click()}
        sx={{ flex: 'none' }}
      >
        {progress === undefined
          ? t('documents.upload')
          : t('documents.uploading', { current: progress.current, total: progress.total })}
      </Button>
      <input
        ref={input}
        type="file"
        multiple
        hidden
        aria-hidden
        tabIndex={-1}
        onChange={(event) => {
          const files = [...(event.target.files ?? [])];
          event.target.value = '';
          void send(files);
        }}
      />
    </>
  ) : undefined;

  const tools = (
    <Box
      sx={{
        flex: 'none',
        display: 'flex',
        flexWrap: desktop ? 'nowrap' : 'wrap',
        gap: 'var(--space-2)',
      }}
    >
      <Box sx={{ flex: 1, minWidth: desktop ? 0 : '100%' }}>
        <TextField
          label={t('documents.search')}
          hideLabel
          placeholder={t('documents.search')}
          type="search"
          size="sm"
          icon={SearchOutlined}
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
      </Box>
      <Select<DocumentStatusFilter>
        label={t('documents.statusFilter')}
        size="sm"
        width={desktop ? 200 : undefined}
        value={status}
        options={(['Active', 'All'] as const).map((value) => ({
          value,
          label: t(`documents.filters.${value}`),
        }))}
        onChange={(value) => {
          setStatus(value);
          setPage(1);
        }}
      />
    </Box>
  );

  const table = (
    <Box
      data-testid="documents-drop"
      onDragEnter={(event) => {
        if (takesFiles && carriesFiles(event)) {
          event.preventDefault();
          setDragging(true);
        }
      }}
      onDragOver={(event) => {
        if (takesFiles && carriesFiles(event)) {
          event.preventDefault();
          event.dataTransfer.dropEffect = 'copy';
        }
      }}
      onDragLeave={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          setDragging(false);
        }
      }}
      onDrop={(event) => {
        if (!takesFiles) {
          return;
        }
        event.preventDefault();
        setDragging(false);
        void send([...event.dataTransfer.files]);
      }}
      sx={{ position: 'relative', flex: 1, minHeight: desktop ? 0 : 360, display: 'flex' }}
    >
      <DataTable
        label={t('documents.table')}
        rows={documents.items}
        columns={columns}
        rowCount={documents.total}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        loading={isFetching}
        emptyLabel={emptyLabel}
        selectedRowId={chosen}
        onRowChoose={choose}
        displayedRowsLabel={footer}
      />
      {dragging && folder !== undefined && (
        <Box
          aria-hidden
          sx={{
            position: 'absolute',
            inset: 0,
            zIndex: 3,
            display: 'grid',
            placeItems: 'center',
            padding: 'var(--space-4)',
            border: '2px dashed var(--action)',
            borderRadius: 'var(--radius-lg)',
            backgroundColor: 'var(--bg-selected)',
            color: 'var(--action-text)',
            fontWeight: 500,
            textAlign: 'center',
            pointerEvents: 'none',
          }}
        >
          {t('documents.dropHere', { folder: folder.name })}
        </Box>
      )}
    </Box>
  );

  const heading = (
    <Box
      sx={{
        flex: 'none',
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        minWidth: 0,
        // The height of the button, so the list does not move between a folder that takes files and one that does not.
        minHeight: 36,
      }}
    >
      <Box
        component="h2"
        sx={{
          flex: 1,
          minWidth: 0,
          margin: 0,
          fontSize: 16,
          lineHeight: '24px',
          fontWeight: 500,
          color: 'var(--text)',
          overflowWrap: 'anywhere',
        }}
      >
        {searching ? t('documents.searchResults') : folder?.name}
        {data !== undefined && (
          <Box
            component="span"
            sx={{
              marginLeft: 'var(--space-2)',
              fontWeight: 400,
              color: 'var(--text-secondary)',
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {documents.total}
          </Box>
        )}
      </Box>
      {desktop && upload}
    </Box>
  );

  const note =
    folder?.isAcceptance === true && !searching ? (
      <Box sx={{ flex: 'none' }}>
        <Alert tone="info">{t('documents.termsFolderNote')}</Alert>
      </Box>
    ) : null;

  const drawer = (
    <DocumentDrawer
      projectId={project.id}
      documentId={chosen}
      onClose={() => {
        setChosen(undefined);
      }}
      notify={notify}
    />
  );

  if (!desktop) {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
        <Select
          label={t('documents.folderLabel')}
          value={folder?.id ?? ''}
          options={folders.map((item) => ({
            value: item.id,
            label: `${item.name} (${String(item.count)})`,
          }))}
          onChange={chooseFolder}
        />
        {tools}
        {upload}
        {heading}
        {note}
        {table}
        {drawer}
      </Box>
    );
  }

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: '232px minmax(0, 1fr)',
        overflow: 'hidden',
      }}
    >
      <SideNav
        label={t('documents.folders')}
        items={folders.map((item) => ({
          key: item.id,
          label: item.name,
          icon: FolderOutlined,
          count: item.count,
        }))}
        current={searching ? undefined : folder?.id}
        onChoose={chooseFolder}
      />
      <Box
        sx={{
          minWidth: 0,
          minHeight: 0,
          boxSizing: 'border-box',
          padding: 'var(--space-5) var(--space-6) var(--space-6)',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-4)',
        }}
      >
        {heading}
        {tools}
        {note}
        {table}
      </Box>
      {drawer}
    </Box>
  );
}
