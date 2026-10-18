// @vitest-environment node
import { describe, expect, it } from 'vitest';
import { initials } from './initials';

describe('Iniciais do avatar', () => {
  it('Primeiro e último nome', () => {
    expect(initials('Marina Costa')).toBe('MC');
    expect(initials('  beatriz  de lima ')).toBe('BL');
    expect(initials('Paulo')).toBe('P');
    expect(initials('')).toBe('');
  });
});
