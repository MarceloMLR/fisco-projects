import ContentCopyOutlined from '@mui/icons-material/ContentCopyOutlined';
import MailOutlined from '@mui/icons-material/MailOutlined';
import Box from '@mui/material/Box';
import Popover from '@mui/material/Popover';
import { useQuery } from '@tanstack/react-query';
import { type ComponentProps, type MouseEvent, type SyntheticEvent, useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { personQuery } from '../api/queries';
import { usePersonPhoto } from '../hooks/usePersonPhoto';
import { Avatar } from './Avatar';
import { Button } from './Button';
import { useNotify } from '../notices/notify';

/** A person shown by the avatar: the user and the name the screen already has. */
export interface CardPerson {
  id: string;
  name: string;
}

type AvatarProps = ComponentProps<typeof Avatar>;

// The card is in a portal, but the events of React still go up to the row of a table: they stop at the card.
const keep = (event: SyntheticEvent) => {
  event.stopPropagation();
};

const captionSx = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

/**
 * The avatar of another person, which opens the card of the person (ADR-0118): the avatar, the name, the part of the
 * person on that screen when the screen knows it, the e-mail, read only when the card opens, and "Copiar e-mail".
 * The surface is the one of the menu of the design system (bg-elevated, border, radius-lg and shadow-menu).
 */
export function CardAvatar({
  person,
  role,
  size = 'md',
  tone,
}: {
  person: CardPerson;
  /** The part of the person on the screen, already written, such as "Gerente do projeto". */
  role?: string;
  size?: AvatarProps['size'];
  tone?: AvatarProps['tone'];
}) {
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const notify = useNotify();
  const titleId = useId();
  const open = anchor !== null;
  const details = useQuery({ ...personQuery(person.id), enabled: open });
  const photo = usePersonPhoto()(person.id);
  const name = details.data?.name ?? person.name;
  const email = details.data?.email ?? null;

  const copy = async (address: string) => {
    try {
      await navigator.clipboard.writeText(address);
      notify(t('person.copied'));
    } catch {
      notify(t('person.copyFailed'), { tone: 'error' });
    }
  };

  return (
    <>
      <Box
        component="button"
        type="button"
        aria-label={t('person.open', { name: person.name })}
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={(event: MouseEvent<HTMLButtonElement>) => {
          event.stopPropagation();
          setAnchor(event.currentTarget);
        }}
        sx={{
          display: 'inline-flex',
          flex: 'none',
          padding: 0,
          margin: 0,
          border: 0,
          background: 'none',
          borderRadius: 'var(--radius-full)',
          cursor: 'pointer',
          '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
        }}
      >
        <Avatar name={person.name} size={size} tone={tone} photo={photo} />
      </Box>
      <Popover
        open={open}
        anchorEl={anchor}
        onClose={() => {
          setAnchor(null);
        }}
        onClick={keep}
        onMouseDown={keep}
        onKeyDown={keep}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        slotProps={{
          paper: {
            role: 'dialog',
            'aria-labelledby': titleId,
            sx: {
              boxSizing: 'border-box',
              width: 288,
              maxWidth: 'calc(100vw - 32px)',
              marginTop: 'var(--space-1)',
              padding: 'var(--space-4)',
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-3)',
            },
          },
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', minWidth: 0 }}>
          <Avatar name={name} size="lg" tone={tone} photo={photo} />
          <Box sx={{ minWidth: 0 }}>
            <Box
              id={titleId}
              sx={{
                fontSize: 15,
                lineHeight: '22px',
                fontWeight: 500,
                color: 'var(--text)',
                overflowWrap: 'anywhere',
              }}
            >
              {name}
            </Box>
            {role !== undefined && (
              <Box component="span" sx={{ ...captionSx, display: 'block' }}>
                {role}
              </Box>
            )}
          </Box>
        </Box>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'flex-start',
            gap: 'var(--space-2)',
            paddingTop: 'var(--space-3)',
            borderTop: '1px solid var(--border)',
          }}
        >
          {details.isPending ? (
            <Box component="span" sx={captionSx}>
              {t('person.loading')}
            </Box>
          ) : details.isError ? (
            <Box component="span" sx={captionSx}>
              {t('person.failed')}
            </Box>
          ) : email === null ? (
            <Box component="span" sx={captionSx}>
              {t('person.removed')}
            </Box>
          ) : (
            <>
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-2)',
                  minWidth: 0,
                  maxWidth: '100%',
                  fontSize: 13,
                  lineHeight: '18px',
                  color: 'var(--text)',
                }}
              >
                <MailOutlined
                  aria-hidden
                  sx={{ fontSize: 'var(--icon-md)', color: 'var(--text-secondary)', flex: 'none' }}
                />
                <Box component="span" sx={{ overflowWrap: 'anywhere' }}>
                  {email}
                </Box>
              </Box>
              <Button
                variant="outlined"
                size="small"
                startIcon={<ContentCopyOutlined />}
                // Copying sends nothing to the server, so the button is never busy (ADR-0131).
                onClick={() => {
                  void copy(email);
                }}
              >
                {t('person.copyEmail')}
              </Button>
            </>
          )}
        </Box>
      </Popover>
    </>
  );
}
