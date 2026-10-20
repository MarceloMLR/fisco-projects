import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import FilterListOutlined from '@mui/icons-material/FilterListOutlined';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import {
  type DragEvent,
  type MouseEvent,
  useCallback,
  useId,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import { api, credentialProblems, type Spreadsheet } from '../api/client';
import { Alert } from '../components/Alert';
import { Checkbox } from '../components/Checkbox';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';
import { theme } from '../theme/theme';
import {
  type CredentialFieldName,
  credentialFields,
  type CredentialValues,
} from './credentialForm';
import {
  credentialOf,
  type ImportChoice,
  problemOf,
  type RowProblem,
  rowHas,
  type SheetRow,
  suggestMapping,
} from './credentialImport';

const pageSize = 100;
const none = 'none';
const typed = 'typed';
const required: readonly CredentialFieldName[] = ['portal', 'environment', 'username', 'password'];

/** A line of the table: the line of the spreadsheet, the credential it becomes and what keeps it out. */
interface ImportLine {
  id: string;
  row: SheetRow;
  values: CredentialValues;
  problem: RowProblem | undefined;
}

const getRowId = (line: ImportLine) => line.id;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

/** Clicks on the mark of a line stay with it, not choosing the line. */
function keepLine(event: MouseEvent) {
  event.stopPropagation();
}

/** Whether a drag carries files from the computer. */
const carriesFiles = (event: DragEvent) => event.dataTransfer.types.includes('Files');

/** The reason of a refusal of the API, as the problem of the import says it. */
function reasonOf(problem: unknown): string | undefined {
  return typeof problem === 'object' &&
    problem !== null &&
    'reason' in problem &&
    typeof problem.reason === 'string'
    ? problem.reason
    : undefined;
}

const refusals = ['Type', 'Unreadable', 'Empty', 'TooLarge', 'TooManyRows'] as const;
const isRefusal = (reason: string): reason is (typeof refusals)[number] =>
  refusals.some((refusal) => refusal === reason);

/** The lines the API refused, from the problem of the import. */
function refusedRows(problem: unknown): number[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'rows' in problem &&
    Array.isArray(problem.rows)
    ? problem.rows
        .map((row: unknown) =>
          typeof row === 'object' && row !== null && 'row' in row && typeof row.row === 'number'
            ? row.row
            : undefined,
        )
        .filter((row): row is number => row !== undefined)
    : [];
}

/**
 * "Importar planilha" of the tab Acessos (ADR-0034, ADR-0094): the .xlsx or .csv chosen or dropped, read by the API
 * without keeping anything; the sheet, when it has more than one; the columns of each field, suggested by the header,
 * with the environment also typed for every line; the filter of the lines, as the client; the lines marked, none at
 * the start, a line with a problem never marked; and "Importar N acessos", all or none. The passwords show only as the
 * mask. Only who removes credentials imports (ADR-0045).
 */
export function CredentialImportDialog({
  projectId,
  onClose,
  onImported,
}: {
  projectId: string;
  onClose: () => void;
  onImported: (message: string) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const input = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File>();
  const [spreadsheet, setSpreadsheet] = useState<Spreadsheet>();
  const [choice, setChoice] = useState<ImportChoice>();
  const [reading, setReading] = useState(false);
  const [refusal, setRefusal] = useState<string>();
  const [filter, setFilter] = useState('');
  const [marked, setMarked] = useState<ReadonlySet<number>>(new Set());
  const [page, setPage] = useState(1);
  const [importing, setImporting] = useState(false);
  const [failure, setFailure] = useState<string>();
  const [dragging, setDragging] = useState(false);

  const read = async (chosen: File, sheet?: number) => {
    setReading(true);
    setRefusal(undefined);
    setFailure(undefined);
    const form = new FormData();
    form.append('file', chosen, chosen.name);
    if (sheet !== undefined) {
      form.append('sheet', String(sheet));
    }
    try {
      const { data, error, response } = await api.POST(
        '/api/projects/{projectId}/credentials/import/preview',
        {
          params: { path: { projectId } },
          body: { file: '', ...(sheet === undefined ? {} : { sheet }) },
          bodySerializer: () => form,
        },
      );
      if (data !== undefined) {
        setFile(chosen);
        setSpreadsheet(data);
        setChoice({ mapping: suggestMapping(data.columns), environment: null });
        setMarked(new Set());
        setFilter('');
        setPage(1);
        return;
      }
      const reason = reasonOf(error);
      setRefusal(
        response.status === 400 && reason !== undefined
          ? t(
              isRefusal(reason)
                ? `credentials.import.refused.${reason}`
                : 'credentials.import.refused.Unreadable',
            )
          : response.status === 409
            ? t('credentials.messages.closed')
            : t('credentials.import.failed'),
      );
    } catch {
      setRefusal(t('credentials.import.failed'));
    } finally {
      setReading(false);
    }
  };

  const lines = useMemo<ImportLine[]>(() => {
    if (spreadsheet === undefined || choice === undefined) {
      return [];
    }
    return spreadsheet.rows.map((row) => {
      const values = credentialOf(row, choice);
      return { id: String(row.number), row, values, problem: problemOf(values) };
    });
  }, [spreadsheet, choice]);
  const shown = useMemo(() => lines.filter((line) => rowHas(line.row, filter)), [lines, filter]);
  const markable = useMemo(() => shown.filter((line) => line.problem === undefined), [shown]);
  const chosen = lines.filter((line) => line.problem === undefined && marked.has(line.row.number));
  const allMarked = markable.length > 0 && markable.every((line) => marked.has(line.row.number));

  const mark = useCallback((line: ImportLine, checked: boolean) => {
    setMarked((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(line.row.number);
      } else {
        next.delete(line.row.number);
      }
      return next;
    });
  }, []);

  const markShown = useCallback(
    (checked: boolean) => {
      setMarked((current) => {
        const next = new Set(current);
        for (const line of markable) {
          if (checked) {
            next.add(line.row.number);
          } else {
            next.delete(line.row.number);
          }
        }
        return next;
      });
    },
    [markable],
  );

  const problemText = useCallback(
    (problem: RowProblem) =>
      problem.kind === 'urlInvalid'
        ? t('credentials.import.problems.urlInvalid')
        : t(`credentials.import.problems.${problem.kind}.${problem.field}`),
    [t],
  );

  const columns = useMemo<DataTableColumn<ImportLine>[]>(() => {
    const text = (
      field: CredentialFieldName,
      header: string,
      flex: number,
    ): DataTableColumn<ImportLine> => ({
      field,
      headerName: header,
      flex,
      minWidth: 120,
      renderCell: ({ row }) => (
        <Box component="span" sx={ellipsisSx}>
          {row.values[field]}
        </Box>
      ),
    });
    return [
      {
        field: 'select',
        width: 44,
        renderHeader: () => (
          <Box onClick={keepLine} sx={{ display: 'flex' }}>
            <Checkbox
              label={t('credentials.import.markAll')}
              hideLabel
              checked={allMarked}
              onChange={markShown}
            />
          </Box>
        ),
        renderCell: ({ row }) =>
          row.problem === undefined ? (
            <Box onClick={keepLine} sx={{ display: 'flex' }}>
              <Checkbox
                label={t('credentials.import.markLine', { number: row.row.number })}
                hideLabel
                checked={marked.has(row.row.number)}
                onChange={(checked) => {
                  mark(row, checked);
                }}
              />
            </Box>
          ) : null,
      },
      {
        field: 'number',
        headerName: t('credentials.import.columns.line'),
        width: 64,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{ color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}
          >
            {row.row.number}
          </Box>
        ),
      },
      text('portal', t('credentials.columns.portal'), 1),
      text('environment', t('credentials.dialog.environment'), 0.8),
      ...(desktop ? [text('url', t('credentials.columns.url'), 1.2)] : []),
      text('username', t('credentials.columns.username'), 1),
      {
        field: 'password',
        headerName: t('credentials.columns.password'),
        width: 120,
        renderCell: ({ row }) =>
          row.values.password === '' ? null : (
            <Box component="span" sx={{ fontFamily: 'var(--font-mono)', letterSpacing: '.12em' }}>
              {t('credentials.mask')}
            </Box>
          ),
      },
      ...(desktop ? [text('notes', t('credentials.columns.notes'), 1)] : []),
      {
        field: 'problem',
        headerName: t('credentials.import.columns.problem'),
        width: 180,
        renderCell: ({ row }) =>
          row.problem === undefined ? null : (
            <Box
              component="span"
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 'var(--space-1)',
                color: 'var(--danger-text)',
                ...ellipsisSx,
              }}
            >
              <ErrorOutlineOutlined aria-hidden sx={{ fontSize: 'var(--icon-sm)' }} />
              {problemText(row.problem)}
            </Box>
          ),
      },
    ];
  }, [allMarked, desktop, mark, markShown, marked, problemText, t]);

  const footer = useCallback(
    () => t('credentials.import.lines', { count: shown.length }),
    [shown.length, t],
  );

  const choose = (field: CredentialFieldName, value: string) => {
    if (choice === undefined) {
      return;
    }
    const column = value === none || value === typed ? null : Number(value);
    setChoice({
      mapping: { ...choice.mapping, [field]: column },
      environment:
        field !== 'environment'
          ? choice.environment
          : value === typed
            ? (choice.environment ?? '')
            : null,
    });
  };

  const save = async () => {
    setImporting(true);
    setFailure(undefined);
    try {
      const { response, error } = await api.POST('/api/projects/{projectId}/credentials/import', {
        params: { path: { projectId } },
        body: {
          items: chosen.map((line) => ({
            row: line.row.number,
            portal: line.values.portal.trim(),
            environment: line.values.environment.trim(),
            url: line.values.url.trim() === '' ? null : line.values.url.trim(),
            username: line.values.username.trim(),
            password: line.values.password,
            notes: line.values.notes.trim() === '' ? null : line.values.notes.trim(),
          })),
        },
      });
      if (response.ok) {
        onImported(t('credentials.import.done', { count: chosen.length }));
        return;
      }
      setFailure(
        response.status === 400
          ? t('credentials.import.invalid', { rows: refusedRows(error).join(', ') })
          : response.status === 409 && problemType(error) === credentialProblems.projectClosed
            ? t('credentials.messages.closed')
            : t('credentials.import.failed'),
      );
    } catch {
      setFailure(t('credentials.import.failed'));
    } finally {
      setImporting(false);
    }
  };

  const optionsOf = (field: CredentialFieldName) => [
    required.includes(field)
      ? { value: none, label: t('credentials.import.chooseColumn'), disabled: true }
      : { value: none, label: t('credentials.import.noColumn') },
    ...(field === 'environment'
      ? [{ value: typed, label: t('credentials.import.sameForAll') }]
      : []),
    ...(spreadsheet?.columns ?? []).map((column, index) => ({
      value: String(index),
      label: column,
    })),
  ];

  const valueOf = (field: CredentialFieldName) => {
    if (
      field === 'environment' &&
      choice?.environment !== null &&
      choice?.environment !== undefined
    ) {
      return typed;
    }
    const index = choice?.mapping[field] ?? null;
    return index === null ? none : String(index);
  };

  const picker = (
    <Box
      onDragEnter={(event) => {
        if (carriesFiles(event)) {
          event.preventDefault();
          setDragging(true);
        }
      }}
      onDragOver={(event) => {
        if (carriesFiles(event)) {
          event.preventDefault();
          event.dataTransfer.dropEffect = 'copy';
        }
      }}
      onDragLeave={() => {
        setDragging(false);
      }}
      onDrop={(event) => {
        event.preventDefault();
        setDragging(false);
        const dropped = event.dataTransfer.files[0];
        if (dropped !== undefined) {
          void read(dropped);
        }
      }}
      sx={{
        flex: 1,
        display: 'grid',
        placeItems: 'center',
        margin: 'var(--space-5)',
        padding: 'var(--space-6)',
        border: '2px dashed var(--border-strong)',
        borderColor: dragging ? 'var(--action)' : 'var(--border-strong)',
        borderRadius: 'var(--radius-lg)',
        backgroundColor: dragging ? 'var(--bg-selected)' : 'var(--bg-subtle)',
        textAlign: 'center',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 'var(--space-3)',
          maxWidth: 420,
        }}
      >
        <UploadFileOutlined aria-hidden sx={{ fontSize: 40, color: 'var(--text-secondary)' }} />
        <Box sx={{ color: 'var(--text-secondary)' }}>{t('credentials.import.pick')}</Box>
        <Button variant="contained" disabled={reading} onClick={() => input.current?.click()}>
          {reading ? t('credentials.import.reading') : t('credentials.import.choose')}
        </Button>
        {refusal !== undefined && <Alert tone="danger">{refusal}</Alert>}
      </Box>
    </Box>
  );

  const body =
    spreadsheet === undefined || choice === undefined ? (
      picker
    ) : (
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-4)',
          padding: 'var(--space-4) var(--space-5) 0',
          overflowY: desktop ? 'hidden' : 'auto',
        }}
      >
        <Box
          sx={{ display: 'flex', alignItems: 'flex-end', gap: 'var(--space-3)', flexWrap: 'wrap' }}
        >
          {spreadsheet.sheets.length > 1 && (
            <FormField label={t('credentials.import.sheet')} width={desktop ? 240 : undefined}>
              <Select<string>
                label={t('credentials.import.sheet')}
                value={String(spreadsheet.sheet)}
                options={spreadsheet.sheets.map((sheet, index) => ({
                  value: String(index),
                  label: sheet,
                }))}
                onChange={(value) => {
                  if (file !== undefined) {
                    void read(file, Number(value));
                  }
                }}
              />
            </FormField>
          )}
          <Button variant="outlined" disabled={reading} onClick={() => input.current?.click()}>
            {t('credentials.import.change')}
          </Button>
        </Box>
        <Box
          role="group"
          aria-labelledby={`${ids}-mapping`}
          sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}
        >
          <Box
            id={`${ids}-mapping`}
            component="h3"
            sx={{ margin: 0, fontSize: 14, fontWeight: 500 }}
          >
            {t('credentials.import.mapping')}
          </Box>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
              gap: 'var(--space-3)',
            }}
          >
            {credentialFields.map((field) => (
              <FormField key={field} label={t(`credentials.import.fields.${field}`)}>
                <Select<string>
                  label={t(`credentials.import.fields.${field}`)}
                  value={valueOf(field)}
                  options={optionsOf(field)}
                  invalid={required.includes(field) && valueOf(field) === none}
                  onChange={(value) => {
                    choose(field, value);
                  }}
                />
              </FormField>
            ))}
            {choice.environment !== null && (
              <TextField
                label={t('credentials.import.environmentForAll')}
                autoComplete="off"
                maxLength={70}
                value={choice.environment}
                onChange={(event) => {
                  setChoice({ ...choice, environment: event.target.value });
                }}
              />
            )}
          </Box>
        </Box>
        <Box sx={{ flex: desktop ? 1 : 'none', minHeight: desktop ? 0 : 420, display: 'flex' }}>
          <DataTable<ImportLine>
            label={t('credentials.import.table')}
            rows={shown.slice((page - 1) * pageSize, page * pageSize)}
            columns={columns}
            rowCount={shown.length}
            page={page}
            pageSize={pageSize}
            onPageChange={setPage}
            rowHeight={44}
            emptyLabel={t('credentials.import.empty')}
            displayedRowsLabel={footer}
            getRowId={getRowId}
            tools={
              <>
                <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
                  <TextField
                    label={t('credentials.import.filter')}
                    hideLabel
                    placeholder={t('credentials.import.filter')}
                    type="search"
                    size="sm"
                    icon={FilterListOutlined}
                    width={desktop ? 280 : undefined}
                    value={filter}
                    onChange={(event) => {
                      setFilter(event.target.value);
                      setPage(1);
                    }}
                  />
                </Box>
                <Box component="span" sx={{ color: 'var(--text-secondary)', fontSize: 13 }}>
                  {t('credentials.import.marked', { count: chosen.length, total: lines.length })}
                </Box>
              </>
            }
          />
        </Box>
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      </Box>
    );

  return (
    <Dialog
      open
      size="full"
      title={t('credentials.import.title')}
      subtitle={file?.name ?? t('credentials.import.subtitle')}
      onClose={onClose}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('credentials.import.cancel')}
          </Button>
          <Button
            variant="contained"
            disabled={chosen.length === 0 || importing}
            onClick={() => {
              void save();
            }}
          >
            {t('credentials.import.confirm', { count: chosen.length })}
          </Button>
        </>
      }
    >
      {body}
      <input
        ref={input}
        type="file"
        accept=".xlsx,.csv"
        hidden
        aria-hidden
        tabIndex={-1}
        data-testid="credential-import-file"
        onChange={(event) => {
          const picked = event.target.files?.[0];
          event.target.value = '';
          if (picked !== undefined) {
            void read(picked);
          }
        }}
      />
    </Dialog>
  );
}
