import type { MsProjectPreview } from '../api/client';
import { demoPeople } from './server';

/**
 * The preview of a fictitious file of the MS Project (ADR-0135), as the API reads it from the start given: a phase with
 * two tasks and a milestone, Carla found by her e-mail, a consultant without one, two changes of the conversion and the
 * whole file brought.
 */
export function demoMsProjectPreview(start = '2026-10-13'): MsProjectPreview {
  // The start moves the dates as the server would, in business days: 13/10 or 19/10/2026, both on a Monday or Tuesday.
  const later = start !== '2026-10-13';
  return {
    title: 'Implantação do ERP',
    firstDay: '2026-10-13',
    start,
    plan: {
      phases: ['Configuração'],
      items: [
        {
          phase: 0,
          parent: null,
          kind: 'Task',
          name: 'Parametrizar regras de ICMS',
          startOffset: 0,
          duration: 3,
          progress: 40,
          after: [],
          resource: 0,
          deadlineOffset: null,
        },
        {
          phase: 0,
          parent: null,
          kind: 'Task',
          name: 'Treinar usuários',
          startOffset: 3,
          duration: 2,
          progress: 0,
          after: [],
          resource: 1,
          deadlineOffset: null,
        },
        {
          phase: 0,
          parent: null,
          kind: 'Milestone',
          name: 'Aceite da configuração',
          startOffset: 2,
          duration: 0,
          progress: 0,
          after: [0],
          resource: null,
          deadlineOffset: null,
        },
      ],
    },
    phases: [
      {
        name: 'Configuração',
        items: [
          {
            kind: 'Task',
            name: 'Parametrizar regras de ICMS',
            isSubtask: false,
            start: later ? '2026-10-19' : '2026-10-13',
            end: later ? '2026-10-21' : '2026-10-15',
            duration: 3,
            progress: 40,
            reached: false,
            resource: 0,
          },
          {
            kind: 'Milestone',
            name: 'Aceite da configuração',
            isSubtask: false,
            start: later ? '2026-10-21' : '2026-10-15',
            end: later ? '2026-10-21' : '2026-10-15',
            duration: 0,
            progress: 0,
            reached: false,
            resource: null,
          },
          {
            kind: 'Task',
            name: 'Treinar usuários',
            isSubtask: false,
            start: later ? '2026-10-22' : '2026-10-16',
            end: later ? '2026-10-23' : '2026-10-19',
            duration: 2,
            progress: 0,
            reached: false,
            resource: 1,
          },
        ],
      },
    ],
    resources: [
      {
        name: 'Carla Mendes',
        email: 'carla.mendes@empresa.example',
        tasks: 1,
        person: {
          id: demoPeople.carla.id,
          directoryId: null,
          name: 'Carla Mendes',
          email: 'carla.mendes@empresa.example',
        },
        client: false,
      },
      { name: 'Consultor externo', email: null, tasks: 1, person: null, client: false },
    ],
    warnings: [
      {
        kind: 'DependencyLags',
        names: ['Parametrizar regras de ICMS → Aceite da configuração'],
        count: 1,
      },
      { kind: 'SeveralResources', names: ['Treinar usuários'], count: 1 },
    ],
    // The phase and its three items came, and the two resources of the file.
    counts: { fileTasks: 4, tasks: 4, fileResources: 2, resources: 2 },
  };
}
