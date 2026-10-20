import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { ProjectTemplate } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { demoTemplates, marina, server } from '../test/server';

const text = ptBR.templates;
const administrator = { ...marina, role: 'Administrator' as const };
let templates: ProjectTemplate[];
const requests: string[] = [];

beforeEach(() => {
  templates = demoTemplates();
  requests.length = 0;
  server.use(
    http.get('/api/session', () => HttpResponse.json(administrator)),
    http.get('/api/admin/project-templates', () => HttpResponse.json(templates)),
    http.put('/api/admin/project-templates/:id', async ({ params, request }) => {
      const body = (await request.json()) as Pick<
        ProjectTemplate,
        'name' | 'description' | 'isActive'
      >;
      requests.push(`PUT ${body.name} ${String(body.isActive)}`);
      templates = templates.map((template) =>
        template.id === params.id ? { ...template, ...body } : template,
      );
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/admin/project-templates/:id', ({ params }) => {
      requests.push(`DELETE ${String(params.id)}`);
      templates = templates.filter((template) => template.id !== params.id);
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

async function openMenu(name: string) {
  fireEvent.click(screen.getByRole('button', { name: text.menu.button.replace('{{name}}', name) }));
  return screen.findByRole('menu');
}

// Spec: modelos-de-projeto, requirement "Modelos pela tela".
describe('Modelos de projeto na Administração', () => {
  it('Modelos na Administração pela tela', async () => {
    await renderApp('/admin/templates');
    await waitFor(() => {
      expect(rows()).toHaveLength(2);
    });
    // The name, the phases and items, the situation and the description of each model.
    expect(rows()[0]).toContain('Implantação de ERP3 fases · 6 itensAtivo');
    expect(rows()[1]).toContain('Revisão tributária de filial1 fase · 1 itemInativo');

    fireEvent.click(
      within(await openMenu('Implantação de ERP')).getByRole('menuitem', {
        name: text.menu.deactivate,
      }),
    );
    expect(
      await screen.findByText(text.messages.deactivated.replace('{{name}}', 'Implantação de ERP')),
    ).toBeVisible();
    await waitFor(() => {
      expect(rows()[0]).toContain(text.statuses.Inactive);
    });

    fireEvent.click(
      within(await openMenu('Implantação de ERP')).getByRole('menuitem', {
        name: text.menu.remove,
      }),
    );
    const dialog = await screen.findByRole('alertdialog', { name: text.remove.title });
    fireEvent.click(within(dialog).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(rows()).toHaveLength(1);
    });
    expect(requests).toEqual([
      'PUT Implantação de ERP false',
      'DELETE 0199f5a2-7c1e-7d40-9a1b-700000000001',
    ]);
  });

  it('Editar um modelo pela tela', async () => {
    await renderApp('/admin/templates');
    await waitFor(() => {
      expect(rows()).toHaveLength(2);
    });

    fireEvent.click(
      within(await openMenu('Revisão tributária de filial')).getByRole('menuitem', {
        name: text.menu.edit,
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    fireEvent.change(within(dialog).getByRole('textbox', { name: text.dialog.name }), {
      target: { value: 'Revisão tributária' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));

    expect(
      await screen.findByText(text.messages.saved.replace('{{name}}', 'Revisão tributária')),
    ).toBeVisible();
    // The situation stays as it was.
    expect(requests).toEqual(['PUT Revisão tributária false']);
  });
});
