import type {
  DefaultFolder,
  DocumentDetails,
  DocumentFolders,
  DocumentRow,
  DocumentStatus,
} from '../api/client';

// The documents of ERP-FIN for the screen tests, as the canvas ProjetoDocumentos (ADR-0031).

const id = (kind: string, index: number) =>
  `0199f5a2-7c1e-7d40-9a1b-${kind}${String(index).padStart(11, '0')}`;

export const folderIds = {
  management: id('a', 1),
  contracts: id('a', 2),
  requirements: id('a', 3),
  minutes: id('a', 4),
  deliveries: id('a', 5),
  terms: id('a', 6),
} as const;

const marina = { id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70', name: 'Marina Costa' };
const beatriz = { id: '0199f5a2-7c1e-7d40-9a1b-000000000003', name: 'Beatriz Lima' };
const rafael = { id: '0199f5a2-7c1e-7d40-9a1b-000000000004', name: 'Rafael Souza' };
const carla = { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' };

/** The default folders of the installation: the six of the migration, the last for the terms. */
export function demoDefaultFolders(): DefaultFolder[] {
  return [
    { id: id('b', 1), name: '01 Gestão do projeto', isAcceptance: false, isActive: true },
    { id: id('b', 2), name: '02 Contratos e compras', isAcceptance: false, isActive: true },
    { id: id('b', 3), name: '03 Requisitos e especificações', isAcceptance: false, isActive: true },
    { id: id('b', 4), name: '04 Atas de reunião', isAcceptance: false, isActive: false },
    { id: id('b', 5), name: '05 Entregas e manuais', isAcceptance: false, isActive: true },
    { id: id('b', 6), name: '06 Termos de aceite', isAcceptance: true, isActive: true },
  ];
}

function row(
  index: number,
  name: string,
  folderId: string,
  folderName: string,
  status: DocumentStatus,
  version: number,
  size: number,
  updated: { id: string; name: string },
  at: string,
  link: DocumentRow['link'],
): DocumentRow {
  return {
    id: id('c', index),
    name,
    folderId,
    folderName,
    status,
    statusChanged: { ...(status === 'Approved' ? marina : updated), at },
    version,
    size,
    updated: { ...updated, at },
    link,
  };
}

const requirements = '03 Requisitos e especificações';

/** The documents of ERP-FIN, by name, as the API lists them. */
export function demoDocuments(): DocumentRow[] {
  return [
    row(
      1,
      'Especificação das regras de ICMS-ST.pdf',
      folderIds.requirements,
      requirements,
      'Approved',
      3,
      1_258_291,
      beatriz,
      '2026-10-14T19:02:00Z',
      {
        id: id('d', 1),
        type: 'Task',
        name: 'Parametrizar regras de ICMS',
      },
    ),
    row(
      2,
      'Especificação de relatórios fiscais v1.pdf',
      folderIds.requirements,
      requirements,
      'Obsolete',
      1,
      411_648,
      beatriz,
      '2026-10-02T14:00:00Z',
      null,
    ),
    row(
      3,
      'Layout de integração NF-e.xlsx',
      folderIds.requirements,
      requirements,
      'InReview',
      1,
      94_208,
      rafael,
      '2026-10-15T13:00:00Z',
      {
        id: id('d', 2),
        type: 'Task',
        name: 'Integrar emissão de NF-e',
      },
    ),
    row(
      4,
      'Mapa de processos fiscais.docx',
      folderIds.requirements,
      requirements,
      'Draft',
      2,
      497_664,
      carla,
      '2026-10-09T13:00:00Z',
      null,
    ),
    row(
      5,
      'Plano do projeto.docx',
      folderIds.management,
      '01 Gestão do projeto',
      'Approved',
      1,
      65_536,
      marina,
      '2026-10-05T12:00:00Z',
      {
        id: id('d', 3),
        type: 'Phase',
        name: 'Levantamento',
      },
    ),
  ];
}

/** The folders of ERP-FIN as Marina, the manager, sees them, with the counts of the documents not obsolete. */
export function demoDocumentArea(): DocumentFolders {
  const documents = demoDocuments().filter((document) => document.status !== 'Obsolete');
  const count = (folderId: string) =>
    documents.filter((document) => document.folderId === folderId).length;
  return {
    folders: [
      {
        id: folderIds.management,
        name: '01 Gestão do projeto',
        isAcceptance: false,
        count: count(folderIds.management),
      },
      { id: folderIds.contracts, name: '02 Contratos e compras', isAcceptance: false, count: 0 },
      {
        id: folderIds.requirements,
        name: requirements,
        isAcceptance: false,
        count: count(folderIds.requirements),
      },
      { id: folderIds.minutes, name: '04 Atas de reunião', isAcceptance: false, count: 0 },
      { id: folderIds.deliveries, name: '05 Entregas e manuais', isAcceptance: false, count: 0 },
      { id: folderIds.terms, name: '06 Termos de aceite', isAcceptance: true, count: 0 },
    ],
    total: documents.length,
    permissions: { canUpload: true, canApprove: true, canDelete: true },
    maxBytes: 25 * 1024 * 1024,
  };
}

/** A page of the documents, filtered as the API does: by folder or by the search, without the obsolete ones by default. */
export function documentsPage(documents: DocumentRow[], url: URL) {
  const folderId = url.searchParams.get('folderId');
  const search = url.searchParams.get('search')?.toLowerCase();
  const all = url.searchParams.get('status') === 'All';
  const items = documents.filter(
    (document) =>
      (folderId === null || document.folderId === folderId) &&
      (search === undefined || document.name.toLowerCase().includes(search)) &&
      (all || document.status !== 'Obsolete'),
  );
  return { items, total: items.length };
}

/** A document with its versions, newest first, and what Marina may do with it. */
export function demoDocumentDetails(document: DocumentRow): DocumentDetails {
  return {
    document,
    versions: Array.from({ length: document.version }, (_, index) => {
      const number = document.version - index;
      return {
        number,
        size: document.size - index * 1024,
        uploaded: {
          ...document.updated,
          at: `2026-10-${String(14 - index).padStart(2, '0')}T12:00:00Z`,
        },
      };
    }),
    permissions: { canUpload: true, canApprove: true, canDelete: true },
  };
}
