import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { connectToTeams, insideAFrame } from './teamsHost';

// The library of Teams, simulated: the page never talks to a real Teams in the tests (ADR-0056, ADR-0152).
const library = vi.hoisted(() => ({
  initialize: vi.fn<() => Promise<void>>(),
  getContext: vi.fn<() => Promise<{ app: { theme: string } }>>(),
  registerOnThemeChangeHandler: vi.fn<(handler: (theme: string) => void) => void>(),
  getAuthToken: vi.fn<() => Promise<string>>(),
}));
vi.mock('@microsoft/teams-js', () => ({
  app: {
    initialize: library.initialize,
    getContext: library.getContext,
    registerOnThemeChangeHandler: library.registerOnThemeChangeHandler,
  },
  authentication: { getAuthToken: library.getAuthToken },
}));

function inAFrame() {
  // Teams opens the page in a frame of its own: the parent is another window.
  vi.stubGlobal('parent', {});
}

beforeEach(() => {
  library.initialize.mockResolvedValue(undefined);
  library.getContext.mockResolvedValue({ app: { theme: 'dark' } });
  library.getAuthToken.mockResolvedValue('token-do-teams');
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

// Spec: avisos, requirements "Página do FiscoProjects no Teams" and "Token do Teams só na memória da página" (ADR-0152).
describe('Teams em volta da página', () => {
  it('Fora de um quadro, sem a biblioteca do Teams', async () => {
    expect(insideAFrame()).toBe(false);

    expect(await connectToTeams()).toBeNull();
    expect(library.initialize).not.toHaveBeenCalled();
    expect(library.getAuthToken).not.toHaveBeenCalled();
  });

  it('Dentro do Teams', async () => {
    inAFrame();

    const host = await connectToTeams();

    expect(host?.scheme).toBe('dark');
    expect(library.getAuthToken).not.toHaveBeenCalled();
    expect(await host?.token()).toBe('token-do-teams');
    const listener = vi.fn();
    host?.onScheme(listener);
    library.registerOnThemeChangeHandler.mock.calls[0]?.[0]('default');
    expect(listener).toHaveBeenCalledWith('light');
  });

  it('Teams que não responde', async () => {
    inAFrame();
    library.initialize.mockReturnValue(new Promise(() => undefined));

    expect(await connectToTeams(20)).toBeNull();
  });

  it('Teams que recusa', async () => {
    inAFrame();
    library.initialize.mockRejectedValue(new Error('Not in Teams'));

    expect(await connectToTeams()).toBeNull();
  });
});
