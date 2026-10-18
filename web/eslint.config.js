import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import i18next from 'eslint-plugin-i18next';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default defineConfig([
    // Generated types, and files that break the rules on purpose for the lint tests.
    globalIgnores(['src/api/schema.ts', 'src/test/fixtures']),
    {
        files: ['**/*.{ts,tsx}'],
        extends: [
            js.configs.recommended,
            tseslint.configs.strictTypeChecked,
            tseslint.configs.stylisticTypeChecked,
            reactHooks.configs.flat.recommended,
            reactRefresh.configs.vite,
        ],
        languageOptions: {
            globals: globals.browser,
            parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
        },
    },
    {
        // ADR-0013: visible text comes from the translation files, never written in a component.
        files: ['src/**/*.tsx'],
        ignores: ['src/**/*.test.tsx'],
        plugins: { i18next },
        rules: { 'i18next/no-literal-string': ['error', { mode: 'jsx-only' }] },
    },
]);
