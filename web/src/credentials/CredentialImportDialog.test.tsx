import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Spreadsheet } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { credentialsList, demoCredentials } from '../test/demoCredentials';
import { renderApp } from '../test/renderApp';
import { demoProjects, problem, server, signedInAs } from '../test/server';
import { credentialOf, problemOf, rowHas, suggestMapping } from './credentialImport';

const text = ptBR.credentials;
const importText = text.import;
const imports: unknown[] = [];
const previews: string[] = [];
let permissions: { canEdit: boolean; canDelete: boolean };

const erp = demoProjects()[0];
if (erp === undefined) {
  throw new Error('No ERP-FIN.');
}
const project = erp;
const path = `/projects/${project.id}/credentials`;

/** The spreadsheet of all the clients, as the API reads it: the header and four lines. */
const sheet: Spreadsheet = {
  sheets: ['Acessos'],
  sheet: 0,
  columns: ['Cliente', 'Sistema', 'Login', 'Senha', 'Obs.'],
  rows: [
    {
      number: 2,
      cells: ['Alfa Distribuidora', 'ERP', 'consultoria.leitura', 'Kx7#pa2V!q', 'Somente leitura.'],
    },
    { number: 3, cells: ['Alfa Distribuidora', 'SEFAZ', '12.345.678/0001-90', 'Senha#Sefaz1', ''] },
    { number: 4, cells: ['Beta Alimentos', 'ERP', 'beta.fiscal', 'Senha#Beta1', ''] },
    { number: 5, cells: ['Alfa Distribuidora', 'Banco', '', 'Senha#Banco1', 'Sem usuário ainda.'] },
  ],
};

beforeEach(() => {
  imports.length = 0;
  previews.length = 0;
  permissions = { canEdit: true, canDelete: true };
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/credentials', ({ request }) =>
      HttpResponse.json(credentialsList(demoCredentials(), new URL(request.url), permissions)),
    ),
    http.post('/api/projects/:id/credentials/import/preview', async ({ request }) => {
      previews.push(await request.text());
      return HttpResponse.json(sheet, { headers: { 'Cache-Control': 'no-store' } });
    }),
    http.post('/api/projects/:id/credentials/import', async ({ request }) => {
      imports.push(await request.json());
      return HttpResponse.json({ imported: 2 });
    }),
  );
});

/** The first cells of the lines of the table of the spreadsheet. */
function lines(): string[][] {
  const grid = screen.getByRole('grid', { name: importText.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) =>
      within(row)
        .queryAllByRole('gridcell')
        .map((cell) => cell.textContent),
    );
}

async function openImport() {
  await renderApp(path);
  fireEvent.click(await screen.findByRole('button', { name: importText.open }));
  const dialog = await screen.findByRole('dialog', { name: importText.title });
  fireEvent.change(screen.getByTestId('credential-import-file'), {
    target: { files: [new File(['Cliente;Sistema'], 'acessos.csv', { type: 'text/csv' })] },
  });
  await within(dialog).findByRole('grid', { name: importText.table });
  return dialog;
}

async function choose(dialog: HTMLElement, field: string, option: string) {
  fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: field }));
  fireEvent.click(await screen.findByRole('option', { name: option }));
}

describe('Importação dos acessos', () => {
  it('Mapeamento sugerido pelo cabeçalho', () => {
    expect(suggestMapping(['Cliente', 'Sistema', 'Login', 'Senha', 'Obs.'])).toEqual({
      portal: 1,
      environment: null,
      url: null,
      username: 2,
      password: 3,
      notes: 4,
    });
    expect(
      suggestMapping(['Portal', 'Ambiente', 'Endereço', 'Usuário', 'Senha', 'Observação']),
    ).toEqual({ portal: 0, environment: 1, url: 2, username: 3, password: 4, notes: 5 });
  });

  it('Linha da planilha como acesso', () => {
    const choice = {
      mapping: { portal: 1, environment: null, url: null, username: 2, password: 3, notes: 4 },
      environment: 'Produção',
    };
    const [first, , , missing] = sheet.rows;
    if (first === undefined || missing === undefined) {
      throw new Error('No lines.');
    }
    const values = credentialOf(first, choice);

    expect(values).toEqual({
      portal: 'ERP',
      environment: 'Produção',
      url: '',
      username: 'consultoria.leitura',
      password: 'Kx7#pa2V!q',
      notes: 'Somente leitura.',
    });
    expect(problemOf(values)).toBeUndefined();
    expect(problemOf(credentialOf(missing, choice))).toEqual({
      kind: 'missing',
      field: 'username',
    });
    expect(problemOf({ ...values, url: 'erp.example' })).toEqual({ kind: 'urlInvalid' });
    expect(problemOf({ ...values, portal: 'p'.repeat(121) })).toEqual({
      kind: 'tooLong',
      field: 'portal',
    });
    expect(rowHas(first, 'alfa distribuidora')).toBe(true);
    expect(rowHas(first, 'beta')).toBe(false);
  });

  // Spec: acessos, requirement "Importação pela tela".
  it('Mapear e importar pela tela', async () => {
    const dialog = await openImport();

    expect(previews[0]).toContain('name="file"');
    expect(
      within(dialog).getByRole('combobox', { name: importText.fields.portal }),
    ).toHaveTextContent('Sistema');
    expect(
      within(dialog).getByRole('combobox', { name: importText.fields.username }),
    ).toHaveTextContent('Login');
    await choose(dialog, importText.fields.environment, 'Cliente');
    expect(lines()[0]?.[3]).toBe('Alfa Distribuidora');
    await choose(dialog, importText.fields.environment, importText.sameForAll);
    fireEvent.change(within(dialog).getByLabelText(importText.environmentForAll), {
      target: { value: 'Produção' },
    });

    fireEvent.change(within(dialog).getByRole('searchbox', { name: importText.filter }), {
      target: { value: 'alfa' },
    });
    await waitFor(() => {
      expect(lines()).toHaveLength(3);
    });
    expect(within(dialog).getByRole('button', { name: importText.confirm_zero })).toBeDisabled();
    fireEvent.click(within(dialog).getByRole('checkbox', { name: importText.markAll }));

    const confirm = within(dialog).getByRole('button', {
      name: importText.confirm_other.replace('{{count}}', '2'),
    });
    fireEvent.click(confirm);

    await waitFor(() => {
      expect(imports).toHaveLength(1);
    });
    expect(imports[0]).toEqual({
      items: [
        {
          row: 2,
          portal: 'ERP',
          environment: 'Produção',
          url: null,
          username: 'consultoria.leitura',
          password: 'Kx7#pa2V!q',
          notes: 'Somente leitura.',
        },
        {
          row: 3,
          portal: 'SEFAZ',
          environment: 'Produção',
          url: null,
          username: '12.345.678/0001-90',
          password: 'Senha#Sefaz1',
          notes: null,
        },
      ],
    });
    expect(await screen.findByText(importText.done_other.replace('{{count}}', '2'))).toBeVisible();
    expect(screen.queryByRole('dialog', { name: importText.title })).toBeNull();
  });

  // Spec: acessos, requirement "Importação pela tela".
  it('Ambiente digitado para todas', async () => {
    const dialog = await openImport();

    expect(
      within(dialog).getByRole('combobox', { name: importText.fields.environment }),
    ).toHaveTextContent(importText.chooseColumn);
    expect(lines()[0]?.[8]).toBe(importText.problems.missing.environment);
    await choose(dialog, importText.fields.environment, importText.sameForAll);
    fireEvent.change(within(dialog).getByLabelText(importText.environmentForAll), {
      target: { value: 'Produção' },
    });

    await waitFor(() => {
      expect(lines()[0]?.[3]).toBe('Produção');
    });
    expect(lines()[0]?.[8]).toBe('');
  });

  // Spec: acessos, requirement "Importação pela tela".
  it('Linha incompleta não se marca', async () => {
    const dialog = await openImport();
    await choose(dialog, importText.fields.environment, importText.sameForAll);
    fireEvent.change(within(dialog).getByLabelText(importText.environmentForAll), {
      target: { value: 'Produção' },
    });

    await waitFor(() => {
      expect(lines()[3]?.[8]).toBe(importText.problems.missing.username);
    });
    expect(
      within(dialog).queryByRole('checkbox', {
        name: importText.markLine.replace('{{number}}', '5'),
      }),
    ).toBeNull();
    fireEvent.click(within(dialog).getByRole('checkbox', { name: importText.markAll }));
    expect(
      within(dialog).getByRole('button', {
        name: importText.confirm_other.replace('{{count}}', '3'),
      }),
    ).toBeEnabled();
    expect(within(dialog).queryByText('Kx7#pa2V!q')).toBeNull();
  });

  // Spec: acessos, requirement "Importação pela tela".
  it('Importar só para quem pode', async () => {
    permissions = { canEdit: true, canDelete: false };
    await renderApp(path);
    await screen.findByRole('button', { name: text.newCredential });

    expect(screen.queryByRole('button', { name: importText.open })).toBeNull();
  });

  it('Planilha recusada pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/credentials/import/preview', () =>
        HttpResponse.json(
          {
            status: 400,
            type: 'urn:fiscoprojects:problem:credential-import-refused',
            reason: 'Unreadable',
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await renderApp(path);
    fireEvent.click(await screen.findByRole('button', { name: importText.open }));
    const dialog = await screen.findByRole('dialog', { name: importText.title });
    fireEvent.change(screen.getByTestId('credential-import-file'), {
      target: { files: [new File(['x'], 'acessos.xlsx')] },
    });

    expect(await within(dialog).findByText(importText.refused.Unreadable)).toBeVisible();
  });

  it('Importação com linha inválida pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/credentials/import', () =>
        HttpResponse.json(
          {
            status: 400,
            type: 'urn:fiscoprojects:problem:credential-import-invalid',
            rows: [{ row: 3, fields: ['username'] }],
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    const dialog = await openImport();
    await choose(dialog, importText.fields.environment, importText.sameForAll);
    fireEvent.change(within(dialog).getByLabelText(importText.environmentForAll), {
      target: { value: 'Produção' },
    });
    fireEvent.click(
      within(dialog).getByRole('checkbox', {
        name: importText.markLine.replace('{{number}}', '3'),
      }),
    );
    fireEvent.click(
      within(dialog).getByRole('button', {
        name: importText.confirm_one.replace('{{count}}', '1'),
      }),
    );

    expect(
      await within(dialog).findByText(importText.invalid.replace('{{rows}}', '3')),
    ).toBeVisible();
  });

  it('Projeto encerrado ao importar pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/credentials/import', () =>
        problem(409, 'urn:fiscoprojects:problem:project-closed'),
      ),
    );
    const dialog = await openImport();
    await choose(dialog, importText.fields.environment, importText.sameForAll);
    fireEvent.change(within(dialog).getByLabelText(importText.environmentForAll), {
      target: { value: 'Produção' },
    });
    fireEvent.click(within(dialog).getByRole('checkbox', { name: importText.markAll }));
    fireEvent.click(
      within(dialog).getByRole('button', {
        name: importText.confirm_other.replace('{{count}}', '3'),
      }),
    );

    expect(await within(dialog).findByText(text.messages.closed)).toBeVisible();
  });
});
