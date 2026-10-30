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
import { demoPeople, demoProjects, membersOf, problem, server, signedInAs } from '../test/server';

const text = ptBR.acceptances;
const requests: { method: string; path: string; body: string }[] = [];
let terms: Acceptance[];
let can: { canRequest: boolean; canAttach: boolean };
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
const [pending, refused] = demoAcceptances();
const before = demoAcceptances().find((term) => term.recipient !== null);
if (pending === undefined || refused === undefined || before === undefined) {
  throw new Error('No pending, refused or earlier term.');
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
  can = { canRequest: true, canAttach: true };
  actions = { canRemind: true, canAttach: true, canCancel: true };
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/members', () => HttpResponse.json(membersOf(project))),
    http.get('/api/projects/:id/acceptances', ({ request }) =>
      HttpResponse.json(acceptanceList(terms, new URL(request.url), can)),
    ),
    http.get('/api/projects/:id/acceptances/milestones', () => HttpResponse.json(demoMilestones)),
    http.get('/api/projects/:id/acceptances/:termId', ({ params }) => {
      const term = terms.find((item) => item.id === params.termId);
      if (term === undefined) {
        return problem(404);
      }
      const open = term.status === 'Pending';
      return HttpResponse.json(
        acceptanceDetails(
          term,
          open ? { ...actions, canRemind: term.recipient === null && actions.canRemind } : {},
        ),
      );
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
          signers: [
            { name: 'Helena Rocha', status: 'Pending' },
            { name: 'Igor Pires', status: 'Pending' },
          ],
          recipient: null,
          requested: { ...demoPeople.marina, at: '2026-10-20T13:00:00Z' },
          answered: null,
          closedAt: null,
          isRequest: true,
        },
        ...terms,
      ];
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.post('/api/projects/:id/acceptances/:termId/attached', async ({ params, request }) => {
      requests.push({ method: 'POST', path: 'attached', body: await request.text() });
      changeTerm(String(params.termId), {
        status: 'Signed',
        answered: { ...demoPeople.marina, at: '2026-10-20T14:00:00Z' },
        closedAt: '2026-10-20T14:00:00Z',
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

/** The rows of the terms in the table, as text. */
function rows(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => row.textContent);
}

/** Chooses an option by its label. */
async function choose(dialog: HTMLElement, field: string, option: string) {
  fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: field }));
  fireEvent.click(await screen.findByRole('option', { name: (name) => name.startsWith(option) }));
}

function typeSigner(dialog: HTMLElement, index: number, name: string, email: string) {
  fireEvent.change(
    within(dialog).getByLabelText(text.dialog.signerName.replace('{{index}}', String(index))),
    { target: { value: name } },
  );
  fireEvent.change(
    within(dialog).getByLabelText(text.dialog.signerEmail.replace('{{index}}', String(index))),
    { target: { value: email } },
  );
}

async function openTerm(termId = pendingId, width?: number) {
  await renderApp(`${path}?term=${termId}`, width);
  return screen.findByRole('dialog', { name: text.details.label });
}

// Spec: termos-de-aceite, requirement "Aba dos termos da gestão".
describe('Aba dos termos da gestão', () => {
  it('Lista com os signatários e o filtro', async () => {
    await renderApp(path);

    await waitFor(() => {
      expect(rows()).toHaveLength(5);
    });
    expect(rows()[0]).toContain('Aceite da homologação');
    expect(rows()[0]).toContain('Homologação concluída · 03/11');
    expect(rows()[0]).toContain('1 de 2 assinou');
    expect(rows()[0]).toContain('Ana Souza e Bruno Lima');
    expect(rows()[2]).toContain('2 de 2 assinaram');
    expect(rows()[3]).toContain('Carla Mendes');
    expect(rows()[3]).toContain(text.signers.before);
    expect(rows()[4]).toContain(text.signers.outside);
    const grid = screen.getByRole('grid', { name: text.table });
    expect(within(grid).getAllByText(text.statuses.Signed)).toHaveLength(2);
    expect(within(grid).getByText(text.statuses.Refused)).toBeVisible();
    expect(screen.getByText('5 termos · 2 pendentes')).toBeVisible();
    expect(screen.getByRole('tab', { name: /Termos de aceite/ })).toHaveAttribute(
      'aria-selected',
      'true',
    );

    fireEvent.click(screen.getByRole('radio', { name: /Assinados/ }));

    await waitFor(() => {
      expect(rows()).toHaveLength(2);
    });
  });

  it('Pedir assinatura pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });

    await choose(dialog, text.dialog.milestone, 'Homologação concluída · 03/11/2026');
    fireEvent.change(within(dialog).getByLabelText(text.dialog.title), {
      target: { value: 'Aceite do cadastro de produtos' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.message), {
      target: { value: 'Assinem até sexta.' },
    });
    fireEvent.change(screen.getByTestId('acceptance-file'), {
      target: { files: [pdf('termo-cadastro.pdf')] },
    });
    typeSigner(dialog, 1, 'Helena Rocha', 'helena.rocha@cliente.example');
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.addSigner }));
    typeSigner(dialog, 2, 'Igor Pires', 'igor.pires@cliente.example');
    expect(within(dialog).getByTestId('acceptance-file-name')).toHaveTextContent(
      'termo-cadastro.pdf',
    );
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));

    expect(await screen.findByText('Assinatura pedida a 2 signatários.')).toBeVisible();
    const sent = requests[0]?.body ?? '';
    expect(sent).toContain('name="milestoneId"');
    expect(sent).toContain(demoMilestones[0]?.id);
    expect(sent).toContain('Aceite do cadastro de produtos');
    expect(sent).toContain('Assinem até sexta.');
    expect(sent).toContain('name="signers[0].name"');
    expect(sent).toContain('helena.rocha@cliente.example');
    expect(sent).toContain('name="signers[1].email"');
    expect(sent).toContain('igor.pires@cliente.example');
    expect(sent).toContain('name="file"');
    expect(sent).not.toContain('recipientId');
    await waitFor(() => {
      expect(rows()[0]).toContain('0 de 2 assinaram');
    });
    expect(rows()[0]).toContain(text.statuses.Pending);
    expect(screen.queryByRole('dialog', { name: text.dialog.requestTitle })).toBeNull();
  });

  // Spec: usuarios-e-papeis, requirement "E-mail na escolha de pessoa".
  it('Pessoa do pedido de aceite com o e-mail', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });

    expect(within(dialog).getAllByTestId('acceptance-signer')).toHaveLength(1);
    expect(
      within(dialog).getByRole('textbox', {
        name: text.dialog.signerName.replace('{{index}}', '1'),
      }),
    ).toHaveValue('');
    expect(
      within(dialog).getByRole('textbox', {
        name: text.dialog.signerEmail.replace('{{index}}', '1'),
      }),
    ).toHaveAttribute('type', 'email');
    expect(within(dialog).queryByRole('combobox', { name: 'Para' })).toBeNull();
  });

  it('Signatários incluídos e tirados', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });
    const remove = (index: number) =>
      within(dialog).getByRole('button', {
        name: text.dialog.removeSigner.replace('{{index}}', String(index)),
      });

    expect(remove(1)).toBeDisabled();
    for (let index = 1; index < 10; index++) {
      fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.addSigner }));
    }

    expect(within(dialog).getAllByTestId('acceptance-signer')).toHaveLength(10);
    expect(within(dialog).getByRole('button', { name: text.dialog.addSigner })).toBeDisabled();
    fireEvent.click(remove(3));
    expect(within(dialog).getAllByTestId('acceptance-signer')).toHaveLength(9);
  });

  it('Pedido incompleto pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });

    fireEvent.change(screen.getByTestId('acceptance-file'), {
      target: { files: [new File(['PK'], 'termo.docx')] },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.addSigner }));
    typeSigner(dialog, 1, 'Ana Souza', 'ana.souza');
    typeSigner(dialog, 2, 'Ana S.', 'ana.souza');
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));

    expect(await within(dialog).findByText(text.errors.milestoneRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.titleRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.fileNotPdf)).toBeVisible();
    expect(within(dialog).getAllByText(text.errors.signerEmailInvalid)).toHaveLength(2);
    typeSigner(dialog, 1, 'Ana Souza', 'ana.souza@cliente.example');
    typeSigner(dialog, 2, 'Ana S.', 'ANA.SOUZA@cliente.example');
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));
    expect(await within(dialog).findByText(text.errors.signerEmailRepeated)).toBeVisible();
    expect(requests).toEqual([]);
  });

  it('Provedor fora do ar ao pedir pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/acceptances', () =>
        problem(502, 'urn:fiscoprojects:problem:signature-provider-failed'),
      ),
    );
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.request }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.requestTitle });
    await choose(dialog, text.dialog.milestone, 'Homologação concluída · 03/11/2026');
    fireEvent.change(within(dialog).getByLabelText(text.dialog.title), {
      target: { value: 'Aceite do cadastro de produtos' },
    });
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    typeSigner(dialog, 1, 'Helena Rocha', 'helena.rocha@cliente.example');
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitRequest }));

    expect(await within(dialog).findByText(text.messages.providerFailed)).toBeVisible();
  });

  it('Anexar termo assinado sem pedido pela tela', async () => {
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: text.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });

    expect(within(dialog).queryByLabelText(text.dialog.message)).toBeNull();
    expect(within(dialog).queryByTestId('acceptance-signer')).toBeNull();
    await choose(dialog, text.dialog.milestone, 'Escopo aprovado · 05/10/2026');
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
    expect(requests[0]?.body).toContain('Aceite do escopo');
    expect(requests[0]?.body).not.toContain('recipientId');
  });

  // Spec: termos-de-aceite, requirement "Provedor de assinatura".
  it('Pedido sem provedor configurado', async () => {
    can = { canRequest: false, canAttach: true };
    terms = [];
    await renderApp(path);

    expect(await screen.findByRole('button', { name: text.attach })).toBeVisible();
    expect(screen.queryByRole('button', { name: text.request })).toBeNull();
    expect(screen.getByText(text.emptyNoProvider)).toBeVisible();
  });

  it('Aba só da gestão', async () => {
    signedInAs('TechLead');
    server.use(
      http.get('/api/session', () =>
        HttpResponse.json({
          ...demoPeople.rafael,
          email: 'rafael.souza@empresa.example',
          role: 'TechLead',
          theme: 'Automatic',
        }),
      ),
      http.get('/api/projects/:id/acceptances', () => problem(403)),
    );
    await renderApp(`/projects/${project.id}`);
    await screen.findByRole('tab', { name: /Acessos/ });

    expect(screen.queryByRole('tab', { name: /Termos de aceite/ })).toBeNull();
  });

  it('Termos pelo endereço para quem não é da gestão', async () => {
    server.use(
      http.get('/api/session', () =>
        HttpResponse.json({
          ...demoPeople.carla,
          email: 'carla.mendes@empresa.example',
          role: 'User',
          theme: 'Automatic',
        }),
      ),
    );
    await renderApp(path);

    expect(await screen.findByText(text.onlyManagement)).toBeVisible();
    expect(screen.queryByRole('grid', { name: text.table })).toBeNull();
  });
});

// Spec: termos-de-aceite, requirement "Gaveta do termo com os signatários".
describe('Gaveta do termo com os signatários', () => {
  it('Signatários na gaveta', async () => {
    const drawer = await openTerm();

    expect(within(drawer).getByRole('heading', { name: 'Aceite da homologação' })).toBeVisible();
    expect(within(drawer).getByText('Homologação concluída · 03/11/2026')).toBeVisible();
    expect(within(drawer).getByText(/Os testes de homologação/)).toBeVisible();
    const signers = within(drawer).getByRole('region', { name: text.details.signers });
    const [ana, bruno] = within(signers).getAllByRole('listitem');
    if (ana === undefined || bruno === undefined) {
      throw new Error('No signers.');
    }
    expect(within(ana).getByText('Ana Souza')).toBeVisible();
    expect(within(ana).getByText(text.signerStatuses.Signed)).toBeVisible();
    expect(within(ana).getByText('Assinou em 16/10/2026 11:20')).toBeVisible();
    expect(within(ana).getByText('ana.souza@cliente.example')).toBeVisible();
    expect(within(ana).queryByRole('link')).toBeNull();
    expect(within(bruno).getByText(text.signerStatuses.Pending)).toBeVisible();
    expect(within(bruno).getByText('bruno.lima@cliente.example')).toBeVisible();
    expect(
      within(bruno).getByRole('link', { name: 'Abrir a assinatura de Bruno Lima' }),
    ).toHaveAttribute('href', `/sign/token-${pendingId.slice(-4)}-1`);
    expect(within(signers).getByText('1 de 2 assinou')).toBeVisible();
    expect(within(drawer).getByText(text.details.localLink)).toBeVisible();
    expect(
      within(drawer).getByRole('link', {
        name: text.details.open.replace('{{name}}', 'termo-homologacao-erp-fin.pdf'),
      }),
    ).toHaveAttribute('href', `/api/projects/${project.id}/acceptances/${pendingId}/file`);
    expect(within(drawer).getByText('Assinatura pedida a Ana Souza e Bruno Lima')).toBeVisible();
    expect(within(drawer).getByText('Assinado por Ana Souza')).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.remind })).toBeVisible();
  });

  it('E-mail fora da gaveta do termo fechado', async () => {
    const drawer = await openTerm(refused.id);

    const signers = within(drawer).getByRole('region', { name: text.details.signers });
    expect(within(signers).getByText(text.signerStatuses.Refused)).toBeVisible();
    expect(within(signers).getByText('Falta o relatório de testes de ICMS-ST.')).toBeVisible();
    expect(within(signers).queryByText(/@cliente\.example/)).toBeNull();
    expect(within(drawer).getByText('Recusado por Ana Souza')).toBeVisible();
    expect(within(drawer).queryByRole('button', { name: text.actions.cancel })).toBeNull();
  });

  it('Pedido anterior pendente pela tela', async () => {
    const drawer = await openTerm(before.id);

    expect(within(drawer).getByText('Carla Mendes')).toBeVisible();
    expect(within(drawer).getByText('Pedido enviado a Carla Mendes')).toBeVisible();
    expect(within(drawer).queryByRole('region', { name: text.details.signers })).toBeNull();
    expect(within(drawer).getByRole('button', { name: text.actions.attach })).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.cancel })).toBeVisible();
    expect(within(drawer).queryByRole('button', { name: text.actions.remind })).toBeNull();
  });

  // Spec: estrutura-das-telas, requirement "Voltar do navegador".
  it('Gaveta fora do histórico', async () => {
    const { router } = await renderApp('/');
    await act(() => router.navigate(path));
    await waitFor(() => {
      expect(rows()).toHaveLength(5);
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

  it('Lembrar e cancelar pela tela', async () => {
    const drawer = await openTerm();

    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.remind }));
    expect(await screen.findByText(text.messages.reminded)).toBeVisible();
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.cancel }));
    const confirm = await screen.findByRole('alertdialog', { name: text.cancelRequest.title });
    expect(
      within(confirm).getByText(
        text.cancelRequest.textSigners.replace('{{title}}', 'Aceite da homologação'),
      ),
    ).toBeVisible();
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
      expect(rows()).toHaveLength(4);
    });
  });

  it('Anexar num pedido pendente pela tela', async () => {
    const drawer = await openTerm();

    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });
    expect(within(dialog).getByText('Pedido Aceite da homologação')).toBeVisible();
    expect(within(dialog).getByText(text.dialog.onBehalfRequestHelp)).toBeVisible();
    expect(within(dialog).queryByRole('combobox', { name: text.dialog.milestone })).toBeNull();
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitAttach }));

    expect(await screen.findByText(text.messages.attached)).toBeVisible();
    expect(requests[0]?.path).toBe('attached');
    expect(
      await within(drawer).findByText('Termo assinado anexado por Marina Costa'),
    ).toBeVisible();
  });

  it('Anexar em nome da pessoa pela tela', async () => {
    const drawer = await openTerm(before.id);

    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });
    expect(within(dialog).getByText('Em nome de Carla Mendes')).toBeVisible();
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitAttach }));

    expect(await screen.findByText(text.messages.attached)).toBeVisible();
    expect(
      await within(drawer).findByText(
        'Termo assinado anexado por Marina Costa em nome de Carla Mendes',
      ),
    ).toBeVisible();
  });

  it('Projeto encerrado ao anexar pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/acceptances/:termId/attached', () =>
        problem(409, 'urn:fiscoprojects:problem:project-closed'),
      ),
    );
    const drawer = await openTerm();
    fireEvent.click(within(drawer).getByRole('button', { name: text.actions.attach }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.attachTitle });
    fireEvent.change(screen.getByTestId('acceptance-file'), { target: { files: [pdf()] } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.submitAttach }));

    expect(await within(dialog).findByText(text.messages.closed)).toBeVisible();
  });

  it('Signatários no celular', async () => {
    const drawer = await openTerm(pendingId, phoneWidth);

    expect(within(drawer).getByRole('region', { name: text.details.signers })).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.actions.remind })).toBeVisible();
  });
});
