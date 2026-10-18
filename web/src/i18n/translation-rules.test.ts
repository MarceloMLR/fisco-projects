// @vitest-environment node
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';
import i18n from '.';

// Spec: aplicacao-web.
describe('Textos da interface em arquivos de tradução', () => {
  it('Texto escrito direto na tela', { timeout: 60_000 }, async () => {
    const eslint = new ESLint({ ignore: false });

    const [result] = await eslint.lintFiles(['src/test/fixtures/LiteralText.tsx']);

    expect(result?.messages.map((message) => message.ruleId)).toContain(
      'i18next/no-literal-string',
    );
  });

  it('Chave de tradução inexistente', () => {
    // @ts-expect-error The key is not in pt-BR.json, so `npm run typecheck` fails if this line compiles.
    const text = i18n.t('app.doesNotExist');

    expect(text).toBe('app.doesNotExist');
  });
});
