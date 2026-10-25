---
status: aceita
date: 2026-10-25
decision-makers: Marcelo Ribeiro
---

# ADR-0115: Publicação no ambiente de desenvolvimento a cada merge

Complementa o ADR-0055 (CI/CD com GitHub Actions) e o ADR-0114 (infraestrutura no Azure), e substitui a regra do ADR-0055 de rodar o `terraform apply` a cada merge na `main`.

## Contexto e problema

O ADR-0055 decidiu que cada merge na `main` publica a aplicação no ambiente de desenvolvimento e demonstração, com `terraform apply`, migrações e publicação, pelo GitHub Actions com identidade federada. Com o ambiente criado (Fundação-6), faltava decidir o que a identidade do pipeline pode fazer. Para aplicar o Terraform inteiro, ela precisaria criar e mudar aplicativos, grupos e permissões no Entra ID e dar papéis no Azure, quase como um administrador do tenant e da assinatura. E o repositório é público: um workflow alterado e aceito num merge teria esse poder.

## Fatores de decisão

- Nenhum segredo no GitHub (ADR-0055).
- A menor permissão possível para a identidade do pipeline.
- A aplicação no ar a cada merge, sem passo manual.
- A infraestrutura diferente do código precisa aparecer, e não ficar escondida.

## Opções consideradas

1. O pipeline publica a aplicação e só confere a infraestrutura (`terraform plan`); o `apply` fica com quem opera a instalação.
2. O pipeline publica e aplica tudo, como no ADR-0055.

## Resultado da decisão

Opção 1, escolhida por Marcelo.

- **Identidade do pipeline**: um aplicativo do Entra ID por ambiente, criado pelo Terraform (módulo `ci`, ligado por `github_repository`). O GitHub Actions a alcança por federação, só a partir do environment do GitHub de mesmo nome (`dev`), que aceita só a `main`. Ela é colaboradora do grupo de recursos do ambiente, para publicar, grava no contêiner do estado, para o `plan` travar o estado, e lê o diretório do Entra ID (`Directory.Read.All`), para o `plan` conferir o aplicativo e os grupos. Ela não dá papéis no Azure nem muda o Entra ID.
- **Variáveis do environment do GitHub**: o cliente da identidade, o tenant, a assinatura e os valores pessoais da instalação (quem opera e o e-mail dos alertas). Nenhuma é segredo. Elas são preparadas pelo `infra/scripts/Set-GitHubEnvironment.ps1`, que também restringe o environment à `main`. O e-mail dos alertas é sensível no Terraform e mascarado nos logs, que são públicos.
- **A cada merge na `main`** (`.github/workflows/deploy-dev.yml`, com a lógica em `scripts/cd`):
  1. `publish.sh` monta o pacote: as telas compiladas dentro da API e a API publicada para o .NET do App Service.
  2. `deploy.sh` publica o pacote pelo Azure CLI, sem as credenciais básicas do App Service, que ficam desligadas, e espera o `/health` responder. No ambiente em `Development`, a aplicação aplica as próprias migrações ao subir (ADR-0114), e depois o script carrega os dados de demonstração que faltam (ADR-0089).
  3. `plan.sh` roda o `terraform plan` com o estado do ambiente. Se houver diferença, o pipeline avisa no resumo da execução, sem falhar; quem opera aplica o plano, como em `docs/instalacao.md`.
- **Migrações na produção**: ficam para o workflow manual da produção (Operação-5).
- **Licenças do NuGet**: o job `backend` do PR passa a conferir, depois do build, que todo pacote do backend, direto ou transitivo, tem licença permitida (MIT, Apache-2.0, BSD e as outras permissivas da lista do frontend). Ficam aceitas as partes nativas do `Microsoft.Data.SqlClient` e do MSAL, que trazem os termos da Microsoft num arquivo e vão junto com a aplicação.

### Consequências

- Boa, porque um workflow alterado não ganha poder sobre o Entra ID nem dá papéis no Azure.
- Boa, porque cada merge põe a aplicação no ar com os dados de demonstração, e a diferença na infraestrutura aparece no resumo da execução.
- Ruim, porque uma mudança na infraestrutura depende de alguém aplicar o plano à mão.
- Neutra, porque a produção ganha a própria identidade e o próprio workflow, manual, na Operação-5.
