import Box from '@mui/material/Box';

/**
 * The logo the installation sent (fp-sidebar__logo): it takes the place of the badge, never taller than the badge.
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
      sx={{ display: 'block', flex: 'none', maxHeight, maxWidth, objectFit: 'contain' }}
    />
  );
}
