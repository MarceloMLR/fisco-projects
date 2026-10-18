import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { AuditEntry } from '../api/client';
import { formatDateTime } from '../format/dates';
import { describeAuditEntry } from './auditText';

const section = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-3)',
  paddingBottom: 'var(--space-5)',
  borderBottom: '1px solid var(--border)',
  '&:last-of-type': { borderBottom: 0, paddingBottom: 0 },
} as const;

const cell = {
  padding: 'var(--space-2) var(--space-3)',
  borderTop: '1px solid var(--border)',
  textAlign: 'left',
  verticalAlign: 'top',
  overflowWrap: 'break-word',
} as const;

/**
 * Details of an audit record, shown in the details drawer (ADR-0081): when, who did what, the item and the table of the
 * changed fields, before and after.
 */
export function AuditDetails({ entry }: { entry: AuditEntry }) {
  const { t } = useTranslation();
  const description = describeAuditEntry(entry, t);

  return (
    <>
      <Box component="section" sx={section}>
        <Box
          component="span"
          sx={{
            fontSize: 12,
            lineHeight: '16px',
            color: 'var(--text-secondary)',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {formatDateTime(entry.occurredAt)}
        </Box>
        <Box
          component="h2"
          sx={{
            margin: 0,
            fontSize: 15,
            lineHeight: '22px',
            fontWeight: 500,
            color: 'var(--text)',
          }}
        >
          {description.sentence}
        </Box>
        {description.properties.length > 0 && (
          <Box
            component="dl"
            sx={{
              display: 'grid',
              gridTemplateColumns: '112px minmax(0, 1fr)',
              gap: 'var(--space-3)',
              margin: 0,
              '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
              '& dd': { margin: 0, minWidth: 0, overflowWrap: 'anywhere' },
            }}
          >
            {description.properties.map(({ label, value }) => (
              <Box key={label} sx={{ display: 'contents' }}>
                <dt>{label}</dt>
                <dd>{value}</dd>
              </Box>
            ))}
          </Box>
        )}
      </Box>
      {description.changes.length > 0 && (
        <Box component="section" sx={section} aria-labelledby={`changes-${entry.id}`}>
          <Box
            component="h3"
            id={`changes-${entry.id}`}
            sx={{
              margin: 0,
              fontSize: 12,
              lineHeight: '16px',
              fontWeight: 500,
              color: 'var(--text-secondary)',
            }}
          >
            {t('audit.details.changes')}
          </Box>
          <Box
            component="table"
            sx={{
              width: '100%',
              borderCollapse: 'separate',
              borderSpacing: 0,
              tableLayout: 'fixed',
              backgroundColor: 'var(--bg-surface)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-lg)',
              overflow: 'hidden',
              fontSize: 14,
              lineHeight: '20px',
              fontVariantNumeric: 'tabular-nums',
              '& thead th': {
                ...cell,
                borderTop: 0,
                backgroundColor: 'var(--bg-subtle)',
                fontSize: 13,
                lineHeight: '18px',
                fontWeight: 500,
                color: 'var(--text-secondary)',
              },
              '& tbody th': { ...cell, fontWeight: 400, color: 'var(--text-secondary)' },
              '& tbody td': cell,
              '& tbody td:last-of-type': { fontWeight: 500 },
            }}
          >
            <thead>
              <tr>
                <Box component="th" scope="col" sx={{ width: '30%' }}>
                  {t('audit.details.field')}
                </Box>
                <th scope="col">{t('audit.details.before')}</th>
                <th scope="col">{t('audit.details.after')}</th>
              </tr>
            </thead>
            <tbody>
              {description.changes.map((change) => (
                <tr key={change.field}>
                  <th scope="row">{change.field}</th>
                  <td>{change.before}</td>
                  <td>{change.after}</td>
                </tr>
              ))}
            </tbody>
          </Box>
        </Box>
      )}
    </>
  );
}
