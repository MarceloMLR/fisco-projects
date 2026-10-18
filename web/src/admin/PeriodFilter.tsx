import CalendarTodayOutlined from '@mui/icons-material/CalendarTodayOutlined';
import Box from '@mui/material/Box';
import Popover from '@mui/material/Popover';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { fieldControl } from '../components/fieldControl';
import { TextField } from '../components/TextField';
import { type DayValue, formatDayValue, formatPeriod } from '../format/dates';

export interface Period {
  from: DayValue;
  to: DayValue;
}

/**
 * Period filter of the Audit (fp-field with the calendar icon, as in the canvas): shows the first and the last
 * day and opens two date fields, From and To. Both days are included. The free MUI X has no range picker, so the
 * fields are the browser's own date fields.
 */
export function PeriodFilter({
  value,
  onChange,
  width,
}: {
  value: Period;
  onChange: (period: Period) => void;
  width: number;
}) {
  const { t } = useTranslation();
  const dialogId = useId();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <Box
        component="button"
        type="button"
        aria-haspopup="dialog"
        aria-expanded={anchor !== null}
        aria-controls={anchor === null ? undefined : dialogId}
        aria-label={t('audit.filters.periodValue', {
          from: formatDayValue(value.from),
          to: formatDayValue(value.to),
        })}
        onClick={(event) => {
          setAnchor(event.currentTarget);
        }}
        sx={{
          ...fieldControl('sm', width),
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-2)',
          padding: '0 var(--space-3)',
          fontFamily: 'var(--font-sans)',
          fontVariantNumeric: 'tabular-nums',
          textAlign: 'left',
          cursor: 'pointer',
          '&:focus-visible': {
            outline: 'none',
            borderColor: 'var(--focus-ring)',
            boxShadow: 'inset 0 0 0 1px var(--focus-ring)',
          },
        }}
      >
        <CalendarTodayOutlined
          aria-hidden
          sx={{ fontSize: 'var(--icon-md)', color: 'var(--text-secondary)' }}
        />
        {formatPeriod(value.from, value.to)}
      </Box>
      <Popover
        open={anchor !== null}
        anchorEl={anchor}
        onClose={() => {
          setAnchor(null);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        slotProps={{
          paper: {
            id: dialogId,
            role: 'dialog',
            'aria-label': t('audit.filters.period'),
            sx: {
              marginTop: 'var(--space-1)',
              padding: 'var(--space-3)',
              display: 'flex',
              gap: 'var(--space-3)',
            },
          },
        }}
      >
        <TextField
          label={t('audit.filters.from')}
          type="date"
          size="sm"
          width={156}
          value={value.from}
          max={value.to}
          onChange={(event) => {
            const from = event.target.value;
            if (from !== '') {
              onChange({ from, to: from > value.to ? from : value.to });
            }
          }}
        />
        <TextField
          label={t('audit.filters.to')}
          type="date"
          size="sm"
          width={156}
          value={value.to}
          min={value.from}
          onChange={(event) => {
            const to = event.target.value;
            if (to !== '') {
              onChange({ from: to < value.from ? to : value.from, to });
            }
          }}
        />
      </Popover>
    </>
  );
}
