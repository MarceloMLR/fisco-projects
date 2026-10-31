import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// Where the API runs in development (src/FiscoProjects.Api/Properties/launchSettings.json).
const api = 'http://localhost:5175';

export default defineConfig({
  plugins: [react()],
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
