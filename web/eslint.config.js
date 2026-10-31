import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import i18next from 'eslint-plugin-i18next';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

const syncfusionImports = {
  group: ['@syncfusion/*'],
  message: 'Syncfusion only inside src/schedule-gantt (ADR-0051); use the ScheduleGantt component.',
};

const buttonMessage =
  'Use the Button and the IconButton of src/components/Button, busy while what they send runs (ADR-0131).';
const muiButtonImports = [
  { name: '@mui/material/Button', message: buttonMessage },
  { name: '@mui/material/IconButton', message: buttonMessage },
  { name: '@mui/material', importNames: ['Button', 'IconButton'], message: buttonMessage },
];

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
    // ADR-0051: only the timeline component imports Syncfusion, so it can give way to a timeline of our own. ADR-0131:
    // every button comes from src/components/Button, busy while what it sends runs.
    files: ['src/**/*.{ts,tsx}'],
    ignores: ['src/schedule-gantt/**', 'src/components/Button.tsx'],
    rules: {
      'no-restricted-imports': [
        'error',
        { patterns: [syncfusionImports], paths: muiButtonImports },
      ],
    },
  },
  {
    files: ['src/schedule-gantt/**/*.{ts,tsx}'],
    rules: { 'no-restricted-imports': ['error', { paths: muiButtonImports }] },
  },
  {
    files: ['src/components/Button.tsx'],
    rules: { 'no-restricted-imports': ['error', { patterns: [syncfusionImports] }] },
  },
  {
    // ADR-0013: visible text comes from the translation files, never written in a component.
    files: ['src/**/*.tsx'],
    ignores: ['src/**/*.test.tsx'],
    plugins: { i18next },
    rules: {
      'i18next/no-literal-string': [
        'error',
        {
          mode: 'jsx-only',
          // Text in JSX and in the attributes people see or hear; styles, ids and routes are not text.
          'jsx-attributes': {
            include: ['^(aria-label|aria-description|title|alt|placeholder|label|helperText)$'],
            exclude: ['.*'],
          },
        },
      ],
    },
  },
]);
