import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import type SvgIcon from '@mui/material/SvgIcon';
import { visuallyHidden } from '@mui/utils';
import type { ReactNode } from 'react';

/**
 * The frame of a text field of the design system (fp-field): the label above, the control with its border and focus
 * ring, and the helper or the error below. The control inside is the field's own (an input or a textarea).
 */
export function FieldFrame({
  inputId,
  helpId,
  label,
  hideLabel = false,
  optional,
  error,
  helper,
  width,
  size = 'md',
  multiline = false,
  readOnly = false,
  icon: Icon,
  children,
}: {
  inputId: string;
  helpId: string;
  label: string;
  hideLabel?: boolean;
  /** The text that marks the field as optional, as "(opcional)", out of its accessible name. */
  optional?: string;
  error?: string;
  helper?: string;
  width?: number;
  size?: 'sm' | 'md' | 'lg';
  /** A control of several lines (fp-field__control--multiline): its height follows the text. */
  multiline?: boolean;
  /** A value shown but not edited, such as the mailbox of the installation (fp-field--readonly). */
  readOnly?: boolean;
  icon?: typeof SvgIcon;
  children: ReactNode;
}) {
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
        {optional !== undefined && (
          <Box
            component="span"
            aria-hidden
            sx={{ marginLeft: 'var(--space-1)', fontWeight: 400, color: 'var(--text-secondary)' }}
          >
            {optional}
          </Box>
        )}
      </Box>
      <Box
        sx={{
          position: 'relative',
          display: 'flex',
          alignItems: multiline ? 'flex-start' : 'center',
          gap: 'var(--space-2)',
          boxSizing: 'border-box',
          height: multiline ? 'auto' : `var(--control-${size})`,
          padding: multiline ? 'var(--space-2) var(--space-3)' : '0 var(--space-3)',
          backgroundColor: readOnly ? 'var(--bg-subtle)' : 'var(--bg-surface)',
          border: '1px solid',
          borderColor:
            error !== undefined
              ? 'var(--status-danger-text)'
              : readOnly
                ? 'var(--border)'
                : 'var(--border-control)',
          borderRadius: 'var(--radius-md)',
          color: 'var(--text)',
          transition: 'border-color 150ms ease-out',
          '&:hover':
            error === undefined && !readOnly ? { borderColor: 'var(--text-secondary)' } : {},
          '&:focus-within': { borderColor: accent, boxShadow: `inset 0 0 0 1px ${accent}` },
        }}
      >
        {Icon !== undefined && (
          <Icon aria-hidden sx={{ fontSize: 'var(--icon-md)', color: 'var(--text-secondary)' }} />
        )}
        {children}
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
