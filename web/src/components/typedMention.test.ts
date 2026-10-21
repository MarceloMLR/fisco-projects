import { describe, expect, it } from 'vitest';
import { peopleTyped } from './typedMention';

const people = [
  { id: '1', name: 'João Pereira' },
  { id: '2', name: 'Marina Costa' },
  { id: '3', name: 'Diego Torres' },
];

// Spec: chamados, requirement "Mencionar ao digitar" (ADR-0107).
describe('Mencionar ao digitar', () => {
  it('Pessoas pelo começo do nome', () => {
    expect(peopleTyped(people, '').map((person) => person.name)).toEqual([
      'João Pereira',
      'Marina Costa',
      'Diego Torres',
    ]);
    // Without accents and by the start of any word of the name.
    expect(peopleTyped(people, 'joao').map((person) => person.name)).toEqual(['João Pereira']);
    expect(peopleTyped(people, 'cos').map((person) => person.name)).toEqual(['Marina Costa']);
    expect(peopleTyped(people, 'Diego T').map((person) => person.name)).toEqual(['Diego Torres']);
    expect(peopleTyped(people, 'xyz')).toEqual([]);
  });
});
