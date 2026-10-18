/**
 * Look of the design system field control (fp-field): height of the size, surface, control border and the focus
 * ring. Shared by the controls that are not a plain text field, such as the select and the person search.
 */
export function fieldControl(size: 'sm' | 'md' | 'lg', width?: number) {
  return {
    boxSizing: 'border-box',
    width,
    height: `var(--control-${size})`,
    backgroundColor: 'var(--bg-surface)',
    border: '1px solid var(--border-control)',
    borderRadius: 'var(--radius-md)',
    color: 'var(--text)',
    fontSize: 14,
    lineHeight: '20px',
    transition: 'border-color 150ms ease-out',
    '&:hover': { borderColor: 'var(--text-secondary)' },
    '&.Mui-focused': {
      borderColor: 'var(--focus-ring)',
      boxShadow: 'inset 0 0 0 1px var(--focus-ring)',
    },
  } as const;
}
