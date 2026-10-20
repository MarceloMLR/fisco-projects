import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Vendor } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoVendors, marina, problem, server } from '../test/server';

const text = ptBR.vendors;
const administrator = { ...marina, role: 'Administrator' as const };
let vendors: Vendor[];
const requests: string[] = [];

beforeEach(() => {
  vendors = demoVendors();
  requests.length = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(administrator)),
    http.get('/api/admin/vendors', () => HttpResponse.json(vendors)),
    http.post('/api/admin/vendors', async ({ request }) => {
      const body = (await request.json()) as Pick<Vendor, 'name' | 'portalUrl'>;
      requests.push(`POST ${body.name} ${String(body.portalUrl)}`);
      if (vendors.some((vendor) => vendor.name.toLowerCase() === body.name.toLowerCase())) {
        return problem(409, 'urn:fiscoprojects:problem:vendor-name-in-use');
      }
      const id = '0199f5a2-7c1e-7d40-9a1b-900000000009';
      vendors = [...vendors, { id, ...body, isActive: true }].sort((a, b) =>
        a.name.localeCompare(b.name),
      );
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/admin/vendors/:id', async ({ params, request }) => {
      const body = (await request.json()) as Pick<Vendor, 'name' | 'portalUrl' | 'isActive'>;
      requests.push(`PUT ${body.name} ${String(body.isActive)}`);
      vendors = vendors.map((vendor) =>
        vendor.id === params.id ? { ...vendor, ...body } : vendor,
      );
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function rows() {
  return within(screen.getByRole('grid', { name: text.table }))
    .getAllByRole('row')
    .slice(1)
    .map((row) => row.textContent);
}

async function addVendor(name: string, portal: string) {
  fireEvent.click(screen.getByRole('button', { name: text.newVendor }));
  const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
  fireEvent.change(within(dialog).getByLabelText(text.dialog.name), { target: { value: name } });
  fireEvent.change(within(dialog).getByLabelText(/Portal de suporte/), {
    target: { value: portal },
  });
  fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));
  return dialog;
}

// Spec: chamados, requirements "Fornecedores" and "Fornecedores pela tela".
describe('Fornecedores na Administração', () => {
  it('Fornecedores na Administração', async () => {
    await renderApp('/admin/vendors');
    await waitFor(() => {
      expect(rows()).toHaveLength(3);
    });
    expect(screen.getByRole('link', { name: /Fornecedores/ })).toHaveAttribute(
      'aria-current',
      'page',
    );

    await addVendor('Portal Fiscal Exemplo', 'https://ajuda.portalfiscal.example');

    await waitFor(() => {
      expect(rows()).toHaveLength(4);
    });
    expect(requests).toEqual(['POST Portal Fiscal Exemplo https://ajuda.portalfiscal.example']);
    expect(rows()[3]).toContain('Portal Fiscal Exemplohttps://ajuda.portalfiscal.exampleAtivo');
    expect(
      await screen.findByText('Fornecedor Portal Fiscal Exemplo incluído.'),
    ).toBeInTheDocument();
  });

  it('Nome repetido', async () => {
    await renderApp('/admin/vendors');
    await waitFor(() => {
      expect(rows()).toHaveLength(3);
    });

    const dialog = await addVendor('emissor exemplo', '');

    expect(await within(dialog).findByText(text.errors.nameInUse)).toBeInTheDocument();
    expect(requests).toEqual(['POST emissor exemplo null']);
    expect(vendors).toHaveLength(3);
  });

  it('Portal inválido', async () => {
    await renderApp('/admin/vendors');
    await waitFor(() => {
      expect(rows()).toHaveLength(3);
    });

    const dialog = await addVendor('Portal Fiscal Exemplo', 'ajuda.portalfiscal.example');

    expect(await within(dialog).findByText(text.errors.portalInvalid)).toBeInTheDocument();
    expect(requests).toHaveLength(0);
  });

  it('Fornecedor inativo', async () => {
    await renderApp('/admin/vendors');
    await waitFor(() => {
      expect(rows()).toHaveLength(3);
    });

    fireEvent.click(
      screen.getByRole('button', { name: text.menu.button.replace('{{name}}', 'Emissor Exemplo') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.deactivate }));

    await waitFor(() => {
      expect(requests).toEqual(['PUT Emissor Exemplo false']);
    });
    expect(await screen.findByText('Fornecedor Emissor Exemplo inativado.')).toBeInTheDocument();
    await waitFor(() => {
      expect(rows()[1]).toContain('Emissor Exemplohttps://suporte.emissor.exampleInativo');
    });
  });
});
