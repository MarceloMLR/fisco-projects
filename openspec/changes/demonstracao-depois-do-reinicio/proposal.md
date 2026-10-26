## Why

Na publicação do merge da M365-1, a carga dos dados de demonstração falhou logo depois de o `/health` responder: a aplicação reiniciou com o pacote novo no meio do caminho, a pergunta das formas de entrada não teve resposta, e o comando disse que a entrada local estava desligada. Rodar de novo passou. A publicação não pode falhar por um reinício normal do App Service, e a mensagem precisa dizer o que de fato aconteceu.

## What Changes

- `scripts/cd/deploy.sh` tenta a carga dos dados de demonstração até três vezes, com 30 segundos de intervalo, e só falha se a última falhar. A carga já só acrescenta o que falta, então repetir é seguro.
- O comando `demo:data` distingue a API que não respondeu às formas de entrada (com o status) da entrada local desligada.

## Non-goals

- Mudar como o pacote é publicado ou como o `/health` é esperado.
- Esperar uma versão da aplicação ou um identificador do pacote.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `publicacao`: a carga dos dados de demonstração resiste a um reinício da aplicação logo depois da publicação.

## Impact

- `scripts/cd/deploy.sh`, `web/scripts/demo/load.ts` e os testes `DeploymentTests` e `web/scripts/demo/load.test.ts`.
- Roadmap: Fundação-7.1 (ADR-0089, ADR-0115).
