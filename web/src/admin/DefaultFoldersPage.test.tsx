import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { DefaultFolder } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoDefaultFolders } from '../test/demoDocuments';
import { renderApp } from '../test/renderApp';
import { marina, problem, server } from '../test/server';

const text = ptBR.defaultFolders;
const administrator = { ...marina, role: 'Administrator' as const };
let folders: DefaultFolder[];
const requests: string[] = [];

beforeEach(() => {
  folders = demoDefaultFolders();
  requests.length = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(administrator)),
    http.get('/api/admin/document-folders', () => HttpResponse.json(folders)),
    http.post('/api/admin/document-folders', async ({ request }) => {
      const body = (await request.json()) as Pick<DefaultFolder, 'name'>;
      requests.push(`POST ${body.name}`);
      if (folders.some((folder) => folder.name.toLowerCase() === body.name.toLowerCase())) {
        return problem(409, 'urn:fiscoprojects:problem:document-folder-name-in-use');
      }
      const id = '0199f5a2-7c1e-7d40-9a1b-b00000000009';
      folders = [...folders, { id, name: body.name, isAcceptance: false, isActive: true }].sort(
        (a, b) => a.name.localeCompare(b.name),
      );
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/admin/document-folders/:id', async ({ params, request }) => {
      const body = (await request.json()) as Pick<DefaultFolder, 'name' | 'isActive'>;
      requests.push(`PUT ${body.name} ${String(body.isActive)}`);
      folders = folders.map((folder) =>
        folder.id === params.id ? { ...folder, ...body } : folder,
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

// Spec: documentos, requirements "Pastas padrão" and "Documentos pela tela".
describe('Pastas padrão na Administração', () => {
  it('Pastas padrão pela tela', async () => {
    await renderApp('/admin/document-folders');
    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });
    expect(screen.getByRole('link', { name: /Pastas padrão/ })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByText(text.note)).toBeVisible();
    expect(rows()[5]).toBe(`06 Termos de aceite${text.acceptance}${text.statuses.Active}`);
    expect(rows()[3]).toBe(`04 Atas de reunião${text.regular}${text.statuses.Inactive}`);

    fireEvent.click(screen.getByRole('button', { name: text.newFolder }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: '05a Treinamentos' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    await waitFor(() => {
      expect(rows()).toHaveLength(7);
    });
    expect(requests).toEqual(['POST 05a Treinamentos']);
    expect(
      await screen.findByText(text.messages.added.replace('{{name}}', '05a Treinamentos')),
    ).toBeVisible();
  });

  it('Nome de pasta repetido pela tela', async () => {
    await renderApp('/admin/document-folders');
    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });

    fireEvent.click(screen.getByRole('button', { name: text.newFolder }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: '03 requisitos e especificações' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    expect(await within(dialog).findByText(text.errors.nameInUse)).toBeVisible();

    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: 'Atas/Reuniões' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));
    expect(await within(dialog).findByText(text.errors.nameRequired)).toBeVisible();
    expect(requests).toEqual(['POST 03 requisitos e especificações']);
  });

  it('Pasta de termos sempre ativa pela tela', async () => {
    await renderApp('/admin/document-folders');
    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });

    fireEvent.click(
      screen.getByRole('button', {
        name: text.menu.button.replace('{{name}}', '06 Termos de aceite'),
      }),
    );
    const menu = await screen.findByRole('menu');
    expect(within(menu).getByRole('menuitem', { name: text.menu.edit })).toBeVisible();
    expect(
      within(menu).queryByRole('menuitem', { name: text.menu.deactivate }),
    ).not.toBeInTheDocument();
    fireEvent.keyDown(menu, { key: 'Escape' });

    fireEvent.click(
      screen.getByRole('button', {
        name: text.menu.button.replace('{{name}}', '04 Atas de reunião'),
      }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.reactivate }));

    await waitFor(() => {
      expect(requests).toEqual(['PUT 04 Atas de reunião true']);
    });
  });
});
