import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import i18next from 'eslint-plugin-i18next';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

const syncfusionImports = {
  group: ['@syncfusion/*'],
  message:
    'The timeline is our own (ADR-0134): use the ScheduleGantt component, without Syncfusion.',
};

const buttonMessage =
  'Use the Button and the IconButton of src/components/Button, busy while what they send runs (ADR-0131).';
const muiButtonImports = [
  { name: '@mui/material/Button', message: buttonMessage },
  { name: '@mui/material/IconButton', message: buttonMessage },
  { name: '@mui/material', importNames: ['Button', 'IconButton'], message: buttonMessage },
];

const noticeMessage =
  'Use the notice of the structure of the screens, useNotify of src/notices/notify, one for all the screens (ADR-0133).';
const muiSnackbarImports = [
  { name: '@mui/material/Snackbar', message: noticeMessage },
  { name: '@mui/material/SnackbarContent', message: noticeMessage },
  { name: '@mui/material', importNames: ['Snackbar', 'SnackbarContent'], message: noticeMessage },
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
    // ADR-0134: the timeline is our own, with no Syncfusion anywhere. ADR-0131: every button comes from
    // src/components/Button, busy while what it sends runs. ADR-0133: one notice for all the screens, the one of
    // src/notices.
    files: ['src/**/*.{ts,tsx}'],
    ignores: ['src/components/Button.tsx'],
    rules: {
      'no-restricted-imports': [
        'error',
        { patterns: [syncfusionImports], paths: [...muiButtonImports, ...muiSnackbarImports] },
      ],
    },
  },
  {
    files: ['src/components/Button.tsx'],
    rules: {
      'no-restricted-imports': [
        'error',
        { patterns: [syncfusionImports], paths: muiSnackbarImports },
      ],
    },
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
