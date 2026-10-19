# Spec Delta

## MODIFIED Requirements

### Requirement: Cronograma na lista de projetos

Na lista, a coluna Cronograma MUST mostrar a faixa de fases, com a parte concluída, as fases atrasadas, os marcos e o dia de hoje, e embaixo o progresso com a fase atual: a primeira fase atrasada ou, sem atraso, a última que já começou; antes do início, "Começa em" e a data; com todas as tarefas a 100%, a data da conclusão. O Prazo final MUST mostrar o fim do cronograma e as tarefas e os marcos atrasados, com ícone e texto. Sem itens, "Sem cronograma" (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma na lista
- **WHEN** hoje é 16/10/2026 e a pessoa abre Projetos com um projeto de três fases, a segunda em andamento, que termina em 13/11/2026 com duas tarefas atrasadas na segunda fase
- **THEN** a linha do projeto mostra a faixa com as três fases, a segunda atrasada, o progresso com o nome da segunda fase, "13/11/2026" e "2 tarefas atrasadas"

#### Scenario: Projeto que ainda não começou
- **WHEN** hoje é 16/10/2026 e o cronograma de um projeto começa em 26/10/2026
- **THEN** a linha do projeto mostra a faixa e "Começa em 26/10"

#### Scenario: Projeto sem cronograma na lista
- **WHEN** um projeto não tem tarefas nem marcos
- **THEN** a linha do projeto mostra "Sem cronograma" e fica sem prazo final

#### Scenario: Marco atrasado na lista
- **WHEN** hoje é 16/10/2026 e um projeto tem uma tarefa e um marco atrasados
- **THEN** o prazo final do projeto diz "1 tarefa atrasada" e "1 marco atrasado"
