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
let timeZone: string;
const requests: string[] = [];

beforeEach(() => {
  installation = { name: 'FiscoProjects', logoUrl: null };
  timeZone = 'America/Sao_Paulo';
  requests.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/installation', () => HttpResponse.json(installation)),
    http.get('/api/installation/rules', () => HttpResponse.json({ timeZone })),
    http.put('/api/installation', async ({ request }) => {
      const body = (await request.json()) as { name: string; timeZone: string };
      requests.push(`PUT ${body.name} ${body.timeZone}`);
      installation = { ...installation, name: body.name };
      timeZone = body.timeZone;
      return new HttpResponse(null, { status: 204 });
    }),
    // The multipart body is not read: the File of the test environment does not cross to the simulated API the same
    // way in every Node version. The API tests check the file, and the check in the browser checks its name.
    http.put('/api/installation/logo', ({ request }) => {
      requests.push(`PUT logo ${request.headers.get('content-type')?.split(';')[0] ?? ''}`);
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

function timeZoneField() {
  return screen.getByRole('combobox', { name: text.timeZone });
}

/** The name the browser gives a time zone, with its id, as the field shows it. */
function timeZoneLabel(id: string) {
  const name = new Intl.DateTimeFormat('pt-BR', { timeZone: id, timeZoneName: 'longGeneric' })
    .formatToParts(new Date())
    .find((part) => part.type === 'timeZoneName')?.value;
  return `${name ?? ''} (${id})`;
}

async function chooseTimeZone(id: string) {
  // As a person does: into the field, then typing; outside it, the field shows the chosen one again.
  timeZoneField().focus();
  fireEvent.change(timeZoneField(), { target: { value: id } });
  fireEvent.click(await screen.findByRole('option', { name: timeZoneLabel(id) }));
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
    expect(requests).toEqual(['PUT Consultoria Fictícia America/Sao_Paulo']);
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
    expect(requests).toEqual(['PUT logo multipart/form-data']);
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

// Spec: configuracao-da-instalacao, requirement "Fuso da instalação" (General settings).
describe('Fuso nas Configurações gerais', () => {
  it('Fuso padrão', async () => {
    await openSettings();

    const rules = screen.getByRole('region', { name: text.rules });
    expect(within(rules).getByRole('combobox', { name: text.timeZone })).toHaveValue(
      timeZoneLabel('America/Sao_Paulo'),
    );
    expect(timeZoneLabel('America/Sao_Paulo')).toMatch(/Brasília \(America\/Sao_Paulo\)$/);
  });

  it('Trocar o fuso', async () => {
    await openSettings();

    await chooseTimeZone('Asia/Tokyo');
    expect(saveButton()).toBeEnabled();
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT FiscoProjects Asia/Tokyo']);
    await waitFor(() => {
      expect(timeZoneField()).toHaveValue(timeZoneLabel('Asia/Tokyo'));
    });
    expect(saveButton()).toBeDisabled();
  });

  it('Descartar o fuso', async () => {
    await openSettings();
    await chooseTimeZone('Asia/Tokyo');

    fireEvent.click(discardButton());

    expect(timeZoneField()).toHaveValue(timeZoneLabel('America/Sao_Paulo'));
    expect(saveButton()).toBeDisabled();
  });

  it('Fuso recusado pela API', async () => {
    server.use(
      http.put('/api/installation', () =>
        HttpResponse.json(
          { status: 400, title: 'Problem', errors: { timeZone: ['Choose an IANA time zone.'] } },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openSettings();
    await chooseTimeZone('Asia/Tokyo');

    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.timeZoneInvalid)).toBeInTheDocument();
    expect(timeZoneField()).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByText(text.saved)).not.toBeInTheDocument();
  });

  it('Fuso obrigatório', async () => {
    await openSettings();

    timeZoneField().focus();
    fireEvent.change(timeZoneField(), { target: { value: '' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.timeZoneRequired)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });
});
