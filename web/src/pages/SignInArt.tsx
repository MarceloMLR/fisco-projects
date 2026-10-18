import Box from '@mui/material/Box';

const dots = [0, 20, 40, 60, 80].flatMap((x) => [0, 20, 40, 60].map((y) => ({ x, y })));

/**
 * Shapes of the brand panel (fp-signin__art): rings, a grid of dots and stacked sheets, one done and with
 * progress. Decoration only, in the brand panel tokens.
 */
export function SignInArt() {
  return (
    <Box
      component="svg"
      viewBox="0 0 600 640"
      preserveAspectRatio="xMidYMax slice"
      aria-hidden="true"
      sx={{
        position: 'absolute',
        inset: 0,
        zIndex: -1,
        width: '100%',
        height: '100%',
        '& .ring': { fill: 'none', stroke: 'var(--brand-panel-shape)', strokeWidth: 1.25 },
        '& .dot': { fill: 'var(--brand-panel-shape)' },
        '& .sheet': {
          fill: 'var(--brand-panel-fill)',
          stroke: 'var(--brand-panel-shape)',
          strokeWidth: 1,
        },
        '& .row': { fill: 'var(--brand-panel-shape)' },
        '& .row--strong': { fill: 'var(--brand-panel-muted)', opacity: 0.55 },
        '& .ok': { fill: 'none', stroke: 'var(--brand-panel-accent)', strokeWidth: 2 },
        '& .bar': { fill: 'var(--brand-panel-accent)' },
      }}
    >
      {[150, 230, 310, 390].map((r) => (
        <circle key={r} className="ring" cx="590" cy="640" r={r} />
      ))}
      <g transform="translate(470 56)">
        {dots.map(({ x, y }) => (
          <circle key={`${String(x)}-${String(y)}`} className="dot" cx={x} cy={y} r="2" />
        ))}
      </g>
      <g transform="translate(48 418) rotate(-7)">
        <rect className="sheet" x="72" y="-56" width="320" height="132" rx="14" />
        <rect className="row" x="96" y="-32" width="150" height="6" rx="3" />
        <rect className="sheet" x="36" y="-18" width="320" height="132" rx="14" />
        <rect className="row" x="60" y="6" width="190" height="6" rx="3" />
        <rect className="row" x="60" y="22" width="120" height="6" rx="3" />
        <rect className="sheet" x="0" y="22" width="320" height="132" rx="14" />
        <circle className="ok" cx="30" cy="56" r="10" />
        <path className="ok" d="M25 56 l4 4 l7 -8" />
        <rect className="row row--strong" x="52" y="47" width="170" height="7" rx="3.5" />
        <rect className="row" x="52" y="62" width="110" height="6" rx="3" />
        <rect className="row" x="20" y="118" width="280" height="6" rx="3" />
        <rect className="bar" x="20" y="118" width="196" height="6" rx="3" />
      </g>
    </Box>
  );
}
