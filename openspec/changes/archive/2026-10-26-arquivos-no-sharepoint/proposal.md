## Why

Os arquivos do FiscoProjects ficam numa pasta do disco: a implementação local, só para desenvolvimento e testes. O ADR-0030 decidiu que eles ficam no SharePoint da empresa, e agora o tenant de Marcelo tem o Microsoft 365 e o site "Projetos" para testar de ponta a ponta (M365-3 do roadmap).

## What Changes

- ADR-0120: como os arquivos ficam no SharePoint (Graph com o token do aplicativo, `Sites.Selected` com o papel de escrita dado por script, ids e versões do SharePoint, o endereço do site fora do repositório e dos logs).
- `Files`: a implementação `SharePointFileStorage` do `IFileStorage`, escolhida por `FileStorage:Provider` = `SharePoint`, com o site, a biblioteca e a pasta raiz em `FileStorage:SharePoint`.
- `Users`: o `IGraphTokens` passa para o `Users.Contracts`, para o `Files` pedir o token do aplicativo.
- Terraform: a permissão `Sites.Selected` com o consentimento do administrador e as configurações do armazenamento quando o `local.auto.tfvars` tem o site; o script `infra/scripts/Grant-SharePointSite.ps1` dá o site ao aplicativo.
- Publicação: a conferência da infraestrutura lê o endereço do site da aplicação no ar e o mascara.
- Testes com um SharePoint simulado no processo, com os mesmos testes do contrato da implementação local.

## Capabilities

### New Capabilities

### Modified Capabilities
- `armazenamento-de-arquivos`: os arquivos no SharePoint.
- `infraestrutura`: a permissão `Sites.Selected` e o site do SharePoint.
- `publicacao`: o endereço do site fora dos logs.

## Impact

- `Files` (Infrastructure e testes), `Users.Contracts`, `FiscoProjects.Testing` (`FakeSharePoint`), testes da API, `infra/terraform`, `infra/scripts`, `scripts/cd/plan.sh`, `docs/instalacao.md`. Depois do merge, o `apply` no `dev`, o script do site e a recarga dos dados de demonstração ficam com Marcelo.
