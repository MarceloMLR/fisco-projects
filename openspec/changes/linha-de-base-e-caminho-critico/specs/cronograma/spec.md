## MODIFIED Requirements

### Requirement: Quem vê e quem edita o cronograma

Todas as pessoas do projeto e o administrador MUST ver o cronograma. Só o administrador e o gerente do projeto MUST poder editá-lo: fases, tarefas, marcos, dependências, responsáveis, o marco atingido e a linha de base. O líder técnico, o colaborador, o desenvolvedor e o leitor MUST receber 403 ao editar; o progresso das tarefas tem regra própria. Para quem não está no projeto e não é administrador, o cronograma não existe (404); sem sessão, 401 (ADR-0043).

#### Scenario: Gerente edita o cronograma
- **WHEN** o gerente do projeto inclui uma fase, uma tarefa e um marco, altera a tarefa e exclui o marco
- **THEN** a API aceita as cinco mudanças, e o cronograma mostra a fase e a tarefa alterada

#### Scenario: Administrador fora do projeto edita o cronograma
- **WHEN** o administrador, sem estar no projeto, vê o cronograma e inclui uma tarefa
- **THEN** a API aceita as duas ações

#### Scenario: Líder técnico só vê o cronograma
- **WHEN** o líder técnico vê o cronograma e depois tenta incluir, alterar ou excluir uma fase ou uma tarefa
- **THEN** a API mostra o cronograma, responde 403 às mudanças e nada muda

#### Scenario: Colaborador, desenvolvedor e leitor só veem o cronograma
- **WHEN** um colaborador, um desenvolvedor ou um leitor do projeto vê o cronograma e tenta alterar uma tarefa, mesmo uma da qual é responsável
- **THEN** a API mostra o cronograma, responde 403 à alteração e nada muda

#### Scenario: Cronograma de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede o cronograma ou tenta alterá-lo
- **THEN** a API responde 404

#### Scenario: Cronograma sem sessão
- **WHEN** alguém sem sessão pede o cronograma de um projeto
- **THEN** a API responde 401

## ADDED Requirements

### Requirement: Linha de base

O administrador e o gerente MUST poder salvar a linha de base do projeto: a cópia do início e do fim de cada tarefa e marco e do projeto, com o dia em que foi salva, no fuso da instalação. Salvar de novo MUST substituir a anterior. Cronograma sem tarefas nem marcos MUST receber 400, e projeto encerrado, 409 (ADR-0016, ADR-0021, ADR-0043).

#### Scenario: Salvar linha de base
- **WHEN** o gerente salva, em 16/10/2026, a linha de base de um cronograma que vai de 05/10/2026 a 03/11/2026
- **THEN** cada tarefa e marco guarda o próprio início e fim, e a linha de base é de 16/10/2026, de 05/10/2026 a 03/11/2026

#### Scenario: Atualizar linha de base
- **WHEN** o gerente muda o início de uma tarefa e salva a linha de base de novo, em 19/10/2026
- **THEN** a linha de base passa a ser de 19/10/2026, com as datas novas da tarefa

#### Scenario: Linha de base sem tarefas
- **WHEN** o gerente tenta salvar a linha de base de um cronograma só com fases
- **THEN** a API responde 400 e nada muda

#### Scenario: Linha de base num projeto encerrado
- **WHEN** o gerente tenta salvar a linha de base de um projeto concluído
- **THEN** a API responde 409 e a linha de base continua como estava

### Requirement: Diferença da linha de base

Com a linha de base salva, cada tarefa e marco que estava nela MUST dizer quantos dias úteis o fim está depois (positivo) ou antes (negativo) do fim na linha de base, e o cronograma MUST dizer o mesmo do fim do projeto. O item incluído depois MUST NOT ter linha de base (ADR-0018, ADR-0021).

#### Scenario: Tarefa depois da linha de base
- **WHEN** a linha de base guardou o fim de uma tarefa em 15/10/2026 e hoje a tarefa termina em 19/10/2026
- **THEN** a tarefa está 2 dias úteis depois da linha de base, 16/10 e 19/10, sem contar o fim de semana

#### Scenario: Tarefa antes da linha de base
- **WHEN** a linha de base guardou o fim de uma tarefa em 14/10/2026 e hoje a tarefa termina em 09/10/2026
- **THEN** a tarefa está 2 dias úteis antes da linha de base, sem contar o feriado de 12/10/2026

#### Scenario: Item incluído depois da linha de base
- **WHEN** o gerente inclui uma tarefa depois de salvar a linha de base
- **THEN** a tarefa não tem linha de base nem diferença

#### Scenario: Fim do projeto contra a linha de base
- **WHEN** a linha de base guardou o fim do projeto na sexta 30/10/2026 e hoje o projeto termina na quarta 04/11/2026
- **THEN** o cronograma está 2 dias úteis depois da linha de base, sem contar o fim de semana e o feriado de 02/11/2026

### Requirement: Caminho crítico

O caminho crítico MUST ser calculado no servidor: as tarefas e os marcos com datas próprias sem folga, isto é, que não podem terminar um dia útil depois sem atrasar o fim do projeto, pelas dependências "termina, então começa" e pelo calendário da instalação. A tarefa-resumo MUST NOT entrar; as subtarefas, sim. Uma tarefa concluída continua no caminho crítico pelas datas (ADR-0018, ADR-0021, ADR-0051, ADR-0088).

#### Scenario: Cadeia mais longa é crítica
- **WHEN** a tarefa A, de 3 dias úteis, vem antes da tarefa B, de 2, que vem antes do marco M, e a tarefa C, de 2 dias úteis, começa com A e não tem seguintes
- **THEN** A, B e M estão no caminho crítico, e C não

#### Scenario: Folga em dias úteis
- **WHEN** uma tarefa termina na sexta 09/10/2026, sem seguintes, e o projeto termina na terça 13/10/2026, com o feriado de 12/10/2026
- **THEN** a tarefa tem um dia útil de folga e não está no caminho crítico

#### Scenario: Subtarefas no caminho crítico
- **WHEN** a subtarefa mais longa de uma tarefa-resumo leva ao fim do projeto
- **THEN** a subtarefa está no caminho crítico, e a tarefa-resumo não

### Requirement: Linha de base pela tela

Para quem edita, o menu "⋮" da aba Cronograma MUST ter "Salvar linha de base" ou, com uma salva, "Atualizar linha de base", que pede confirmação. Com a linha de base ligada em "Exibir", a Linha do tempo MUST mostrar uma barra fina embaixo de cada barra. O resumo MUST mostrar a diferença do fim do projeto, e o detalhe, as datas salvas e a diferença do item (ADR-0021, ADR-0071, ADR-0081).

#### Scenario: Salvar linha de base pela tela
- **WHEN** o gerente escolhe "Salvar linha de base" no menu "⋮" de um cronograma sem linha de base
- **THEN** a tela avisa que a linha de base foi salva, e o resumo mostra o fim do projeto na data da linha de base

#### Scenario: Atualizar linha de base pela tela
- **WHEN** o gerente escolhe "Atualizar linha de base" e confirma
- **THEN** a tela avisa que a linha de base foi atualizada, e voltar na confirmação não muda nada

#### Scenario: Linha de base no detalhe
- **WHEN** uma pessoa do projeto abre o detalhe de uma tarefa que escorregou 2 dias úteis desde a linha de base
- **THEN** a seção Linha de base mostra o início e o fim salvos e "2 dias úteis depois"

#### Scenario: Sem editar, sem salvar a linha de base
- **WHEN** quem não edita o cronograma abre a aba Cronograma
- **THEN** a tela não oferece salvar nem atualizar a linha de base

### Requirement: Caminho crítico pela tela

Com o caminho crítico ligado em "Exibir", as barras das tarefas e os marcos do caminho crítico MUST ficar em vermelho, com a parte concluída em verde. "Exibir" MUST ligar e desligar a linha de base e o caminho crítico de forma independente, e no Expandir as duas opções ficam no cabeçalho. A legenda MUST dizer o que é cada cor (ADR-0065, ADR-0067, ADR-0071).

#### Scenario: Caminho crítico na linha do tempo
- **WHEN** uma pessoa do projeto abre a Linha do tempo de um cronograma com linha de base
- **THEN** as tarefas do caminho crítico aparecem em vermelho, cada tarefa com linha de base tem a barra fina embaixo, e a legenda tem "Caminho crítico" e "Linha de base"

#### Scenario: Exibir sem linha de base e caminho crítico
- **WHEN** a pessoa desmarca "Linha de base" e "Caminho crítico" em "Exibir"
- **THEN** a Linha do tempo some com as barras finas e o vermelho, e a legenda deixa de citá-los

### Requirement: Linha de base na auditoria

Salvar e atualizar a linha de base MUST entrar na auditoria como "Linha de base", pelo nome do projeto, com quem fez e o dia, o início e o fim antes e depois. A tela de Auditoria MUST ter o tipo "Linhas de base" no filtro. As datas de cada item na linha de base MUST NOT entrar uma a uma (ADR-0039, ADR-0084).

#### Scenario: Linha de base na auditoria
- **WHEN** o gerente salva a linha de base e depois a atualiza
- **THEN** a auditoria tem "Salvou a linha de base" e "Atualizou a linha de base" do projeto, com o gerente como autor e as datas antes e depois, e nenhum registro por tarefa
