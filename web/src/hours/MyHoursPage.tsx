import AddOutlined from '@mui/icons-material/AddOutlined';
import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type TimeEntry, timeEntryProblems } from '../api/client';
import { queryKeys, timeEntriesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { CodeChip } from '../components/Card';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { type DayValue, formatDayMonthText, formatDayText } from '../format/dates';
import { useDates } from '../format/useDates';
import { problemType } from '../projects/projectForm';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import {
  addDays,
  formatHours,
  type HoursRow,
  hoursRows,
  isDayRow,
  weekEnd,
  weekStart,
  workOf,
} from './hours';
import { TimeEntryDialog } from './TimeEntryDialog';

/** A week has a few dozen entries: they all fit in one page of the free Data Grid (ADR-0050). */
const pageSize = 100;

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const secondarySx = { color: 'var(--text-secondary)' } as const;
const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

const getRowId = (row: HoursRow) => row.id;

/**
 * Minhas horas (canvas Horas do projeto, ADR-0128): the week from Monday to Sunday, the entries of the person grouped
 * by day, the newest first, with the total of each day and of the week; the week before and after and "Esta semana";
 * "Apontar horas", and "⋮" with "Corrigir" and "Apagar" on the entries the person may change. On the phone, each entry
 * shows the project, what was done and the hours.
 */
export function MyHoursPage() {
  const { t } = useTranslation();
  const { toDayValue, formatWeekday } = useDates();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const today = toDayValue(new Date());
  const thisWeek = weekStart(today);
  const [start, setStart] = useState<DayValue>(thisWeek);
  const end = weekEnd(start);
  const [menu, setMenu] = useState<{ anchor: HTMLElement; entry: TimeEntry }>();
  // An entry to correct, or null for a new one.
  const [editing, setEditing] = useState<TimeEntry | null>();
  const [removing, setRemoving] = useState<TimeEntry>();
  const [message, setMessage] = useState<string>();
  const { data, isFetching, error } = useQuery({
    ...timeEntriesQuery(start, end),
    placeholderData: keepPreviousData,
  });
  const items = data?.items;
  const rows = useMemo(() => hoursRows(items ?? []), [items]);
  const total = data?.total ?? 0;

  const dayLabel = useCallback(
    (day: DayValue) => {
      const weekday = formatWeekday(day);
      return `${weekday.charAt(0).toUpperCase()}${weekday.slice(1)}, ${formatDayMonthText(day)}`;
    },
    [formatWeekday],
  );

  const changed = useCallback(
    (text: string) => {
      setMessage(text);
      void queryClient.invalidateQueries({ queryKey: queryKeys.timeEntries });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient, setMessage],
  );

  const openMenu = useCallback(
    (event: MouseEvent<HTMLElement>, entry: TimeEntry) => {
      setMenu({ anchor: event.currentTarget, entry });
    },
    [setMenu],
  );

  const remove = async (entry: TimeEntry) => {
    setRemoving(undefined);
    try {
      const { response, error: problem } = await api.DELETE('/api/time-entries/{entryId}', {
        params: { path: { entryId: entry.id } },
      });
      changed(
        response.ok
          ? t('hours.messages.removed')
          : response.status === 409 && problemType(problem) === timeEntryProblems.projectClosed
            ? t('hours.messages.closed')
            : t('hours.messages.failed'),
      );
    } catch {
      setMessage(t('hours.messages.failed'));
    }
  };

  const columns = useMemo<DataTableColumn<HoursRow>[]>(() => {
    const workCell = (entry: TimeEntry) => {
      const work = workOf(entry);
      if (work.kind === 'project') {
        return (
          <Box component="span" sx={secondarySx}>
            {t('hours.onlyProject')}
          </Box>
        );
      }
      const Icon = work.kind === 'task' ? TaskAltOutlined : ConfirmationNumberOutlined;
      const text = work.kind === 'task' ? (work.name ?? t('hours.deletedTask')) : work.text;
      return (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
          <Icon aria-hidden sx={{ fontSize: 18, flex: 'none', color: 'var(--text-secondary)' }} />
          <Box
            component="span"
            sx={{
              ...ellipsisSx,
              ...(work.kind === 'task' && work.name === null ? secondarySx : {}),
            }}
          >
            {text}
          </Box>
        </Box>
      );
    };
    const first: DataTableColumn<HoursRow> = {
      field: 'project',
      headerName: desktop ? t('hours.columns.project') : t('hours.columns.description'),
      flex: desktop ? 1.1 : 1,
      minWidth: desktop ? 240 : 160,
      // A day spans the project, the work and the description (fp-group-row).
      colSpan: (_value, row) => (row.kind === 'day' && desktop ? 3 : undefined),
      renderCell: ({ row }) =>
        row.kind === 'day' ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
            {dayLabel(row.day)}
            {row.day === today && (
              <Box component="span" sx={{ ...secondarySx, fontWeight: 400 }}>
                {t('hours.today')}
              </Box>
            )}
          </Box>
        ) : desktop ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
            <CodeChip code={row.entry.project.code} />
            <Box component="span" sx={{ ...ellipsisSx, ...secondarySx }}>
              {row.entry.project.name}
            </Box>
          </Box>
        ) : (
          <Box sx={{ minWidth: 0 }}>
            <Box
              component="span"
              sx={{
                ...ellipsisSx,
                ...secondarySx,
                display: 'block',
                fontFamily: 'var(--font-mono)',
                fontSize: 12,
                lineHeight: '16px',
              }}
            >
              {row.entry.project.code}
            </Box>
            <Box component="span" sx={{ ...ellipsisSx, display: 'block' }}>
              {row.entry.description}
            </Box>
          </Box>
        ),
    };
    const hours: DataTableColumn<HoursRow> = {
      field: 'hours',
      headerName: t('hours.columns.hours'),
      width: desktop ? 96 : 72,
      align: 'right',
      headerAlign: 'right',
      renderCell: ({ row }) => (
        <Box
          component="span"
          sx={{ fontVariantNumeric: 'tabular-nums', fontWeight: row.kind === 'day' ? 500 : 400 }}
        >
          {formatHours(row.kind === 'day' ? row.total : row.entry.hours)}
        </Box>
      ),
    };
    const actions: DataTableColumn<HoursRow> = {
      field: 'actions',
      headerName: t('hours.columns.actions'),
      renderHeader: () => null,
      width: 56,
      align: 'right',
      renderCell: ({ row }) =>
        row.kind === 'entry' &&
        row.entry.canChange && (
          <IconButton
            size="small"
            aria-label={t('hours.menu.button', { day: formatDayText(row.entry.day) })}
            aria-haspopup="menu"
            onClick={(event) => {
              openMenu(event, row.entry);
            }}
            sx={iconButtonSx}
          >
            <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
        ),
    };
    if (!desktop) {
      return [first, hours, actions];
    }
    return [
      first,
      {
        field: 'work',
        headerName: t('hours.columns.work'),
        flex: 1.2,
        minWidth: 220,
        renderCell: ({ row }) => row.kind === 'entry' && workCell(row.entry),
      },
      {
        field: 'description',
        headerName: t('hours.columns.description'),
        flex: 1.4,
        minWidth: 220,
        renderCell: ({ row }) =>
          row.kind === 'entry' && (
            <Box component="span" sx={ellipsisSx} title={row.entry.description}>
              {row.entry.description}
            </Box>
          ),
      },
      hours,
      actions,
    ];
  }, [dayLabel, desktop, openMenu, t, today]);

  const footer = useCallback(() => t('hours.total', { hours: formatHours(total) }), [t, total]);

  const subtitle = t('hours.subtitle', {
    from: formatDayMonthText(start),
    to: formatDayText(end),
    hours: formatHours(total),
  });

  const log = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing(null);
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('hours.log')}
    </Button>
  );

  const weekButtons = (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
      <IconButton
        aria-label={t('hours.previousWeek')}
        onClick={() => {
          setStart(addDays(start, -7));
        }}
        sx={{
          border: '1px solid var(--border-control)',
          borderRadius: 'var(--radius-md)',
          width: 36,
          height: 36,
        }}
      >
        <ChevronLeftOutlined />
      </IconButton>
      {desktop ? (
        <Button
          variant="outlined"
          disabled={start === thisWeek}
          onClick={() => {
            setStart(thisWeek);
          }}
        >
          {t('hours.thisWeek')}
        </Button>
      ) : (
        <Box sx={{ flex: 1, textAlign: 'center' }}>
          <Box sx={{ fontWeight: 500 }}>
            {t('hours.weekShort', { from: formatDayMonthText(start), to: formatDayMonthText(end) })}
          </Box>
          <Box
            sx={{
              ...secondarySx,
              fontSize: 12,
              lineHeight: '16px',
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {t('hours.weekTotal', { hours: formatHours(total) })}
          </Box>
        </Box>
      )}
      <IconButton
        aria-label={t('hours.nextWeek')}
        disabled={start >= thisWeek}
        onClick={() => {
          setStart(addDays(start, 7));
        }}
        sx={{
          border: '1px solid var(--border-control)',
          borderRadius: 'var(--radius-md)',
          width: 36,
          height: 36,
        }}
      >
        <ChevronRightOutlined />
      </IconButton>
    </Box>
  );

  return (
    <Page
      subtitle={subtitle}
      actions={
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          {weekButtons}
          {log(false)}
        </Box>
      }
    >
      {!desktop && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          {weekButtons}
          {start !== thisWeek && (
            <Button
              variant="outlined"
              onClick={() => {
                setStart(thisWeek);
              }}
            >
              {t('hours.thisWeek')}
            </Button>
          )}
          {log(true)}
        </Box>
      )}
      {error === null ? (
        <DataTable<HoursRow>
          label={t('hours.table')}
          rows={rows}
          columns={columns}
          rowCount={rows.length}
          page={1}
          pageSize={pageSize}
          onPageChange={noPaging}
          loading={isFetching}
          rowHeight={desktop ? 44 : 60}
          emptyLabel={t('hours.empty')}
          displayedRowsLabel={footer}
          isGroupRow={isDayRow}
          getRowId={getRowId}
        />
      ) : (
        <Alert tone="danger">{t('hours.loadFailed')}</Alert>
      )}
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 180, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined
                ? undefined
                : t('hours.menu.button', { day: formatDayText(menu.entry.day) }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.entry);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('hours.menu.edit')}
        </MenuItem>
        <MenuItem
          onClick={() => {
            setRemoving(menu?.entry);
            setMenu(undefined);
          }}
        >
          <DeleteOutlined aria-hidden />
          {t('hours.menu.remove')}
        </MenuItem>
      </Menu>
      {editing !== undefined && (
        <TimeEntryDialog
          entry={editing ?? undefined}
          today={today}
          day={start === thisWeek ? today : end}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(text, day) => {
            setEditing(undefined);
            setStart(weekStart(day));
            changed(text);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('hours.remove.title')}
        onClose={() => {
          setRemoving(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(undefined);
              }}
            >
              {t('hours.remove.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => {
                if (removing !== undefined) {
                  void remove(removing);
                }
              }}
            >
              {t('hours.remove.confirm')}
            </Button>
          </>
        }
      >
        {t('hours.remove.text', {
          hours: formatHours(removing?.hours ?? 0),
          day: formatDayText(removing?.day ?? today),
          project: removing?.project.code ?? '',
        })}
      </Dialog>
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

function noPaging() {
  // Every entry of the week is in the one page.
}
