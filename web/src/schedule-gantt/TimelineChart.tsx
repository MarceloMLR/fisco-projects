import {
  type MouseEvent,
  type PointerEvent as ReactPointerEvent,
  type ReactNode,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import { daysBetween, type DayValue, formatDayMonthText } from '../format/dates';
import type { GanttRow } from './ganttRows';
import {
  type DragMode,
  draggedDates,
  scaleTiers,
  type TimelineGeometry,
  timelineSizes,
} from './timelineGeometry';

/** What the screen answers to a move, a pulled edge or a link: nothing, or the promise of the API's answer. */
export type TimelineAnswer = Promise<unknown> | undefined;

const {
  row: rowHeight,
  head: headHeight,
  tier: tierHeight,
  bar: barHeight,
  barTop,
} = timelineSizes;
const corner = timelineSizes.milestone;
/** Below this, a press is a click: it chooses the line instead of moving the bar. */
const clickSlop = 3;
/** The edges of a bar that pull its dates, half inside and half outside it. */
const handleWidth = 6;
/** The point at the end of a bar that links it to another one, past the end. */
const connectorGap = 6;
const connectorRadius = 4;
/** About how wide a letter of a label is, at 12 px, to keep the label inside the timeline. */
const letterWidth = 6.6;

/** A bar being dragged, or dropped and waiting for the API: drawn at these dates meanwhile. */
interface Draft {
  key: string;
  start: DayValue;
  end: DayValue;
  token: number;
}

/** The line from the end of a bar to the pointer, while a link is drawn. */
interface Link {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

const round = (value: number) => Math.round(value * 100) / 100;
const classes = (...names: (string | false | undefined)[]) => names.filter(Boolean).join(' ');

export interface TimelineChartProps {
  rows: readonly GanttRow[];
  geometry: TimelineGeometry;
  /** The least height of the lines, so the grid goes down to the end of the area (ADR-0071). */
  minHeight: number;
  today: DayValue;
  nonWorkingDays: readonly DayValue[];
  editable: boolean;
  selectedId?: string;
  showBaseline: boolean;
  onChoose: (row: GanttRow) => void;
  onMove: (itemId: string, start: DayValue) => TimelineAnswer;
  onResize: (itemId: string, start: DayValue, end: DayValue) => TimelineAnswer;
  onLink: (predecessorId: string, successorId: string) => TimelineAnswer;
}

/**
 * The chart of the timeline, as the Gantt of the design system draws it: the scale, the days off, the lines, the
 * phases, the tasks with the part done, the milestones, the baseline, the arrows of the dependencies, the deadlines
 * (GanttA-2) and today. Who
 * edits drags a bar, pulls its edges and links its end to another bar; the bar stays where it was dropped until the API
 * answers, and the schedule that comes back is drawn (ADR-0019, ADR-0051, ADR-0134).
 */
export function TimelineChart({
  rows,
  geometry,
  minHeight,
  today,
  nonWorkingDays,
  editable,
  selectedId,
  showBaseline,
  onChoose,
  onMove,
  onResize,
  onLink,
}: TimelineChartProps) {
  const { t } = useTranslation();
  const body = useRef<SVGSVGElement>(null);
  const [draft, setDraft] = useState<Draft>();
  const [link, setLink] = useState<Link>();
  const [pressing, setPressing] = useState<DragMode | 'link'>();
  const tokens = useRef(0);
  const swallowingClick = useRef(false);
  const stopFollowing = useRef<(() => void) | undefined>(undefined);
  useEffect(
    () => () => {
      stopFollowing.current?.();
    },
    [],
  );

  const { width, period } = geometry;
  const height = Math.max(rows.length * rowHeight, minHeight);
  const tiers = useMemo(() => scaleTiers(geometry, today), [geometry, today]);
  const centerOf = (day: DayValue) => geometry.xOf(day) + geometry.dayWidth(day) / 2;
  const datesOf = (row: GanttRow) =>
    draft?.key === row.key
      ? draft
      : row.start === undefined || row.end === undefined
        ? undefined
        : { start: row.start, end: row.end };

  const clear = (token: number) => {
    setDraft((current) => (current?.token === token ? undefined : current));
  };

  /** The pointer, followed in the whole window while a bar is dragged or linked. */
  const follow = (
    mode: DragMode | 'link',
    move: (event: PointerEvent) => void,
    up: (event: PointerEvent) => void,
    cancel: () => void,
  ) => {
    stopFollowing.current?.();
    const stop = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', release);
      window.removeEventListener('pointercancel', abandon);
      window.removeEventListener('keydown', escape);
      stopFollowing.current = undefined;
      setPressing(undefined);
    };
    const release = (event: PointerEvent) => {
      stop();
      up(event);
    };
    const abandon = () => {
      stop();
      cancel();
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        abandon();
      }
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', release);
    window.addEventListener('pointercancel', abandon);
    window.addEventListener('keydown', escape);
    stopFollowing.current = stop;
    setPressing(mode);
  };

  /** The click the browser sends after a drag does not choose the line. */
  const swallowClick = () => {
    swallowingClick.current = true;
    window.setTimeout(() => {
      swallowingClick.current = false;
    }, 0);
  };

  const pressBar = (event: ReactPointerEvent<SVGElement>, row: GanttRow, mode: DragMode) => {
    const { start, end } = row;
    if (event.button !== 0 || start === undefined || end === undefined) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    const origin = { x: event.clientX, y: event.clientY };
    const day = geometry.dayWidth(mode === 'end' ? end : start);
    tokens.current += 1;
    const token = tokens.current;
    let moved = false;
    let dates = { start, end };
    follow(
      mode,
      (moving) => {
        if (
          !moved &&
          Math.abs(moving.clientX - origin.x) < clickSlop &&
          Math.abs(moving.clientY - origin.y) < clickSlop
        ) {
          return;
        }
        moved = true;
        dates = draggedDates(mode, start, end, Math.round((moving.clientX - origin.x) / day));
        setDraft({ key: row.key, ...dates, token });
      },
      () => {
        if (!moved) {
          // A click: the one the browser sends next chooses the line.
          return;
        }
        swallowClick();
        if (dates.start === start && dates.end === end) {
          clear(token);
          return;
        }
        const answer =
          mode === 'move'
            ? onMove(row.key, dates.start)
            : onResize(row.key, dates.start, dates.end);
        // The bar stays where it was dropped until the schedule comes back from the API: at the new dates, or at the
        // old ones when the API refuses them. The schedule is drawn first, then the bar leaves the draft.
        void Promise.resolve(answer)
          .catch(() => undefined)
          .then(() => {
            window.setTimeout(() => {
              clear(token);
            }, 0);
          });
      },
      () => {
        clear(token);
      },
    );
  };

  const pressConnector = (
    event: ReactPointerEvent<SVGElement>,
    row: GanttRow,
    x: number,
    y: number,
  ) => {
    if (event.button !== 0) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    const pointOf = (moving: PointerEvent) => {
      const box = body.current?.getBoundingClientRect();
      return { x: moving.clientX - (box?.left ?? 0), y: moving.clientY - (box?.top ?? 0) };
    };
    setLink({ x1: x, y1: y, x2: x, y2: y });
    follow(
      'link',
      (moving) => {
        const point = pointOf(moving);
        setLink({ x1: x, y1: y, x2: point.x, y2: point.y });
      },
      (released) => {
        setLink(undefined);
        swallowClick();
        // Dropped on the line of another task or milestone; the screen says why a task with subtasks does not link.
        const target = rows[Math.floor(pointOf(released).y / rowHeight)];
        if (target?.target.kind === 'item' && target.key !== row.key) {
          void Promise.resolve(onLink(row.key, target.key)).catch(() => undefined);
        }
      },
      () => {
        setLink(undefined);
      },
    );
  };

  const click = (event: MouseEvent<SVGSVGElement>) => {
    if (swallowingClick.current) {
      return;
    }
    const line = (event.target as Element).closest('[data-row]');
    const row = line === null ? undefined : rows[Number(line.getAttribute('data-row'))];
    if (row !== undefined) {
      onChoose(row);
    }
  };

  const labelOf = (row: GanttRow, dates: { start: DayValue; end: DayValue }) => {
    const label = shapeLabelOf(row, dates);
    const deadline = row.deadline;
    if (deadline === undefined) {
      return label;
    }
    const day = formatDayMonthText(deadline.day);
    return deadline.missed
      ? t('projects.timeline.bars.missedDeadline', { label, day, count: deadline.days })
      : t('projects.timeline.bars.deadline', { label, day });
  };

  const shapeLabelOf = (row: GanttRow, dates: { start: DayValue; end: DayValue }) =>
    row.shape === 'milestone'
      ? t('projects.timeline.bars.milestone', {
          name: row.name,
          day: formatDayMonthText(dates.start),
        })
      : t(
          row.target.kind === 'phase'
            ? 'projects.timeline.bars.phase'
            : 'projects.timeline.bars.task',
          {
            name: row.name,
            start: formatDayMonthText(dates.start),
            end: formatDayMonthText(dates.end),
            progress: row.progress,
          },
        );

  const inPeriod = (day: DayValue) =>
    daysBetween(period.start, day) >= 0 && daysBetween(day, period.end) >= 0;
  const todayX = round(centerOf(today));
  const showsToday = inPeriod(today);
  const indexOf = new Map(rows.map((row, index) => [row.key, index]));

  const dependencies: ReactNode[] = [];
  const baselines: ReactNode[] = [];
  const deadlines: ReactNode[] = [];
  const labels: ReactNode[] = [];
  rows.forEach((row, index) => {
    const dates = datesOf(row);
    const top = index * rowHeight;
    // At the end of the day of the deadline, on the line of the item, over its bar (GanttA-2).
    if (row.deadline !== undefined && inPeriod(row.deadline.day)) {
      const x = round(geometry.xAfter(row.deadline.day));
      deadlines.push(
        <g
          key={row.key}
          className={classes('g-deadline', row.deadline.missed && 'g-deadline--missed')}
          data-day={row.deadline.day}
        >
          <line className="g-deadline-line" x1={x} x2={x} y1={top + 6} y2={top + rowHeight - 3} />
          <path
            className="g-deadline-head"
            d={`M${String(x - 4)} ${String(top + 2)} H${String(x + 4)} L${String(x)} ${String(top + 7)} Z`}
          />
        </g>,
      );
    }
    if (showBaseline && row.baseline !== undefined) {
      const x = geometry.xOf(row.baseline.start);
      baselines.push(
        <rect
          key={row.key}
          className="g-baseline"
          x={round(x)}
          y={top + barTop + barHeight + 3}
          width={round(geometry.xAfter(row.baseline.end) - x)}
          height={4}
          rx={1}
        />,
      );
    }
    if (dates === undefined || !row.ownDates) {
      return;
    }
    if (row.shape === 'milestone') {
      const x = centerOf(dates.start);
      const text = row.name;
      const right = x + 14 + text.length * letterWidth <= width - 4;
      labels.push(
        <text
          key={row.key}
          className="g-label"
          x={round(right ? x + 14 : x - 14)}
          y={top + rowHeight / 2 + 4}
          textAnchor={right ? 'start' : 'end'}
        >
          {text}
        </text>,
      );
    }
    // Each dependency from the end of the one before to the start of this one, with the elbow of the design system.
    for (const key of row.predecessors) {
      const from = indexOf.get(key);
      const before = from === undefined ? undefined : rows[from];
      const beforeDates = before === undefined ? undefined : datesOf(before);
      if (from === undefined || before === undefined || beforeDates === undefined) {
        continue;
      }
      const x1 = round(
        before.shape === 'milestone'
          ? centerOf(beforeDates.start) + corner
          : geometry.xAfter(beforeDates.end),
      );
      const y1 = from * rowHeight + rowHeight / 2;
      const x2 = round(
        row.shape === 'milestone' ? centerOf(dates.start) - corner : geometry.xOf(dates.start),
      );
      const y2 = top + rowHeight / 2;
      const middle = x1 + 6;
      const path =
        x2 - 12 >= middle
          ? `M${String(x1)} ${String(y1)} H${String(middle)} V${String(y2)} H${String(x2 - 5)}`
          : `M${String(x1)} ${String(y1)} H${String(middle)} V${String(top)} H${String(x2 - 12)} V${String(y2)} H${String(x2 - 5)}`;
      dependencies.push(
        <path key={`${key}-${row.key}`} className="g-dep" d={path} />,
        <path
          key={`${key}-${row.key}-head`}
          className="g-dep-head"
          d={`M${String(x2 - 6)} ${String(y2 - 4)} L${String(x2)} ${String(y2)} L${String(x2 - 6)} ${String(y2 + 4)} Z`}
        />,
      );
    }
  });

  const bars = rows.map((row, index) => {
    const dates = datesOf(row);
    if (dates === undefined) {
      return null;
    }
    const top = index * rowHeight;
    const selected = row.key === selectedId;
    const draggable = editable && row.ownDates;
    const label = labelOf(row, dates);
    const group = classes(
      'g-item',
      draggable && 'g-item--draggable',
      draft?.key === row.key && 'g-item--dragging',
    );
    if (row.shape === 'summary') {
      const x = round(geometry.xOf(dates.start));
      const w = round(geometry.xAfter(dates.end) - x);
      const y = top + rowHeight / 2 - 5;
      return (
        <g key={row.key} data-row={index} role="img" aria-label={label} className={group}>
          <path
            className="g-summary"
            d={`M${String(x)} ${String(y)} h${String(w)} v12 l-6 -4 H${String(x + 6)} l-6 4 Z`}
          />
        </g>
      );
    }
    if (row.shape === 'milestone') {
      const x = round(centerOf(dates.start));
      const y = top + rowHeight / 2;
      return (
        <g
          key={row.key}
          data-row={index}
          role="img"
          aria-label={label}
          className={group}
          onPointerDown={
            draggable
              ? (event) => {
                  pressBar(event, row, 'move');
                }
              : undefined
          }
        >
          <path
            className={classes('g-milestone', row.critical && 'g-milestone--critical')}
            d={`M${String(x)} ${String(y - corner)} L${String(x + corner)} ${String(y)} L${String(x)} ${String(y + corner)} L${String(x - corner)} ${String(y)} Z`}
          />
          {draggable && (
            <circle
              className="g-connector"
              cx={x + corner + connectorGap}
              cy={y}
              r={connectorRadius}
              onPointerDown={(event) => {
                pressConnector(event, row, x + corner + connectorGap, y);
              }}
            />
          )}
        </g>
      );
    }
    const x = round(geometry.xOf(dates.start));
    const w = round(geometry.xAfter(dates.end) - x);
    const y = top + barTop;
    return (
      <g
        key={row.key}
        data-row={index}
        role="img"
        aria-label={label}
        className={group}
        onPointerDown={
          draggable
            ? (event) => {
                pressBar(event, row, 'move');
              }
            : undefined
        }
      >
        <rect
          className={classes(
            'g-bar',
            row.critical && 'g-bar--critical',
            selected && 'g-bar--selected',
          )}
          x={x + 0.5}
          y={y + 0.5}
          width={Math.max(w - 1, 0)}
          height={barHeight - 1}
          rx={3}
        />
        {row.progress > 0 && (
          <rect
            className={classes('g-progress', row.critical && 'g-progress--critical')}
            x={x + 1}
            y={y + 1}
            width={round(Math.max(0, ((w - 2) * row.progress) / 100))}
            height={barHeight - 2}
            rx={2.5}
          />
        )}
        {draggable && (
          <>
            <rect
              className="g-handle"
              data-edge="start"
              x={x - handleWidth / 2}
              y={y}
              width={handleWidth}
              height={barHeight}
              onPointerDown={(event) => {
                pressBar(event, row, 'start');
              }}
            />
            <rect
              className="g-handle"
              data-edge="end"
              x={x + w - handleWidth / 2}
              y={y}
              width={handleWidth}
              height={barHeight}
              onPointerDown={(event) => {
                pressBar(event, row, 'end');
              }}
            />
            <circle
              className="g-connector"
              cx={x + w + connectorGap + connectorRadius}
              cy={y + barHeight / 2}
              r={connectorRadius}
              onPointerDown={(event) => {
                pressConnector(
                  event,
                  row,
                  x + w + connectorGap + connectorRadius,
                  y + barHeight / 2,
                );
              }}
            />
          </>
        )}
      </g>
    );
  });

  return (
    <div className="fp-gantt__chart" data-pressing={pressing}>
      <svg
        className="fp-gantt__scale"
        width={width}
        height={headHeight}
        viewBox={`0 0 ${String(width)} ${String(headHeight)}`}
        aria-hidden
      >
        <rect className="g-scale" x={0} y={0} width={width} height={headHeight} />
        {tiers.top.map((cell) => (
          <g key={`top-${String(cell.x)}`}>
            {cell.x > 0 && (
              <line
                className="g-scale-line"
                x1={round(cell.x) + 0.5}
                x2={round(cell.x) + 0.5}
                y1={0}
                y2={tierHeight}
              />
            )}
            <text className="g-scale-month" x={round(cell.x) + 6} y={17}>
              {cell.text}
            </text>
          </g>
        ))}
        <line
          className="g-scale-line"
          x1={0}
          x2={width}
          y1={tierHeight + 0.5}
          y2={tierHeight + 0.5}
        />
        {tiers.bottom.map((cell) => (
          <g key={`bottom-${String(cell.x)}`}>
            {geometry.scale !== 'days' && cell.x > 0 && (
              <line
                className="g-scale-line"
                x1={round(cell.x) + 0.5}
                x2={round(cell.x) + 0.5}
                y1={tierHeight}
                y2={headHeight}
              />
            )}
            <text
              className={classes('g-scale-text', cell.today && 'g-scale-today')}
              x={round(cell.x + cell.width / 2)}
              y={41}
              textAnchor="middle"
            >
              {cell.text}
            </text>
          </g>
        ))}
        <line
          className="g-scale-line"
          x1={0}
          x2={width}
          y1={headHeight - 0.5}
          y2={headHeight - 0.5}
        />
        {showsToday && (
          <>
            <line className="g-today" x1={todayX} x2={todayX} y1={26} y2={headHeight} />
            <rect className="g-today-flag" x={todayX - 18} y={4} width={36} height={17} rx={3} />
            <text className="g-today-label" x={todayX} y={16.5} textAnchor="middle">
              {t('projects.timeline.today')}
            </text>
          </>
        )}
      </svg>
      <svg
        ref={body}
        className="fp-gantt__body"
        width={width}
        height={height}
        viewBox={`0 0 ${String(width)} ${String(height)}`}
        role="group"
        aria-label={t('projects.timeline.label')}
        onClick={click}
      >
        {nonWorkingDays.filter(inPeriod).map((day) => (
          <rect
            key={day}
            className="g-nonworking"
            data-day={day}
            x={round(geometry.xOf(day))}
            y={0}
            width={round(geometry.dayWidth(day))}
            height={height}
          />
        ))}
        {rows.map((row, index) => (
          <rect
            key={row.key}
            className="g-hit"
            data-row={index}
            x={0}
            y={index * rowHeight}
            width={width}
            height={rowHeight}
          />
        ))}
        {rows.map((row, index) =>
          row.key === selectedId ? (
            <rect
              key={row.key}
              className="g-selected"
              x={0}
              y={index * rowHeight}
              width={width}
              height={rowHeight - 1}
            />
          ) : null,
        )}
        {rows.map((row, index) => (
          <line
            key={row.key}
            className="g-row-line"
            x1={0}
            x2={width}
            y1={(index + 1) * rowHeight - 0.5}
            y2={(index + 1) * rowHeight - 0.5}
          />
        ))}
        {baselines}
        {dependencies}
        {bars}
        <g aria-hidden>{deadlines}</g>
        <g aria-hidden>{labels}</g>
        {showsToday && <line className="g-today" x1={todayX} x2={todayX} y1={0} y2={height} />}
        {link !== undefined && (
          <line className="g-link" x1={link.x1} y1={link.y1} x2={link.x2} y2={link.y2} />
        )}
      </svg>
    </div>
  );
}
