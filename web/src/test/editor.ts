import { screen } from '@testing-library/react';

interface EditorElement {
  editor: {
    commands: {
      insertContent: (content: unknown) => boolean;
      focus: () => boolean;
    };
  };
}

/**
 * Types in an editor of the tickets (Tiptap), found by its name: the editor keeps itself on its element, and its
 * commands change the text as typing would, with the same onUpdate.
 */
export async function typeInEditor(name: string, content: unknown): Promise<void> {
  const element = (await screen.findByRole('textbox', { name })) as unknown as EditorElement;
  element.editor.commands.focus();
  element.editor.commands.insertContent(content);
}
