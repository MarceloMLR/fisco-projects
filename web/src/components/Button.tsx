import Box from '@mui/material/Box';
import MuiButton, { type ButtonProps as MuiButtonProps } from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import MuiIconButton, { type IconButtonProps as MuiIconButtonProps } from '@mui/material/IconButton';
import type { SxProps, Theme } from '@mui/material/styles';
import { type MouseEvent, useContext } from 'react';
import { FormPending, usePendingAction } from './pending';

type Click = (event: MouseEvent<HTMLButtonElement>) => unknown;

/** The props of a button of the system: the ones of MUI, with a click that may give the promise of what it sends. */
export type ButtonProps = Omit<MuiButtonProps, 'onClick' | 'loadingPosition' | 'loadingIndicator'> & {
  onClick?: Click;
};

export type IconButtonProps = Omit<MuiIconButtonProps, 'onClick' | 'loadingIndicator'> & {
  onClick?: Click;
};

/** The loading icon in the place of the icon: 16px in the box of the icon, in the color of the text of the button. */
function Spinner() {
  return (
    <Box
      component="span"
      aria-hidden
      data-testid="button-spinner"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: 'var(--icon-md)',
        height: 'var(--icon-md)',
      }}
    >
      <CircularProgress size="var(--icon-sm)" thickness={5} color="inherit" />
    </Box>
  );
}

// A busy button takes no new click nor hover, keeps the colors of its variant and tells it is busy (ADR-0131).
const busySx: SxProps<Theme> = { pointerEvents: 'none' };

function withBusy(sx: SxProps<Theme> | undefined, busy: boolean): SxProps<Theme> {
  const own = Array.isArray(sx) ? (sx as readonly SxProps<Theme>[]) : [sx];
  return [busy && busySx, ...own] as SxProps<Theme>;
}

function ignore(event: MouseEvent<HTMLButtonElement>) {
  event.preventDefault();
}

/**
 * Button of the design system (fp-btn), for every button of the screens (ADR-0131): while the promise its click gives
 * runs, or while the form around sends (a submit button), or with loading, the loading icon takes the place of the
 * icon at the start (or comes before the text), the label stays, and the button takes no new click.
 */
export function Button({ onClick, loading, startIcon, type, sx, ...props }: ButtonProps) {
  const sending = useContext(FormPending);
  const [pending, run] = usePendingAction(onClick);
  const busy = Boolean(loading) || pending || (type === 'submit' && sending);
  return (
    <MuiButton
      {...props}
      type={type}
      startIcon={busy ? <Spinner /> : startIcon}
      aria-busy={busy || undefined}
      aria-disabled={busy || props['aria-disabled'] || undefined}
      onClick={busy ? ignore : run}
      sx={withBusy(sx, busy)}
    />
  );
}

/** Icon button of the design system (fp-btn--icon): busy as the Button, with the loading icon in the place of the icon. */
export function IconButton({ onClick, loading, children, sx, ...props }: IconButtonProps) {
  const [pending, run] = usePendingAction(onClick);
  const busy = Boolean(loading) || pending;
  return (
    <MuiIconButton
      {...props}
      aria-busy={busy || undefined}
      aria-disabled={busy || props['aria-disabled'] || undefined}
      onClick={busy ? ignore : run}
      sx={withBusy(sx, busy)}
    >
      {busy ? <Spinner /> : children}
    </MuiIconButton>
  );
}
