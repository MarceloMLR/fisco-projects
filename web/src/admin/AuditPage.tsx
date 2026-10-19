import Box from '@mui/material/Box';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type AuditEntry, type AuditItemType, auditItemTypes } from '../api/client';
import { auditQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Select } from '../components/Select';
import { useDates } from '../format/useDates';
import { Page } from '../shell/Page';
import { AuditDetails } from './AuditDetails';
import { describeAuditEntry } from './auditText';
import { type Period, PeriodFilter } from './PeriodFilter';
import { type PersonOption, PersonFilter } from './PersonFilter';

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
 * the drawer over the screen (ADR-0081), so the list keeps its whole width.
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
              <Avatar name={name} size="sm" />
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
                width={160}
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
