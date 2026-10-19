import FlagOutlined from '@mui/icons-material/FlagOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import { registerLicense, Telemetry } from '@syncfusion/ej2-base';
import {
  type ActionBeginArgs,
  ColumnDirective,
  ColumnsDirective,
  DayMarkers,
  Edit,
  GanttComponent,
  type IGanttData,
  Inject,
  type ITaskbarEditedEventArgs,
  type RowSelectEventArgs,
  Selection,
} from '@syncfusion/ej2-react-gantt';
import '@syncfusion/ej2-material3-theme/styles/gantt/index.css';
import {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import type { Schedule } from '../api/client';
import { type DayValue, formatDayMonthText } from '../format/dates';
import { DueDate } from '../components/DueDate';
import { milestoneStatus } from '../projects/schedule';
import { ganttPeriod, type GanttScale, ganttUnitWidth } from './ganttLayout';
import {
  dateOfDay,
  dayOfDate,
  type GanttRow,
  ganttRowsOf,
  type GanttTarget,
  lastDayOf,
} from './ganttRows';
import { colorSchemeAttribute } from '../theme/theme';
import './ScheduleGantt.css';
import { applySyncfusionPalette } from './syncfusionPalette';

// Syncfusion would send the use of its components to a service of its own; nothing about the people of the
// installation leaves it (ADR-0058).
Telemetry.disable();

/** The license is registered once, before the first timeline is drawn (ADR-0087). */
let registered: string | undefined;

function register(license: string | null) {
  if (license !== null && license !== registered) {
    registerLicense(license);
    registered = license;
  }
}

const listWidth = { compact: 320, detailed: 560 } as const;
const columnWidth = { progress: 96, day: 64, owner: 136 } as const;
const splitterWidth = 4;
/** What a bar may do in the timeline: move, or have an edge pulled; never its progress (ADR-0086). */
const barActions = new Set(['ChildDrag', 'MilestoneDrag', 'LeftResizing', 'RightResizing']);
/** Below this width (a phone), the table keeps only the names, in less than half of the space. */
const narrowWidth = 640;
const narrowShare = 0.45;

export interface ScheduleGanttProps {
  schedule: Schedule;
  scale: GanttScale;
  /** Who edits the schedule, on a computer, drags, stretches and links bars (ADR-0021, ADR-0043). */
  editable: boolean;
  /** The full table beside the timeline, as Expandir shows it (ADR-0071). */
  detailed?: boolean;
  selectedId?: string;
  /** The Syncfusion license of the installation (ADR-0087); none shows the notice of the trial. */
  license: string | null;
  onChoose: (target: GanttTarget) => void;
  onMove: (itemId: string, start: DayValue) => void;
  onResize: (itemId: string, start: DayValue, end: DayValue) => void;
  onLink: (predecessorId: string, successorId: string) => void;
}

export interface ScheduleGanttHandle {
  /** Brings today into view. */
  goToToday: () => void;
}

const monthFormat = new Intl.DateTimeFormat('pt-BR', { month: 'short' });

/** "Out", from "out.": the month as the scale of the design system writes it. */
function monthText(date: Date): string {
  const text = monthFormat.format(date).replace('.', '');
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** The texts of the scale: "Out 2026" over the days or the weeks, the year over the months, and each unit below. */
function scaleText(date?: Date, _format?: string, tier?: string, mode?: string): string {
  if (date === undefined) {
    return '';
  }
  if (tier === 'topTier') {
    return mode === 'Year'
      ? String(date.getFullYear())
      : `${monthText(date)} ${String(date.getFullYear())}`;
  }
  if (mode === 'Month') {
    return monthText(date);
  }
  return mode === 'Week' ? formatDayMonthText(dayOfDate(date)) : String(date.getDate());
}

/** The line of FiscoProjects behind a record of the Gantt, which keeps it as its task data. */
function lineOf(data: unknown): GanttRow | undefined {
  const record = data as { taskData?: GanttRow; target?: GanttTarget } | undefined;
  return record?.taskData ?? (record?.target === undefined ? undefined : (record as GanttRow));
}

const rowOf = (data: unknown) => lineOf(data)?.target;

/**
 * The Linha do tempo of a schedule (ADR-0051, ADR-0071): the only component that uses the Syncfusion Gantt, with the
 * properties and events of FiscoProjects. It draws the dates the API computed and never moves anything itself: a
 * drag, a stretch or a link becomes an event, and the schedule that comes back from the API is drawn again.
 */
export const ScheduleGantt = forwardRef<ScheduleGanttHandle, ScheduleGanttProps>(
  function ScheduleGantt(
    {
      schedule,
      scale,
      editable,
      detailed = false,
      selectedId,
      license,
      onChoose,
      onMove,
      onResize,
      onLink,
    },
    ref,
  ) {
    const { t } = useTranslation();
    register(license);
    const gantt = useRef<GanttComponent>(null);
    const area = useRef<HTMLDivElement>(null);
    const [width, setWidth] = useState(0);
    const narrow = !detailed && width > 0 && width < narrowWidth;
    const list = detailed
      ? listWidth.detailed
      : narrow
        ? Math.round(width * narrowShare)
        : listWidth.compact;
    const rows = useMemo(() => ganttRowsOf(schedule), [schedule]);
    const period = useMemo(
      () =>
        schedule.start === null || schedule.end === null
          ? undefined
          : ganttPeriod(schedule.start, schedule.end, scale),
      [schedule.start, schedule.end, scale],
    );
    const unit = ganttUnitWidth(scale, period?.units ?? 0, width - list - splitterWidth);

    // The colors follow the theme of the screens: again whenever the person switches between light and dark.
    useLayoutEffect(() => {
      const element = area.current;
      if (element === null) {
        return undefined;
      }
      applySyncfusionPalette(element);
      const observer = new MutationObserver(() => {
        applySyncfusionPalette(element);
      });
      observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: [colorSchemeAttribute],
      });
      return () => {
        observer.disconnect();
      };
    }, []);

    useEffect(() => {
      const element = area.current;
      if (element === null || typeof ResizeObserver === 'undefined') {
        return undefined;
      }
      const observer = new ResizeObserver(() => {
        setWidth(element.clientWidth);
      });
      observer.observe(element);
      setWidth(element.clientWidth);
      return () => {
        observer.disconnect();
      };
    }, []);

    useImperativeHandle(ref, () => ({
      goToToday: () => {
        gantt.current?.scrollToDate(dateOfDay(schedule.today).toString());
      },
    }));

    const timelineSettings = useMemo(
      () => ({
        timelineUnitSize: unit,
        showTooltip: false,
        weekStartDay: 1,
        topTier:
          scale === 'months'
            ? { unit: 'Year' as const, formatter: scaleText }
            : { unit: 'Month' as const, formatter: scaleText },
        bottomTier: {
          unit:
            scale === 'days'
              ? ('Day' as const)
              : scale === 'weeks'
                ? ('Week' as const)
                : ('Month' as const),
          formatter: scaleText,
        },
      }),
      [unit, scale],
    );
    const holidays = useMemo(
      () =>
        schedule.nonWorkingDays.map((day) => ({
          from: dateOfDay(day),
          to: dateOfDay(day),
          cssClass: 'fp-gantt-nonworking',
        })),
      [schedule.nonWorkingDays],
    );
    const markers = useMemo(
      () => [
        {
          day: dateOfDay(schedule.today),
          label: t('projects.timeline.today'),
          cssClass: 'fp-gantt-today',
          top: '4px',
        },
      ],
      [schedule.today, t],
    );

    const choose = useCallback(
      (args: RowSelectEventArgs) => {
        const target = rowOf(args.data);
        if (target !== undefined) {
          onChoose(target);
        }
      },
      [onChoose],
    );

    /** What the bar being edited does: moves, or has an edge pulled. */
    const editAction = useRef<string | undefined>(undefined);

    // Only the bars of tasks and milestones move; the phases follow them, and the progress changes in the details
    // (ADR-0086).
    const editing = useCallback((args: ITaskbarEditedEventArgs) => {
      const target = rowOf(args.data);
      if (target?.kind !== 'item' || !barActions.has(args.taskBarEditAction ?? '')) {
        args.cancel = true;
        editAction.current = undefined;
        return;
      }
      editAction.current = args.taskBarEditAction;
    }, []);

    const beforeChange = useCallback(
      (args: ActionBeginArgs) => {
        const request = args as ActionBeginArgs & {
          requestType?: string;
          action?: string;
          data?: IGanttData;
        };
        // The Gantt never edits nor saves anything itself (ADR-0051): the details open in the drawer (ADR-0081), and
        // a moved bar or a new dependency becomes an event, saved by the API, which sends the schedule again.
        if (
          request.requestType === 'beforeOpenEditDialog' ||
          request.requestType === 'beforeOpenAddDialog'
        ) {
          args.cancel = true;
          return;
        }
        if (request.requestType !== 'beforeSave') {
          return;
        }
        args.cancel = true;
        const action = editAction.current;
        editAction.current = undefined;
        const line = lineOf(request.data);
        if (line?.target.kind !== 'item') {
          return;
        }
        const id = line.target.item.id;
        const properties = request.data?.ganttProperties;
        if (request.action !== 'DrawConnectorLine' && action !== undefined) {
          if (properties?.startDate === undefined) {
            return;
          }
          const start = dayOfDate(properties.startDate);
          if (action === 'LeftResizing' || action === 'RightResizing') {
            if (properties.endDate !== undefined) {
              onResize(id, start, lastDayOf(properties.endDate));
            }
          } else {
            onMove(id, start);
          }
          return;
        }
        const known = new Set(line.target.item.predecessorIds);
        const added = (properties?.predecessor ?? [])
          .filter((predecessor) => predecessor.to === String(line.key))
          .map((predecessor) => rows.find((row) => String(row.key) === predecessor.from)?.target)
          .find((target) => target?.kind === 'item' && !known.has(target.item.id));
        if (added?.kind === 'item') {
          onLink(added.item.id, id);
        }
      },
      [rows, onMove, onResize, onLink],
    );

    const selectedIndex = rows.findIndex(
      (row) =>
        (row.target.kind === 'item' ? row.target.item.id : row.target.phase.id) === selectedId,
    );

    return (
      <div
        ref={area}
        className={detailed ? 'fp-schedule-gantt fp-schedule-gantt--detailed' : 'fp-schedule-gantt'}
      >
        {width > 0 && period !== undefined && (
          <GanttComponent
            // The columns are read once: a phone turned sideways draws the timeline again.
            key={narrow ? 'narrow' : 'wide'}
            ref={gantt}
            dataSource={rows}
            height="100%"
            width="100%"
            taskFields={{
              id: 'key',
              name: 'name',
              startDate: 'start',
              endDate: 'end',
              progress: 'progress',
              parentID: 'parentKey',
              dependency: 'predecessor',
              milestone: 'milestone',
              cssClass: 'cssClass',
            }}
            // Manual: the bars stay in the dates of the API. Its own computing stays on, or the bars of the phases
            // lose their place.
            taskMode="Manual"
            autoCalculateDateScheduling
            includeWeekend
            dayWorkingTime={[{ from: 0, to: 24 }]}
            holidays={holidays}
            eventMarkers={markers}
            projectStartDate={dateOfDay(period.start)}
            projectEndDate={dateOfDay(period.end)}
            timelineSettings={timelineSettings}
            splitterSettings={{ position: `${String(list)}px` }}
            rowHeight={40}
            taskbarHeight={18}
            allowUnscheduledTasks
            allowSelection
            selectedRowIndex={selectedIndex}
            tooltipSettings={{ showTooltip: false }}
            editSettings={{ allowTaskbarEditing: editable, allowEditing: editable, mode: 'Dialog' }}
            readOnly={!editable}
            rowSelected={choose}
            taskbarEditing={editing}
            actionBegin={beforeChange}
            gridLines="Horizontal"
          >
            <ColumnsDirective>
              <ColumnDirective
                field="name"
                headerText={t('projects.timeline.columns.name')}
                template={(data: unknown) => <NameCell row={lineOf(data)} withDelay={narrow} />}
              />
              {detailed && (
                <ColumnDirective
                  field="start"
                  headerText={t('projects.timeline.columns.start')}
                  width={columnWidth.day}
                  template={(data: unknown) => {
                    const target = rowOf(data);
                    return target?.kind === 'item' ? formatDayMonthText(target.item.start) : '';
                  }}
                />
              )}
              {detailed && (
                <ColumnDirective
                  field="end"
                  headerText={t('projects.timeline.columns.end')}
                  width={columnWidth.day}
                  template={(data: unknown) => {
                    const target = rowOf(data);
                    return target?.kind === 'item' && target.item.kind === 'Task'
                      ? formatDayMonthText(target.item.end)
                      : '';
                  }}
                />
              )}
              {detailed && (
                <ColumnDirective
                  field="owner"
                  headerText={t('projects.timeline.columns.owner')}
                  width={columnWidth.owner}
                  template={(data: unknown) => {
                    const target = rowOf(data);
                    return target?.kind === 'item' ? (target.item.owner?.name ?? '') : '';
                  }}
                />
              )}
              {!narrow && (
                <ColumnDirective
                  field="progress"
                  headerText={t('projects.timeline.columns.progress')}
                  width={columnWidth.progress}
                  textAlign="Right"
                  template={(data: unknown) => <ProgressCell row={lineOf(data)} />}
                />
              )}
            </ColumnsDirective>
            <Inject services={[Edit, Selection, DayMarkers]} />
          </GanttComponent>
        )}
      </div>
    );
  },
);

/**
 * The name of a line, with a flag on a milestone. Without the column of the progress (a phone), a late task or
 * milestone says so beside the name (ADR-0065).
 */
function NameCell({ row, withDelay }: { row: GanttRow | undefined; withDelay: boolean }) {
  const { t } = useTranslation();
  if (row === undefined) {
    return null;
  }
  const target = row.target;
  const late =
    withDelay &&
    target.kind === 'item' &&
    (target.item.kind === 'Milestone' ? milestoneStatus(target.item) === 'late' : target.item.late);
  return (
    <span
      className={
        target.kind === 'phase' || target.item.hasSubtasks
          ? 'fp-gantt-name fp-gantt-name--phase'
          : 'fp-gantt-name'
      }
    >
      {target.kind === 'item' && target.item.kind === 'Milestone' && (
        <FlagOutlined
          titleAccess={t('projects.schedule.kinds.Milestone')}
          className="fp-gantt-name__icon"
        />
      )}
      <span className="fp-gantt-name__text" title={row.name}>
        {row.name}
      </span>
      {late && (
        <WarningAmberOutlined
          titleAccess={t('projects.schedule.late')}
          className="fp-gantt-progress__icon fp-gantt-name__late"
        />
      )}
    </span>
  );
}

/** The progress of a line, or the state of a milestone; a late task says so by icon, color and text. */
function ProgressCell({ row }: { row: GanttRow | undefined }) {
  const { t } = useTranslation();
  if (row === undefined) {
    return null;
  }
  const target = row.target;
  if (target.kind === 'item' && target.item.kind === 'Milestone') {
    const status = milestoneStatus(target.item);
    return (
      <DueDate state={status === 'reached' ? 'met' : status === 'late' ? 'overdue' : 'onTrack'}>
        {t(`projects.timeline.milestone.${status}`)}
      </DueDate>
    );
  }
  const late = target.kind === 'item' && target.item.late;
  const progress = target.kind === 'item' ? target.item.progress : target.phase.progress;
  const empty = target.kind === 'phase' && target.phase.start === null;
  return (
    <span className={late ? 'fp-gantt-progress fp-gantt-progress--late' : 'fp-gantt-progress'}>
      {late && (
        <WarningAmberOutlined
          titleAccess={t('projects.schedule.late')}
          className="fp-gantt-progress__icon"
        />
      )}
      {empty ? '' : `${String(progress)}%`}
    </span>
  );
}

export default ScheduleGantt;
