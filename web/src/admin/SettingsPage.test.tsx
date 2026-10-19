import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { type AuditEntry, type Installation, installationProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { auditPage, problem, server, signedInAs } from '../test/server';

const text = ptBR.settings;
const logoUrl = '/api/installation/logo?v=1';

/** The installation as the simulated API keeps it, changed by the requests as the real one would. */
let installation: Installation;
const requests: string[] = [];
let sentLogo: File | undefined;

beforeEach(() => {
  installation = { name: 'FiscoProjects', logoUrl: null };
  requests.length = 0;
  sentLogo = undefined;
  signedInAs('Administrator');
  server.use(
    http.get('/api/installation', () => HttpResponse.json(installation)),
    http.put('/api/installation', async ({ request }) => {
      const body = (await request.json()) as { name: string };
      requests.push(`PUT name ${body.name}`);
      installation = { ...installation, name: body.name };
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/installation/logo', async ({ request }) => {
      // The File of the test environment and the one of the simulated API are not the same class, and its name does
      // not cross from one to the other (it arrives as "blob"); the browser sends it, as the API tests check.
      sentLogo = ((await request.formData()).get('file') ?? undefined) as File | undefined;
      requests.push('PUT logo');
      installation = { ...installation, logoUrl };
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/installation/logo', () => {
      requests.push('DELETE logo');
      installation = { ...installation, logoUrl: null };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openSettings() {
  const view = await renderApp('/admin/settings');
  await screen.findByRole('heading', { level: 1, name: ptBR.pages.settings.title });
  await waitFor(() => {
    expect(nameField()).toHaveValue(installation.name);
  });
  return view;
}

function nameField() {
  return screen.getByRole('textbox', { name: text.name });
}

function saveButton() {
  return screen.getByRole('button', { name: text.save });
}

function discardButton() {
  return screen.getByRole('button', { name: text.discard });
}

/** The brand at the top of the sidebar of the application, outside the form and its Preview. */
function sidebarBrand() {
  const sidebar = screen.getByRole('navigation', { name: ptBR.shell.mainNavigation });
  const brand = within(sidebar).getAllByRole('link').at(0);
  if (brand === undefined) {
    throw new Error('The sidebar has no brand.');
  }
  return brand;
}

function preview() {
  return screen.getByTestId('preview-sidebar');
}

function chooseLogo(file: File) {
  fireEvent.change(screen.getByTestId('logo-file'), { target: { files: [file] } });
}

const png = () =>
  new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])], 'marca.png', {
    type: 'image/png',
  });

// Spec: configuracao-da-instalacao, requirement "Tela Configurações gerais".
describe('Tela Configurações gerais', () => {
  it('Configurações gerais na Administração', async () => {
    await renderApp('/admin/users');
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });

    fireEvent.click(within(sections).getByRole('link', { name: ptBR.pages.settings.title }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.settings.title }),
    ).toBeInTheDocument();
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(within(trail).getByText(ptBR.pages.admin.title)).toBeInTheDocument();
    expect(screen.getByText(ptBR.admin.sections.installation)).toBeInTheDocument();
    const brand = screen.getByRole('region', { name: text.brand });
    await waitFor(() => {
      expect(within(brand).getByRole('textbox', { name: text.name })).toHaveValue('FiscoProjects');
    });
    expect(within(brand).getByTestId('logo-tile')).toHaveTextContent(text.noLogo);
    expect(within(brand).getByRole('button', { name: text.sendLogo })).toBeInTheDocument();
    expect(within(brand).getByText(text.logoHint)).toBeInTheDocument();
    expect(screen.getByRole('complementary', { name: text.previewLabel })).toBeInTheDocument();
    expect(preview()).toHaveTextContent('FiscoProjects');
    expect(saveButton()).toBeDisabled();
    expect(discardButton()).toBeDisabled();
  });

  it('Prévia antes de salvar', async () => {
    await openSettings();

    fireEvent.change(nameField(), { target: { value: 'Consultoria Fictícia' } });
    chooseLogo(png());

    expect(preview()).toHaveTextContent('Consultoria Fictícia');
    await waitFor(() => {
      expect(preview().querySelector('img')).toHaveAttribute(
        'src',
        expect.stringMatching(/^data:image\/png/),
      );
    });
    expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    // The rest of the application changes only after saving.
    expect(sidebarBrand()).toHaveTextContent('FiscoProjects');
    expect(sidebarBrand().querySelector('img')).toBeNull();
    expect(requests).toEqual([]);
    expect(saveButton()).toBeEnabled();
  });

  it('Descartar alterações', async () => {
    await openSettings();
    fireEvent.change(nameField(), { target: { value: 'Consultoria Fictícia' } });
    chooseLogo(png());
    await waitFor(() => {
      expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    });

    fireEvent.click(discardButton());

    expect(nameField()).toHaveValue('FiscoProjects');
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(text.noLogo);
    expect(saveButton()).toBeDisabled();
    expect(discardButton()).toBeDisabled();
  });
});

// Spec: configuracao-da-instalacao, requirements "Nome da instalação" and "Logo da instalação" (the screen).
describe('Nome e logo nas Configurações gerais', () => {
  it('Trocar o nome', async () => {
    await openSettings();

    fireEvent.change(nameField(), { target: { value: '  Consultoria Fictícia ' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT name Consultoria Fictícia']);
    await waitFor(() => {
      expect(sidebarBrand()).toHaveTextContent('Consultoria Fictícia');
    });
    await waitFor(() => {
      expect(document.title).toBe('Consultoria Fictícia');
    });
    expect(saveButton()).toBeDisabled();
  });

  it('Nome inválido', async () => {
    await openSettings();

    fireEvent.change(nameField(), { target: { value: '   ' } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(text.errors.nameRequired)).toBeInTheDocument();

    fireEvent.change(nameField(), { target: { value: 'a'.repeat(61) } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(text.errors.nameTooLong)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });

  it('Enviar o logo', async () => {
    await openSettings();

    chooseLogo(png());
    await waitFor(() => {
      expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT logo']);
    expect(sentLogo?.size).toBe(8);
    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);
    });
    expect(screen.getByRole('button', { name: text.replaceLogo })).toBeInTheDocument();
  });

  it('Remover o logo', async () => {
    installation = { name: 'FiscoProjects', logoUrl };
    await openSettings();
    expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);

    fireEvent.click(screen.getByRole('button', { name: text.removeLogo }));
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(text.noLogo);
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['DELETE logo']);
    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toBeNull();
    });
  });

  it('Logo recusado', async () => {
    await openSettings();

    chooseLogo(new File([new Uint8Array([0xff, 0xd8, 0xff])], 'foto.jpg', { type: 'image/jpeg' }));
    expect(await screen.findByText(text.errors.logoFormat)).toBeInTheDocument();

    chooseLogo(new File([new Uint8Array(1024 * 1024 + 1)], 'grande.png', { type: 'image/png' }));
    expect(await screen.findByText(text.errors.logoTooLarge)).toBeInTheDocument();
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(text.noLogo);

    // An SVG with a script is refused by the API, which checks its content.
    server.use(
      http.put('/api/installation/logo', () => problem(400, installationProblems.logoUnsafe)),
    );
    chooseLogo(
      new File(['<svg xmlns="http://www.w3.org/2000/svg"/>'], 'marca.svg', {
        type: 'image/svg+xml',
      }),
    );
    await waitFor(() => {
      expect(saveButton()).toBeEnabled();
    });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.logoUnsafe)).toBeInTheDocument();
    expect(sidebarBrand().querySelector('img')).toBeNull();
    expect(screen.queryByText(text.saved)).not.toBeInTheDocument();
  });
});

// Spec: configuracao-da-instalacao, requirement "Mudança das configurações na auditoria".
describe('Configurações na auditoria', () => {
  beforeEach(() => {
    // 12:00:30 of October 19th in the time zone of the tests; only the date is simulated, timers stay real.
    vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('Configurações no filtro da Auditoria', async () => {
    const entries: AuditEntry[] = [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000001',
        occurredAt: '2026-10-19T13:20:00Z',
        actor: { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' },
        action: 'Updated',
        itemType: 'InstallationSettings',
        itemId: '0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20',
        itemLabel: null,
        detail: null,
        changes: [
          { field: 'Name', before: 'FiscoProjects', after: 'Consultoria Fictícia', secret: false },
        ],
      },
    ];
    const asked: (string | null)[] = [];
    server.use(
      http.get('/api/audit', ({ request }) => {
        const url = new URL(request.url);
        asked.push(url.searchParams.get('itemType'));
        return HttpResponse.json(auditPage(entries, url));
      }),
    );
    await renderApp('/admin/audit');

    fireEvent.mouseDown(screen.getByRole('combobox', { name: ptBR.audit.filters.type }));
    fireEvent.click(
      await screen.findByRole('option', { name: ptBR.audit.itemTypes.InstallationSettings }),
    );

    await waitFor(() => {
      expect(asked.at(-1)).toBe('InstallationSettings');
    });
    const grid = screen.getByRole('grid', { name: ptBR.audit.table });
    expect(
      await within(grid).findByText(ptBR.audit.actions.InstallationSettings.Updated),
    ).toBeInTheDocument();
    expect(
      within(grid).getByText(
        `${ptBR.audit.fields.InstallationSettings.Name.toLowerCase()} FiscoProjects → Consultoria Fictícia`,
      ),
    ).toBeInTheDocument();
  });
});
