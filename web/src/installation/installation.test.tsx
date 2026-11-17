import { screen, waitFor, within } from '@testing-library/react';
import { delay, http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { productFavicon, productLogo, productName, productSymbol } from './brand';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import { server } from '../test/server';
import { color } from '../theme/tokens';

const navigationName = ptBR.shell.mainNavigation;
const logoUrl = '/api/installation/logo?v=639000000000000000';

function signedOut() {
  server.use(http.get('/api/session', () => new HttpResponse(null, { status: 401 })));
}

function installation(name: string, logo: string | null = null) {
  server.use(
    http.get('/api/installation', () =>
      HttpResponse.json({ name, logoUrl: logo, mainColor: null }),
    ),
  );
}

/** The brand at the top of the sidebar: the link to the home page, with the name and the logo or the badge. */
async function sidebarBrand(name: string) {
  const sidebar = screen.getByRole('navigation', { name: navigationName });
  return (await within(sidebar).findByText(name)).closest('a') as HTMLElement;
}

// Spec: configuracao-da-instalacao, requirements "Nome da instalação", "Logo da instalação" and "Marca visível sem
// sessão" (screens that show the brand).
describe('Marca da instalação', () => {
  beforeEach(() => {
    // The tab title is of the document, which the tests share.
    document.title = '';
  });

  it('Nome padrão', async () => {
    await renderApp('/');

    // The name of the product: the whole logo says it, with no text beside it (ADR-0160).
    const sidebar = screen.getByRole('navigation', { name: navigationName });
    const logo = await within(sidebar).findByRole('img', { name: productName });
    expect(logo.closest('a')).toHaveAttribute('href', '/');
    await waitFor(() => {
      expect(document.title).toBe(productName);
    });
  });

  it('Nome da instalação na barra lateral e na aba', async () => {
    installation('Consultoria Fictícia');

    await renderApp('/');

    expect(await sidebarBrand('Consultoria Fictícia')).toBeInTheDocument();
    expect(screen.queryByRole('img', { name: productName })).not.toBeInTheDocument();
    await waitFor(() => {
      expect(document.title).toBe('Consultoria Fictícia');
    });
  });

  it('Logo no lugar do selo', async () => {
    installation('Consultoria Fictícia', logoUrl);

    await renderApp('/');

    const brand = await sidebarBrand('Consultoria Fictícia');
    // The logo is decoration beside the name: no text of its own.
    expect(brand.querySelector('img')).toHaveAttribute('src', logoUrl);
    expect(brand.querySelector('img')).toHaveAttribute('alt', '');
    expect(brand.querySelector('svg')).toBeNull();
  });

  it('Símbolo ao lado de outro nome', async () => {
    installation('Consultoria Fictícia');

    await renderApp('/');

    // The symbol of the product in the place of the logo, as decoration beside the name (ADR-0160).
    const brand = await sidebarBrand('Consultoria Fictícia');
    expect(brand.querySelector('img')).toHaveAttribute('src', productSymbol);
    expect(brand.querySelector('img')).toHaveAttribute('alt', '');
    expect(brand.querySelector('svg')).toBeNull();
  });

  it('Logo inteira com o nome do produto', async () => {
    installation(productName);

    await renderApp('/');

    // The whole logo says the name: it is not written beside it.
    const logo = await screen.findByRole('img', { name: productName });
    expect(logo).toHaveAttribute('src', productLogo(false));
    expect(logo.closest('a')).toHaveTextContent('');
  });

  it('Marca na página de entrada', async () => {
    signedOut();
    // The brand comes after the answer that there is no session, as in a browser.
    server.use(
      http.get('/api/installation', async () => {
        await delay(50);
        return HttpResponse.json({ name: 'Consultoria Fictícia', logoUrl, mainColor: null });
      }),
    );

    await renderApp('/sign-in');

    const name = await screen.findByText('Consultoria Fictícia');
    expect(name.querySelector('img')).toHaveAttribute('src', logoUrl);
    await waitFor(() => {
      expect(document.title).toBe('Consultoria Fictícia');
    });
  });

  it('Frase da página de entrada', async () => {
    signedOut();
    installation(productName);

    await renderApp('/sign-in');

    expect(
      await screen.findByText('Implantação e suporte dos seus clientes em um só lugar.'),
    ).toBeInTheDocument();
    // The panel is always dark: the whole logo made for a dark background, with no name beside it.
    const logo = await screen.findByRole('img', { name: productName });
    expect(logo).toHaveAttribute('src', productLogo(true));
    expect(screen.queryByText(productName)).toBeNull();
  });

  it('Nome do produto se a marca não chega', async () => {
    signedOut();
    server.use(http.get('/api/installation', () => new HttpResponse(null, { status: 500 })));

    await renderApp('/sign-in');

    expect(await screen.findByRole('img', { name: ptBR.app.name })).toBeInTheDocument();
  });
});

// Colors as the Administrator types them, without "#": the colors in the code come only from tokens.json.
const hash = (value: string) => `#${value}`;

/** The rules of the main color the application applies, joined; empty without a color of its own. */
function brandRules() {
  return [...document.querySelectorAll('style')]
    .map((style) => style.textContent)
    .filter(
      (css) =>
        css.includes('[data-theme="dark"][data-theme="dark"]') ||
        css.includes(':root:not([data-theme="dark"])'),
    )
    .join('\n');
}

// Spec: configuracao-da-instalacao, requirement "Cor principal" (every screen).
describe('Cor principal da instalação', () => {
  it('Cor padrão', async () => {
    await renderApp('/');

    expect(await screen.findByRole('img', { name: productName })).toBeInTheDocument();
    expect(brandRules()).toBe('');
  });

  it('Cor principal na estrutura e na entrada', async () => {
    server.use(
      http.get('/api/installation', () =>
        HttpResponse.json({
          name: 'Consultoria Fictícia',
          logoUrl: null,
          mainColor: hash('7A1F3D'),
        }),
      ),
    );
    const { unmount } = await renderApp('/');
    await waitFor(() => {
      expect(brandRules()).toMatch(new RegExp(`--action:\\s*${hash('7a1f3d')};`));
    });
    unmount();

    signedOut();
    await renderApp('/sign-in');
    await waitFor(() => {
      expect(brandRules()).toMatch(new RegExp(`--brand-panel-bg-2:\\s*${hash('7a1f3d')};`));
    });
  });
});

// Spec: configuracao-da-instalacao, requirements "Cor secundária", "Cor da barra lateral" and "Logo para o tema
// escuro" (every screen, ADR-0146).
describe('Cores da instalação nas telas', () => {
  it('Trocar a cor secundária', async () => {
    server.use(
      http.get('/api/installation', () =>
        HttpResponse.json({
          name: 'Consultoria Fictícia',
          logoUrl: null,
          mainColor: null,
          darkLogoUrl: null,
          secondaryColor: hash('C0395A'),
          sidebarColor: null,
        }),
      ),
    );
    signedOut();

    await renderApp('/sign-in');

    await waitFor(() => {
      expect(brandRules()).toMatch(new RegExp(`--brand-panel-bg-2:\\s*${hash('c0395a')};`));
    });
    expect(brandRules()).toMatch(/--bg-selected:/);
    // The main button stays in the main color of the design system.
    expect(brandRules()).toMatch(new RegExp(`--action:\\s*${color('action', 'light')};`));
  });

  it('Logo escuro na barra lateral escura do tema claro', async () => {
    const darkLogoUrl = '/api/installation/logo/dark?v=1';
    server.use(
      http.get('/api/installation', () =>
        HttpResponse.json({
          name: 'Consultoria Fictícia',
          logoUrl,
          mainColor: null,
          darkLogoUrl,
          secondaryColor: null,
          sidebarColor: hash('1F2933'),
        }),
      ),
    );

    await renderApp('/');

    const brand = await sidebarBrand('Consultoria Fictícia');
    await waitFor(() => {
      expect(brand.querySelector('img')).toHaveAttribute('src', darkLogoUrl);
    });
    expect(brandRules()).toMatch(new RegExp(`--sidebar-bg:\\s*${hash('1f2933')};`));
    expect(brandRules()).toMatch(/--sidebar-title:/);
  });
});

/** The icon of the tab of the browser, as the page has it. */
function favicon() {
  return document.head.querySelector<HTMLLinkElement>('link[rel="icon"]')?.getAttribute('href');
}

// Spec: configuracao-da-instalacao, requirement "Ícone da aba do navegador" (ADR-0108).
describe('Ícone da aba do navegador', () => {
  beforeEach(() => {
    document.head.querySelector('link[rel="icon"]')?.remove();
  });

  it('Logo na aba', async () => {
    installation('Consultoria Fictícia', logoUrl);

    await renderApp('/');

    await waitFor(() => {
      expect(favicon()).toBe(logoUrl);
    });
  });

  it('Símbolo na aba sem logo', async () => {
    signedOut();
    installation('Consultoria Fictícia');

    await renderApp('/sign-in');

    // The symbol of the product, the icon the page starts with (ADR-0160).
    await waitFor(() => {
      expect(favicon()).toBe(productFavicon);
    });
  });
});
