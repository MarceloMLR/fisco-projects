import type { TeamsTicket } from '../api/client';

// The ticket #6 of the ERP-FIN as the page in Teams shows it to Diego, after the mention of Marina, as the canvas
// "Chamado no Teams" (ADR-0152).

export const carla = { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' };
export const diego = { id: '0199f5a2-7c1e-7d40-9a1b-000000000006', name: 'Diego Torres' };
export const marina = { id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70', name: 'Marina Costa' };
export const rafael = { id: '0199f5a2-7c1e-7d40-9a1b-000000000004', name: 'Rafael Souza' };

const doc = (...content: unknown[]) =>
  JSON.stringify({ type: 'doc', content: [{ type: 'paragraph', content }] });

export const mentionOfDiego = doc(
  { type: 'mention', attrs: { id: diego.id, label: diego.name } },
  {
    type: 'text',
    text: ' consegue validar esses dois arquivos até quinta? O cliente precisa fechar a entrada de outubro.',
  },
);

const noEvent = {
  assigneeId: null,
  fromPriority: null,
  toPriority: null,
  fromTeam: null,
  toTeam: null,
  fromPersonId: null,
  toPersonId: null,
  vendorTicketId: null,
  fromVendorStatus: null,
  toVendorStatus: null,
  note: null,
} as const;

export const demoTeamsTicket: TeamsTicket = {
  number: 6,
  title: 'Importar XML de entrada',
  project: {
    id: '0199f5a2-7c1e-7d40-9a1b-0000000000e1',
    code: 'ERP-FIN',
    name: 'Implantação do ERP financeiro',
  },
  team: 'Development',
  priority: 'High',
  status: 'InProgress',
  requester: carla,
  assignee: diego,
  managers: [marina],
  openedAt: '2026-10-29T16:58:00Z',
  attachmentCount: 2,
  deadlines: {
    firstResponse: { state: 'Met', dueOn: '2026-10-30', daysLate: null },
    firstRespondedAt: '2026-10-29T17:31:00Z',
    resolution: { state: 'Soon', dueOn: '2026-11-05', daysLate: null },
    usedDays: 3,
    totalDays: 5,
    pausedDays: 0,
  },
  viewerId: diego.id,
  conversation: {
    earlier: 10,
    description: null,
    messages: [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-0000000a0001',
        authorId: carla.id,
        body: doc({
          type: 'text',
          text: 'Ao importar o XML de entrada das notas de devolução, o sistema recusa o arquivo com “Layout inválido”. Seguem dois exemplos.',
        }),
        sentAt: '2026-11-03T16:58:00Z',
      },
      {
        id: '0199f5a2-7c1e-7d40-9a1b-0000000a0002',
        authorId: rafael.id,
        body: doc({
          type: 'text',
          text: 'O validador recusa o grupo refNFe quando a natureza não é “Devolução de venda”.',
        }),
        sentAt: '2026-11-03T17:31:00Z',
      },
      {
        id: '0199f5a2-7c1e-7d40-9a1b-0000000a0003',
        authorId: marina.id,
        body: mentionOfDiego,
        sentAt: '2026-11-03T17:59:00Z',
      },
    ],
    events: [
      {
        ...noEvent,
        id: '0199f5a2-7c1e-7d40-9a1b-0000000b0001',
        kind: 'StatusChanged',
        actorId: rafael.id,
        from: 'Open',
        to: 'InProgress',
        action: 'StartWork',
        at: '2026-11-03T17:20:00Z',
      },
    ],
    attachments: [
      {
        messageId: '0199f5a2-7c1e-7d40-9a1b-0000000a0001',
        name: 'nfe-devolucao-3391.xml',
        size: 36_864,
        contentType: 'application/xml',
      },
      {
        messageId: '0199f5a2-7c1e-7d40-9a1b-0000000a0001',
        name: 'nfe-devolucao-3392.xml',
        size: 35_840,
        contentType: 'application/xml',
      },
    ],
    participants: [
      { ...carla, role: 'Requester' },
      { ...diego, role: 'Owner' },
      { ...marina, role: 'Manager' },
      { ...rafael, role: 'TechLead' },
    ],
    vendorTickets: [],
  },
};
