import CalendarMonthOutlined from '@mui/icons-material/CalendarMonthOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import InboxOutlined from '@mui/icons-material/InboxOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import TodayOutlined from '@mui/icons-material/TodayOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import type SvgIcon from '@mui/material/SvgIcon';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { MyWork, MyWorkGroup, MyWorkItem, MyWorkView } from '../api/client';
import { AcceptanceStatusBadge } from '../acceptances/AcceptanceStatusBadge';
import { CodeChip } from '../components/Card';
import { DueDate } from '../components/DueDate';
import { useDates } from '../format/useDates';
import { TicketStatusBadge } from '../tickets/TicketTags';
import { dueOf, groupLabel, itemPath, itemTitle, metaText } from './myWork';

const groupLooks: Record<MyWorkGroup, { icon: typeof SvgIcon; color: string }> = {
  Late: { icon: WarningAmberOutlined, color: 'var(--status-danger-text)' },
  Waiting: { icon: ScheduleOutlined, color: 'var(--status-warning-text)' },
  Soon: { icon: TodayOutlined, color: 'var(--text)' },
  Week: { icon: CalendarMonthOutlined, color: 'var(--text)' },
};

/** The icon of a line: a task, a ticket, a ticket nobody took yet or a term. */
function ItemIcon({ item }: { item: MyWorkItem }) {
  const sx = { fontSize: 18 };
  if (item.ticket !== null) {
    return item.ticket.assignee === null ? (
      <InboxOutlined aria-hidden sx={sx} />
    ) : (
      <ConfirmationNumberOutlined aria-hidden sx={sx} />
    );
  }
  return item.kind === 'Task' ? (
    <ViewTimelineOutlined aria-hidden sx={sx} />
  ) : (
    <TaskAltOutlined aria-hidden sx={sx} />
  );
}

/** The actions done on the line itself; the others lead to the item. */
const actionsHere = new Set(['Assign', 'Start', 'Update', 'Complete']);

const titleSx = {
  display: 'block',
  fontWeight: 500,
  color: 'var(--text)',
  textDecoration: 'none',
  '&:hover': { color: 'var(--action-text)', textDecoration: 'underline' },
} as const;

const metaSx = {
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  minWidth: 0,
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  whiteSpace: 'nowrap',
  overflow: 'hidden',
} as const;

/**
 * The queue of Meu trabalho (ADR-0070, canvas Main and CelularMeuTrabalho): the groups of the view with their icon and
 * count, and in each line the kind, the title, the code of the project, the detail, the situation, the deadline and
 * the one action. On the phone, each line has two layers: the title, then the code, the kind and the deadline.
 */
export function MyWorkQueue({
  work,
  view,
  userId,
  desktop,
  busy,
  onAction,
}: {
  work: MyWork;
  view: MyWorkView;
  userId: string;
  desktop: boolean;
  /** The line whose action is being saved. */
  busy?: string;
  /** An action done on the line itself: Atribuir, Iniciar, Atualizar or Concluir. */
  onAction: (item: MyWorkItem) => void;
}) {
  const { t } = useTranslation();
  const dates = useDates();
  const groups = work.views.find((entry) => entry.view === view)?.groups ?? [];

  return (
    <Box
      component="section"
      aria-label={t('myWork.queue')}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        // On the computer the queue takes the height of its column and scrolls inside; on the phone, the page scrolls.
        ...(desktop
          ? { flex: 1, minHeight: 0, overflow: 'auto' }
          : { flex: 'none', overflow: 'hidden' }),
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
      }}
    >
      {groups.length === 0 && (
        <Box
          sx={{
            padding: 'var(--space-6) var(--space-4)',
            textAlign: 'center',
            color: 'var(--text-secondary)',
          }}
        >
          {t(`myWork.empty.${view}`)}
        </Box>
      )}
      {groups.map((group) => {
        const look = groupLooks[group.group];
        const GroupIcon = look.icon;
        const label = groupLabel(group.group, work.today, work.nextBusinessDay, t);
        return (
          <Box
            key={group.group}
            component="section"
            aria-label={label}
            sx={{ display: 'contents' }}
          >
            <Box
              component="h2"
              sx={{
                position: 'sticky',
                top: 0,
                zIndex: 1,
                flex: 'none',
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                height: 36,
                margin: 0,
                padding: desktop ? '0 var(--space-4)' : '0 12px',
                backgroundColor: 'var(--bg-subtle)',
                borderBottom: '1px solid var(--border)',
                fontSize: 13,
                lineHeight: '18px',
                fontWeight: 500,
                color: look.color,
              }}
            >
              <GroupIcon
                aria-hidden
                sx={{
                  fontSize: 18,
                  color:
                    group.group === 'Late' || group.group === 'Waiting'
                      ? look.color
                      : 'var(--text-secondary)',
                }}
              />
              <span>{label}</span>
              <Box
                component="span"
                sx={{
                  color: 'var(--text-secondary)',
                  fontWeight: 400,
                  fontVariantNumeric: 'tabular-nums',
                }}
              >
                {group.items.length}
              </Box>
            </Box>
            {group.items.map((item) =>
              desktop ? (
                <QueueRow
                  key={item.id}
                  item={item}
                  group={group.group}
                  today={work.today}
                  userId={userId}
                  busy={busy === item.id}
                  onAction={onAction}
                  dates={dates}
                />
              ) : (
                <PhoneRow key={item.id} item={item} group={group.group} today={work.today} />
              ),
            )}
          </Box>
        );
      })}
    </Box>
  );
}

function QueueRow({
  item,
  group,
  today,
  userId,
  busy,
  onAction,
  dates,
}: {
  item: MyWorkItem;
  group: MyWorkGroup;
  today: string;
  userId: string;
  busy: boolean;
  onAction: (item: MyWorkItem) => void;
  dates: ReturnType<typeof useDates>;
}) {
  const { t } = useTranslation();
  const due = dueOf(item, group, today, t);
  const title = itemTitle(item);
  const action = t(`myWork.actions.${item.action}`);
  const label = t('myWork.actionLabel', { action, title });
  return (
    <Box
      sx={{
        flex: 'none',
        display: 'grid',
        gridTemplateColumns: '28px minmax(0, 1fr) 168px 168px 104px',
        alignItems: 'center',
        gap: 'var(--space-3)',
        minHeight: 56,
        boxSizing: 'border-box',
        padding: 'var(--space-2) var(--space-4)',
        borderBottom: '1px solid var(--border)',
        '&:last-child': { borderBottom: 0 },
        '&:hover': { backgroundColor: 'var(--bg-hover)' },
      }}
    >
      <Box
        component="span"
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          width: 28,
          height: 28,
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'var(--bg-subtle)',
          color: 'var(--text-secondary)',
        }}
      >
        <ItemIcon item={item} />
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Box
          component={Link}
          to={itemPath(item)}
          sx={{ ...titleSx, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}
        >
          {title}
        </Box>
        <Box sx={{ ...metaSx, marginTop: '2px' }}>
          <CodeChip code={item.project.code} />
          <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {metaText(item, userId, dates, t)}
          </Box>
        </Box>
      </Box>
      <span>
        {item.ticket !== null && <TicketStatusBadge status={item.ticket.status} />}
        {item.acceptance !== null && <AcceptanceStatusBadge status="Pending" />}
      </span>
      <span>
        <DueDate state={due.state}>{due.text}</DueDate>
      </span>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
        {actionsHere.has(item.action) ? (
          <Button
            variant="outlined"
            size="small"
            aria-label={label}
            disabled={busy}
            onClick={() => {
              onAction(item);
            }}
          >
            {action}
          </Button>
        ) : (
          <Button
            component={Link}
            to={itemPath(item)}
            variant="outlined"
            size="small"
            aria-label={label}
          >
            {action}
          </Button>
        )}
      </Box>
    </Box>
  );
}

function PhoneRow({ item, group, today }: { item: MyWorkItem; group: MyWorkGroup; today: string }) {
  const { t } = useTranslation();
  const due = dueOf(item, group, today, t);
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: '6px',
        padding: '10px 12px',
        borderBottom: '1px solid var(--border)',
        '&:last-child': { borderBottom: 0 },
      }}
    >
      <Box component={Link} to={itemPath(item)} sx={titleSx}>
        {itemTitle(item)}
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-2)',
        }}
      >
        <Box sx={metaSx}>
          <CodeChip code={item.project.code} />
          <span>{t(`myWork.kinds.${item.kind}`)}</span>
        </Box>
        <DueDate state={due.state}>{due.text}</DueDate>
      </Box>
    </Box>
  );
}
