import { fireEvent, screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { problem, server, signedOut } from '../test/server';

const text = ptBR.localSigning;
const token = 'Yk3q0Qn1kq8yQ6dJ2XrS9wUe7tLzA4pM5vB0cN1hF2g';
const path = `/sign/${token}`;
const answers: { action: string; body: string }[] = [];

beforeEach(() => {
  answers.length = 0;
  // The signer is outside the company: nobody is in a session.
  signedOut();
  server.use(
    http.get('/api/local-signatures/:token', ({ params }) =>
      params.token === token
        ? HttpResponse.json({
            title: 'Aceite da homologação',
            message: 'Os testes de homologação terminaram sem pendências.',
            signer: 'Bruno Lima',
            fileName: 'termo-homologacao.pdf',
          })
        : problem(404),
    ),
    http.post('/api/local-signatures/:token/sign', async ({ request }) => {
      answers.push({ action: 'sign', body: await request.text() });
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/local-signatures/:token/refuse', async ({ request }) => {
      answers.push({ action: 'refuse', body: await request.text() });
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

// Spec: termos-de-aceite, requirement "Provedor local".
describe('Página local de assinatura', () => {
  it('Assinar pela página local', async () => {
    await renderApp(path);

    expect(await screen.findByRole('heading', { name: 'Aceite da homologação' })).toBeVisible();
    expect(screen.getByText('Olá, Bruno Lima.')).toBeVisible();
    expect(screen.getByText('Os testes de homologação terminaram sem pendências.')).toBeVisible();
    expect(screen.getByRole('link', { name: text.open })).toHaveAttribute(
      'href',
      `/api/local-signatures/${token}/document`,
    );
    fireEvent.click(screen.getByRole('button', { name: text.sign }));

    expect(await screen.findByText('Termo assinado. Obrigado, Bruno Lima.')).toBeVisible();
    expect(answers.map((answer) => answer.action)).toEqual(['sign']);
    expect(screen.queryByRole('button', { name: text.sign })).toBeNull();
  });

  it('Recusar pela página local', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.refuse }));

    fireEvent.click(screen.getByRole('button', { name: text.confirmRefusal }));
    expect(await screen.findByText(ptBR.acceptances.errors.reasonRequired)).toBeVisible();
    fireEvent.change(screen.getByLabelText(text.reason), {
      target: { value: 'Falta o relatório de testes.' },
    });
    fireEvent.click(screen.getByRole('button', { name: text.confirmRefusal }));

    expect(await screen.findByText(text.refused)).toBeVisible();
    expect(answers.map((answer) => [answer.action, JSON.parse(answer.body) as unknown])).toEqual([
      ['refuse', { reason: 'Falta o relatório de testes.' }],
    ]);
  });

  it('Link local de outro termo ou já usado', async () => {
    await renderApp('/sign/link-que-nao-existe');

    expect(await screen.findByRole('heading', { name: text.invalid })).toBeVisible();
    expect(within(screen.getByRole('status')).getByText(text.invalidDetail)).toBeVisible();
    expect(screen.queryByRole('button', { name: text.sign })).toBeNull();
    expect(screen.queryByText('Aceite da homologação')).toBeNull();
  });

  it('Link usado enquanto a página estava aberta', async () => {
    server.use(http.post('/api/local-signatures/:token/sign', () => problem(404)));
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.sign }));

    expect(await screen.findByText(text.invalidDetail)).toBeVisible();
  });

  it('Página local no celular', async () => {
    await renderApp(path, phoneWidth);

    expect(await screen.findByRole('button', { name: text.sign })).toBeVisible();
    expect(screen.getByRole('button', { name: text.refuse })).toBeVisible();
  });
});
