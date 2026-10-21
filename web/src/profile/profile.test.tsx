import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { SessionUser, ThemePreference } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { simulateDevice } from '../test/media';
import { renderApp } from '../test/renderApp';
import { marina, profileOf, server } from '../test/server';

const text = ptBR.profile;

/** The theme in the simulated profile, changed by the simulated API as the real one would. */
let theme: ThemePreference;
const chosen: unknown[] = [];

function person(): SessionUser {
  return { ...marina, theme };
}

beforeEach(() => {
  // 12:00:30 of October 19th in the time zone of the tests; only the date is simulated, timers stay real.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  theme = 'Automatic';
  chosen.length = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(person())),
    http.get('/api/profile', () => HttpResponse.json(profileOf(person()))),
    http.put('/api/profile/theme', async ({ request }) => {
      const body = (await request.json()) as { theme: ThemePreference };
      chosen.push(body);
      theme = body.theme;
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

function screenTheme() {
  return document.documentElement.getAttribute('data-theme');
}

async function openAccountMenu() {
  fireEvent.click(
    screen.getByRole('button', { name: ptBR.shell.account.replace('{{name}}', marina.name) }),
  );
  return screen.findByRole('menu', { name: ptBR.shell.accountMenu });
}

function themeInMenu(menu: HTMLElement, name: string) {
  return within(within(menu).getByRole('radiogroup', { name: text.theme })).getByRole('radio', {
    name,
  });
}

// Spec: perfil, requirement "Tema no perfil".
describe('Tema no perfil', () => {
  it('Tema segue o sistema', async () => {
    for (const systemTheme of ['light', 'dark'] as const) {
      simulateDevice({ systemTheme });

      const { unmount } = await renderApp('/');

      await waitFor(() => {
        expect(screenTheme()).toBe(systemTheme);
      });
      unmount();
    }
  });

  it('Escolher o tema', async () => {
    simulateDevice({ systemTheme: 'light' });
    await renderApp('/');
    await waitFor(() => {
      expect(screenTheme()).toBe('light');
    });

    fireEvent.click(themeInMenu(await openAccountMenu(), text.themes.Dark));

    // At once, before the API answers, and kept in the profile.
    expect(screenTheme()).toBe('dark');
    await waitFor(() => {
      expect(chosen).toEqual([{ theme: 'Dark' }]);
    });
    expect(themeInMenu(screen.getByRole('menu'), text.themes.Dark)).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });

  it('Tema em outro navegador', async () => {
    theme = 'Dark';
    simulateDevice({ systemTheme: 'light' });

    await renderApp('/');

    await waitFor(() => {
      expect(screenTheme()).toBe('dark');
    });
  });

  it('Página de entrada segue o sistema', async () => {
    theme = 'Dark';
    simulateDevice({ systemTheme: 'light' });
    server.use(
      http.post('/api/session/sign-out', () => {
        server.use(http.get('/api/session', () => new HttpResponse(null, { status: 401 })));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await renderApp('/');
    await waitFor(() => {
      expect(screenTheme()).toBe('dark');
    });

    fireEvent.click(
      within(await openAccountMenu()).getByRole('menuitem', { name: ptBR.shell.signOut }),
    );

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.signIn.title }),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(screenTheme()).toBe('light');
    });
  });

  it('Troca de tema recusada', async () => {
    simulateDevice({ systemTheme: 'light' });
    server.use(http.put('/api/profile/theme', () => new HttpResponse(null, { status: 500 })));
    await renderApp('/');

    fireEvent.click(themeInMenu(await openAccountMenu(), text.themes.Dark));

    // The theme goes back to the one the profile has, and the person is told.
    expect(await screen.findByText(text.themeFailed)).toBeInTheDocument();
    expect(screenTheme()).toBe('light');
    expect(themeInMenu(screen.getByRole('menu'), text.themes.Automatic)).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });
});

// Spec: perfil, requirement "Tela Meu perfil".
describe('Tela Meu perfil', () => {
  it('Dados da conta', async () => {
    await renderApp('/profile');

    expect(screen.getByRole('heading', { level: 1, name: marina.name })).toBeInTheDocument();
    expect(
      screen.getByText(`${marina.email} · ${ptBR.users.roles.User}`, { exact: true }),
    ).toBeInTheDocument();
    const account = screen.getByRole('complementary', { name: text.account });
    await waitFor(() => {
      expect(within(account).getByText(text.signInMethods.local)).toBeInTheDocument();
    });
    const pairs = within(account)
      .getAllByRole('term')
      .map((term) => [term.textContent, term.nextElementSibling?.textContent]);
    expect(pairs).toEqual([
      [text.role, ptBR.users.roles.User],
      [text.signIn, text.signInMethods.local],
      [text.lastAccess, ptBR.dates.today.replace('{{time}}', '09:12')],
    ]);
    expect(within(account).getByRole('button', { name: ptBR.shell.signOut })).toBeInTheDocument();
  });

  // Spec: usuarios-e-papeis, requirement "Papel no sistema", scenario "Papel Líder técnico" (ADR-0097).
  it('Papel Líder técnico em Meu perfil', async () => {
    server.use(http.get('/api/session', () => HttpResponse.json({ ...marina, role: 'TechLead' })));
    await renderApp('/profile');

    const account = screen.getByRole('complementary', { name: text.account });
    expect(within(account).getByText(ptBR.users.roles.TechLead)).toBeInTheDocument();
  });

  it('Tema na tela Meu perfil', async () => {
    simulateDevice({ systemTheme: 'light' });
    await renderApp('/profile');
    const options = screen.getByRole('radiogroup', { name: text.theme });
    expect(
      within(options)
        .getAllByRole('radio')
        .map((option) => [
          option.closest('label')?.textContent,
          (option as HTMLInputElement).checked,
        ]),
    ).toEqual([
      [text.themes.Light, false],
      [text.themes.Dark, false],
      [text.themes.Automatic, true],
    ]);

    fireEvent.click(within(options).getByRole('radio', { name: text.themes.Dark }));

    expect(within(options).getByRole('radio', { name: text.themes.Dark })).toBeChecked();
    expect(screenTheme()).toBe('dark');
    await waitFor(() => {
      expect(chosen).toEqual([{ theme: 'Dark' }]);
    });
    // The account menu shows the same choice.
    expect(themeInMenu(await openAccountMenu(), text.themes.Dark)).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });

  it('Meu perfil pelo menu da conta', async () => {
    const { router } = await renderApp('/');

    fireEvent.click(
      within(await openAccountMenu()).getByRole('menuitem', { name: ptBR.pages.profile.title }),
    );

    expect(await screen.findByRole('heading', { level: 1, name: marina.name })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/profile');
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(within(trail).getByText(ptBR.pages.profile.title)).toHaveAttribute(
      'aria-current',
      'page',
    );
  });
});
