import Box from '@mui/material/Box';
import { useQueryClient } from '@tanstack/react-query';
import { lazy, type ReactNode, type Ref, Suspense, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Project, projectProblems, type Schedule } from '../api/client';
import { queryKeys } from '../api/queries';
import type { DayValue } from '../format/dates';
import type { GanttScale } from '../schedule-gantt/ganttLayout';
import type { GanttTarget } from '../schedule-gantt/ganttRows';
import type { ScheduleGanttHandle } from '../schedule-gantt/ScheduleGantt';
import { problemType } from './projectForm';
import { findItem, itemsOf, type ScheduleDisplay } from './schedule';
import { failureOf } from './scheduleItemValues';
import { useNotify } from '../notices/notify';

// The timeline comes only when it opens, loaded ahead when a project opens on a computer (ADR-0106).
const ScheduleGantt = lazy(() => import('../schedule-gantt/ScheduleGantt'));

const quietSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/**
 * The Linha do tempo of the tab Cronograma (ADR-0071): the ScheduleGantt with what it needs from the API and the moves
 * of the bars, saved by the API, which pushes the items after them (ADR-0019). Each move answers once the schedule
 * came back, so the bar stays where it was dropped until then (ADR-0134). The legend says what each color is.
 */
export function ScheduleTimeline({
  project,
  schedule,
  scale,
  editable,
  detailed = false,
  selectedId,
  display,
  ganttRef,
  onChoose,
  emptyText,
  aside,
  collapsed,
  onToggle,
}: {
  project: Project;
  schedule: Schedule;
  scale: GanttScale;
  editable: boolean;
  detailed?: boolean;
  selectedId?: string;
  /** What "Exibir" has on: the baseline and the critical path (ADR-0071). */
  display: ScheduleDisplay;
  ganttRef?: Ref<ScheduleGanttHandle>;
  onChoose: (target: GanttTarget) => void;
  /** Said in place of the timeline when a filter leaves no task or milestone (GanttA-3). */
  emptyText?: string;
  /** Beside the legend, such as the progress of the project and its delays. */
  aside?: ReactNode;
  /** The phases and summary tasks folded, the same of the Lista, and the toggle of one (Cronograma-4.2). */
  collapsed?: ReadonlySet<string>;
  onToggle?: (key: string) => void;
}) {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  /** The schedule and what depends on it asked again; done when they came back. */
  const refresh = useCallback(
    () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.projects }),
        queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
      ]),
    [queryClient],
  );

  /** Saves new dates of an item and says how many items after it were pushed. */
  const reschedule = useCallback(
    async (itemId: string, start: DayValue, end: DayValue | null) => {
      const item = findItem(schedule, itemId)?.item;
      if (item === undefined) {
        return;
      }
      try {
        const { data, error, response } = await api.PUT(
          '/api/projects/{id}/schedule/items/{itemId}/dates',
          {
            params: { path: { id: project.id, itemId } },
            body: { start, end },
          },
        );
        const refreshed = refresh();
        if (!response.ok) {
          notify(failureOf(error, response.status, t), { tone: 'error' });
        } else {
          const saved = t(`projects.schedule.messages.itemSaved.${item.kind}`, {
            name: item.name,
          });
          const pushed = data?.pushed ?? 0;
          notify(
            pushed === 0 ? saved : t('projects.schedule.messages.pushed', { saved, count: pushed }),
          );
        }
        await refreshed;
      } catch {
        notify(t('projects.schedule.messages.failed'), { tone: 'error' });
        await refresh();
      }
    },
    [schedule, project.id, refresh, notify, t],
  );

  const move = useCallback(
    (itemId: string, start: DayValue) => reschedule(itemId, start, null),
    [reschedule],
  );

  const resize = useCallback(
    (itemId: string, start: DayValue, end: DayValue) => reschedule(itemId, start, end),
    [reschedule],
  );

  /** Makes an item depend on another, as the form of the item does, keeping its progress (ADR-0086). */
  const link = useCallback(
    async (predecessorId: string, successorId: string) => {
      const successor = findItem(schedule, successorId)?.item;
      const predecessor = findItem(schedule, predecessorId)?.item;
      if (
        successor === undefined ||
        predecessor === undefined ||
        successor.predecessorIds.includes(predecessorId)
      ) {
        return;
      }
      // Only the items with dates of their own have dependencies (ADR-0088).
      const summary = [predecessor, successor].find((item) => item.hasSubtasks);
      if (summary !== undefined) {
        notify(t('projects.timeline.messages.summaryLink', { name: summary.name }), {
          tone: 'error',
        });
        return;
      }
      const names = { predecessor: predecessor.name, successor: successor.name };
      try {
        const { error, response } = await api.PUT('/api/projects/{id}/schedule/items/{itemId}', {
          params: { path: { id: project.id, itemId: successorId } },
          body: {
            phaseId: successor.phaseId,
            name: successor.name,
            start: successor.start,
            duration: successor.duration,
            ownerId: successor.owner?.id ?? null,
            ownedByClient: successor.ownedByClient,
            progress: null,
            predecessorIds: [...successor.predecessorIds, predecessorId],
            parentId: successor.parentId,
            // The deadline stays as it was (GanttA-2).
            deadline: successor.deadline,
          },
        });
        const refreshed = refresh();
        if (response.ok) {
          notify(t('projects.timeline.messages.linked', names));
        } else if (problemType(error) === projectProblems.scheduleCycle) {
          notify(t('projects.timeline.messages.cycle', names), { tone: 'error' });
        } else {
          notify(failureOf(error, response.status, t), { tone: 'error' });
        }
        await refreshed;
      } catch {
        notify(t('projects.schedule.messages.failed'), { tone: 'error' });
        await refresh();
      }
    },
    [schedule, project.id, refresh, notify, t],
  );

  const loading = (
    <Box role="status" sx={{ ...quietSx, padding: 'var(--space-4)' }}>
      {t('projects.timeline.loading')}
    </Box>
  );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
      }}
    >
      {schedule.start === null || emptyText !== undefined ? (
        <Box
          sx={{
            ...quietSx,
            fontSize: 14,
            padding: 'var(--space-6) var(--space-5)',
            backgroundColor: 'var(--bg-surface)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-lg)',
          }}
        >
          {emptyText ??
            t(
              schedule.phases.length === 0
                ? 'projects.schedule.empty'
                : 'projects.timeline.noItems',
            )}
        </Box>
      ) : (
        <Suspense fallback={loading}>
          <ScheduleGantt
            ref={ganttRef}
            schedule={schedule}
            scale={scale}
            editable={editable}
            detailed={detailed}
            selectedId={selectedId}
            showBaseline={display.baseline}
            showCritical={display.critical}
            clientName={project.client?.name ?? null}
            onChoose={onChoose}
            onMove={move}
            onResize={resize}
            onLink={link}
            collapsed={collapsed}
            onToggle={onToggle}
          />
        </Suspense>
      )}
      {!detailed && schedule.start !== null && (
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: 'var(--space-2) var(--space-4)',
          }}
        >
          <TimelineLegend
            critical={display.critical}
            baseline={display.baseline && schedule.baseline !== null}
            deadline={itemsOf(schedule).some((item) => item.deadline !== null)}
          />
          {aside}
        </Box>
      )}
    </Box>
  );
}

type LegendKey =
  | 'done'
  | 'todo'
  | 'critical'
  | 'baseline'
  | 'phase'
  | 'milestone'
  | 'deadline'
  | 'slack'
  | 'nonWorking';

/** The mark of a deadline in the legend: the dashed line with the arrow on top, as the timeline draws it. */
function DeadlineMark() {
  return (
    <Box
      component="svg"
      aria-hidden
      width={10}
      height={14}
      viewBox="0 0 10 14"
      sx={{ flex: 'none' }}
    >
      <path d="M1 0.5 H9 L5 5.5 Z" fill="var(--gantt-dependency)" />
      <line
        x1={5}
        x2={5}
        y1={5}
        y2={14}
        stroke="var(--gantt-dependency)"
        strokeWidth={1.5}
        strokeDasharray="3 2"
      />
    </Box>
  );
}

/**
 * What each color of the timeline is (Gantt.Legend of the design system), with the critical path and the baseline
 * while they are shown, and the deadline when an item has one (GanttA-2).
 */
export function TimelineLegend({
  critical,
  baseline,
  deadline = false,
}: {
  critical: boolean;
  baseline: boolean;
  deadline?: boolean;
}) {
  const { t } = useTranslation();
  const swatch = (background: string, more: object = {}): object => ({
    display: 'inline-block',
    width: 20,
    height: 10,
    boxSizing: 'border-box',
    borderRadius: 'var(--radius-sm)',
    background: `var(--${background})`,
    ...more,
  });
  const entries: { key: LegendKey; sx?: object }[] = [
    { key: 'done', sx: swatch('gantt-progress') },
    { key: 'todo', sx: swatch('gantt-bar', { border: '1px solid var(--gantt-bar-border)' }) },
  ];
  if (critical) {
    entries.push({
      key: 'critical',
      sx: swatch('gantt-critical-bar', { border: '1px solid var(--gantt-critical-border)' }),
    });
  }
  if (baseline) {
    entries.push({ key: 'baseline', sx: swatch('gantt-baseline', { height: 4 }) });
  }
  // The float goes with the critical path (GanttB-1).
  if (critical) {
    entries.push({ key: 'slack', sx: swatch('gantt-baseline', { height: 2, borderRadius: 0 }) });
  }
  entries.push(
    { key: 'phase', sx: swatch('gantt-summary', { height: 6 }) },
    {
      key: 'milestone',
      sx: swatch('gantt-milestone', { width: 10, transform: 'rotate(45deg)', borderRadius: 0 }),
    },
  );
  if (deadline) {
    entries.push({ key: 'deadline' });
  }
  entries.push({
    key: 'nonWorking',
    sx: swatch('gantt-nonworking', { border: '1px solid var(--border)' }),
  });
  return (
    <Box
      component="ul"
      aria-label={t('projects.timeline.legend.label')}
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 'var(--space-4)',
        margin: 0,
        padding: 0,
        listStyle: 'none',
        fontSize: 12,
        lineHeight: '16px',
        color: 'var(--text-secondary)',
      }}
    >
      {entries.map((entry) => (
        <Box
          component="li"
          key={entry.key}
          sx={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
        >
          {entry.sx === undefined ? (
            <DeadlineMark />
          ) : (
            <Box component="span" aria-hidden sx={entry.sx} />
          )}
          {t(`projects.timeline.legend.${entry.key}`)}
        </Box>
      ))}
    </Box>
  );
}
