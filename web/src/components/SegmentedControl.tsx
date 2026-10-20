import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import { type KeyboardEvent, useRef } from 'react';

export interface SegmentedOption<Value extends string> {
  value: Value;
  label: string;
  icon?: typeof SvgIcon;
  /** How many items the option has, after the label (as "Todos 9" in the tab Chamados). */
  count?: number;
}

/**
 * Segmented control of the design system (SegmentedControl, fp-segmented): a few options side by side, one chosen.
 * It is a radio group, so the left and right arrows move between the options and choose them. With iconsOnly, each
 * option shows only its icon, and the label is its accessible name and tooltip (fp-segmented--icons).
 */
export function SegmentedControl<Value extends string>({
  label,
  value,
  options,
  onChange,
  iconsOnly = false,
}: {
  /** Accessible name of the group. */
  label: string;
  value: Value;
  options: readonly SegmentedOption<Value>[];
  onChange: (value: Value) => void;
  iconsOnly?: boolean;
}) {
  const buttons = useRef<(HTMLButtonElement | null)[]>([]);

  const move = (event: KeyboardEvent, index: number) => {
    // Left and right only: up and down stay with what holds the control, such as the items of a menu.
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (step === 0) {
      return;
    }
    event.preventDefault();
    const next = (index + step + options.length) % options.length;
    const option = options[next];
    if (option !== undefined) {
      onChange(option.value);
      buttons.current[next]?.focus();
    }
  };

  return (
    <Box
      role="radiogroup"
      aria-label={label}
      sx={{
        display: 'inline-flex',
        gap: '2px',
        padding: '2px',
        backgroundColor: 'var(--bg-subtle)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
      }}
    >
      {options.map((option, index) => {
        const checked = option.value === value;
        const Icon = option.icon;
        return (
          <Box
            key={option.value}
            component="button"
            type="button"
            role="radio"
            aria-checked={checked}
            aria-label={iconsOnly ? option.label : undefined}
            title={iconsOnly ? option.label : undefined}
            tabIndex={checked ? 0 : -1}
            ref={(element: HTMLButtonElement | null) => {
              buttons.current[index] = element;
            }}
            onClick={() => {
              onChange(option.value);
            }}
            onKeyDown={(event: KeyboardEvent) => {
              move(event, index);
            }}
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '6px',
              height: 28,
              ...(iconsOnly ? { width: 32, padding: 0 } : { padding: '0 var(--space-3)' }),
              border: 0,
              borderRadius: 'var(--radius-sm)',
              backgroundColor: 'transparent',
              color: 'var(--text-secondary)',
              fontFamily: 'var(--font-sans)',
              fontSize: 13,
              lineHeight: '18px',
              fontWeight: 500,
              whiteSpace: 'nowrap',
              cursor: 'pointer',
              '&:hover': { color: 'var(--text)' },
              '&[aria-checked="true"]': {
                backgroundColor: 'var(--bg-surface)',
                color: 'var(--text)',
                boxShadow: '0 0 0 1px var(--border-strong)',
              },
              '& > svg': { fontSize: 'var(--icon-sm)' },
            }}
          >
            {Icon !== undefined && <Icon aria-hidden />}
            {!iconsOnly && option.label}
            {!iconsOnly && option.count !== undefined && (
              <Box
                component="span"
                sx={{
                  fontWeight: 400,
                  color: 'var(--text-secondary)',
                  fontVariantNumeric: 'tabular-nums',
                }}
              >
                {option.count}
              </Box>
            )}
          </Box>
        );
      })}
    </Box>
  );
}
