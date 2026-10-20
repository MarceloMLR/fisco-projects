import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectLink } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoLinks, linkCategoryIds, linksList } from '../test/demoLinks';
import { phoneWidth, renderApp } from '../test/renderApp';
import { demoProjects, problem, server, signedInAs } from '../test/server';
import { initialOf, linkRows, shownAddress } from './links';

const text = ptBR.links;
const requests: { method: string; path: string; body: unknown }[] = [];
const searches: URL[] = [];
let links: ProjectLink[];
let canEdit: boolean;
const writeText = vi.fn<(text: string) => Promise<void>>();

const erp = demoProjects()[0];
if (erp === undefined) {
  throw new Error('No ERP-FIN.');
}
const project = erp;
const path = `/projects/${project.id}/links`;

beforeEach(() => {
  requests.length = 0;
  searches.length = 0;
  links = demoLinks();
  canEdit = true;
  writeText.mockReset();
  writeText.mockResolvedValue();
  Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/links', ({ request }) => {
      const url = new URL(request.url);
      searches.push(url);
      return HttpResponse.json(linksList(links, url, canEdit));
    }),
    http.post('/api/projects/:id/links', async ({ request }) => {
      const body = (await request.json()) as Pick<ProjectLink, 'title' | 'url' | 'categoryId'>;
      requests.push({ method: 'POST', path: '', body });
      links = [
        ...links,
        {
          ...body,
          id: '0199f5a2-7c1e-7d40-9a1b-d00000000099',
          description: null,
          categoryName: 'Sistemas',
          created: { id: 'x', name: 'Marina Costa', at: '2026-10-20T13:00:00Z' },
        },
      ];
      return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-d00000000099' }, { status: 201 });
    }),
    http.put('/api/projects/:id/links/:linkId', async ({ params, request }) => {
      const body = (await request.json()) as Pick<ProjectLink, 'title' | 'description'>;
      requests.push({ method: 'PUT', path: String(params.linkId), body });
      links = links.map((link) => (link.id === params.linkId ? { ...link, ...body } : link));
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/projects/:id/links/:linkId', ({ params }) => {
      requests.push({ method: 'DELETE', path: String(params.linkId), body: null });
      links = links.filter((link) => link.id !== params.linkId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  Reflect.deleteProperty(navigator, 'clipboard');
});

/** The text of the first cell of each line of the grid of the links, the categories among them. */
function lines(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => within(row).queryAllByRole('gridcell')[0]?.textContent ?? '');
}

async function openLinks(width?: number) {
  await renderApp(path, width);
  await screen.findByRole('grid', { name: text.table });
  await waitFor(() => {
    expect(lines().length).toBeGreaterThan(0);
  });
}

const named = (key: string, title: string) => key.replace('{{title}}', title);

describe('Links do projeto', () => {
  it('Linhas e iniciais dos links', () => {
    expect(
      linkRows(demoLinks()).map((row) =>
        row.kind === 'group' ? `${row.name} ${String(row.count)}` : row.link.title,
      ),
    ).toEqual([
      'Ambientes 2',
      'ERP · Homologação',
      'ERP · Produção',
      'Painéis 1',
      'Indicadores de apuração',
      'Repositórios 1',
      'Layouts de integração',
      'Sistemas 1',
      'Emissor de NF-e',
    ]);
    expect(initialOf('ERP · Homologação')).toBe('H');
    expect(initialOf('emissor de NF-e')).toBe('E');
    expect(shownAddress('https://nfe.fornecedor-exemplo.example/painel/')).toBe(
      'nfe.fornecedor-exemplo.example/painel',
    );
  });

  // Spec: links, requirement "Links pela tela".
  it('Links agrupados por categoria pela tela', async () => {
    await openLinks();

    expect(lines()).toEqual([
      'Ambientes2',
      'HERP · Homologação',
      'PERP · Produção',
      'Painéis1',
      'IIndicadores de apuração',
      'Repositórios1',
      'LLayouts de integração',
      'Sistemas1',
      'EEmissor de NF-e',
    ]);
    expect(screen.getByRole('tab', { name: /Links/ })).toHaveAttribute('aria-selected', 'true');
    const grid = screen.getByRole('grid', { name: text.table });
    const address = within(grid).getByRole('link', { name: 'hml-erp.alfadistribuidora.example' });
    expect(address).toHaveAttribute('href', 'https://hml-erp.alfadistribuidora.example');
    expect(address).toHaveAttribute('target', '_blank');
    expect(within(grid).getByText('Usado nos testes com a equipe do cliente.')).toBeVisible();
    expect(within(grid).getAllByText('Rafael Souza')).toHaveLength(2);
    expect(
      within(grid).getByRole('link', { name: named(text.open, 'ERP · Produção') }),
    ).toHaveAttribute('href', 'https://erp.alfadistribuidora.example');
    expect(screen.getByRole('radio', { name: /Todos/ })).toBeChecked();
    expect(screen.getByText(text.total_other.replace('{{count}}', '5'))).toBeVisible();
  });

  // Spec: links, requirement "Links pela tela".
  it('Copiar o endereço pela tela', async () => {
    await openLinks();

    fireEvent.click(screen.getByRole('button', { name: named(text.copy, 'Emissor de NF-e') }));

    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith('https://nfe.fornecedor-exemplo.example/painel');
    });
    expect(await screen.findByText(text.copied)).toBeVisible();
  });

  // Spec: links, requirement "Links pela tela".
  it('Novo link pela tela', async () => {
    await openLinks();

    fireEvent.click(screen.getByRole('button', { name: text.newLink }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.title), {
      target: { value: 'Portal da SEFAZ' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.url), {
      target: { value: 'nfe.sefaz.example' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));
    expect(await within(dialog).findByText(text.errors.urlInvalid)).toBeVisible();
    expect(within(dialog).getByText(text.errors.categoryRequired)).toBeVisible();

    fireEvent.change(within(dialog).getByLabelText(text.dialog.url), {
      target: { value: 'https://nfe.sefaz.example/consulta' },
    });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.category }));
    const listbox = await screen.findByRole('listbox');
    expect(within(listbox).queryByRole('option', { name: 'Painéis' })).not.toBeInTheDocument();
    fireEvent.click(within(listbox).getByRole('option', { name: 'Sistemas' }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    await waitFor(() => {
      expect(lines()).toContain('PPortal da SEFAZ');
    });
    expect(requests).toEqual([
      {
        method: 'POST',
        path: '',
        body: {
          title: 'Portal da SEFAZ',
          url: 'https://nfe.sefaz.example/consulta',
          categoryId: linkCategoryIds.systems,
          description: null,
        },
      },
    ]);
    expect(await screen.findByText(named(text.messages.added, 'Portal da SEFAZ'))).toBeVisible();
  });

  it('Editar link com categoria inativa pela tela', async () => {
    await openLinks();

    fireEvent.click(
      screen.getByRole('button', { name: named(text.menu.button, 'Indicadores de apuração') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.editTitle });
    expect(within(dialog).getByRole('combobox', { name: text.dialog.category })).toHaveTextContent(
      text.dialog.inactive.replace('{{name}}', 'Painéis'),
    );
    fireEvent.change(within(dialog).getByLabelText(text.dialog.description), {
      target: { value: 'Conferência mensal.' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));

    await waitFor(() => {
      expect(requests).toHaveLength(1);
    });
    expect(requests[0]).toEqual({
      method: 'PUT',
      path: links.find((link) => link.title === 'Indicadores de apuração')?.id,
      body: {
        title: 'Indicadores de apuração',
        url: 'https://bi.alfadistribuidora.example/apuracao',
        categoryId: linkCategoryIds.panels,
        description: 'Conferência mensal.',
      },
    });
  });

  it('Excluir link pela tela', async () => {
    await openLinks();

    fireEvent.click(
      screen.getByRole('button', { name: named(text.menu.button, 'Layouts de integração') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    expect(
      within(confirm).getByText(named(text.remove.text, 'Layouts de integração')),
    ).toBeVisible();
    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(lines()).not.toContain('LLayouts de integração');
    });
    expect(lines()).not.toContain('Repositórios1');
    expect(requests.map((request) => request.method)).toEqual(['DELETE']);
  });

  // Spec: links, requirement "Busca nos links".
  it('Buscar e filtrar pela tela', async () => {
    await openLinks();

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'hml' },
    });
    await waitFor(() => {
      expect(lines()).toEqual(['Ambientes1', 'HERP · Homologação']);
    });
    expect(searches.at(-1)?.searchParams.get('search')).toBe('hml');

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: '' },
    });
    fireEvent.click(screen.getByRole('radio', { name: /Repositórios/ }));
    await waitFor(() => {
      expect(lines()).toEqual(['Repositórios1', 'LLayouts de integração']);
    });
    expect(searches.at(-1)?.searchParams.get('categoryId')).toBe(linkCategoryIds.repositories);
  });

  it('Leitor só vê os links', async () => {
    canEdit = false;
    signedInAs('User');
    await openLinks();

    expect(screen.queryByRole('button', { name: text.newLink })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: named(text.menu.button, 'ERP · Produção') }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: named(text.copy, 'ERP · Produção') })).toBeVisible();
  });

  it('Projeto encerrado ao excluir pela tela', async () => {
    server.use(
      http.delete('/api/projects/:id/links/:linkId', () =>
        problem(409, 'urn:fiscoprojects:problem:project-closed'),
      ),
    );
    await openLinks();

    fireEvent.click(
      screen.getByRole('button', { name: named(text.menu.button, 'ERP · Produção') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    fireEvent.click(
      within(await screen.findByRole('alertdialog')).getByRole('button', {
        name: text.remove.confirm,
      }),
    );

    expect(await screen.findByText(text.messages.closed)).toBeVisible();
  });

  it('Links no celular', async () => {
    await openLinks(phoneWidth);

    const grid = screen.getByRole('grid', { name: text.table });
    expect(within(grid).getByText('hml-erp.alfadistribuidora.example')).toBeVisible();
    expect(within(grid).queryByRole('columnheader', { name: text.columns.created })).toBeNull();
    expect(screen.getByRole('combobox', { name: text.category })).toBeVisible();
    expect(screen.getByRole('button', { name: text.newLink })).toBeVisible();
  });
});
