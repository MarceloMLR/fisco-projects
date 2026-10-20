## ADDED Requirements

### Requirement: Trocar o responsável

Só o administrador, o gerente e o líder técnico MUST trocar o responsável de chamados em aberto, um a um ou em lote. O novo responsável MUST ser alguém do projeto que atende o time do chamado ou o gerente do projeto; passa a acompanhar o chamado, e a conversa registra a troca. Os outros MUST receber 403, e um responsável que não serve, 400 (ADR-0027, ADR-0044, ADR-0068).

#### Scenario: Atribuir um chamado
- **WHEN** o gerente atribui a um desenvolvedor um chamado de Desenvolvimento sem responsável
- **THEN** o desenvolvedor passa a ser o responsável e a acompanhar o chamado, e a conversa mostra a troca

#### Scenario: Atribuir em lote
- **WHEN** o líder técnico marca três chamados de Desenvolvimento e os atribui a um desenvolvedor
- **THEN** os três passam a ter o desenvolvedor como responsável

#### Scenario: Responsável fora do time
- **WHEN** o gerente tenta atribuir um chamado de Desenvolvimento a uma colaboradora do Funcional
- **THEN** a API responde 400 e o responsável não muda

#### Scenario: Quem atende não troca o responsável
- **WHEN** um desenvolvedor tenta atribuir um chamado a outro desenvolvedor
- **THEN** a API responde 403 e o responsável não muda

#### Scenario: Opções de responsável
- **WHEN** alguém pede as opções de responsável de chamados dos dois times do mesmo projeto
- **THEN** a API devolve só o gerente do projeto, que serve para os dois

### Requirement: Trocar o solicitante

O administrador e os gestores MUST trocar o solicitante de chamados não fechados, um a um ou em lote, e o próprio solicitante MUST trocar o dos chamados dele, um a um. O novo solicitante MUST ser alguém do projeto que abre chamados; o anterior continua acompanhando, e a conversa registra a troca (ADR-0027, ADR-0044).

#### Scenario: Solicitante passa o chamado
- **WHEN** a solicitante passa o próprio chamado para outra colaboradora
- **THEN** a outra colaboradora passa a ser a solicitante, a anterior continua acompanhando, e a conversa mostra a troca

#### Scenario: Solicitante não troca em lote
- **WHEN** a solicitante tenta trocar o solicitante de dois chamados de uma vez
- **THEN** a API responde 403 e nada muda

#### Scenario: Solicitante leitor
- **WHEN** o gerente tenta passar um chamado para um leitor do projeto
- **THEN** a API responde 400 e o solicitante não muda

### Requirement: Transferir de time

Quem atende o chamado e os gestores MUST transferir um chamado em aberto para o outro time, se ele tem alguém para atender. O chamado MUST ficar sem responsável, e a primeira resposta de alguém do novo time define o próximo. A conversa registra a transferência (ADR-0044, ADR-0068).

#### Scenario: Transferir para o Funcional
- **WHEN** o desenvolvedor responsável transfere um chamado de Desenvolvimento para o Funcional
- **THEN** o chamado fica no Funcional e sem responsável, e a primeira resposta de uma colaboradora a torna responsável

#### Scenario: Time sem ninguém
- **WHEN** alguém tenta transferir um chamado para um time sem ninguém no projeto
- **THEN** a API responde 400 e o time não muda

### Requirement: Chamados de quem sai

Quando alguém sai do projeto, ou muda para um papel que não pode atender o time de um chamado em aberto do qual é responsável, o chamado MUST ficar sem responsável, e a conversa registra o motivo. Os chamados de que a pessoa é solicitante continuam com ela (ADR-0042, ADR-0068).

#### Scenario: Responsável sai do projeto
- **WHEN** o gerente remove do projeto o desenvolvedor responsável por dois chamados
- **THEN** os dois chamados ficam sem responsável, e a conversa mostra o motivo

#### Scenario: Responsável muda de time
- **WHEN** o gerente troca para colaborador o papel do desenvolvedor responsável por um chamado de Desenvolvimento
- **THEN** o chamado fica sem responsável

### Requirement: Trocas pela tela

A lista MUST permitir marcar os chamados em que a pessoa pode trocar o responsável ou o solicitante, com a barra "N selecionados", "Atribuir a…", "Trocar solicitante" e "Limpar seleção", e o link "Atribuir" embaixo de "Sem responsável". O "⋮" do chamado MUST ter "Trocar responsável", "Trocar solicitante" e "Transferir para …", só para quem pode (ADR-0027, ADR-0068).

#### Scenario: Atribuir em lote pela tela
- **WHEN** o gerente marca dois chamados, escolhe "Atribuir a…", escolhe um desenvolvedor e confirma
- **THEN** os dois chamados mostram o desenvolvedor como responsável e a seleção é limpa

#### Scenario: Transferir pela tela
- **WHEN** quem atende escolhe "Transferir para Funcional" no "⋮" e confirma
- **THEN** o chamado mostra o time Funcional e "Sem responsável"

## MODIFIED Requirements

### Requirement: Visões e filtros da lista

A lista MUST ter as visões "Atribuídos a mim", "Sem responsável nos meus times", "Abertos por mim", "Que acompanho" e "Todos", com a contagem dos chamados em aberto de cada uma, e os filtros por projeto, time, situação e pessoa (responsável ou solicitante). A busca MUST achar pelo número, pelo título e pela descrição. A ordem MUST ser pela prioridade, a mais alta primeiro, ou pelo número. Cada chamado MUST mostrar o número de interações (ADR-0027, ADR-0029, ADR-0068).

#### Scenario: Sem responsável nos meus times
- **WHEN** um desenvolvedor abre a visão "Sem responsável nos meus times" num projeto com chamados sem responsável para os dois times
- **THEN** a visão traz só os do time de Desenvolvimento, e a do gerente traz os dos dois times

#### Scenario: Abertos por mim
- **WHEN** uma colaboradora abre a visão "Abertos por mim"
- **THEN** a visão traz só os chamados que ela abriu

#### Scenario: Busca nos chamados
- **WHEN** alguém busca pelo número de um chamado, por uma palavra do título ou por uma palavra da descrição
- **THEN** a lista traz o chamado correspondente

#### Scenario: Filtros da lista
- **WHEN** alguém filtra por projeto, por time ou pelos encerrados
- **THEN** a lista traz só os chamados correspondentes, dentro do acesso da pessoa

#### Scenario: Interações na lista
- **WHEN** um chamado tem três mensagens
- **THEN** a lista mostra 3 nas interações dele

#### Scenario: Chamados de uma pessoa
- **WHEN** o gerente filtra a lista por um desenvolvedor
- **THEN** a lista traz os chamados de que o desenvolvedor é responsável ou solicitante
