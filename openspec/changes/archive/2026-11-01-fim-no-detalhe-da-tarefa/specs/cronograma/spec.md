## MODIFIED Requirements

### Requirement: Tarefas e marcos

Uma tarefa MUST ter a fase, o nome, com até 200 caracteres, o início e a duração em dias úteis, de 1 a 999; o fim MUST ser calculado pelo sistema, contando o início e pulando os dias que não são úteis no calendário da instalação. No lugar da duração, o pedido MAY trazer o fim: a duração MUST ser então os dias úteis do início ao fim, os dois incluídos, e o fim antes do início, longe demais, num marco ou numa tarefa com subtarefas MUST receber 400 no fim. Um marco MUST ser uma data, sem duração, sem responsável e sem progresso. O tipo não muda depois da criação (ADR-0018, ADR-0019, ADR-0145).

#### Scenario: Criar tarefa
- **WHEN** o gerente inclui a tarefa "Levantar cadastros" começando na quinta-feira 08/10/2026, com 3 dias úteis, e 12/10/2026 é feriado
- **THEN** a tarefa termina na terça-feira 13/10/2026

#### Scenario: Marco sem duração
- **WHEN** o gerente inclui o marco "Aceite da homologação" em 03/11/2026
- **THEN** o marco começa e termina em 03/11/2026, com duração zero

#### Scenario: Tarefa inválida
- **WHEN** o gerente inclui uma tarefa sem nome, com duração zero, numa fase de outro projeto, ou um marco com responsável
- **THEN** a API responde 400 nos campos errados e nada é criado

#### Scenario: Criar tarefa pelo fim
- **WHEN** o gerente inclui uma tarefa começando na quinta-feira 08/10/2026 com o fim na terça-feira 13/10/2026, e 12/10/2026 é feriado, e depois muda o fim dela para 15/10/2026
- **THEN** a tarefa fica com 3 dias úteis e, depois, com 5, terminando em 15/10/2026

#### Scenario: Fim inválido
- **WHEN** o gerente inclui uma tarefa com o fim antes do início, ou manda o fim de um marco ou de uma tarefa com subtarefas
- **THEN** a API responde 400 no fim e nada muda

### Requirement: Editar o cronograma pela tela

Para quem edita o cronograma, com o projeto aberto, a aba MUST oferecer "Nova fase" e "Nova tarefa", que pede o tipo (tarefa ou marco), o nome, a fase, o início, o fim ou a duração, o responsável e de quem depende. Escolher uma tarefa MUST abrir o detalhe numa gaveta, com o fim calculado, para alterar ou excluir; escolher uma fase MUST permitir renomear ou excluir, com confirmação. Uma mensagem MUST dizer quantos itens foram empurrados. Para os outros, o detalhe é só leitura (ADR-0043, ADR-0081, ADR-0145).

#### Scenario: Nova tarefa pela tela
- **WHEN** o gerente escolhe "Nova tarefa", preenche o nome, a fase, o início e a duração, escolhe um responsável e confirma
- **THEN** a tarefa aparece na fase, com o fim calculado pelo sistema, e uma mensagem confirma a inclusão

#### Scenario: Empurrar pela tela
- **WHEN** o gerente aumenta no detalhe a duração de uma tarefa com duas tarefas seguintes e salva
- **THEN** as datas das três mudam na lista e a mensagem diz que 2 itens foram empurrados

#### Scenario: Excluir fase pela tela
- **WHEN** o gerente escolhe uma fase com tarefas, escolhe "Excluir fase" e confirma
- **THEN** a confirmação diz que as tarefas e os marcos da fase também saem, e a fase sai da lista

#### Scenario: Cronograma sem editar pela tela
- **WHEN** o líder técnico abre a aba Cronograma e escolhe uma tarefa
- **THEN** a tela não mostra "Nova fase" nem "Nova tarefa", e o detalhe mostra a tarefa sem permitir alterar ou excluir

## ADDED Requirements

### Requirement: Datas da tarefa antes de salvar

Para quem vê o projeto, a API MUST calcular, sem gravar nada, as datas de uma tarefa pelo calendário da instalação: pelo início e pela duração, o fim; pelo início e pelo fim, a duração; nos dois casos, com o início passado ao dia útil seguinte quando não é um. A duração fora de 1 a 999, o fim antes do início ou longe demais e o início inválido MUST receber 400 no campo; quem não vê o projeto MUST receber 404 (ADR-0018, ADR-0051, ADR-0145).

#### Scenario: Fim pela duração
- **WHEN** alguém do projeto pede as datas de uma tarefa começando no sábado 10/10/2026 com 3 dias úteis, e 12/10/2026 é feriado
- **THEN** a API devolve o início na terça-feira 13/10/2026, o fim na quinta-feira 15/10/2026 e os 3 dias úteis

#### Scenario: Duração pelo fim
- **WHEN** alguém do projeto pede as datas de uma tarefa começando na quinta-feira 08/10/2026 com o fim no sábado 17/10/2026, e 12/10/2026 é feriado
- **THEN** a API devolve 6 dias úteis e o fim na sexta-feira 16/10/2026

#### Scenario: Datas inválidas
- **WHEN** alguém do projeto pede as datas com o fim antes do início, com 1.000 dias úteis ou sem a duração nem o fim
- **THEN** a API responde 400 no campo errado

#### Scenario: Datas de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede as datas de uma tarefa dele
- **THEN** a API responde 404

### Requirement: Fim e duração pela tela

No detalhe da tarefa e em "Nova tarefa", a tarefa com datas próprias MUST ter o campo "Fim" ao lado da duração em dias úteis, os dois calculados um pelo outro pelo servidor enquanto a pessoa digita: vale o último digitado, e mudar o início mantém o que vale e recalcula o outro. Salvar MUST mandar o que vale, a duração ou o fim, mesmo antes de a conta chegar; o fim recusado MUST voltar ao campo. A tarefa-resumo e o marco MUST NOT ter o campo "Fim" (ADR-0051, ADR-0088, ADR-0145).

#### Scenario: Duração pelo fim digitado
- **WHEN** o gerente digita no detalhe de uma tarefa o fim 15/10/2026
- **THEN** a duração passa a ser a que o servidor calcula do início até esse fim

#### Scenario: Fim pela duração digitada
- **WHEN** o gerente digita em "Nova tarefa" o início e depois a duração de 5 dias úteis
- **THEN** o fim passa a ser o que o servidor calcula

#### Scenario: Salvar com o fim digitado
- **WHEN** o gerente digita o fim de uma tarefa no detalhe e salva
- **THEN** o pedido leva o fim, e o detalhe volta com as datas que o servidor gravou
