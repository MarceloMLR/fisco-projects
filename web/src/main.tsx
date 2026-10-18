import '@fontsource/ibm-plex-mono/latin-400.css';
import '@fontsource/ibm-plex-mono/latin-500.css';
import '@fontsource/ibm-plex-sans/latin-400.css';
import '@fontsource/ibm-plex-sans/latin-500.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './i18n';
import { AppThemeProvider } from './theme/AppThemeProvider';
import { colorSchemeAttribute } from './theme/theme';

// Start in the system theme before the first paint, so the screen does not draw light and then fade to dark.
// The theme provider keeps the attribute from here on (ADR-0014).
document.documentElement.setAttribute(
  colorSchemeAttribute,
  window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light',
);

const container = document.getElementById('root');
if (container === null) {
  throw new Error('Element #root not found in index.html.');
}

createRoot(container).render(
  <StrictMode>
    <AppThemeProvider>
      <App />
    </AppThemeProvider>
  </StrictMode>,
);
