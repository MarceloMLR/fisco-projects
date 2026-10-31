import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { EmailStatus } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { expectBusy, expectIdle, heldAnswer } from '../test/busy';
import { renderApp } from '../test/renderApp';
import { problem, server, signedInAs } from '../test/server';

const text = ptBR.settings;

/** The e-mails as the simulated API keeps them, changed by the test as the real queue would. */
let status: EmailStatus;
let tests: number;

beforeEach(() => {
  status = { mailbox: 'projetos@empresa.example', lastTest: null };
  tests = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/admin/email', () => HttpResponse.json(status)),
    http.post('/api/admin/email/test', () => {
      tests++;
      status = {
        ...status,
        lastTest: {
          status: 'Pending',
          at: '2026-10-20T13:00:00Z',
          to: 'paulo.nogueira@empresa.example',
        },
      };
      return HttpResponse.json({ to: 'paulo.nogueira@empresa.example' }, { status: 202 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

async function openSection() {
  await renderApp('/admin/settings');
  const section = await screen.findByRole('region', { name: text.m365 });
  await waitFor(() => {
    expect(within(section).getByRole('textbox', { name: text.mailbox })).toHaveValue(
      'projetos@empresa.example',
    );
  });
  return section;
}

// Spec: avisos, requirement "Teste de envio".
describe('Teste de envio', () => {
  it('Enviar teste pela tela', async () => {
    // Only the timers: the screen reads the result again every 5 seconds while the test waits in the queue.
    vi.useFakeTimers({
      toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval'],
      shouldAdvanceTime: true,
    });
    const section = await openSection();
    expect(within(section).getByRole('textbox', { name: text.mailbox })).toHaveAttribute(
      'readonly',
    );
    expect(within(section).getByText(text.emailTest.none)).toBeInTheDocument();

    fireEvent.click(within(section).getByRole('button', { name: text.emailTest.send }));

    expect(
      await screen.findByText(
        text.emailTest.queued.replace('{{to}}', 'paulo.nogueira@empresa.example'),
      ),
    ).toBeInTheDocument();
    expect(await within(section).findByText(text.emailTest.Pending)).toBeInTheDocument();
    expect(tests).toBe(1);
    // Nothing else of the form changed: the test does not wait for Save.
    expect(screen.getByRole('button', { name: text.save })).toBeDisabled();

    // The queue sent it.
    status = {
      ...status,
      lastTest: {
        status: 'Sent',
        at: '2026-10-20T13:00:00Z',
        to: 'paulo.nogueira@empresa.example',
      },
    };
    await act(async () => {
      await vi.advanceTimersByTimeAsync(5000);
    });

    expect(await within(section).findByText(text.emailTest.Sent)).toBeInTheDocument();
    expect(
      within(section).getByText(
        text.emailTest.detail
          .replace('{{when}}', '20/10/2026 10:00')
          .replace('{{to}}', 'paulo.nogueira@empresa.example'),
      ),
    ).toBeInTheDocument();
  });

  it('Teste que falhou', async () => {
    status = {
      ...status,
      lastTest: {
        status: 'Failed',
        at: '2026-10-20T13:00:00Z',
        to: 'paulo.nogueira@empresa.example',
      },
    };

    const section = await openSection();

    expect(await within(section).findByText(text.emailTest.Failed)).toBeInTheDocument();
  });

  it('Sem caixa de envio configurada', async () => {
    status = { mailbox: null, lastTest: null };
    await renderApp('/admin/settings');
    const section = await screen.findByRole('region', { name: text.m365 });

    const mailbox = within(section).getByRole('textbox', { name: text.mailbox });

    await waitFor(() => {
      expect(mailbox).toHaveAttribute('placeholder', text.noMailbox);
    });
    expect(mailbox).toHaveValue('');
  });

  it('Pessoa sem e-mail', async () => {
    server.use(http.post('/api/admin/email/test', () => problem(400)));
    const section = await openSection();

    fireEvent.click(within(section).getByRole('button', { name: text.emailTest.send }));

    expect(await screen.findByText(text.emailTest.noAddress)).toBeInTheDocument();
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
describe('Botões em andamento no teste de envio', () => {
  it('Botão em andamento ao enviar o teste', async () => {
    const held = heldAnswer();
    server.use(
      http.post('/api/admin/email/test', async () => {
        await held.until;
      }),
    );
    const section = await openSection();
    const send = within(section).getByRole('button', { name: text.emailTest.send });

    fireEvent.click(send);

    await waitFor(() => {
      expectBusy(send, text.emailTest.send);
    });
    fireEvent.click(send);
    held.release();
    await waitFor(() => {
      expectIdle(send);
    });
    expect(tests).toBe(1);
  });
});
