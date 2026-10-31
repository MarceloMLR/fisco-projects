import DownloadOutlined from '@mui/icons-material/DownloadOutlined';
import Box from '@mui/material/Box';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  api,
  type AuditEntry,
  type AuditItemType,
  auditItemTypes,
  auditProblems,
} from '../api/client';
import { auditQuery, queryKeys } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { CardAvatar } from '../components/PersonCard';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Select } from '../components/Select';
import { saveFile } from '../format/saveFile';
import { useDates } from '../format/useDates';
import { xlsxType } from '../format/xlsx';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { problemField } from '../tickets/ticketChanges';
import { AuditDetails } from './AuditDetails';
import { auditReport, type AuditReportFilters, auditReportName } from './auditReport';
import { describeAuditEntry } from './auditText';
import { type Period, PeriodFilter } from './PeriodFilter';
import { type PersonOption, PersonFilter } from './PersonFilter';
import { Button } from '../components/Button';
import { useNotify } from '../notices/notify';

const pageSize = 25;
const defaultDays = 30;
const count = new Intl.NumberFormat('pt-BR');

type ItemTypeFilter = 'all' | AuditItemType;
const noEntries = { items: [], total: 0 };

const sub = {
  display: 'block',
  marginTop: '2px',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

const ellipsis = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

/**
 * Audit of the Administration, as in the canvas (ADR-0039): the records newest first, with filters by person, item
 * type and period (the last 30 days to start) and the total in the period. Choosing a record opens its details in
 * the drawer over the screen (ADR-0081), so the list keeps its whole width. "Baixar planilha" saves the records of the
 * filters in a spreadsheet, and the export becomes a record of the audit (ADR-0099).
 */
export function AuditPage() {
  const { t } = useTranslation();
  const dates = useDates();
  const { startOfDayValue, nextDayValue } = dates;
  const [person, setPerson] = useState<PersonOption | null>(null);
  const [itemType, setItemType] = useState<ItemTypeFilter>('all');
  // The shell shows the screens only once the time zone is known, so the days are already of the installation.
  const [period, setPeriod] = useState<Period>(() => dates.lastDays(defaultDays));
  const [page, setPage] = useState(1);
  // The record whose details are open; kept even if the list behind it is brought up to date.
  const [chosen, setChosen] = useState<AuditEntry>();
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [exporting, setExporting] = useState(false);
  const notify = useNotify();
  const { session } = useSession();
  const queryClient = useQueryClient();

  // A filter already seen in the session shows at once; another keeps the rows on screen until it arrives.
  const { data, isFetching, isError } = useQuery({
    ...auditQuery({
      actorId: person?.id,
      itemType: itemType === 'all' ? undefined : itemType,
      from: startOfDayValue(period.from).toISOString(),
      to: startOfDayValue(nextDayValue(period.to)).toISOString(),
      page,
      pageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const result = data ?? noEntries;

  // The details open only when the administrator chooses a record (ADR-0081).
  const choose = useCallback((entry: AuditEntry) => {
    setChosen(entry);
    setDetailsOpen(true);
  }, []);
  const closeDetails = useCallback(() => {
    setDetailsOpen(false);
  }, []);

  const filtered = useCallback(<T,>(set: (value: T) => void) => {
    return (value: T) => {
      set(value);
      setPage(1);
    };
  }, []);

  const columns = useMemo<DataTableColumn<AuditEntry>[]>(
    () => [
      {
        field: 'occurredAt',
        headerName: t('audit.columns.when'),
        width: 96,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0, fontVariantNumeric: 'tabular-nums' }}>
            <Box component="span" sx={{ display: 'block' }}>
              {dates.formatDayMonth(row.occurredAt)}
            </Box>
            <Box component="span" sx={sub}>
              {dates.formatTime(row.occurredAt)}
            </Box>
          </Box>
        ),
      },
      {
        field: 'actor',
        headerName: t('audit.columns.who'),
        width: 172,
        renderCell: ({ row }) => {
          const name = row.actor?.name ?? undefined;
          return (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                minWidth: 0,
                color: name === undefined ? 'var(--text-secondary)' : 'inherit',
              }}
            >
              {row.actor === null || name === undefined ? (
                <Avatar size="sm" />
              ) : (
                <CardAvatar person={{ id: row.actor.id, name }} size="sm" />
              )}
              <Box component="span" sx={ellipsis}>
                {name ?? t('audit.system')}
              </Box>
            </Box>
          );
        },
      },
      {
        field: 'action',
        headerName: t('audit.columns.what'),
        flex: 1,
        minWidth: 180,
        renderCell: ({ row }) => {
          const { title, summary } = describeAuditEntry(row, t);
          return (
            <Box sx={{ minWidth: 0 }}>
              <Box component="span" sx={{ ...ellipsis, display: 'block', fontWeight: 500 }}>
                {title}
              </Box>
              {summary !== '' && (
                <Box component="span" sx={sub}>
                  {summary}
                </Box>
              )}
            </Box>
          );
        },
      },
    ],
    [t, dates],
  );

  // The records of the filters of the screen, in a spreadsheet made here, with the texts of the list (ADR-0099).
  const download = async () => {
    setExporting(true);
    try {
      const filters: AuditReportFilters = {
        from: period.from,
        to: period.to,
        itemType: itemType === 'all' ? undefined : itemType,
        person: person?.name,
      };
      const { data: exported, error } = await api.POST('/api/audit/exports', {
        body: {
          from: period.from,
          to: period.to,
          itemType: filters.itemType ?? null,
          actorId: person?.id ?? null,
        },
      });
      if (exported === undefined) {
        const total = problemField(error, 'total');
        const limit = problemField(error, 'limit');
        notify(
          problemField(error, 'type') === auditProblems.exportTooLarge &&
            typeof total === 'number' &&
            typeof limit === 'number'
            ? t('audit.export.tooMany', { total: count.format(total), limit: count.format(limit) })
            : t('audit.export.failed'),
          { tone: 'error' },
        );
        return;
      }
      const bytes = await auditReport({
        entries: exported.items,
        filters,
        generatedAt: new Date().toISOString(),
        generatedBy: session.status === 'signed-in' ? session.user.name : '',
        dates,
        t,
      });
      saveFile(new Blob([bytes], { type: xlsxType }), auditReportName(filters, t));
      // The export is a record of the audit now.
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    } catch {
      notify(t('audit.export.failed'), { tone: 'error' });
    } finally {
      setExporting(false);
    }
  };

  const displayedRows = useCallback(
    ({ from, to, count: total }: { from: number; to: number; count: number }) =>
      t('audit.displayedRows', { from, to, total: count.format(total) }),
    [t],
  );

  return (
    <>
      <Page>
        <DataTable
          label={t('audit.table')}
          rows={result.items}
          columns={columns}
          rowCount={result.total}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          // Any wait, a new filter or a refresh of the rows on screen, shows the bar if it lasts (DataTable).
          loading={isFetching}
          emptyLabel={t(isError ? 'audit.loadFailed' : 'audit.empty')}
          selectedRowId={detailsOpen ? chosen?.id : undefined}
          onRowChoose={choose}
          displayedRowsLabel={displayedRows}
          tools={
            <>
              <PersonFilter value={person} onChange={filtered(setPerson)} width={240} />
              <Select<ItemTypeFilter>
                label={t('audit.filters.type')}
                size="sm"
                width={200}
                value={itemType}
                options={[
                  { value: 'all', label: t('audit.filters.allTypes') },
                  ...auditItemTypes.map((value) => ({
                    value,
                    label: t(`audit.itemTypes.${value}`),
                  })),
                ]}
                onChange={filtered(setItemType)}
              />
              <PeriodFilter value={period} onChange={filtered(setPeriod)} width={172} />
              <Button
                variant="outlined"
                size="small"
                startIcon={<DownloadOutlined />}
                loading={exporting}
                onClick={() => download()}
              >
                {t('audit.export.button')}
              </Button>
            </>
          }
        />
      </Page>
      <DetailsDrawer open={detailsOpen} onClose={closeDetails} label={t('audit.details.label')}>
        {chosen !== undefined && <AuditDetails entry={chosen} />}
      </DetailsDrawer>
    </>
  );
}
