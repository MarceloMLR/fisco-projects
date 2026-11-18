## Why

Os recursos do Azure, o estado do Terraform e o pacote da versão ainda levam o nome antigo do produto. O Azure não renomeia esses recursos, e só existem os ambientes de Marcelo, com dados de demonstração. O ADR-0162 decidiu destruir e recriar com o prefixo `gerenware`.

## What Changes

- **BREAKING** Prefixo dos recursos `gerenware-<instalação>-<ambiente>`, banco `sqldb-gerenware`, estado do Terraform em `rg-gerenware-<instalação>-tfstate` e `stgw<instalação>tfstate`, e nome curto dos alertas `gerenware`, no Terraform, nos scripts de `infra/scripts` e de `scripts/cd` e nos arquivos de `environments/`.
- **BREAKING** Pacote da versão `gerenware-vX.Y.Z.zip` (`release.sh` e `Publish-Version.ps1`).
- Guias (`docs/instalacao.md`, `docs/restauracao.md`, `docs/glossario.md`), `CLAUDE.md` e as specs `infraestrutura` e `publicacao` com os nomes novos; os testes do pipeline acompanham.
- A operação nos ambientes `dev` e `prod` (destruir, preparar o estado novo, aplicar, dar as permissões de novo, publicar, instalar o Teams) fica com quem opera, depois do merge, pelo roteiro do `design.md`.

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma regra muda: os nomes dos recursos e do pacote citados nas specs `infraestrutura` e `publicacao` foram trocados direto no texto.

## Non-goals

- Rodar o `destroy` e o `apply` pela sessão ou pelo pipeline: continuam com quem opera (ADR-0115).
- O nome do repositório e a credencial federada do deploy (Marca-7).
- Mover dados entre os ambientes antigos e os novos: não há dados a manter.

## Impact

- `infra/terraform/main.tf`, `modules/alerts`, `environments/*.backend.hcl`, `infra/scripts/Prepare-TerraformState.ps1` e `Publish-Version.ps1`, `scripts/cd/*.sh` e os testes de `tests/GerenWare.Pipeline.Tests`.
- Os ambientes `dev` e `prod` ficam fora do ar entre o `destroy` e a publicação seguinte.
- Passos manuais do Microsoft 365 refeitos: SharePoint, Exchange, Teams e o aviso da Autentique.
