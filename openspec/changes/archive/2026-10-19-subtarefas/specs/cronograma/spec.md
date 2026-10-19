# Spec Delta

## ADDED Requirements

### Requirement: Subtarefas

Uma tarefa que está diretamente na fase MUST poder ter subtarefas, tarefas e marcos, num nível só e na fase dela. Subtarefa de subtarefa, subtarefa de marco, tarefa-resumo dentro de outra tarefa ou subtarefa numa fase diferente da tarefa MUST receber 400 em `parentId`. Mudar a fase da tarefa-resumo MUST levar as subtarefas junto (ADR-0088).

#### Scenario: Incluir subtarefa
- **WHEN** o gerente inclui a tarefa "Mapear regras de ICMS" e o marco "Regras aprovadas" dentro de "Parametrizar regras fiscais"
- **THEN** os dois ficam abaixo de "Parametrizar regras fiscais", na fase dela

#### Scenario: Subtarefa fora do nível
- **WHEN** o gerente tenta pôr uma tarefa dentro de uma subtarefa ou de um marco, ou uma tarefa-resumo dentro de outra tarefa
- **THEN** a API responde 400 em `parentId` e o cronograma continua como estava

#### Scenario: Mudar a fase da tarefa-resumo
- **WHEN** o gerente muda uma tarefa-resumo da fase Configuração para a fase Homologação
- **THEN** a tarefa e as subtarefas dela passam para Homologação

### Requirement: Tarefa-resumo

Uma tarefa com subtarefas MUST ter o início, o fim, a duração em dias úteis, o progresso ponderado pela duração, a conclusão e o atraso calculados das subtarefas. Nela só mudam o nome, a fase e o responsável; atualizar o progresso ou mandar novas datas MUST receber 400 (ADR-0020, ADR-0088).

#### Scenario: Datas e progresso da tarefa-resumo
- **WHEN** uma tarefa tem uma subtarefa de 05/10 a 06/10/2026 a 100% e outra de 07/10 a 19/10/2026, com 8 dias úteis, a 25%, e 12/10/2026 é feriado
- **THEN** a tarefa vai de 05/10 a 19/10/2026, com 10 dias úteis e 40%

#### Scenario: Tarefa-resumo atrasada
- **WHEN** hoje é 16/10/2026 e uma subtarefa terminou em 15/10/2026 a 80%
- **THEN** a subtarefa, a tarefa-resumo e a fase aparecem atrasadas, e o cronograma conta uma tarefa atrasada

#### Scenario: Progresso de uma tarefa-resumo
- **WHEN** o gerente tenta atualizar o progresso ou mudar as datas de uma tarefa-resumo
- **THEN** a API responde 400 e a tarefa continua como estava

### Requirement: Dependências com subtarefas

Só subtarefas, tarefas sem subtarefas e marcos MUST ter dependências, inclusive entre tarefas e fases diferentes; uma tarefa-resumo como anterior ou seguinte MUST receber 400. Quando uma tarefa ganha a primeira subtarefa, as dependências dela MUST passar para a subtarefa; quando a última subtarefa é excluída, as dela MUST voltar para a tarefa (ADR-0019, ADR-0088).

#### Scenario: Primeira subtarefa herda as dependências
- **WHEN** "Testes integrados" depende de "Parametrizar regras de ICMS", "Testes com a equipe do cliente" depende de "Testes integrados", e o gerente inclui nela a primeira subtarefa, "Roteiro de testes"
- **THEN** "Roteiro de testes" depende de "Parametrizar regras de ICMS", "Testes com a equipe do cliente" depende de "Roteiro de testes", e "Testes integrados" fica sem dependências

#### Scenario: Depender de uma tarefa-resumo
- **WHEN** o gerente tenta fazer uma tarefa depender de uma tarefa-resumo, ou uma tarefa-resumo depender de outra tarefa
- **THEN** a API responde 400 em `predecessorIds` e as dependências continuam como estavam

#### Scenario: Empurrar subtarefas
- **WHEN** uma subtarefa de uma tarefa atrasa e uma subtarefa de outra tarefa depende dela
- **THEN** a segunda subtarefa é empurrada, as duas tarefas-resumo passam a ter as novas datas, e a resposta conta um item empurrado

#### Scenario: Excluir a última subtarefa
- **WHEN** o gerente exclui a única subtarefa de uma tarefa, que dependia de outra tarefa
- **THEN** a tarefa volta a ser uma tarefa comum, com as datas e o progresso que tinha, e passa a depender da outra tarefa

### Requirement: Excluir tarefa-resumo

Excluir uma tarefa-resumo MUST excluir as subtarefas e as dependências delas, sem mover os outros itens do cronograma (ADR-0019, ADR-0088).

#### Scenario: Excluir tarefa com subtarefas
- **WHEN** o gerente exclui uma tarefa com duas subtarefas, e uma tarefa de outra fase depende de uma delas
- **THEN** a tarefa e as subtarefas saem do cronograma, e a outra tarefa perde a dependência e continua com as mesmas datas

### Requirement: Subtarefas pela tela

A Lista e a Linha do tempo MUST mostrar as subtarefas abaixo da tarefa, e a tarefa-resumo com as datas e o progresso calculados. Quem edita MUST poder incluir uma subtarefa por "Nova subtarefa" ou pelo campo "Dentro de". A tarefa-resumo MUST NOT se arrastar nem se ligar na Linha do tempo, e "Depende de" MUST NOT oferecer tarefas-resumo (ADR-0071, ADR-0081, ADR-0088).

#### Scenario: Subtarefas na lista
- **WHEN** uma pessoa do projeto abre a Lista de um cronograma com uma tarefa-resumo
- **THEN** as subtarefas aparecem abaixo dela, com um recuo a mais, e a tarefa-resumo mostra as datas, a duração e o progresso calculados

#### Scenario: Nova subtarefa pela tela
- **WHEN** o gerente abre o detalhe de uma tarefa sem subtarefas, escolhe "Nova subtarefa" e inclui uma tarefa
- **THEN** o formulário vem com "Dentro de" já escolhido e avisa que a tarefa passa a ser resumo, e a subtarefa aparece abaixo dela

#### Scenario: Subtarefas na linha do tempo
- **WHEN** o gerente abre a Linha do tempo de um cronograma com uma tarefa-resumo
- **THEN** a tarefa-resumo aparece como barra de resumo com as subtarefas abaixo, as subtarefas se arrastam e a tarefa-resumo não

#### Scenario: Detalhe da tarefa-resumo
- **WHEN** o gerente abre o detalhe de uma tarefa-resumo
- **THEN** o nome, a fase e o responsável se editam, e o início, o fim, a duração e o progresso aparecem calculados pelas subtarefas, sem editar

### Requirement: Subtarefas na auditoria

Incluir, mover e excluir subtarefas MUST entrar na auditoria como as tarefas, com a tarefa de "Dentro de" pelo nome atual; a mudança de datas da tarefa-resumo entra como alteração da tarefa (ADR-0039, ADR-0084).

#### Scenario: Subtarefa na auditoria
- **WHEN** o gerente inclui uma subtarefa e depois a move para outra tarefa
- **THEN** a auditoria tem "Incluiu uma tarefa" e "Alterou uma tarefa", com "Dentro de" pelo nome das duas tarefas

## MODIFIED Requirements

### Requirement: Datas e progresso das fases e do projeto

As datas de uma fase MUST ir do primeiro início ao último fim das tarefas e marcos dela, e as do projeto, do primeiro ao último de todo o cronograma. O progresso da fase e o do projeto MUST ser ponderados pela duração das tarefas, arredondados para o inteiro mais próximo; marcos não contam, e a tarefa-resumo também não, porque as subtarefas já contam. Uma fase sem tarefas nem marcos não tem datas (ADR-0016, ADR-0019, ADR-0020, ADR-0088).

#### Scenario: Progresso ponderado pela duração
- **WHEN** uma fase tem uma tarefa de 2 dias úteis a 100%, uma de 8 dias úteis a 25% e um marco
- **THEN** a fase está a 40%, e as datas dela vão do primeiro início ao último fim, inclusive o do marco

#### Scenario: Progresso sem contar a tarefa-resumo
- **WHEN** uma fase tem só uma tarefa-resumo, com uma subtarefa de 2 dias úteis a 100% e outra de 8 dias úteis a 25%
- **THEN** a tarefa-resumo e a fase estão a 40%

### Requirement: Tarefa atrasada

Uma tarefa MUST ficar atrasada quando o fim é anterior a hoje, no fuso da instalação, e o progresso está abaixo de 100%. Uma fase com uma tarefa atrasada MUST ficar atrasada. O cronograma MUST dizer quantas tarefas estão atrasadas, contando as subtarefas e não a tarefa-resumo (ADR-0020, ADR-0088).

#### Scenario: Tarefa vencida
- **WHEN** hoje é 16/10/2026 e uma tarefa terminou em 15/10/2026 a 80%
- **THEN** a tarefa e a fase dela aparecem atrasadas, e o cronograma conta uma tarefa atrasada

#### Scenario: Tarefa concluída ou do dia não fica atrasada
- **WHEN** hoje é 16/10/2026, uma tarefa terminou em 15/10/2026 a 100% e outra termina hoje a 50%
- **THEN** nenhuma das duas aparece atrasada
