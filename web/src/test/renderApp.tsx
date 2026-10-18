import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from '../routes';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { simulateDevice } from './media';

export const desktopWidth = 1440;
export const phoneWidth = 390;

/** Renders the whole application at an address, on a window of the given width. */
export function renderApp(path: string, width = desktopWidth) {
  simulateDevice({ width });
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const view = render(
    <AppThemeProvider>
      <RouterProvider router={router} />
    </AppThemeProvider>,
  );
  return { ...view, router };
}
