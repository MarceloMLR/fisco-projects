## MODIFIED Requirements

### Requirement: Início em dia útil

Tarefas e marcos MUST começar num dia útil do calendário da instalação: um início num fim de semana ou feriado MUST passar para o dia útil seguinte. As datas guardadas não mudam sozinhas quando o calendário muda depois: a próxima alteração da tarefa usa o calendário do momento, e o administrador pode recalcular as datas dos cronogramas em aberto (ADR-0018, ADR-0126).

#### Scenario: Início num sábado
- **WHEN** o gerente inclui uma tarefa começando no sábado 10/10/2026, e 12/10/2026 é feriado
- **THEN** a tarefa começa na terça-feira 13/10/2026

## ADDED Requirements

### Requirement: Recálculo pelo calendário

O administrador MUST poder ver quantas tarefas e marcos dos projetos em aberto mudam com o calendário atual e recalcular as datas deles. Cada item não concluído MUST manter o início, passado ao dia útil seguinte quando deixou de ser útil, e a duração em dias úteis, empurrado pelas dependências; os concluídos, a linha de base e os projetos encerrados MUST NOT mudar. Os responsáveis MUST ser avisados, e o recálculo MUST NOT entrar no desfazer (ADR-0126).

#### Scenario: Feriado novo no meio de uma tarefa
- **WHEN** a tarefa A vai de 26/10 a 30/10/2026, a tarefa B depende de A e vai de 03/11 a 04/11, e o administrador considera o Dia do Servidor Público (28/10) e recalcula as datas
- **THEN** A vai de 26/10 a 03/11 e B, de 04/11 a 05/11/2026

#### Scenario: Marco num feriado novo
- **WHEN** um marco não atingido está em 28/10/2026, e o administrador considera o Dia do Servidor Público e recalcula as datas
- **THEN** o marco passa para 29/10/2026

#### Scenario: Feriado desconsiderado não puxa as seguintes
- **WHEN** a tarefa A vai de 29/10 a 03/11/2026, a tarefa B depende de A e fica em 04/11, e o administrador desconsidera Finados e recalcula as datas
- **THEN** A termina em 02/11, e B continua em 04/11/2026

#### Scenario: Concluídos não mudam
- **WHEN** uma tarefa a 100% vai de 26/10 a 30/10/2026, um marco atingido está em 28/10, e o administrador considera o Dia do Servidor Público e recalcula as datas
- **THEN** a tarefa e o marco continuam com as mesmas datas

#### Scenario: Ver antes de recalcular
- **WHEN** o administrador considera o Dia do Servidor Público com uma tarefa de 26/10 a 30/10/2026 num projeto em andamento e pede o que o calendário atual muda
- **THEN** a API diz que 1 tarefa ou marco do projeto muda, e a tarefa continua de 26/10 a 30/10 até o recálculo

#### Scenario: Projeto encerrado fora do recálculo
- **WHEN** a mesma tarefa está num projeto concluído e o administrador recalcula as datas
- **THEN** o projeto não aparece no recálculo, e o cronograma dele não muda

#### Scenario: Responsável avisado do recálculo
- **WHEN** o recálculo muda as datas de uma tarefa de Carla
- **THEN** Carla recebe o aviso de que as datas da tarefa mudaram, e a auditoria registra a alteração da tarefa pelo administrador

#### Scenario: Recálculo só do administrador
- **WHEN** a gerente do projeto pede o que o calendário muda ou o recálculo
- **THEN** a API responde 403 e nada muda

#### Scenario: Recalcular de novo
- **WHEN** o administrador recalcula as datas duas vezes seguidas
- **THEN** a segunda vez não muda nada e não traz projetos

#### Scenario: Desfazer depois do recálculo
- **WHEN** a gerente desfaz a própria mudança de uma tarefa que o recálculo mudou depois
- **THEN** a API responde 409, a tarefa fica com as datas do recálculo e a mudança sai do histórico
