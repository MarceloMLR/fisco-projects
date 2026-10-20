import ArrowForwardOutlined from '@mui/icons-material/ArrowForwardOutlined';
import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { TicketActor, TicketTransition } from '../api/client';
import { ticketFlowQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Page } from '../shell/Page';
import { TicketStatusBadge } from './TicketTags';

type Row = TicketTransition & { id: string };

const textSx = {
  margin: 0,
  fontSize: 14,
  lineHeight: '20px',
  color: 'var(--text-secondary)',
} as const;

function columnsOf(t: TFunction): DataTableColumn<Row>[] {
  return [
    {
      field: 'from',
      headerName: t('tickets.flow.columns.from'),
      width: 200,
      sortable: false,
      renderCell: ({ row }) => <TicketStatusBadge status={row.from} />,
    },
    {
      field: 'action',
      headerName: t('tickets.flow.columns.action'),
      flex: 1,
      minWidth: 200,
      sortable: false,
      valueGetter: (_value, row) => t(`tickets.actions.${row.action}`),
    },
    {
      field: 'to',
      headerName: t('tickets.flow.columns.to'),
      width: 200,
      sortable: false,
      renderCell: ({ row }) => <TicketStatusBadge status={row.to} />,
    },
    {
      field: 'actors',
      headerName: t('tickets.flow.columns.actors'),
      flex: 1,
      minWidth: 200,
      sortable: false,
      valueGetter: (_value, row) =>
        row.actors.map((actor) => t(`tickets.flow.actors.${actor as TicketActor}`)).join(', '),
    },
    {
      field: 'requiresMessage',
      headerName: t('tickets.flow.columns.message'),
      width: 140,
      sortable: false,
      valueGetter: (_value, row) =>
        t(row.requiresMessage ? 'tickets.flow.needsMessage' : 'tickets.flow.noMessage'),
    },
    {
      field: 'automatic',
      headerName: t('tickets.flow.columns.automatic'),
      width: 140,
      sortable: false,
      valueGetter: (_value, row) =>
        t(row.automatic ? 'tickets.flow.automatic' : 'tickets.flow.byButton'),
    },
  ];
}

const keepPage = () => undefined;

/**
 * Fluxo de chamados (ADR-0024): the statuses in their order and the transitions read from the table, each with the
 * action, who may take it, whether it asks for a message and whether the system takes it alone. No diagram: the
 * screen was not drawn.
 */
export function TicketFlowPage() {
  const { t } = useTranslation();
  const { data, error, isFetching } = useQuery(ticketFlowQuery());
  const columns = useMemo(() => columnsOf(t), [t]);
  const rows = useMemo<Row[]>(
    () =>
      (data?.transitions ?? []).map((transition) => ({
        ...transition,
        id: `${transition.from}-${transition.action}`,
      })),
    [data],
  );

  return (
    <Page>
      <Box component="p" sx={{ ...textSx, color: 'var(--text)' }}>
        {t('tickets.flow.intro')}
      </Box>
      {error !== null && <Alert tone="danger">{t('tickets.flow.loadFailed')}</Alert>}
      {data !== undefined && (
        <Box
          component="section"
          aria-labelledby="ticket-flow-statuses"
          sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}
        >
          <Box
            component="h2"
            id="ticket-flow-statuses"
            sx={{ margin: 0, fontSize: 14, lineHeight: '20px', fontWeight: 500 }}
          >
            {t('tickets.flow.statuses')}
          </Box>
          <Box
            component="ol"
            sx={{
              display: 'flex',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 'var(--space-2)',
              margin: 0,
              padding: 0,
              listStyle: 'none',
              '& svg[data-arrow]': { fontSize: 16, color: 'var(--text-secondary)' },
            }}
          >
            {data.statuses.map((status, index) => (
              <Box
                component="li"
                key={status}
                sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}
              >
                {index > 0 && <ArrowForwardOutlined aria-hidden data-arrow />}
                <TicketStatusBadge status={status} />
              </Box>
            ))}
          </Box>
          <Box component="p" sx={textSx}>
            {t('tickets.flow.attendantHelp')}{' '}
            {t('tickets.flow.autoCloseHelp', { count: data.autoCloseBusinessDays })}
          </Box>
        </Box>
      )}
      <DataTable
        label={t('tickets.flow.transitions')}
        rows={rows}
        columns={columns}
        rowCount={rows.length}
        page={1}
        pageSize={Math.max(rows.length, 1)}
        onPageChange={keepPage}
        loading={isFetching}
        displayedRowsLabel={({ count }) => t('tickets.flow.range', { count })}
      />
    </Page>
  );
}
