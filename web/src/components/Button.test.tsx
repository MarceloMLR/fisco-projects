import CheckOutlined from '@mui/icons-material/CheckOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { Button, IconButton } from './Button';
import { Form } from './Form';

/** An answer of the server held until the test lets it go, with success or error. */
function heldAnswer() {
  let resolve = () => {};
  let reject = () => {};
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

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
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
    expect(spinnerOf(button)).toBeNull();
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
    expect(within(button).getByTestId('icon')).toBeInTheDocument();
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
    expect(spinnerOf(save)).toBeNull();
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

    expect(spinnerOf(button)).toBeNull();
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
});
