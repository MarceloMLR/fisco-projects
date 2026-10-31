import AssignmentIndOutlined from '@mui/icons-material/AssignmentIndOutlined';
import ForumOutlined from '@mui/icons-material/ForumOutlined';
import PersonOutlined from '@mui/icons-material/PersonOutlined';
import Box from '@mui/material/Box';
import { useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, type ReactNode, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import { prefetchTicket } from '../api/prefetch';
import type { TicketRow } from '../api/client';
import { Checkbox } from '../components/Checkbox';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { useDates } from '../format/useDates';
import { PriorityTag, TicketPersonTag, TicketStatusBadge } from './TicketTags';
import { TicketDue } from './TicketDue';
import { PeopleChangeDialog } from './TicketPeopleDialogs';
import type { PeopleChange } from './ticketChanges';
import { Button } from '../components/Button';
import { useNotify } from '../notices/notify';

export const ticketPageSize = 25;

/** A row the person may change in bulk: the assignee or the requester (ADR-0027). */
const isSelectable = (row: TicketRow) => row.canChangeAssignee || row.canChangeRequester;

/** A click on a control of a row that must not open the ticket. */
const keepRow = (event: MouseEvent) => {
  event.stopPropagation();
};

const linkButtonSx = {
  padding: 0,
  marginLeft: '32px',
  border: 0,
  background: 'none',
  font: 'inherit',
  fontSize: 13,
  lineHeight: '18px',
  color: 'var(--action-text)',
  cursor: 'pointer',
  '&:hover': { textDecoration: 'underline' },
  '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
} as const;

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
  // The marked tickets, by number, kept across the pages until the change or "Limpar seleção" (canvas Chamados).
  const [marked, setMarked] = useState<ReadonlyMap<number, TicketRow>>(new Map());
  const [changing, setChanging] = useState<{ kind: PeopleChange; numbers: number[] }>();
  const notify = useNotify();
  const selectable = rows.filter(isSelectable);
  const allMarked = selectable.length > 0 && selectable.every((row) => marked.has(row.number));

  const mark = useCallback((row: TicketRow, checked: boolean) => {
    setMarked((current) => {
      const next = new Map(current);
      if (checked) {
        next.set(row.number, row);
      } else {
        next.delete(row.number);
      }
      return next;
    });
  }, []);

  const markPage = useCallback(
    (checked: boolean) => {
      setMarked((current) => {
        const next = new Map(current);
        for (const row of rows.filter(isSelectable)) {
          if (checked) {
            next.set(row.number, row);
          } else {
            next.delete(row.number);
          }
        }
        return next;
      });
    },
    [rows],
  );

  // The vendor of the ticket in the second line of the title (ADR-0028).
  const atVendor = useCallback(
    (row: TicketRow) =>
      row.vendor === null ? '' : ` · ${t('tickets.atVendor', { vendor: row.vendor.vendor })}`,
    [t],
  );

  const columns = useMemo<DataTableColumn<TicketRow>[]>(
    () => [
      ...(rows.some(isSelectable)
        ? [
            {
              field: 'select',
              width: 40,
              sortable: false,
              renderHeader: () => (
                <Box onClick={keepRow} sx={{ display: 'flex' }}>
                  <Checkbox
                    label={t('tickets.select.all')}
                    hideLabel
                    checked={allMarked}
                    onChange={markPage}
                  />
                </Box>
              ),
              renderCell: ({ row }: { row: TicketRow }) =>
                isSelectable(row) ? (
                  <Box onClick={keepRow} sx={{ display: 'flex' }}>
                    <Checkbox
                      label={t('tickets.select.one', { number: row.number })}
                      hideLabel
                      checked={marked.has(row.number)}
                      onChange={(checked) => {
                        mark(row, checked);
                      }}
                    />
                  </Box>
                ) : null,
            } satisfies DataTableColumn<TicketRow>,
          ]
        : []),
      {
        field: 'number',
        headerName: t('tickets.columns.number'),
        width: 64,
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
        minWidth: 232,
        renderCell: ({ row }) => {
          // The vendor right after the team, so a long name of the requester does not hide it (ADR-0028).
          const team = t(`tickets.teams.${row.team}`) + atVendor(row);
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
        headerName: t('tickets.columns.statusAndDue'),
        width: 184,
        // The status with the deadline of the resolution under it, while it is open (canvas Chamados, ADR-0025).
        renderCell: ({ row }) => (
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'flex-start',
              justifyContent: 'center',
              gap: '2px',
              height: '100%',
            }}
          >
            <TicketStatusBadge status={row.status} />
            {row.resolution !== null && row.status !== 'Resolved' && row.status !== 'Closed' && (
              <TicketDue due={row.resolution} />
            )}
          </Box>
        ),
      },
      {
        field: 'assignee',
        headerName: t('tickets.columns.assignee'),
        width: 168,
        renderCell: ({ row }) =>
          row.assignee === null && row.canChangeAssignee ? (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'flex-start',
                gap: '2px',
              }}
            >
              <TicketPersonTag person={null} />
              <Box
                component="button"
                type="button"
                onClick={(event: MouseEvent) => {
                  keepRow(event);
                  setChanging({ kind: 'assignee', numbers: [row.number] });
                }}
                sx={linkButtonSx}
              >
                {t('tickets.assignLink')}
              </Box>
            </Box>
          ) : (
            <TicketPersonTag person={row.assignee} />
          ),
      },
      {
        field: 'priority',
        headerName: t('tickets.columns.priority'),
        width: 92,
        renderCell: ({ row }) => <PriorityTag priority={row.priority} />,
      },
      {
        field: 'interactions',
        headerName: t('tickets.columns.interactions'),
        width: 52,
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
    [t, inProject, formatDayMonth, rows, allMarked, marked, mark, markPage, atVendor],
  );

  const chosen = [...marked.values()];
  const selectionActions =
    chosen.length === 0
      ? undefined
      : {
          label: t('tickets.bulk.label'),
          content: (
            <>
              <Box component="span" sx={{ fontWeight: 500 }}>
                {t('tickets.bulk.selected', { count: chosen.length })}
              </Box>
              <Button
                variant="outlined"
                size="small"
                startIcon={<AssignmentIndOutlined />}
                disabled={!chosen.every((row) => row.canChangeAssignee)}
                onClick={() => {
                  setChanging({ kind: 'assignee', numbers: chosen.map((row) => row.number) });
                }}
              >
                {t('tickets.bulk.assign')}
              </Button>
              <Button
                variant="outlined"
                size="small"
                startIcon={<PersonOutlined />}
                disabled={!chosen.every((row) => row.canChangeRequester)}
                onClick={() => {
                  setChanging({ kind: 'requester', numbers: chosen.map((row) => row.number) });
                }}
              >
                {t('tickets.bulk.requester')}
              </Button>
              <Box sx={{ flex: 1 }} />
              <Button
                variant="text"
                size="small"
                onClick={() => {
                  setMarked(new Map());
                }}
              >
                {t('tickets.bulk.clear')}
              </Button>
            </>
          ),
        };

  const choose = useCallback(
    (row: TicketRow) => {
      void navigate(`/tickets/${String(row.number)}`);
    },
    [navigate],
  );
  // The ticket starts loading when the mouse comes over its row (ADR-0106).
  const queryClient = useQueryClient();
  const hover = useCallback(
    (row: TicketRow) => {
      prefetchTicket(queryClient, row.number);
    },
    [queryClient],
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
    <>
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
        onRowHover={hover}
        tools={tools}
        selectionActions={selectionActions}
      />
      {changing !== undefined && (
        <PeopleChangeDialog
          kind={changing.kind}
          numbers={changing.numbers}
          onClose={() => {
            setChanging(undefined);
          }}
          onDone={(text) => {
            setChanging(undefined);
            setMarked(new Map());
            notify(text);
          }}
        />
      )}
    </>
  );
}
