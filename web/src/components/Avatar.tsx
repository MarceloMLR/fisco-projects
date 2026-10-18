import Box from '@mui/material/Box';
import { initials } from './initials';

const sizes = {
  sm: { size: 24, fontSize: 10 },
  md: { size: 32, fontSize: 12 },
  lg: { size: 40, fontSize: 14 },
} as const;

/** Round avatar with the initials (Avatar, fp-avatar). Decorative: the name is always written next to it. */
export function Avatar({ name, size = 'md' }: { name: string; size?: keyof typeof sizes }) {
  const { size: diameter, fontSize } = sizes[size];

  return (
    <Box
      component="span"
      aria-hidden="true"
      sx={{
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        width: diameter,
        height: diameter,
        borderRadius: 'var(--radius-full)',
        backgroundColor: 'var(--avatar-bg)',
        color: 'var(--avatar-text)',
        fontSize,
        lineHeight: 1,
        fontWeight: 500,
        letterSpacing: '0.02em',
        userSelect: 'none',
      }}
    >
      {initials(name)}
    </Box>
  );
}
