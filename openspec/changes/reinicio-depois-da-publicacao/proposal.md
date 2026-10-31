## Why

Na publicação do merge da Gantt-3, a carga dos dados de demonstração falhou nas três tentativas: depois do `az webapp deploy`, o `/health` respondia 200, mas as outras rotas davam 500 por cerca de um minuto e meio. A publicação em zip copia os arquivos por cima da aplicação que está rodando e só reinicia depois. Nesse meio-tempo, o processo antigo atende com arquivos novos e velhos misturados. Rodar os jobs de novo passou. A publicação não pode falhar, nem deixar as pessoas com erros, por essa janela. Correção da Fundação-7.1, aprovada por Marcelo no chat.

## What Changes

- `scripts/cd/deploy.sh` reinicia a aplicação (`az webapp restart`) logo depois de publicar o pacote, antes de esperar o `/health`, para o processo antigo não atender com os arquivos misturados. Vale para o `dev` e para a produção, que usam o mesmo script.
- A carga dos dados de demonstração tenta até seis vezes, com 30 segundos de intervalo (antes eram três), para cobrir um reinício mais lento do App Service.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `publicacao`: o reinício da aplicação logo depois da publicação e as seis tentativas da carga da demonstração.

## Non-goals

- Mudar como o pacote é publicado, como rodar a aplicação direto do zip (`WEBSITE_RUN_FROM_PACKAGE`), que muda a infraestrutura.
- Esperar uma versão da aplicação ou um identificador do pacote no `/health`.
- Mudar o `infra/scripts/Publish-Version.ps1` das instalações das empresas, que não carrega dados de demonstração.

## Impact

- `scripts/cd/deploy.sh` e os testes `DeploymentTests`.
- Roadmap: Fundação-7.2 (ADR-0055, ADR-0089, ADR-0115).
