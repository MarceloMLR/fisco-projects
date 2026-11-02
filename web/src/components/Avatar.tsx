import PersonOutlined from '@mui/icons-material/PersonOutlined';
import Box from '@mui/material/Box';
import { useState } from 'react';
import { initials } from './initials';

const sizes = {
  sm: { size: 24, fontSize: 10, icon: 14 },
  md: { size: 32, fontSize: 12, icon: 18 },
  lg: { size: 40, fontSize: 14, icon: 18 },
} as const;

/**
 * Round avatar with the initials (Avatar, fp-avatar). Decorative: the name is always written next to it. Without a
 * name it is the dashed avatar of the design system's empty Person, as for the system on the Audit screen. With the
 * photo of the person in Microsoft 365, the photo covers the circle once it loads, without a ring in the color of the
 * part (ADR-0124); until then, and without it, the initials stay.
 */
export function Avatar({
  name,
  size = 'md',
  tone,
  photo,
}: {
  name?: string;
  size?: keyof typeof sizes;
  /** The color of the part of the person in a ticket (avatar-*, ADR-0069, ADR-0104), with white initials. */
  tone?: 'requester' | 'owner' | 'manager' | 'techlead' | 'dev' | 'functional';
  /** The address of the photo of the person (usePersonPhoto). */
  photo?: string;
}) {
  const { size: diameter, fontSize, icon } = sizes[size];
  const empty = name === undefined;
  const toned = !empty && tone !== undefined;
  // By the address, so another person's photo starts hidden again.
  const [loaded, setLoaded] = useState<string>();
  const [failed, setFailed] = useState<string>();
  const withPhoto = !empty && photo !== undefined && failed !== photo;

  return (
    <Box
      component="span"
      aria-hidden="true"
      sx={{
        position: 'relative',
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        overflow: 'hidden',
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
        color: empty
          ? 'var(--text-secondary)'
          : toned
            ? 'var(--on-avatar-role)'
            : 'var(--avatar-text)',
        fontSize,
        lineHeight: 1,
        fontWeight: 500,
        letterSpacing: '0.02em',
        userSelect: 'none',
      }}
    >
      {empty ? <PersonOutlined sx={{ fontSize: icon }} /> : initials(name)}
      {withPhoto && (
        <Box
          component="img"
          src={photo}
          alt=""
          draggable={false}
          onLoad={() => {
            setLoaded(photo);
          }}
          onError={() => {
            setFailed(photo);
          }}
          sx={{
            position: 'absolute',
            inset: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            borderRadius: 'inherit',
            opacity: loaded === photo ? 1 : 0,
          }}
        />
      )}
    </Box>
  );
}
