import BlockOutlined from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import VisibilityOutlined from '@mui/icons-material/VisibilityOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import { useTranslation } from 'react-i18next';
import type { DocumentStatus } from '../api/client';
import { StatusBadge, type StatusTone } from '../components/StatusBadge';

/** The look of each status of a document (StatusBadge kind document of the design system). */
const documentStatusLook: Record<DocumentStatus, { tone: StatusTone; icon: typeof SvgIcon }> = {
  Draft: { tone: 'neutral', icon: EditOutlined },
  InReview: { tone: 'warning', icon: VisibilityOutlined },
  Approved: { tone: 'success', icon: CheckCircleOutlined },
  Obsolete: { tone: 'neutral', icon: BlockOutlined },
};

/** The status of a document (ADR-0031): icon, color and text (ADR-0065). */
export function DocumentStatusBadge({ status }: { status: DocumentStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = documentStatusLook[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`documents.statuses.${status}`)}
    </StatusBadge>
  );
}
