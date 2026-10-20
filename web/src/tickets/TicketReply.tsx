import AttachFileOutlined from '@mui/icons-material/AttachFileOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import SendOutlined from '@mui/icons-material/SendOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import { useQueryClient } from '@tanstack/react-query';
import { lazy, Suspense, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Ticket } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import type { MentionChoice } from '../components/RichTextEditor';
import { sizeText } from '../format/size';
import { ticketProblemText } from './ticketProblems';

// The editor only loads on the screens of the tickets.
const RichTextEditor = lazy(async () => ({
  default: (await import('../components/RichTextEditor')).RichTextEditor,
}));

/** The files chosen to go with a text, each with its size and a button to take it out. */
export function ChosenFiles({
  files,
  onRemove,
}: {
  files: readonly File[];
  onRemove: (index: number) => void;
}) {
  const { t } = useTranslation();
  return files.length === 0 ? null : (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)' }}>
      {files.map((file, index) => (
        <Box
          key={`${file.name}-${String(index)}`}
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            height: 32,
            padding: '0 var(--space-1) 0 var(--space-3)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-md)',
            fontSize: 13,
          }}
        >
          <span>{file.name}</span>
          <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
            {sizeText(file.size)}
          </Box>
          <IconButton
            size="small"
            aria-label={t('tickets.reply.removeFile', { name: file.name })}
            onClick={() => {
              onRemove(index);
            }}
          >
            <CloseOutlined sx={{ fontSize: 16 }} />
          </IconButton>
        </Box>
      ))}
    </Box>
  );
}

/** "Escolher arquivos": several at a time, without dragging (ADR-0069). */
export function ChooseFiles({
  onChoose,
  compact = false,
}: {
  onChoose: (files: File[]) => void;
  compact?: boolean;
}) {
  const { t } = useTranslation();
  const input = useRef<HTMLInputElement>(null);
  return (
    <>
      <Box
        component="input"
        ref={input}
        type="file"
        multiple
        aria-label={t('tickets.reply.files')}
        sx={{ display: 'none' }}
        onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
          onChoose([...(event.target.files ?? [])]);
          event.target.value = '';
        }}
      />
      {compact ? (
        <IconButton
          size="small"
          aria-label={t('tickets.reply.files')}
          title={t('tickets.reply.files')}
          onClick={() => input.current?.click()}
          sx={{
            width: 30,
            height: 30,
            borderRadius: 'var(--radius-sm)',
            color: 'var(--text-secondary)',
            '& svg': { fontSize: 18 },
          }}
        >
          <AttachFileOutlined />
        </IconButton>
      ) : (
        <Button
          variant="outlined"
          size="small"
          startIcon={<AttachFileOutlined />}
          onClick={() => input.current?.click()}
        >
          {t('tickets.reply.files')}
        </Button>
      )}
    </>
  );
}

/**
 * The answer box at the bottom of the conversation (Composer, ADR-0069): the editor with the mentions of the people who
 * may be mentioned, the files and "Enviar". Only for who sends messages (ADR-0044).
 */
export function TicketReply({ ticket, me }: { ticket: Ticket; me: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [text, setText] = useState({ json: '', empty: true });
  const [files, setFiles] = useState<File[]>([]);
  const [error, setError] = useState<string>();
  const [sending, setSending] = useState(false);
  const [round, setRound] = useState(0);
  const names = new Map(ticket.participants.map((person) => [person.id, person.name]));
  const mentionable: MentionChoice[] = ticket.mentionable
    .filter((id) => id !== me)
    .map((id) => ({ id, name: names.get(id) ?? '' }))
    .filter((person) => person.name !== '')
    .sort((one, other) => one.name.localeCompare(other.name, 'pt-BR'));

  const send = async () => {
    if (text.empty) {
      setError(t('tickets.reply.empty'));
      return;
    }
    setSending(true);
    setError(undefined);
    try {
      const form = new FormData();
      form.append('body', text.json);
      for (const file of files) {
        form.append('files', file, file.name);
      }
      const { error: problem, response } = await api.POST('/api/tickets/{number}/messages', {
        params: { path: { number: ticket.number } },
        body: { files: [] },
        bodySerializer: () => form,
      });
      if (response.ok) {
        setFiles([]);
        setText({ json: '', empty: true });
        setRound((value) => value + 1);
        void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
        return;
      }
      setError(ticketProblemText(response.status, problem, t));
    } catch {
      setError(t('tickets.reply.failed'));
    } finally {
      setSending(false);
    }
  };

  return (
    <Box
      sx={{
        maxWidth: 960,
        margin: '0 auto',
        width: '100%',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
      }}
    >
      {error !== undefined && <Alert tone="danger">{error}</Alert>}
      <Suspense fallback={<Box sx={{ height: 132 }} />}>
        <RichTextEditor
          key={round}
          label={t('tickets.reply.label')}
          minHeight={44}
          placeholder={
            ticket.requester.id === me
              ? t('tickets.reply.placeholderOwn')
              : t('tickets.reply.placeholder', { name: ticket.requester.name })
          }
          mentionable={mentionable}
          onChange={(json, empty) => {
            setText({ json, empty });
          }}
          tools={
            <ChooseFiles
              compact
              onChoose={(chosen) => {
                setFiles((current) => [...current, ...chosen]);
              }}
            />
          }
          footer={
            <Box
              sx={{
                display: 'flex',
                alignItems: 'flex-end',
                gap: 'var(--space-2)',
                padding: '0 var(--space-2) var(--space-2) var(--space-4)',
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <ChosenFiles
                  files={files}
                  onRemove={(index) => {
                    setFiles((current) => current.filter((_file, place) => place !== index));
                  }}
                />
              </Box>
              <Button
                variant="contained"
                startIcon={<SendOutlined />}
                disabled={sending}
                onClick={() => {
                  void send();
                }}
              >
                {t('tickets.reply.send')}
              </Button>
            </Box>
          }
        />
      </Suspense>
    </Box>
  );
}
