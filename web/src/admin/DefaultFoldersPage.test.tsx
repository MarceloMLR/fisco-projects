import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { DefaultFolder } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { demoDefaultFolders } from '../test/demoDocuments';
import { renderApp } from '../test/renderApp';
import { marina, problem, server } from '../test/server';

const text = ptBR.defaultFolders;
const visibility = ptBR.folderVisibility;
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
      const body = (await request.json()) as Pick<DefaultFolder, 'name' | 'visibleTo'>;
      requests.push(`POST ${body.name} ${body.visibleTo.join(',')}`);
      if (folders.some((folder) => folder.name.toLowerCase() === body.name.toLowerCase())) {
        return problem(409, 'urn:fiscoprojects:problem:document-folder-name-in-use');
      }
      const id = '0199f5a2-7c1e-7d40-9a1b-b00000000009';
      folders = [
        ...folders,
        { id, name: body.name, isAcceptance: false, isActive: true, visibleTo: body.visibleTo },
      ].sort((a, b) => a.name.localeCompare(b.name));
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/admin/document-folders/:id', async ({ params, request }) => {
      const body = (await request.json()) as Pick<DefaultFolder, 'name' | 'isActive'> &
        Partial<Pick<DefaultFolder, 'visibleTo'>>;
      requests.push(
        `PUT ${body.name} ${String(body.isActive)}${body.visibleTo === undefined ? '' : ` ${body.visibleTo.join(',')}`}`,
      );
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
    expect(rows()[5]).toBe(
      `06 Termos de aceite${visibility.onlyManagement}${text.acceptance}${text.statuses.Active}`,
    );
    expect(rows()[3]).toBe(
      `04 Atas de reunião${visibility.everyone}${text.regular}${text.statuses.Inactive}`,
    );
    expect(rows()[0]).toBe(
      `01 Gestão do projeto${visibility.management} e Líder técnico${text.regular}${text.statuses.Active}`,
    );

    fireEvent.click(screen.getByRole('button', { name: text.newFolder }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.name), {
      target: { value: '05a Treinamentos' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    await waitFor(() => {
      expect(rows()).toHaveLength(7);
    });
    expect(requests).toEqual(['POST 05a Treinamentos TechLead,Contributor,Developer,Viewer']);
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
    expect(requests).toEqual([
      'POST 03 requisitos e especificações TechLead,Contributor,Developer,Viewer',
    ]);
  });

  // Spec: documentos, requirement "Quem vê as pastas pela tela" (ADR-0148).
  it('Quem vê na pasta padrão pela tela', async () => {
    await renderApp('/admin/document-folders');
    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });

    fireEvent.click(
      screen.getByRole('button', {
        name: text.menu.button.replace('{{name}}', '02 Contratos e compras'),
      }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.editTitle });
    expect(within(dialog).getByText(visibility.help)).toBeVisible();
    const viewer = within(dialog).getByRole('checkbox', { name: ptBR.projects.roles.Viewer });
    expect(viewer).toBeChecked();
    fireEvent.click(viewer);
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));

    await waitFor(() => {
      expect(rows()[1]).toBe(
        `02 Contratos e compras${visibility.management}, Líder técnico, Colaborador e Desenvolvedor${text.regular}${text.statuses.Active}`,
      );
    });
    expect(requests).toEqual(['PUT 02 Contratos e compras true TechLead,Contributor,Developer']);
  });

  it('Pasta de termos com quem vê fixo pela tela', async () => {
    await renderApp('/admin/document-folders');
    await waitFor(() => {
      expect(rows()).toHaveLength(6);
    });

    fireEvent.click(
      screen.getByRole('button', {
        name: text.menu.button.replace('{{name}}', '06 Termos de aceite'),
      }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.editTitle });
    expect(within(dialog).getByText(visibility.termsFixed)).toBeVisible();
    expect(within(dialog).queryByRole('checkbox')).not.toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));

    await waitFor(() => {
      expect(requests).toEqual(['PUT 06 Termos de aceite true']);
    });
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

// Spec: configuracao-da-instalacao, requirement "Pasta dos projetos" (Default folders, ADR-0121, ADR-0150).
describe('Pasta dos projetos em Pastas padrão', () => {
  const folderText = text.projectsFolder;
  let projectsFolder: string;

  beforeEach(() => {
    projectsFolder = '';
    server.use(
      http.get('/api/installation/rules', () =>
        HttpResponse.json({
          timeZone: 'America/Sao_Paulo',
          dailySummaryAt: '07:30:00',
          notificationRetentionDays: 90,
          auditRetentionYears: 5,
          projectsFolder,
          workHoursPerDay: 8,
        }),
      ),
      http.put('/api/installation/projects-folder', async ({ request }) => {
        const body = (await request.json()) as { projectsFolder: string };
        requests.push(`PUT projects folder ${body.projectsFolder}`);
        projectsFolder = body.projectsFolder;
        return new HttpResponse(null, { status: 204 });
      }),
    );
  });

  async function openFolders() {
    await renderApp('/admin/document-folders');
    const section = await screen.findByRole('region', { name: folderText.label });
    await waitFor(() => {
      expect(within(section).getByRole('button', { name: folderText.changeLabel })).toBeEnabled();
    });
    return section;
  }

  function folderField() {
    return within(screen.getByRole('dialog', { name: folderText.title })).getByRole('textbox', {
      name: folderText.field,
    });
  }

  it('Pasta dos projetos padrão', async () => {
    const section = await openFolders();

    expect(within(section).getByTestId('projects-folder')).toHaveTextContent(folderText.root);
  });

  it('Alterar a pasta dos projetos pela tela', async () => {
    const section = await openFolders();

    fireEvent.click(within(section).getByRole('button', { name: folderText.changeLabel }));
    expect(folderField()).toHaveValue('');
    expect(screen.getByText(folderText.hint)).toBeInTheDocument();
    fireEvent.change(folderField(), { target: { value: ' Clientes/Projetos ' } });
    fireEvent.click(screen.getByRole('button', { name: folderText.save }));

    expect(await screen.findByText(folderText.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT projects folder Clientes/Projetos']);
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: folderText.title })).toBeNull();
    });
    await waitFor(() => {
      expect(within(section).getByTestId('projects-folder')).toHaveTextContent('Clientes/Projetos');
    });
  });

  it('Pasta dos projetos inválida', async () => {
    server.use(
      http.put('/api/installation/projects-folder', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { projectsFolder: ['Separate the folders with /.'] },
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    const section = await openFolders();

    fireEvent.click(within(section).getByRole('button', { name: folderText.changeLabel }));
    fireEvent.change(folderField(), { target: { value: 'Projetos/Fin:2026' } });
    fireEvent.click(screen.getByRole('button', { name: folderText.save }));

    expect(await screen.findByText(folderText.invalid)).toBeInTheDocument();
    expect(folderField()).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByRole('dialog', { name: folderText.title })).toBeInTheDocument();
    expect(screen.queryByText(folderText.saved)).not.toBeInTheDocument();
  });
});
