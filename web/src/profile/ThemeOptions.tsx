import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import type { ThemePreference } from '../api/client';
import { useThemeOptions } from './themeChoice';

/** A line of the picture, as text or a list item would be. */
function Bar({ height, color, width = '100%' }: { height: number; color: string; width?: string }) {
  return (
    <Box
      component="i"
      sx={{ display: 'block', height, width, borderRadius: '2px', backgroundColor: color }}
    />
  );
}

/**
 * A small picture of the screen in one theme (fp-theme-opt__thumb): sidebar, page, a field and the action, drawn
 * with the tokens of that theme whatever the theme of the screen around it.
 */
function Thumbnail({ scheme }: { scheme: 'light' | 'dark' }) {
  return (
    <Box
      component="span"
      data-theme={scheme}
      aria-hidden
      sx={{
        display: 'grid',
        gridTemplateColumns: '22% 1fr',
        gridTemplateRows: '12px 1fr',
        height: 76,
        borderRadius: 'var(--radius-md)',
        overflow: 'hidden',
        backgroundColor: 'var(--sidebar-bg)',
      }}
    >
      <Box component="span" sx={{ gridColumn: '1 / -1', backgroundColor: 'var(--sidebar-bg)' }} />
      <Box
        component="span"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: '4px',
          padding: '6px 4px',
          backgroundColor: 'var(--sidebar-bg)',
        }}
      >
        <Bar height={4} color="var(--sidebar-active-bg)" />
        <Bar height={4} color="var(--sidebar-hover-bg)" width="70%" />
      </Box>
      <Box
        component="span"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: '4px',
          margin: '0 5px 5px 0',
          padding: '6px',
          borderRadius: '4px',
          backgroundColor: 'var(--bg-page)',
        }}
      >
        <Bar height={5} color="var(--text)" width="50%" />
        <Box
          component="i"
          sx={{
            display: 'block',
            height: 14,
            borderRadius: '3px',
            border: '1px solid var(--border)',
            backgroundColor: 'var(--bg-surface)',
          }}
        />
        <Bar height={6} color="var(--action)" width="40%" />
      </Box>
    </Box>
  );
}

/**
 * The themes as cards with a picture (fp-opts and fp-theme-opt), as in My profile of the canvas: a radio group, so
 * the arrow keys move between them; Automatic shows the light and the dark halves.
 */
export function ThemeOptions({
  label,
  value,
  onChange,
}: {
  /** Accessible name of the group. */
  label: string;
  value: ThemePreference;
  onChange: (theme: ThemePreference) => void;
}) {
  const options = useThemeOptions();

  return (
    <Box
      role="radiogroup"
      aria-label={label}
      sx={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 220px))',
        gap: 'var(--space-3)',
      }}
    >
      {options.map((option) => {
        const checked = option.value === value;
        return (
          <Box
            key={option.value}
            component="label"
            data-checked={checked}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-2)',
              boxSizing: 'border-box',
              padding: 'var(--space-2)',
              border: '1px solid var(--border-control)',
              borderRadius: 'var(--radius-lg)',
              backgroundColor: 'var(--bg-surface)',
              cursor: 'pointer',
              '&[data-checked="true"]': {
                borderColor: 'var(--control-selected)',
                boxShadow: 'inset 0 0 0 1px var(--control-selected)',
              },
              '&:has(input:focus-visible)': {
                outline: '2px solid var(--focus-ring)',
                outlineOffset: '2px',
              },
            }}
          >
            <Box
              component="input"
              type="radio"
              name="theme"
              value={option.value}
              checked={checked}
              onChange={() => {
                onChange(option.value);
              }}
              sx={visuallyHidden}
            />
            {option.value === 'Automatic' ? (
              <Box
                component="span"
                sx={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr',
                  borderRadius: 'var(--radius-md)',
                  overflow: 'hidden',
                  '& > span': { borderRadius: 0 },
                }}
              >
                <Thumbnail scheme="light" />
                <Thumbnail scheme="dark" />
              </Box>
            ) : (
              <Thumbnail scheme={option.value === 'Dark' ? 'dark' : 'light'} />
            )}
            <Box
              component="span"
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                padding: '0 var(--space-1) var(--space-1)',
                fontSize: 13,
                lineHeight: '18px',
                fontWeight: 500,
                color: 'var(--text)',
              }}
            >
              <Box
                component="span"
                aria-hidden
                sx={{
                  flex: 'none',
                  width: 18,
                  height: 18,
                  boxSizing: 'border-box',
                  border: checked
                    ? '5px solid var(--control-selected)'
                    : '2px solid var(--border-control)',
                  borderRadius: 'var(--radius-full)',
                  backgroundColor: 'var(--bg-surface)',
                }}
              />
              {option.label}
            </Box>
          </Box>
        );
      })}
    </Box>
  );
}
