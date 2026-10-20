## ADDED Requirements

### Requirement: Mensagens do chamado

O administrador, o gerente, o líder técnico, quem atende o time do chamado e o solicitante MUST poder mandar mensagens com negrito, listas e links, visíveis a todos que veem o chamado. Os outros membros MUST receber 403. Mensagens não se editam nem se apagam, e cada uma atualiza o chamado (ADR-0026, ADR-0044).

#### Scenario: Mandar mensagem
- **WHEN** o solicitante manda uma mensagem com um trecho em negrito
- **THEN** a mensagem aparece na conversa para todos que veem o chamado, com o negrito, e o chamado fica atualizado

#### Scenario: Leitor não manda mensagem
- **WHEN** um leitor do projeto tenta mandar uma mensagem
- **THEN** a API responde 403 e nada é gravado

#### Scenario: Texto formatado inválido
- **WHEN** alguém manda uma mensagem vazia ou com um elemento fora do negrito, das listas, dos links e das menções
- **THEN** a API responde 400 e nada é gravado

### Requirement: Menções no chamado

Quem manda mensagem MUST poder mencionar os gestores do projeto e os envolvidos no chamado: o solicitante, quem atende o time e o responsável. Menção a outra pessoa MUST receber 400. Quem é mencionado MUST passar a acompanhar o chamado (ADR-0026).

#### Scenario: Mencionar quem atende
- **WHEN** a solicitante menciona um desenvolvedor num chamado de Desenvolvimento
- **THEN** a mensagem é gravada e o desenvolvedor passa a acompanhar o chamado

#### Scenario: Menção fora dos envolvidos
- **WHEN** alguém menciona um colaborador num chamado de Desenvolvimento
- **THEN** a API responde 400 e nada é gravado

### Requirement: Acompanhar o chamado

O solicitante MUST acompanhar o chamado desde a abertura. Qualquer pessoa que vê o chamado MUST poder acompanhar e deixar de acompanhar. A lista MUST ter a visão "Que acompanho", com a contagem (ADR-0026, ADR-0068).

#### Scenario: Acompanhar um chamado
- **WHEN** um leitor do projeto escolhe acompanhar um chamado
- **THEN** o chamado entra na visão "Que acompanho" dele, e ele aparece em "Acompanham"

#### Scenario: Deixar de acompanhar
- **WHEN** quem acompanha escolhe deixar de acompanhar
- **THEN** o chamado sai da visão "Que acompanho" dessa pessoa

### Requirement: Anexos do chamado

Quem manda mensagem MUST poder anexar vários arquivos a ela, e o solicitante, à descrição logo depois de abrir o chamado. Cada arquivo MUST ter até 15 MB (configurável). Executáveis e scripts MUST ser recusados com 400. Os arquivos ficam na pasta do chamado e só quem vê o chamado MUST conseguir abri-los; imagens e PDF abrem no navegador (ADR-0026, ADR-0030).

#### Scenario: Anexar arquivos a uma mensagem
- **WHEN** alguém manda uma mensagem com uma imagem e um XML
- **THEN** os dois ficam na mensagem e em "Anexos da conversa", e a imagem abre no navegador

#### Scenario: Anexo recusado
- **WHEN** alguém anexa um arquivo acima do limite ou um executável
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Anexo fora do acesso
- **WHEN** alguém que não está no projeto pede um anexo de um chamado dele
- **THEN** a API responde 404

### Requirement: Conversa pela tela

A página do chamado MUST mostrar a descrição no topo e a conversa em duas colunas, com o solicitante à esquerda e os demais à direita. Cada mensagem MUST ter a cor e o selo do papel de quem escreveu e "(você)" na própria pessoa. A conversa MUST rolar por dentro, com a resposta fixa embaixo, o separador por dia e as interações recolhidas acima de 10 (ADR-0069).

#### Scenario: Cores e lados por papel
- **WHEN** a gerente abre um chamado com mensagens do solicitante, do líder técnico e dela
- **THEN** a do solicitante fica à esquerda e as outras à direita, cada uma com o selo e a cor do papel, e a dela com "(você)"

#### Scenario: Interações antigas recolhidas
- **WHEN** um chamado tem 14 mensagens
- **THEN** a conversa mostra as 10 últimas e "Mostrar 4 interações anteriores", que mostra as outras

#### Scenario: Responder pela tela
- **WHEN** quem atende digita uma resposta com uma menção e um anexo e escolhe "Enviar"
- **THEN** a mensagem aparece no fim da conversa, e o anexo, em "Anexos da conversa"

#### Scenario: Sem resposta para quem só vê
- **WHEN** um leitor abre o chamado
- **THEN** a conversa aparece sem a caixa de resposta, com "Acompanhar" no painel

## MODIFIED Requirements

### Requirement: Visões e filtros da lista

A lista MUST ter as visões "Atribuídos a mim", "Sem responsável nos meus times", "Abertos por mim", "Que acompanho" e "Todos", com a contagem dos chamados em aberto de cada uma, e os filtros por projeto, time e situação. A busca MUST achar pelo número, pelo título e pela descrição. A ordem MUST ser pela prioridade, a mais alta primeiro, ou pelo número. Cada chamado MUST mostrar o número de interações (ADR-0029, ADR-0068).

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
