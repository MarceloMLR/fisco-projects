import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import UpdateOutlined from '@mui/icons-material/UpdateOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { ScheduleItem } from '../api/client';
import { projectionTexts } from './schedule';
import { DetailsSection } from './ScheduleItemProgress';

const textSx = { fontSize: 14, lineHeight: '20px', margin: 0 } as const;

/**
 * "Projeção do atraso" in the details of a late task (ADR-0164), for who edits and who only sees the schedule: the end
 * of what is left at the planned pace and the business days past the planned one, each milestone it pushes and the end
 * of the project, or that nothing moves, and that the dates change only when who edits the schedule decides
 * (ADR-0051, ADR-0140). Nothing without a projection.
 */
export function ProjectionSection({
  item,
}: {
  item: Pick<ScheduleItem, 'name' | 'progress' | 'projection'>;
}) {
  const { t } = useTranslation();
  if (item.projection === null) {
    return null;
  }
  const { summary, pushes } = projectionTexts({ ...item, projection: item.projection }, t);
  const moves = item.projection.milestones.length > 0 || item.projection.projectEnd !== null;
  const Icon = moves ? UpdateOutlined : CheckCircleOutlined;
  return (
    <DetailsSection title={t('projects.schedule.projection.title')}>
      <Box component="p" sx={textSx}>
        {summary}
      </Box>
      <Box
        component="ul"
        aria-label={t('projects.schedule.projection.list', { name: item.name })}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-1)',
          margin: 0,
          padding: 0,
          listStyle: 'none',
        }}
      >
        {pushes.map((push) => (
          <Box
            component="li"
            key={push}
            data-state={moves ? 'pushes' : 'absorbed'}
            sx={{
              display: 'flex',
              alignItems: 'flex-start',
              gap: 'var(--space-1)',
              fontSize: 13,
              lineHeight: '18px',
              fontWeight: 500,
              color: moves ? 'var(--status-warning-text)' : 'var(--status-success-text)',
            }}
          >
            <Icon aria-hidden sx={{ fontSize: 16, marginTop: '1px', flex: 'none' }} />
            {push}
          </Box>
        ))}
      </Box>
      <Box
        component="p"
        sx={{ fontSize: 13, lineHeight: '18px', margin: 0, color: 'var(--text-secondary)' }}
      >
        {t('projects.schedule.projection.help')}
      </Box>
    </DetailsSection>
  );
}
