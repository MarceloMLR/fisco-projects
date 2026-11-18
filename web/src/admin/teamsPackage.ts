import { zip } from '../format/xlsx';
import { productSymbolFull, productSymbolWhite } from '../installation/brand';
import { color } from '../theme/tokens';

/** The brand the icons of the package show: the logo of the installation or, without it, the symbol of the product. */
export interface TeamsIconBrand {
  logoUrl: string | null;
}

/** The names of the files of the package, the ones the manifest gives (TeamsAppManifest, ADR-0149). */
export const teamsPackageFiles = {
  manifest: 'manifest.json',
  color: 'color.png',
  outline: 'outline.png',
} as const;

/** The color icon, full-bleed, and the outline icon, as Teams asks for them. */
const colorSize = 192;
const outlineSize = 32;
/** The logo fits a box with a margin of 24 px. */
const logoBox = 144;

/** What the icons show without a logo: the symbol of the product, in the version of its files. */
const symbolSource = 'gerenware-symbol-1';

/**
 * What the icons show, sent with the request of the package so the version goes up when it changes (ADR-0149): the
 * address of the logo, which has its version, or the symbol of the product; and the outline, which is the symbol for
 * every installation.
 */
export function iconSourceOf(brand: TeamsIconBrand): string {
  return `${brand.logoUrl ?? symbolSource} ${symbolSource}`;
}

/**
 * The package of the application of Teams of the installation, as a .zip (ADR-0149, ADR-0160): the manifest the
 * server made, the color icon (the logo on white or, without it, the symbol of the product with the full square, as
 * Teams rounds the corners itself) and the outline icon (the symbol in white on transparent), drawn on the screen from
 * the files of the brand, never redrawn.
 */
export async function teamsPackage(manifest: string, brand: TeamsIconBrand): Promise<Blob> {
  const outline = await imageOf(productSymbolWhite);
  const files: [string, Uint8Array<ArrayBuffer>][] = [
    [teamsPackageFiles.manifest, new TextEncoder().encode(manifest)],
    [teamsPackageFiles.color, await colorIcon(brand)],
    [
      teamsPackageFiles.outline,
      pngOf(outlineSize, (context) => {
        context.drawImage(outline, 0, 0, outlineSize, outlineSize);
      }),
    ],
  ];
  return new Blob([await zip(files)], { type: 'application/zip' });
}

async function colorIcon(brand: TeamsIconBrand): Promise<Uint8Array<ArrayBuffer>> {
  if (brand.logoUrl === null) {
    const symbol = await imageOf(productSymbolFull);
    return pngOf(colorSize, (context) => {
      context.drawImage(symbol, 0, 0, colorSize, colorSize);
    });
  }
  const logo = await imageOf(brand.logoUrl);
  return pngOf(colorSize, (context) => {
    context.fillStyle = white();
    context.fillRect(0, 0, colorSize, colorSize);
    const width = logo.naturalWidth || logoBox;
    const height = logo.naturalHeight || logoBox;
    const scale = Math.min(logoBox / width, logoBox / height);
    const drawnWidth = width * scale;
    const drawnHeight = height * scale;
    context.drawImage(
      logo,
      (colorSize - drawnWidth) / 2,
      (colorSize - drawnHeight) / 2,
      drawnWidth,
      drawnHeight,
    );
  });
}

/** White, as Teams asks behind a logo: the surface of the light theme. */
function white(): string {
  return color('bg-surface', 'light');
}

/** A square image of the size, drawn on a canvas, as the bytes of a PNG. */
function pngOf(
  size: number,
  draw: (context: CanvasRenderingContext2D) => void,
): Uint8Array<ArrayBuffer> {
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const context = canvas.getContext('2d');
  if (context === null) {
    throw new Error('The browser cannot draw the icons of the package.');
  }
  draw(context);
  const data = canvas.toDataURL('image/png');
  const binary = atob(data.slice(data.indexOf(',') + 1));
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index++) {
    bytes[index] = binary.charCodeAt(index);
  }
  return bytes;
}

/** An image of the application (the logo of the installation or a file of the brand), loaded to draw. */
function imageOf(address: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => {
      resolve(image);
    };
    image.onerror = () => {
      reject(new Error('The image of the icon could not be loaded.'));
    };
    image.src = address;
  });
}
