import ForumOutlined from '@mui/icons-material/ForumOutlined';
import Box from '@mui/material/Box';
import { type ReactNode, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import type { TicketRow } from '../api/client';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { useDates } from '../format/useDates';
import { PriorityTag, TicketPersonTag, TicketStatusBadge } from './TicketTags';

export const ticketPageSize = 25;

const secondarySx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

/**
 * The table of tickets (canvas Chamados and ProjetoChamados, fp-gt): the number, the title with the project, the team
 * and who opened it, the status, the assignee and the priority. In the tab of a project, the line under the title
 * leaves the project out and says the day it was opened. The deadline, the selection and the interactions join it with
 * the changes that bring them.
 */
export function TicketTable({
  rows,
  total,
  page,
  onPageChange,
  loading,
  inProject = false,
  emptyLabel,
  tools,
}: {
  rows: readonly TicketRow[];
  total: number;
  page: number;
  onPageChange: (page: number) => void;
  loading: boolean;
  inProject?: boolean;
  emptyLabel: string;
  tools?: ReactNode;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { formatDayMonth } = useDates();

  const columns = useMemo<DataTableColumn<TicketRow>[]>(
    () => [
      {
        field: 'number',
        headerName: t('tickets.columns.number'),
        width: 76,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              fontWeight: 500,
              color: 'var(--text-secondary)',
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {t('tickets.number', { number: row.number })}
          </Box>
        ),
      },
      {
        field: 'title',
        headerName: t('tickets.columns.title'),
        flex: 2,
        minWidth: 260,
        renderCell: ({ row }) => {
          const team = t(`tickets.teams.${row.team}`);
          return (
            <Box sx={{ minWidth: 0 }}>
              <Box
                component={Link}
                to={`/tickets/${String(row.number)}`}
                sx={{
                  ...ellipsisSx,
                  display: 'block',
                  fontWeight: 500,
                  color: 'var(--text)',
                  textDecoration: 'none',
                  '&:hover': { textDecoration: 'underline' },
                }}
              >
                {row.title}
              </Box>
              <Box
                component="span"
                sx={{ ...secondarySx, ...ellipsisSx, display: 'block', marginTop: '2px' }}
              >
                {inProject ? (
                  t('tickets.projectRow', {
                    team,
                    name: row.requester.name,
                    day: formatDayMonth(row.openedAt),
                  })
                ) : (
                  <>
                    <Box
                      component="span"
                      sx={{ fontFamily: 'var(--font-mono)', fontSize: 12, fontWeight: 500 }}
                    >
                      {row.project.code}
                    </Box>
                    {` · ${t('tickets.row', { team, name: row.requester.name })}`}
                  </>
                )}
              </Box>
            </Box>
          );
        },
      },
      {
        field: 'status',
        headerName: t('tickets.columns.status'),
        width: 196,
        renderCell: ({ row }) => <TicketStatusBadge status={row.status} />,
      },
      {
        field: 'assignee',
        headerName: t('tickets.columns.assignee'),
        width: 184,
        renderCell: ({ row }) => <TicketPersonTag name={row.assignee?.name} />,
      },
      {
        field: 'priority',
        headerName: t('tickets.columns.priority'),
        width: 112,
        renderCell: ({ row }) => <PriorityTag priority={row.priority} />,
      },
      {
        field: 'interactions',
        headerName: t('tickets.columns.interactions'),
        width: 60,
        align: 'right',
        headerAlign: 'right',
        renderHeader: () => (
          <ForumOutlined
            aria-label={t('tickets.columns.interactions')}
            titleAccess={t('tickets.columns.interactions')}
            sx={{ fontSize: 18, color: 'var(--text-secondary)' }}
          />
        ),
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{ color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}
          >
            {row.interactions}
          </Box>
        ),
      },
    ],
    [t, inProject, formatDayMonth],
  );

  const choose = useCallback(
    (row: TicketRow) => {
      void navigate(`/tickets/${String(row.number)}`);
    },
    [navigate],
  );

  // "9 chamados"; with more than a page, the tickets shown come first: "1–25 de 30 chamados".
  const footer = useCallback(
    ({ from, to, count }: { from: number; to: number; count: number }) => {
      const all = t('tickets.total', { count });
      return count > ticketPageSize ? t('tickets.range', { from, to, total: all }) : all;
    },
    [t],
  );

  return (
    <DataTable
      label={t('tickets.table')}
      rows={rows}
      columns={columns}
      rowCount={total}
      page={page}
      pageSize={ticketPageSize}
      onPageChange={onPageChange}
      loading={loading}
      rowHeight={60}
      emptyLabel={emptyLabel}
      displayedRowsLabel={footer}
      onRowChoose={choose}
      tools={tools}
    />
  );
}
