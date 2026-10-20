/** The look of a formatted text, the same in the editor and in the messages (fp-msg__body). */
export const richTextSx = {
  fontSize: 14,
  lineHeight: '22px',
  color: 'var(--text)',
  overflowWrap: 'anywhere',
  '& p': { margin: 0, minHeight: 22 },
  '& p + p, & ul, & ol': { marginTop: 'var(--space-2)' },
  '& ul, & ol': { marginBottom: 0, paddingLeft: 'var(--space-5)' },
  '& a': { color: 'var(--action-text)' },
  '& .fp-mention': {
    padding: '0 2px',
    borderRadius: 'var(--radius-sm)',
    backgroundColor: 'var(--bg-selected)',
    color: 'var(--action-text)',
    fontWeight: 500,
  },
} as const;
