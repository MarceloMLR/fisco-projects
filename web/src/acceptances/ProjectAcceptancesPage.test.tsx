import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Acceptance, AcceptanceDetails } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  acceptanceDetails,
  acceptanceList,
  demoAcceptances,
  demoMilestones,
} from '../test/demoAcceptances';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoProjects,
  emailOf,
  membersOf,
  problem,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.acceptances;
const requests: { method: string; path: string; body: string }[] = [];
let terms: Acceptance[];
let manage: { canManage: boolean; canRequest: boolean };
let actions: Partial<AcceptanceDetails['actions']>;

const erp = demoProjects()[0];
if (erp === undefined) {
  throw new Error('No ERP-FIN.');
}
const project = {
  ...erp,
  members: [
    ...erp.members,
    { ...demoPeople.carla, role: 'Contributor' as const },
    { ...demoPeople.diego, role: 'Developer' as const },
  ],
};
const path = `/projects/${project.id}/acceptances`;
const [pending] = demoAcceptances();
if (pending === undefined) {
  throw new Error('No pending term.');
}
const pendingId = pending.id;

const pdf = (name = 'termo-assinado.pdf') =>
  new File(['%PDF-1.7 termo'], name, { type: 'application/pdf' });

function changeTerm(termId: string, change: Partial<Acceptance>) {
  terms = terms.map((term) => (term.id === termId ? { ...term, ...change } : term));
}

beforeEach(() => {
  requests.length = 0;
  terms = demoAcceptances();
  manage = { canManage: true, canRequest: true };
  actions = { canRemind: true, canAttach: true, canCancel: true };
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/members', () => HttpResponse.json(membersOf(project))),
    http.get('/api/projects/:id/acceptances', ({ request }) =>
      HttpResponse.json(acceptanceList(terms, new URL(request.url), manage)),
    ),
    http.get('/api/projects/:id/acceptances/milestones', () => HttpResponse.json(demoMilestones)),
    http.get('/api/projects/:id/acceptances/:termId', ({ params }) => {
      const term = terms.find((item) => item.id === params.termId);
      return term === undefined
        ? problem(404)
        : HttpResponse.json(acceptanceDetails(term, term.status === 'Pending' ? actions : {}));
    }),
    http.post('/api/projects/:id/acceptances', async ({ request }) => {
      const body = await request.text();
      requests.push({ method: 'POST', path: 'request', body });
      const id = '0199f5a2-7c1e-7d40-9a1b-a00000000099';
      terms = [
        {
          id,
          title: 'Aceite do cadastro de produtos',
          status: 'Pending',
          milestoneId: demoMilestones[0]?.id ?? '',
          milestone: demoMilestones[0] ?? null,
          recipient: demoPeople.carla,
          requested: { ...demoPeople.marina, at: '2026-10-20T13:00:00Z' },
          answered: null,
          isRequest: true,
        },
        ...terms,
      ];
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.post('/api/projects/:id/acceptances/:termId/signed', async ({ params, request }) => {
      requests.push({ method: 'POST', path: 'signed', body: await request.text() });
      changeTerm(String(params.termId), {
        status: 'Signed',
        answered: { ...demoPeople.carla, at: '2026-10-20T14:00:00Z' },
      });
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/projects/:id/acceptances/:termId/attached', async ({ params, request }) => {
      requests.push({ method: 'POST', path: 'attached', body: await request.text() });
      changeTerm(String(params.termId), {
        status: 'Signed',
        answered: { ...demoPeople.marina, at: '2026-10-20T14:00:00Z' },
      });
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/projects/:id/acceptances/:termId/refusal', async ({ params, request }) => {
      requests.push({ method: 'POST', path: 'refusal', body: await request.text() });
      changeTerm(String(params.termId), {
        status: 'Refused',
        answered: { ...demoPeople.carla, at: '2026-10-20T14:00:00Z' },
      });
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/projects/:id/acceptances/:termId/reminder', ({ params }) => {
      requests.push({ method: 'POST', path: `reminder ${String(params.termId)}`, body: '' });
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/projects/:id/acceptances/:termId', ({ params }) => {
      requests.push({ method: 'DELETE', path: String(params.termId), body: '' });
      terms = terms.filter((term) => term.id !== params.termId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** The titles of the terms in the table. */
function titles(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => within(row).queryAllByRole('gridcell')[0]?.textContent ?? '');
}

/** Chooses an option by its label, above the e-mail of a person. */
async function choose(dialog: HTMLElement, field: string, option: string) {
  fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: field }));
  fireEvent.click(await screen.findByRole('option', { name: (name) => name.startsWith(option) }));
}

async function openTerm(termId = pendingId, width?: number) {
  await renderApp(`${path}?term=${termId}`, width);
  return screen.findByRole('dialog', { name: text.details.label });
}

// Spec: termos-de-aceite, requirement "Termos pela tela".
describe('Termos pela tela', () => {
  it('Lista com a situação e o filtro', async () => {
    await renderApp(path);

    await waitFor(() => {
      expect(titles()).toHaveLength(4);
    });
    expect(titles()[0]).toContain('Aceite da homologação');
    expect(titles()[0]).toContain('Homologação concluída · 03/11');
    const grid = screen.getByRole('grid', { name: text.table });
    expect(within(grid).getAllByText(text.statuses.Signed)).toHaveLength(2);
    expect(within(grid).getByText(text.statuses.Refused)).toBeVisible();
    expect(screen.getByText('4 termos · 1 pendente')).toBeVisible();
    expect(screen.getByRole('tab', { name: /Termos de aceite/ })).toHaveAttribute(
      'aria-selected',
      'true',
    );

    fireEvent.click(screen.getByRole('radio', { name: /Assinados/ }));

    await waitFor(() => {
      expect(titles()).toHaveLength(2);
    });
  });

  it('Pedir aceite pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });

    await choose(dialog, text.dialog.milestone, 'Homologação concluída · 03/11/2026');
    await choose(dialog, text.dialog.recipient, 'Carla Mendes');
    fireEvent.change(within(dialog).getByLabelText(text.dialog.title), {
      target: { value: 'Aceite do cadastro de produtos' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.message), {
      target: { value: 'Assine até sexta.' },
    });
    fireEvent.change(screen.getByTestId('acceptance-file'), {
      target: { files: [pdf('termo-cadastro.pdf')] },
    });
    expect(within(dialog).getByTestId('acceptance-file-name')).toHaveTextContent(
      'termo-cadastro.pdf',
    );
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));

    expect(await screen.findByText('Aceite pedido a Carla Mendes.')).toBeVisible();
    const sent = requests[0]?.body ?? '';
    expect(sent).toContain('name="milestoneId"');
    expect(sent).toContain(demoMilestones[0]?.id);
    expect(sent).toContain(demoPeople.carla.id);
    expect(sent).toContain('Aceite do cadastro de produtos');
    expect(sent).toContain('Assine até sexta.');
    expect(sent).toContain('name="file"');
    await waitFor(() => {
      expect(titles()[0]).toContain('Aceite do cadastro de produtos');
    });
    expect(screen.queryByRole('dialog', { name: text.dialog.requestTitle })).toBeNull();
  });

  // Spec: usuarios-e-papeis, requirement "E-mail na escolha de pessoa".
  it('Pessoa do pedido de aceite com o e-mail', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });
    const field = within(dialog).getByRole('combobox', { name: text.dialog.recipient });

    fireEvent.mouseDown(field);

    const people = project.members
      .map((member) => member.name)
      .toSorted((one, other) => one.localeCompare(other, 'pt-BR'));
    await waitFor(() => {
      expect(
        screen
          .getAllByRole('option')
          .slice(1)
          .map((option) => option.textContent),
      ).toEqual(people.map((name) => `${name}${emailOf(name)}`));
    });
    fireEvent.click(
      screen.getByRole('option', { name: (name) => name.startsWith('Carla Mendes') }),
    );
    expect(field).toHaveTextContent(/^Carla Mendes$/);
  });

  it('Pedido incompleto pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });

    fireEvent.change(screen.getByTestId('acceptance-file'), {
      target: { files: [new File(['PK'], 'termo.docx')] },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));

    expect(await within(dialog).findByText(text.errors.milestoneRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.recipientRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.titleRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.fileNotPdf)).toBeVisible();
    expect(requests).toEqual([]);
  });

  it('Anexar termo assinado sem pedido pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });

    expect(within(dialog).queryByLabelText(text.dialog.message)).toBeNull();
    await choose(dialog, text.dialog.milestone, 'Escopo aprovado · 05/10/2026');
    await choose(dialog, text.dialog.recipientAttached, 'Diego Torres');
    fireEvent.change(within(dialog).getByLabelText(text.dialog.title), {
      target: { value: 'Aceite do escopo' },
    });
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    server.use(
      http.post('/api/projects/:id/acceptances/attached', async ({ request }) => {
        requests.push({ method: 'POST', path: 'attach', body: await request.text() });
        return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-a00000000098' }, { status: 201 });
      }),
    );
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitAttach }));

    expect(await screen.findByText(text.messages.attached)).toBeVisible();
    expect(requests[0]?.path).toBe('attach');
    expect(requests[0]?.body).toContain(demoPeople.diego.id);
  });

  it('Sem pedir aceite para quem não gerencia', async () => {
    manage = { canManage: false, canRequest: false };
    terms = terms.filter((term) => term.recipient.id === demoPeople.carla.id);
    await renderApp(path);
    await waitFor(() => {
      expect(titles()).toHaveLength(3);
    });

    expect(screen.queryByRole('button', { name: text.request })).toBeNull();
    expect(screen.queryByRole('button', { name: text.attach })).toBeNull();
  });

  it('Aba só para quem vê termos', async () => {
    manage = { canManage: false, canRequest: false };
    terms = [];
    await renderApp(`/projects/${project.id}`);
    await screen.findByRole('tab', { name: /Acessos/ });

    expect(screen.queryByRole('tab', { name: /Termos de aceite/ })).toBeNull();
  });
});

// Spec: termos-de-aceite, requirement "Gaveta do termo".
describe('Gaveta do termo', () => {
  it('Detalhes do termo', async () => {
    const drawer = await openTerm();

    expect(within(drawer).getByRole('heading', { name: 'Aceite da homologação' })).toBeVisible();
    expect(within(drawer).getByText('Homologação concluída · 03/11/2026')).toBeVisible();
    expect(within(drawer).getByText(/Carla, os testes de homologação/)).toBeVisible();
    expect(within(drawer).getByText('termo-homologacao-erp-fin.pdf')).toBeVisible();
    expect(
      within(drawer).getByRole('link', {
        name: text.details.open.replace('{{name}}', 'termo-homologacao-erp-fin.pdf'),
      }),
    ).toHaveAttribute('href', `/api/projects/${project.id}/acceptances/${pendingId}/file`);
    expect(within(drawer).getByText('Pedido enviado a Carla Mendes')).toBeVisible();
    expect(within(drawer).getByRole('button', { name: 'Lembrar Carla' })).toBeVisible();
  });

  // Spec: estrutura-das-telas, requirement "Voltar do navegador".
  it('Gaveta fora do histórico', async () => {
    const { router } = await renderApp('/');
    await act(() => router.navigate(path));
    await waitFor(() => {
      expect(titles()).toHaveLength(4);
    });

    const grid = screen.getByRole('grid', { name: text.table });
    fireEvent.click(
      within(grid).getAllByRole('row')[1]?.querySelector('[role="gridcell"]') ?? grid,
    );
    const drawer = await screen.findByRole('dialog', { name: text.details.label });
    expect(router.state.location.search).toContain('term=');
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(router.state.location.search).toBe('');
    });

    // One Back leaves the tab: opening and closing the term added nothing to the history.
    await act(() => router.navigate(-1));
    expect(router.state.location.pathname).toBe('/');
  });

  it('Responder pela tela', async () => {
    actions = { canAnswer: true };
    const drawer = await openTerm();

    expect(within(drawer).getByText(text.details.toSign)).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.send })).toBeDisabled();
    fireEvent.change(screen.getByTestId('acceptance-signed-file'), {
      target: { files: [pdf()] },
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.send }));

    expect(await screen.findByText(text.messages.signed)).toBeVisible();
    expect(requests[0]?.path).toBe('signed');
    expect(requests[0]?.body).toContain('name="file"');
    expect(
      await within(drawer).findByText('Termo assinado enviado por Carla Mendes'),
    ).toBeVisible();
    expect(within(drawer).getByText('termo-assinado.pdf')).toBeVisible();
    expect(within(drawer).queryByRole('button', { name: text.actions.send })).toBeNull();
  });

  it('Termo assinado só em PDF pela tela', async () => {
    actions = { canAnswer: true };
    const drawer = await openTerm();

    fireEvent.change(screen.getByTestId('acceptance-signed-file'), {
      target: { files: [new File(['x'], 'termo.png', { type: 'image/png' })] },
    });

    expect(await within(drawer).findByText(text.errors.fileNotPdf)).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.send })).toBeDisabled();
  });

  it('Recusar pela tela', async () => {
    actions = { canAnswer: true };
    const drawer = await openTerm();

    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.refuse }));
    const dialog = await screen.findByRole('dialog', { name: text.refuse.title });
    fireEvent.click(within(dialog).getByRole('button', { name: text.refuse.submit }));
    expect(await within(dialog).findByText(text.errors.reasonRequired)).toBeVisible();
    fireEvent.change(within(dialog).getByLabelText(text.refuse.reason), {
      target: { value: 'Falta o relatório de testes de ICMS-ST.' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.refuse.submit }));

    expect(await screen.findByText(text.messages.refused)).toBeVisible();
    expect(JSON.parse(requests[0]?.body ?? '{}')).toEqual({
      reason: 'Falta o relatório de testes de ICMS-ST.',
    });
    expect(await within(drawer).findByText(text.statuses.Refused)).toBeVisible();
    expect(within(drawer).getByText('Falta o relatório de testes de ICMS-ST.')).toBeVisible();
  });

  it('Lembrar e cancelar pela tela', async () => {
    const drawer = await openTerm();

    fireEvent.click(within(drawer).getByRole('button', { name: 'Lembrar Carla' }));
    expect(await screen.findByText('Lembrete enviado a Carla Mendes.')).toBeVisible();
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.cancel }));
    const confirm = await screen.findByRole('alertdialog', { name: text.cancelRequest.title });
    fireEvent.click(within(confirm).getByRole('button', { name: text.cancelRequest.confirm }));

    expect(await screen.findByText(text.messages.cancelled)).toBeVisible();
    expect(requests.map((request) => `${request.method} ${request.path}`)).toEqual([
      `POST reminder ${pendingId}`,
      `DELETE ${pendingId}`,
    ]);
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.details.label })).toBeNull();
    });
    await waitFor(() => {
      expect(titles()).toHaveLength(3);
    });
  });

  it('Anexar em nome da pessoa pela tela', async () => {
    const drawer = await openTerm();

    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });
    expect(within(dialog).getByText('Em nome de Carla Mendes')).toBeVisible();
    expect(within(dialog).queryByRole('combobox', { name: text.dialog.milestone })).toBeNull();
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitAttach }));

    expect(await screen.findByText(text.messages.attached)).toBeVisible();
    expect(requests[0]?.path).toBe('attached');
    expect(
      await within(drawer).findByText(
        'Termo assinado anexado por Marina Costa em nome de Carla Mendes',
      ),
    ).toBeVisible();
  });

  it('Projeto encerrado ao responder pela tela', async () => {
    actions = { canAnswer: true };
    server.use(
      http.post('/api/projects/:id/acceptances/:termId/signed', () =>
        problem(409, 'urn:fiscoprojects:problem:project-closed'),
      ),
    );
    const drawer = await openTerm();
    fireEvent.change(screen.getByTestId('acceptance-signed-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.send }));

    expect(await within(drawer).findByText(text.messages.closed)).toBeVisible();
  });

  it('Termo recusado pela API pela tela', async () => {
    actions = { canAnswer: true };
    server.use(
      http.post('/api/projects/:id/acceptances/:termId/signed', () =>
        HttpResponse.json(
          {
            status: 400,
            type: 'urn:fiscoprojects:problem:acceptance-file-refused',
            reason: 'NotPdf',
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    const drawer = await openTerm();
    fireEvent.change(screen.getByTestId('acceptance-signed-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.send }));

    expect(await within(drawer).findByText(text.fileRefused.NotPdf)).toBeVisible();
  });

  it('Responder no celular', async () => {
    actions = { canAnswer: true };
    const drawer = await openTerm(pendingId, phoneWidth);

    expect(within(drawer).getByRole('button', { name: text.actions.choose })).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.refuse })).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.send })).toBeDisabled();
  });
});
