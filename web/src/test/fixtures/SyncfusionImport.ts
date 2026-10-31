// Brings the Syncfusion back on purpose: the lint test checks that ESLint reports it anywhere (ADR-0134).
// @ts-expect-error: the project has no Syncfusion package.
import { registerLicense } from '@syncfusion/ej2-base';

export const register: unknown = registerLicense;
