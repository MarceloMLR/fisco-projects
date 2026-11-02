import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import { type ChangeEvent, useId, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '../components/Button';
import { InstallationLogo } from '../installation/InstallationLogo';
import type { LogoError } from './logos';

/** A logo of the brand (ADR-0100): the tile with the logo, send or replace it and remove it. */
export function LogoField({
  label,
  hint,
  shown,
  error,
  dark,
  testId,
  sendLabel,
  replaceLabel,
  removeLabel,
  onChoose,
  onRemove,
}: {
  label: string;
  hint: string;
  shown: string | null;
  error?: LogoError;
  /** The tile is dark, as the sidebar that shows the logo for the dark theme. */
  dark: boolean;
  testId: string;
  sendLabel: string;
  replaceLabel: string;
  removeLabel: string;
  onChoose: (event: ChangeEvent<HTMLInputElement>) => void;
  onRemove: () => void;
}) {
  const { t } = useTranslation();
  const labelId = useId();
  const helpId = useId();
  const fileInput = useRef<HTMLInputElement>(null);
  return (
    <Box
      role="group"
      aria-labelledby={labelId}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0 }}
    >
      <Box
        component="span"
        id={labelId}
        sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
      >
        {label}
        <Box
          component="span"
          sx={{ marginLeft: 'var(--space-1)', fontWeight: 400, color: 'var(--text-secondary)' }}
        >
          {t('brand.optional')}
        </Box>
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}>
        <Box
          component="span"
          data-testid={`${testId}-tile`}
          data-theme={dark ? 'dark' : undefined}
          sx={{
            display: 'inline-grid',
            placeItems: 'center',
            flex: 'none',
            boxSizing: 'border-box',
            width: 36,
            height: 36,
            padding: '3px',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-md)',
            backgroundColor: dark ? 'var(--sidebar-bg)' : 'var(--bg-surface)',
            color: 'var(--text-secondary)',
            fontSize: 11,
            fontWeight: 500,
          }}
        >
          {shown === null ? (
            t('brand.noLogo')
          ) : (
            <InstallationLogo src={shown} maxHeight={28} maxWidth={28} />
          )}
        </Box>
        <Button
          variant="outlined"
          size="small"
          startIcon={<UploadOutlined />}
          aria-describedby={helpId}
          onClick={() => fileInput.current?.click()}
        >
          {shown === null ? sendLabel : replaceLabel}
        </Button>
        {shown !== null && (
          <Button variant="text" size="small" onClick={onRemove}>
            {removeLabel}
          </Button>
        )}
        <input
          ref={fileInput}
          type="file"
          accept=".png,.svg,image/png,image/svg+xml"
          hidden
          data-testid={`${testId}-file`}
          onChange={onChoose}
        />
      </Box>
      <Box
        component="span"
        id={helpId}
        role={error === undefined ? undefined : 'alert'}
        sx={{
          fontSize: 12,
          lineHeight: '16px',
          color: error === undefined ? 'var(--text-secondary)' : 'var(--status-danger-text)',
        }}
      >
        {error === undefined ? hint : t(`brand.errors.${error}`)}
      </Box>
    </Box>
  );
}
