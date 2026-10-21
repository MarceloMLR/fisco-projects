## Context

O item do cronograma (`ScheduleItem`) tem `OwnerId`, uma pessoa do projeto que não é leitora, conferida pelo `ScheduleEditing.MayOwn`. O agregado guarda fotos dos itens (`ItemState`) para o desfazer (ADR-0091), a tela manda o item inteiro na alteração (inclusive a ligação de barras da Linha do tempo) e a exportação para o MS Project transforma os responsáveis em recursos.

## Goals / Non-Goals

**Goals:**
- O cliente do projeto como responsável de uma tarefa.

**Non-Goals:**
- Contatos do cliente, acesso do cliente ou avisos para ele (ADR-0007).
- O cliente como responsável de um marco.

## Decisions

- **Domínio**: `ScheduleItem.OwnedByClient` (bit, falso nos itens que já existem), gravado pelo `Set` só sem pessoa; `ScheduleItemPlan` e `ScheduleItemForm` ganham o campo, com padrão falso; `Apply` recusa marco com cliente e pessoa com cliente; a conferência do formulário dá 400 no responsável sem cliente no projeto. `ItemState` ganha o campo no fim, com padrão falso, para as mudanças já guardadas continuarem válidas.
- **Progresso**: sem pessoa, a tarefa do cliente segue a regra da tarefa sem responsável (`MayUpdateProgress(false)`): só quem edita.
- **API**: `ownedByClient` nos pedidos (padrão falso) e na resposta de cada item; `owner` continua só para pessoas.
- **Exportação**: o `MsProjectExport` recebe o nome do cliente e o acrescenta como último recurso, com as tarefas dele.
- **Tela**: o valor `client` no campo do responsável (`clientOwner`), oferecido só com cliente; `bodyOf` manda `ownerId` nulo e `ownedByClient`; `ownerText` (`schedule.ts`) diz o responsável em um lugar só, usado na Lista (com o `LogoTile` do cliente), no detalhe e na coluna da Linha do tempo (`clientName` no `ScheduleGantt`). A ligação de barras manda o `ownedByClient` do item.

## Risks / Trade-offs

- [O projeto perde o cliente] → a tarefa continua do cliente e mostra "O cliente"; escolher de novo só com cliente.
