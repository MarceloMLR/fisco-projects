// @vitest-environment node
import openapiTS, { astToString, COMMENT_HEADER, type OpenAPI3 } from 'openapi-typescript';
import { describe, expect, it } from 'vitest';
import contract from './openapi.json';
import versionedTypes from './schema.ts?raw';

// Spec: aplicacao-web.
describe('Tipos da API gerados do contrato', () => {
  it('Tipos do frontend desatualizados', async () => {
    // Same output as the openapi-typescript CLI used by `npm run api:types`.
    const generatedTypes = COMMENT_HEADER + astToString(await openapiTS(contract as OpenAPI3));

    expect(versionedTypes, 'Types are stale: run `npm run api:types` in web/.').toBe(
      generatedTypes,
    );
  });
});
