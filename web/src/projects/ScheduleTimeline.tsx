import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { lazy, type ReactNode, type Ref, Suspense, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Project, projectProblems, type Schedule } from '../api/client';
import { clientSettingsQuery, queryKeys } from '../api/queries';
import type { DayValue } from '../format/dates';
import type { GanttScale } from '../schedule-gantt/ganttLayout';
import type { GanttTarget } from '../schedule-gantt/ganttRows';
import type { ScheduleGanttHandle } from '../schedule-gantt/ScheduleGantt';
import { problemType } from './projectForm';
import { findItem } from './schedule';
import { failureOf } from './scheduleItemValues';

// The Syncfusion Gantt is large: it comes only when the Linha do tempo opens (ADR-0051).
const ScheduleGantt = lazy(() => import('../schedule-gantt/ScheduleGantt'));

const quietSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/**
 * The Linha do tempo of the tab Cronograma (ADR-0071): the ScheduleGantt with what it needs from the API, the license
 * of the installation (ADR-0087) and the moves of the bars, saved by the API, which pushes the items after them
 * (ADR-0019). The legend says what each color is.
 */
export function ScheduleTimeline({
  project,
  schedule,
  scale,
  editable,
  detailed = false,
  selectedId,
  ganttRef,
  onChoose,
  notify,
  aside,
}: {
  project: Project;
  schedule: Schedule;
  scale: GanttScale;
  editable: boolean;
  detailed?: boolean;
  selectedId?: string;
  ganttRef?: Ref<ScheduleGanttHandle>;
  onChoose: (target: GanttTarget) => void;
  notify: (message: string) => void;
  /** Beside the legend, such as the progress of the project and its delays. */
  aside?: ReactNode;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const settings = useQuery(clientSettingsQuery());

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  }, [queryClient]);

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
        if (!response.ok) {
          refresh();
          notify(failureOf(error, response.status, t));
          return;
        }
        refresh();
        const saved = t(`projects.schedule.messages.itemSaved.${item.kind}`, { name: item.name });
        const pushed = data?.pushed ?? 0;
        notify(
          pushed === 0 ? saved : t('projects.schedule.messages.pushed', { saved, count: pushed }),
        );
      } catch {
        refresh();
        notify(t('projects.schedule.messages.failed'));
      }
    },
    [schedule, project.id, refresh, notify, t],
  );

  const move = useCallback(
    (itemId: string, start: DayValue) => {
      void reschedule(itemId, start, null);
    },
    [reschedule],
  );

  const resize = useCallback(
    (itemId: string, start: DayValue, end: DayValue) => {
      void reschedule(itemId, start, end);
    },
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
            progress: null,
            predecessorIds: [...successor.predecessorIds, predecessorId],
          },
        });
        refresh();
        if (response.ok) {
          notify(t('projects.timeline.messages.linked', names));
        } else if (problemType(error) === projectProblems.scheduleCycle) {
          notify(t('projects.timeline.messages.cycle', names));
        } else {
          notify(failureOf(error, response.status, t));
        }
      } catch {
        refresh();
        notify(t('projects.schedule.messages.failed'));
      }
    },
    [schedule, project.id, refresh, notify, t],
  );

  const linkLater = useCallback(
    (predecessorId: string, successorId: string) => {
      void link(predecessorId, successorId);
    },
    [link],
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
      {schedule.start === null ? (
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
          {t(
            schedule.phases.length === 0 ? 'projects.schedule.empty' : 'projects.timeline.noItems',
          )}
        </Box>
      ) : settings.isPending ? (
        loading
      ) : (
        <Suspense fallback={loading}>
          <ScheduleGantt
            ref={ganttRef}
            schedule={schedule}
            scale={scale}
            editable={editable}
            detailed={detailed}
            selectedId={selectedId}
            license={settings.data?.timelineLicense ?? null}
            onChoose={onChoose}
            onMove={move}
            onResize={resize}
            onLink={linkLater}
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
          <TimelineLegend />
          {aside}
        </Box>
      )}
    </Box>
  );
}

/** What each color of the timeline is (Gantt.Legend of the design system). */
function TimelineLegend() {
  const { t } = useTranslation();
  const swatch = (background: string, more: object = {}) => ({
    display: 'inline-block',
    width: 20,
    height: 10,
    boxSizing: 'border-box',
    borderRadius: 'var(--radius-sm)',
    background: `var(--${background})`,
    ...more,
  });
  const entries = [
    { key: 'done', sx: swatch('gantt-progress') },
    { key: 'todo', sx: swatch('gantt-bar', { border: '1px solid var(--gantt-bar-border)' }) },
    { key: 'phase', sx: swatch('gantt-summary', { height: 6 }) },
    {
      key: 'milestone',
      sx: swatch('gantt-milestone', { width: 10, transform: 'rotate(45deg)', borderRadius: 0 }),
    },
    { key: 'nonWorking', sx: swatch('gantt-nonworking', { border: '1px solid var(--border)' }) },
  ] as const;
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
          <Box component="span" aria-hidden sx={entry.sx} />
          {t(`projects.timeline.legend.${entry.key}`)}
        </Box>
      ))}
    </Box>
  );
}
