import { zip } from '../format/xlsx';
import { timelinePaths } from '../installation/favicon';
import { brandTokens } from '../theme/brandColors';
import { color } from '../theme/tokens';

/** The brand the icons of the package show: the logo of the installation or, without it, its main color. */
export interface TeamsIconBrand {
  logoUrl: string | null;
  mainColor: string | null;
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
/** The logo fits a box with a margin of 24 px; the icon of the timeline fits the safe area of 120 px in the middle. */
const logoBox = 144;
const glyphBox = 160;

/**
 * What the icons show, sent with the request of the package so the version goes up when it changes (ADR-0149): the
 * address of the logo, which has its version, or the color of the badge.
 */
export function iconSourceOf(brand: TeamsIconBrand): string {
  return brand.logoUrl ?? brandTokens(brand.mainColor).light.action;
}

/**
 * The package of the application of Teams of the installation, as a .zip (ADR-0149): the manifest the server made,
 * the color icon (the logo on white or, without it, the badge in the main color, as the tab of the browser shows it)
 * and the outline icon (the icon of the timeline in white on transparent), drawn on the screen.
 */
export async function teamsPackage(manifest: string, brand: TeamsIconBrand): Promise<Blob> {
  const files: [string, Uint8Array<ArrayBuffer>][] = [
    [teamsPackageFiles.manifest, new TextEncoder().encode(manifest)],
    [teamsPackageFiles.color, await colorIcon(brand)],
    [
      teamsPackageFiles.outline,
      // The box of 24 of the icon already has its margin of 3.
      pngOf(outlineSize, (context) => {
        glyph(context, 0, outlineSize, white());
      }),
    ],
  ];
  return new Blob([await zip(files)], { type: 'application/zip' });
}

async function colorIcon(brand: TeamsIconBrand): Promise<Uint8Array<ArrayBuffer>> {
  if (brand.logoUrl !== null) {
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

  const { action, 'on-action': onAction } = brandTokens(brand.mainColor).light;
  return pngOf(colorSize, (context) => {
    context.fillStyle = action;
    context.fillRect(0, 0, colorSize, colorSize);
    glyph(context, (colorSize - glyphBox) / 2, glyphBox, onAction);
  });
}

/** The icon of the timeline, drawn from its box of 24 into a square of the size, at the offset from the corner. */
function glyph(context: CanvasRenderingContext2D, offset: number, size: number, fill: string) {
  context.save();
  context.translate(offset, offset);
  context.scale(size / 24, size / 24);
  context.fillStyle = fill;
  for (const path of timelinePaths) {
    context.fill(new Path2D(path));
  }
  context.restore();
}

/** White, as Teams asks for the outline and behind a logo: the surface of the light theme. */
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

/** The logo of the installation, loaded from its address in the application, to draw. */
function imageOf(address: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => {
      resolve(image);
    };
    image.onerror = () => {
      reject(new Error('The logo of the installation could not be loaded.'));
    };
    image.src = address;
  });
}
