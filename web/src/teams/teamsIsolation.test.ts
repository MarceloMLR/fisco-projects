// @vitest-environment node
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

// ADR-0152: the library of Teams is loaded only by the page in Teams, from one file.
describe('Biblioteca do Teams só na página do Teams', () => {
  it('Biblioteca do Teams fora do teamsHost', { timeout: 60_000 }, async () => {
    const eslint = new ESLint({ ignore: false });

    const [outside] = await eslint.lintFiles(['src/test/fixtures/TeamsImport.ts']);
    const [host] = await eslint.lintFiles(['src/teams/teamsHost.ts']);

    expect(outside?.messages.map((message) => message.ruleId)).toEqual(
      expect.arrayContaining(['no-restricted-imports', 'no-restricted-syntax']),
    );
    expect(host?.messages).toEqual([]);
  });
});
