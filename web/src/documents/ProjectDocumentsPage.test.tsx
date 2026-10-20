import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { DocumentFolders, DocumentRow } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  demoDocumentArea,
  demoDocumentDetails,
  demoDocuments,
  documentsPage,
  folderIds,
} from '../test/demoDocuments';
import { phoneWidth, renderApp } from '../test/renderApp';
import { demoProjects, demoSchedule, problem, server, signedInAs } from '../test/server';

const text = ptBR.documents;
const requests: { method: string; path: string; body: string }[] = [];
let area: DocumentFolders;
let documents: DocumentRow[];

const erp = demoProjects()[0];
if (erp === undefined) {
  throw new Error('No ERP-FIN.');
}
const project = erp;
const path = `/projects/${project.id}/documents`;

beforeEach(() => {
  requests.length = 0;
  area = demoDocumentArea();
  documents = demoDocuments();
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(demoSchedule())),
    http.get('/api/projects/:id/documents/folders', () => HttpResponse.json(area)),
    http.get('/api/projects/:id/documents', ({ request }) =>
      HttpResponse.json(documentsPage(documents, new URL(request.url))),
    ),
    http.get('/api/projects/:id/documents/:documentId', ({ params }) => {
      const row = documents.find((document) => document.id === params.documentId);
      return row === undefined
        ? new HttpResponse(null, { status: 404 })
        : HttpResponse.json({ ...demoDocumentDetails(row), permissions: area.permissions });
    }),
    http.post('/api/projects/:id/documents', async ({ request }) => {
      // The raw form: the parser of the tests does not take the files of jsdom.
      requests.push({ method: 'POST', path: '', body: await request.text() });
      return HttpResponse.json({ id: 'novo', version: 1 }, { status: 201 });
    }),
    http.put('/api/projects/:id/documents/:documentId/:action', async ({ params, request }) => {
      requests.push({
        method: 'PUT',
        path: `${String(params.documentId)}/${String(params.action)}`,
        body: await request.text(),
      });
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/projects/:id/documents/:documentId', ({ params }) => {
      requests.push({ method: 'DELETE', path: String(params.documentId), body: '' });
      documents = documents.filter((document) => document.id !== params.documentId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** The names in the grid of the documents. */
function names(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => within(row).queryAllByRole('gridcell')[0]?.textContent ?? '');
}

async function openDocuments(width?: number) {
  await renderApp(path, width);
  await screen.findByRole('grid', { name: text.table });
  await waitFor(() => {
    expect(names().length).toBeGreaterThan(0);
  });
}

/** Opens the details of the document of the row whose first cell has the name. */
async function choose(name: string) {
  const grid = screen.getByRole('grid', { name: text.table });
  const cell = within(grid).getByText(name);
  fireEvent.click(cell);
  const drawer = await screen.findByRole('dialog', { name: text.details.label });
  await within(drawer).findByRole('heading', { name });
  return drawer;
}

describe('Documentos', () => {
  it('Aba Documentos com as pastas', async () => {
    await openDocuments();

    expect(screen.getByRole('tab', { name: /Documentos/ })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    const folders = screen.getByRole('navigation', { name: text.folders });
    expect(
      within(folders)
        .getAllByRole('button')
        .map((button) => button.textContent),
    ).toEqual([
      '01 Gestão do projeto1',
      '02 Contratos e compras0',
      '03 Requisitos e especificações3',
      '04 Atas de reunião0',
      '05 Entregas e manuais0',
      '06 Termos de aceite0',
    ]);
    // The first folder until another is chosen.
    expect(names()).toEqual([expect.stringContaining('Plano do projeto.docx')]);

    fireEvent.click(within(folders).getByRole('button', { name: /03 Requisitos/ }));

    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    expect(names()[0]).toContain('Especificação das regras de ICMS-ST.pdf');
    expect(names()[0]).toContain('Versão 3 · Parametrizar regras de ICMS');
    expect(names()[2]).toContain('Versão 2 · sem vínculo');
  });

  it('Obsoleto sai da lista padrão', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });

    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.statusFilter }));
    fireEvent.click(await screen.findByRole('option', { name: text.filters.All }));

    await waitFor(() => {
      expect(names()).toHaveLength(4);
    });
    expect(
      names().some((name) => name.includes('Especificação de relatórios fiscais v1.pdf')),
    ).toBe(true);
  });

  it('Buscar nos documentos do projeto', async () => {
    await openDocuments();

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'especificação' },
    });

    expect(
      await screen.findByRole('heading', { name: new RegExp(text.searchResults) }),
    ).toBeVisible();
    // The folder of each document found, since the search goes through all of them.
    await waitFor(() => {
      expect(names()).toEqual([
        expect.stringContaining('03 Requisitos e especificações · Versão 3'),
      ]);
    });
    expect(names()[0]).toContain('Especificação das regras de ICMS-ST.pdf');
  });

  it('Enviar arrastando', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    const files = [
      new File(['CFOP;Descrição'], 'Regras de CFOP.csv', { type: 'text/csv' }),
      new File(['ata'], 'Ata de kickoff.docx'),
    ];
    const drop = screen.getByTestId('documents-drop');

    fireEvent.dragEnter(drop, { dataTransfer: { types: ['Files'], files } });
    expect(
      screen.getByText(text.dropHere.replace('{{folder}}', '03 Requisitos e especificações')),
    ).toBeVisible();
    fireEvent.drop(drop, { dataTransfer: { types: ['Files'], files } });

    await waitFor(() => {
      expect(requests).toHaveLength(2);
    });
    // One file at a time, to the folder; the file goes in the field file (jsdom does not write its content).
    expect(requests[0]?.body).toContain(folderIds.requirements);
    expect(requests[0]?.body).toContain('name="file"');
    expect(requests[1]?.body).toContain('name="file"');
    expect(await screen.findByText(text.uploaded_other.replace('{{count}}', '2'))).toBeVisible();
  });

  it('Envio recusado pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/documents', () =>
        HttpResponse.json(
          {
            status: 400,
            title: 'Document refused',
            type: 'urn:fiscoprojects:problem:document-refused',
            reason: 'Dangerous',
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openDocuments();

    const input = document.querySelector<HTMLInputElement>('input[type="file"]');
    if (input === null) {
      throw new Error('No input of files.');
    }
    fireEvent.change(input, { target: { files: [new File(['MZ'], 'instalador.exe')] } });

    expect(
      await screen.findByText(text.refused.Dangerous.replace('{{name}}', 'instalador.exe')),
    ).toBeVisible();
  });

  it('Detalhes do documento', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });

    const drawer = await choose('Especificação das regras de ICMS-ST.pdf');

    expect(
      within(drawer).getByRole('heading', { name: 'Especificação das regras de ICMS-ST.pdf' }),
    ).toBeVisible();
    expect(within(drawer).getByText('1,2 MB · versão 3')).toBeVisible();
    expect(within(drawer).getByRole('link', { name: text.details.open })).toHaveAttribute(
      'href',
      `/api/projects/${project.id}/documents/${documents[0]?.id ?? ''}/content`,
    );
    expect(within(drawer).getByRole('link', { name: text.details.download })).toHaveAttribute(
      'href',
      `/api/projects/${project.id}/documents/${documents[0]?.id ?? ''}/content?download=true`,
    );
    expect(within(drawer).getByText(text.details.approvedBy)).toBeVisible();
    expect(within(drawer).getByText('Marina Costa em 14/10/2026')).toBeVisible();
    expect(
      within(drawer).getByRole('link', { name: 'Parametrizar regras de ICMS' }),
    ).toHaveAttribute('href', `/projects/${project.id}/schedule`);
    expect(within(drawer).getByText('03 Requisitos e especificações')).toBeVisible();
    const versions = within(drawer).getByRole('list');
    expect(within(versions).getAllByRole('listitem')).toHaveLength(3);
    expect(
      within(versions).getByRole('link', {
        name: text.details.downloadVersion.replace('{{number}}', '1'),
      }),
    ).toHaveAttribute(
      'href',
      `/api/projects/${project.id}/documents/${documents[0]?.id ?? ''}/content?version=1&download=true`,
    );
  });

  it('Mudar o status pela tela', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    const drawer = await choose('Layout de integração NF-e.xlsx');
    // Neither PDF, nor image, CSV or text: only downloaded.
    expect(within(drawer).queryByRole('link', { name: text.details.open })).not.toBeInTheDocument();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.more }));
    const menu = await screen.findByRole('menu', { name: text.details.more });
    expect(
      within(menu)
        .getAllByRole('menuitemradio')
        .map((item) => item.textContent),
    ).toEqual([
      text.statuses.Draft,
      text.statuses.InReview,
      text.statuses.Approved,
      text.statuses.Obsolete,
    ]);
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: text.statuses.Approved }));

    await waitFor(() => {
      expect(requests).toEqual([
        { method: 'PUT', path: `${documents[2]?.id ?? ''}/status`, body: '{"status":"Approved"}' },
      ]);
    });
  });

  it('Colaborador não aprova pela tela', async () => {
    area = { ...area, permissions: { canUpload: true, canApprove: false, canDelete: false } };
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    const drawer = await choose('Layout de integração NF-e.xlsx');

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.more }));
    const menu = await screen.findByRole('menu', { name: text.details.more });

    expect(
      within(menu).queryByRole('menuitemradio', { name: text.statuses.Approved }),
    ).not.toBeInTheDocument();
    expect(
      within(menu).queryByRole('menuitem', { name: text.menu.remove }),
    ).not.toBeInTheDocument();
    expect(within(menu).getByRole('menuitem', { name: text.menu.link })).toBeVisible();
  });

  it('Vincular pela tela', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    const drawer = await choose('Mapa de processos fiscais.docx');
    fireEvent.click(within(drawer).getByRole('button', { name: text.details.more }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.link }));
    const dialog = await screen.findByRole('dialog', { name: text.link.title });

    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.link.field }));
    fireEvent.click(
      await screen.findByRole('option', { name: /^Parametrizar regras de ICMS · Tarefa$/ }),
    );
    fireEvent.click(within(dialog).getByRole('button', { name: text.link.save }));

    await waitFor(() => {
      expect(requests).toHaveLength(1);
    });
    const schedule = demoSchedule();
    const task = schedule.phases
      .flatMap((phase) => phase.items)
      .find((item) => item.name === 'Parametrizar regras de ICMS');
    expect(requests[0]).toEqual({
      method: 'PUT',
      path: `${documents[3]?.id ?? ''}/link`,
      body: JSON.stringify({ phaseId: null, itemId: task?.id }),
    });
  });

  it('Excluir pela tela', async () => {
    await openDocuments();
    fireEvent.click(screen.getByRole('button', { name: /03 Requisitos/ }));
    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    const map = documents[3]?.id ?? '';
    const drawer = await choose('Mapa de processos fiscais.docx');
    fireEvent.click(within(drawer).getByRole('button', { name: text.details.more }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });

    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(requests).toEqual([{ method: 'DELETE', path: map, body: '' }]);
    });
  });

  it('Leitor só vê os documentos', async () => {
    area = { ...area, permissions: { canUpload: false, canApprove: false, canDelete: false } };
    await openDocuments();

    expect(screen.queryByRole('button', { name: text.upload })).not.toBeInTheDocument();
    const drawer = await choose('Plano do projeto.docx');
    expect(within(drawer).getByRole('link', { name: text.details.download })).toBeVisible();
    expect(
      within(drawer).queryByRole('button', { name: text.details.more }),
    ).not.toBeInTheDocument();
  });

  it('Pasta de termos pela tela', async () => {
    await openDocuments();

    fireEvent.click(screen.getByRole('button', { name: /06 Termos de aceite/ }));

    expect(await screen.findByText(text.termsFolderNote)).toBeVisible();
    expect(screen.queryByRole('button', { name: text.upload })).not.toBeInTheDocument();
  });

  it('Documentos no celular', async () => {
    await openDocuments(phoneWidth);

    expect(screen.queryByRole('navigation', { name: text.folders })).not.toBeInTheDocument();
    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.folderLabel }));
    fireEvent.click(
      await screen.findByRole('option', { name: '03 Requisitos e especificações (3)' }),
    );

    await waitFor(() => {
      expect(names()).toHaveLength(3);
    });
    expect(screen.getByRole('button', { name: text.upload })).toBeVisible();
  });

  it('Falha ao carregar os documentos', async () => {
    server.use(http.get('/api/projects/:id/documents/folders', () => problem(500)));
    await renderApp(path);

    expect(await screen.findByText(text.loadFailed)).toBeVisible();
  });
});
