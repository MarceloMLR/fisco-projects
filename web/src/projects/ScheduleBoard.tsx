import AutorenewOutlined from '@mui/icons-material/AutorenewOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import OpenWithOutlined from '@mui/icons-material/OpenWithOutlined';
import RadioButtonUncheckedOutlined from '@mui/icons-material/RadioButtonUncheckedOutlined';
import Box from '@mui/material/Box';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import type SvgIcon from '@mui/material/SvgIcon';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQueryClient } from '@tanstack/react-query';
import { type DragEvent, type MouseEvent, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Project, Schedule, ScheduleItem } from '../api/client';
import { queryKeys } from '../api/queries';
import { IconButton } from '../components/Button';
import { DueDate } from '../components/DueDate';
import { LogoTile } from '../components/LogoTile';
import { CardAvatar } from '../components/PersonCard';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge, type StatusTone } from '../components/StatusBadge';
import { formatDayText } from '../format/dates';
import { useNotify } from '../notices/notify';
import { theme } from '../theme/theme';
import { lateText, missesDeadline, ownerText, showsDelay } from './schedule';
import {
  type BoardCard,
  type BoardColumnId,
  boardColumnIds,
  boardColumns,
  moveOf,
  progressOf,
} from './scheduleBoard';
import { ProgressDialog } from './ScheduleItemProgress';
import { NoteMark, OwnerAway } from './ScheduleStatus';
import { saveTaskProgress } from './scheduleProgress';

/** The icon and the tone of each column, the same meanings as the statuses of the product (ADR-0065, ADR-0155). */
const columnLooks: Record<BoardColumnId, { tone: StatusTone; icon: typeof SvgIcon }> = {
  todo: { tone: 'neutral', icon: RadioButtonUncheckedOutlined },
  doing: { tone: 'info', icon: AutorenewOutlined },
  done: { tone: 'success', icon: CheckCircleOutlined },
};

const captionSx = {
  margin: 0,
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

/**
 * The Quadro of the tab Cronograma (ADR-0155): the tasks with their own dates in the columns A fazer, Em andamento and
 * Concluída, by their progress, with the count of each column; a card opens the details of its task. Who updates the
 * progress of a task (`canUpdateProgress`, ADR-0043) moves its card: dragging it, on a computer, or by "Mover", on any
 * screen. Concluída and A fazer save 100% and 0% at once; Em andamento asks for the percentage (ADR-0086). While the
 * progress is saved, the card stays in the new column; a refusal takes it back, with the notice.
 */
export function ScheduleBoard({
  project,
  schedule,
  selectedId,
  emptyText,
  onChoose,
}: {
  project: Project;
  /** The schedule the tab shows, already filtered. */
  schedule: Schedule;
  /** The task whose details are open. */
  selectedId: string | undefined;
  /** What the board says when no task passes the filter. */
  emptyText: string | undefined;
  onChoose: (itemId: string) => void;
}) {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  /** The cards whose progress is being saved, shown in the column they go to. */
  const [drafts, setDrafts] = useState<ReadonlyMap<string, BoardColumnId>>(() => new Map());
  /** The card being dragged, and the column under it that takes it. */
  const dragged = useRef<BoardCard>(undefined);
  const [over, setOver] = useState<BoardColumnId>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; card: BoardCard }>();
  const [asking, setAsking] = useState<ScheduleItem>();
  const columns = boardColumns(schedule, drafts);
  const cardCount = columns.reduce((count, column) => count + column.cards.length, 0);

  const refresh = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.projects }),
      queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
    ]);

  const move = async (card: BoardCard, to: BoardColumnId) => {
    const { item } = card;
    const how = moveOf(item, to);
    if (how === undefined || !item.canUpdateProgress || drafts.has(item.id)) {
      return;
    }
    const progress = progressOf(how);
    if (progress === undefined) {
      setAsking(item);
      return;
    }
    setDrafts((current) => new Map(current).set(item.id, to));
    const failed = await saveTaskProgress(project.id, item.id, progress, t);
    if (failed === undefined) {
      notify(
        t(
          how === 'complete' ? 'projects.schedule.messages.completed' : 'projects.board.restarted',
          {
            name: item.name,
          },
        ),
      );
      await refresh();
    } else {
      notify(failed, { tone: 'error' });
    }
    setDrafts((current) => {
      const next = new Map(current);
      next.delete(item.id);
      return next;
    });
  };

  if (cardCount === 0 && emptyText !== undefined) {
    return (
      <Box role="region" aria-label={t('projects.board.label')} sx={{ flex: 1, minHeight: 0 }}>
        <Box component="p" sx={{ ...captionSx, fontSize: 14, lineHeight: '20px' }}>
          {emptyText}
        </Box>
      </Box>
    );
  }

  return (
    <>
      <Box
        role="region"
        aria-label={t('projects.board.label')}
        sx={{
          flex: 1,
          minHeight: 0,
          minWidth: 0,
          display: 'grid',
          gap: 'var(--space-3)',
          // Side by side on a computer; on a phone, almost the width of the screen each, scrolled sideways.
          ...(desktop
            ? { gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }
            : {
                gridAutoFlow: 'column',
                gridAutoColumns: 'min(85%, 320px)',
                overflowX: 'auto',
                scrollSnapType: 'x mandatory',
              }),
        }}
      >
        {columns.map((column) => (
          <BoardColumnView
            key={column.id}
            id={column.id}
            cards={column.cards}
            over={over === column.id}
            selectedId={selectedId}
            drafts={drafts}
            draggable={desktop}
            clientName={project.client?.name ?? null}
            clientLogo={project.client?.logoUrl}
            onChoose={onChoose}
            onMenu={(anchor, card) => {
              setMenu({ anchor, card });
            }}
            onDragStart={(card) => {
              dragged.current = card;
            }}
            onDragEnd={() => {
              dragged.current = undefined;
              setOver(undefined);
            }}
            onDragOver={(event) => {
              const card = dragged.current;
              if (card === undefined || moveOf(card.item, column.id) === undefined) {
                return;
              }
              event.preventDefault();
              event.dataTransfer.dropEffect = 'move';
              setOver(column.id);
            }}
            onDragLeave={(event) => {
              if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
                setOver((current) => (current === column.id ? undefined : current));
              }
            }}
            onDrop={(event) => {
              event.preventDefault();
              const card = dragged.current;
              dragged.current = undefined;
              setOver(undefined);
              if (card !== undefined) {
                void move(card, column.id);
              }
            }}
          />
        ))}
      </Box>
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { minWidth: 220, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined
                ? undefined
                : t('projects.board.move', { name: menu.card.item.name }),
          },
        }}
      >
        {menu !== undefined &&
          boardColumnIds
            .filter((id) => moveOf(menu.card.item, id) !== undefined)
            .map((id) => {
              const Icon = columnLooks[id].icon;
              return (
                <MenuItem
                  key={id}
                  onClick={() => {
                    const { card } = menu;
                    setMenu(undefined);
                    void move(card, id);
                  }}
                >
                  <Icon aria-hidden />
                  {t('projects.board.moveTo', { column: t(`projects.board.columns.${id}`) })}
                </MenuItem>
              );
            })}
      </Menu>
      {asking !== undefined && (
        <ProgressDialog
          projectId={project.id}
          item={asking}
          onClose={() => {
            setAsking(undefined);
          }}
          onSaved={(message) => {
            setAsking(undefined);
            void refresh();
            notify(message);
          }}
        />
      )}
    </>
  );
}

/** A column of the board: its status, its count and its cards, scrolled inside (ADR-0066). */
function BoardColumnView({
  id,
  cards,
  over,
  selectedId,
  drafts,
  draggable,
  clientName,
  clientLogo,
  onChoose,
  onMenu,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDragLeave,
  onDrop,
}: {
  id: BoardColumnId;
  cards: BoardCard[];
  /** A dragged card is over the column, and the column takes it. */
  over: boolean;
  selectedId: string | undefined;
  drafts: ReadonlyMap<string, BoardColumnId>;
  draggable: boolean;
  clientName: string | null;
  clientLogo: string | null | undefined;
  onChoose: (itemId: string) => void;
  onMenu: (anchor: HTMLElement, card: BoardCard) => void;
  onDragStart: (card: BoardCard) => void;
  onDragEnd: () => void;
  onDragOver: (event: DragEvent<HTMLElement>) => void;
  onDragLeave: (event: DragEvent<HTMLElement>) => void;
  onDrop: (event: DragEvent<HTMLElement>) => void;
}) {
  const { t } = useTranslation();
  const headingId = useId();
  const { tone, icon } = columnLooks[id];
  const name = t(`projects.board.columns.${id}`);
  return (
    <Box
      component="section"
      aria-label={t('projects.board.column', { name, count: cards.length })}
      data-column={id}
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        minWidth: 0,
        boxSizing: 'border-box',
        backgroundColor: over ? 'var(--bg-selected)' : 'var(--bg-subtle)',
        border: '1px solid',
        borderColor: over ? 'var(--border-strong)' : 'var(--border)',
        borderRadius: 'var(--radius-lg)',
        scrollSnapAlign: 'start',
        transition: 'background-color 150ms ease-out, border-color 150ms ease-out',
      }}
    >
      <Box
        component="h3"
        id={headingId}
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-2)',
          margin: 0,
          padding: 'var(--space-3)',
          fontSize: 13,
          lineHeight: '18px',
          fontWeight: 500,
        }}
      >
        <StatusBadge tone={tone} icon={icon}>
          {name}
        </StatusBadge>
        <Box
          component="span"
          aria-hidden
          sx={{ color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}
        >
          {cards.length}
        </Box>
      </Box>
      <Box
        component="ul"
        aria-labelledby={headingId}
        sx={{
          flex: 1,
          minHeight: 0,
          overflowY: 'auto',
          listStyle: 'none',
          margin: 0,
          padding: '0 var(--space-2) var(--space-2)',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-2)',
        }}
      >
        {cards.length === 0 && (
          <Box component="li" sx={{ ...captionSx, padding: 'var(--space-2) var(--space-1)' }}>
            {t('projects.board.columnEmpty')}
          </Box>
        )}
        {cards.map((card) => (
          <BoardCardView
            key={card.item.id}
            card={card}
            column={id}
            selected={card.item.id === selectedId}
            saving={drafts.has(card.item.id)}
            draggable={draggable}
            clientName={clientName}
            clientLogo={clientLogo}
            onChoose={onChoose}
            onMenu={onMenu}
            onDragStart={onDragStart}
            onDragEnd={onDragEnd}
          />
        ))}
      </Box>
    </Box>
  );
}

/**
 * A card of the board (ADR-0155): the name, with the mark of the note, the phase and the task it is in, the owner, the
 * end or the delay, the deadline passed, the bar of the progress while in progress and the day it was completed. The
 * name is the button that opens the details; the rest of the card opens them too, by the pointer.
 */
function BoardCardView({
  card,
  column,
  selected,
  saving,
  draggable,
  clientName,
  clientLogo,
  onChoose,
  onMenu,
  onDragStart,
  onDragEnd,
}: {
  card: BoardCard;
  column: BoardColumnId;
  selected: boolean;
  saving: boolean;
  draggable: boolean;
  clientName: string | null;
  clientLogo: string | null | undefined;
  onChoose: (itemId: string) => void;
  onMenu: (anchor: HTMLElement, card: BoardCard) => void;
  onDragStart: (card: BoardCard) => void;
  onDragEnd: () => void;
}) {
  const { t } = useTranslation();
  const openRef = useRef<HTMLButtonElement>(null);
  const [dragging, setDragging] = useState(false);
  const { item, phase, parent } = card;
  const movable = item.canUpdateProgress && !saving;

  // The card opens the details, unless the click was on another control in it (the owner, "Mover") or outside it, in
  // what it opened (the card of the owner); the name opens them by the keyboard too, being a button.
  const open = (event: MouseEvent<HTMLElement>) => {
    const target = event.target as HTMLElement;
    if (!event.currentTarget.contains(target)) {
      return;
    }
    const control = target.closest('button, a');
    if (control !== null && control !== openRef.current) {
      return;
    }
    onChoose(item.id);
  };

  return (
    <Box
      component="li"
      data-item={item.id}
      aria-busy={saving || undefined}
      // Written out, as the attribute "false" stops the browser from dragging the text of a card that does not move.
      draggable={draggable && movable ? 'true' : 'false'}
      onClick={open}
      onDragStart={(event: DragEvent<HTMLElement>) => {
        if (!(draggable && movable)) {
          event.preventDefault();
          return;
        }
        event.dataTransfer.effectAllowed = 'move';
        event.dataTransfer.setData('text/plain', item.id);
        setDragging(true);
        onDragStart(card);
      }}
      onDragEnd={() => {
        setDragging(false);
        onDragEnd();
      }}
      sx={{
        flex: 'none',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        boxSizing: 'border-box',
        padding: 'var(--space-3)',
        backgroundColor: selected ? 'var(--bg-selected)' : 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        cursor: draggable && movable ? 'grab' : 'pointer',
        opacity: dragging || saving ? 0.6 : 1,
        transition: 'border-color 150ms ease-out, opacity 150ms ease-out',
        '&:hover': { borderColor: 'var(--border-strong)' },
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-1)', minWidth: 0 }}>
        <Box
          component="button"
          type="button"
          ref={openRef}
          sx={{
            flex: 1,
            minWidth: 0,
            margin: 0,
            padding: 0,
            border: 0,
            background: 'none',
            textAlign: 'left',
            font: 'inherit',
            fontSize: 14,
            lineHeight: '20px',
            fontWeight: 500,
            color: 'var(--text)',
            cursor: 'inherit',
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
            borderRadius: 'var(--radius-sm)',
            '&:focus-visible': {
              outline: '2px solid var(--focus-ring)',
              outlineOffset: '2px',
            },
          }}
        >
          {item.name}
        </Box>
        <Box sx={{ flex: 'none', display: 'flex', alignItems: 'center', minHeight: 20 }}>
          <NoteMark note={item.note} />
        </Box>
        {item.canUpdateProgress && (
          <IconButton
            size="small"
            aria-label={t('projects.board.move', { name: item.name })}
            title={t('projects.board.move', { name: item.name })}
            aria-haspopup="menu"
            disabled={saving}
            onClick={(event) => {
              onMenu(event.currentTarget, card);
            }}
            sx={{ flex: 'none', margin: '-4px -4px -4px 0', color: 'var(--text-secondary)' }}
          >
            <OpenWithOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
          </IconButton>
        )}
      </Box>
      <Box component="p" sx={{ ...captionSx, ...ellipsisSx }}>
        {parent === undefined ? phase.name : `${phase.name} · ${parent.name}`}
      </Box>
      {column === 'doing' && (
        <ProgressBar
          value={item.progress}
          label={t('projects.schedule.itemProgress', { name: item.name })}
          width="100%"
        />
      )}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 'var(--space-1) var(--space-2)',
          minWidth: 0,
        }}
      >
        <CardOwner item={item} clientName={clientName} clientLogo={clientLogo} />
        <CardDate item={item} column={column} />
      </Box>
      {missesDeadline(item) && (
        <DueDate state="overdue">
          {t('projects.schedule.deadline.past', { count: item.deadlineDeviation ?? 0 })}
        </DueDate>
      )}
    </Box>
  );
}

/** Who is in charge of the task on its card: the person, the client of the project (ADR-0102) or nobody. */
function CardOwner({
  item,
  clientName,
  clientLogo,
}: {
  item: ScheduleItem;
  clientName: string | null;
  clientLogo: string | null | undefined;
}) {
  const { t } = useTranslation();
  const rowSx = {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-2)',
    minWidth: 0,
    flex: '1 1 auto',
    fontSize: 13,
    lineHeight: '18px',
  } as const;
  if (item.ownedByClient) {
    return (
      <Box sx={rowSx}>
        <LogoTile name={clientName ?? ''} src={clientLogo} />
        <Box component="span" sx={ellipsisSx}>
          {ownerText(item, clientName, t)}
        </Box>
      </Box>
    );
  }
  if (item.owner === null) {
    return (
      <Box component="span" sx={{ ...rowSx, color: 'var(--text-secondary)' }}>
        {t('projects.schedule.noOwner')}
      </Box>
    );
  }
  return (
    <Box sx={rowSx}>
      <CardAvatar person={item.owner} size="sm" />
      <Box component="span" sx={ellipsisSx}>
        {item.owner.name}
      </Box>
      <OwnerAway item={item} />
    </Box>
  );
}

/** The day of the card: the completion in Concluída, the delay of a late task, or the end. */
function CardDate({ item, column }: { item: ScheduleItem; column: BoardColumnId }) {
  const { t } = useTranslation();
  if (column === 'done' && item.completedOn !== null) {
    return (
      <DueDate state="met">
        {t('projects.schedule.completedOn', { day: formatDayText(item.completedOn) })}
      </DueDate>
    );
  }
  if (showsDelay(item)) {
    return <DueDate state="overdue">{lateText(item, t)}</DueDate>;
  }
  return (
    <Box component="span" sx={{ ...captionSx, fontVariantNumeric: 'tabular-nums' }}>
      {t('projects.board.endsOn', { day: formatDayText(item.end) })}
    </Box>
  );
}
