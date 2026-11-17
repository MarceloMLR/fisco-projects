import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import indexHtml from '../../index.html?raw';
import ptBR from '../locales/pt-BR.json';
import { productFavicon, productLogo, productName, productSymbol, showsProductLogo } from './brand';
import { ProductMark } from './ProductMark';

// The files of the brand live in docs/marca (ADR-0159); the application carries copies, never redrawn.
const brandFolder = resolve(process.cwd(), '../docs/marca');
const fileOf = (path: string) => readFileSync(resolve(process.cwd(), path));

// Spec: configuracao-da-instalacao, requirement "Marca do produto sem logo da instalação" (ADR-0160).
describe('Marca do produto sem logo da instalação', () => {
  it('Arquivos iguais aos da marca', () => {
    for (const name of [
      'gerenware-icone.svg',
      'gerenware-icone-branca.svg',
      'gerenware-icone-quadrado-cheio.svg',
      'gerenware-horizontal-fundo-claro.svg',
      'gerenware-horizontal-fundo-escuro.svg',
    ]) {
      expect(
        fileOf(`src/assets/brand/${name}`).equals(fileOf(`${brandFolder}/svg/${name}`)),
        name,
      ).toBe(true);
    }
    // The logo of the top of the e-mails, which the API points to by Product.LogoPath.
    expect(
      fileOf('public/gerenware-horizontal-fundo-claro-64px.png').equals(
        fileOf(`${brandFolder}/png/gerenware-horizontal-fundo-claro-64px.png`),
      ),
    ).toBe(true);
    for (const name of [
      'favicon.svg',
      'favicon.ico',
      'favicon-32.png',
      'apple-touch-icon.png',
      'android-chrome-192.png',
      'android-chrome-512.png',
      'android-chrome-maskable-192.png',
      'android-chrome-maskable-512.png',
      'site.webmanifest',
    ]) {
      expect(fileOf(`public/${name}`).equals(fileOf(`${brandFolder}/favicon/${name}`)), name).toBe(
        true,
      );
    }
  });

  it('Logo inteira com o nome do produto', () => {
    // The name the screens write when the brand does not come is the one of the product.
    expect(ptBR.app.name).toBe(productName);

    render(<ProductMark name={productName} dark={false} height={28} />);

    const logo = screen.getByRole('img', { name: productName });
    expect(logo).toHaveAttribute('src', productLogo(false));
    expect(productLogo(true)).not.toBe(productLogo(false));
    expect(showsProductLogo(productName, null)).toBe(true);
  });

  it('Logo para o fundo escuro', () => {
    render(<ProductMark name={productName} dark height={32} />);

    expect(screen.getByRole('img', { name: productName })).toHaveAttribute(
      'src',
      productLogo(true),
    );
  });

  it('Símbolo ao lado de outro nome', () => {
    const { container } = render(<ProductMark name="Empresa Exemplo" dark height={28} />);

    // Decoration: the name is written beside it.
    const symbol = container.querySelector('img');
    expect(symbol).toHaveAttribute('src', productSymbol);
    expect(symbol).toHaveAttribute('alt', '');
    expect(showsProductLogo('Empresa Exemplo', null)).toBe(false);
    expect(showsProductLogo(productName, '/api/installation/logo?v=1')).toBe(false);
  });

  it('Ícones do navegador e do celular', () => {
    expect(indexHtml).toContain(
      `<link rel="icon" href="${productFavicon}" type="image/svg+xml" />`,
    );
    expect(indexHtml).toContain('<link rel="apple-touch-icon" href="/apple-touch-icon.png" />');
    expect(indexHtml).toContain('<link rel="manifest" href="/site.webmanifest" />');

    const manifest = JSON.parse(fileOf('public/site.webmanifest').toString('utf8')) as {
      name: string;
      icons: { src: string; purpose: string }[];
    };
    expect(manifest.name).toBe(productName);
    for (const icon of manifest.icons) {
      expect(() => fileOf(`public${icon.src}`), icon.src).not.toThrow();
    }
    expect(manifest.icons.some((icon) => icon.purpose === 'maskable')).toBe(true);
  });
});
