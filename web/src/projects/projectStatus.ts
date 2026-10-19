import AutorenewOutlined from '@mui/icons-material/AutorenewOutlined';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import EventNoteOutlined from '@mui/icons-material/EventNoteOutlined';
import PauseCircleOutlined from '@mui/icons-material/PauseCircleOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import type { ProjectStatus } from '../api/client';
import type { StatusTone } from '../components/StatusBadge';

/** The tone and the icon of each status of a project, as the design system has them (StatusBadge kind project). */
export const projectStatusLook: Record<ProjectStatus, { tone: StatusTone; icon: typeof SvgIcon }> =
  {
    Planning: { tone: 'neutral', icon: EventNoteOutlined },
    InProgress: { tone: 'info', icon: AutorenewOutlined },
    Paused: { tone: 'warning', icon: PauseCircleOutlined },
    Completed: { tone: 'success', icon: CheckCircleOutlined },
    Cancelled: { tone: 'neutral', icon: BlockOutlined },
  };
