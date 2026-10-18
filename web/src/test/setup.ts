import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import '../i18n';
import { installMatchMedia, resetDevice } from './media';
import { server } from './server';

installMatchMedia();

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
