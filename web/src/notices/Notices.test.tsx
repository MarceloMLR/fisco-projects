import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, Outlet, RouterProvider, useNavigate } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { simulateDevice } from '../test/media';
import {
  type SimulatedAnimations,
  simulateAnimations,
  stopSimulatingAnimations,
} from '../test/motion';
import { motion } from '../theme/motion';
import { NoticeProvider } from './Notices';
import {
  type NoticeOptions,
  noticeMs,
  noticeWithActionMs,
  useNoticesInShell,
  useNotify,
} from './notify';

const saved = 'Configurações salvas.';
const failed = 'Não foi possível salvar. Tente de novo.';

/** A screen that gives a notice on a click. */
function Notifier({ text, options }: { text: string; options?: NoticeOptions }) {
  const notify = useNotify();
  return (
    <button
      type="button"
      onClick={() => {
        notify(text, options);
      }}
    >
      {text}
    </button>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  useNoticesInShell();
  return children;
}

function notice() {
  return screen.queryByTestId('notice');
}

async function give(text: string) {
  await act(async () => {
    fireEvent.click(screen.getByRole('button', { name: text }));
    await Promise.resolve();
  });
}

/** Lets the time pass and what waits for the end of an animation run. */
async function wait(ms: number) {
  await act(async () => {
    vi.advanceTimersByTime(ms);
    await Promise.resolve();
  });
}

// Spec: estrutura-das-telas, requirements "Confirmações no alto da tela" and "Aparência e tempo do aviso" (ADR-0109, ADR-0133).
describe('Confirmações no alto da tela', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('Confirmação no alto, no meio', async () => {
    render(
      <NoticeProvider>
        <Shell>
          <Notifier text={saved} />
        </Shell>
      </NoticeProvider>,
    );

    await give(saved);

    // On the computer, in the strip of the invisible header and in the middle of the content, beside the sidebar.
    const area = screen.getByTestId('notice-area');
    expect(area).toHaveAttribute('aria-live', 'polite');
    expect(area).toHaveAttribute('data-place', 'content');
    expect(getComputedStyle(area).left).toBe('calc(50% + var(--sidebar-width) / 2)');
    const shown = within(area).getByRole('status');
    expect(shown).toHaveTextContent(saved);
    expect(within(shown).getByTestId('CheckCircleOutlinedIcon')).toBeInTheDocument();
    expect(getComputedStyle(shown).backgroundColor).toBe('var(--bg-elevated)');
  });

  it('Confirmação no alto, no meio, no celular', async () => {
    simulateDevice({ width: 390 });
    render(
      <NoticeProvider>
        <Shell>
          <Notifier text={saved} />
        </Shell>
      </NoticeProvider>,
    );

    await give(saved);

    const area = screen.getByTestId('notice-area');
    expect(area).toHaveAttribute('data-place', 'below-top-bar');
    expect(getComputedStyle(area).top).toBe('calc(var(--topbar-height) + var(--space-2))');
    expect(getComputedStyle(area).left).toBe('50%');
  });

  it('Confirmação fora da estrutura das telas', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);

    expect(screen.getByTestId('notice-area')).toHaveAttribute('data-place', 'top');
  });

  it('Aviso de uma falha', async () => {
    render(
      <NoticeProvider>
        <Notifier text={failed} options={{ tone: 'error' }} />
      </NoticeProvider>,
    );

    await give(failed);

    const shown = notice();
    if (shown === null) {
      throw new Error('No notice.');
    }
    expect(shown).toHaveAttribute('data-tone', 'error');
    expect(within(shown).getByTestId('ErrorOutlineOutlinedIcon')).toBeInTheDocument();
    expect(within(shown).queryByTestId('CheckCircleOutlinedIcon')).not.toBeInTheDocument();
  });

  it('Um aviso de cada vez', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
        <Notifier text={failed} options={{ tone: 'error' }} />
      </NoticeProvider>,
    );

    await give(saved);
    await give(failed);

    // The one on screen left, and the new one came in: never the two together.
    expect(screen.getAllByTestId('notice')).toHaveLength(1);
    expect(notice()).toHaveTextContent(failed);
  });

  it('Aviso depois de mudar de página', async () => {
    function Leaving() {
      const notify = useNotify();
      const navigate = useNavigate();
      return (
        <button
          type="button"
          onClick={() => {
            notify(saved);
            void navigate('/next');
          }}
        >
          {ptBR.shell.notices}
        </button>
      );
    }
    const router = createMemoryRouter(
      [
        {
          element: (
            <NoticeProvider>
              <Outlet />
            </NoticeProvider>
          ),
          children: [
            { path: '/', element: <Leaving /> },
            { path: '/next', element: <h1>Página seguinte</h1> },
          ],
        },
      ],
      { initialEntries: ['/'] },
    );
    render(<RouterProvider router={router} />);

    await give(ptBR.shell.notices);

    expect(screen.getByRole('heading', { name: 'Página seguinte' })).toBeInTheDocument();
    expect(notice()).toHaveTextContent(saved);
  });

  it('Tempo do aviso', async () => {
    const action = vi.fn();
    render(
      <NoticeProvider>
        <Notifier text={saved} />
        <Notifier text={failed} options={{ action: { label: 'Ver chamado', onClick: action } }} />
      </NoticeProvider>,
    );

    await give(saved);
    await wait(noticeMs - 1);
    expect(notice()).toHaveTextContent(saved);
    await wait(1);
    expect(notice()).not.toBeInTheDocument();

    // With an action, 8 seconds.
    await give(failed);
    await wait(noticeMs);
    expect(notice()).toHaveTextContent(failed);
    await wait(noticeWithActionMs - noticeMs);
    expect(notice()).not.toBeInTheDocument();
  });

  it('Ação do aviso', async () => {
    const action = vi.fn();
    render(
      <NoticeProvider>
        <Notifier text={saved} options={{ action: { label: 'Ver chamado', onClick: action } }} />
      </NoticeProvider>,
    );

    await give(saved);
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Ver chamado' }));
      await Promise.resolve();
    });

    expect(action).toHaveBeenCalledOnce();
    expect(notice()).not.toBeInTheDocument();
  });

  it('Aviso parado com o ponteiro', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);
    await wait(2000);
    const shown = notice();
    if (shown === null) {
      throw new Error('No notice.');
    }
    fireEvent.mouseEnter(shown);
    await wait(noticeMs * 3);
    expect(notice()).toHaveTextContent(saved);

    // Then it stays the time it had left: 3 of the 5 seconds.
    fireEvent.mouseLeave(shown);
    await wait(noticeMs - 2000 - 1);
    expect(notice()).toBeInTheDocument();
    await wait(1);
    expect(notice()).not.toBeInTheDocument();
  });

  it('Aviso parado com o foco', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);
    const close = screen.getByRole('button', { name: ptBR.shell.closeNotice });
    act(() => {
      close.focus();
    });
    await wait(noticeMs * 2);
    expect(notice()).toHaveTextContent(saved);

    act(() => {
      close.blur();
    });
    await wait(noticeMs);
    expect(notice()).not.toBeInTheDocument();
  });

  it('Fechar o aviso', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: ptBR.shell.closeNotice }));
      await Promise.resolve();
    });

    expect(notice()).not.toBeInTheDocument();
  });
});

// Spec: estrutura-das-telas, requirement "Aparência e tempo do aviso" (ADR-0133).
describe('Movimento do aviso', () => {
  let animations: SimulatedAnimations;

  beforeEach(() => {
    vi.useFakeTimers();
    animations = simulateAnimations();
  });
  afterEach(() => {
    vi.useRealTimers();
    stopSimulatingAnimations();
  });

  const last = () => animations.played.at(-1);

  it('Aviso entra descendo e sai esmaecendo', async () => {
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);
    expect(last()?.frames).toEqual([
      { opacity: 0, transform: 'translateY(-8px)' },
      { opacity: 1, transform: 'translateY(0)' },
    ]);
    expect(last()?.options).toEqual({
      duration: motion.notice.enter,
      easing: motion.enter,
      fill: 'none',
    });

    await wait(noticeMs);
    expect(last()?.frames.at(-1)).toMatchObject({ opacity: 0 });
    expect(last()?.options).toEqual({
      duration: motion.notice.exit,
      easing: motion.exit,
      fill: 'forwards',
    });
    expect(notice()).not.toBeInTheDocument();
  });

  it('Aviso com movimento reduzido', async () => {
    simulateDevice({ reducedMotion: true });
    render(
      <NoticeProvider>
        <Notifier text={saved} />
      </NoticeProvider>,
    );

    await give(saved);
    await wait(noticeMs);

    // Only a fade, in 150 ms, with no move.
    expect(animations.played).toHaveLength(2);
    for (const { frames, options } of animations.played) {
      expect(frames.every((frame) => frame.transform === undefined)).toBe(true);
      expect(options.duration).toBe(motion.reduced);
    }
  });
});
