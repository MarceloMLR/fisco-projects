import Box from '@mui/material/Box';

/**
 * The logo the installation sent (fp-sidebar__logo): it takes the place of the badge, at the height of the badge.
 * It is decoration beside the name, which says the same.
 */
export function InstallationLogo({
  src,
  maxHeight,
  maxWidth = 160,
}: {
  src: string;
  maxHeight: number;
  maxWidth?: number;
}) {
  return (
    <Box
      component="img"
      src={src}
      alt=""
      // A fixed height, since an SVG without its own size would have none; a wide logo stops at the width.
      sx={{
        display: 'block',
        flex: 'none',
        height: maxHeight,
        width: 'auto',
        maxWidth,
        objectFit: 'contain',
      }}
    />
  );
}
