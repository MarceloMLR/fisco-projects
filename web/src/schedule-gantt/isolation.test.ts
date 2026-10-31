// @vitest-environment node
import { readFileSync } from 'node:fs';
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

interface PackageFile {
  dependencies?: Record<string, string>;
  devDependencies?: Record<string, string>;
}

// ADR-0134: the timeline is drawn by FiscoProjects, with no Syncfusion in the project.
describe('Linha do tempo sem licença de terceiros', () => {
  it('Sem Syncfusion no projeto', { timeout: 60_000 }, async () => {
    const file = JSON.parse(
      readFileSync(new URL('../../package.json', import.meta.url), 'utf-8'),
    ) as PackageFile;
    const packages = Object.keys({ ...file.dependencies, ...file.devDependencies });

    expect(packages.filter((name) => name.startsWith('@syncfusion/'))).toEqual([]);
    // Nor brought back by an import, anywhere in the code.
    const eslint = new ESLint({ ignore: false });
    const [result] = await eslint.lintFiles(['src/test/fixtures/SyncfusionImport.ts']);
    expect(result?.messages.map((message) => message.ruleId)).toContain('no-restricted-imports');
  });
});
