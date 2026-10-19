import Box from '@mui/material/Box';
import { initials } from './initials';

/**
 * The logo of a company, such as a client, beside its name (fp-logo-tile), or the initials of the name without one.
 * It is decoration: the name beside it says the same.
 */
export function LogoTile({ name, src }: { name: string; src?: string | null }) {
  return (
    <Box
      component="span"
      aria-hidden
      data-testid="logo-tile"
      sx={{
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        boxSizing: 'border-box',
        width: 32,
        height: 32,
        padding: src === null || src === undefined ? 0 : '3px',
        overflow: 'hidden',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
        color: 'var(--text-secondary)',
        fontSize: 11,
        lineHeight: '16px',
        fontWeight: 500,
      }}
    >
      {src === null || src === undefined ? (
        initials(name)
      ) : (
        <Box
          component="img"
          src={src}
          alt=""
          sx={{ display: 'block', maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
        />
      )}
    </Box>
  );
}
