import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import '../i18n';
import { installMatchMedia, resetDevice } from './media';
import { server } from './server';

installMatchMedia();

// The editor of the tickets (Tiptap, ProseMirror) measures the text to scroll to it, and jsdom draws nothing.
if (typeof Range !== 'undefined') {
  const noRects = () =>
    ({
      length: 0,
      item: () => null,
      [Symbol.iterator]: [][Symbol.iterator],
    }) as unknown as DOMRectList;
  Range.prototype.getClientRects = noRects;
  Range.prototype.getBoundingClientRect = () => new DOMRect();
  if (!('getClientRects' in Text.prototype)) {
    Object.defineProperty(Text.prototype, 'getClientRects', { value: noRects });
  }
}

// Any call to the API that a test did not simulate fails the test.
beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  cleanup();
  resetDevice();
  server.resetHandlers();
  if (typeof window !== 'undefined') {
    window.localStorage.clear();
  }
});

afterAll(() => {
  server.close();
});
