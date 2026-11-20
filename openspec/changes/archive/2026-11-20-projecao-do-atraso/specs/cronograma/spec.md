## ADDED Requirements

### Requirement: Tarefa atrasada para a projeção

Para a projeção do atraso, MUST contar como atrasada a tarefa em aberto (abaixo de 100%), com datas próprias, que já devia ter terminado (o fim é anterior a hoje, ADR-0020) ou que devia ter começado (o início é anterior a hoje) e está em 0%. A tarefa-resumo, a tarefa concluída e o marco MUST não ter projeção, e o resto das telas MUST continuar contando o atraso só pelo fim (ADR-0020, ADR-0088, ADR-0164).

#### Scenario: Tarefa vencida tem projeção
- **WHEN** hoje é 19/10/2026 e uma tarefa terminou em 15/10/2026 a 40%
- **THEN** a tarefa tem projeção

#### Scenario: Tarefa que devia ter começado tem projeção
- **WHEN** hoje é 19/10/2026 e uma tarefa de 13/10/2026 a 23/10/2026 está em 0%
- **THEN** a tarefa tem projeção, e o cronograma não a conta entre as atrasadas

#### Scenario: Tarefa em dia não tem projeção
- **WHEN** hoje é 19/10/2026 e uma tarefa de 13/10/2026 a 23/10/2026 está em 30%, e outra começa hoje em 0%
- **THEN** nenhuma das duas tem projeção

### Requirement: Projeção do atraso

O servidor MUST projetar cada tarefa atrasada, sem gravar nada: o que falta (a duração vezes a parte que falta pelo progresso, em dias úteis, arredondada para cima, no mínimo 1) a partir de hoje, ou do próximo dia útil quando hoje não é útil; a tarefa em 0% começa nesse dia. As tarefas e os marcos que dependem dela MUST andar pelas regras das dependências só quando não cabem mais, e o item concluído MUST não andar (ADR-0018, ADR-0051, ADR-0137, ADR-0164).

#### Scenario: Tarefa atrasada com progresso
- **WHEN** hoje é 19/10/2026 e uma tarefa de 3 dias úteis terminou em 15/10/2026 a 40%
- **THEN** a projeção a termina em 20/10/2026, com 2 dias úteis que faltam, 3 dias úteis depois do fim planejado

#### Scenario: Tarefa que devia ter começado
- **WHEN** hoje é 19/10/2026 e uma tarefa de 5 dias úteis devia ter começado em 13/10/2026 e está em 0%
- **THEN** a projeção a começa em 19/10/2026 e a termina em 23/10/2026, 4 dias úteis depois do fim planejado

#### Scenario: Hoje não é dia útil
- **WHEN** hoje é o sábado 17/10/2026 e uma tarefa de 3 dias úteis terminou em 15/10/2026 a 40%
- **THEN** a projeção conta a partir da segunda 19/10/2026 e a termina em 20/10/2026

#### Scenario: Dependência que absorve o atraso pela folga
- **WHEN** a tarefa projetada para terminar em 23/10/2026 tem depois dela o marco Aceite da homologação, em 28/10/2026
- **THEN** a projeção não muda o marco nem o fim do projeto

#### Scenario: Item concluído não anda
- **WHEN** o item que depende da tarefa atrasada já foi concluído ou atingido
- **THEN** a projeção não o muda

### Requirement: O que a tarefa atrasada empurra

A projeção MUST dizer, para cada tarefa atrasada, o novo fim dela e quantos dias úteis depois do fim planejado; os marcos não atingidos que mudariam, pelo dia, com o dia de hoje, o projetado e quantos dias úteis andam; e o fim do projeto, quando muda, com o de hoje, o projetado e os dias úteis (ADR-0164).

#### Scenario: Marco empurrado pelas dependências
- **WHEN** a tarefa projetada para terminar em 23/10/2026 tem depois dela o marco Aceite da homologação, em 21/10/2026, e depois dele o marco Go-live, em 22/10/2026
- **THEN** a projeção leva Aceite da homologação e Go-live para 23/10/2026, 2 e 1 dias úteis depois

#### Scenario: Fim do projeto que muda
- **WHEN** a tarefa projetada para terminar em 23/10/2026 é a última do projeto, que termina em 19/10/2026
- **THEN** a projeção leva o fim do projeto para 23/10/2026, 4 dias úteis depois

### Requirement: Projeção do atraso pela API

O `GET /schedule` MUST trazer, em cada tarefa atrasada, a projeção (`projection`: o início e o fim projetados, os dias úteis depois do fim planejado, os marcos que mudariam e o fim do projeto, quando muda), para todos que veem o cronograma, e nada nos outros itens. Num projeto encerrado, nenhum item MUST ter projeção (ADR-0016, ADR-0043, ADR-0164).

#### Scenario: Projeção pela API
- **WHEN** uma pessoa do projeto pede o cronograma com uma tarefa atrasada de que depende o marco Go-live
- **THEN** a tarefa traz a projeção com o Go-live, e os itens em dia não trazem projeção

#### Scenario: Projeto encerrado não projeta
- **WHEN** o projeto com a tarefa atrasada é concluído
- **THEN** o cronograma não traz projeção em nenhum item

### Requirement: Projeção do atraso pela tela

O detalhe da tarefa atrasada MUST ter a seção "Projeção do atraso", para quem edita e para quem só vê: o novo fim e quantos dias úteis depois do planejado, cada marco que mudaria e o fim do projeto, com os dias úteis e o dia projetado, ou "Não muda nenhum marco nem o fim do projeto", e o aviso de que as datas só mudam quando quem edita o cronograma decidir. Sem projeção, a seção MUST não aparecer (ADR-0065, ADR-0140, ADR-0164).

#### Scenario: Projeção no detalhe
- **WHEN** alguém abre o detalhe de uma tarefa projetada para 20/10, 3 dias úteis depois do planejado, que empurra o marco Go-live em 3 dias úteis
- **THEN** a seção "Projeção do atraso" diz o novo fim, os 3 dias úteis e "Empurra o marco Go-live em 3 dias úteis"

#### Scenario: Atraso que não empurra nada
- **WHEN** alguém abre o detalhe de uma tarefa atrasada cujo atraso a folga absorve
- **THEN** a seção diz "Não muda nenhum marco nem o fim do projeto"

#### Scenario: Tarefa sem projeção
- **WHEN** alguém abre o detalhe de uma tarefa em dia
- **THEN** o detalhe não mostra a seção "Projeção do atraso"
