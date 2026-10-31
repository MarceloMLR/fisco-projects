// @vitest-environment node
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

// ADR-0131: every button comes from src/components/Button, busy while what it sends runs.
describe('Botões do sistema', () => {
  it('Botão do MUI usado direto', { timeout: 60_000 }, async () => {
    const eslint = new ESLint({ ignore: false });

    const [result] = await eslint.lintFiles(['src/test/fixtures/MuiButtonImport.tsx']);

    expect(
      result?.messages.filter((message) => message.ruleId === 'no-restricted-imports'),
    ).toHaveLength(2);
  });
});
