// Breaks the isolation of the timeline on purpose: the lint test checks that ESLint reports it (ADR-0051).
import { registerLicense } from '@syncfusion/ej2-base';

export const register = registerLicense;
