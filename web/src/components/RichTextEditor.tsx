import AlternateEmailOutlined from '@mui/icons-material/AlternateEmailOutlined';
import FormatBoldOutlined from '@mui/icons-material/FormatBoldOutlined';
import FormatListBulletedOutlined from '@mui/icons-material/FormatListBulletedOutlined';
import LinkOutlined from '@mui/icons-material/LinkOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { Node } from '@tiptap/core';
import { Placeholder } from '@tiptap/extensions';
import { EditorContent, useEditor, useEditorState } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import { type MouseEvent, type ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Avatar } from './Avatar';
import { Dialog } from './Dialog';
import { richTextSx } from './richTextSx';
import { TextField } from './TextField';

/** A person who may be mentioned. */
export interface MentionChoice {
  id: string;
  name: string;
}

/** A mention of a person (ADR-0026): kept with the id and the name, shown as @name. */
const Mention = Node.create({
  name: 'mention',
  group: 'inline',
  inline: true,
  atom: true,
  selectable: false,
  addAttributes() {
    return { id: { default: null }, label: { default: null } };
  },
  parseHTML() {
    return [{ tag: 'span[data-mention]' }];
  },
  renderHTML({ node }) {
    const label = typeof node.attrs.label === 'string' ? node.attrs.label : '';
    return ['span', { 'data-mention': '', class: 'fp-mention' }, `@${label}`];
  },
  renderText({ node }) {
    return `@${typeof node.attrs.label === 'string' ? node.attrs.label : ''}`;
  },
});

const safeLink = /^(https?:\/\/|mailto:)/i;

function ToolButton({
  label,
  active = false,
  onClick,
  children,
}: {
  label: string;
  active?: boolean;
  onClick: (event: MouseEvent<HTMLElement>) => void;
  children: ReactNode;
}) {
  return (
    <IconButton
      size="small"
      aria-label={label}
      title={label}
      aria-pressed={active}
      onMouseDown={(event) => {
        // Keeps the selection of the text while the button is pressed.
        event.preventDefault();
      }}
      onClick={onClick}
      sx={{
        width: 30,
        height: 30,
        borderRadius: 'var(--radius-sm)',
        color: active ? 'var(--action-text)' : 'var(--text-secondary)',
        backgroundColor: active ? 'var(--bg-selected)' : 'transparent',
        '& svg': { fontSize: 18 },
      }}
    >
      {children}
    </IconButton>
  );
}

/**
 * The editor of the tickets (Tiptap, ADR-0026; Composer of the design system): bold, bulleted lists, links and
 * mentions of the people who may be mentioned, chosen by "@". Gives the document as JSON, as the API keeps it, and
 * whether it is empty. To clear it after sending, give it another key.
 */
export function RichTextEditor({
  label,
  placeholder,
  mentionable,
  onChange,
  invalid = false,
  minHeight = 72,
  tools,
  footer,
}: {
  label: string;
  placeholder?: string;
  /** Who may be mentioned; without it, no "@", as in the description of a new ticket. */
  mentionable?: readonly MentionChoice[];
  onChange: (json: string, empty: boolean) => void;
  invalid?: boolean;
  minHeight?: number;
  /** More buttons at the end of the toolbar, such as "Escolher arquivos". */
  tools?: ReactNode;
  /** What goes under the text, such as the files chosen and "Enviar". */
  footer?: ReactNode;
}) {
  const { t } = useTranslation();
  const [people, setPeople] = useState<HTMLElement>();
  const [linking, setLinking] = useState(false);
  const [address, setAddress] = useState('');
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: false,
        blockquote: false,
        code: false,
        codeBlock: false,
        horizontalRule: false,
        italic: false,
        strike: false,
        underline: false,
        link: {
          openOnClick: false,
          autolink: true,
          defaultProtocol: 'https',
          protocols: ['mailto'],
        },
      }),
      Placeholder.configure({ placeholder: placeholder ?? '' }),
      Mention,
    ],
    editorProps: { attributes: { 'aria-label': label, 'aria-multiline': 'true', role: 'textbox' } },
    onUpdate: ({ editor: changed }) => {
      onChange(JSON.stringify(changed.getJSON()), changed.isEmpty);
    },
  });
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => ({
      bold: current.isActive('bold'),
      list: current.isActive('bulletList'),
      link: current.isActive('link'),
    }),
  });

  const saveLink = () => {
    const href = address.trim();
    const chain = editor.chain().focus().extendMarkRange('link');
    if (href === '') {
      chain.unsetLink().run();
    } else if (safeLink.test(href)) {
      chain.setLink({ href }).run();
    } else {
      chain.setLink({ href: `https://${href}` }).run();
    }
    setLinking(false);
  };

  return (
    <Box
      data-invalid={invalid}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        border: '1px solid var(--border-control)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
        '&:focus-within': {
          borderColor: 'var(--control-selected)',
          boxShadow: '0 0 0 1px var(--control-selected)',
        },
        '&[data-invalid="true"]': { borderColor: 'var(--status-danger-text)' },
      }}
    >
      <Box
        role="toolbar"
        aria-label={t('editor.toolbar')}
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: '2px',
          padding: 'var(--space-1) var(--space-2)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <ToolButton
          label={t('editor.bold')}
          active={state.bold}
          onClick={() => editor.chain().focus().toggleBold().run()}
        >
          <FormatBoldOutlined />
        </ToolButton>
        <ToolButton
          label={t('editor.list')}
          active={state.list}
          onClick={() => editor.chain().focus().toggleBulletList().run()}
        >
          <FormatListBulletedOutlined />
        </ToolButton>
        <ToolButton
          label={t('editor.link')}
          active={state.link}
          onClick={() => {
            const current: unknown = editor.getAttributes('link').href;
            setAddress(typeof current === 'string' ? current : '');
            setLinking(true);
          }}
        >
          <LinkOutlined />
        </ToolButton>
        <Box
          aria-hidden
          sx={{
            width: '1px',
            height: 18,
            margin: '0 var(--space-1)',
            backgroundColor: 'var(--border)',
          }}
        />
        {mentionable !== undefined && (
          <ToolButton
            label={t('editor.mention')}
            onClick={(event) => {
              setPeople(event.currentTarget);
            }}
          >
            <AlternateEmailOutlined />
          </ToolButton>
        )}
        {tools}
      </Box>
      <Box
        sx={{
          ...richTextSx,
          padding: 'var(--space-3) var(--space-4)',
          cursor: 'text',
          '& .ProseMirror': { outline: 'none', minHeight },
          '& .ProseMirror p.is-editor-empty:first-of-type::before': {
            content: 'attr(data-placeholder)',
            float: 'left',
            height: 0,
            color: 'var(--text-placeholder)',
            pointerEvents: 'none',
          },
        }}
        onClick={() => {
          if (!editor.isFocused) {
            editor.commands.focus();
          }
        }}
      >
        <EditorContent editor={editor} />
      </Box>
      {footer}
      <Menu
        anchorEl={people}
        open={people !== undefined}
        onClose={() => {
          setPeople(undefined);
        }}
        slotProps={{ list: { 'aria-label': t('editor.mention') } }}
      >
        {mentionable?.length === 0 && <MenuItem disabled>{t('editor.nobody')}</MenuItem>}
        {(mentionable ?? []).map((person) => (
          <MenuItem
            key={person.id}
            onClick={() => {
              setPeople(undefined);
              editor
                .chain()
                .focus()
                .insertContent([
                  { type: 'mention', attrs: { id: person.id, label: person.name } },
                  { type: 'text', text: ' ' },
                ])
                .run();
            }}
            sx={{ gap: 'var(--space-2)' }}
          >
            <Avatar name={person.name} size="sm" />
            {person.name}
          </MenuItem>
        ))}
      </Menu>
      {linking && (
        <Dialog
          open
          title={t('editor.linkTitle')}
          onClose={() => {
            setLinking(false);
          }}
          onSubmit={(event) => {
            event.preventDefault();
            saveLink();
          }}
          actions={
            <>
              <Button
                variant="outlined"
                onClick={() => {
                  setLinking(false);
                }}
              >
                {t('editor.linkCancel')}
              </Button>
              <Button type="submit" variant="contained">
                {t('editor.linkSave')}
              </Button>
            </>
          }
        >
          <TextField
            label={t('editor.linkAddress')}
            value={address}
            autoFocus
            placeholder={t('editor.linkPlaceholder')}
            onChange={(event) => {
              setAddress(event.target.value);
            }}
          />
        </Dialog>
      )}
    </Box>
  );
}
