import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { LinkCategory } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoLinkCategories } from '../test/demoLinks';
import { renderApp } from '../test/renderApp';
import { marina, problem, server } from '../test/server';

const text = ptBR.linkCategories;
const administrator = { ...marina, role: 'Administrator' as const };
let categories: LinkCategory[];
const requests: string[] = [];

beforeEach(() => {
  categories = demoLinkCategories();
  requests.length = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(administrator)),
    http.get('/api/admin/link-categories', () => HttpResponse.json(categories)),
    http.post('/api/admin/link-categories', async ({ request }) => {
      const body = (await request.json()) as Pick<LinkCategory, 'name'>;
      requests.push(`POST ${body.name}`);
      if (categories.some((category) => category.name.toLowerCase() === body.name.toLowerCase())) {
        return problem(409, 'urn:fiscoprojects:problem:link-category-name-in-use');
      }
      const id = '0199f5a2-7c1e-7d40-9a1b-c00000000009';
      categories = [...categories, { id, name: body.name, isActive: true }].sort((a, b) =>
        a.name.localeCompare(b.name),
      );
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/admin/link-categories/:id', async ({ params, request }) => {
      const body = (await request.json()) as Pick<LinkCategory, 'name' | 'isActive'>;
      requests.push(`PUT ${body.name} ${String(body.isActive)}`);
      categories = categories.map((category) =>
        category.id === params.id ? { ...category, ...body } : category,
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

// Spec: links, requirements "Categorias de links" and "Links pela tela".
describe('Categorias de links na Administração', () => {
  it('Categorias de links pela tela', async () => {
    await renderApp('/admin/link-categories');
    await waitFor(() => {
      expect(rows()).toHaveLength(5);
    });
    expect(screen.getByRole('link', { name: /Categorias de links/ })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByText(text.note)).toBeVisible();
    expect(rows()[2]).toBe(`Painéis${text.statuses.Inactive}`);

    fireEvent.click(screen.getByRole('button', { name: text.newCategory }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: 'Documentação' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });
    expect(requests).toEqual(['POST Documentação']);
    expect(
      await screen.findByText(text.messages.added.replace('{{name}}', 'Documentação')),
    ).toBeVisible();
  });

  it('Nome de categoria repetido pela tela', async () => {
    await renderApp('/admin/link-categories');
    await waitFor(() => {
      expect(rows()).toHaveLength(5);
    });

    fireEvent.click(screen.getByRole('button', { name: text.newCategory }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: 'sistemas' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    expect(await within(dialog).findByText(text.errors.nameInUse)).toBeVisible();
    expect(requests).toEqual(['POST sistemas']);
  });

  it('Inativar e reativar categoria pela tela', async () => {
    await renderApp('/admin/link-categories');
    await waitFor(() => {
      expect(rows()).toHaveLength(5);
    });

    fireEvent.click(
      screen.getByRole('button', { name: text.menu.button.replace('{{name}}', 'Sistemas') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.deactivate }));
    await waitFor(() => {
      expect(requests).toEqual(['PUT Sistemas false']);
    });

    fireEvent.click(
      screen.getByRole('button', { name: text.menu.button.replace('{{name}}', 'Painéis') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.reactivate }));
    await waitFor(() => {
      expect(requests).toEqual(['PUT Sistemas false', 'PUT Painéis true']);
    });
  });
});
