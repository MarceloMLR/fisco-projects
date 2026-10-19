// @vitest-environment node
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

// ADR-0051: the Syncfusion packages are imported only by the timeline component, in src/schedule-gantt.
describe('Linha do tempo isolada', () => {
  it('Syncfusion fora da linha do tempo', { timeout: 60_000 }, async () => {
    const eslint = new ESLint({ ignore: false });

    const [result] = await eslint.lintFiles(['src/test/fixtures/SyncfusionImport.ts']);

    expect(result?.messages.map((message) => message.ruleId)).toContain('no-restricted-imports');
  });
});
