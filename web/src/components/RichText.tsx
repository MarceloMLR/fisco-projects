import Box from '@mui/material/Box';
import type { ReactNode } from 'react';
import { richTextSx } from './richTextSx';

/** A node of the document of the editor (Tiptap, ProseMirror), as the API keeps it (RichText in the Tickets module). */
interface RichNode {
  type?: unknown;
  text?: unknown;
  attrs?: Record<string, unknown>;
  marks?: { type?: unknown; attrs?: Record<string, unknown> }[];
  content?: RichNode[];
}

const safeLink = /^(https?:|mailto:)/i;
const rootKey = 'd';

function isNode(value: unknown): value is RichNode {
  return typeof value === 'object' && value !== null;
}

function children(node: RichNode, prefix: string): ReactNode[] {
  return Array.isArray(node.content)
    ? node.content.filter(isNode).map((child, index) => draw(child, `${prefix}.${String(index)}`))
    : [];
}

function marked(text: string, marks: RichNode['marks'], key: string): ReactNode {
  let element: ReactNode = text;
  for (const mark of marks ?? []) {
    if (mark.type === 'bold') {
      element = <strong key={key}>{element}</strong>;
    } else if (
      mark.type === 'link' &&
      typeof mark.attrs?.href === 'string' &&
      safeLink.test(mark.attrs.href)
    ) {
      element = (
        <a key={key} href={mark.attrs.href} target="_blank" rel="noopener noreferrer">
          {element}
        </a>
      );
    }
  }
  return element;
}

function draw(node: RichNode, key: string): ReactNode {
  switch (node.type) {
    case 'paragraph':
      return <p key={key}>{children(node, key)}</p>;
    case 'bulletList':
      return <ul key={key}>{children(node, key)}</ul>;
    case 'orderedList':
      return (
        <ol key={key} start={typeof node.attrs?.start === 'number' ? node.attrs.start : undefined}>
          {children(node, key)}
        </ol>
      );
    case 'listItem':
      return <li key={key}>{children(node, key)}</li>;
    case 'hardBreak':
      return <br key={key} />;
    case 'mention':
      return (
        <span key={key} className="fp-mention">
          {`@${typeof node.attrs?.label === 'string' ? node.attrs.label : ''}`}
        </span>
      );
    case 'text':
      return typeof node.text === 'string' ? (
        <span key={key}>{marked(node.text, node.marks, `${key}m`)}</span>
      ) : null;
    default:
      return null;
  }
}

/**
 * A text with basic formatting (ADR-0026), drawn from the document the API keeps, node by node and never as HTML:
 * paragraphs, lists, bold, links to http, https or mailto, and mentions. A text that is no document shows as written.
 */
export function RichText({ value }: { value: string }) {
  let document: unknown;
  try {
    document = JSON.parse(value);
  } catch {
    document = undefined;
  }
  return (
    <Box sx={richTextSx}>
      {isNode(document) && document.type === 'doc' ? (
        children(document, rootKey)
      ) : (
        <Box component="span" sx={{ whiteSpace: 'pre-wrap' }}>
          {value}
        </Box>
      )}
    </Box>
  );
}
