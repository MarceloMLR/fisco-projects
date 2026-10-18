import { QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { expect } from 'vitest';
import { createQueryClient } from '../api/queryClient';
import { routes } from '../routes';
import { SessionProvider } from '../session/SessionProvider';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { simulateDevice } from './media';

export const desktopWidth = 1440;
export const phoneWidth = 390;

/**
 * Renders the whole application at an address, on a window of the given width, and waits until it knows who is
 * in the session (the simulated API of src/test/server.ts). Each test has a cache of its own, without asking again
 * after a failure.
 */
export async function renderApp(path: string, width = desktopWidth) {
  simulateDevice({ width });
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = createQueryClient({ askAgain: false });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <AppThemeProvider>
        <SessionProvider>
          <RouterProvider router={router} />
        </SessionProvider>
      </AppThemeProvider>
    </QueryClientProvider>,
  );
  await waitFor(() => {
    expect(screen.queryByTestId('session-pending')).not.toBeInTheDocument();
  });
  return { ...view, router, queryClient };
}
