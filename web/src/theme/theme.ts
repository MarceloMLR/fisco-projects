import type {} from '@mui/material/themeCssVarsAugmentation';
import { createTheme, duration, type Shadows } from '@mui/material/styles';
import { color, fontFamilies, type ColorScheme, textStyle } from './tokens';

declare module '@mui/material/styles' {
  interface TypographyVariants {
    label: React.CSSProperties;
    badge: React.CSSProperties;
    code: React.CSSProperties;
  }
  interface TypographyVariantsOptions {
    label?: React.CSSProperties;
    badge?: React.CSSProperties;
    code?: React.CSSProperties;
  }
}

declare module '@mui/material/Typography' {
  interface TypographyPropsVariantOverrides {
    label: true;
    badge: true;
    code: true;
  }
}

/** The attribute the design system and MUI both switch on: data-theme="light" or data-theme="dark". */
export const colorSchemeAttribute = 'data-theme';

function palette(scheme: ColorScheme) {
  const token = (name: string) => color(name, scheme);
  return {
    primary: {
      main: token('action'),
      dark: token('action-hover'),
      contrastText: token('on-action'),
    },
    secondary: { main: token('text-secondary'), contrastText: token('text-inverse') },
    error: { main: token('danger'), dark: token('danger-hover'), contrastText: token('on-danger') },
    success: { main: token('success'), contrastText: token('on-action') },
    warning: { main: token('warning'), contrastText: token('on-action') },
    info: { main: token('status-info-text'), contrastText: token('text-inverse') },
    background: { default: token('bg-page'), paper: token('bg-surface') },
    text: {
      primary: token('text'),
      secondary: token('text-secondary'),
      disabled: token('text-disabled'),
    },
    divider: token('border'),
    action: { hover: token('bg-hover'), selected: token('bg-selected') },
  };
}

// Borders separate, shadows do not (ADR-0065): only menus (elevation 8) and dialogs (24) cast one.
const shadows = Array.from({ length: 25 }, (_, elevation) =>
  elevation === 8 ? 'var(--shadow-menu)' : elevation === 24 ? 'var(--shadow-dialog)' : 'none',
) as Shadows;

const text = (name: string) => {
  const style = textStyle(name);
  return { ...style, letterSpacing: 0, textTransform: 'none' as const };
};

export const theme = createTheme({
  cssVariables: { colorSchemeSelector: colorSchemeAttribute },
  colorSchemes: { light: { palette: palette('light') }, dark: { palette: palette('dark') } },
  shape: { borderRadius: 6 },
  shadows,
  typography: {
    fontFamily: fontFamilies.sans,
    fontSize: 14,
    fontWeightLight: 400,
    fontWeightRegular: 400,
    fontWeightMedium: 500,
    fontWeightBold: 500,
    h1: text('page-title'),
    h2: text('section-title'),
    h3: text('card-title'),
    h4: text('card-title'),
    h5: text('card-title'),
    h6: text('card-title'),
    subtitle1: text('body-strong'),
    subtitle2: text('label'),
    body1: text('body'),
    body2: text('body-long'),
    button: text('body-strong'),
    caption: text('caption'),
    overline: text('caption'),
    label: text('label'),
    badge: text('badge'),
    code: text('code'),
  },
  components: {
    // Short, discreet motion only (design system): no ripple; the keyboard focus ring comes from the global styles.
    MuiButtonBase: { defaultProps: { disableRipple: true } },
    // Button of the design system (fp-btn): heights of the control sizes, medium weight, no uppercase.
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: {
          gap: 'var(--space-2)',
          minWidth: 0,
          boxSizing: 'border-box',
          height: 'var(--control-md)',
          padding: '0 var(--space-4)',
          borderRadius: 'var(--radius-md)',
          fontSize: 14,
          lineHeight: '20px',
          fontWeight: 500,
          whiteSpace: 'nowrap',
          transition:
            'background-color 150ms ease-out, border-color 150ms ease-out, color 150ms ease-out',
          '&.Mui-disabled': {
            backgroundColor: 'var(--bg-subtle)',
            borderColor: 'transparent',
            color: 'var(--text-disabled)',
          },
          variants: [
            {
              props: { size: 'small' },
              style: {
                height: 'var(--control-sm)',
                padding: '0 var(--space-3)',
                fontSize: 13,
                lineHeight: '18px',
              },
            },
            {
              props: { size: 'large' },
              style: { height: 'var(--control-lg)', padding: '0 var(--space-5)' },
            },
            {
              props: { variant: 'contained', color: 'primary' },
              style: {
                backgroundColor: 'var(--action)',
                color: 'var(--on-action)',
                '&:hover': { backgroundColor: 'var(--action-hover)' },
                '&:active': { backgroundColor: 'var(--action-pressed)' },
              },
            },
          ],
        },
      },
    },
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: { root: { backgroundImage: 'none' } },
    },
    MuiCard: {
      defaultProps: { variant: 'outlined' },
      styleOverrides: { root: { borderRadius: 'var(--radius-lg)' } },
    },
    MuiPopover: {
      styleOverrides: {
        paper: {
          backgroundColor: 'var(--bg-elevated)',
          border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-menu)',
        },
      },
    },
    // A list of options under a search field looks like the design system menu (fp-menu).
    MuiAutocomplete: {
      styleOverrides: {
        paper: {
          backgroundColor: 'var(--bg-elevated)',
          border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-menu)',
        },
        listbox: { padding: 'var(--space-1) 0' },
        option: {
          minHeight: 36,
          '@media (min-width: 600px)': { minHeight: 36 },
          padding: '0 var(--space-3)',
          color: 'var(--text)',
          fontSize: 14,
          lineHeight: '20px',
          '&.Mui-focused, &.Mui-focusVisible': { backgroundColor: 'var(--bg-hover)' },
          '&[aria-selected="true"]': { backgroundColor: 'transparent', fontWeight: 500 },
          '&[aria-selected="true"].Mui-focused': { backgroundColor: 'var(--bg-hover)' },
        },
        noOptions: {
          padding: 'var(--space-2) var(--space-3)',
          color: 'var(--text-secondary)',
          fontSize: 14,
          lineHeight: '20px',
        },
      },
    },
    // Menu of the design system (fp-menu): 36px items, section labels and dividers.
    MuiMenu: {
      // Choosing closes the menu at once, so the screen behind it, already changed, shows without waiting.
      defaultProps: { transitionDuration: { enter: duration.enteringScreen, exit: 0 } },
      styleOverrides: { list: { padding: 'var(--space-1) 0' } },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          gap: 'var(--space-3)',
          // MUI drops the minimum height from 600px on; the design system keeps 36px items.
          minHeight: 36,
          '@media (min-width: 600px)': { minHeight: 36 },
          padding: '0 var(--space-3)',
          color: 'var(--text)',
          fontSize: 14,
          lineHeight: '20px',
          '& > .MuiSvgIcon-root': { fontSize: 'var(--icon-md)', color: 'var(--text-secondary)' },
          '&:hover, &.Mui-focusVisible': { backgroundColor: 'var(--bg-hover)' },
          // The current option is in bold, with a check, as in fp-menu__item[aria-selected].
          '&.Mui-selected': { backgroundColor: 'transparent', fontWeight: 500 },
          '&.Mui-selected:hover, &.Mui-selected.Mui-focusVisible': {
            backgroundColor: 'var(--bg-hover)',
          },
        },
      },
    },
    MuiListSubheader: {
      styleOverrides: {
        root: {
          padding: 'var(--space-2) var(--space-3) var(--space-1)',
          backgroundColor: 'transparent',
          color: 'var(--text-secondary)',
          fontSize: 12,
          lineHeight: '16px',
          fontWeight: 500,
        },
      },
    },
    MuiDivider: {
      styleOverrides: { root: { borderColor: 'var(--border)' } },
    },
    // Snackbar of the design system (fp-snackbar): inverse colors, short message, optional action.
    MuiSnackbarContent: {
      styleOverrides: {
        root: {
          gap: 'var(--space-3)',
          boxSizing: 'border-box',
          minHeight: 44,
          maxWidth: 480,
          padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
          borderRadius: 'var(--radius-lg)',
          backgroundColor: 'var(--bg-inverse)',
          color: 'var(--text-inverse)',
          boxShadow: 'var(--shadow-menu)',
          fontSize: 14,
          lineHeight: '20px',
        },
        message: { padding: 0 },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          backgroundColor: 'var(--bg-elevated)',
          borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-dialog)',
        },
      },
    },
  },
});
