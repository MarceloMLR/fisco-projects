import BlockOutlined from '@mui/icons-material/BlockOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import { useTranslation } from 'react-i18next';
import type { AcceptanceSignerStatus, AcceptanceStatus } from '../api/client';
import { StatusBadge, type StatusTone } from '../components/StatusBadge';

/** The look of each situation of a term (StatusBadge kind acceptance of the design system). */
const acceptanceStatusLook: Record<AcceptanceStatus, { tone: StatusTone; icon: typeof SvgIcon }> = {
  Pending: { tone: 'warning', icon: ScheduleOutlined },
  Signed: { tone: 'success', icon: TaskAltOutlined },
  Refused: { tone: 'danger', icon: BlockOutlined },
};

/** The situation of an acceptance term (ADR-0035): icon, color and text (ADR-0065). */
export function AcceptanceStatusBadge({ status }: { status: AcceptanceStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = acceptanceStatusLook[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`acceptances.statuses.${status}`)}
    </StatusBadge>
  );
}

/** The look of each situation of a signer at the provider (ADR-0129), as the situations of the term. */
const signerStatusLook: Record<AcceptanceSignerStatus, { tone: StatusTone; icon: typeof SvgIcon }> =
  {
    Pending: { tone: 'warning', icon: ScheduleOutlined },
    Signed: { tone: 'success', icon: TaskAltOutlined },
    Refused: { tone: 'danger', icon: BlockOutlined },
  };

/** The situation of a signer of a request: icon, color and text (ADR-0065). */
export function SignerStatusBadge({ status }: { status: AcceptanceSignerStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = signerStatusLook[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`acceptances.signerStatuses.${status}`)}
    </StatusBadge>
  );
}
