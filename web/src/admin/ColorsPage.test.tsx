import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Installation } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { problem, server, signedInAs } from '../test/server';
import { colorFunction } from '../theme/colorFormats';

const text = ptBR.colors;

// Colors as the Administrator types them, without "#": colors in the code come only from tokens.json.
const hash = (value: string) => `#${value}`;

/** The installation as the simulated API keeps it, changed by the requests as the real one would. */
let installation: Installation;
const sent: unknown[] = [];

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
      installation = { ...installation, ...body };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openColors(width?: number) {
  const view = await renderApp('/admin/colors', width);
  await screen.findByRole('heading', { level: 1, name: ptBR.pages.colors.title });
  return view;
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
  return screen.getByRole('button', { name: ptBR.settings.save });
}

function discardButton() {
  return screen.getByRole('button', { name: ptBR.settings.discard });
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

// Spec: configuracao-da-instalacao, requirement "Tela Cores" (ADR-0146).
describe('Tela Cores', () => {
  it('Cores na Administração', async () => {
    await renderApp('/admin/users');
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const links = within(sections).getAllByRole('link');
    const settings = links.findIndex((link) => link.textContent === ptBR.pages.settings.title);
    // Right after General settings.
    expect(links[settings + 1]).toHaveTextContent(ptBR.pages.colors.title);

    fireEvent.click(within(sections).getByRole('link', { name: ptBR.pages.colors.title }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.colors.title }),
    ).toBeInTheDocument();
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

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(sent).toEqual([
      { mainColor: hash('0F766E'), secondaryColor: hash('14B8A6'), sidebarColor: hash('1F2933') },
    ]);
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

    fireEvent.click(discardButton());

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

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
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
    expect(await screen.findByText(text.saved)).toBeInTheDocument();
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

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
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
