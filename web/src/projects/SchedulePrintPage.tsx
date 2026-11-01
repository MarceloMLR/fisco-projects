import FlagOutlined from '@mui/icons-material/FlagOutlined';
import PrintOutlined from '@mui/icons-material/PrintOutlined';
import Box from '@mui/material/Box';
import GlobalStyles from '@mui/material/GlobalStyles';
import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router';
import type { Schedule, ScheduleItem, SchedulePhase } from '../api/client';
import { projectQuery, projectScheduleQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import {
  type DayValue,
  formatDayMonthText,
  formatDayText,
  formatPeriodText,
} from '../format/dates';
import {
  itemsOf,
  missesDeadline,
  predecessorNames,
  type ScheduleDisplay,
  scheduleRows,
} from './schedule';
import { choiceOf, ganttStripSx, type PrintScale, printScale, scaleSpans } from './schedulePrint';
import { TimelineLegend } from './ScheduleTimeline';
import { Button } from '../components/Button';

/** The width of the strip of the timeline in a line, in an A4 page in landscape, beside the columns of the table. */
const stripWidth = 520;
const rowHeight = 26;
const barHeight = 12;
const noCollapsed: ReadonlySet<string> = new Set();

/**
 * The print page of a schedule (ADR-0021): the header of the project, a line for each phase, task and milestone with its
 * dates, progress and what it depends on, and its strip of the timeline in the period and the scale the person chose,
 * with the baseline and the critical path when "Exibir" had them on, and the legend. Always light, for paper, in A4
 * landscape; the scale repeats at the top of each page, and pages break between lines. "Imprimir" opens the printing of
 * the browser, which saves it as PDF. The strips are drawn here with the tokens of the design system, in SVG, as the
 * Linha do tempo draws its bars (ADR-0134).
 */
export function SchedulePrintPage() {
  const { t } = useTranslation();
  const { projectId = '' } = useParams();
  const [search] = useSearchParams();
  const project = useQuery(projectQuery(projectId));
  const schedule = useQuery(projectScheduleQuery(projectId));

  useEffect(() => {
    if (project.data !== undefined) {
      document.title = t('projects.print.documentTitle', { code: project.data.code });
    }
  }, [project.data, t]);

  if (project.isError || schedule.isError) {
    return (
      <Box sx={{ padding: 'var(--space-6)' }}>
        <Alert tone="danger">{t('projects.print.failed')}</Alert>
      </Box>
    );
  }
  if (project.data === undefined || schedule.data === undefined) {
    return (
      <Box role="status" sx={{ padding: 'var(--space-6)', color: 'var(--text-secondary)' }}>
        {t('projects.print.loading')}
      </Box>
    );
  }

  const data = schedule.data;
  const choice = choiceOf(search, data.start ?? data.today, data.end ?? data.today);
  const scale = printScale(choice.from, choice.to, stripWidth);
  const managers = project.data.members
    .filter((member) => member.role === 'Manager')
    .map((member) => member.name)
    .join(', ');
  const rows = scheduleRows(data, noCollapsed);

  return (
    <Box
      data-theme="light"
      sx={{
        minHeight: '100vh',
        backgroundColor: 'var(--bg-surface)',
        color: 'var(--text)',
        fontFamily: 'var(--font-sans)',
        padding: 'var(--space-6)',
        // The colors of the bars and of the legend also on paper.
        printColorAdjust: 'exact',
        WebkitPrintColorAdjust: 'exact',
        '@media print': { padding: 0, minHeight: 0 },
      }}
    >
      <GlobalStyles styles={{ '@page': { size: 'A4 landscape', margin: '10mm' } }} />
      <Box
        component="header"
        sx={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          gap: 'var(--space-4)',
          marginBottom: 'var(--space-4)',
        }}
      >
        <Box sx={{ minWidth: 0 }}>
          <Box component="h1" sx={{ margin: 0, fontSize: 20, lineHeight: '28px', fontWeight: 500 }}>
            {t('projects.print.title', { code: project.data.code, name: project.data.name })}
          </Box>
          <Box sx={{ fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' }}>
            {[
              project.data.client?.name,
              managers === '' ? undefined : t('projects.print.manager', { names: managers }),
              t('projects.print.period', {
                period: formatPeriodText(choice.from, choice.to),
                scale: t(`projects.timeline.scales.${choice.scale}`),
              }),
              t('projects.print.generatedOn', { day: formatDayText(data.today) }),
            ]
              .filter((part) => part !== undefined)
              .join(' · ')}
          </Box>
        </Box>
        <Button
          variant="contained"
          startIcon={<PrintOutlined />}
          onClick={() => {
            window.print();
          }}
          sx={{ flex: 'none', '@media print': { display: 'none' } }}
        >
          {t('projects.print.print')}
        </Button>
      </Box>
      <Box
        component="table"
        aria-label={t('projects.print.table')}
        sx={{
          width: '100%',
          borderCollapse: 'collapse',
          tableLayout: 'fixed',
          fontSize: 11,
          lineHeight: '14px',
          '& th, & td': {
            borderBottom: '1px solid var(--border)',
            padding: '0 var(--space-2)',
            textAlign: 'left',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          },
          '& th': {
            fontWeight: 500,
            color: 'var(--text-secondary)',
            backgroundColor: 'var(--bg-subtle)',
          },
          '& tr': { breakInside: 'avoid' },
          '& td.strip, & th.strip': { padding: 0, width: stripWidth },
          ...ganttStripSx,
        }}
      >
        <colgroup>
          <col style={{ width: 196 }} />
          <col style={{ width: 52 }} />
          <col style={{ width: 52 }} />
          <col style={{ width: 68 }} />
          <col style={{ width: 124 }} />
          <col style={{ width: stripWidth }} />
        </colgroup>
        <thead>
          <tr>
            <th>{t('projects.schedule.columns.name')}</th>
            <th>{t('projects.schedule.columns.start')}</th>
            <th>{t('projects.schedule.columns.end')}</th>
            <th>{t('projects.schedule.columns.progress')}</th>
            <th>{t('projects.schedule.columns.dependsOn')}</th>
            <th className="strip">
              <ScaleStrip from={choice.from} to={choice.to} scale={choice.scale} printing={scale} />
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) =>
            row.kind === 'phase' ? (
              <tr key={row.id} style={{ height: rowHeight }}>
                <Box component="td" sx={{ fontWeight: 500 }}>
                  {row.phase.name}
                </Box>
                <td>{row.phase.start === null ? '' : formatDayMonthText(row.phase.start)}</td>
                <td>{row.phase.end === null ? '' : formatDayMonthText(row.phase.end)}</td>
                <td>{row.phase.start === null ? '' : `${String(row.phase.progress)}%`}</td>
                <td />
                <td className="strip">
                  <LineStrip
                    phase={row.phase}
                    schedule={data}
                    printing={scale}
                    display={choice.display}
                  />
                </td>
              </tr>
            ) : (
              <tr key={row.id} style={{ height: rowHeight }}>
                <Box
                  component="td"
                  title={row.item.name}
                  sx={{
                    paddingLeft:
                      row.item.parentId === null
                        ? 'var(--space-4) !important'
                        : 'var(--space-6) !important',
                    fontWeight: row.item.hasSubtasks ? 500 : undefined,
                  }}
                >
                  {row.item.kind === 'Milestone' && (
                    <FlagOutlined
                      aria-label={t('projects.schedule.kinds.Milestone')}
                      sx={{ fontSize: 12, verticalAlign: 'middle', marginRight: '4px' }}
                    />
                  )}
                  {row.item.name}
                </Box>
                <td>{formatDayMonthText(row.item.start)}</td>
                <td>{row.item.kind === 'Task' ? formatDayMonthText(row.item.end) : ''}</td>
                <td>
                  {row.item.kind === 'Task'
                    ? `${String(row.item.progress)}%`
                    : t(
                        `projects.timeline.milestone.${row.item.reachedOn === null ? (row.item.late ? 'late' : 'pending') : 'reached'}`,
                      )}
                </td>
                <td title={predecessorNames(row.item, data).join(', ')}>
                  {predecessorNames(row.item, data).join(', ')}
                </td>
                <td className="strip">
                  <LineStrip
                    item={row.item}
                    schedule={data}
                    printing={scale}
                    display={choice.display}
                  />
                </td>
              </tr>
            ),
          )}
        </tbody>
      </Box>
      <Box sx={{ marginTop: 'var(--space-3)' }}>
        <TimelineLegend
          critical={choice.display.critical}
          baseline={choice.display.baseline && data.baseline !== null}
          deadline={itemsOf(data).some((item) => item.deadline !== null)}
        />
      </Box>
    </Box>
  );
}

/** In days too narrow for every number, only the Mondays keep theirs. */
function mondayLabel(day: DayValue, scale: 'days' | 'weeks' | 'months'): string | undefined {
  return scale === 'days' && new Date(`${day}T00:00:00Z`).getUTCDay() === 1
    ? String(Number(day.slice(8, 10)))
    : undefined;
}

/** The two rows of the scale: the months over the days or weeks, or the years over the months. */
function ScaleStrip({
  from,
  to,
  scale,
  printing,
}: {
  from: DayValue;
  to: DayValue;
  scale: 'days' | 'weeks' | 'months';
  printing: PrintScale;
}) {
  const spans = scaleSpans(from, to, scale);
  const height = 36;
  return (
    <svg width={printing.width} height={height} aria-hidden style={{ display: 'block' }}>
      {spans.top.map((span) => (
        <g key={`top-${span.from}`}>
          <line
            className="g-scale-line"
            x1={printing.x(span.from)}
            x2={printing.x(span.from)}
            y1={0}
            y2={height}
          />
          <text className="g-scale-month" x={printing.x(span.from) + 3} y={13}>
            {span.label}
          </text>
        </g>
      ))}
      <line className="g-scale-line" x1={0} x2={printing.width} y1={18} y2={18} />
      {spans.bottom.map((span) => {
        const left = printing.x(span.from);
        const width = printing.xEnd(span.to) - left;
        return (
          <g key={`bottom-${span.from}`}>
            <line className="g-scale-line" x1={left} x2={left} y1={18} y2={height} />
            {(width >= 16 || span.label === mondayLabel(span.from, scale)) && (
              <text className="g-scale-text" x={left + width / 2} y={31} textAnchor="middle">
                {span.label}
              </text>
            )}
          </g>
        );
      })}
    </svg>
  );
}

/**
 * The strip of one line: the days off shaded (in days), today, and the phase, task or milestone in the period, cut at
 * its edges; the part done in green, the critical path in red and the baseline under the bar when they are on; and the
 * deadline at the end of its day, in red when missed, as the Linha do tempo marks it (GanttA-2).
 */
function LineStrip({
  phase,
  item,
  schedule,
  printing,
  display,
}: {
  phase?: SchedulePhase;
  item?: ScheduleItem;
  schedule: Schedule;
  printing: PrintScale;
  display: ScheduleDisplay;
}) {
  const clamp = (value: number) => Math.min(printing.width, Math.max(0, value));
  const middle = rowHeight / 2;
  const top = middle - barHeight / 2;
  const shapes = [];
  const start = phase?.start ?? item?.start ?? null;
  const end = phase?.end ?? item?.end ?? null;
  if (start !== null && end !== null) {
    const left = printing.x(start);
    const right = printing.xEnd(end);
    if (right > 0 && left < printing.width) {
      const x = clamp(left);
      const width = clamp(right) - x;
      if (phase !== undefined || item?.hasSubtasks === true) {
        shapes.push(
          <path
            key="summary"
            className="g-summary"
            d={`M${String(x)} ${String(top)} h${String(width)} v10 l-5 -3 H${String(x + 5)} l-5 3 Z`}
          />,
        );
      } else if (item?.kind === 'Milestone') {
        const center = (left + right) / 2;
        if (center >= 0 && center <= printing.width) {
          shapes.push(
            <path
              key="milestone"
              className={
                display.critical && item.critical
                  ? 'g-milestone g-milestone--critical'
                  : 'g-milestone'
              }
              d={`M${String(center)} ${String(middle - 6)} L${String(center + 6)} ${String(middle)} L${String(center)} ${String(middle + 6)} L${String(center - 6)} ${String(middle)} Z`}
            />,
          );
        }
      } else if (item !== undefined) {
        const critical = display.critical && item.critical;
        if (display.baseline && item.baselineStart !== null && item.baselineEnd !== null) {
          const baseLeft = clamp(printing.x(item.baselineStart));
          const baseRight = clamp(printing.xEnd(item.baselineEnd));
          if (baseRight > baseLeft) {
            shapes.push(
              <rect
                key="baseline"
                className="g-baseline"
                x={baseLeft}
                y={top + barHeight + 2}
                width={baseRight - baseLeft}
                height={3}
                rx={1}
              />,
            );
          }
        }
        shapes.push(
          <rect
            key="bar"
            className={critical ? 'g-bar g-bar--critical' : 'g-bar'}
            x={x + 0.5}
            y={top + 0.5}
            width={Math.max(0, width - 1)}
            height={barHeight - 1}
            rx={2}
          />,
        );
        // The part done, from the start of the task, cut at the period.
        const done = clamp(left + (right - left) * (item.progress / 100)) - x;
        if (item.progress > 0 && done > 0) {
          shapes.push(
            <rect
              key="progress"
              className="g-progress"
              x={x + 1}
              y={top + 1}
              width={Math.max(0, done - 2)}
              height={barHeight - 2}
              rx={1.5}
            />,
          );
        }
      }
    }
  }
  const off = printing.days <= 62 ? schedule.nonWorkingDays : [];
  const today =
    printing.x(schedule.today) + (printing.xEnd(schedule.today) - printing.x(schedule.today)) / 2;
  return (
    <svg width={printing.width} height={rowHeight} aria-hidden style={{ display: 'block' }}>
      {off.map((day) => {
        const left = printing.x(day);
        return left >= 0 && left < printing.width ? (
          <rect
            key={day}
            className="g-nonworking"
            x={left}
            y={0}
            width={printing.xEnd(day) - left}
            height={rowHeight}
          />
        ) : null;
      })}
      {today >= 0 && today <= printing.width && (
        <line className="g-today" x1={today} x2={today} y1={0} y2={rowHeight} />
      )}
      {shapes}
      {item?.deadline !== undefined && item.deadline !== null && (
        <DeadlineMarker item={item} deadline={item.deadline} printing={printing} />
      )}
    </svg>
  );
}

/** The deadline of an item in its strip, when it is in the period. */
function DeadlineMarker({
  item,
  deadline,
  printing,
}: {
  item: ScheduleItem;
  deadline: DayValue;
  printing: PrintScale;
}) {
  const x = printing.xEnd(deadline);
  if (x < 0 || x > printing.width) {
    return null;
  }
  return (
    <g
      className={missesDeadline(item) ? 'g-deadline g-deadline--missed' : 'g-deadline'}
      data-day={deadline}
    >
      <line className="g-deadline-line" x1={x} x2={x} y1={5} y2={rowHeight - 2} />
      <path
        className="g-deadline-head"
        d={`M${String(x - 3.5)} ${String(1)} H${String(x + 3.5)} L${String(x)} ${String(5.5)} Z`}
      />
    </g>
  );
}
