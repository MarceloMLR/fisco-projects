// Breaks the rule on purpose: the library of Teams outside src/teams/teamsHost.ts (ADR-0152).
import { app } from '@microsoft/teams-js';

export const initialize = () => app.initialize();
export const loadLater = () => import('@microsoft/teams-js');
