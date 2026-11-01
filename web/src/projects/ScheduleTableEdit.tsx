import EditOutlined from '@mui/icons-material/EditOutlined';
import Box from '@mui/material/Box';
import InputBase from '@mui/material/InputBase';
import Paper from '@mui/material/Paper';
import Popper from '@mui/material/Popper';
import { type GridRenderEditCellParams, useGridApiContext } from '@mui/x-data-grid';
import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Schedule } from '../api/client';
import { Button } from '../components/Button';
import { fieldControl } from '../components/fieldControl';
import { MultiSelect } from '../components/MultiSelect';
import { maskTypedDay } from '../format/dates';
import { predecessorChoices, type ScheduleRow } from './schedule';
import { cellError, type CellEdit, type EditableField } from './scheduleTableEdit';

/** A line of the Lista, with the cell edited in the mode "Editar na tabela" until it is saved (ADR-0143). */
export type ListRow = ScheduleRow & { edit?: CellEdit };

/** "Editar na tabela": turns the mode on and off, pressed while on (ADR-0143). */
export function EditInTableButton({
  on,
  onChange,
}: {
  on: boolean;
  onChange: (on: boolean) => void;
}) {
  const { t } = useTranslation();
  return (
    <Button
      variant="outlined"
      size="small"
      startIcon={<EditOutlined />}
      aria-pressed={on}
      onClick={() => {
        onChange(!on);
      }}
      sx={on ? { color: 'var(--action-text)', backgroundColor: 'var(--bg-selected)' } : undefined}
    >
      {t('projects.tableEdit.button')}
    </Button>
  );
}

/** The line that says how to edit by the keyboard, under the bar, while the mode is on. */
export function TableEditHelp() {
  const { t } = useTranslation();
  return (
    <Box
      component="p"
      role="status"
      sx={{ margin: 0, fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
    >
      {t('projects.tableEdit.help')}
    </Box>
  );
}

const hiddenSx = {
  position: 'absolute',
  width: 1,
  height: 1,
  overflow: 'hidden',
  clip: 'rect(0 0 0 0)',
  whiteSpace: 'nowrap',
} as const;

/**
 * The text of a cell being edited: a field of the design system inside the cell, the day masked as dd/mm/aaaa, and the
 * reason of a value that cannot be saved in the danger color, on hover and for who reads the screen (ADR-0143).
 */
export function ScheduleEditCell({
  params,
  label,
  day = false,
  numeric = false,
}: {
  params: GridRenderEditCellParams<ListRow>;
  label: string;
  day?: boolean;
  numeric?: boolean;
}) {
  const { t } = useTranslation();
  const apiRef = useGridApiContext();
  const input = useRef<HTMLInputElement>(null);
  const helpId = useId();
  const { id, field, row, hasFocus } = params;
  const value = typeof params.value === 'string' ? params.value : '';
  const error =
    row.kind === 'item' ? cellError(field as EditableField, value, row.item) : undefined;
  const reason = error === undefined ? undefined : t(error);

  useEffect(() => {
    if (hasFocus) {
      input.current?.focus();
    }
  }, [hasFocus]);

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        width: '100%',
        height: '100%',
        padding: '0 var(--space-1)',
      }}
    >
      <InputBase
        inputRef={input}
        value={value}
        title={reason}
        inputProps={{
          'aria-label': label,
          'aria-invalid': error === undefined ? undefined : true,
          'aria-describedby': reason === undefined ? undefined : helpId,
          inputMode: numeric ? 'numeric' : undefined,
          autoComplete: 'off',
        }}
        onChange={(event) => {
          const typed = day ? maskTypedDay(event.target.value) : event.target.value;
          void apiRef.current.setEditCellValue({ id, field, value: typed });
        }}
        sx={{
          ...fieldControl('sm'),
          width: '100%',
          padding: '0 var(--space-2)',
          fontVariantNumeric: numeric || day ? 'tabular-nums' : undefined,
          ...(error === undefined
            ? {}
            : {
                borderColor: 'var(--status-danger-text)',
                '&.Mui-focused': {
                  borderColor: 'var(--status-danger-text)',
                  boxShadow: 'inset 0 0 0 1px var(--status-danger-text)',
                },
              }),
        }}
      />
      {reason !== undefined && (
        <Box component="span" id={helpId} sx={hiddenSx}>
          {reason}
        </Box>
      )}
    </Box>
  );
}

/**
 * "Depende de" being edited: the items it may depend on (with dates of their own, not the ones after it, not the task
 * it is inside of) in the select of several options, over the cell, as the Data Grid suggests for long values; the
 * cell keeps the ids chosen until it is saved by Enter, Tab or a click out of it (ADR-0137, ADR-0143).
 */
export function DependsOnEditCell({
  params,
  schedule,
  label,
}: {
  params: GridRenderEditCellParams<ListRow>;
  schedule: Schedule;
  label: string;
}) {
  const { t } = useTranslation();
  const apiRef = useGridApiContext();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const paper = useRef<HTMLDivElement>(null);
  const { id, field, row } = params;
  const chosen = Array.isArray(params.value) ? (params.value as string[]) : [];
  const options =
    row.kind === 'item'
      ? predecessorChoices(schedule, row.item.id, row.item.parentId ?? undefined)
      : [];

  // The select takes the keyboard as the cell starts editing.
  useEffect(() => {
    if (anchor !== null) {
      paper.current?.querySelector('input')?.focus();
    }
  }, [anchor]);

  return (
    <Box ref={setAnchor} sx={{ width: '100%', height: '100%' }}>
      {anchor !== null && (
        <Popper
          open
          anchorEl={anchor}
          placement="bottom-start"
          sx={{ zIndex: (theme) => theme.zIndex.modal }}
        >
          <Paper
            ref={paper}
            elevation={0}
            // The keys of the open list stay with the select; Tab, and Enter or Esc with the list closed, go to the
            // table, which saves or gives up the cell (ADR-0143).
            onKeyDown={(event) => {
              const open = (event.target as HTMLElement).getAttribute('aria-expanded') === 'true';
              if (
                event.key === 'Tab' ||
                ((event.key === 'Enter' || event.key === 'Escape') && !open)
              ) {
                return;
              }
              event.stopPropagation();
            }}
            sx={{
              width: Math.max(anchor.offsetWidth, 320),
              padding: 'var(--space-3)',
              backgroundColor: 'var(--bg-surface)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-lg)',
              boxShadow: 'var(--shadow-menu)',
            }}
          >
            <MultiSelect
              label={label}
              value={chosen}
              options={options}
              onChange={(ids) => {
                void apiRef.current.setEditCellValue({ id, field, value: ids });
              }}
              placeholder={t('projects.schedule.form.dependsOnPlaceholder')}
              noOptionsText={t('projects.schedule.form.noItems')}
            />
          </Paper>
        </Popper>
      )}
    </Box>
  );
}
