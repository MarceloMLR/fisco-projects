import Box from '@mui/material/Box';
import { productLogo, productName, productSymbol } from './brand';

/**
 * The mark of the product where the installation has no logo of its own (ADR-0160): the whole GerenWare logo when the
 * installation keeps the name of the product, which the logo already says, otherwise the symbol, beside the name the
 * caller writes. The colors of the mark never change with the colors of the installation.
 */
export function ProductMark({
  name,
  dark,
  height,
}: {
  /** The name of the installation. */
  name: string;
  /** Whether the background is dark, for the logo made for it. */
  dark: boolean;
  /** The height in px: the manual asks at least 24 for the logo and 16 for the symbol. */
  height: number;
}) {
  const whole = name === productName;

  return (
    <Box
      component="img"
      src={whole ? productLogo(dark) : productSymbol}
      // The whole logo says the name; the symbol is decoration beside the name.
      alt={whole ? productName : ''}
      sx={{ display: 'block', flex: 'none', height, width: whole ? 'auto' : height }}
    />
  );
}
