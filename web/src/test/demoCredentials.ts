import type { Credential, CredentialList } from '../api/client';

// The credentials of ERP-FIN for the screen tests, as the canvas ProjetoAcessos (ADR-0034). The passwords are only
// in the answers of the password, never in the list.

const id = (index: number) => `0199f5a2-7c1e-7d40-9a1b-e${String(index).padStart(11, '0')}`;

/** The credentials of ERP-FIN, in the order of the API: the environment, then the portal. */
export function demoCredentials(): Credential[] {
  return [
    {
      id: id(1),
      portal: 'Portal de suporte',
      environment: 'Fornecedores',
      url: 'https://suporte.fornecedor-exemplo.example',
      username: 'marina.costa',
      notes: 'Registro de chamados no fornecedor.',
    },
    {
      id: id(2),
      portal: 'ERP',
      environment: 'Homologação',
      url: 'https://hml-erp.alfadistribuidora.example',
      username: 'consultoria.fiscal',
      notes: 'Perfil de administrador do módulo fiscal.',
    },
    {
      id: id(3),
      portal: 'ERP',
      environment: 'Produção',
      url: 'https://erp.alfadistribuidora.example',
      username: 'consultoria.leitura',
      notes: 'Somente leitura.',
    },
    {
      id: id(4),
      portal: 'SEFAZ',
      environment: 'Produção',
      url: null,
      username: '12.345.678/0001-90',
      notes: null,
    },
  ];
}

/** The fictitious password of each credential, as the API answers it. */
export function demoPasswordOf(credentialId: string): string {
  return `senha-${credentialId.slice(-2)}`;
}

/** The answer of the API for the credentials, with the search in the portal and in the user and the environment. */
export function credentialsList(
  credentials: readonly Credential[],
  url: URL,
  permissions = { canEdit: true, canDelete: true },
): CredentialList {
  const search = (url.searchParams.get('search') ?? '').toLowerCase();
  const environment = url.searchParams.get('environment');
  const counts = new Map<string, number>();
  for (const item of credentials) {
    counts.set(item.environment, (counts.get(item.environment) ?? 0) + 1);
  }
  return {
    items: credentials.filter(
      (item) =>
        (environment === null || item.environment === environment) &&
        (item.portal.toLowerCase().includes(search) ||
          item.username.toLowerCase().includes(search)),
    ),
    environments: [...counts.entries()]
      .map(([name, count]) => ({ name, count }))
      .sort((first, second) => first.name.localeCompare(second.name)),
    total: credentials.length,
    permissions,
  };
}
