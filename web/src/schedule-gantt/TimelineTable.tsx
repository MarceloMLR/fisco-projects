import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import type { KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { IconButton } from '../components/Button';
import { DueDate } from '../components/DueDate';
import { formatDayMonthText } from '../format/dates';
import {
  deadlineText,
  lateText,
  milestoneStatus,
  missesDeadline,
  ownerText,
} from '../projects/schedule';
import type { GanttRow } from './ganttRows';

/** The columns beside the timeline: the name and the progress; in Expandir, all of them; on a phone, only the name. */
export type TimelineColumns = 'compact' | 'detailed' | 'narrow';

/** How far each level of the tree is from the one above it (fp-gantt__row--child). */
const indent = 16;

export interface TimelineTableProps {
  /** The lines on the screen, without the ones inside a hidden phase or task. */
  rows: readonly GanttRow[];
  collapsed: ReadonlySet<string>;
  columns: TimelineColumns;
  clientName: string | null;
  selectedId?: string;
  /** The line the keyboard is on. */
  active?: string;
  /** The beginning of the ids of the lines, unique in the page (aria-activedescendant). */
  idPrefix: string;
  onActivate: (key: string) => void;
  onToggle: (key: string) => void;
  onChoose: (row: GanttRow) => void;
}

/**
 * The table beside the timeline, as a tree (treegrid): each phase with its tasks and milestones, each task with its
 * subtasks, the arrow that hides and shows them, and the keyboard (ADR-0071, ADR-0088). The table keeps the focus and
 * says which line is active, as the arrows move it; Enter opens the details, as the click does (ADR-0081).
 */
export function TimelineTable({
  rows,
  collapsed,
  columns,
  clientName,
  selectedId,
  active,
  idPrefix,
  onActivate,
  onToggle,
  onChoose,
}: TimelineTableProps) {
  const { t } = useTranslation();
  const activeIndex = rows.findIndex((row) => row.key === active);
  const current = rows[activeIndex];

  const go = (index: number) => {
    const row = rows[Math.min(Math.max(index, 0), rows.length - 1)];
    if (row !== undefined) {
      onActivate(row.key);
      document.getElementById(`${idPrefix}-${row.key}`)?.scrollIntoView({ block: 'nearest' });
    }
  };

  const keyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) {
      return;
    }
    if (current === undefined) {
      if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        go(0);
      }
      return;
    }
    const open = current.hasChildren && !collapsed.has(current.key);
    switch (event.key) {
      case 'ArrowDown':
        go(activeIndex + 1);
        break;
      case 'ArrowUp':
        go(activeIndex - 1);
        break;
      case 'Home':
        go(0);
        break;
      case 'End':
        go(rows.length - 1);
        break;
      case 'ArrowRight':
        if (current.hasChildren && !open) {
          onToggle(current.key);
        } else if (open) {
          go(activeIndex + 1);
        }
        break;
      case 'ArrowLeft':
        if (open) {
          onToggle(current.key);
        } else if (current.parentKey !== undefined) {
          go(rows.findIndex((row) => row.key === current.parentKey));
        }
        break;
      case 'Enter':
      case ' ':
        onChoose(current);
        break;
      default:
        return;
    }
    event.preventDefault();
  };

  return (
    <div
      role="treegrid"
      aria-label={t('projects.timeline.table')}
      aria-readonly
      aria-activedescendant={current === undefined ? undefined : `${idPrefix}-${current.key}`}
      tabIndex={0}
      className="fp-gantt__table"
      onKeyDown={keyDown}
      onFocus={(event) => {
        // Entering the table, the keyboard starts on the chosen line, or on the first one.
        if (event.target === event.currentTarget && current === undefined) {
          const chosen = rows.find((row) => row.key === selectedId) ?? rows[0];
          if (chosen !== undefined) {
            onActivate(chosen.key);
          }
        }
      }}
    >
      <div role="row" className="fp-gantt__head">
        <span role="columnheader" className="fp-gantt__name">
          {t('projects.timeline.columns.name')}
        </span>
        {columns === 'detailed' && (
          <>
            <span role="columnheader" className="fp-gantt__col">
              {t('projects.timeline.columns.start')}
            </span>
            <span role="columnheader" className="fp-gantt__col">
              {t('projects.timeline.columns.end')}
            </span>
            <span role="columnheader" className="fp-gantt__col fp-gantt__col--deadline">
              {t('projects.timeline.columns.deadline')}
            </span>
            <span role="columnheader" className="fp-gantt__col fp-gantt__col--who">
              {t('projects.timeline.columns.owner')}
            </span>
          </>
        )}
        {columns !== 'narrow' && (
          <span role="columnheader" className="fp-gantt__pct">
            {t('projects.timeline.columns.progress')}
          </span>
        )}
      </div>
      {rows.map((row) => (
        <TableRow
          key={row.key}
          row={row}
          id={`${idPrefix}-${row.key}`}
          open={!collapsed.has(row.key)}
          columns={columns}
          clientName={clientName}
          selected={row.key === selectedId}
          active={row.key === current?.key}
          onToggle={() => {
            onActivate(row.key);
            onToggle(row.key);
          }}
          onChoose={() => {
            onActivate(row.key);
            onChoose(row);
          }}
        />
      ))}
    </div>
  );
}

function TableRow({
  row,
  id,
  open,
  columns,
  clientName,
  selected,
  active,
  onToggle,
  onChoose,
}: {
  row: GanttRow;
  id: string;
  open: boolean;
  columns: TimelineColumns;
  clientName: string | null;
  selected: boolean;
  active: boolean;
  onToggle: () => void;
  onChoose: () => void;
}) {
  const { t } = useTranslation();
  const target = row.target;
  const item = target.kind === 'item' ? target.item : undefined;
  const classes = ['fp-gantt__row'];
  if (row.shape === 'summary') {
    classes.push('fp-gantt__row--phase');
  }
  if (active) {
    classes.push('fp-gantt__row--active');
  }
  return (
    <div
      role="row"
      id={id}
      aria-level={row.level + 1}
      aria-expanded={row.hasChildren ? open : undefined}
      aria-selected={selected}
      className={classes.join(' ')}
      onClick={onChoose}
    >
      <span role="gridcell" className="fp-gantt__name" style={{ paddingLeft: row.level * indent }}>
        {row.hasChildren ? (
          <IconButton
            className="fp-gantt__toggle"
            tabIndex={-1}
            aria-label={t(open ? 'projects.timeline.collapseRow' : 'projects.timeline.expandRow', {
              name: row.name,
            })}
            onClick={(event) => {
              event.stopPropagation();
              onToggle();
            }}
          >
            {open ? <ExpandMoreOutlined /> : <ChevronRightOutlined />}
          </IconButton>
        ) : (
          <span className="fp-gantt__spacer" />
        )}
        {item?.kind === 'Milestone' && (
          <FlagOutlined
            titleAccess={t('projects.schedule.kinds.Milestone')}
            className="fp-gantt__icon"
          />
        )}
        <span className="fp-gantt__text" title={row.name}>
          {row.name}
        </span>
        {columns === 'narrow' && item !== undefined && isLate(row) && (
          <WarningAmberOutlined
            titleAccess={lateText(item, t)}
            className="fp-gantt__icon fp-gantt__icon--late"
          />
        )}
      </span>
      {columns === 'detailed' && (
        <>
          <span role="gridcell" className="fp-gantt__col">
            {item === undefined ? '' : formatDayMonthText(item.start)}
          </span>
          <span role="gridcell" className="fp-gantt__col">
            {item?.kind === 'Task' ? formatDayMonthText(item.end) : ''}
          </span>
          <span role="gridcell" className="fp-gantt__col fp-gantt__col--deadline">
            {item?.deadline !== undefined && item.deadline !== null && <DeadlineCell item={item} />}
          </span>
          <span role="gridcell" className="fp-gantt__col fp-gantt__col--who">
            {item === undefined ? '' : (ownerText(item, clientName, t) ?? '')}
          </span>
        </>
      )}
      {columns !== 'narrow' && (
        <span role="gridcell" className="fp-gantt__pct">
          <ProgressCell row={row} />
        </span>
      )}
    </div>
  );
}

/** The day of the deadline; missed, in the color of the alarm, with the icon that says by how much (GanttA-2). */
function DeadlineCell({ item }: { item: GanttItem }) {
  const { t } = useTranslation();
  if (item.deadline === null) {
    return null;
  }
  const day = formatDayMonthText(item.deadline);
  return missesDeadline(item) ? (
    <span className="fp-gantt__deadline fp-gantt__deadline--missed">
      <WarningAmberOutlined titleAccess={deadlineText(item, t)} className="fp-gantt__icon" />
      {day}
    </span>
  ) : (
    <span className="fp-gantt__deadline">{day}</span>
  );
}

type GanttItem = Extract<GanttRow['target'], { kind: 'item' }>['item'];

/** A task late, or a milestone not reached past its day (ADR-0065). */
function isLate(row: GanttRow): boolean {
  const target = row.target;
  return (
    target.kind === 'item' &&
    (target.item.kind === 'Milestone' ? milestoneStatus(target.item) === 'late' : target.item.late)
  );
}

/**
 * The progress of a line, or the state of a milestone; a late task says so by icon, color and text, and the icon says
 * by how many business days, as the state of a late milestone does.
 */
function ProgressCell({ row }: { row: GanttRow }) {
  const { t } = useTranslation();
  const target = row.target;
  if (target.kind === 'item' && target.item.kind === 'Milestone') {
    const status = milestoneStatus(target.item);
    return (
      <span title={status === 'late' ? lateText(target.item, t) : undefined}>
        <DueDate state={status === 'reached' ? 'met' : status === 'late' ? 'overdue' : 'onTrack'}>
          {t(`projects.timeline.milestone.${status}`)}
        </DueDate>
      </span>
    );
  }
  if (target.kind === 'phase' && target.phase.start === null) {
    return null;
  }
  const late = target.kind === 'item' && target.item.late;
  return (
    <span className={late ? 'fp-gantt__progress fp-gantt__progress--late' : 'fp-gantt__progress'}>
      {late && (
        <WarningAmberOutlined titleAccess={lateText(target.item, t)} className="fp-gantt__icon" />
      )}
      {`${String(row.progress)}%`}
    </span>
  );
}
