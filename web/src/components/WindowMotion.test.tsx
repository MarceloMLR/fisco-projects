import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { simulateDevice } from '../test/media';
import {
  type SimulatedAnimations,
  simulateAnimations,
  stopSimulatingAnimations,
} from '../test/motion';
import { backwards, dialogFrames, drawerFrames, motion } from '../theme/motion';
import { DetailsDrawer } from './DetailsDrawer';
import { Dialog } from './Dialog';

/** A dialog whose text comes from the state that closes it, as the confirmation of a removal. */
function Removing() {
  const [item, setItem] = useState<string>();
  return (
    <>
      <button
        type="button"
        onClick={() => {
          setItem('Projeto A');
        }}
      >
        Abrir
      </button>
      <Dialog
        open={item !== undefined}
        title="Excluir"
        onClose={() => {
          setItem(undefined);
        }}
      >
        {item}
      </Dialog>
    </>
  );
}

/** A dialog shown only while a state lasts, as a form of a screen. */
function Shown() {
  const [open, setOpen] = useState(true);
  return (
    open && (
      <Dialog
        open
        title="Novo cliente"
        onClose={() => {
          setOpen(false);
        }}
      >
        Formulário
      </Dialog>
    )
  );
}

/** The details of an item chosen in a list. */
function Details() {
  const [chosen, setChosen] = useState<string>();
  return (
    <>
      <button
        type="button"
        onClick={() => {
          setChosen('Termo de aceite');
        }}
      >
        Escolher
      </button>
      <DetailsDrawer
        open={chosen !== undefined}
        label="Detalhes"
        onClose={() => {
          setChosen(undefined);
        }}
      >
        {chosen !== undefined && <p>{chosen}</p>}
      </DetailsDrawer>
    </>
  );
}

let animations: SimulatedAnimations;

function playedOn(selector: string) {
  return animations.played.filter(({ element }) => element.matches(selector));
}

// Spec: estrutura-das-telas, requirement "Movimento dos diálogos" (ADR-0133).
describe('Movimento dos diálogos', () => {
  beforeEach(() => {
    animations = simulateAnimations({ held: true });
  });
  afterEach(() => {
    animations.endAll();
    stopSimulatingAnimations();
  });

  it('Diálogo que cresce ao abrir', () => {
    render(<Removing />);

    fireEvent.click(screen.getByRole('button', { name: 'Abrir' }));

    const [opening] = playedOn('.MuiDialog-container');
    expect(opening?.frames).toEqual(dialogFrames);
    expect(opening?.options).toEqual({
      duration: motion.dialog.enter,
      easing: motion.enter,
      fill: 'none',
    });
  });

  it('Diálogo até o fim da saída', async () => {
    render(<Removing />);
    fireEvent.click(screen.getByRole('button', { name: 'Abrir' }));
    act(() => {
      animations.endAll();
    });

    fireEvent.click(screen.getByRole('button', { name: ptBR.dialog.close }));

    // Going away, with the text the screen already cleared.
    const closing = playedOn('.MuiDialog-container').at(-1);
    expect(closing?.frames).toEqual(backwards(dialogFrames));
    expect(closing?.options).toEqual({
      duration: motion.dialog.exit,
      easing: motion.exit,
      fill: 'forwards',
    });
    expect(screen.getByRole('dialog', { name: 'Excluir' })).toHaveTextContent('Projeto A');

    act(() => {
      animations.endAll();
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('Diálogo tirado da tela', async () => {
    render(<Shown />);

    fireEvent.click(screen.getByRole('button', { name: ptBR.dialog.close }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    // A copy goes away in its place, which takes no click and no screen reader.
    const copy = await waitFor(() => {
      const leaving = document.querySelector<HTMLElement>('[data-leaving="dialog"]');
      if (leaving === null) {
        throw new Error('No copy going away.');
      }
      return leaving;
    });
    expect(copy).toHaveAttribute('aria-hidden', 'true');
    expect(copy.inert).toBe(true);
    expect(copy).toHaveTextContent('Formulário');
    const surface = animations.played.find(({ element }) =>
      element.matches('[data-leaving="dialog"] .MuiDialog-container'),
    );
    expect(surface?.frames).toEqual(backwards(dialogFrames));

    act(() => {
      animations.endAll();
    });
    await waitFor(() => {
      expect(copy).not.toBeInTheDocument();
    });
  });
});

// Spec: estrutura-das-telas, requirement "Movimento da gaveta" (ADR-0133).
describe('Movimento da gaveta', () => {
  beforeEach(() => {
    animations = simulateAnimations({ held: true });
  });
  afterEach(() => {
    animations.endAll();
    stopSimulatingAnimations();
  });

  it('Gaveta desliza nos dois sentidos', async () => {
    render(<Details />);

    fireEvent.click(screen.getByRole('button', { name: 'Escolher' }));
    const opening = playedOn('.MuiDrawer-paper').at(-1);
    expect(opening?.frames).toEqual(drawerFrames);
    expect(opening?.options).toMatchObject({ duration: motion.drawer.enter, easing: motion.enter });
    act(() => {
      animations.endAll();
    });

    fireEvent.click(screen.getByRole('button', { name: ptBR.details.close }));
    const closing = playedOn('.MuiDrawer-paper').at(-1);
    expect(closing?.frames).toEqual(backwards(drawerFrames));
    expect(closing?.options).toMatchObject({ duration: motion.drawer.exit, easing: motion.exit });
    // The details stay until the drawer has gone.
    expect(screen.getByRole('dialog', { name: 'Detalhes' })).toHaveTextContent('Termo de aceite');

    act(() => {
      animations.endAll();
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});

// Spec: estrutura-das-telas, requirement "Movimento reduzido" (ADR-0133).
describe('Movimento reduzido', () => {
  beforeEach(() => {
    animations = simulateAnimations();
    simulateDevice({ reducedMotion: true });
  });
  afterEach(() => {
    stopSimulatingAnimations();
  });

  it('Janelas com movimento reduzido', async () => {
    render(
      <>
        <Removing />
        <Details />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Abrir' }));
    fireEvent.click(screen.getByRole('button', { name: ptBR.dialog.close }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: 'Escolher' }));

    // Only a fade, in 150 ms, with no move.
    const windows = playedOn('.MuiDialog-container, .MuiDrawer-paper');
    expect(windows.length).toBeGreaterThanOrEqual(3);
    for (const { frames, options } of windows) {
      expect(frames.every((frame) => frame.transform === undefined)).toBe(true);
      expect(options.duration).toBe(motion.reduced);
    }
  });
});
