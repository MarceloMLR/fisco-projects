import type {} from '@mui/material/themeCssVarsAugmentation';
import { createTheme, type Shadows } from '@mui/material/styles';
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
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: { root: { borderRadius: 'var(--radius-md)' } },
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
