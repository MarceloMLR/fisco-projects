---
status: aceita
date: 2026-10-25
decision-makers: Marcelo Ribeiro
---

# ADR-0116: Identidade do pipeline pelos números do repositório e nada pessoal nos logs públicos

Complementa o ADR-0115 (publicação no ambiente de desenvolvimento) e substitui dois pontos dele: o e-mail dos alertas como variável do environment do GitHub e a leitura do Entra ID só com `Directory.Read.All`.

## Contexto e problema

A primeira publicação pela `main` falhou no login do Azure, e o log dela mostrou dois problemas:

- O GitHub mandou o token com o subject no formato imutável, com os números do dono e do repositório (`repo:<dono>@<id do dono>/<repositório>@<id do repositório>:environment:dev`). Desde 15/07/2026, esse é o formato dos repositórios criados, renomeados ou transferidos. A federação do Entra ID esperava o formato só com os nomes e recusou o token.
- O GitHub mostra, no começo de cada passo, os valores das variáveis do job. O e-mail dos alertas, que o ADR-0115 punha como variável do environment e mascarava só no `plan.sh`, apareceu no log, que é público.

Além disso, as permissões do `terraform plan` previstas no ADR-0115 não cobrem duas leituras: as credenciais federadas dos aplicativos (a API do Graph pede `Application.Read.All`, que o `Directory.Read.All` não inclui) e o papel da identidade no contêiner do estado (que pede leitura das atribuições de papel na conta de armazenamento).

## Fatores de decisão

- Nenhum dado pessoal nos logs públicos do repositório.
- A identidade do pipeline continua sem segredos, sem dar papéis e sem mudar o Entra ID (ADR-0115).
- A federação não pode passar para outro repositório que pegue o mesmo nome.

## Opções consideradas

1. Subject imutável na federação, e-mail dos alertas lido do Azure pelo próprio script, e só leituras a mais para o `plan`.
2. Voltar o repositório ao subject só com nomes, guardar o e-mail como segredo do GitHub e rodar o `plan` sem atualizar o estado (`-refresh=false`).

## Resultado da decisão

Opção 1.

- **Federação pelos números**: `github_repository` passa a ter o formato do subject imutável (`<dono>@<id do dono>/<repositório>@<id do repositório>`), conferido pela validação da variável. Os números saem de `gh api repos/<dono>/<repositório>` (`owner.id` e `id`). Um repositório renomeado, recriado ou de outra pessoa com o mesmo nome não ganha a identidade.
- **E-mail dos alertas fora do workflow**: o `plan.sh` lê o e-mail do grupo de ações da instalação, que o `apply` mantém a partir do `local.auto.tfvars` de quem opera, e o mascara antes de escrever qualquer coisa. Ele deixa de ser variável do environment, e o `Set-GitHubEnvironment.ps1` deixa de gravá-lo. Nenhum valor pessoal entra no workflow.
- **Leituras do `plan`**: a identidade ganha `Application.Read.All` no Graph, junto do `Directory.Read.All`, e o papel Leitor só na conta de armazenamento do estado. As duas são só de leitura.
- O que continua visível nos logs são identificadores, não credenciais: o tenant, a assinatura, o cliente da identidade e o objeto de quem opera. Nenhum dá acesso sem a federação, que só aceita a `main` deste repositório.

### Consequências

- Boa, porque a federação fica presa a este repositório, mesmo se ele for renomeado ou se outro tomar o nome.
- Boa, porque o e-mail dos alertas não aparece nos logs públicos, e há um lugar só para ele, o `local.auto.tfvars` de quem opera.
- Ruim, porque o log da primeira publicação, que mostrou o e-mail, precisa ser apagado por quem administra o repositório.
- Neutra, porque a identidade continua só com leituras fora do grupo de recursos do ambiente.
