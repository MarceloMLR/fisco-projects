import FlagOutlined from '@mui/icons-material/FlagOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import Box from '@mui/material/Box';
import { type ReactNode, useLayoutEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { OverviewRow, OverviewTimeline as Timeline } from '../api/client';
import { type DayValue, formatDayText } from '../format/dates';
import {
  dayNumber,
  dayOfNumber,
  ganttStripSx,
  type PrintScale,
  printScale,
  scaleSpans,
} from './schedulePrint';

const rowHeight = 30;
const headHeight = 48;
const barHeight = 16;
const barTop = Math.round((rowHeight - barHeight) / 2) - 2;

/** The width of an element, followed as the window changes; a width of reference where it cannot be measured. */
function useWidth() {
  const ref = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(600);
  useLayoutEffect(() => {
    const element = ref.current;
    if (element === null) {
      return;
    }
    const measure = () => {
      if (element.clientWidth > 0) {
        setWidth(element.clientWidth);
      }
    };
    measure();
    if (typeof ResizeObserver === 'undefined') {
      return;
    }
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => {
      observer.disconnect();
    };
  }, []);
  return [ref, width] as const;
}

const weekdayOf = (day: DayValue) => new Date(dayNumber(day) * 86_400_000).getUTCDay();

/**
 * The scale over the chart, as the Gantt of the design system draws it: the months on top and the days below, every
 * day while there is room and, when there is not, the Mondays, the first day and today; when even the Mondays do not
 * fit, the years over the months.
 */
function Scale({ scale, today }: { scale: PrintScale; today: DayValue }) {
  const { t } = useTranslation();
  const unit = scale.width / scale.days;
  const from = dayOfNumber(scale.first);
  const to = dayOfNumber(scale.first + scale.days - 1);
  const shapes: ReactNode[] = [];
  if (unit * 7 >= 24) {
    for (const span of scaleSpans(from, to, 'days').top) {
      const left = scale.x(span.from);
      if (left > 0) {
        shapes.push(
          <line
            key={`m-${span.from}`}
            className="g-scale-line"
            x1={left + 0.5}
            x2={left + 0.5}
            y1={0}
            y2={24}
          />,
        );
      }
      shapes.push(
        <text key={`t-${span.from}`} className="g-scale-month" x={left + 6} y={17}>
          {span.label}
        </text>,
      );
    }
    for (let index = 0; index < scale.days; index++) {
      const day = dayOfNumber(scale.first + index);
      const isToday = day === today;
      if (unit >= 17 || weekdayOf(day) === 1 || index === 0 || isToday) {
        shapes.push(
          <text
            key={`d-${day}`}
            className={isToday ? 'g-scale-text g-scale-today' : 'g-scale-text'}
            x={scale.x(day) + unit / 2}
            y={41}
            textAnchor="middle"
          >
            {Number(day.slice(8, 10))}
          </text>,
        );
      }
    }
  } else {
    const { top, bottom } = scaleSpans(from, to, 'months');
    for (const span of top) {
      shapes.push(
        <text key={`y-${span.from}`} className="g-scale-month" x={scale.x(span.from) + 6} y={17}>
          {span.label}
        </text>,
      );
    }
    for (const span of bottom) {
      const left = scale.x(span.from);
      const spanWidth = scale.xEnd(span.to) - left;
      shapes.push(
        <g key={`b-${span.from}`}>
          {left > 0 && (
            <line
              className="g-scale-line"
              x1={left + 0.5}
              x2={left + 0.5}
              y1={24}
              y2={headHeight}
            />
          )}
          {spanWidth >= 28 && (
            <text className="g-scale-text" x={left + spanWidth / 2} y={41} textAnchor="middle">
              {span.label}
            </text>
          )}
        </g>,
      );
    }
  }
  const todayX = scale.x(today) + unit / 2;
  return (
    <svg width={scale.width} height={headHeight} aria-hidden style={{ display: 'block' }}>
      <rect className="g-scale" x={0} y={0} width={scale.width} height={headHeight} />
      {shapes}
      <line className="g-scale-line" x1={0} x2={scale.width} y1={24.5} y2={24.5} />
      {today >= from && today <= to && (
        <g>
          <line className="g-today" x1={todayX} x2={todayX} y1={26} y2={headHeight} />
          <rect className="g-today-flag" x={todayX - 18} y={4} width={36} height={17} rx={3} />
          <text className="g-today-label" x={todayX} y={16.5} textAnchor="middle">
            {t('projects.timeline.today')}
          </text>
        </g>
      )}
    </svg>
  );
}

function RowShapes({ row, top, scale }: { row: OverviewRow; top: number; scale: PrintScale }) {
  if (row.kind === 'Milestone') {
    const x = (scale.x(row.start) + scale.xEnd(row.start)) / 2;
    const y = top + rowHeight / 2;
    return (
      <path
        className={row.critical ? 'g-milestone g-milestone--critical' : 'g-milestone'}
        d={`M${String(x)} ${String(y - 8)} L${String(x + 8)} ${String(y)} L${String(x)} ${String(y + 8)} L${String(x - 8)} ${String(y)} Z`}
      />
    );
  }
  const y = top + barTop;
  const left = scale.x(row.start);
  const width = scale.xEnd(row.end) - left;
  return (
    <g>
      {row.baselineStart !== null && row.baselineEnd !== null && (
        <rect
          className="g-baseline"
          x={scale.x(row.baselineStart)}
          y={y + barHeight + 3}
          width={Math.max(0, scale.xEnd(row.baselineEnd) - scale.x(row.baselineStart))}
          height={4}
          rx={1}
        />
      )}
      <rect
        className={row.critical ? 'g-bar g-bar--critical' : 'g-bar'}
        x={left + 0.5}
        y={y + 0.5}
        width={Math.max(0, width - 1)}
        height={barHeight - 1}
        rx={3}
      />
      {row.progress > 0 && (
        <rect
          className="g-progress"
          x={left + 1}
          y={y + 1}
          width={Math.max(0, ((width - 2) * row.progress) / 100)}
          height={barHeight - 2}
          rx={2.5}
        />
      )}
    </g>
  );
}

const headSx = {
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  boxSizing: 'border-box',
  height: headHeight,
  padding: '0 var(--space-3)',
  backgroundColor: 'var(--bg-subtle)',
  borderRight: '1px solid var(--border)',
  color: 'var(--text-secondary)',
  fontWeight: 500,
} as const;

const nameSx = { flex: 1, minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis' } as const;

const progressSx = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'flex-end',
  gap: '2px',
  flex: 'none',
  width: 72,
  fontVariantNumeric: 'tabular-nums',
} as const;

/** The scale and the today flag of the Gantt of the design system (fp-gantt), besides the colors of the strips. */
const timelineSx = {
  ...ganttStripSx,
  '& .g-scale': { fill: 'var(--bg-subtle)' },
  '& .g-scale-text': {
    fill: 'var(--text-secondary)',
    fontSize: 12,
    fontFamily: 'var(--font-sans)',
    fontVariantNumeric: 'tabular-nums',
  },
  '& .g-scale-month': {
    fill: 'var(--text)',
    fontSize: 12,
    fontWeight: 500,
    fontFamily: 'var(--font-sans)',
  },
  '& .g-scale-today': { fill: 'var(--action-text)', fontWeight: 500 },
  '& .g-today-flag': { fill: 'var(--gantt-today)' },
  '& .g-today-label': {
    fill: 'var(--bg-surface)',
    fontSize: 11,
    fontWeight: 500,
    fontFamily: 'var(--font-sans)',
  },
} as const;

/**
 * The phases and the milestones of the project in a smaller timeline (ADR-0071, ADR-0073), as the Gantt of the design
 * system draws it in the canvas ProjetoVisaoGeral: the names and the progress at the left and, beside them, the period
 * of the schedule in the width there is, with the months and the days, today, the days off shaded, the bars of the
 * phases with the part done, the critical path, the baseline under the bar and the milestones as diamonds. It only
 * shows: the Cronograma tab edits. Drawn here with the tokens of the Gantt, without the Syncfusion, as the print page
 * (ADR-0051).
 */
export function OverviewTimeline({
  timeline,
  today,
  listWidth,
}: {
  timeline: Timeline;
  today: DayValue;
  listWidth: number;
}) {
  const { t } = useTranslation();
  const [chartRef, width] = useWidth();
  // A day on each side, so the diamonds of the first and the last day show whole.
  const scale = printScale(
    dayOfNumber(dayNumber(timeline.start) - 1),
    dayOfNumber(dayNumber(timeline.end) + 1),
    width,
  );
  const unit = width / scale.days;
  const height = timeline.rows.length * rowHeight;
  const todayX = scale.x(today) + unit / 2;
  const showsToday =
    today >= dayOfNumber(scale.first) && today <= dayOfNumber(scale.first + scale.days - 1);
  const columns = `${String(listWidth)}px minmax(0, 1fr)`;

  return (
    <Box
      sx={{
        ...timelineSx,
        display: 'flex',
        flexDirection: 'column',
        flex: 1,
        minHeight: 0,
        fontSize: 13,
        lineHeight: '18px',
      }}
    >
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: columns,
          flex: 'none',
          overflow: 'hidden',
          scrollbarGutter: 'stable',
          backgroundColor: 'var(--bg-subtle)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box sx={headSx}>
          <Box component="span" sx={nameSx}>
            {t('projects.overview.timeline.name')}
          </Box>
          <Box component="span" sx={progressSx}>
            {t('projects.timeline.columns.progress')}
          </Box>
        </Box>
        <Box sx={{ minWidth: 0, overflow: 'hidden' }}>
          <Scale scale={scale} today={today} />
        </Box>
      </Box>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'grid',
          gridTemplateColumns: columns,
          alignItems: 'stretch',
          overflowX: 'hidden',
          overflowY: 'auto',
          scrollbarGutter: 'stable',
        }}
      >
        <Box
          component="ul"
          sx={{
            margin: 0,
            padding: 0,
            listStyle: 'none',
            minWidth: 0,
            borderRight: '1px solid var(--border)',
          }}
        >
          {timeline.rows.map((row) => {
            const milestone = row.kind === 'Milestone';
            return (
              <Box
                component="li"
                key={row.id}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-2)',
                  boxSizing: 'border-box',
                  height: rowHeight,
                  padding: '0 var(--space-3)',
                  borderBottom: '1px solid var(--border)',
                  color: 'var(--text)',
                  whiteSpace: 'nowrap',
                }}
              >
                {milestone && (
                  <FlagOutlined aria-hidden sx={{ fontSize: 16, color: 'var(--text-secondary)' }} />
                )}
                <Box component="span" title={row.name} sx={nameSx}>
                  {row.name}
                </Box>
                <Box
                  component="span"
                  sx={{
                    ...progressSx,
                    color: row.late ? 'var(--status-danger-text)' : 'var(--text-secondary)',
                    fontWeight: row.late ? 500 : 400,
                  }}
                >
                  {row.late && (
                    <WarningAmberOutlined
                      sx={{ fontSize: 16 }}
                      titleAccess={
                        milestone
                          ? t('projects.timeline.milestone.late')
                          : t('projects.schedule.late')
                      }
                    />
                  )}
                  {milestone
                    ? row.reached
                      ? t('projects.timeline.milestone.reached')
                      : t('projects.timeline.milestone.pending')
                    : `${String(row.progress)}%`}
                </Box>
              </Box>
            );
          })}
        </Box>
        <Box ref={chartRef} sx={{ minWidth: 0, overflow: 'hidden' }}>
          <svg
            width={width}
            height={height}
            role="img"
            aria-label={t('projects.overview.timeline.label', {
              start: formatDayText(timeline.start),
              end: formatDayText(timeline.end),
            })}
            style={{ display: 'block' }}
          >
            {unit >= 3 &&
              timeline.nonWorkingDays.map((day) => (
                <rect
                  key={day}
                  className="g-nonworking"
                  x={scale.x(day)}
                  y={0}
                  width={unit}
                  height={height}
                />
              ))}
            {timeline.rows.map((row, index) => (
              <line
                key={`line-${row.id}`}
                className="g-scale-line"
                x1={0}
                x2={width}
                y1={(index + 1) * rowHeight - 0.5}
                y2={(index + 1) * rowHeight - 0.5}
              />
            ))}
            {timeline.rows.map((row, index) => (
              <RowShapes key={row.id} row={row} top={index * rowHeight} scale={scale} />
            ))}
            {showsToday && <line className="g-today" x1={todayX} x2={todayX} y1={0} y2={height} />}
          </svg>
        </Box>
      </Box>
    </Box>
  );
}
