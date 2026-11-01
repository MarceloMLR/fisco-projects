## MODIFIED Requirements

### Requirement: Caminho crítico

O caminho crítico MUST ser calculado no servidor: as tarefas e os marcos com datas próprias sem folga, isto é, que não podem terminar um dia útil depois sem atrasar o fim do projeto, pelas dependências com o tipo e o intervalo e pelo calendário da instalação. O servidor MUST dizer a folga de cada um, em dias úteis. A tarefa-resumo MUST NOT entrar; as subtarefas, sim. Uma tarefa concluída continua no caminho crítico pelas datas (ADR-0018, ADR-0021, ADR-0051, ADR-0088, ADR-0137).

#### Scenario: Cadeia mais longa é crítica
- **WHEN** a tarefa A, de 3 dias úteis, vem antes da tarefa B, de 2, que vem antes do marco M, e a tarefa C, de 2 dias úteis, começa com A e não tem seguintes
- **THEN** A, B e M estão no caminho crítico, e C não

#### Scenario: Folga em dias úteis
- **WHEN** uma tarefa termina na sexta 09/10/2026, sem seguintes, e o projeto termina na terça 13/10/2026, com o feriado de 12/10/2026
- **THEN** a tarefa tem um dia útil de folga, com o último fim em 13/10/2026, e não está no caminho crítico

#### Scenario: Subtarefas no caminho crítico
- **WHEN** a subtarefa mais longa de uma tarefa-resumo leva ao fim do projeto
- **THEN** a subtarefa está no caminho crítico, e a tarefa-resumo não

#### Scenario: Folga da tarefa
- **WHEN** a tarefa A, de 5 dias úteis, e a tarefa B, de 2, começam juntas e vêm antes do marco M
- **THEN** A e M estão no caminho crítico, sem folga, e B tem 3 dias úteis de folga

#### Scenario: Folga com os tipos
- **WHEN** a tarefa B, de 2 dias úteis, termina junto com a tarefa A, de 5, que vem antes do marco M
- **THEN** B não tem folga e está no caminho crítico

## ADDED Requirements

### Requirement: Folga pela tela

O detalhe da tarefa e do marco MUST dizer a folga ("4 dias úteis de folga" ou "Sem folga: no caminho crítico"). Com o caminho crítico ligado em "Exibir", a Linha do tempo MUST desenhar, depois da barra de cada item com folga, uma linha fina até o último dia em que ele pode terminar, com "Folga" na legenda; o Excel MUST ter a coluna "Folga (dias úteis)" (ADR-0021, ADR-0071).

#### Scenario: Folga no detalhe
- **WHEN** uma pessoa do projeto abre o detalhe de uma tarefa com 4 dias úteis de folga e o de uma tarefa do caminho crítico
- **THEN** o primeiro diz "4 dias úteis de folga" e o segundo "Sem folga: no caminho crítico"

#### Scenario: Folga na Linha do tempo
- **WHEN** a Linha do tempo mostra, com o caminho crítico ligado, uma tarefa que termina em 28/10/2026 e pode terminar até 03/11/2026
- **THEN** uma linha fina vai do fim da barra até o fim de 03/11/2026, e a legenda diz "Folga"; com o caminho crítico desligado, a linha some

#### Scenario: Folga no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um cronograma com uma tarefa de 4 dias úteis de folga
- **THEN** a coluna "Folga (dias úteis)" da linha dela diz 4
