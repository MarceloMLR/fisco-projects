import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import IconButton from '@mui/material/IconButton';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type AuditEntry, type AuditItemType, auditItemTypes } from '../api/client';
import { auditQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Select } from '../components/Select';
import {
  formatDayMonth,
  formatTime,
  lastDays,
  nextDayValue,
  startOfDayValue,
} from '../format/dates';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
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
 * type and period (the last 30 days to start), the total in the period and the details of the chosen record in
 * the side pane, which opens as a drawer on narrower screens.
 */
export function AuditPage() {
  const { t } = useTranslation();
  const wide = useMediaQuery(theme.breakpoints.up('lg'), { noSsr: true });
  const [person, setPerson] = useState<PersonOption | null>(null);
  const [itemType, setItemType] = useState<ItemTypeFilter>('all');
  const [period, setPeriod] = useState<Period>(() => lastDays(defaultDays));
  const [page, setPage] = useState(1);
  const [chosenId, setChosenId] = useState<string>();
  const [drawerOpen, setDrawerOpen] = useState(false);

  // A filter already seen in the session shows at once; another keeps the rows on screen until it arrives.
  const { data, isFetching, isPlaceholderData, isError } = useQuery({
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
  const loading = isFetching && (data === undefined || isPlaceholderData);

  // The chosen record; on a wide screen the first one of the page until the administrator chooses another.
  const selected =
    result.items.find((entry) => entry.id === chosenId) ?? (wide ? result.items[0] : undefined);

  const choose = useCallback((entry: AuditEntry) => {
    setChosenId(entry.id);
    setDrawerOpen(true);
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
              {formatDayMonth(row.occurredAt)}
            </Box>
            <Box component="span" sx={sub}>
              {formatTime(row.occurredAt)}
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
    [t],
  );

  const displayedRows = useCallback(
    ({ from, to, count: total }: { from: number; to: number; count: number }) =>
      t('audit.displayedRows', { from, to, total: count.format(total) }),
    [t],
  );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: wide ? 'minmax(0, 1fr) 340px' : 'minmax(0, 1fr)',
        gridTemplateRows: 'minmax(0, 1fr)',
      }}
    >
      <Page>
        <DataTable
          label={t('audit.table')}
          rows={result.items}
          columns={columns}
          rowCount={result.total}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          loading={loading}
          emptyLabel={t(isError ? 'audit.loadFailed' : 'audit.empty')}
          selectedRowId={selected?.id}
          onRowChoose={choose}
          displayedRowsLabel={displayedRows}
          tools={
            <>
              <PersonFilter value={person} onChange={filtered(setPerson)} width={180} />
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
      {wide ? (
        <Box
          component="aside"
          aria-label={t('audit.details.label')}
          sx={{
            minHeight: 0,
            boxSizing: 'border-box',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-5)',
            padding: 'var(--space-5) var(--space-5) var(--space-6)',
            backgroundColor: 'var(--bg-subtle)',
            borderLeft: '1px solid var(--border)',
            overflow: 'auto',
          }}
        >
          {selected !== undefined && <AuditDetails entry={selected} />}
        </Box>
      ) : (
        <Drawer
          anchor="right"
          open={drawerOpen && selected !== undefined}
          onClose={() => {
            setDrawerOpen(false);
          }}
          slotProps={{
            paper: {
              role: 'dialog',
              'aria-label': t('audit.details.label'),
              sx: {
                width: 'min(100%, 360px)',
                boxSizing: 'border-box',
                display: 'flex',
                flexDirection: 'column',
                gap: 'var(--space-5)',
                padding: 'var(--space-4) var(--space-5) var(--space-6)',
                backgroundColor: 'var(--bg-subtle)',
              },
            },
          }}
        >
          <IconButton
            aria-label={t('audit.details.close')}
            onClick={() => {
              setDrawerOpen(false);
            }}
            sx={{ alignSelf: 'flex-end', color: 'var(--text-secondary)' }}
          >
            <CloseOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
          {selected !== undefined && <AuditDetails entry={selected} />}
        </Drawer>
      )}
    </Box>
  );
}
