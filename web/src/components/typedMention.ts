import { Extension } from '@tiptap/core';
import Suggestion, { type SuggestionProps } from '@tiptap/suggestion';

/** A person who may be mentioned. */
export interface MentionChoice {
  id: string;
  name: string;
}

/** The most people the list of a mention being typed shows. */
const typedMentionLimit = 8;

/** A name or a search without accents and in small letters, so "joao" finds "João". */
const plain = (text: string) =>
  text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();

/** The people whose name, or a word of it, starts with what was typed after "@". */
export function peopleTyped(
  people: readonly MentionChoice[],
  typed: string,
): readonly MentionChoice[] {
  const query = plain(typed.trim());
  return people
    .filter((person) => {
      const name = plain(person.name);
      return (
        query === '' ||
        name.startsWith(query) ||
        name.split(/\s+/).some((word) => word.startsWith(query))
      );
    })
    .slice(0, typedMentionLimit);
}

/** A mention being typed after "@": the people that match, the one marked, where the text is and how to choose. */
export interface TypedMention {
  people: readonly MentionChoice[];
  marked: number;
  rect: () => DOMRect | null;
  choose: (person: MentionChoice) => void;
}

/**
 * What the editor and the screen share about a mention being typed (ADR-0107): the people who may be mentioned now,
 * and the list the screen shows, kept here so the keys of the editor read the latest.
 */
export class TypedMentions {
  private people: readonly MentionChoice[] = [];
  private shown: TypedMention | undefined;
  private listener: (typed: TypedMention | undefined) => void = () => undefined;

  /** The people who may be mentioned, as the screen has them now. */
  setPeople(people: readonly MentionChoice[]): void {
    this.people = people;
  }

  /** What the screen does when the list changes. */
  listen(listener: (typed: TypedMention | undefined) => void): void {
    this.listener = listener;
  }

  matching(query: string): MentionChoice[] {
    return [...peopleTyped(this.people, query)];
  }

  get current(): TypedMention | undefined {
    return this.shown;
  }

  show(typed: TypedMention | undefined): void {
    this.shown = typed;
    this.listener(typed);
  }

  /** The keys of the list while it shows: the arrows mark a person, Enter or Tab chooses the one marked. */
  keyDown(event: KeyboardEvent): boolean {
    const current = this.shown;
    if (current === undefined) {
      return false;
    }
    const count = current.people.length;
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      const step = event.key === 'ArrowDown' ? 1 : count - 1;
      this.show({ ...current, marked: (current.marked + step) % count });
      return true;
    }
    if (event.key === 'Enter' || event.key === 'Tab') {
      const person = current.people[current.marked];
      if (person !== undefined) {
        current.choose(person);
      }
      return true;
    }
    return false;
  }
}

/** Typing "@" and part of a name lists the people who may be mentioned, as the button "@" does (ADR-0107). */
export function typedMentionExtension(mentions: TypedMentions) {
  return Extension.create({
    name: 'typedMention',
    addProseMirrorPlugins() {
      const show = (props: SuggestionProps<MentionChoice, MentionChoice>) => {
        const marked = Math.min(mentions.current?.marked ?? 0, Math.max(props.items.length - 1, 0));
        mentions.show(
          props.items.length === 0
            ? undefined
            : {
                people: props.items,
                marked,
                rect: () => props.clientRect?.() ?? null,
                choose: props.command,
              },
        );
      };
      return [
        Suggestion<MentionChoice, MentionChoice>({
          editor: this.editor,
          char: '@',
          // Names have spaces: "@Diego T" keeps looking.
          allowSpaces: true,
          items: ({ query }) => mentions.matching(query),
          command: ({ editor, range, props: person }) => {
            editor
              .chain()
              .focus()
              .insertContentAt(range, [
                { type: 'mention', attrs: { id: person.id, label: person.name } },
                { type: 'text', text: ' ' },
              ])
              .run();
          },
          render: () => ({
            onStart: show,
            onUpdate: show,
            onKeyDown: ({ event }) => mentions.keyDown(event),
            onExit: () => {
              mentions.show(undefined);
            },
          }),
        }),
      ];
    },
  });
}
