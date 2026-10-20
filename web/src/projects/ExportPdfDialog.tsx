import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '../components/Dialog';
import { SegmentedControl } from '../components/SegmentedControl';
import { TextField } from '../components/TextField';
import type { DayValue } from '../format/dates';
import { type GanttScale, ganttScales } from '../schedule-gantt/ganttLayout';
import type { ScheduleDisplay } from './schedule';
import { printAddress } from './schedulePrint';

/**
 * "Exportar em PDF" (ADR-0021): the period, from the start to the end of the schedule at first, and the scale, the one
 * of the tab at first; it opens the print page in a new tab, where the browser saves it as PDF.
 */
export function ExportPdfDialog({
  projectId,
  start,
  end,
  scale,
  display,
  onClose,
}: {
  projectId: string;
  start: DayValue;
  end: DayValue;
  scale: GanttScale;
  display: ScheduleDisplay;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const [from, setFrom] = useState(start);
  const [to, setTo] = useState(end);
  const [chosen, setChosen] = useState(scale);
  return (
    <Dialog
      open
      title={t('projects.export.dialog.title')}
      onClose={onClose}
      onSubmit={(event) => {
        event.preventDefault();
        window.open(
          printAddress(projectId, { from, to, scale: chosen, display }),
          '_blank',
          'noopener',
        );
        onClose();
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.export.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projects.export.dialog.open')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {t('projects.export.dialog.text')}
        </Box>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-3)' }}>
          <TextField
            label={t('projects.export.dialog.from')}
            type="date"
            width={168}
            value={from}
            max={to}
            onChange={(event) => {
              const day = event.target.value;
              if (day !== '') {
                setFrom(day);
                if (day > to) {
                  setTo(day);
                }
              }
            }}
          />
          <TextField
            label={t('projects.export.dialog.to')}
            type="date"
            width={168}
            value={to}
            min={from}
            onChange={(event) => {
              const day = event.target.value;
              if (day !== '') {
                setTo(day);
                if (day < from) {
                  setFrom(day);
                }
              }
            }}
          />
        </Box>
        <SegmentedControl<GanttScale>
          label={t('projects.timeline.scales.label')}
          value={chosen}
          options={ganttScales.map((value) => ({
            value,
            label: t(`projects.timeline.scales.${value}`),
          }))}
          onChange={setChosen}
        />
      </Box>
    </Dialog>
  );
}
