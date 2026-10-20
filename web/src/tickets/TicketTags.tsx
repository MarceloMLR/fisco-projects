import AutorenewOutlined from '@mui/icons-material/AutorenewOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import DragHandleOutlined from '@mui/icons-material/DragHandleOutlined';
import FilterAltOutlined from '@mui/icons-material/FilterAltOutlined';
import InboxOutlined from '@mui/icons-material/InboxOutlined';
import KeyboardArrowDownOutlined from '@mui/icons-material/KeyboardArrowDownOutlined';
import KeyboardArrowUpOutlined from '@mui/icons-material/KeyboardArrowUpOutlined';
import KeyboardDoubleArrowUpOutlined from '@mui/icons-material/KeyboardDoubleArrowUpOutlined';
import LockOutlined from '@mui/icons-material/LockOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { TicketPriority, TicketStatus, VendorTicketStatus } from '../api/client';
import { Avatar } from '../components/Avatar';
import { StatusBadge, type StatusTone } from '../components/StatusBadge';

const statuses: Record<TicketStatus, { tone: StatusTone; icon: typeof SvgIcon }> = {
  Open: { tone: 'neutral', icon: InboxOutlined },
  Triage: { tone: 'neutral', icon: FilterAltOutlined },
  InProgress: { tone: 'info', icon: AutorenewOutlined },
  WaitingForRequester: { tone: 'warning', icon: ScheduleOutlined },
  Resolved: { tone: 'success', icon: CheckCircleOutlined },
  Closed: { tone: 'neutral', icon: LockOutlined },
};

/** The status of a ticket (StatusBadge kind ticket): icon, color and text (ADR-0065). */
export function TicketStatusBadge({ status }: { status: TicketStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = statuses[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`tickets.statuses.${status}`)}
    </StatusBadge>
  );
}

const vendorStatuses: Record<VendorTicketStatus, { tone: StatusTone; icon: typeof SvgIcon }> = {
  Open: { tone: 'neutral', icon: InboxOutlined },
  InAnalysis: { tone: 'info', icon: AutorenewOutlined },
  WaitingForUs: { tone: 'warning', icon: ScheduleOutlined },
  Resolved: { tone: 'success', icon: CheckCircleOutlined },
  Closed: { tone: 'neutral', icon: LockOutlined },
};

/** The status of a ticket at a vendor (StatusBadge kind vendor, ADR-0028): icon, color and text (ADR-0065). */
export function VendorStatusBadge({ status }: { status: VendorTicketStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = vendorStatuses[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`tickets.vendors.statuses.${status}`)}
    </StatusBadge>
  );
}

const priorities: Record<TicketPriority, { icon: typeof SvgIcon; color: string; strong: boolean }> =
  {
    Low: { icon: KeyboardArrowDownOutlined, color: 'var(--text-secondary)', strong: false },
    Medium: { icon: DragHandleOutlined, color: 'var(--text-secondary)', strong: false },
    High: { icon: KeyboardArrowUpOutlined, color: 'var(--status-warning-text)', strong: true },
    Critical: {
      icon: KeyboardDoubleArrowUpOutlined,
      color: 'var(--status-danger-text)',
      strong: true,
    },
  };

/** The priority of a ticket (PriorityTag, fp-priority): icon, color and text; high and critical stand out. */
export function PriorityTag({ priority }: { priority: TicketPriority }) {
  const { t } = useTranslation();
  const { icon: Icon, color, strong } = priorities[priority];
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '2px',
        fontSize: 13,
        lineHeight: '18px',
        fontWeight: strong ? 500 : 400,
        color,
        whiteSpace: 'nowrap',
      }}
    >
      <Icon aria-hidden sx={{ fontSize: 18 }} />
      {t(`tickets.priorities.${priority}`)}
    </Box>
  );
}

/** A person of a ticket (Person, fp-person): the avatar and the name; without one, "Sem responsável" (ADR-0068). */
export function TicketPersonTag({ name }: { name: string | undefined }) {
  const { t } = useTranslation();
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
      <Avatar name={name} size="sm" />
      <Box
        component="span"
        sx={{
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
          color: name === undefined ? 'var(--text-secondary)' : 'var(--text)',
        }}
      >
        {name ?? t('tickets.noAssignee')}
      </Box>
    </Box>
  );
}
