import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import type SvgIcon from '@mui/material/SvgIcon';
import { visuallyHidden } from '@mui/utils';
import { type ComponentProps, useId } from 'react';

export interface TextFieldProps extends Omit<ComponentProps<'input'>, 'size'> {
  label: string;
  /** Message of a field with an error; it replaces the helper text. */
  error?: string;
  helper?: string;
  size?: 'sm' | 'md' | 'lg';
  /** Icon at the start of the control, such as the search icon. */
  icon?: typeof SvgIcon;
  /** Keeps the label only for screen readers, as in the search above a table. */
  hideLabel?: boolean;
  width?: number;
}

/** Text field of the design system (TextField, fp-field): label above, control, helper or error below. */
export function TextField({
  label,
  error,
  helper,
  size = 'md',
  icon: Icon,
  hideLabel = false,
  width,
  id,
  ref,
  ...input
}: TextFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const helpId = `${inputId}-help`;
  const help = error ?? helper;
  const accent = error === undefined ? 'var(--focus-ring)' : 'var(--status-danger-text)';

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0, width }}
    >
      <Box
        component="label"
        htmlFor={inputId}
        sx={
          hideLabel
            ? visuallyHidden
            : { fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }
        }
      >
        {label}
      </Box>
      <Box
        sx={{
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-2)',
          boxSizing: 'border-box',
          height: `var(--control-${size})`,
          padding: '0 var(--space-3)',
          backgroundColor: 'var(--bg-surface)',
          border: '1px solid',
          borderColor: error === undefined ? 'var(--border-control)' : 'var(--status-danger-text)',
          borderRadius: 'var(--radius-md)',
          color: 'var(--text)',
          transition: 'border-color 150ms ease-out',
          '&:hover': error === undefined ? { borderColor: 'var(--text-secondary)' } : {},
          '&:focus-within': { borderColor: accent, boxShadow: `inset 0 0 0 1px ${accent}` },
        }}
      >
        {Icon !== undefined && (
          <Icon aria-hidden sx={{ fontSize: 'var(--icon-md)', color: 'var(--text-secondary)' }} />
        )}
        <Box
          component="input"
          id={inputId}
          ref={ref}
          aria-invalid={error === undefined ? undefined : true}
          aria-describedby={help === undefined ? undefined : helpId}
          {...input}
          sx={{
            flex: 1,
            minWidth: 0,
            padding: 0,
            border: 0,
            outline: 0,
            backgroundColor: 'transparent',
            color: 'var(--text)',
            fontFamily: 'var(--font-sans)',
            fontSize: 14,
            lineHeight: '20px',
            '&:focus-visible': { outline: 'none' },
            '&::placeholder': { color: 'var(--text-placeholder)', opacity: 1 },
          }}
        />
      </Box>
      {help !== undefined && (
        <Box
          id={helpId}
          sx={{
            display: 'flex',
            alignItems: 'flex-start',
            gap: 'var(--space-1)',
            fontSize: 12,
            lineHeight: '16px',
            color: error === undefined ? 'var(--text-secondary)' : 'var(--status-danger-text)',
          }}
        >
          {error !== undefined && <ErrorOutlineOutlined aria-hidden sx={{ fontSize: 16 }} />}
          {help}
        </Box>
      )}
    </Box>
  );
}
