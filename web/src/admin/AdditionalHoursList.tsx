import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import { type MouseEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { AdditionalHours } from '../api/client';
import { Dialog } from '../components/Dialog';
import { formatDayText } from '../format/dates';
import { formatHours } from '../hours/hours';
import { hoursTableSx, subLineSx } from './hoursTable';
import type { HoursProject } from './ProjectHoursDialogs';
import { Button, IconButton } from '../components/Button';

/** How many additional hours the drawer shows, the newest; the others are in "Ver todas". */
export const shownAdditional = 3;

/** How many additional hours a page of "Ver todas" shows. */
export const additionalPageSize = 10;

/** The header and ten rows of one line of reason and the line of who registered them. */
const fullPageHeight = 40 + additionalPageSize * 57;

/** A long reason goes in two lines, with the whole of it on hover and in "Corrigir". */
const reasonSx = {
  display: '-webkit-box',
  WebkitBoxOrient: 'vertical',
  WebkitLineClamp: 2,
  overflow: 'hidden',
  overflowWrap: 'anywhere',
} as const;

/**
 * Additional hours of a project in a table (canvas Horas do projeto, AdminProjeto, ADR-0128): the day, the hours, the
 * reason, in up to two lines, with who registered them, and the "⋮" to correct or delete each one.
 */
export function AdditionalHoursTable({
  items,
  label,
  onActions,
}: {
  items: readonly AdditionalHours[];
  /** Accessible name of the table, when it is not the one of its section. */
  label?: string;
  onActions: (event: MouseEvent<HTMLElement>, item: AdditionalHours) => void;
}) {
  const { t } = useTranslation();
  return (
    <Box component="table" aria-label={label} sx={hoursTableSx}>
      <thead>
        <tr>
          <Box component="th" scope="col" sx={{ width: 104 }}>
            {t('projectHours.details.columns.day')}
          </Box>
          <Box component="th" scope="col" className="fp-right" sx={{ width: 72 }}>
            {t('projectHours.details.columns.hours')}
          </Box>
          <th scope="col">{t('projectHours.details.columns.reason')}</th>
          <Box component="th" scope="col" sx={{ width: 52 }}>
            <Box component="span" sx={visuallyHidden}>
              {t('projectHours.details.columns.actions')}
            </Box>
          </Box>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.id}>
            <td>{formatDayText(item.day)}</td>
            <td className="fp-right">{formatHours(item.hours)}</td>
            <td>
              <Box component="span" title={item.reason} sx={reasonSx}>
                {item.reason}
              </Box>
              <Box component="span" sx={subLineSx}>
                {t('projectHours.details.createdBy', { name: item.createdBy.name })}
              </Box>
            </td>
            <td className="fp-right">
              <IconButton
                size="small"
                aria-label={t('projectHours.additionalMenu.button', {
                  day: formatDayText(item.day),
                })}
                aria-haspopup="menu"
                onClick={(event) => {
                  onActions(event, item);
                }}
                sx={{ color: 'var(--text-secondary)' }}
              >
                <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
              </IconButton>
            </td>
          </tr>
        ))}
      </tbody>
    </Box>
  );
}

/**
 * "Ver todas" the additional hours of a project (ADR-0128): every one of them, the newest first, ten at a time, with
 * the page before and after, and the same "⋮" of the drawer.
 */
export function AdditionalHoursListDialog({
  project,
  items,
  onActions,
  onClose,
}: {
  project: HoursProject;
  items: readonly AdditionalHours[];
  onActions: (event: MouseEvent<HTMLElement>, item: AdditionalHours) => void;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const [page, setPage] = useState(0);
  const pages = Math.max(1, Math.ceil(items.length / additionalPageSize));
  // A deletion may leave the last page empty: it shows the one before.
  const current = Math.min(page, pages - 1);
  const from = current * additionalPageSize;
  const shown = items.slice(from, from + additionalPageSize);

  return (
    <Dialog
      open
      size="lg"
      title={t('projectHours.allAdditional.title')}
      subtitle={t('projectHours.entries.subtitle', { code: project.code, name: project.name })}
      onClose={onClose}
      actions={
        <Button variant="outlined" onClick={onClose}>
          {t('projectHours.allAdditional.close')}
        </Button>
      }
    >
      {/* With more than a page, the table keeps the height of a full page, so the dialog does not jump when paging. */}
      <Box sx={pages > 1 ? { minHeight: fullPageHeight } : undefined}>
        <AdditionalHoursTable
          items={shown}
          label={t('projectHours.allAdditional.table')}
          onActions={onActions}
        />
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          gap: 'var(--space-2)',
          fontSize: 13,
          lineHeight: '18px',
          color: 'var(--text-secondary)',
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        <span aria-live="polite">
          {t('projectHours.allAdditional.range', {
            from: shown.length === 0 ? 0 : from + 1,
            to: from + shown.length,
            count: items.length,
          })}
        </span>
        <IconButton
          size="small"
          aria-label={t('projectHours.allAdditional.previous')}
          disabled={current === 0}
          onClick={() => {
            setPage(current - 1);
          }}
        >
          <ChevronLeftOutlined />
        </IconButton>
        <IconButton
          size="small"
          aria-label={t('projectHours.allAdditional.next')}
          disabled={current >= pages - 1}
          onClick={() => {
            setPage(current + 1);
          }}
        >
          <ChevronRightOutlined />
        </IconButton>
      </Box>
    </Dialog>
  );
}
