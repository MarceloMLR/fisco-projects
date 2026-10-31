// @vitest-environment node
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

// Spec: estrutura-das-telas, requirement "Confirmações no alto da tela" (ADR-0133).
describe('Confirmações no alto da tela', () => {
  it('Toda confirmação no mesmo lugar', { timeout: 60_000 }, async () => {
    const eslint = new ESLint({ ignore: false });

    const [result] = await eslint.lintFiles(['src/test/fixtures/MuiSnackbarImport.tsx']);

    expect(
      result?.messages.filter((message) => message.ruleId === 'no-restricted-imports'),
    ).toHaveLength(2);
  });
});
