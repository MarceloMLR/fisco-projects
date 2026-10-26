## Why

Na instalação real, a busca de pessoas só traz quem já entrou: o diretório da empresa ainda é o de demonstração da entrada local. Com a entrada pelo Entra (M365-1), o diretório passa a ser o tenant da empresa, para incluir num projeto quem ainda não entrou (M365-2 do roadmap, ADR-0005).

## What Changes

- ADR-0119: o diretório da empresa pelo Microsoft Graph, com a permissão de aplicativo `User.Read.All` escolhida por Marcelo.
- `Users`: o diretório da Microsoft (`GraphPeopleDirectory`), que busca os membros ativos do tenant pelo Graph com o token pedido pela prova da identidade gerenciada, e o diretório da empresa que junta o de demonstração e o da Microsoft conforme as entradas ligadas. A pessoa incluída ganha o vínculo `entra` pelo identificador do objeto, o mesmo da entrada.
- Terraform: o aplicativo do Entra ganha a permissão de aplicativo `User.Read.All` do Graph, com o consentimento do administrador.
- Testes com um Graph simulado no próprio processo.

## Capabilities

### New Capabilities

### Modified Capabilities
- `usuarios-e-papeis`: o diretório da Microsoft na busca de pessoas.
- `infraestrutura`: o aplicativo do Entra lê o diretório.

## Impact

- `Users` (Infrastructure, registro e testes), `FiscoProjects.Testing` (Graph simulado), testes da API, `infra/terraform/modules/entra`, `InfrastructureTests`, `docs/instalacao.md`. Depois do merge, o `apply` do Terraform no `dev` fica com Marcelo.
