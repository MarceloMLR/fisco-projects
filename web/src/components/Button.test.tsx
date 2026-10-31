import CheckOutlined from '@mui/icons-material/CheckOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useState } from 'react';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { Button, IconButton } from './Button';
import { Form } from './Form';
import { minimumBusyMs } from './pending';

/** An answer of the server held until the test lets it go, with success or error. */
function heldAnswer() {
  let resolve: () => void = () => undefined;
  let reject: () => void = () => undefined;
  const promise = new Promise<void>((done, fail) => {
    resolve = done;
    reject = () => {
      fail(new Error('falhou'));
    };
  });
  return { promise, resolve, reject };
}

function themed(children: React.ReactNode) {
  return render(<AppThemeProvider>{children}</AppThemeProvider>);
}

const spinnerOf = (button: HTMLElement) => within(button).queryByTestId('button-spinner');

afterEach(() => {
  vi.useRealTimers();
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131, ADR-0132).
describe('Botões em andamento', () => {
  it('Botão em andamento', async () => {
    const answer = heldAnswer();
    themed(
      <Button startIcon={<CheckOutlined data-testid="icon" />} onClick={() => answer.promise}>
        Salvar
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Salvar' });

    fireEvent.click(button);

    expect(spinnerOf(button)).not.toBeNull();
    expect(within(button).queryByTestId('icon')).toBeNull();
    expect(button).toHaveTextContent('Salvar');
    expect(button).toHaveAttribute('aria-busy', 'true');
    expect(button).toHaveAttribute('aria-disabled', 'true');
    expect(button).not.toBeDisabled();
    await act(async () => {
      answer.resolve();
      await answer.promise;
    });
    await waitFor(() => {
      expect(spinnerOf(button)).toBeNull();
    });
    expect(within(button).getByTestId('icon')).toBeInTheDocument();
    expect(button).not.toHaveAttribute('aria-busy');
  });

  it('Botão sem ícone em andamento', async () => {
    const answer = heldAnswer();
    themed(<Button onClick={() => answer.promise}>Enviar</Button>);
    const button = screen.getByRole('button', { name: 'Enviar' });
    expect(spinnerOf(button)).toBeNull();

    fireEvent.click(button);

    const spinner = spinnerOf(button);
    expect(spinner).not.toBeNull();
    // The loading icon comes before the text.
    expect(button.textContent).toBe('Enviar');
    expect(button.firstElementChild?.contains(spinner ?? null)).toBe(true);
    await act(async () => {
      answer.resolve();
      await answer.promise;
    });
  });

  it('Botão de ícone em andamento', async () => {
    const answer = heldAnswer();
    themed(
      <IconButton aria-label="Lembrar" onClick={() => answer.promise}>
        <CloseOutlined data-testid="icon" />
      </IconButton>,
    );
    const button = screen.getByRole('button', { name: 'Lembrar' });

    fireEvent.click(button);

    expect(spinnerOf(button)).not.toBeNull();
    expect(within(button).queryByTestId('icon')).toBeNull();
    expect(button).toHaveAttribute('aria-busy', 'true');
    await act(async () => {
      answer.resolve();
      await answer.promise;
    });
    expect(await within(button).findByTestId('icon')).toBeInTheDocument();
  });

  it('Clique repetido durante o andamento', async () => {
    const answer = heldAnswer();
    let calls = 0;
    themed(
      <Button
        onClick={() => {
          calls += 1;
          return answer.promise;
        }}
      >
        Concluir
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Concluir' });

    fireEvent.click(button);
    fireEvent.click(button);
    fireEvent.click(button);

    expect(calls).toBe(1);
    await act(async () => {
      answer.resolve();
      await answer.promise;
    });
    await waitFor(() => {
      expect(button).not.toHaveAttribute('aria-busy');
    });
    fireEvent.click(button);
    expect(calls).toBe(2);
  });

  it('Formulário em andamento', async () => {
    const answer = heldAnswer();
    let sent = 0;
    themed(
      <Form
        onSubmit={(event) => {
          event.preventDefault();
          sent += 1;
          return answer.promise;
        }}
      >
        <input aria-label="Título" />
        <Button type="submit">Salvar</Button>
        <Button>Cancelar</Button>
      </Form>,
    );
    const save = screen.getByRole('button', { name: 'Salvar' });

    // Enter in a field sends the form, as the submit button.
    fireEvent.submit(screen.getByLabelText('Título'));
    fireEvent.submit(screen.getByLabelText('Título'));
    fireEvent.click(save);

    expect(sent).toBe(1);
    expect(spinnerOf(save)).not.toBeNull();
    expect(spinnerOf(screen.getByRole('button', { name: 'Cancelar' }))).toBeNull();
    await act(async () => {
      answer.resolve();
      await answer.promise;
    });
    await waitFor(() => {
      expect(spinnerOf(save)).toBeNull();
    });
  });

  it('Fim do andamento com erro', async () => {
    const answer = heldAnswer();
    themed(
      <Button startIcon={<CheckOutlined data-testid="icon" />} onClick={() => answer.promise}>
        Lembrar
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Lembrar' });

    fireEvent.click(button);
    await act(async () => {
      answer.reject();
      await answer.promise.catch(() => undefined);
    });

    await waitFor(() => {
      expect(spinnerOf(button)).toBeNull();
    });
    expect(within(button).getByTestId('icon')).toBeInTheDocument();
    expect(button).not.toHaveAttribute('aria-busy');
  });

  it('Botão em andamento por fora', () => {
    themed(
      <Button loading startIcon={<CheckOutlined data-testid="icon" />}>
        Anexar
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Anexar' });

    expect(spinnerOf(button)).not.toBeNull();
    expect(button).toHaveAttribute('aria-busy', 'true');
  });

  it('Clique sem promessa não fica em andamento', () => {
    let opened = false;
    themed(
      <Button
        onClick={() => {
          opened = true;
        }}
      >
        Abrir
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Abrir' });

    fireEvent.click(button);

    expect(opened).toBe(true);
    expect(spinnerOf(button)).toBeNull();
  });

  it('Resposta rápida', async () => {
    vi.useFakeTimers();
    let calls = 0;
    themed(
      <Button
        startIcon={<CheckOutlined data-testid="icon" />}
        onClick={() => {
          calls += 1;
          return Promise.resolve();
        }}
      >
        Salvar
      </Button>,
    );
    const button = screen.getByRole('button', { name: 'Salvar' });

    fireEvent.click(button);
    // The answer comes at once, but the button stays busy until half a second from the click.
    await act(async () => {
      await vi.advanceTimersByTimeAsync(minimumBusyMs - 100);
    });
    expect(spinnerOf(button)).not.toBeNull();
    fireEvent.click(button);
    expect(calls).toBe(1);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(100);
    });

    expect(spinnerOf(button)).toBeNull();
    expect(within(button).getByTestId('icon')).toBeInTheDocument();
    expect(button).not.toHaveAttribute('aria-busy');
  });

  it('Botão desabilitado pela resposta fica em andamento antes', async () => {
    vi.useFakeTimers();
    function Saving() {
      const [changed, setChanged] = useState(true);
      return (
        <Button
          disabled={!changed}
          onClick={() => {
            setChanged(false);
            return Promise.resolve();
          }}
        >
          Salvar
        </Button>
      );
    }
    themed(<Saving />);
    const button = screen.getByRole('button', { name: 'Salvar' });

    fireEvent.click(button);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10);
    });

    // Busy, not grey, while the half second lasts; then disabled.
    expect(button).not.toBeDisabled();
    expect(spinnerOf(button)).not.toBeNull();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(minimumBusyMs);
    });
    expect(button).toBeDisabled();
    expect(spinnerOf(button)).toBeNull();
  });
});
