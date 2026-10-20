# Proposta: encerramento do projeto (Chamados-7)

## Why

Hoje o gerente conclui ou cancela um projeto sem olhar os chamados: um projeto concluído pode ficar com chamados em andamento, e o cancelado deixa chamados abertos que ninguém mais atende. O ADR-0043 decidiu as regras de encerramento:
- **Concluir:** só com todos os chamados fechados. Qualquer outro status impede, inclusive Resolvido aguardando confirmação.
- **Cancelar:** os chamados abertos são fechados automaticamente, com o motivo "projeto cancelado" na linha do tempo, depois de uma confirmação que mostra quantos são.
- **Aviso:** concluir com tarefas abaixo de 100% avisa, mas não impede. Os termos de aceite pendentes entram no aviso quando existirem (Documentos-4).

## What Changes

- **Concluir** (`PUT /api/projects/{id}/status` com `Completed`): com algum chamado não fechado, a API responde 409 (`project-has-open-tickets`) com o número de chamados, e nada muda.
- **Cancelar** (`PUT /api/projects/{id}/status` com `Cancelled`): os chamados não fechados do projeto são fechados, cada um com o evento "projeto cancelado" na conversa e quem cancelou, e o projeto fica cancelado.
- **Resumo do encerramento** (`GET /api/projects/{id}/closing`): quantos chamados não estão fechados e quantas tarefas estão abaixo de 100%, para quem muda o status.
- **Contrato entre módulos:** o `Projects` pergunta aos outros módulos pelo `IProjectClosing` do `Projects.Contracts`, que o `Tickets` implementa, sem ler as tabelas dos chamados.
- **Na tela:**
  - "Concluir projeto" mostra os chamados não fechados, que impedem a conclusão, com o link para a aba Chamados, e o aviso das tarefas incompletas;
  - "Cancelar projeto" diz quantos chamados serão fechados.
- **Conversa:** o evento "fechou o chamado · projeto cancelado", com o status de antes.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `projetos`: regras de encerramento com chamados e o encerramento pela tela.
- `chamados`: chamados fechados pelo cancelamento do projeto.

## Impact

- **Backend:**
  - `Projects`: o caso de uso de status, o resumo do encerramento e o contrato `IProjectClosing`;
  - `Tickets`: a implementação do contrato, o evento novo e o fechamento pelo cancelamento nos prazos.
- **Frontend:** os diálogos de concluir e cancelar do projeto e a linha do evento na conversa.
- **Sem migração:** o evento novo usa as colunas que já existem.
- **Fora do escopo:** os termos de aceite pendentes no aviso, que chegam com os termos (Documentos-4, ADR-0035).
