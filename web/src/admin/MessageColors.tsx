import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { toneOf } from '../tickets/conversation';
import { PersonAvatar, RoleTag } from '../tickets/TicketConversation';
import { type ColorScheme } from '../theme/tokens';
import { type MessageColors, messageParts, messageTokens } from '../theme/brandColors';
import { roleOfPart } from './messageParts';

/**
 * The Preview of the colors of the messages (ADR-0101): a message of each part, with its avatar and its tag, in the
 * colors being typed, before saving.
 */
export function MessagePreview({ colors, scheme }: { colors: MessageColors; scheme: ColorScheme }) {
  const { t } = useTranslation();
  const variables = Object.fromEntries(
    Object.entries(messageTokens(colors, scheme)).map(([name, value]) => [`--${name}`, value]),
  );
  return (
    <Box
      role="list"
      aria-label={t('settings.messagePreview')}
      style={variables}
      sx={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
        gap: 'var(--space-2)',
      }}
    >
      {messageParts.map((part) => {
        const role = roleOfPart[part];
        const tone = toneOf[role];
        return (
          <Box
            key={part}
            role="listitem"
            data-testid={`message-preview-${part}`}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-2)',
              minWidth: 0,
              padding: 'var(--space-2) var(--space-3)',
              borderRadius: 'var(--radius-lg)',
              backgroundColor: `var(--msg-${tone}-bg)`,
              border: `1px solid var(--msg-${tone}-border)`,
            }}
          >
            <PersonAvatar name={t(`tickets.roles.${role}`)} tone={tone} size="sm" />
            <RoleTag role={role} />
            <Box
              component="span"
              sx={{
                minWidth: 0,
                fontSize: 13,
                lineHeight: '18px',
                color: 'var(--text)',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {t('settings.messageSample')}
            </Box>
          </Box>
        );
      })}
    </Box>
  );
}
