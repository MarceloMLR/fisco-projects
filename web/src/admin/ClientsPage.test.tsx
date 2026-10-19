import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type ClientRow, clientProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { clientsPage, demoClients, problem, server, signedInAs } from '../test/server';

const text = ptBR.clients;

interface ClientBody {
  name: string;
  cnpj: string | null;
  observation: string | null;
}

/** The clients of this test, changed by the simulated API as the real one would. */
let clients: ClientRow[];
const requests: string[] = [];

beforeEach(() => {
  clients = demoClients();
  requests.length = 0;
  // The handlers keep the clients of this test, so a late request of another test never changes them.
  const current = clients;
  const find = (id: unknown) => current.find((client) => client.id === id);
  const owner = (body: ClientBody, except?: unknown) =>
    current.find(
      (client) => body.cnpj !== null && client.cnpj === body.cnpj && client.id !== except,
    );
  const cnpjInUse = (name: string) =>
    HttpResponse.json(
      { status: 409, title: 'Conflict', type: clientProblems.cnpjInUse, clientName: name },
      { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
    );
  signedInAs('Administrator');
  server.use(
    http.get('/api/clients', ({ request }) =>
      HttpResponse.json(clientsPage(current, new URL(request.url))),
    ),
    http.post('/api/clients', async ({ request }) => {
      const body = (await request.json()) as ClientBody;
      requests.push(`POST ${body.name} ${String(body.cnpj)} ${String(body.observation)}`);
      const other = owner(body);
      if (other !== undefined) {
        return cnpjInUse(other.name);
      }
      const id = '0199f5a2-7c1e-7d40-9a1b-300000000009';
      current.push({ id, ...body, logoUrl: null, status: 'Active' });
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/clients/:id', async ({ request, params }) => {
      const body = (await request.json()) as ClientBody;
      requests.push(`PUT ${body.name} ${String(body.cnpj)} ${String(body.observation)}`);
      const client = find(params.id);
      if (client === undefined) {
        return problem(404);
      }
      const other = owner(body, params.id);
      if (other !== undefined) {
        return cnpjInUse(other.name);
      }
      Object.assign(client, body);
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/clients/:id/deactivate', ({ params }) => {
      requests.push(`POST deactivate ${String(params.id)}`);
      const client = find(params.id);
      if (client !== undefined) {
        client.status = 'Inactive';
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/clients/:id/reactivate', ({ params }) => {
      requests.push(`POST reactivate ${String(params.id)}`);
      const client = find(params.id);
      if (client !== undefined) {
        client.status = 'Active';
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/clients/:id', ({ params }) => {
      requests.push(`DELETE ${String(params.id)}`);
      const index = current.findIndex((client) => client.id === params.id);
      current.splice(index, 1);
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/clients/:id/logo', ({ request, params }) => {
      requests.push(`PUT logo ${request.headers.get('content-type')?.split(';')[0] ?? ''}`);
      const client = find(params.id);
      if (client !== undefined) {
        client.logoUrl = `/api/clients/${String(params.id)}/logo?v=2`;
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/clients/:id/logo', ({ params }) => {
      requests.push(`DELETE logo ${String(params.id)}`);
      const client = find(params.id);
      if (client !== undefined) {
        client.logoUrl = null;
      }
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

function bodyRows() {
  return within(table()).getAllByRole('row').slice(1);
}

/** The names of the clients in the table, in the order shown. */
function names() {
  return bodyRows().map((row) => within(row).getAllByRole('gridcell')[0]?.textContent ?? '');
}

function rowOf(name: string) {
  const row = bodyRows().find((candidate) => candidate.textContent.includes(name));
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

function cells(name: string) {
  return within(rowOf(name))
    .getAllByRole('gridcell')
    .slice(1, 4)
    .map((cell) => cell.textContent);
}

async function openClients(width?: number) {
  const view = await renderApp('/admin/clients', width);
  await waitFor(() => {
    expect(bodyRows()).toHaveLength(clients.length);
  });
  return view;
}

async function openMenuOf(name: string) {
  const label = text.menu.button.replace('{{name}}', name);
  fireEvent.click(within(rowOf(name)).getByRole('button', { name: label }));
  return screen.findByRole('menu', { name: label });
}

const png = () =>
  new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])], 'kappa.png', {
    type: 'image/png',
  });

function chooseLogo(file: File) {
  fireEvent.change(screen.getByTestId('client-logo-file'), { target: { files: [file] } });
}

function field(dialog: HTMLElement, label: string) {
  return within(dialog).getByLabelText(label);
}

const cnpjLabel = `${text.cnpj} ${text.optional}`;
const observationLabel = `${text.observation} ${text.optional}`;

// Spec: clientes, requirement "Tela Clientes".
describe('Tela Clientes', () => {
  it('Clientes na Administração', async () => {
    await renderApp('/admin/users');
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const link = within(sections).getByRole('link', { name: new RegExp(ptBR.pages.clients.title) });
    await waitFor(() => {
      expect(link).toHaveTextContent(`${ptBR.pages.clients.title}5`);
    });

    fireEvent.click(link);

    expect(
      await screen.findByRole('heading', { level: 1, name: `${ptBR.pages.clients.title}5` }),
    ).toBeVisible();
    expect(link).toHaveAttribute('aria-current', 'page');
    await waitFor(() => {
      expect(names()).toEqual([
        'Alfa Distribuidora',
        'BABeta Alimentos',
        'ÉLÉpsilon Logística',
        'GTGama Têxtil',
        'KEKappa Energia',
      ]);
    });
    expect(
      within(rowOf('Alfa Distribuidora')).getByTestId('logo-tile').querySelector('img'),
    ).toHaveAttribute('src', '/api/clients/0199f5a2-7c1e-7d40-9a1b-300000000001/logo?v=1');
    expect(cells('Alfa Distribuidora')).toEqual([
      '11.222.333/0001-81',
      text.statuses.Active,
      'Matriz em Campinas',
    ]);
    expect(cells('Gama Têxtil')).toEqual(['12.ABC.345/01DE-35', text.statuses.Active, '—']);
    expect(cells('Épsilon Logística')).toEqual([
      '—',
      text.statuses.Inactive,
      'Contrato encerrado em 2025',
    ]);
    expect(rowOf('Épsilon Logística')).toHaveClass('row-muted');
    expect(rowOf('Alfa Distribuidora')).not.toHaveClass('row-muted');
    expect(await screen.findByText('5 clientes · 4 ativos')).toBeInTheDocument();
  });

  it('Buscar cliente', async () => {
    await openClients();
    const search = screen.getByRole('searchbox', { name: text.search });

    fireEvent.change(search, { target: { value: 'ALFA' } });
    await waitFor(() => {
      expect(names()).toEqual(['Alfa Distribuidora']);
    });

    fireEvent.change(search, { target: { value: 'epsilon' } });
    await waitFor(() => {
      expect(names()).toEqual(['ÉLÉpsilon Logística']);
    });

    fireEvent.change(search, { target: { value: '222.333/0001' } });
    await waitFor(() => {
      expect(names()).toEqual(['Alfa Distribuidora']);
    });
    expect(await screen.findByText('1 cliente · 1 ativo')).toBeInTheDocument();

    fireEvent.change(search, { target: { value: '12abc345' } });
    await waitFor(() => {
      expect(names()).toEqual(['GTGama Têxtil']);
    });
  });

  it('Filtrar por situação', async () => {
    await openClients();
    const status = screen.getByRole('radiogroup', { name: text.status });

    fireEvent.click(within(status).getByRole('radio', { name: text.filters.Inactive }));
    await waitFor(() => {
      expect(names()).toEqual(['ÉLÉpsilon Logística']);
    });
    expect(await screen.findByText('1 cliente · 0 ativos')).toBeInTheDocument();

    fireEvent.click(within(status).getByRole('radio', { name: text.filters.Active }));
    await waitFor(() => {
      expect(names()).toHaveLength(4);
    });
    expect(await screen.findByText('4 clientes · 4 ativos')).toBeInTheDocument();
  });

  it('Inativar e reativar cliente pela tela', async () => {
    await openClients();

    fireEvent.click(
      within(await openMenuOf('Beta Alimentos')).getByRole('menuitem', {
        name: text.menu.deactivate,
      }),
    );
    expect(
      await screen.findByText(text.messages.deactivated.replace('{{name}}', 'Beta Alimentos')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(cells('Beta Alimentos')[1]).toBe(text.statuses.Inactive);
    });
    expect(rowOf('Beta Alimentos')).toHaveClass('row-muted');

    fireEvent.click(
      within(await openMenuOf('Beta Alimentos')).getByRole('menuitem', {
        name: text.menu.reactivate,
      }),
    );
    await waitFor(() => {
      expect(cells('Beta Alimentos')[1]).toBe(text.statuses.Active);
    });
    expect(requests).toEqual([
      'POST deactivate 0199f5a2-7c1e-7d40-9a1b-300000000002',
      'POST reactivate 0199f5a2-7c1e-7d40-9a1b-300000000002',
    ]);
  });

  it('Excluir cliente pela tela', async () => {
    await openClients();

    fireEvent.click(
      within(await openMenuOf('Gama Têxtil')).getByRole('menuitem', { name: text.menu.remove }),
    );
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    expect(confirm).toHaveAccessibleDescription(
      text.remove.text.replace('{{name}}', 'Gama Têxtil'),
    );
    fireEvent.click(within(confirm).getByRole('button', { name: text.cancel }));
    await waitFor(() => {
      expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    });
    expect(requests).toEqual([]);

    fireEvent.click(
      within(await openMenuOf('Gama Têxtil')).getByRole('menuitem', { name: text.menu.remove }),
    );
    fireEvent.click(
      within(await screen.findByRole('alertdialog')).getByRole('button', {
        name: text.remove.confirm,
      }),
    );

    expect(
      await screen.findByText(text.messages.removed.replace('{{name}}', 'Gama Têxtil')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(names()).not.toContain('GTGama Têxtil');
    });
    expect(requests).toEqual(['DELETE 0199f5a2-7c1e-7d40-9a1b-300000000004']);
  });

  it('Clientes no celular', async () => {
    await openClients(phoneWidth);

    expect(
      screen.queryByRole('navigation', { name: ptBR.admin.sections.label }),
    ).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: text.newClient }));
    expect(await screen.findByRole('dialog', { name: text.newClient })).toBeVisible();
  });
});

// Spec: clientes, requirements "Tela Clientes", "Dados do cliente", "CNPJ do cliente" and "Logo do cliente" (screen).
describe('Diálogo do cliente', () => {
  it('Novo cliente pela tela', async () => {
    await openClients();

    fireEvent.click(screen.getByRole('button', { name: text.newClient }));
    const dialog = await screen.findByRole('dialog', { name: text.newClient });
    fireEvent.change(field(dialog, text.name), { target: { value: ' Lambda Química ' } });
    fireEvent.change(field(dialog, cnpjLabel), { target: { value: '12abc34501de35' } });
    expect(field(dialog, cnpjLabel)).toHaveValue('12.ABC.345/01DE-35');
    fireEvent.change(field(dialog, observationLabel), { target: { value: 'Matriz em Sorocaba' } });
    chooseLogo(png());
    expect(await within(dialog).findByRole('button', { name: text.replaceLogo })).toBeVisible();

    // The CNPJ is Gama's: the field says whose it is.
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));
    expect(
      await within(dialog).findByText(text.errors.cnpjInUse.replace('{{name}}', 'Gama Têxtil')),
    ).toBeVisible();
    expect(field(dialog, cnpjLabel)).toHaveAttribute('aria-invalid', 'true');

    fireEvent.change(field(dialog, cnpjLabel), { target: { value: '77.555.888/0001-53' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));

    expect(
      await screen.findByText(text.messages.included.replace('{{name}}', 'Lambda Química')),
    ).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await waitFor(() => {
      expect(names()).toContain('Lambda Química');
    });
    expect(
      within(rowOf('Lambda Química')).getByTestId('logo-tile').querySelector('img'),
    ).toHaveAttribute('src', '/api/clients/0199f5a2-7c1e-7d40-9a1b-300000000009/logo?v=2');
    expect(requests).toEqual([
      'POST Lambda Química 12ABC34501DE35 Matriz em Sorocaba',
      'POST Lambda Química 77555888000153 Matriz em Sorocaba',
      'PUT logo multipart/form-data',
    ]);
  });

  it('Editar cliente pela tela', async () => {
    await openClients();

    fireEvent.click(
      within(await openMenuOf('Beta Alimentos')).getByRole('menuitem', { name: text.menu.edit }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.editClient });
    expect(field(dialog, text.name)).toHaveValue('Beta Alimentos');
    expect(field(dialog, cnpjLabel)).toHaveValue('22.333.444/0001-81');
    fireEvent.change(field(dialog, observationLabel), { target: { value: 'Filiais SP, RS e PR' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.save }));

    expect(
      await screen.findByText(text.messages.changed.replace('{{name}}', 'Beta Alimentos')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(cells('Beta Alimentos')[2]).toBe('Filiais SP, RS e PR');
    });
    expect(requests).toEqual(['PUT Beta Alimentos 22333444000181 Filiais SP, RS e PR']);
  });

  it('Remover o logo do cliente pela tela', async () => {
    await openClients();

    fireEvent.click(
      within(await openMenuOf('Alfa Distribuidora')).getByRole('menuitem', {
        name: text.menu.edit,
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.editClient });
    fireEvent.click(within(dialog).getByRole('button', { name: text.removeLogo }));
    expect(within(dialog).getByRole('button', { name: text.sendLogo })).toBeVisible();
    fireEvent.click(within(dialog).getByRole('button', { name: text.save }));

    await waitFor(() => {
      expect(names()[0]).toBe('ADAlfa Distribuidora');
    });
    expect(requests).toEqual([
      'PUT Alfa Distribuidora 11222333000181 Matriz em Campinas',
      'DELETE logo 0199f5a2-7c1e-7d40-9a1b-300000000001',
    ]);
  });

  it('Cliente inválido pela tela', async () => {
    await openClients();

    fireEvent.click(screen.getByRole('button', { name: text.newClient }));
    const dialog = await screen.findByRole('dialog', { name: text.newClient });
    fireEvent.change(field(dialog, cnpjLabel), { target: { value: '11.222.333/0001-80' } });
    fireEvent.change(field(dialog, observationLabel), { target: { value: 'a'.repeat(501) } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));

    expect(await within(dialog).findByText(text.errors.nameRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.cnpjInvalid)).toBeVisible();
    expect(within(dialog).getByText(text.errors.observationTooLong)).toBeVisible();
    expect(requests).toEqual([]);
  });

  it('Logo do cliente recusado pela tela', async () => {
    await openClients();

    fireEvent.click(screen.getByRole('button', { name: text.newClient }));
    const dialog = await screen.findByRole('dialog', { name: text.newClient });
    expect(within(dialog).getByText(text.logoHint)).toBeVisible();

    chooseLogo(new File(['GIF89a'], 'kappa.gif', { type: 'image/gif' }));
    expect(await within(dialog).findByRole('alert')).toHaveTextContent(text.errors.logoFormat);

    chooseLogo(new File([new Uint8Array(1024 * 1024 + 1)], 'grande.png', { type: 'image/png' }));
    await waitFor(() => {
      expect(within(dialog).getByRole('alert')).toHaveTextContent(text.errors.logoTooLarge);
    });
    expect(within(dialog).getByRole('button', { name: text.sendLogo })).toBeVisible();
  });

  it('Logo recusado pela API depois de incluir o cliente', async () => {
    server.use(
      http.put('/api/clients/:id/logo', () => {
        requests.push('PUT logo refused');
        return problem(400, clientProblems.logoFormat);
      }),
    );
    await openClients();

    fireEvent.click(screen.getByRole('button', { name: text.newClient }));
    const dialog = await screen.findByRole('dialog', { name: text.newClient });
    fireEvent.change(field(dialog, text.name), { target: { value: 'Lambda Química' } });
    chooseLogo(png());
    await within(dialog).findByRole('button', { name: text.replaceLogo });
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(text.errors.logoFormat);
    // The client is saved: saving again changes it, and does not include another.
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));
    await waitFor(() => {
      expect(requests).toHaveLength(4);
    });
    expect(requests).toEqual([
      'POST Lambda Química null null',
      'PUT logo refused',
      'PUT Lambda Química null null',
      'PUT logo refused',
    ]);
  });
});
