import { color } from '../theme/tokens';
import type { XlsxColors } from './xlsx';

/** A color of the tokens, light theme, as RRGGBB. */
const tokenColor = (name: string) => color(name, 'light').replace(/^#/, '');

/** The colors of the spreadsheets of the screens (ADR-0099), from the tokens of the light theme. */
export const xlsxColors: XlsxColors = {
  headerFill: tokenColor('bg-subtle'),
  headerText: tokenColor('text'),
  border: tokenColor('border'),
};
