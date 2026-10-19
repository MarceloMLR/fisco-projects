import { useTranslation } from 'react-i18next';
import type { ProjectStatus } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { projectStatusLook } from './projectStatus';

/** The status of a project: icon, color and text (ADR-0065). */
export function ProjectStatusBadge({ status }: { status: ProjectStatus }) {
  const { t } = useTranslation();
  const { tone, icon } = projectStatusLook[status];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {t(`projects.statuses.${status}`)}
    </StatusBadge>
  );
}
