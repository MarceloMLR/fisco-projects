import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import Box from '@mui/material/Box';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import useMediaQuery from '@mui/material/useMediaQuery';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type TimeEntry, timeEntryProblems } from '../api/client';
import { CodeChip } from '../components/Card';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { Dialog } from '../components/Dialog';
import { CardAvatar } from '../components/PersonCard';
import { type DayValue, formatDayMonthText, formatDayText } from '../format/dates';
import { useDates } from '../format/useDates';
import { problemType } from '../projects/projectForm';
import { theme } from '../theme/theme';
import { formatHours, type HoursRow, hoursRows, isDayRow, workOf } from './hours';
import { TimeEntryDialog } from './TimeEntryDialog';
import { Button, IconButton } from '../components/Button';
import { useNotify } from '../notices/notify';

/** A week or a month has a few dozen entries: they all fit in one page of the free Data Grid (ADR-0050). */
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

function noPaging() {
  // Every entry of the period is in the one page.
}

/**
 * The entries of a period grouped by day, the newest first, with the total of each day (ADR-0128): the table of
 * Minhas horas, where the first column is the project, and of the entries of a project for the Administrator, where it
 * is the person who logged them. The "⋮" of each entry the person may change corrects it, in the dialog of the entry,
 * or deletes it, after a confirmation. On the phone, each entry shows the project, what was done and the hours.
 */
export function TimeEntryTable({
  label,
  entries,
  first,
  loading,
  emptyLabel,
  footer,
  onChanged,
}: {
  /** Accessible name of the table. */
  label: string;
  entries: readonly TimeEntry[];
  /** What the first column tells: the project of each entry or the person who logged it. */
  first: 'project' | 'person';
  loading: boolean;
  emptyLabel: string;
  /** The text of the footer, such as "Total da semana: 27 h". */
  footer: string;
  /** An entry was corrected or deleted, with the message and, when corrected, its day. */
  onChanged: (message: string, day?: DayValue) => void;
}) {
  const { t } = useTranslation();
  const notify = useNotify();
  const { toDayValue, formatWeekday } = useDates();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const today = toDayValue(new Date());
  const rows = useMemo(() => hoursRows(entries), [entries]);
  const [menu, setMenu] = useState<{ anchor: HTMLElement; entry: TimeEntry }>();
  const [editing, setEditing] = useState<TimeEntry>();
  const [removing, setRemoving] = useState<TimeEntry>();

  const dayLabel = useCallback(
    (day: DayValue) => {
      const weekday = formatWeekday(day);
      return `${weekday.charAt(0).toUpperCase()}${weekday.slice(1)}, ${formatDayMonthText(day)}`;
    },
    [formatWeekday],
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
      if (response.ok) {
        onChanged(t('hours.messages.removed'));
      } else {
        notify(
          response.status === 409 && problemType(problem) === timeEntryProblems.projectClosed
            ? t('hours.messages.closed')
            : t('hours.messages.failed'),
          { tone: 'error' },
        );
      }
    } catch {
      notify(t('hours.messages.failed'), { tone: 'error' });
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
    const subjectCell = (entry: TimeEntry) =>
      first === 'project' ? (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
          <CodeChip code={entry.project.code} />
          <Box component="span" sx={{ ...ellipsisSx, ...secondarySx }}>
            {entry.project.name}
          </Box>
        </Box>
      ) : (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
          <CardAvatar person={entry.person} size="sm" />
          <Box component="span" sx={ellipsisSx}>
            {entry.person.name}
          </Box>
        </Box>
      );
    const firstColumn: DataTableColumn<HoursRow> = {
      field: 'first',
      headerName: desktop
        ? t(first === 'project' ? 'hours.columns.project' : 'projectHours.entries.person')
        : t('hours.columns.description'),
      flex: desktop ? 1.1 : 1,
      minWidth: desktop ? 240 : 160,
      // A day spans the project or the person, the work and the description (fp-group-row).
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
          subjectCell(row.entry)
        ) : (
          <Box sx={{ minWidth: 0 }}>
            <Box
              component="span"
              sx={{
                ...ellipsisSx,
                ...secondarySx,
                display: 'block',
                ...(first === 'project' && { fontFamily: 'var(--font-mono)' }),
                fontSize: 12,
                lineHeight: '16px',
              }}
            >
              {first === 'project' ? row.entry.project.code : row.entry.person.name}
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
      return [firstColumn, hours, actions];
    }
    return [
      firstColumn,
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
  }, [dayLabel, desktop, first, openMenu, t, today]);

  const displayedRows = useCallback(() => footer, [footer]);

  return (
    <>
      <DataTable<HoursRow>
        label={label}
        rows={rows}
        columns={columns}
        rowCount={rows.length}
        page={1}
        pageSize={pageSize}
        onPageChange={noPaging}
        loading={loading}
        rowHeight={desktop ? 44 : 60}
        emptyLabel={emptyLabel}
        displayedRowsLabel={displayedRows}
        isGroupRow={isDayRow}
        getRowId={getRowId}
      />
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
          entry={editing}
          today={today}
          day={editing.day}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(text, day) => {
            setEditing(undefined);
            onChanged(text, day);
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
              onClick={() => (removing === undefined ? undefined : remove(removing))}
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
    </>
  );
}
