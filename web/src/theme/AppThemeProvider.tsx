import CssBaseline from '@mui/material/CssBaseline';
import GlobalStyles from '@mui/material/GlobalStyles';
import { ThemeProvider } from '@mui/material/styles';
import type { ReactNode } from 'react';
import { theme } from './theme';
import { cssVariables } from './tokens';

// Base styles of the design system (bundle.css): body, keyboard focus and reduced motion.
const globalStyles = `
${cssVariables()}
body {
  margin: 0;
  background: var(--bg-page);
  color: var(--text);
  font-family: var(--font-sans);
  font-size: 14px;
  line-height: 20px;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { transition: none !important; animation: none !important; }
}
`;

/**
 * Light and dark themes from the design system tokens. The mode follows the operating system (ADR-0014)
 * until the person picks one in the account menu; nothing is kept in the browser storage.
 */
export function AppThemeProvider({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider theme={theme} defaultMode="system" storageManager={null}>
      <CssBaseline enableColorScheme />
      <GlobalStyles styles={globalStyles} />
      {children}
    </ThemeProvider>
  );
}
