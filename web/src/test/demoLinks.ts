import type { LinkCategory, LinkList, ProjectLink } from '../api/client';

// The links of ERP-FIN for the screen tests, as the canvas ProjetoLinks (ADR-0032).

const id = (kind: string, index: number) =>
  `0199f5a2-7c1e-7d40-9a1b-${kind}${String(index).padStart(11, '0')}`;

export const linkCategoryIds = {
  systems: id('c', 1),
  environments: id('c', 2),
  repositories: id('c', 3),
  panels: id('c', 4),
  others: id('c', 5),
} as const;

const rafael = { id: '0199f5a2-7c1e-7d40-9a1b-000000000004', name: 'Rafael Souza' };
const carla = { id: '0199f5a2-7c1e-7d40-9a1b-000000000005', name: 'Carla Mendes' };
const diego = { id: '0199f5a2-7c1e-7d40-9a1b-000000000006', name: 'Diego Torres' };

/** The categories of the installation: the five of the migration, with Painéis inactive. */
export function demoLinkCategories(): LinkCategory[] {
  return [
    { id: linkCategoryIds.environments, name: 'Ambientes', isActive: true },
    { id: linkCategoryIds.others, name: 'Outros', isActive: true },
    { id: linkCategoryIds.panels, name: 'Painéis', isActive: false },
    { id: linkCategoryIds.repositories, name: 'Repositórios', isActive: true },
    { id: linkCategoryIds.systems, name: 'Sistemas', isActive: true },
  ];
}

function link(
  index: number,
  title: string,
  url: string,
  category: keyof typeof linkCategoryIds,
  description: string | null,
  by: { id: string; name: string },
  day: number,
): ProjectLink {
  const names = {
    environments: 'Ambientes',
    systems: 'Sistemas',
    panels: 'Painéis',
    repositories: 'Repositórios',
    others: 'Outros',
  };
  return {
    id: id('d', index),
    title,
    url,
    description,
    categoryId: linkCategoryIds[category],
    categoryName: names[category],
    created: { ...by, at: `2026-10-${String(day).padStart(2, '0')}T13:00:00Z` },
  };
}

/** The links of ERP-FIN, in the order of the API: the category, then the title. */
export function demoLinks(): ProjectLink[] {
  return [
    link(
      1,
      'ERP · Homologação',
      'https://hml-erp.alfadistribuidora.example',
      'environments',
      'Usado nos testes com a equipe do cliente.',
      rafael,
      6,
    ),
    link(
      2,
      'ERP · Produção',
      'https://erp.alfadistribuidora.example',
      'environments',
      'Somente leitura até a virada para produção.',
      rafael,
      6,
    ),
    link(
      3,
      'Indicadores de apuração',
      'https://bi.alfadistribuidora.example/apuracao',
      'panels',
      null,
      carla,
      12,
    ),
    link(
      4,
      'Layouts de integração',
      'https://git.empresa.example/erp-fin/layouts',
      'repositories',
      'Layouts de arquivo trocados com o emissor.',
      diego,
      11,
    ),
    link(
      5,
      'Emissor de NF-e',
      'https://nfe.fornecedor-exemplo.example/painel',
      'systems',
      'Painel do emissor usado pela filial.',
      diego,
      8,
    ),
  ];
}

/** The answer of the API for the links, with the search in the title and in the address and the category. */
export function linksList(links: readonly ProjectLink[], url: URL, canEdit = true): LinkList {
  const search = (url.searchParams.get('search') ?? '').toLowerCase();
  const categoryId = url.searchParams.get('categoryId');
  const counts = new Map<string, { id: string; name: string; count: number }>();
  for (const item of links) {
    const count = counts.get(item.categoryId) ?? {
      id: item.categoryId,
      name: item.categoryName,
      count: 0,
    };
    counts.set(item.categoryId, { ...count, count: count.count + 1 });
  }
  return {
    items: links.filter(
      (item) =>
        (categoryId === null || item.categoryId === categoryId) &&
        (item.title.toLowerCase().includes(search) || item.url.toLowerCase().includes(search)),
    ),
    categories: [...counts.values()].sort((first, second) => first.name.localeCompare(second.name)),
    total: links.length,
    canEdit,
  };
}
