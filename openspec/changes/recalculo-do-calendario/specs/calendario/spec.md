## ADDED Requirements

### Requirement: Recálculo das datas pela tela

Calendário e feriados MUST avisar no topo quando tarefas e marcos de projetos em aberto ainda seguem o calendário anterior, com a quantidade e "Recalcular as datas", conferindo ao abrir a tela e depois de cada mudança gravada. "Recalcular as datas" MUST pedir confirmação, mostrando os projetos, e depois dizer em quantos projetos as datas foram recalculadas (ADR-0126).

#### Scenario: Aviso depois de mudar o calendário
- **WHEN** o administrador considera um feriado que cai numa tarefa não concluída de um projeto em aberto
- **THEN** a tela mostra no topo que 1 tarefa ou marco de 1 projeto em aberto ainda segue o calendário anterior, com "Recalcular as datas"

#### Scenario: Recalcular as datas pela tela
- **WHEN** o administrador escolhe "Recalcular as datas" e confirma no diálogo, que mostra o projeto e a quantidade
- **THEN** a tela avisa "Datas recalculadas em 1 projeto." e o aviso some

#### Scenario: Cronogramas em dia
- **WHEN** nenhum cronograma em aberto muda com o calendário atual
- **THEN** a tela não mostra o aviso
