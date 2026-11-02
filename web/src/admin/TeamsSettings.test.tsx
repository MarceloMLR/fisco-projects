import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { TeamsApp } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { brandTokens } from '../theme/brandColors';
import { renderApp } from '../test/renderApp';
import { defaultInstallation, problem, server, signedInAs } from '../test/server';
import { bytesOf, unzip } from '../test/unzip';

const saved = vi.hoisted(() => ({ files: [] as { content: Blob; name: string }[] }));

// The browser saves the package; the tests keep it to open.
vi.mock('../format/saveFile', () => ({
  saveFile: (content: Blob, name: string) => {
    saved.files.push({ content, name });
  },
}));

const text = ptBR.settings.teams;
const manifest = '{"manifestVersion":"1.17","version":"1.0.1"}';

/** The application of Teams as the simulated API keeps it. */
let app: TeamsApp;
let turns: boolean[];
let packages: string[];
/** What each canvas drew: its size and the calls to draw an image, a path or a rectangle. */
let drawn: { size: number; images: number; paths: number; fills: string[] }[];

beforeEach(() => {
  app = { available: true, noticesOn: false, version: null };
  turns = [];
  packages = [];
  drawn = [];
  saved.files.length = 0;
  signedInAs('Administrator');
  // jsdom does not draw: a canvas that keeps what it was asked and gives the bytes of its size.
  vi.stubGlobal(
    'Path2D',
    class {
      constructor(readonly path: string) {}
    },
  );
  vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockImplementation(function (
    this: HTMLCanvasElement,
  ) {
    const record = { size: this.width, images: 0, paths: 0, fills: [] as string[] };
    drawn.push(record);
    const context = {
      fillStyle: '',
      save: () => undefined,
      restore: () => undefined,
      translate: () => undefined,
      scale: () => undefined,
      fillRect: () => {
        record.fills.push(context.fillStyle);
      },
      fill: () => {
        record.paths++;
      },
      drawImage: () => {
        record.images++;
      },
    };
    return context as unknown as CanvasRenderingContext2D;
  });
  vi.spyOn(HTMLCanvasElement.prototype, 'toDataURL').mockImplementation(function (
    this: HTMLCanvasElement,
  ) {
    return `data:image/png;base64,${btoa(`png ${String(this.width)}`)}`;
  });
  server.use(
    http.get('/api/admin/teams-app', () => HttpResponse.json(app)),
    http.put('/api/admin/teams-app', async ({ request }) => {
      const body = (await request.json()) as { noticesOn: boolean };
      turns.push(body.noticesOn);
      app = { ...app, noticesOn: body.noticesOn };
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/admin/teams-app/package', async ({ request }) => {
      const body = (await request.json()) as { iconSource: string };
      packages.push(body.iconSource);
      app = { ...app, version: '1.0.1' };
      return HttpResponse.json({ manifest, version: '1.0.1' });
    }),
  );
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

async function openSection() {
  await renderApp('/admin/settings');
  return screen.findByRole('region', { name: ptBR.settings.m365 });
}

// Spec: configuracao-da-instalacao, requirement "Aplicativo do Teams".
describe('Aplicativo do Teams', () => {
  it('Pacote do administrador', async () => {
    const section = await openSection();
    await waitFor(() => {
      expect(within(section).getByRole('textbox', { name: text.package })).toHaveAttribute(
        'placeholder',
        text.neverDownloaded,
      );
    });

    fireEvent.click(within(section).getByRole('button', { name: text.download }));

    expect(
      await screen.findByText(text.downloaded.replace('{{version}}', '1.0.1')),
    ).toBeInTheDocument();
    // Without a logo, the icons show the badge in the main color (the one of the design system here).
    expect(packages).toEqual([brandTokens(null).light.action]);
    expect(saved.files.map(({ name }) => name)).toEqual([
      text.fileName.replace('{{version}}', '1.0.1'),
    ]);
    const files = await unzip(await bytesOf(saved.files[0]?.content ?? new Blob()));
    expect([...files.keys()]).toEqual(['manifest.json', 'color.png', 'outline.png']);
    expect(files.get('manifest.json')).toBe(manifest);
    expect(files.get('color.png')).toBe('png 192');
    expect(files.get('outline.png')).toBe('png 32');
    expect(drawn).toEqual([
      { size: 192, images: 0, paths: 2, fills: [brandTokens(null).light.action] },
      { size: 32, images: 0, paths: 2, fills: [] },
    ]);
    await waitFor(() => {
      expect(within(section).getByRole('textbox', { name: text.package })).toHaveValue(
        text.version.replace('{{version}}', '1.0.1'),
      );
    });
  });

  it('Ícone com o logo da instalação', async () => {
    const logoUrl = '/api/installation/logo?v=3';
    server.use(
      http.get('/api/installation', () => HttpResponse.json({ ...defaultInstallation, logoUrl })),
    );
    // jsdom does not load images: one that is ready as soon as it has its address.
    vi.stubGlobal(
      'Image',
      class {
        naturalWidth = 300;
        naturalHeight = 100;
        onload: (() => void) | null = null;
        set src(_: string) {
          queueMicrotask(() => this.onload?.());
        }
      },
    );
    const section = await openSection();

    fireEvent.click(await within(section).findByRole('button', { name: text.download }));

    await waitFor(() => {
      expect(saved.files).toHaveLength(1);
    });
    expect(packages).toEqual([logoUrl]);
    expect(drawn[0]).toMatchObject({ size: 192, images: 1, paths: 0 });
  });

  it('Pacote sem o Teams na instalação', async () => {
    app = { available: false, noticesOn: false, version: null };
    const section = await openSection();

    expect(await within(section).findByText(text.unavailable)).toBeInTheDocument();
    expect(within(section).getByRole('checkbox', { name: text.noticesOn })).toBeDisabled();
    expect(within(section).queryByRole('button', { name: text.download })).not.toBeInTheDocument();
  });

  it('Pacote recusado pela API', async () => {
    server.use(
      http.post('/api/admin/teams-app/package', () =>
        problem(409, 'urn:fiscoprojects:problem:teams-unavailable'),
      ),
    );
    const section = await openSection();

    fireEvent.click(await within(section).findByRole('button', { name: text.download }));

    expect(await screen.findByText(text.unavailable)).toBeInTheDocument();
    expect(saved.files).toHaveLength(0);
  });
});

// Spec: configuracao-da-instalacao, requirement "Avisar pelo Teams".
describe('Avisar pelo Teams', () => {
  it('Ligar os avisos pelo Teams', async () => {
    const section = await openSection();
    const box = await within(section).findByRole('checkbox', { name: text.noticesOn });
    await waitFor(() => {
      expect(box).toBeEnabled();
    });
    expect(box).not.toBeChecked();
    expect(within(section).getByText(text.noticesOnHint)).toBeInTheDocument();

    fireEvent.click(box);

    await waitFor(() => {
      expect(turns).toEqual([true]);
    });
    await waitFor(() => {
      expect(box).toBeChecked();
    });
  });

  it('Falha ao ligar os avisos pelo Teams', async () => {
    server.use(http.put('/api/admin/teams-app', () => problem(500)));
    const section = await openSection();
    const box = await within(section).findByRole('checkbox', { name: text.noticesOn });
    await waitFor(() => {
      expect(box).toBeEnabled();
    });

    fireEvent.click(box);

    expect(await screen.findByText(text.turnFailed)).toBeInTheDocument();
    await waitFor(() => {
      expect(box).not.toBeChecked();
    });
  });
});
