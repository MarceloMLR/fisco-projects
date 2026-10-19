import react from '@vitejs/plugin-react';
import type { Plugin } from 'vite';
import { defineConfig } from 'vitest/config';

// Where the API runs in development (src/FiscoProjects.Api/Properties/launchSettings.json).
const api = 'http://localhost:5175';

/**
 * The fonts come from the application, never from Google (ADR-0064); the theme of the timeline (ADR-0051) imports
 * Roboto from Google, so the import goes away when its CSS is compiled.
 */
const withoutGoogleFonts: Plugin = {
  name: 'fiscoprojects:without-google-fonts',
  transform(code, id) {
    if (!id.includes('@syncfusion') || !/\.css($|\?)/.test(id)) {
      return undefined;
    }
    return {
      code: code.replace(/@import\s+url\(["']?https:\/\/fonts\.googleapis\.com[^)]*\)\s*;?/g, ''),
      map: null,
    };
  },
};

export default defineConfig({
  plugins: [react(), withoutGoogleFonts],
  build: {
    // The API host serves the build from its static files folder (ADR-0047).
    outDir: '../src/FiscoProjects.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    strictPort: true,
    // The browser sees a single address, as in production (ADR-0054).
    proxy: { '/api': api, '/health': api, '/openapi': api },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Dates on screen use the browser's time zone; the tests fix one, so they pass on any machine.
    env: { TZ: 'America/Sao_Paulo' },
  },
});
