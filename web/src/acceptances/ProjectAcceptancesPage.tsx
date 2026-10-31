import AddOutlined from '@mui/icons-material/AddOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import type { Acceptance, AcceptanceStatus } from '../api/client';
import { acceptancesQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { formatDayMonthText } from '../format/dates';
import { useDates } from '../format/useDates';
import { useProjectOutlet } from '../projects/projectOutlet';
import { theme } from '../theme/theme';
import { AcceptanceDialog } from './AcceptanceDialog';
import { AcceptanceDrawer } from './AcceptanceDrawer';
import { useSession } from '../session/SessionContext';
import { managesTerms, signersSummary } from './acceptances';
import { AcceptanceStatusBadge } from './AcceptanceStatusBadge';
import { Button } from '../components/Button';

/** A project has a few dozen terms: they all fit in one page of the free Data Grid (ADR-0050). */
const pageSize = 100;
const all = 'All';
type Filter = AcceptanceStatus | typeof all;
const filters: readonly Filter[] = [all, 'Pending', 'Signed', 'Refused'];

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const subSx = {
  ...ellipsisSx,
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-1)',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;

const getRowId = (row: Acceptance) => row.id;
const noPaging = () => undefined;

/**
 * The tab Termos de aceite of a project (canvas ProjetoTermos, ADR-0129), only for the Administrator and the manager:
 * the terms, newest first, with the term and its milestone, the signers ("2 de 3 assinaram"), when it was asked for and
 * the situation; the situations with their counts, "Anexar termo assinado" and, with a provider of signatures,
 * "Pedir assinatura". A term opens in the drawer (ADR-0081), also from the address of a notice (?term=).
 */
export function ProjectAcceptancesPage() {
  const { t } = useTranslation();
  const { formatDate } = useDates();
  const { project, notify } = useProjectOutlet();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [params, setParams] = useSearchParams();
  const [filter, setFilter] = useState<Filter>(all);
  const [dialog, setDialog] = useState<'request' | 'attach'>();
  const { session } = useSession();
  const manages = managesTerms(project, session.status === 'signed-in' ? session.user : undefined);
  const { data, isFetching, error } = useQuery({
    ...acceptancesQuery(project.id, filter === all ? {} : { status: filter }),
    placeholderData: keepPreviousData,
    enabled: manages,
  });
  const chosen = params.get('term') ?? undefined;

  // The drawer of a term is in the address, for the links of the notices, but not in the browser history: opening and
  // closing it replace the address, so Back leaves the tab at once (ADR-0105).
  const choose = useCallback(
    (row: Acceptance) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          next.set('term', row.id);
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
        next.delete('term');
        return next;
      },
      { replace: true },
    );
  }, [setParams]);

  const changed = useCallback(
    (message: string) => {
      notify(message);
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.acceptances, project.id] });
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.documents, project.id] });
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.projects, project.id] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [notify, project.id, queryClient],
  );

  const columns = useMemo<DataTableColumn<Acceptance>[]>(() => {
    const term: DataTableColumn<Acceptance> = {
      field: 'title',
      headerName: t('acceptances.columns.term'),
      flex: 1.6,
      minWidth: desktop ? 220 : 160,
      renderCell: ({ row }) => (
        <Box sx={{ minWidth: 0 }}>
          <Box component="span" sx={{ ...ellipsisSx, display: 'block', fontWeight: 500 }}>
            {row.title}
          </Box>
          <Box component="span" sx={subSx}>
            <FlagOutlined aria-hidden sx={{ fontSize: 14, flex: 'none' }} />
            <Box component="span" sx={ellipsisSx}>
              {row.milestone === null
                ? t('acceptances.milestoneRemoved')
                : row.milestone.day === null
                  ? row.milestone.name
                  : t('acceptances.milestone', {
                      name: row.milestone.name,
                      day: formatDayMonthText(row.milestone.day),
                    })}
            </Box>
          </Box>
        </Box>
      ),
    };
    const status: DataTableColumn<Acceptance> = {
      field: 'status',
      headerName: t('acceptances.columns.status'),
      width: desktop ? 170 : 150,
      renderCell: ({ row }) => <AcceptanceStatusBadge status={row.status} />,
    };
    if (!desktop) {
      return [term, status];
    }
    return [
      term,
      {
        field: 'signers',
        headerName: t('acceptances.columns.signers'),
        flex: 1,
        minWidth: 190,
        renderCell: ({ row }) => {
          const summary = signersSummary(row, t);
          return (
            <Box sx={{ minWidth: 0 }}>
              <Box component="span" sx={{ ...ellipsisSx, display: 'block' }}>
                {summary.main}
              </Box>
              <Box component="span" sx={{ ...subSx, display: 'block' }}>
                {summary.detail}
              </Box>
            </Box>
          );
        },
      },
      {
        field: 'requested',
        headerName: t('acceptances.columns.requestedAt'),
        width: 130,
        renderCell: ({ row }) => formatDate(row.requested.at),
      },
      status,
    ];
  }, [desktop, formatDate, t]);

  const counts = data?.counts;
  const footer = useCallback(() => {
    const total = t('acceptances.total', { count: counts?.all ?? 0 });
    return counts === undefined || counts.pending === 0
      ? total
      : `${total} · ${t('acceptances.pending', { count: counts.pending })}`;
  }, [counts, t]);

  if (!manages) {
    return <Alert tone="info">{t('acceptances.onlyManagement')}</Alert>;
  }
  if (error !== null) {
    return <Alert tone="danger">{t('acceptances.loadFailed')}</Alert>;
  }

  const countOf = (value: Filter) =>
    counts === undefined
      ? undefined
      : value === all
        ? counts.all
        : value === 'Pending'
          ? counts.pending
          : value === 'Signed'
            ? counts.signed
            : counts.refused;
  const options = filters.map((value) => ({
    value,
    label: t(`acceptances.filters.${value}`),
    count: countOf(value),
  }));
  const canRequest = data?.canRequest === true;
  const canAttach = data?.canAttach === true;
  const emptyLabel =
    filter !== all
      ? t('acceptances.emptyStatus')
      : canRequest || !canAttach
        ? t('acceptances.empty')
        : t('acceptances.emptyNoProvider');

  // Without a provider of signatures, the terms are only attached (ADR-0129).
  const buttons = (block: boolean) => (
    <>
      {canAttach && (
        <Button
          variant="outlined"
          size="small"
          startIcon={<UploadOutlined />}
          onClick={() => {
            setDialog('attach');
          }}
          sx={block ? { width: '100%', flex: 'none' } : { flex: 'none' }}
        >
          {t('acceptances.attach')}
        </Button>
      )}
      {canRequest && (
        <Button
          variant="contained"
          size="small"
          startIcon={<AddOutlined />}
          onClick={() => {
            setDialog('request');
          }}
          sx={block ? { width: '100%', flex: 'none' } : { flex: 'none' }}
        >
          {t('acceptances.request')}
        </Button>
      )}
    </>
  );

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
      <DataTable<Acceptance>
        label={t('acceptances.table')}
        rows={data?.items ?? []}
        columns={columns}
        rowCount={data?.items.length ?? 0}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={isFetching}
        rowHeight={60}
        emptyLabel={emptyLabel}
        displayedRowsLabel={footer}
        getRowId={getRowId}
        selectedRowId={chosen}
        onRowChoose={choose}
        tools={
          <>
            {desktop ? (
              <SegmentedControl<Filter>
                label={t('acceptances.filter')}
                value={filter}
                options={options}
                onChange={setFilter}
              />
            ) : (
              <Box sx={{ flex: '1 1 100%', minWidth: 0 }}>
                <Select<Filter>
                  label={t('acceptances.filter')}
                  size="sm"
                  value={filter}
                  options={options.map(({ value, label, count }) => ({
                    value,
                    label: count === undefined ? label : `${label} (${String(count)})`,
                  }))}
                  onChange={setFilter}
                />
              </Box>
            )}
            {desktop && <Box sx={{ flex: 1 }} />}
            {desktop && buttons(false)}
          </>
        }
      />
      {!desktop && buttons(true)}
      <AcceptanceDrawer project={project} termId={chosen} onClose={close} onChanged={changed} />
      {dialog !== undefined && (
        <AcceptanceDialog
          projectId={project.id}
          mode={dialog}
          onClose={() => {
            setDialog(undefined);
          }}
          onSaved={(message, id) => {
            setDialog(undefined);
            changed(message);
            if (id !== undefined && dialog === 'request') {
              setFilter(all);
            }
          }}
        />
      )}
    </Box>
  );
}
