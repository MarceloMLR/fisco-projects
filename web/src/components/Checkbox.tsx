import CheckOutlined from '@mui/icons-material/CheckOutlined';
import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import type { ChangeEvent } from 'react';

/**
 * Checkbox of the design system (Checkbox, fp-check): a box of 18 px with the control border, filled with the color
 * of the checked controls and a check mark. The native input stays under the box, so the keyboard, the label and the
 * screen readers work as usual. With hideLabel, the label is only the accessible name, as in a column of a table.
 */
export function Checkbox({
  label,
  checked,
  onChange,
  disabled = false,
  hideLabel = false,
}: {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
  hideLabel?: boolean;
}) {
  return (
    <Box
      component="label"
      sx={{
        position: 'relative',
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        minWidth: 0,
        fontSize: 14,
        lineHeight: '20px',
        color: disabled ? 'var(--text-disabled)' : 'var(--text)',
        cursor: disabled ? 'not-allowed' : 'pointer',
        '& input:focus-visible + .checkbox-box': {
          outline: '2px solid var(--focus-ring)',
          outlineOffset: 2,
        },
        '& input:checked + .checkbox-box': {
          backgroundColor: disabled ? 'var(--text-disabled)' : 'var(--control-selected)',
          borderColor: disabled ? 'var(--text-disabled)' : 'var(--control-selected)',
        },
        '& input:checked + .checkbox-box > svg': { visibility: 'visible' },
      }}
    >
      <Box
        component="input"
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(event: ChangeEvent<HTMLInputElement>) => {
          onChange(event.target.checked);
        }}
        sx={{
          position: 'absolute',
          width: 18,
          height: 18,
          margin: 0,
          opacity: 0,
          cursor: 'inherit',
        }}
      />
      <Box
        aria-hidden
        className="checkbox-box"
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          flex: 'none',
          boxSizing: 'border-box',
          width: 18,
          height: 18,
          border: '2px solid',
          borderColor: disabled ? 'var(--border)' : 'var(--border-control)',
          borderRadius: 'var(--radius-sm)',
          backgroundColor: disabled ? 'var(--bg-subtle)' : 'var(--bg-surface)',
          color: 'var(--on-control-selected)',
          transition: 'background-color 150ms ease-out, border-color 150ms ease-out',
          '& > svg': { fontSize: 14, visibility: 'hidden' },
        }}
      >
        <CheckOutlined />
      </Box>
      <Box component="span" sx={hideLabel ? visuallyHidden : { minWidth: 0 }}>
        {label}
      </Box>
    </Box>
  );
}

/**
 * The box of the Checkbox alone, for a menu item that is itself the control (role menuitemcheckbox): it only shows
 * whether the option is on.
 */
export function CheckboxMark({ checked }: { checked: boolean }) {
  return (
    <Box
      aria-hidden
      sx={{
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        boxSizing: 'border-box',
        width: 18,
        height: 18,
        border: '2px solid',
        borderColor: checked ? 'var(--control-selected)' : 'var(--border-control)',
        borderRadius: 'var(--radius-sm)',
        backgroundColor: checked ? 'var(--control-selected)' : 'var(--bg-surface)',
        color: 'var(--on-control-selected)',
        '& > svg': { fontSize: 14, visibility: checked ? 'visible' : 'hidden' },
      }}
    >
      <CheckOutlined />
    </Box>
  );
}
