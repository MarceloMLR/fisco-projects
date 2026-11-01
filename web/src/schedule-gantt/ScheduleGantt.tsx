import {
  forwardRef,
  type KeyboardEvent,
  type PointerEvent as ReactPointerEvent,
  useCallback,
  useEffect,
  useId,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import type { Schedule } from '../api/client';
import type { DayValue } from '../format/dates';
import { ganttPeriod, type GanttScale, ganttSpan, ganttUnitWidth } from './ganttLayout';
import { type GanttRow, ganttRowsOf, type GanttTarget, shownRows } from './ganttRows';
import { type TimelineAnswer, TimelineChart } from './TimelineChart';
import { type TimelineColumns, TimelineTable } from './TimelineTable';
import { timelineGeometry, timelineSizes } from './timelineGeometry';
import './ScheduleGantt.css';

export type { TimelineAnswer } from './TimelineChart';

/** The width of the table beside the timeline, before the person drags the divider (ADR-0071). */
const listWidth = { compact: 320, detailed: 640 } as const;
/** Below this width (a phone), the table keeps only the names, in less than half of the space. */
const narrowWidth = 640;
const narrowShare = 0.45;
/** The divider goes from this width of the table to half of the timeline, by the pointer or by the keyboard. */
const minListWidth = 160;
const dividerStep = 16;

export interface ScheduleGanttProps {
  schedule: Schedule;
  scale: GanttScale;
  /** Who edits the schedule, on a computer, drags, stretches and links bars (ADR-0021, ADR-0043). */
  editable: boolean;
  /** The full table beside the timeline, as Expandir shows it (ADR-0071). */
  detailed?: boolean;
  selectedId?: string;
  /** The dates of the baseline as a thin bar under each task (ADR-0021); only with a baseline saved. */
  showBaseline?: boolean;
  /** The tasks and milestones of the critical path in red, with the part done in green (ADR-0021, ADR-0067). */
  showCritical?: boolean;
  /** The name of the client of the project, for the tasks in its charge (ADR-0102). */
  clientName?: string | null;
  onChoose: (target: GanttTarget) => void;
  /** The promise of the API's answer keeps the bar where it was dropped until the schedule comes back. */
  onMove: (itemId: string, start: DayValue) => TimelineAnswer;
  onResize: (itemId: string, start: DayValue, end: DayValue) => TimelineAnswer;
  onLink: (predecessorId: string, successorId: string) => TimelineAnswer;
}

export interface ScheduleGanttHandle {
  /** Brings today into view. */
  goToToday: () => void;
}

/**
 * The Linha do tempo of a schedule (ADR-0071, ADR-0134), drawn by FiscoProjects from the Gantt of the design system:
 * the table as a tree on the left, the chart on the right, scrolling together up and down, and only the chart sideways.
 * It draws the dates the API computed and never moves anything itself: a drag, a pulled edge or a link becomes an
 * event, and the schedule that comes back from the API is drawn again.
 */
export const ScheduleGantt = forwardRef<ScheduleGanttHandle, ScheduleGanttProps>(
  function ScheduleGantt(
    {
      schedule,
      scale,
      editable,
      detailed = false,
      selectedId,
      showBaseline = false,
      showCritical = false,
      clientName = null,
      onChoose,
      onMove,
      onResize,
      onLink,
    },
    ref,
  ) {
    const { t } = useTranslation();
    const idPrefix = useId();
    const scroller = useRef<HTMLDivElement>(null);
    const [size, setSize] = useState({ width: 0, height: 0 });
    const columns: TimelineColumns = detailed
      ? 'detailed'
      : size.width > 0 && size.width < narrowWidth
        ? 'narrow'
        : 'compact';
    const defaultList =
      columns === 'narrow' ? Math.round(size.width * narrowShare) : listWidth[columns];
    const [draggedList, setDraggedList] = useState<{ columns: TimelineColumns; width: number }>();
    const maxList = Math.max(defaultList, Math.floor(size.width / 2));
    const list = draggedList?.columns === columns ? draggedList.width : defaultList;

    const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(() => new Set());
    const [active, setActive] = useState<string>();
    const rows = useMemo(
      () => ganttRowsOf(schedule, { critical: showCritical }),
      [schedule, showCritical],
    );
    const shown = useMemo(() => shownRows(rows, collapsed), [rows, collapsed]);
    const deadlines = useMemo(
      () => rows.flatMap((row) => (row.deadline === undefined ? [] : [row.deadline.day])),
      [rows],
    );
    const geometry = useMemo(() => {
      if (schedule.start === null || schedule.end === null) {
        return undefined;
      }
      const span = ganttSpan(schedule.start, schedule.end, deadlines);
      const period = ganttPeriod(span.start, span.end, scale);
      return timelineGeometry(
        period,
        scale,
        ganttUnitWidth(scale, period.units, size.width - list),
      );
    }, [schedule.start, schedule.end, deadlines, scale, size.width, list]);

    useEffect(() => {
      const element = scroller.current;
      if (element === null) {
        return undefined;
      }
      const measure = () => {
        setSize((current) =>
          current.width === element.clientWidth && current.height === element.clientHeight
            ? current
            : { width: element.clientWidth, height: element.clientHeight },
        );
      };
      measure();
      if (typeof ResizeObserver === 'undefined') {
        return undefined;
      }
      const observer = new ResizeObserver(measure);
      observer.observe(element);
      return () => {
        observer.disconnect();
      };
    }, []);

    useImperativeHandle(
      ref,
      () => ({
        goToToday: () => {
          const element = scroller.current;
          if (element === null || geometry === undefined) {
            return;
          }
          // Today in the middle of the part of the chart in view, beside the table that stays on the left.
          const x = geometry.xOf(schedule.today) + geometry.dayWidth(schedule.today) / 2;
          element.scrollLeft = Math.max(0, Math.round(x - (element.clientWidth - list) / 2));
        },
      }),
      [geometry, schedule.today, list],
    );

    const toggle = useCallback((key: string) => {
      setCollapsed((current) => {
        const next = new Set(current);
        if (!next.delete(key)) {
          next.add(key);
        }
        return next;
      });
    }, []);

    const choose = useCallback(
      (row: GanttRow) => {
        setActive(row.key);
        onChoose(row.target);
      },
      [onChoose],
    );

    const resizeList = (width: number) => {
      setDraggedList({
        columns,
        width: Math.round(Math.min(Math.max(width, minListWidth), maxList)),
      });
    };

    return (
      <div
        className={detailed ? 'fp-gantt fp-gantt--detailed' : 'fp-gantt'}
        data-scale={scale}
        data-editable={String(editable)}
        data-detailed={String(detailed)}
        data-baseline={String(showBaseline)}
        data-critical={String(showCritical)}
      >
        <div ref={scroller} className="fp-gantt__scroller">
          {geometry !== undefined && (
            <div
              className="fp-gantt__grid"
              style={{
                gridTemplateColumns: `${String(list)}px minmax(${String(geometry.width)}px, 1fr)`,
              }}
            >
              <div className="fp-gantt__list">
                <TimelineTable
                  rows={shown}
                  collapsed={collapsed}
                  columns={columns}
                  clientName={clientName}
                  selectedId={selectedId}
                  active={active}
                  idPrefix={idPrefix}
                  onActivate={setActive}
                  onToggle={toggle}
                  onChoose={choose}
                />
                <Divider
                  label={t('projects.timeline.divider')}
                  width={list}
                  min={minListWidth}
                  max={maxList}
                  onChange={resizeList}
                />
              </div>
              <TimelineChart
                rows={shown}
                geometry={geometry}
                minHeight={size.height - timelineSizes.head}
                today={schedule.today}
                nonWorkingDays={schedule.nonWorkingDays}
                editable={editable}
                selectedId={selectedId}
                showBaseline={showBaseline && schedule.baseline !== null}
                onChoose={choose}
                onMove={onMove}
                onResize={onResize}
                onLink={onLink}
              />
            </div>
          )}
        </div>
      </div>
    );
  },
);

/**
 * The line between the table and the chart, which a person drags to give the table more or less room, or moves by the
 * arrows of the keyboard.
 */
function Divider({
  label,
  width,
  min,
  max,
  onChange,
}: {
  label: string;
  width: number;
  min: number;
  max: number;
  onChange: (width: number) => void;
}) {
  const [dragging, setDragging] = useState(false);

  const press = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) {
      return;
    }
    event.preventDefault();
    const origin = event.clientX;
    const move = (moving: PointerEvent) => {
      onChange(width + moving.clientX - origin);
    };
    const stop = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', stop);
      window.removeEventListener('pointercancel', stop);
      setDragging(false);
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', stop);
    window.addEventListener('pointercancel', stop);
    setDragging(true);
  };

  const keyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const next =
      event.key === 'ArrowLeft'
        ? width - dividerStep
        : event.key === 'ArrowRight'
          ? width + dividerStep
          : event.key === 'Home'
            ? min
            : event.key === 'End'
              ? max
              : undefined;
    if (next !== undefined) {
      event.preventDefault();
      onChange(next);
    }
  };

  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label={label}
      aria-valuenow={width}
      aria-valuemin={min}
      aria-valuemax={max}
      tabIndex={0}
      className={dragging ? 'fp-gantt__divider fp-gantt__divider--dragging' : 'fp-gantt__divider'}
      onPointerDown={press}
      onKeyDown={keyDown}
    />
  );
}

export default ScheduleGantt;
