## ADDED Requirements

### Requirement: Cor secundária

O administrador MUST poder escolher uma cor secundária em hexadecimal, qualquer cor, ou nenhuma. Sem ela, as telas MUST usar o design system. Com ela, nos dois temas, a barra lateral, a linha selecionada, a notificação não lida, a menção e o painel da entrada MUST segui-la, com os textos sobre ela com contraste de 4,5:1; as cores de status MUST NOT mudar. Uma cor fora do formato MUST ser recusada (ADR-0100).

#### Scenario: Sem cor secundária
- **WHEN** ninguém escolheu a cor secundária
- **THEN** a barra lateral, os destaques e o painel da entrada usam as cores do design system

#### Scenario: Trocar a cor secundária
- **WHEN** o administrador escolhe um azul-marinho como cor secundária e salva
- **THEN** a barra lateral passa a ser azul-marinho no tema claro e um tom escuro dele no escuro, a linha selecionada ganha o matiz dele e o painel da entrada sai dele

#### Scenario: Contraste na barra lateral
- **WHEN** a cor secundária é clara, escura, branca ou preta
- **THEN** os textos, os contadores, o item ativo e o nome da instalação na barra lateral e o texto do painel da entrada têm contraste de pelo menos 4,5:1, nos dois temas

#### Scenario: Cor secundária inválida
- **WHEN** o administrador envia uma cor secundária fora do formato `#RRGGBB`
- **THEN** a API responde 400 e nada muda

#### Scenario: Cor secundária pela tela
- **WHEN** o administrador digita a cor secundária nas Configurações gerais
- **THEN** a Prévia mostra a barra lateral com a cor antes de salvar, e "Sem cor secundária" volta ao design system

#### Scenario: Cor secundária e logo escuro na auditoria
- **WHEN** o administrador troca a cor secundária e envia o logo para o tema escuro
- **THEN** a auditoria registra a cor e o nome do arquivo do logo

### Requirement: Logo para o tema escuro

O administrador MUST poder enviar, trocar e remover um logo para o tema escuro, opcional, com as regras do logo. A barra lateral escura (o tema escuro, ou uma cor secundária escura) MUST mostrar o logo escuro, quando há; sem ele, o logo. O selo do painel da entrada MUST continuar com o logo (ADR-0100).

#### Scenario: Logo para o tema escuro pela API
- **WHEN** o administrador envia o logo para o tema escuro e depois o remove
- **THEN** a marca traz o endereço dele enquanto existe, o logo continua o mesmo, e um arquivo que não é PNG nem SVG é recusado

#### Scenario: Enviar o logo para o tema escuro
- **WHEN** o administrador escolhe o logo para o tema escuro nas Configurações gerais e salva
- **THEN** o logo aparece num quadro escuro, é enviado, e "Remover logo escuro" o tira

#### Scenario: Logo escuro na barra lateral escura
- **WHEN** a barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral e a Prévia mostram o logo escuro, e o selo da entrada mostra o logo

#### Scenario: Sem logo escuro
- **WHEN** a barra lateral é escura e a instalação não tem o logo escuro
- **THEN** a barra lateral mostra o logo

### Requirement: Seletor de cores

Clicar na amostra da cor principal ou da secundária MUST abrir o seletor de cores do navegador, e a cor escolhida MUST ir para o campo em hexadecimal, com as regras de quem digita (ADR-0100).

#### Scenario: Escolher a cor pelo seletor
- **WHEN** o administrador escolhe as cores no seletor
- **THEN** os campos mostram as cores em hexadecimal e o formulário fica pronto para salvar
