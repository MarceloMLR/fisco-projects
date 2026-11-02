import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Installation, installationProblems } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { expectBusy, heldAnswer, untilIdle } from '../test/busy';
import { phoneWidth, renderApp } from '../test/renderApp';
import { marina, problem, server, signedInAs } from '../test/server';
import { colorFunction } from '../theme/colorFormats';

const text = ptBR.colors;
const brand = ptBR.brand;
const visual = ptBR.visual;
const logoUrl = '/api/installation/logo?v=1';
const darkLogoUrl = '/api/installation/logo/dark?v=1';

// Colors as the Administrator types them, without "#": colors in the code come only from tokens.json.
const hash = (value: string) => `#${value}`;

/** The installation as the simulated API keeps it, changed by the requests as the real one would. */
let installation: Installation;
const sent: unknown[] = [];
/** What the screen asked the API to change, in order. */
const requests: string[] = [];

beforeEach(() => {
  installation = {
    name: 'Consultoria Fictícia',
    logoUrl: null,
    mainColor: null,
    darkLogoUrl: null,
    secondaryColor: null,
    sidebarColor: null,
  };
  sent.length = 0;
  requests.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/installation', () => HttpResponse.json(installation)),
    http.put('/api/installation/colors', async ({ request }) => {
      const body = (await request.json()) as {
        mainColor: string | null;
        secondaryColor: string | null;
        sidebarColor: string | null;
      };
      sent.push(body);
      requests.push('PUT colors');
      installation = { ...installation, ...body };
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/installation/name', async ({ request }) => {
      const body = (await request.json()) as { name: string };
      requests.push(`PUT name ${body.name}`);
      installation = { ...installation, name: body.name };
      return new HttpResponse(null, { status: 204 });
    }),
    // The multipart body is not read: the File of the test environment does not cross to the simulated API the same
    // way in every Node version. The API tests check the file, and the check in the browser checks its name.
    http.put('/api/installation/logo', () => {
      requests.push('PUT logo');
      installation = { ...installation, logoUrl };
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/installation/logo', () => {
      requests.push('DELETE logo');
      installation = { ...installation, logoUrl: null };
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/installation/logo/dark', () => {
      requests.push('PUT dark logo');
      installation = { ...installation, darkLogoUrl };
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/installation/logo/dark', () => {
      requests.push('DELETE dark logo');
      installation = { ...installation, darkLogoUrl: null };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openColors(width?: number) {
  const view = await renderApp('/admin/visual', width);
  await screen.findByRole('heading', { level: 1, name: ptBR.pages.visual.title });
  await waitFor(() => {
    expect(nameField()).toHaveValue(installation.name);
  });
  return view;
}

function nameField() {
  return screen.getByRole('textbox', { name: brand.name });
}

/** The brand at the top of the sidebar of the application, outside the form and its preview. */
function sidebarBrand() {
  const sidebar = screen.getByRole('navigation', { name: ptBR.shell.mainNavigation });
  const link = within(sidebar).getAllByRole('link').at(0);
  if (link === undefined) {
    throw new Error('The sidebar has no brand.');
  }
  return link;
}

function previewSidebar() {
  return screen.getByTestId('colors-preview-sidebar');
}

function chooseLogo(file: File, testId = 'logo') {
  fireEvent.change(screen.getByTestId(`${testId}-file`), { target: { files: [file] } });
}

const png = () =>
  new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])], 'marca.png', {
    type: 'image/png',
  });

/** Marina, the Administrator, has chosen the dark theme in her profile (ADR-0014). */
function inTheDarkTheme() {
  server.use(
    http.get('/api/session', () =>
      HttpResponse.json({ ...marina, role: 'Administrator', theme: 'Dark' }),
    ),
  );
}

function field(name: 'main' | 'secondary' | 'sidebar') {
  return screen.getByRole('textbox', { name: text[name].value });
}

function group(name: 'main' | 'secondary' | 'sidebar') {
  return screen.getByRole('group', { name: `${text[name].label} ${text.optional}` });
}

function typeColor(name: 'main' | 'secondary' | 'sidebar', value: string) {
  fireEvent.change(field(name), { target: { value } });
  fireEvent.blur(field(name));
}

function previewApp() {
  return screen.getByTestId('colors-preview-app');
}

function saveButton() {
  return screen.getByRole('button', { name: visual.save });
}

function discardButton() {
  return screen.getByRole('button', { name: visual.discard });
}

/** The colors applied to the whole application, when the installation has colors of its own. */
function appliedColors() {
  const rules = [...document.querySelectorAll('style')]
    .map((style) => style.textContent)
    .filter(
      (css) =>
        css.includes('[data-theme="light"][data-theme="light"]') ||
        css.includes('[data-theme="dark"][data-theme="dark"]'),
    );
  return rules.join('\n');
}

function picker() {
  return screen.getByRole('dialog', { name: text.main.label });
}

// Spec: configuracao-da-instalacao, requirement "Tela Visual" (ADR-0146, ADR-0150).
describe('Tela Visual', () => {
  it('Cores na Administração', async () => {
    await renderApp('/admin/users');
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const links = within(sections).getAllByRole('link');
    const settings = links.findIndex((link) => link.textContent === ptBR.pages.settings.title);
    // Right after General settings.
    expect(links[settings + 1]).toHaveTextContent(ptBR.pages.visual.title);

    fireEvent.click(within(sections).getByRole('link', { name: ptBR.pages.visual.title }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.visual.title }),
    ).toBeInTheDocument();
    expect(screen.getByRole('region', { name: visual.colors })).toBeInTheDocument();
    for (const name of ['main', 'secondary', 'sidebar'] as const) {
      const colorGroup = group(name);
      expect(within(colorGroup).getByText(text[name].where)).toBeInTheDocument();
      expect(
        within(colorGroup).getByRole('button', {
          name: text.choose.replace('{{color}}', text[name].label.toLowerCase()),
        }),
      ).toBeInTheDocument();
      expect(within(colorGroup).getByRole('button', { name: text.defaultColor })).toBeDisabled();
    }
    expect(screen.getByRole('region', { name: text.preview.label })).toBeInTheDocument();
    expect(saveButton()).toBeDisabled();
    expect(discardButton()).toBeDisabled();
  });

  it('Marca em Visual', async () => {
    await openColors();

    const sections = screen
      .getAllByRole('region')
      .map((region) => region.getAttribute('aria-labelledby'));
    const brandSection = screen.getByRole('region', { name: brand.title });
    // The Brand comes before the colors.
    expect(sections.indexOf(brandSection.getAttribute('aria-labelledby'))).toBeLessThan(
      sections.indexOf(
        screen.getByRole('region', { name: visual.colors }).getAttribute('aria-labelledby'),
      ),
    );
    expect(within(brandSection).getByRole('textbox', { name: brand.name })).toHaveValue(
      'Consultoria Fictícia',
    );
    expect(within(brandSection).getByTestId('logo-tile')).toHaveTextContent(brand.noLogo);
    expect(within(brandSection).getByRole('button', { name: brand.sendLogo })).toBeInTheDocument();
    expect(within(brandSection).getByText(brand.logoHint)).toBeInTheDocument();
    expect(
      within(brandSection).getByRole('button', { name: brand.sendDarkLogo }),
    ).toBeInTheDocument();
    expect(saveButton()).toBeDisabled();
  });

  it('Endereço antigo de Cores', async () => {
    const view = await renderApp('/admin/colors');

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.visual.title }),
    ).toBeInTheDocument();
    expect(view.router.state.location.pathname).toBe('/admin/visual');
  });

  it('Prévia da marca antes de salvar', async () => {
    await openColors();

    fireEvent.change(nameField(), { target: { value: 'Projetos da Empresa' } });
    chooseLogo(png());

    await waitFor(() => {
      expect(previewSidebar().querySelector('img')).toHaveAttribute(
        'src',
        expect.stringMatching(/^data:image\/png/),
      );
    });
    expect(previewSidebar()).toHaveTextContent('Projetos da Empresa');
    // The rest of the application changes only after saving.
    expect(sidebarBrand()).toHaveTextContent('Consultoria Fictícia');
    expect(sidebarBrand().querySelector('img')).toBeNull();
    expect(requests).toEqual([]);
    expect(saveButton()).toBeEnabled();
  });

  it('Salvar a marca e as cores', async () => {
    await openColors();
    fireEvent.change(nameField(), { target: { value: '  Projetos da Empresa ' } });
    chooseLogo(png());
    await waitFor(() => {
      expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    });
    typeColor('main', '0f766e');

    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT logo', 'PUT name Projetos da Empresa', 'PUT colors']);
    await waitFor(() => {
      expect(sidebarBrand()).toHaveTextContent('Projetos da Empresa');
    });
    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);
    });
    await waitFor(() => {
      expect(appliedColors()).toMatch(new RegExp(`--action:\\s*${hash('0f766e')};`));
    });
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
  });

  it('Prévia das cores antes de salvar', async () => {
    await openColors();

    fireEvent.change(field('main'), { target: { value: '0f766e' } });

    // While typed, the preview shows the color; the field keeps it until it is used.
    expect(previewApp().style.getPropertyValue('--action')).toBe(hash('0f766e'));
    expect(screen.getByTestId('main-swatch')).toHaveStyle({ backgroundColor: hash('0F766E') });
    fireEvent.blur(field('main'));
    expect(field('main')).toHaveValue(hash('0F766E'));
    expect(within(group('main')).getByText(text.notes.goodContrast)).toBeInTheDocument();
    // The rest of the application changes only after saving.
    expect(appliedColors()).toBe('');
    expect(sent).toEqual([]);
  });

  it('Prévia nos dois temas', async () => {
    await openColors();
    typeColor('main', '0f766e');

    fireEvent.click(screen.getByRole('radio', { name: text.preview.dark }));

    expect(previewApp()).toHaveAttribute('data-theme', 'dark');
    expect(document.documentElement).not.toHaveAttribute('data-theme', 'dark');
    expect(previewApp().style.getPropertyValue('--action')).toBe(hash('0f766e'));
  });

  it('Salvar as cores', async () => {
    await openColors();
    typeColor('main', '0f766e');
    typeColor('secondary', '14b8a6');
    typeColor('sidebar', '1f2933');

    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(sent).toEqual([
      { mainColor: hash('0F766E'), secondaryColor: hash('14B8A6'), sidebarColor: hash('1F2933') },
    ]);
    // The name and the logos stay as they were.
    expect(requests).toEqual(['PUT colors']);
    await waitFor(() => {
      expect(appliedColors()).toMatch(new RegExp(`--sidebar-bg:\\s*${hash('1f2933')};`));
    });
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
  });

  it('Descartar as cores', async () => {
    await openColors();
    typeColor('main', '0f766e');
    typeColor('sidebar', 'ffffff');
    fireEvent.change(nameField(), { target: { value: 'Projetos da Empresa' } });
    chooseLogo(png());
    await waitFor(() => {
      expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    });

    fireEvent.click(discardButton());

    expect(nameField()).toHaveValue('Consultoria Fictícia');
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(brand.noLogo);
    expect(field('main')).toHaveValue('');
    expect(field('sidebar')).toHaveValue('');
    expect(previewApp().style.getPropertyValue('--action')).not.toBe(hash('0f766e'));
    expect(saveButton()).toBeDisabled();
  });

  it('Cor fora do formato pela API', async () => {
    server.use(
      http.put('/api/installation/colors', () =>
        problem(400, 'Invalid', { errors: { sidebarColor: ['Choose a #RRGGBB color.'] } }),
      ),
    );
    await openColors();
    typeColor('sidebar', '1f2933');

    fireEvent.click(saveButton());

    expect(await within(group('sidebar')).findByText(text.invalid)).toBeInTheDocument();
    expect(field('sidebar')).toHaveAttribute('aria-invalid', 'true');
  });
});

// Spec: configuracao-da-instalacao, requirements "Cor principal" and "Contraste calculado sobre as cores" (Colors).
describe('Cor principal em Cores', () => {
  it('Trocar a cor principal', async () => {
    await openColors();
    typeColor('main', '7a1f3d');

    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    await waitFor(() => {
      expect(appliedColors()).toMatch(new RegExp(`--action:\\s*${hash('7a1f3d')};`));
    });
  });

  it('Cor sem contraste', async () => {
    await openColors();

    typeColor('main', 'F5C400');

    const notes = within(group('main'));
    expect(notes.getByText(text.notes.darkText)).toBeInTheDocument();
    expect(notes.getByText(text.notes.darkerLinks)).toBeInTheDocument();
    expect(previewApp().style.getPropertyValue('--action')).toBe(hash('f5c400'));
    fireEvent.click(saveButton());
    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(sent).toEqual([{ mainColor: hash('F5C400'), secondaryColor: null, sidebarColor: null }]);
  });

  it('Voltar ao padrão', async () => {
    installation = { ...installation, mainColor: hash('7A1F3D') };
    await openColors();
    await waitFor(() => {
      expect(field('main')).toHaveValue(hash('7A1F3D'));
    });

    fireEvent.click(within(group('main')).getByRole('button', { name: text.defaultColor }));
    expect(field('main')).toHaveValue('');
    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(sent).toEqual([{ mainColor: null, secondaryColor: null, sidebarColor: null }]);
  });
});

// Spec: configuracao-da-instalacao, requirements "Cor secundária" and "Cor da barra lateral" (Colors).
describe('Cor secundária e da barra lateral em Cores', () => {
  it('Painel da entrada legível', async () => {
    await openColors();

    typeColor('secondary', 'F5C400');

    expect(within(group('secondary')).getByText(text.notes.darkerPanel)).toBeInTheDocument();
  });

  it('Barra lateral escura no tema claro', async () => {
    await openColors();

    typeColor('sidebar', '1F2933');

    expect(within(group('sidebar')).getByText(text.notes.lightSidebarText)).toBeInTheDocument();
    expect(previewApp().style.getPropertyValue('--sidebar-bg')).toBe(hash('1f2933'));
  });
});

// Spec: configuracao-da-instalacao, requirement "Seletor de cores" (ADR-0146).
describe('Seletor de cores', () => {
  it('Cor em vários formatos', async () => {
    await openColors();

    typeColor('main', colorFunction('rgb', '15, 118, 110'));
    expect(field('main')).toHaveValue(hash('0F766E'));

    typeColor('secondary', colorFunction('hsl', '0, 0%, 100%'));
    expect(field('secondary')).toHaveValue(hash('FFFFFF'));

    typeColor('sidebar', '0f7');
    expect(field('sidebar')).toHaveValue(hash('00FF77'));
  });

  it('Valor que não é cor', async () => {
    await openColors();

    typeColor('main', 'verde');

    expect(within(group('main')).getByText(text.invalid)).toBeInTheDocument();
    expect(field('main')).toHaveAttribute('aria-invalid', 'true');
    expect(saveButton()).toBeDisabled();
  });

  it('Escolher a cor pelo seletor', async () => {
    await openColors();

    fireEvent.click(screen.getByTestId('main-swatch'));
    const value = within(picker()).getByRole('textbox', { name: text.picker.value });
    fireEvent.change(value, { target: { value: '0f766e' } });
    expect(previewApp().style.getPropertyValue('--action')).toBe(hash('0f766e'));
    fireEvent.click(within(picker()).getByRole('button', { name: text.picker.use }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.main.label })).toBeNull();
    });
    expect(field('main')).toHaveValue(hash('0F766E'));
    expect(saveButton()).toBeEnabled();
  });

  it('Seletor pelo teclado', async () => {
    await openColors();
    fireEvent.click(screen.getByTestId('main-swatch'));
    const area = within(picker()).getByRole('slider', { name: text.picker.area });
    const hue = within(picker()).getByRole('slider', { name: text.picker.hue });
    const before = previewApp().style.getPropertyValue('--action');
    const saturation = Number(area.getAttribute('aria-valuenow'));
    const startHue = Number(hue.getAttribute('aria-valuenow'));

    fireEvent.keyDown(area, { key: 'ArrowLeft', shiftKey: true });
    fireEvent.keyDown(hue, { key: 'ArrowRight', shiftKey: true });

    expect(Number(area.getAttribute('aria-valuenow'))).toBe(saturation - 10);
    expect(Number(hue.getAttribute('aria-valuenow'))).toBe((startHue + 10) % 360);
    expect(previewApp().style.getPropertyValue('--action')).not.toBe(before);
  });

  it('Cancelar o seletor', async () => {
    await openColors();
    const before = previewApp().style.getPropertyValue('--action');
    fireEvent.click(screen.getByTestId('main-swatch'));
    fireEvent.keyDown(within(picker()).getByRole('slider', { name: text.picker.hue }), {
      key: 'ArrowRight',
      shiftKey: true,
    });
    expect(previewApp().style.getPropertyValue('--action')).not.toBe(before);

    fireEvent.keyDown(picker(), { key: 'Escape' });

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.main.label })).toBeNull();
    });
    expect(field('main')).toHaveValue('');
    expect(previewApp().style.getPropertyValue('--action')).toBe(before);
    expect(saveButton()).toBeDisabled();

    fireEvent.click(screen.getByTestId('main-swatch'));
    fireEvent.change(within(picker()).getByRole('textbox', { name: text.picker.value }), {
      target: { value: '0f766e' },
    });
    fireEvent.click(within(picker()).getByRole('button', { name: text.picker.cancel }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.main.label })).toBeNull();
    });
    expect(field('main')).toHaveValue('');
  });

  it('Seletor no celular', async () => {
    await openColors(phoneWidth);

    fireEvent.click(screen.getByTestId('main-swatch'));

    // A sheet from the foot of the screen, over a darkened background.
    expect(picker().closest('.MuiDrawer-root')).not.toBeNull();
    expect(picker().className).toMatch(/MuiDrawer-paper/);
    expect(picker().closest('.MuiDrawer-root')?.className).toMatch(
      /MuiDrawer-anchorBottom|MuiModal-root/,
    );
    expect(document.querySelector('.MuiBackdrop-root')).not.toBeNull();
  });
});

// Spec: configuracao-da-instalacao, requirements "Nome da instalação" and "Logo da instalação" (the screen).
describe('Nome e logo em Visual', () => {
  it('Trocar o nome', async () => {
    await openColors();

    fireEvent.change(nameField(), { target: { value: '  Projetos da Empresa ' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT name Projetos da Empresa']);
    await waitFor(() => {
      expect(sidebarBrand()).toHaveTextContent('Projetos da Empresa');
    });
    await waitFor(() => {
      expect(document.title).toBe('Projetos da Empresa');
    });
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
  });

  it('Nome inválido', async () => {
    await openColors();

    fireEvent.change(nameField(), { target: { value: '   ' } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(brand.errors.nameRequired)).toBeInTheDocument();

    fireEvent.change(nameField(), { target: { value: 'a'.repeat(61) } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(brand.errors.nameTooLong)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });

  it('Enviar o logo', async () => {
    await openColors();

    chooseLogo(png());
    await waitFor(() => {
      expect(screen.getByTestId('logo-tile').querySelector('img')).not.toBeNull();
    });
    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT logo']);
    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);
    });
    expect(screen.getByRole('button', { name: brand.replaceLogo })).toBeInTheDocument();
  });

  it('Remover o logo', async () => {
    installation = { ...installation, logoUrl };
    await openColors();
    expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);

    fireEvent.click(screen.getByRole('button', { name: brand.removeLogo }));
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(brand.noLogo);
    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['DELETE logo']);
    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toBeNull();
    });
  });

  it('Logo recusado', async () => {
    await openColors();

    chooseLogo(new File([new Uint8Array([0xff, 0xd8, 0xff])], 'foto.jpg', { type: 'image/jpeg' }));
    expect(await screen.findByText(brand.errors.logoFormat)).toBeInTheDocument();

    chooseLogo(new File([new Uint8Array(1024 * 1024 + 1)], 'grande.png', { type: 'image/png' }));
    expect(await screen.findByText(brand.errors.logoTooLarge)).toBeInTheDocument();
    expect(screen.getByTestId('logo-tile')).toHaveTextContent(brand.noLogo);

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

    expect(await screen.findByText(brand.errors.logoUnsafe)).toBeInTheDocument();
    expect(sidebarBrand().querySelector('img')).toBeNull();
    expect(screen.queryByText(visual.saved)).not.toBeInTheDocument();
  });
});

// Spec: configuracao-da-instalacao, requirement "Logo para o tema escuro" (Visual, ADR-0100, ADR-0146, ADR-0150).
describe('Logo escuro em Visual', () => {
  it('Enviar o logo para o tema escuro', async () => {
    installation = { ...installation, logoUrl };
    await openColors();

    chooseLogo(png(), 'dark-logo');
    await waitFor(() => {
      expect(screen.getByTestId('dark-logo-tile').querySelector('img')).not.toBeNull();
    });
    // The tile of the logo for the dark theme is dark, as the sidebar that shows it.
    expect(screen.getByTestId('dark-logo-tile')).toHaveAttribute('data-theme', 'dark');
    fireEvent.click(saveButton());

    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT dark logo']);
    expect(screen.getByRole('button', { name: brand.replaceDarkLogo })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: brand.removeDarkLogo }));
    await untilIdle(saveButton());
    fireEvent.click(saveButton());
    await waitFor(() => {
      expect(requests).toEqual(['PUT dark logo', 'DELETE dark logo']);
    });
  });

  it('Logo escuro na barra lateral escura', async () => {
    installation = { ...installation, logoUrl, darkLogoUrl };
    inTheDarkTheme();
    await openColors();

    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', darkLogoUrl);
    });
    fireEvent.click(screen.getByRole('radio', { name: text.preview.dark }));
    expect(previewSidebar().querySelector('img')).toHaveAttribute('src', darkLogoUrl);
  });

  it('Logo claro na barra lateral clara', async () => {
    installation = { ...installation, logoUrl, darkLogoUrl };
    await openColors();

    expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);
    expect(previewSidebar().querySelector('img')).toHaveAttribute('src', logoUrl);
  });

  it('Sem logo escuro', async () => {
    installation = { ...installation, logoUrl };
    inTheDarkTheme();
    await openColors();

    await waitFor(() => {
      expect(sidebarBrand().querySelector('img')).toHaveAttribute('src', logoUrl);
    });
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
describe('Botões em andamento em Visual', () => {
  it('Formulário em andamento ao salvar', async () => {
    const held = heldAnswer();
    // Held first, then the saving of the simulated API.
    server.use(
      http.put('/api/installation/name', async () => {
        await held.until;
      }),
    );
    await openColors();
    fireEvent.change(nameField(), { target: { value: 'Projetos da Empresa' } });
    // Salvar sits in the bar under the page, outside the form, and is busy as well.
    const save = saveButton();

    fireEvent.click(save);

    await waitFor(() => {
      expectBusy(save, visual.save);
    });
    fireEvent.click(save);
    held.release();
    expect(await screen.findByText(visual.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT name Projetos da Empresa']);
  });
});
