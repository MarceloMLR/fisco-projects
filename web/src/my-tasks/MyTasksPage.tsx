import EventOutlined from '@mui/icons-material/EventOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import TimelineOutlined from '@mui/icons-material/TimelineOutlined';
import ViewKanbanOutlined from '@mui/icons-material/ViewKanbanOutlined';
import ViewListOutlined from '@mui/icons-material/ViewListOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { myTasksQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { CodeChip } from '../components/Card';
import { Checkbox } from '../components/Checkbox';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { DueDate } from '../components/DueDate';
import { ProgressBar } from '../components/ProgressBar';
import { SegmentedControl } from '../components/SegmentedControl';
import { formatDayText } from '../format/dates';
import { useNotify } from '../notices/notify';
import { lateText, missesDeadline, showsDelay } from '../projects/schedule';
import { TaskBoard } from '../projects/ScheduleBoard';
import { CommentSection } from '../projects/ScheduleComments';
import { TaskProgress } from '../projects/ScheduleItemProgress';
import { CommentCount, DeadlineStatus, NoteMark } from '../projects/ScheduleStatus';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import {
  cardsOf,
  entriesOf,
  isGroupRow,
  type MyTaskEntry,
  type MyTaskRow,
  type MyTasksGrouping,
  openCounts,
  rowsByDate,
  rowsByProject,
} from './myTasks';

type MyTasksView = 'list' | 'board';

const pageSize = 100;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const secondarySx = { color: 'var(--text-secondary)' } as const;
const getRowId = (row: MyTaskRow) => row.id;

/**
 * Minhas tarefas (ADR-0158): the tasks the person is in charge of, in the open projects the person is in, grouped by
 * project or by date in the Lista, or in the columns of the progress in the Quadro, with the completed ones of the last
 * 30 days when asked. The details open right here, with the progress and the comments; nothing changes in who sees
 * what in the projects (ADR-0043).
 */
export function MyTasksPage() {
  const { t } = useTranslation();
  const notify = useNotify();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [search, setSearch] = useSearchParams();
  const view: MyTasksView = search.get('view') === 'board' ? 'board' : 'list';
  const grouping: MyTasksGrouping = search.get('group') === 'date' ? 'date' : 'project';
  const withDone = search.get('done') === 'true';
  const [page, setPage] = useState(1);
  const { data, isFetching, isError } = useQuery({
    ...myTasksQuery(withDone),
    placeholderData: keepPreviousData,
  });
  const entries = useMemo(() => entriesOf(data), [data]);
  const rows = useMemo(
    () =>
      grouping === 'date' && data !== undefined
        ? rowsByDate(entries, data.today)
        : rowsByProject(entries),
    [data, entries, grouping],
  );
  const pageRows = useMemo(() => rows.slice((page - 1) * pageSize, page * pageSize), [rows, page]);
  const chosenId = search.get('item') ?? undefined;
  const chosen = entries.find((entry) => entry.item.id === chosenId);
  const counts = openCounts(entries);

  const setParam = useCallback(
    (name: string, value: string | undefined) => {
      setSearch(
        (current) => {
          const next = new URLSearchParams(current);
          if (value === undefined) {
            next.delete(name);
          } else {
            next.set(name, value);
          }
          return next;
        },
        { replace: true },
      );
    },
    [setSearch],
  );
  const choose = useCallback(
    (itemId: string) => {
      setParam('item', itemId);
    },
    [setParam],
  );

  const columns = useMemo<DataTableColumn<MyTaskRow>[]>(() => {
    const nameCell = (entry: MyTaskEntry) => (
      <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
          {grouping === 'date' && <CodeChip code={entry.project.code} />}
          <Box component="span" sx={ellipsisSx} title={entry.item.name}>
            {entry.item.name}
          </Box>
          <NoteMark note={entry.item.note} />
          <CommentCount count={entry.item.commentCount} />
        </Box>
        {!desktop && (
          <Box
            component="span"
            sx={{ ...ellipsisSx, ...secondarySx, fontSize: 12, lineHeight: '16px' }}
          >
            {t('projects.board.endsOn', { day: formatDayText(entry.item.end) })}
          </Box>
        )}
      </Box>
    );
    const groupCell = (row: Exclude<MyTaskRow, { kind: 'task' }>) =>
      row.kind === 'project' ? (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
          <CodeChip code={row.project.code} />
          <Box component="span" sx={ellipsisSx}>
            {row.project.name}
          </Box>
          <Box component="span" sx={{ ...secondarySx, fontWeight: 400, flex: 'none' }}>
            {t('myTasks.tasks', { count: row.count })}
          </Box>
          <Button
            component={Link}
            to={`/projects/${row.project.id}/schedule?view=list`}
            variant="text"
            size="small"
            startIcon={<TimelineOutlined />}
            aria-label={t('myTasks.openScheduleOf', { code: row.project.code })}
            sx={{ flex: 'none' }}
          >
            {desktop ? t('myTasks.openSchedule') : null}
          </Button>
        </Box>
      ) : (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          {t(`myTasks.dateGroups.${row.group}`)}
          <Box component="span" sx={{ ...secondarySx, fontWeight: 400 }}>
            {t('myTasks.tasks', { count: row.count })}
          </Box>
        </Box>
      );
    const task: DataTableColumn<MyTaskRow> = {
      field: 'task',
      headerName: t('myTasks.columns.task'),
      flex: 1.6,
      minWidth: desktop ? 260 : 180,
      // A group spans the line (fp-group-row).
      colSpan: (_value, row) => (row.kind === 'task' ? undefined : desktop ? 5 : 2),
      renderCell: ({ row }) => (row.kind === 'task' ? nameCell(row.entry) : groupCell(row)),
    };
    const progress: DataTableColumn<MyTaskRow> = {
      field: 'progress',
      headerName: t('myTasks.columns.progress'),
      width: desktop ? 148 : 120,
      renderCell: ({ row }) =>
        row.kind === 'task' && (
          <ProgressBar
            value={row.entry.item.progress}
            label={t('projects.schedule.itemProgress', { name: row.entry.item.name })}
            width="100%"
          />
        ),
    };
    if (!desktop) {
      return [task, progress];
    }
    return [
      task,
      {
        field: 'phase',
        headerName: t('myTasks.columns.phase'),
        flex: 1,
        minWidth: 160,
        renderCell: ({ row }) =>
          row.kind === 'task' && (
            <Box component="span" sx={{ ...ellipsisSx, ...secondarySx }}>
              {row.entry.parent === null
                ? row.entry.phase
                : `${row.entry.phase} · ${row.entry.parent}`}
            </Box>
          ),
      },
      {
        field: 'end',
        headerName: t('myTasks.columns.end'),
        width: 196,
        renderCell: ({ row }) =>
          row.kind === 'task' && (
            <Box sx={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              <span>{formatDayText(row.entry.item.end)}</span>
              {showsDelay(row.entry.item) && (
                <DueDate state="overdue">{lateText(row.entry.item, t)}</DueDate>
              )}
            </Box>
          ),
      },
      progress,
      {
        field: 'deadline',
        headerName: t('myTasks.columns.deadline'),
        width: 160,
        // The day, with how far past it an open task went, as in the Lista of the schedule (GanttA-2).
        renderCell: ({ row }) =>
          row.kind === 'task' &&
          row.entry.item.deadline !== null && (
            <Box sx={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              <span>{formatDayText(row.entry.item.deadline)}</span>
              {missesDeadline(row.entry.item) && (
                <DueDate state="overdue">
                  {t('projects.schedule.deadline.pastShort', {
                    count: row.entry.item.deadlineDeviation ?? 0,
                  })}
                </DueDate>
              )}
            </Box>
          ),
      },
    ];
  }, [desktop, grouping, t]);

  const chooseRow = useCallback(
    (row: MyTaskRow) => {
      if (row.kind === 'task') {
        choose(row.entry.item.id);
      }
    },
    [choose],
  );
  const footer = useCallback(
    () => t('myTasks.tasks', { count: entries.length }),
    [entries.length, t],
  );
  const emptyText = t(withDone ? 'myTasks.emptyWithDone' : 'myTasks.empty');

  return (
    <Page
      subtitle={
        data === undefined
          ? undefined
          : counts.tasks === 0
            ? t('myTasks.openTasks', { count: 0 })
            : t('myTasks.summary', {
                tasks: t('myTasks.openTasks', { count: counts.tasks }),
                projects: t('myTasks.inProjects', { count: counts.projects }),
              })
      }
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 'var(--space-2) var(--space-3)',
          minWidth: 0,
        }}
      >
        <SegmentedControl<MyTasksView>
          label={t('myTasks.views.label')}
          value={view}
          options={[
            { value: 'list', label: t('myTasks.views.list'), icon: ViewListOutlined },
            { value: 'board', label: t('myTasks.views.board'), icon: ViewKanbanOutlined },
          ]}
          onChange={(value) => {
            setParam('view', value === 'list' ? undefined : value);
          }}
        />
        {view === 'list' && (
          <SegmentedControl<MyTasksGrouping>
            label={t('myTasks.grouping.label')}
            value={grouping}
            options={[
              { value: 'project', label: t('myTasks.grouping.project'), icon: FolderOutlined },
              { value: 'date', label: t('myTasks.grouping.date'), icon: EventOutlined },
            ]}
            onChange={(value) => {
              setPage(1);
              setParam('group', value === 'project' ? undefined : value);
            }}
          />
        )}
        <Checkbox
          label={t('myTasks.showDone')}
          checked={withDone}
          onChange={(checked) => {
            setPage(1);
            setParam('done', checked ? 'true' : undefined);
          }}
        />
      </Box>
      {isError ? (
        <Alert tone="danger">{t('myTasks.loadFailed')}</Alert>
      ) : view === 'board' ? (
        <TaskBoard
          cards={cardsOf(entries)}
          owner={undefined}
          selectedId={chosen?.item.id}
          emptyText={data === undefined ? undefined : entries.length === 0 ? emptyText : undefined}
          onChoose={choose}
        />
      ) : (
        <DataTable<MyTaskRow>
          label={t('myTasks.table')}
          rows={pageRows}
          columns={columns}
          rowCount={rows.length}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          loading={isFetching}
          rowHeight={desktop ? 48 : 56}
          emptyLabel={emptyText}
          displayedRowsLabel={footer}
          onRowChoose={chooseRow}
          isGroupRow={isGroupRow}
          getRowId={getRowId}
          selectedRowId={chosen?.item.id}
        />
      )}
      <DetailsDrawer
        open={chosen !== undefined}
        onClose={() => {
          setParam('item', undefined);
        }}
        label={t('myTasks.details.label')}
      >
        {chosen !== undefined && <MyTaskDetails entry={chosen} onDone={notify} />}
      </DetailsDrawer>
    </Page>
  );
}

/** The dl of the details: the label on the left and the value on the right. */
const detailsListSx = {
  display: 'grid',
  gridTemplateColumns: '128px minmax(0, 1fr)',
  gap: 'var(--space-3)',
  margin: 0,
  fontSize: 14,
  lineHeight: '20px',
  '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
  '& dd': { margin: 0, minWidth: 0 },
} as const;

/**
 * The details of a task of the person (ADR-0158): the project and the phase, the progress, the dates, the delay and
 * the deadline, the note, "Abrir no cronograma" and the comments (ADR-0157).
 */
function MyTaskDetails({
  entry,
  onDone,
}: {
  entry: MyTaskEntry;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { item, project } = entry;
  return (
    <Box
      component="section"
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
    >
      <Box sx={{ minWidth: 0 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            minWidth: 0,
            fontSize: 13,
            lineHeight: '18px',
            color: 'var(--text-secondary)',
          }}
        >
          <CodeChip code={project.code} />
          <Box component="span" sx={ellipsisSx}>
            {project.name}
          </Box>
        </Box>
        <Box
          component="h2"
          sx={{
            margin: 'var(--space-2) 0 0',
            fontSize: 18,
            lineHeight: '26px',
            fontWeight: 600,
            overflowWrap: 'anywhere',
          }}
        >
          {item.name}
        </Box>
      </Box>
      <TaskProgress project={project} item={item} onDone={onDone} />
      <Box component="dl" sx={detailsListSx}>
        <dt>{t('projects.schedule.details.phase')}</dt>
        <dd>{entry.phase}</dd>
        {entry.parent !== null && (
          <>
            <dt>{t('myTasks.details.parent')}</dt>
            <dd>{entry.parent}</dd>
          </>
        )}
        <dt>{t('projects.schedule.details.start')}</dt>
        <dd>{formatDayText(item.start)}</dd>
        <dt>{t('projects.schedule.details.end')}</dt>
        <dd>
          <Box
            sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 'var(--space-2)' }}
          >
            {formatDayText(item.end)}
            {showsDelay(item) && <DueDate state="overdue">{lateText(item, t)}</DueDate>}
          </Box>
        </dd>
        <dt>{t('projects.schedule.details.duration')}</dt>
        <dd>{t('projects.schedule.duration', { count: item.duration })}</dd>
        {item.deadline !== null && (
          <>
            <dt>{t('projects.schedule.details.deadline')}</dt>
            <dd>
              <Box
                sx={{
                  display: 'flex',
                  flexWrap: 'wrap',
                  alignItems: 'center',
                  gap: 'var(--space-2)',
                }}
              >
                {formatDayText(item.deadline)}
                <DeadlineStatus item={item} />
              </Box>
            </dd>
          </>
        )}
        {item.note !== null && (
          <>
            <dt>{t('projects.schedule.details.note')}</dt>
            <Box component="dd" sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
              {item.note}
            </Box>
          </>
        )}
      </Box>
      <Box>
        <Button
          component={Link}
          to={`/projects/${project.id}/schedule?view=list&item=${item.id}`}
          variant="outlined"
          startIcon={<TimelineOutlined />}
        >
          {t('myTasks.details.openInSchedule')}
        </Button>
      </Box>
      <CommentSection project={project} item={item} />
    </Box>
  );
}
