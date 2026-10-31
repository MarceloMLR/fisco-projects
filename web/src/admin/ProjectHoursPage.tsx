import DownloadOutlined from '@mui/icons-material/DownloadOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import type { ProjectHours, ProjectHoursView } from '../api/client';
import { prefetchProjectHours } from '../api/prefetch';
import { projectHoursQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { ProgressBar } from '../components/ProgressBar';
import { SegmentedControl } from '../components/SegmentedControl';
import { TextField } from '../components/TextField';
import { saveFile } from '../format/saveFile';
import { useDates } from '../format/useDates';
import { xlsxType } from '../format/xlsx';
import { useDebounced } from '../hooks/useDebounced';
import { ProjectStatusBadge } from '../projects/ProjectStatusBadge';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ProjectHoursDrawer } from './ProjectHoursDrawer';
import { HoursBalanceBadge } from './HoursBalanceBadge';
import { balanceLook, hoursOrDash, projectHoursMeta } from './projectHours';
import { projectHoursReport, projectHoursReportName } from './projectHoursReport';
import { Button } from '../components/Button';

/** A page of the list; an installation has a few dozen projects, so most views fit in one. */
const pageSize = 50;
const searchDelay = 300;
const noRows: ProjectHours[] = [];

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const numberSx = { fontVariantNumeric: 'tabular-nums' } as const;
const mutedSx = { ...numberSx, color: 'var(--text-secondary)' } as const;

const getRowId = (row: ProjectHours) => row.id;

/**
 * Horas dos projetos, in the section Acompanhamento of the Administration (canvas Horas do projeto, AdminHoras,
 * ADR-0128): how many projects the view shows and how many are over, "Baixar planilha", the search by code or name,
 * the views Em aberto, Encerrados and Todos, and the projects with the contracted, the additional and the logged
 * hours, the balance by icon, color and text and the consumption. A project chosen opens its hours in the drawer, by
 * the ?project= of the address, without entering the history (ADR-0105).
 */
export function ProjectHoursPage() {
  const { t } = useTranslation();
  const dates = useDates();
  const { session } = useSession();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();
  const [view, setView] = useState<ProjectHoursView>('Open');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [message, setMessage] = useState<string>();
  const [exporting, setExporting] = useState(false);
  const searched = useDebounced(search, searchDelay).trim();
  const { data, isFetching, error } = useQuery({
    ...projectHoursQuery(view, searched),
    placeholderData: keepPreviousData,
  });
  const rows = data ?? noRows;
  const shown = useMemo(() => rows.slice((page - 1) * pageSize, page * pageSize), [rows, page]);
  const chosen = params.get('project') ?? undefined;

  const choose = useCallback(
    (row: ProjectHours) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          next.set('project', row.id);
          return next;
        },
        { replace: true },
      );
    },
    [setParams],
  );

  const close = useCallback(() => {
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.delete('project');
        return next;
      },
      { replace: true },
    );
  }, [setParams]);

  // The hours of a project start loading when the mouse comes over its row (ADR-0106).
  const hover = useCallback(
    (row: ProjectHours) => {
      prefetchProjectHours(queryClient, row.id);
    },
    [queryClient],
  );

  const changed = useCallback(
    (text: string) => {
      setMessage(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.projectHours });
      void queryClient.invalidateQueries({ queryKey: queryKeys.timeEntries });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient],
  );

  const download = async () => {
    setExporting(true);
    try {
      const bytes = await projectHoursReport({
        rows,
        view,
        search: searched,
        generatedAt: new Date().toISOString(),
        generatedBy: session.status === 'signed-in' ? session.user.name : '',
        dates,
        t,
      });
      saveFile(
        new Blob([bytes], { type: xlsxType }),
        projectHoursReportName(dates.toDayValue(new Date()), t),
      );
    } catch {
      setMessage(t('projectHours.downloadFailed'));
    } finally {
      setExporting(false);
    }
  };

  const columns = useMemo<DataTableColumn<ProjectHours>[]>(
    () => [
      {
        field: 'name',
        headerName: t('projectHours.columns.project'),
        flex: 1,
        minWidth: 172,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0 }}>
            <Box component="span" sx={{ ...ellipsisSx, display: 'block', fontWeight: 500 }}>
              {row.name}
            </Box>
            <Box
              component="span"
              sx={{
                ...ellipsisSx,
                display: 'block',
                fontSize: 12,
                lineHeight: '16px',
                color: 'var(--text-secondary)',
              }}
            >
              <Box component="span" sx={{ fontFamily: 'var(--font-mono)' }}>
                {row.code}
              </Box>
              {t('projectHours.clientSuffix', {
                client: row.clientName ?? t('projectHours.internal'),
              })}
            </Box>
          </Box>
        ),
      },
      {
        field: 'status',
        headerName: t('projectHours.columns.status'),
        width: 132,
        renderCell: ({ row }) => <ProjectStatusBadge status={row.status} />,
      },
      {
        field: 'contracted',
        headerName: t('projectHours.columns.contracted'),
        width: 100,
        align: 'right',
        headerAlign: 'right',
        renderCell: ({ row }) => (
          <Box component="span" sx={row.balance.contracted === null ? mutedSx : numberSx}>
            {hoursOrDash(row.balance.contracted, t)}
          </Box>
        ),
      },
      {
        field: 'additional',
        headerName: t('projectHours.columns.additional'),
        width: 92,
        align: 'right',
        headerAlign: 'right',
        renderCell: ({ row }) => (
          <Box component="span" sx={row.balance.additional === 0 ? mutedSx : numberSx}>
            {hoursOrDash(row.balance.additional, t)}
          </Box>
        ),
      },
      {
        field: 'logged',
        headerName: t('projectHours.columns.logged'),
        width: 96,
        align: 'right',
        headerAlign: 'right',
        renderCell: ({ row }) => (
          <Box component="span" sx={numberSx}>
            {hoursOrDash(row.balance.logged, t)}
          </Box>
        ),
      },
      {
        field: 'balance',
        headerName: t('projectHours.columns.balance'),
        width: 184,
        renderCell: ({ row }) => <HoursBalanceBadge balance={row.balance} />,
      },
      {
        field: 'consumption',
        headerName: t('projectHours.columns.consumption'),
        width: 136,
        renderCell: ({ row }) =>
          row.balance.consumption === null ? (
            <Box component="span" sx={mutedSx}>
              {t('projectHours.none')}
            </Box>
          ) : (
            <ProgressBar
              value={row.balance.consumption}
              valueText={`${String(row.balance.consumption)}%`}
              label={t('projectHours.consumptionLabel')}
              tone={balanceLook[row.balance.state].progress}
              width="100%"
            />
          ),
      },
    ],
    [t],
  );

  return (
    <Page
      subtitle={data === undefined ? undefined : projectHoursMeta(rows, view, t)}
      actions={
        <Button
          variant="outlined"
          startIcon={<DownloadOutlined />}
          disabled={exporting || rows.length === 0}
          onClick={() => download()}
        >
          {t('projectHours.download')}
        </Button>
      }
    >
      {error === null ? (
        <DataTable
          label={t('projectHours.table')}
          rows={shown}
          columns={columns}
          rowCount={rows.length}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          loading={isFetching}
          rowHeight={56}
          emptyLabel={t('projectHours.empty')}
          selectedRowId={chosen}
          onRowChoose={choose}
          onRowHover={hover}
          getRowId={getRowId}
          tools={
            <>
              <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
                <TextField
                  label={t('projectHours.search')}
                  hideLabel
                  placeholder={t('projectHours.search')}
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
              <Box sx={{ flex: 1 }} />
              <SegmentedControl<ProjectHoursView>
                label={t('projectHours.viewsLabel')}
                value={view}
                options={[
                  { value: 'Open', label: t('projectHours.views.Open') },
                  { value: 'Closed', label: t('projectHours.views.Closed') },
                  { value: 'All', label: t('projectHours.views.All') },
                ]}
                onChange={(value) => {
                  setView(value);
                  setPage(1);
                }}
              />
            </>
          }
        />
      ) : (
        <Alert tone="danger">{t('projectHours.loadFailed')}</Alert>
      )}
      <ProjectHoursDrawer projectId={chosen} onClose={close} onChanged={changed} />
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
