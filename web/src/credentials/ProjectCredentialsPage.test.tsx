import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Credential } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { credentialsList, demoCredentials, demoPasswordOf } from '../test/demoCredentials';
import { expectBusy, heldAnswer, untilIdle } from '../test/busy';
import { phoneWidth, renderApp } from '../test/renderApp';
import { demoProjects, problem, server, signedInAs } from '../test/server';
import { credentialRows } from './credentials';

const text = ptBR.credentials;
const requests: { method: string; path: string; body: unknown }[] = [];
const passwordAsks: string[] = [];
const searches: URL[] = [];
let credentials: Credential[];
let permissions: { canEdit: boolean; canDelete: boolean };
const writeText = vi.fn<(text: string) => Promise<void>>();

const erp = demoProjects()[0];
if (erp === undefined) {
  throw new Error('No ERP-FIN.');
}
const project = erp;
const path = `/projects/${project.id}/credentials`;
const erpProduction = '0199f5a2-7c1e-7d40-9a1b-e00000000003';

beforeEach(() => {
  requests.length = 0;
  passwordAsks.length = 0;
  searches.length = 0;
  credentials = demoCredentials();
  permissions = { canEdit: true, canDelete: true };
  writeText.mockReset();
  writeText.mockResolvedValue();
  Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/credentials', ({ request }) => {
      const url = new URL(request.url);
      searches.push(url);
      return HttpResponse.json(credentialsList(credentials, url, permissions));
    }),
    http.get('/api/projects/:id/credentials/:credentialId/password', ({ params }) => {
      passwordAsks.push(String(params.credentialId));
      return HttpResponse.json(
        { password: demoPasswordOf(String(params.credentialId)) },
        { headers: { 'Cache-Control': 'no-store' } },
      );
    }),
    http.post('/api/projects/:id/credentials', async ({ request }) => {
      const body = (await request.json()) as Omit<Credential, 'id'> & { password: string };
      requests.push({ method: 'POST', path: '', body });
      credentials = [
        ...credentials,
        {
          id: '0199f5a2-7c1e-7d40-9a1b-e00000000099',
          portal: body.portal,
          environment: body.environment,
          url: body.url,
          username: body.username,
          notes: body.notes,
        },
      ].sort((first, second) => first.environment.localeCompare(second.environment));
      return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-e00000000099' }, { status: 201 });
    }),
    http.put('/api/projects/:id/credentials/:credentialId', async ({ params, request }) => {
      const body = (await request.json()) as Partial<Credential>;
      requests.push({ method: 'PUT', path: String(params.credentialId), body });
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/projects/:id/credentials/:credentialId', ({ params }) => {
      requests.push({ method: 'DELETE', path: String(params.credentialId), body: null });
      credentials = credentials.filter((credential) => credential.id !== params.credentialId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  Reflect.deleteProperty(navigator, 'clipboard');
});

/** The text of the first cell of each line of the grid of the credentials, the environments among them. */
function lines(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => within(row).queryAllByRole('gridcell')[0]?.textContent ?? '');
}

/** The password shown in the line of the credential, or the mask. */
function passwordIn(portalLine: number): string {
  return screen.getAllByTestId('credential-password')[portalLine]?.textContent ?? '';
}

async function openCredentials(width?: number) {
  await renderApp(path, width);
  await screen.findByRole('grid', { name: text.table });
  await waitFor(() => {
    expect(lines().length).toBeGreaterThan(0);
  });
}

const named = (key: string, portal: string) => key.replace('{{portal}}', portal);

describe('Acessos do projeto', () => {
  it('Linhas dos acessos por ambiente', () => {
    expect(
      credentialRows(demoCredentials()).map((row) =>
        row.kind === 'group' ? `${row.name} ${String(row.count)}` : row.credential.portal,
      ),
    ).toEqual([
      'Fornecedores 1',
      'Portal de suporte',
      'Homologação 1',
      'ERP',
      'Produção 2',
      'ERP',
      'SEFAZ',
    ]);
  });

  // Spec: acessos, requirement "Senha mascarada".
  it('Senha mascarada por padrão', async () => {
    await openCredentials();

    expect(lines()).toEqual([
      'Fornecedores1',
      'Portal de suporte',
      'Homologação1',
      'ERP',
      'Produção2',
      'ERP',
      'SEFAZ',
    ]);
    expect(screen.getAllByTestId('credential-password').map((cell) => cell.textContent)).toEqual(
      Array(4).fill(text.mask),
    );
    expect(passwordAsks).toEqual([]);
    const grid = screen.getByRole('grid', { name: text.table });
    expect(within(grid).getByText('consultoria.leitura')).toBeVisible();
    expect(
      within(grid).getByRole('link', { name: 'hml-erp.alfadistribuidora.example' }),
    ).toHaveAttribute('target', '_blank');
    expect(screen.getByRole('tab', { name: /Acessos/ })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByText(text.total_other.replace('{{count}}', '4'))).toBeVisible();
  });

  // Spec: acessos, requirement "Senha mascarada".
  it('Mostrar a senha', async () => {
    await openCredentials();

    const [, production] = screen.getAllByRole('button', { name: named(text.show, 'ERP') });
    if (production === undefined) {
      throw new Error('No ERP in Produção.');
    }
    fireEvent.click(production);

    await waitFor(() => {
      expect(passwordIn(2)).toBe(demoPasswordOf(erpProduction));
    });
    expect(passwordIn(1)).toBe(text.mask);
    expect(passwordAsks).toEqual([erpProduction]);

    await untilIdle(screen.getByRole('button', { name: named(text.hide, 'ERP') }));
    fireEvent.click(screen.getByRole('button', { name: named(text.hide, 'ERP') }));
    await waitFor(() => {
      expect(passwordIn(2)).toBe(text.mask);
    });
  });

  // Spec: acessos, requirement "Acessos pela tela".
  it('Copiar a senha pela tela', async () => {
    await openCredentials();

    fireEvent.click(screen.getByRole('button', { name: named(text.copyPassword, 'SEFAZ') }));

    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(
        demoPasswordOf('0199f5a2-7c1e-7d40-9a1b-e00000000004'),
      );
    });
    expect(await screen.findByText(text.passwordCopied)).toBeVisible();
    expect(screen.getAllByTestId('credential-password')[3]?.textContent).toBe(text.mask);
  });

  it('Copiar o usuário pela tela', async () => {
    await openCredentials();

    fireEvent.click(
      screen.getByRole('button', { name: named(text.copyUsername, 'Portal de suporte') }),
    );

    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith('marina.costa');
    });
    expect(passwordAsks).toEqual([]);
  });

  // Spec: acessos, requirement "Acessos pela tela".
  it('Novo acesso pela tela', async () => {
    await openCredentials();

    fireEvent.click(screen.getByRole('button', { name: text.newCredential }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));
    expect(await within(dialog).findByText(text.errors.portalRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.passwordRequired)).toBeVisible();

    fireEvent.change(within(dialog).getByLabelText(text.dialog.portal), {
      target: { value: 'Emissor de NF-e' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.environment), {
      target: { value: 'Homologação' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.username), {
      target: { value: 'alfa-hml' },
    });
    const password = within(dialog).getByLabelText(text.dialog.password);
    expect(password).toHaveAttribute('type', 'password');
    fireEvent.change(password, { target: { value: ' Kx7 pa2V ' } });
    fireEvent.click(within(dialog).getByRole('checkbox', { name: text.dialog.showPassword }));
    expect(password).toHaveAttribute('type', 'text');
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    await waitFor(() => {
      expect(lines()).toContain('Emissor de NF-e');
    });
    expect(requests).toEqual([
      {
        method: 'POST',
        path: '',
        body: {
          portal: 'Emissor de NF-e',
          environment: 'Homologação',
          url: null,
          username: 'alfa-hml',
          password: ' Kx7 pa2V ',
          notes: null,
        },
      },
    ]);
    expect(lines().indexOf('Emissor de NF-e')).toBeGreaterThan(lines().indexOf('Homologação2'));
    expect(
      screen.getAllByTestId('credential-password').every((cell) => cell.textContent === text.mask),
    ).toBe(true);
  });

  it('Editar sem trocar a senha pela tela', async () => {
    await openCredentials();

    fireEvent.click(screen.getByRole('button', { name: named(text.menu.button, 'SEFAZ') }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.editTitle });
    expect(within(dialog).getByLabelText(text.dialog.password)).toHaveValue('');
    expect(within(dialog).getByText(text.dialog.passwordKeep)).toBeVisible();
    fireEvent.change(within(dialog).getByLabelText(text.dialog.notes), {
      target: { value: 'Certificado A1 no cofre.' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));

    await waitFor(() => {
      expect(requests).toHaveLength(1);
    });
    expect(requests[0]).toEqual({
      method: 'PUT',
      path: '0199f5a2-7c1e-7d40-9a1b-e00000000004',
      body: {
        portal: 'SEFAZ',
        environment: 'Produção',
        url: null,
        username: '12.345.678/0001-90',
        password: null,
        notes: 'Certificado A1 no cofre.',
      },
    });
  });

  it('Excluir acesso pela tela', async () => {
    await openCredentials();

    fireEvent.click(
      screen.getByRole('button', { name: named(text.menu.button, 'Portal de suporte') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(lines()).not.toContain('Portal de suporte');
    });
    expect(lines()).not.toContain('Fornecedores1');
  });

  // Spec: acessos, requirement "Busca nos acessos".
  it('Buscar e filtrar os acessos pela tela', async () => {
    await openCredentials();

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'consultoria' },
    });
    await waitFor(() => {
      expect(lines()).toEqual(['Homologação1', 'ERP', 'Produção1', 'ERP']);
    });
    expect(searches.at(-1)?.searchParams.get('search')).toBe('consultoria');

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: '' },
    });
    fireEvent.click(screen.getByRole('radio', { name: /Produção/ }));
    await waitFor(() => {
      expect(lines()).toEqual(['Produção2', 'ERP', 'SEFAZ']);
    });
    expect(searches.at(-1)?.searchParams.get('environment')).toBe('Produção');
  });

  it('Colaborador inclui e não exclui', async () => {
    permissions = { canEdit: true, canDelete: false };
    await openCredentials();

    fireEvent.click(screen.getByRole('button', { name: named(text.menu.button, 'SEFAZ') }));
    const menu = await screen.findByRole('menu');
    expect(within(menu).getByRole('menuitem', { name: text.menu.edit })).toBeVisible();
    expect(within(menu).queryByRole('menuitem', { name: text.menu.remove })).toBeNull();
  });

  it('Leitor vê e mostra a senha', async () => {
    permissions = { canEdit: false, canDelete: false };
    await openCredentials();

    expect(screen.queryByRole('button', { name: text.newCredential })).toBeNull();
    expect(screen.queryByRole('button', { name: named(text.menu.button, 'SEFAZ') })).toBeNull();
    fireEvent.click(screen.getByRole('button', { name: named(text.show, 'SEFAZ') }));
    await waitFor(() => {
      expect(passwordIn(3)).toBe(demoPasswordOf('0199f5a2-7c1e-7d40-9a1b-e00000000004'));
    });
  });

  it('Projeto encerrado ao incluir pela tela', async () => {
    server.use(
      http.post('/api/projects/:id/credentials', () =>
        problem(409, 'urn:fiscoprojects:problem:project-closed'),
      ),
    );
    await openCredentials();

    fireEvent.click(screen.getByRole('button', { name: text.newCredential }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.portal), {
      target: { value: 'ERP' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.environment), {
      target: { value: 'Produção' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.username), {
      target: { value: 'usuario' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.password), {
      target: { value: 'senha' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    expect(await within(dialog).findByText(text.messages.closed)).toBeVisible();
  });

  it('Acessos no celular', async () => {
    await openCredentials(phoneWidth);

    const grid = screen.getByRole('grid', { name: text.table });
    expect(within(grid).getByText('consultoria.leitura')).toBeVisible();
    expect(within(grid).queryByRole('columnheader', { name: text.columns.notes })).toBeNull();
    expect(screen.getAllByTestId('credential-password')).toHaveLength(4);
    expect(screen.getByRole('combobox', { name: text.environment })).toBeVisible();
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
describe('Botões em andamento nos acessos', () => {
  it('Botão de ícone em andamento ao mostrar a senha', async () => {
    const held = heldAnswer();
    // Held first, then the password of the simulated API.
    server.use(
      http.get('/api/projects/:id/credentials/:credentialId/password', async () => {
        await held.until;
      }),
    );
    await openCredentials();
    const [, production] = screen.getAllByRole('button', { name: named(text.show, 'ERP') });
    if (production === undefined) {
      throw new Error('No ERP in Produção.');
    }

    fireEvent.click(production);

    await waitFor(() => {
      expectBusy(production);
    });
    fireEvent.click(production);
    held.release();
    await waitFor(() => {
      expect(passwordIn(2)).toBe(demoPasswordOf(erpProduction));
    });
    expect(passwordAsks).toEqual([erpProduction]);
  });

  it('Formulário em andamento ao incluir o acesso', async () => {
    const held = heldAnswer();
    server.use(
      http.post('/api/projects/:id/credentials', async () => {
        await held.until;
      }),
    );
    await openCredentials();
    fireEvent.click(screen.getByRole('button', { name: text.newCredential }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.portal), {
      target: { value: 'Emissor de NF-e' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.environment), {
      target: { value: 'Homologação' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.username), {
      target: { value: 'alfa-hml' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.password), {
      target: { value: 'Kx7pa2V' },
    });
    const add = within(dialog).getByRole('button', { name: text.dialog.add });

    fireEvent.click(add);

    await waitFor(() => {
      expectBusy(add, text.dialog.add);
    });
    fireEvent.click(add);
    held.release();
    await waitFor(() => {
      expect(lines()).toContain('Emissor de NF-e');
    });
    expect(requests.map((request) => request.method)).toEqual(['POST']);
  });
});
