import PersonOutlined from '@mui/icons-material/PersonOutlined';
import Box from '@mui/material/Box';
import { initials } from './initials';

const sizes = {
  sm: { size: 24, fontSize: 10, icon: 14 },
  md: { size: 32, fontSize: 12, icon: 18 },
  lg: { size: 40, fontSize: 14, icon: 18 },
} as const;

/**
 * Round avatar with the initials (Avatar, fp-avatar). Decorative: the name is always written next to it. Without a
 * name it is the dashed avatar of the design system's empty Person, as for the system on the Audit screen.
 */
export function Avatar({
  name,
  size = 'md',
  tone,
}: {
  name?: string;
  size?: keyof typeof sizes;
  /** The color of the part of the person in a ticket (avatar-*, ADR-0069), with white initials. */
  tone?: 'owner' | 'manager' | 'techlead' | 'dev' | 'functional';
}) {
  const { size: diameter, fontSize, icon } = sizes[size];
  const empty = name === undefined;
  const toned = !empty && tone !== undefined;

  return (
    <Box
      component="span"
      aria-hidden="true"
      sx={{
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        boxSizing: 'border-box',
        width: diameter,
        height: diameter,
        borderRadius: 'var(--radius-full)',
        backgroundColor: empty
          ? 'transparent'
          : toned
            ? `var(--avatar-${tone})`
            : 'var(--avatar-bg)',
        border: empty ? '1px dashed var(--border-control)' : 0,
        color: empty ? 'var(--text-secondary)' : toned ? 'var(--on-action)' : 'var(--avatar-text)',
        fontSize,
        lineHeight: 1,
        fontWeight: 500,
        letterSpacing: '0.02em',
        userSelect: 'none',
      }}
    >
      {empty ? <PersonOutlined sx={{ fontSize: icon }} /> : initials(name)}
    </Box>
  );
}
