import { zodResolver } from '@hookform/resolvers/zod';
import AddOutlined from '@mui/icons-material/AddOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import CloseFullscreenOutlined from '@mui/icons-material/CloseFullscreenOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import OpenInFullOutlined from '@mui/icons-material/OpenInFullOutlined';
import TimelineOutlined from '@mui/icons-material/TimelineOutlined';
import TodayOutlined from '@mui/icons-material/TodayOutlined';
import ViewListOutlined from '@mui/icons-material/ViewListOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import {
  api,
  type Project,
  projectProblems,
  type Schedule,
  type ScheduleItem,
  type ScheduleItemKind,
  type SchedulePhase,
} from '../api/client';
import { projectScheduleQuery, queryKeys, taskHoursQuery } from '../api/queries';
import { TaskHoursCell, TaskHoursSection } from '../admin/TaskHours';
import { seesTaskHours, taskHoursById } from '../admin/taskHours';
import { Alert } from '../components/Alert';
import { CardAvatar } from '../components/PersonCard';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { LogoTile } from '../components/LogoTile';
import { ProgressBar } from '../components/ProgressBar';
import { SegmentedControl } from '../components/SegmentedControl';
import { formatDayText } from '../format/dates';
import { type GanttScale, ganttScales } from '../schedule-gantt/ganttLayout';
import type { GanttTarget } from '../schedule-gantt/ganttRows';
import type { ScheduleGanttHandle } from '../schedule-gantt/ScheduleGantt';
import { theme } from '../theme/theme';
import { useProjectOutlet } from './projectOutlet';
import {
  findItem,
  fullDisplay,
  isPhaseRow,
  itemsOf,
  lateText,
  missesDeadline,
  predecessorTexts,
  slackText,
  type ScheduleDisplay,
  type ScheduleRow,
  scheduleRows,
  showsDelay,
  ownerText,
} from './schedule';
import { ScheduleItemFields } from './ScheduleItemFields';
import { MilestoneState, TaskProgress } from './ScheduleItemProgress';
import {
  bodyOf,
  failureOf,
  newItemValues,
  scheduleItemErrors,
  scheduleItemSchema,
  type ScheduleItemValues,
  showRefusal,
  valuesOfItem,
} from './scheduleItemValues';
import { problemType } from './projectForm';
import {
  BaselineSection,
  BaselineSummary,
  DisplayChecks,
  DisplayMenu,
  ScheduleMoreMenu,
} from './ScheduleBaseline';
import { UndoButton } from './ScheduleSteps';
import { SchedulePhaseDialog } from './SchedulePhaseDialog';
import { SubtaskSection } from './ScheduleSubtasks';
import {
  DeadlineStatus,
  LateCounts,
  MilestoneStatus,
  MissedDeadlines,
  NoteMark,
  OwnerAway,
  OwnerAwayLine,
} from './ScheduleStatus';
import { ScheduleTimeline } from './ScheduleTimeline';
import {
  DependsOnEditCell,
  EditInTableButton,
  type ListRow,
  ScheduleEditCell,
  TableEditHelp,
} from './ScheduleTableEdit';
import {
  cellError,
  cellRequest,
  type CellValue,
  type EditableField,
  editableFields,
} from './scheduleTableEdit';
import { ScheduleFilterMenu } from './ScheduleFilterMenu';
import {
  type FilterContext,
  filterOf,
  filteredSchedule,
  filterText,
  ownerOptions,
  passingCount,
  type ScheduleFilter,
  withFilter,
} from './scheduleFilter';
import { useSession } from '../session/SessionContext';
import { Button, IconButton } from '../components/Button';
import { Form } from '../components/Form';

/** Rows of a page of the list; the free Data Grid shows up to 100 (ADR-0050). */
const pageSize = 100;

const emptySchedule: Schedule = {
  today: '',
  start: null,
  end: null,
  progress: 0,
  lateTasks: 0,
  lateMilestones: 0,
  phases: [],
  nonWorkingDays: [],
  baseline: null,
  undo: null,
  redo: null,
  missedDeadlines: 0,
};

/** The views of the tab (ADR-0071). */
type ScheduleView = 'timeline' | 'list';

const isView = (value: string | null): value is ScheduleView =>
  value === 'timeline' || value === 'list';
const isScale = (value: string | null): value is GanttScale =>
  ganttScales.some((scale) => scale === value);

const secondarySx = { color: 'var(--text-secondary)' } as const;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

type PhaseDialogState = { phase?: SchedulePhase } | undefined;

/**
 * The tab Cronograma of a project, with the Linha do tempo and the Lista (ADR-0071). The Linha do tempo draws the
 * phases, tasks, milestones and dependencies in the time, in days, weeks or months, filling the width, and who edits
 * the schedule drags, stretches and links the bars on a computer; Expandir opens it over almost the whole screen, with
 * the full table. The Lista has the phases as group rows with their dates and progress, and under each one its tasks
 * and milestones in order, with the start, the end, the duration in business days, the owner, the progress (for a
 * milestone, its state) and what each depends on. A late task or milestone says so by icon, color and text
 * (ADR-0065, ADR-0086). Who edits the schedule (ADR-0043) includes phases and tasks and changes them; the details of
 * a task open in a drawer (ADR-0081), where the people in charge of a task update its progress. Every date comes
 * computed from the API (ADR-0051). The view and the scale stay in the address, to come back to them.
 */
export function ProjectSchedulePage() {
  const { t } = useTranslation();
  const { project, notify } = useProjectOutlet();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data, isFetching } = useQuery(projectScheduleQuery(project.id));
  const schedule = data ?? emptySchedule;
  const canEdit = project.permissions.canEditSchedule;
  const [page, setPage] = useState(1);
  /** The task or milestone whose details are open, chosen in the screen or asked by the address (?item=). */
  const [chosenId, setChosenId] = useState<string>();
  const [phaseDialog, setPhaseDialog] = useState<PhaseDialogState>();
  /** "Nova tarefa" open, maybe inside a task ("Nova subtarefa", ADR-0088). */
  const [creating, setCreating] = useState<{ parentId?: string }>();
  const [expanded, setExpanded] = useState(false);
  /** "Editar na tabela" on: the cells of the Lista edit in place, by the keyboard (ADR-0143). */
  const [tableEdit, setTableEdit] = useState(false);
  /** What "Exibir" shows in the Linha do tempo, the same in Expandir (ADR-0071). */
  const [display, setDisplay] = useState<ScheduleDisplay>(fullDisplay);
  const [search, setSearch] = useSearchParams();
  const asked = search.get('view');
  const view: ScheduleView = isView(asked) ? asked : desktop ? 'timeline' : 'list';
  const askedScale = search.get('scale');
  const scale: GanttScale = isScale(askedScale) ? askedScale : 'days';
  const timeline = useRef<ScheduleGanttHandle>(null);
  const expandedTimeline = useRef<ScheduleGanttHandle>(null);
  /**
   * The phases and summary tasks folded, until the arrow shows them again, the same in the Lista, the Linha do tempo
   * and Expandir (Cronograma-4.2); all are shown at first.
   */
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(() => new Set());
  /** The filter of the Lista, the Linha do tempo and Expandir, in the address (GanttA-3). */
  const filter = useMemo(() => filterOf(search), [search]);
  const { session } = useSession();
  const me = session.status === 'signed-in' ? session.user.id : undefined;
  /** The hours of the tasks are the Administrator's and the manager's of the project (ADR-0138, ADR-0144). */
  const seesHours = seesTaskHours(
    project,
    session.status === 'signed-in' ? session.user : undefined,
  );
  const { data: taskHours } = useQuery({
    ...taskHoursQuery(project.id),
    enabled: seesHours,
  });
  const hoursById = useMemo(() => taskHoursById(taskHours), [taskHours]);
  const filterContext = useMemo<FilterContext>(
    () => ({ today: schedule.today, me }),
    [schedule.today, me],
  );
  const shown = useMemo(
    () => (filter === undefined ? schedule : filteredSchedule(schedule, filter, filterContext)),
    [schedule, filter, filterContext],
  );
  const shownCount =
    filter === undefined ? undefined : passingCount(schedule, filter, filterContext);
  const rows = useMemo(() => scheduleRows(shown, collapsed), [shown, collapsed]);
  const toggleSubtasks = useCallback((itemId: string) => {
    setCollapsed((current) => {
      const next = new Set(current);
      if (!next.delete(itemId)) {
        next.add(itemId);
      }
      return next;
    });
  }, []);
  const pageRows = useMemo(() => rows.slice((page - 1) * pageSize, page * pageSize), [rows, page]);
  const openId = chosenId ?? search.get('item') ?? undefined;
  const chosen = openId === undefined ? undefined : findItem(schedule, openId);
  /** Closes the details, and takes the item out of the address that asked for it. */
  const closeDetails = () => {
    setChosenId(undefined);
    if (search.has('item')) {
      setSearch(
        (current) => {
          const next = new URLSearchParams(current);
          next.delete('item');
          return next;
        },
        { replace: true },
      );
    }
  };
  const itemCount = itemsOf(schedule).length;

  const editingTable = tableEdit && desktop && view === 'list';
  /** Who edits the schedule, or updates the progress of some task, may turn the edition in the table on. */
  const mayEditTable = canEdit || itemsOf(schedule).some((item) => item.canUpdateProgress);
  /** What an editable column has, in the mode "Editar na tabela": the cell kept until saved and its check. */
  const editableColumn = useCallback(
    (field: EditableField) => ({
      editable: editingTable,
      valueSetter: (value: CellValue, row: ListRow): ListRow => ({
        ...row,
        edit: { field, value },
      }),
      preProcessEditCellProps: ({ props, row }: { props: { value?: unknown }; row: ListRow }) => ({
        ...props,
        error:
          row.kind === 'item' && cellError(field, props.value as CellValue, row.item) !== undefined,
      }),
    }),
    [editingTable],
  );

  const columns = useMemo<DataTableColumn<ListRow>[]>(
    () => [
      {
        field: 'name',
        headerName: t('projects.schedule.columns.name'),
        flex: 2,
        minWidth: 220,
        ...editableColumn('name'),
        valueGetter: (_value, row) => (row.kind === 'phase' ? row.phase.name : row.item.name),
        renderEditCell: (params) => (
          <ScheduleEditCell params={params} label={t('projects.schedule.columns.name')} />
        ),
        renderCell: ({ row, tabIndex }) =>
          row.kind === 'phase' ? (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
              <SubtasksArrow
                name={row.phase.name}
                phase
                open={!collapsed.has(row.phase.id)}
                tabIndex={tabIndex}
                onToggle={() => {
                  toggleSubtasks(row.phase.id);
                }}
              />
              <Box component="span" sx={ellipsisSx}>
                {row.phase.name}
              </Box>
            </Box>
          ) : (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                minWidth: 0,
                // A subtask one step further in than the items of the phase; a task with subtasks has its arrow in
                // the step, with the name where the others start (ADR-0088).
                paddingLeft: row.item.hasSubtasks
                  ? 0
                  : row.item.parentId === null
                    ? 'var(--space-6)'
                    : 'calc(2 * var(--space-6))',
                fontWeight: row.item.hasSubtasks ? 500 : undefined,
              }}
            >
              {row.item.hasSubtasks && (
                <SubtasksArrow
                  name={row.item.name}
                  open={!collapsed.has(row.item.id)}
                  tabIndex={tabIndex}
                  onToggle={() => {
                    toggleSubtasks(row.item.id);
                  }}
                />
              )}
              {row.item.kind === 'Milestone' && (
                <FlagOutlined
                  aria-label={t('projects.schedule.kinds.Milestone')}
                  sx={{ fontSize: 16, color: 'var(--text-secondary)', flex: 'none' }}
                />
              )}
              <Box component="span" sx={ellipsisSx} title={row.item.name}>
                {row.item.name}
              </Box>
              <NoteMark note={row.item.note} />
            </Box>
          ),
      },
      {
        field: 'start',
        headerName: t('projects.schedule.columns.start'),
        width: 120,
        ...editableColumn('start'),
        renderEditCell: (params) => (
          <ScheduleEditCell params={params} label={t('projects.schedule.columns.start')} day />
        ),
        valueGetter: (_value, row) => {
          const start = row.kind === 'phase' ? row.phase.start : row.item.start;
          return start === null ? '' : formatDayText(start);
        },
      },
      {
        field: 'end',
        headerName: t('projects.schedule.columns.end'),
        width: 196,
        ...editableColumn('end'),
        valueGetter: (_value, row) => (row.kind === 'phase' ? '' : formatDayText(row.item.end)),
        renderEditCell: (params) => (
          <ScheduleEditCell params={params} label={t('projects.schedule.columns.end')} day />
        ),
        renderCell: ({ row }) => {
          if (row.kind === 'phase') {
            return (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
                {row.phase.end !== null && <span>{formatDayText(row.phase.end)}</span>}
                {row.phase.late && <DueDate state="overdue">{t('projects.schedule.late')}</DueDate>}
              </Box>
            );
          }
          // The delay in business days, under the end of a task, and alone for a milestone, which has no end.
          const { item } = row;
          return (
            <Box sx={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              {item.kind === 'Task' && <span>{formatDayText(item.end)}</span>}
              {showsDelay(item) && <DueDate state="overdue">{lateText(item, t)}</DueDate>}
            </Box>
          );
        },
      },
      {
        field: 'deadline',
        headerName: t('projects.schedule.columns.deadline'),
        width: 160,
        // The day, with how far past it an open item went (GanttA-2).
        renderCell: ({ row }) =>
          row.kind === 'phase' || row.item.deadline === null ? null : (
            <Box sx={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              <span>{formatDayText(row.item.deadline)}</span>
              {missesDeadline(row.item) && (
                <DueDate state="overdue">
                  {t('projects.schedule.deadline.pastShort', {
                    count: row.item.deadlineDeviation ?? 0,
                  })}
                </DueDate>
              )}
            </Box>
          ),
      },
      {
        field: 'duration',
        headerName: t('projects.schedule.columns.duration'),
        width: 112,
        ...editableColumn('duration'),
        valueGetter: (_value, row) => (row.kind === 'phase' ? '' : String(row.item.duration)),
        renderEditCell: (params) => (
          <ScheduleEditCell
            params={params}
            label={t('projects.schedule.columns.duration')}
            numeric
          />
        ),
        renderCell: ({ row }) =>
          row.kind === 'phase'
            ? null
            : row.item.kind === 'Milestone'
              ? t('projects.schedule.kinds.Milestone')
              : t('projects.schedule.duration', { count: row.item.duration }),
      },
      {
        field: 'owner',
        headerName: t('projects.schedule.columns.owner'),
        width: 172,
        renderCell: ({ row }) => {
          if (row.kind === 'phase' || row.item.kind === 'Milestone') {
            return null;
          }
          const owner = row.item.owner;
          // The client of the project in charge of the task, with its logo (ADR-0102).
          if (row.item.ownedByClient) {
            return (
              <Box
                sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}
              >
                <LogoTile name={project.client?.name ?? ''} src={project.client?.logoUrl} />
                <Box component="span" sx={ellipsisSx}>
                  {ownerText(row.item, project.client?.name ?? null, t)}
                </Box>
              </Box>
            );
          }
          return owner === null ? (
            <Box component="span" sx={secondarySx}>
              {t('projects.schedule.noOwner')}
            </Box>
          ) : (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
              <CardAvatar person={owner} size="sm" />
              <Box component="span" sx={ellipsisSx}>
                {owner.name}
              </Box>
              <OwnerAway item={row.item} />
            </Box>
          );
        },
      },
      {
        field: 'progress',
        headerName: t('projects.schedule.columns.progress'),
        width: 148,
        ...editableColumn('progress'),
        valueGetter: (_value, row) => (row.kind === 'phase' ? '' : String(row.item.progress)),
        renderEditCell: (params) => (
          <ScheduleEditCell
            params={params}
            label={t('projects.schedule.columns.progress')}
            numeric
          />
        ),
        renderCell: ({ row }) => {
          if (row.kind === 'phase') {
            return row.phase.start === null ? null : (
              <ProgressBar
                value={row.phase.progress}
                label={t('projects.schedule.phaseProgress', { name: row.phase.name })}
                width="100%"
              />
            );
          }
          return row.item.kind === 'Milestone' ? (
            <MilestoneStatus item={row.item} />
          ) : (
            <ProgressBar
              value={row.item.progress}
              label={t('projects.schedule.itemProgress', { name: row.item.name })}
              width="100%"
            />
          );
        },
      },
      ...(seesHours
        ? [
            {
              field: 'hours',
              headerName: t('projects.schedule.columns.hours'),
              width: 132,
              renderCell: ({ row }) =>
                row.kind === 'phase' || row.item.kind === 'Milestone' ? null : (
                  <TaskHoursCell task={hoursById.get(row.item.id)} />
                ),
            } satisfies DataTableColumn<ScheduleRow>,
          ]
        : []),
      {
        field: 'dependsOn',
        headerName: t('projects.schedule.columns.dependsOn'),
        flex: 1,
        minWidth: 160,
        ...editableColumn('dependsOn'),
        valueGetter: (_value, row) => (row.kind === 'phase' ? [] : row.item.predecessorIds),
        renderEditCell: (params) => (
          <DependsOnEditCell
            params={params}
            schedule={schedule}
            label={t('projects.schedule.columns.dependsOn')}
          />
        ),
        renderCell: ({ row }) => {
          const text =
            row.kind === 'phase' ? '' : predecessorTexts(row.item, schedule, t).join(', ');
          return (
            <Box component="span" sx={{ ...ellipsisSx, ...secondarySx }} title={text}>
              {text}
            </Box>
          );
        },
      },
    ],
    [t, schedule, collapsed, toggleSubtasks, project.client, seesHours, hoursById, editableColumn],
  );

  const queryClient = useQueryClient();
  const isCellEditable = useCallback(
    ({ row, field }: { row: ListRow; field: string }) =>
      row.kind === 'item' && editableFields(row.item, canEdit).has(field as EditableField),
    [canEdit],
  );
  /**
   * Saves the cell edited, as the details and the Linha do tempo save (ADR-0143): one request, a notice, and the
   * schedule asked again; gives the line read again, or the one before when the API refused it.
   */
  const saveCell = useCallback(
    async (newRow: ListRow, oldRow: ListRow): Promise<ListRow> => {
      const { edit } = newRow;
      const before: ListRow = { ...oldRow, edit: undefined };
      if (edit === undefined || oldRow.kind !== 'item') {
        return before;
      }
      const { item } = oldRow;
      const request = cellRequest(edit.field, edit.value, item);
      if (request === undefined) {
        return before;
      }
      const path = { id: project.id, itemId: item.id };
      try {
        const { error, response, pushed } =
          request.kind === 'item'
            ? await api
                .PUT('/api/projects/{id}/schedule/items/{itemId}', {
                  params: { path },
                  body: request.body,
                })
                .then((answer) => ({ ...answer, pushed: answer.data?.pushed ?? 0 }))
            : request.kind === 'dates'
              ? await api
                  .PUT('/api/projects/{id}/schedule/items/{itemId}/dates', {
                    params: { path },
                    body: { start: request.start, end: request.end },
                  })
                  .then((answer) => ({ ...answer, pushed: answer.data?.pushed ?? 0 }))
              : await api
                  .PUT('/api/projects/{id}/schedule/items/{itemId}/progress', {
                    params: { path },
                    body: { progress: request.progress },
                  })
                  .then((answer) => ({ ...answer, pushed: 0 }));
        if (!response.ok) {
          notify(
            problemType(error) === projectProblems.scheduleCycle
              ? t(scheduleItemErrors.cycle)
              : failureOf(error, response.status, t),
            { tone: 'error' },
          );
        } else if (request.kind === 'progress') {
          notify(
            t('projects.schedule.messages.progressUpdated', {
              name: item.name,
              progress: request.progress,
            }),
          );
        } else {
          const name = request.kind === 'item' ? request.body.name : item.name;
          const saved = t(`projects.schedule.messages.itemSaved.${item.kind}`, { name });
          notify(
            pushed === 0 ? saved : t('projects.schedule.messages.pushed', { saved, count: pushed }),
          );
        }
      } catch {
        notify(t('projects.schedule.messages.failed'), { tone: 'error' });
      }
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.projects }),
        queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
      ]);
      const fresh = queryClient.getQueryData(projectScheduleQuery(project.id).queryKey);
      const found = fresh === undefined ? undefined : findItem(fresh, item.id);
      return found === undefined
        ? before
        : { kind: 'item', id: item.id, item: found.item, phase: found.phase };
    },
    [project.id, notify, t, queryClient],
  );
  const editing = useMemo(
    () => (editingTable ? { isCellEditable, processRowUpdate: saveCell } : undefined),
    [editingTable, isCellEditable, saveCell],
  );

  const footer = useCallback(
    () =>
      shownCount === undefined
        ? t('projects.schedule.footer', {
            phases: t('projects.schedule.phases', { count: schedule.phases.length }),
            items: t('projects.schedule.items', { count: itemCount }),
          })
        : t('projects.schedule.filter.shown', { shown: shownCount, count: itemCount }),
    [t, schedule.phases.length, itemCount, shownCount],
  );
  const owners = useMemo(
    () => ownerOptions(schedule, project.client?.name ?? null, t),
    [schedule, project.client, t],
  );
  const chooseFilter = useCallback(
    (chosen: ScheduleFilter | undefined) => {
      setPage(1);
      setSearch((current) => withFilter(current, chosen), { replace: true });
    },
    [setSearch],
  );
  const filterMenu = <ScheduleFilterMenu filter={filter} owners={owners} onChange={chooseFilter} />;
  const choose = useCallback(
    (row: ScheduleRow) => {
      if (row.kind === 'item') {
        setChosenId(row.item.id);
      } else if (canEdit) {
        setPhaseDialog({ phase: row.phase });
      }
    },
    [canEdit],
  );
  const getRowId = useCallback((row: ScheduleRow) => row.id, []);
  const chooseInTimeline = useCallback(
    (target: GanttTarget) => {
      if (target.kind === 'item') {
        setChosenId(target.item.id);
      } else if (canEdit) {
        setPhaseDialog({ phase: target.phase });
      }
    },
    [canEdit],
  );
  const setParam = (name: string, value: string) => {
    setSearch(
      (current) => {
        const next = new URLSearchParams(current);
        next.set(name, value);
        return next;
      },
      { replace: true },
    );
  };

  const scaleControl = (
    <SegmentedControl<GanttScale>
      label={t('projects.timeline.scales.label')}
      value={scale}
      options={ganttScales.map((value) => ({
        value,
        label: t(`projects.timeline.scales.${value}`),
      }))}
      onChange={(value) => {
        setParam('scale', value);
      }}
    />
  );
  // The progress of the project and the delays (ADR-0020, ADR-0086), beside the Lista or under the Linha do tempo.
  const summary = itemCount > 0 && (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 'var(--space-2) var(--space-4)',
        minWidth: 0,
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
        <Box
          component="span"
          sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
        >
          {t('projects.schedule.progress')}
        </Box>
        <ProgressBar
          value={schedule.progress}
          label={t('projects.schedule.progress')}
          width={160}
        />
      </Box>
      <LateCounts tasks={schedule.lateTasks} milestones={schedule.lateMilestones} />
      <MissedDeadlines count={schedule.missedDeadlines} />
      <BaselineSummary schedule={schedule} />
      {shownCount !== undefined && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <Box component="span" sx={{ fontSize: 13, lineHeight: '18px', ...secondarySx }}>
            {filter !== undefined && `${filterText(filter, owners, t)} · `}
            {t('projects.schedule.filter.shown', { shown: shownCount, count: itemCount })}
          </Box>
          <Button
            variant="text"
            size="small"
            onClick={() => {
              chooseFilter(undefined);
            }}
          >
            {t('projects.schedule.filter.clear')}
          </Button>
        </Box>
      )}
    </Box>
  );
  const moreMenu = (
    <ScheduleMoreMenu
      project={project}
      schedule={schedule}
      canEdit={canEdit}
      scale={scale}
      display={display}
    />
  );
  const undoButton = canEdit && <UndoButton project={project} schedule={schedule} />;
  const newItemButton = (
    <Button
      variant="contained"
      size="small"
      startIcon={<AddOutlined />}
      disabled={schedule.phases.length === 0}
      onClick={() => {
        setCreating({});
      }}
    >
      {t('projects.schedule.newItem')}
    </Button>
  );

  const actions = (fullWidth: boolean) =>
    canEdit && (
      <Box
        sx={{
          display: 'flex',
          gap: 'var(--space-2)',
          ...(fullWidth ? { width: '100%', '& > *': { flex: 1 } } : { flex: 'none' }),
        }}
      >
        <Button
          variant="outlined"
          size="small"
          startIcon={<AddOutlined />}
          onClick={() => {
            setPhaseDialog({});
          }}
        >
          {t('projects.schedule.newPhase')}
        </Button>
        {newItemButton}
      </Box>
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
      <Box
        role="toolbar"
        aria-label={t('projects.schedule.table')}
        sx={{
          display: 'flex',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 'var(--space-2) var(--space-3)',
          minWidth: 0,
        }}
      >
        <SegmentedControl<ScheduleView>
          label={t('projects.timeline.views.label')}
          value={view}
          options={[
            {
              value: 'timeline',
              label: t('projects.timeline.views.timeline'),
              icon: TimelineOutlined,
            },
            { value: 'list', label: t('projects.timeline.views.list'), icon: ViewListOutlined },
          ]}
          onChange={(value) => {
            setParam('view', value);
          }}
        />
        {view === 'timeline' && scaleControl}
        {view === 'timeline' && (
          <TodayButton
            disabled={schedule.start === null}
            onClick={() => {
              timeline.current?.goToToday();
            }}
          />
        )}
        {desktop && <Box sx={{ flex: 1 }} />}
        {desktop && view === 'list' && mayEditTable && itemCount > 0 && (
          <EditInTableButton on={tableEdit} onChange={setTableEdit} />
        )}
        {itemCount > 0 && filterMenu}
        {view === 'timeline' && <DisplayMenu display={display} onChange={setDisplay} />}
        {undoButton}
        {desktop && view === 'timeline' && (
          <Button
            variant="outlined"
            size="small"
            startIcon={<OpenInFullOutlined />}
            disabled={schedule.start === null}
            onClick={() => {
              setExpanded(true);
            }}
          >
            {t('projects.timeline.expand')}
          </Button>
        )}
        {desktop && actions(false)}
        {moreMenu}
      </Box>
      {editingTable && <TableEditHelp />}
      {view === 'timeline' ? (
        <ScheduleTimeline
          project={project}
          schedule={shown}
          emptyText={shownCount === 0 ? t('projects.schedule.filter.empty') : undefined}
          scale={scale}
          editable={canEdit && desktop}
          selectedId={chosen?.item.id ?? phaseDialog?.phase?.id}
          display={display}
          ganttRef={timeline}
          onChoose={chooseInTimeline}
          aside={summary}
          collapsed={collapsed}
          onToggle={toggleSubtasks}
        />
      ) : (
        <DataTable<ListRow>
          label={t('projects.schedule.table')}
          rows={pageRows}
          columns={columns}
          rowCount={rows.length}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          loading={isFetching}
          rowHeight={48}
          emptyLabel={t(
            shownCount === undefined ? 'projects.schedule.empty' : 'projects.schedule.filter.empty',
          )}
          displayedRowsLabel={footer}
          onRowChoose={editingTable ? undefined : choose}
          editing={editing}
          isGroupRow={isPhaseRow}
          groupRowsChoosable={canEdit}
          getRowId={getRowId}
          selectedRowId={chosen?.item.id ?? phaseDialog?.phase?.id}
          tools={summary === false ? undefined : summary}
        />
      )}
      <Dialog
        open={expanded}
        size="full"
        title={t('projects.timeline.expandedTitle')}
        subtitle={`${project.code} · ${project.name}`}
        onClose={() => {
          setExpanded(false);
        }}
        headerTools={
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 'var(--space-2)',
              flex: 1,
            }}
          >
            {scaleControl}
            <TodayButton
              disabled={schedule.start === null}
              onClick={() => {
                expandedTimeline.current?.goToToday();
              }}
            />
            {filterMenu}
            <DisplayChecks display={display} onChange={setDisplay} />
            <Box sx={{ flex: 1 }} />
            {undoButton}
            {canEdit && newItemButton}
            <Button
              variant="outlined"
              size="small"
              startIcon={<CloseFullscreenOutlined />}
              onClick={() => {
                setExpanded(false);
              }}
            >
              {t('projects.timeline.collapse')}
            </Button>
          </Box>
        }
      >
        {expanded && (
          <ScheduleTimeline
            project={project}
            schedule={shown}
            emptyText={shownCount === 0 ? t('projects.schedule.filter.empty') : undefined}
            scale={scale}
            editable={canEdit}
            detailed
            selectedId={chosen?.item.id ?? phaseDialog?.phase?.id}
            display={display}
            ganttRef={expandedTimeline}
            onChoose={chooseInTimeline}
            collapsed={collapsed}
            onToggle={toggleSubtasks}
          />
        )}
      </Dialog>
      {!desktop && actions(true)}
      {phaseDialog !== undefined && (
        <SchedulePhaseDialog
          project={project}
          phase={phaseDialog.phase}
          onClose={() => {
            setPhaseDialog(undefined);
          }}
          onDone={(message, options) => {
            setPhaseDialog(undefined);
            notify(message, options);
          }}
        />
      )}
      {creating !== undefined && (
        <NewItemDialog
          project={project}
          schedule={schedule}
          parentId={creating.parentId}
          onClose={() => {
            setCreating(undefined);
          }}
          onCreated={(message) => {
            setCreating(undefined);
            notify(message);
          }}
        />
      )}
      <DetailsDrawer
        open={chosen !== undefined}
        onClose={closeDetails}
        label={t('projects.schedule.details.label')}
      >
        {chosen !== undefined &&
          (canEdit ? (
            <ItemEditor
              // A saved change comes back with the dates the API computed, and the form starts again from them; a
              // change of the progress or of the state of a milestone leaves what is being typed as it is.
              key={`${chosen.item.id}:${JSON.stringify(valuesOfItem(chosen.item))}:${chosen.item.end}`}
              project={project}
              schedule={schedule}
              item={chosen.item}
              onSaved={notify}
              onRemoved={(message) => {
                closeDetails();
                notify(message);
              }}
              onNewSubtask={() => {
                closeDetails();
                setCreating({ parentId: chosen.item.id });
              }}
              onOpen={setChosenId}
            />
          ) : (
            <ItemDetails
              project={project}
              item={chosen.item}
              phase={chosen.phase}
              schedule={schedule}
              onDone={notify}
              onOpen={setChosenId}
            />
          ))}
      </DetailsDrawer>
    </Box>
  );
}

/**
 * The arrow of a task with subtasks in the Lista, as in the Linha do tempo: it hides and shows the subtasks, without
 * opening the details of the task, by the mouse or by Enter and Space (ADR-0088).
 */
function SubtasksArrow({
  name,
  phase = false,
  open,
  tabIndex,
  onToggle,
}: {
  name: string;
  /** The arrow of a phase, named as the one of the Linha do tempo (Cronograma-4.2). */
  phase?: boolean;
  open: boolean;
  tabIndex: -1 | 0;
  onToggle: () => void;
}) {
  const { t } = useTranslation();
  const label = phase
    ? t(open ? 'projects.timeline.collapseRow' : 'projects.timeline.expandRow', { name })
    : t(open ? 'projects.schedule.collapseSubtasks' : 'projects.schedule.expandSubtasks', {
        name,
      });
  return (
    <IconButton
      aria-expanded={open}
      aria-label={label}
      tabIndex={tabIndex}
      onClick={(event) => {
        event.stopPropagation();
        onToggle();
      }}
      onKeyDown={(event) => {
        // The key acts on the arrow, and the line does not open its details.
        if (event.key === 'Enter' || event.key === ' ') {
          event.stopPropagation();
        }
      }}
      sx={{
        flex: 'none',
        width: 24,
        height: 24,
        padding: 0,
        // The name starts where the names of the other tasks do.
        marginLeft: 'calc(var(--space-2) * -1)',
        color: 'var(--text-secondary)',
        '& .MuiSvgIcon-root': { fontSize: 20 },
      }}
    >
      {open ? <ExpandMoreOutlined /> : <ChevronRightOutlined />}
    </IconButton>
  );
}

/** "Hoje": brings today into view in the Linha do tempo (ADR-0071). */
function TodayButton({ disabled, onClick }: { disabled: boolean; onClick: () => void }) {
  const { t } = useTranslation();
  return (
    <Button
      variant="outlined"
      size="small"
      startIcon={<TodayOutlined />}
      disabled={disabled}
      onClick={onClick}
    >
      {t('projects.timeline.today')}
    </Button>
  );
}

/**
 * "Nova tarefa": a task or a milestone, its name, phase, the task it goes into and start, and, for a task, the duration
 * and the owner, and what it depends on (ADR-0018, ADR-0019, ADR-0088). A task starts at 0%. "Nova subtarefa" opens it
 * inside the task.
 */
function NewItemDialog({
  project,
  schedule,
  parentId,
  onClose,
  onCreated,
}: {
  project: Project;
  schedule: Schedule;
  /** The task the new item goes into. */
  parentId?: string;
  onClose: () => void;
  onCreated: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const parent = parentId === undefined ? undefined : findItem(schedule, parentId)?.item;
  const firstPhase = parent?.phaseId ?? schedule.phases[0]?.id ?? '';
  const {
    control,
    handleSubmit,
    setError,
    setValue,
    formState: { errors },
  } = useForm<ScheduleItemValues>({
    resolver: zodResolver(scheduleItemSchema),
    defaultValues: newItemValues(firstPhase, parent?.id),
  });
  const kind = useWatch({ control, name: 'kind' });

  const create = handleSubmit(async (values) => {
    try {
      const { error, response } = await api.POST('/api/projects/{id}/schedule/items', {
        params: { path: { id: project.id } },
        body: { kind: values.kind, ...bodyOf(values), progress: 0 },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onCreated(
          t(`projects.schedule.messages.itemAdded.${values.kind}`, { name: values.name.trim() }),
        );
        return;
      }
      if (!showRefusal(error, setError)) {
        setError('root', { message: failureOf(error, response.status, t) });
      }
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(
        parent === undefined ? 'projects.schedule.form.title' : 'projects.schedule.form.newSubtask',
      )}
      onClose={onClose}
      onSubmit={(event) => create(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.schedule.form.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(`projects.schedule.form.create.${kind}`)}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <SegmentedControl<ScheduleItemKind>
          label={t('projects.schedule.form.kind')}
          value={kind}
          options={[
            { value: 'Task', label: t('projects.schedule.kinds.Task') },
            { value: 'Milestone', label: t('projects.schedule.kinds.Milestone') },
          ]}
          onChange={(value) => {
            setValue('kind', value);
          }}
        />
        <ScheduleItemFields
          control={control}
          errors={errors}
          kind={kind}
          schedule={schedule}
          project={project}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}

/** The heading of the details of a task or milestone: its kind, its name and its phase. */
function ItemHeading({ item, phase }: { item: ScheduleItem; phase: string }) {
  const { t } = useTranslation();
  return (
    <Box sx={{ minWidth: 0 }}>
      <Box
        component="span"
        sx={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 'var(--space-1)',
          fontSize: 13,
          lineHeight: '18px',
          color: 'var(--text-secondary)',
        }}
      >
        {item.kind === 'Milestone' && <FlagOutlined aria-hidden sx={{ fontSize: 16 }} />}
        {t(`projects.schedule.kinds.${item.kind}`)}
        {' · '}
        {phase}
      </Box>
      <Box
        component="h2"
        sx={{
          margin: '2px 0 0',
          fontSize: 18,
          lineHeight: '26px',
          fontWeight: 500,
          color: 'var(--text)',
          overflowWrap: 'anywhere',
        }}
      >
        {item.name}
      </Box>
      {(showsDelay(item) || item.deadline !== null) && (
        <Box
          sx={{
            marginTop: 'var(--space-1)',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'flex-start',
          }}
        >
          {showsDelay(item) && <DueDate state="overdue">{lateText(item, t)}</DueDate>}
          <DeadlineStatus item={item} />
        </Box>
      )}
    </Box>
  );
}

/**
 * The details of a task or milestone for who edits the schedule: the progress of the task or the state of the
 * milestone, each saved by its own action (ADR-0086); for a task right in the phase, its Subtarefas, with "Nova
 * subtarefa"; the same fields as "Nova tarefa", with the end the API computed; "Salvar", which says how many items
 * after it were pushed (ADR-0019), and keeps the progress as it is; and the removal, with confirmation. A task with
 * subtasks shows its dates and progress as computed from them, and changes only its name, phase and owner (ADR-0088).
 */
function ItemEditor({
  project,
  schedule,
  item,
  onSaved,
  onRemoved,
  onNewSubtask,
  onOpen,
}: {
  project: Project;
  schedule: Schedule;
  item: ScheduleItem;
  onSaved: (message: string) => void;
  onRemoved: (message: string) => void;
  onNewSubtask: () => void;
  /** Opens the details of another item, as a subtask of this one. */
  onOpen: (itemId: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [removing, setRemoving] = useState(false);
  const [removePending, setRemovePending] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ScheduleItemValues>({
    resolver: zodResolver(scheduleItemSchema),
    defaultValues: valuesOfItem(item),
  });
  const phaseName = schedule.phases.find((phase) => phase.id === item.phaseId)?.name ?? '';
  const subtasks = itemsOf(schedule).filter((other) => other.parentId === item.id).length;
  const refreshed = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    // The hours of the tasks show their current names, and a removed task leaves them (ADR-0138).
    void queryClient.invalidateQueries({ queryKey: taskHoursQuery(project.id).queryKey });
  };

  const save = handleSubmit(async (values) => {
    try {
      const { data, error, response } = await api.PUT(
        '/api/projects/{id}/schedule/items/{itemId}',
        {
          params: { path: { id: project.id, itemId: item.id } },
          // Without the progress, which stays as it is (ADR-0086).
          body: { ...bodyOf(values), progress: null },
        },
      );
      if (response.ok) {
        refreshed();
        const saved = t(`projects.schedule.messages.itemSaved.${item.kind}`, {
          name: values.name.trim(),
        });
        const pushed = data?.pushed ?? 0;
        onSaved(
          pushed === 0 ? saved : t('projects.schedule.messages.pushed', { saved, count: pushed }),
        );
        return;
      }
      if (!showRefusal(error, setError)) {
        setError('root', { message: failureOf(error, response.status, t) });
      }
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  const remove = async () => {
    setRemovePending(true);
    try {
      const { error, response } = await api.DELETE('/api/projects/{id}/schedule/items/{itemId}', {
        params: { path: { id: project.id, itemId: item.id } },
      });
      setRemoving(false);
      if (response.ok) {
        refreshed();
        onRemoved(t(`projects.schedule.messages.itemRemoved.${item.kind}`, { name: item.name }));
        return;
      }
      setError('root', { message: failureOf(error, response.status, t) });
    } catch {
      setRemoving(false);
      setError('root', { message: t('projects.schedule.messages.failed') });
    } finally {
      setRemovePending(false);
    }
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      <ItemHeading item={item} phase={phaseName} />
      {item.kind === 'Task' ? (
        <TaskProgress project={project} item={item} onDone={onSaved} subtasks={subtasks} />
      ) : (
        <MilestoneState project={project} item={item} canEdit onDone={onSaved} />
      )}
      {item.kind === 'Task' && item.parentId === null && (
        <SubtaskSection item={item} schedule={schedule} onOpen={onOpen} onNew={onNewSubtask} />
      )}
      {item.hasSubtasks && <SummaryDates item={item} />}
      <OwnerAwayLine item={item} />
      <SlackLine item={item} />
      <BaselineSection item={item} schedule={schedule} />
      {item.kind === 'Task' && <ItemHours project={project} item={item} onSaved={onSaved} />}
      <Form
        aria-label={item.name}
        onSubmit={(event) => save(event)}
        sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
      >
        <ScheduleItemFields
          control={control}
          errors={errors}
          kind={item.kind}
          schedule={schedule}
          project={project}
          itemId={item.id}
          summary={item.hasSubtasks}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <Button
            variant="text"
            startIcon={<DeleteOutlined />}
            onClick={() => {
              setRemoving(true);
            }}
            sx={{ color: 'var(--status-danger-text)' }}
          >
            {t(`projects.schedule.form.remove.${item.kind}`)}
          </Button>
          <Box sx={{ flex: 1 }} />
          <Button type="submit" variant="contained">
            {t('projects.schedule.form.save')}
          </Button>
        </Box>
      </Form>
      <Dialog
        open={removing}
        alert
        title={t(`projects.schedule.removeItem.title.${item.kind}`)}
        onClose={() => {
          setRemoving(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(false);
              }}
            >
              {t('projects.schedule.removeItem.back')}
            </Button>
            <Button
              variant="contained"
              color="error"
              loading={removePending}
              onClick={() => remove()}
            >
              {t(`projects.schedule.form.remove.${item.kind}`)}
            </Button>
          </>
        }
      >
        {subtasks > 0
          ? t('projects.schedule.removeItem.textSummary', { name: item.name, count: subtasks })
          : t('projects.schedule.removeItem.text', { name: item.name })}
      </Dialog>
    </Box>
  );
}

/** The hours of a task in its details, for the Administrator and the manager of the project (ADR-0138, ADR-0144). */
function ItemHours({
  project,
  item,
  onSaved,
}: {
  project: Project;
  item: ScheduleItem;
  onSaved: (message: string) => void;
}) {
  const queryClient = useQueryClient();
  const { session } = useSession();
  const sees = seesTaskHours(project, session.status === 'signed-in' ? session.user : undefined);
  const { data } = useQuery({ ...taskHoursQuery(project.id), enabled: sees });
  if (!sees) {
    return null;
  }
  return (
    <TaskHoursSection
      projectId={project.id}
      task={item}
      hours={data?.items.find((task) => task.itemId === item.id)}
      canChange={data?.canChangePlanned ?? false}
      onSaved={(message) => {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projectHours });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onSaved(message);
      }}
    />
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

/** The float of the item, for who edits the schedule (GanttB-1); nothing for a task with subtasks. */
function SlackLine({ item }: { item: ScheduleItem }) {
  const { t } = useTranslation();
  const text = slackText(item, t);
  return text === undefined ? null : (
    <Box component="dl" sx={detailsListSx}>
      <dt>{t('projects.schedule.details.slack')}</dt>
      <dd>{text}</dd>
    </Box>
  );
}

/** The dates of a task with subtasks, computed from them and not typed (ADR-0088). */
function SummaryDates({ item }: { item: ScheduleItem }) {
  const { t } = useTranslation();
  return (
    <Box component="dl" sx={detailsListSx}>
      <dt>{t('projects.schedule.details.start')}</dt>
      <dd>{formatDayText(item.start)}</dd>
      <dt>{t('projects.schedule.details.end')}</dt>
      <dd>{formatDayText(item.end)}</dd>
      <dt>{t('projects.schedule.details.duration')}</dt>
      <dd>{t('projects.schedule.duration', { count: item.duration })}</dd>
    </Box>
  );
}

/**
 * The details of a task or milestone for who does not edit the schedule (ADR-0043): read only, but for the progress of
 * a task the person is in charge of. A subtask says the task it is in, and a task with subtasks lists them, each
 * opening its own details (ADR-0088).
 */
function ItemDetails({
  project,
  item,
  phase,
  schedule,
  onDone,
  onOpen,
}: {
  project: Project;
  item: ScheduleItem;
  phase: SchedulePhase;
  schedule: Schedule;
  onDone: (message: string) => void;
  /** Opens the details of another item, as a subtask of this one. */
  onOpen: (itemId: string) => void;
}) {
  const { t } = useTranslation();
  const names = predecessorTexts(item, schedule, t);
  const task = item.kind === 'Task';
  const subtasks = itemsOf(schedule).filter((other) => other.parentId === item.id).length;
  const parent = item.parentId === null ? undefined : findItem(schedule, item.parentId)?.item;
  return (
    <Box
      component="section"
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
    >
      <ItemHeading item={item} phase={phase.name} />
      {task ? (
        <TaskProgress project={project} item={item} onDone={onDone} subtasks={subtasks} />
      ) : (
        <MilestoneState project={project} item={item} canEdit={false} onDone={onDone} />
      )}
      {subtasks > 0 && <SubtaskSection item={item} schedule={schedule} onOpen={onOpen} />}
      <OwnerAwayLine item={item} />
      <BaselineSection item={item} schedule={schedule} />
      <Box component="dl" sx={detailsListSx}>
        {parent !== undefined && (
          <>
            <dt>{t('projects.schedule.form.parent')}</dt>
            <dd>{parent.name}</dd>
          </>
        )}
        <dt>{t(task ? 'projects.schedule.details.start' : 'projects.schedule.details.day')}</dt>
        <dd>{formatDayText(item.start)}</dd>
        {task && (
          <>
            <dt>{t('projects.schedule.details.end')}</dt>
            <dd>{formatDayText(item.end)}</dd>
            <dt>{t('projects.schedule.details.duration')}</dt>
            <dd>{t('projects.schedule.duration', { count: item.duration })}</dd>
            <dt>{t('projects.schedule.details.owner')}</dt>
            <dd>
              {ownerText(item, project.client?.name ?? null, t) ?? t('projects.schedule.noOwner')}
            </dd>
          </>
        )}
        {item.deadline !== null && (
          <>
            <dt>{t('projects.schedule.details.deadline')}</dt>
            <dd>{formatDayText(item.deadline)}</dd>
          </>
        )}
        {slackText(item, t) !== undefined && (
          <>
            <dt>{t('projects.schedule.details.slack')}</dt>
            <dd>{slackText(item, t)}</dd>
          </>
        )}
        {subtasks === 0 && (
          <>
            <dt>{t('projects.schedule.details.dependsOn')}</dt>
            <dd>{names.length === 0 ? t('projects.schedule.none') : names.join(', ')}</dd>
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
    </Box>
  );
}
