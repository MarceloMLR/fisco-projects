import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

/**
 * A field of a form whose control draws its own border, such as a select (fp-field): the label above, with
 * "(opcional)" in the secondary color when given, the control, and the helper or the error below.
 */
export function FormField({
  label,
  optional,
  error,
  helper,
  helpId,
  width,
  children,
}: {
  label: string;
  /** The text that marks the field as optional, as "(opcional)". */
  optional?: string;
  error?: string;
  helper?: string;
  helpId?: string;
  width?: number;
  children: ReactNode;
}) {
  const help = error ?? helper;
  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0, width }}
    >
      <Box
        component="span"
        aria-hidden
        sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
      >
        {label}
        {optional !== undefined && (
          <Box
            component="span"
            sx={{ marginLeft: 'var(--space-1)', fontWeight: 400, color: 'var(--text-secondary)' }}
          >
            {optional}
          </Box>
        )}
      </Box>
      {children}
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
