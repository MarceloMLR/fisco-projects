## REMOVED Requirements

### Requirement: Cor secundária
**Reason**: Marcelo pediu para voltar a ter só a cor principal na marca (ADR-0103).
**Migration**: A barra lateral, os destaques e o painel da entrada usam as cores do design system; a cor principal continua derivando os tokens da marca.

### Requirement: Cores das mensagens
**Reason**: Marcelo pediu para tirar as cores das mensagens configuráveis (ADR-0103).
**Migration**: A conversa dos chamados usa as cores de cada papel do design system (ADR-0069).

## MODIFIED Requirements

### Requirement: Logo para o tema escuro

O administrador MUST poder enviar, trocar e remover um logo para o tema escuro, opcional, com as regras do logo. A barra lateral escura (a do tema escuro) MUST mostrar o logo escuro, quando há; sem ele, o logo. No tema claro, a barra lateral MUST mostrar o logo. O selo do painel da entrada MUST continuar com o logo (ADR-0100, ADR-0103).

#### Scenario: Logo para o tema escuro pela API
- **WHEN** o administrador envia o logo para o tema escuro e depois o remove
- **THEN** a marca traz o endereço dele enquanto existe, o logo continua o mesmo, e um arquivo que não é PNG nem SVG é recusado

#### Scenario: Enviar o logo para o tema escuro
- **WHEN** o administrador escolhe o logo para o tema escuro nas Configurações gerais e salva
- **THEN** o logo aparece num quadro escuro, é enviado, e "Remover logo escuro" o tira

#### Scenario: Logo escuro na barra lateral escura
- **WHEN** a barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral e a Prévia mostram o logo escuro, e o selo da entrada mostra o logo

#### Scenario: Logo claro na barra lateral clara
- **WHEN** o tema é o claro e a instalação tem os dois logos
- **THEN** a barra lateral e a Prévia mostram o logo

#### Scenario: Sem logo escuro
- **WHEN** a barra lateral é escura e a instalação não tem o logo escuro
- **THEN** a barra lateral mostra o logo

#### Scenario: Logo escuro na auditoria
- **WHEN** o administrador envia o logo para o tema escuro
- **THEN** a auditoria registra o nome do arquivo

### Requirement: Seletor de cores

Clicar na amostra da cor principal MUST abrir o seletor de cores do navegador, e a cor escolhida MUST ir para o campo em hexadecimal, com as regras de quem digita (ADR-0100).

#### Scenario: Escolher a cor pelo seletor
- **WHEN** o administrador escolhe a cor principal no seletor
- **THEN** o campo mostra a cor em hexadecimal e o formulário fica pronto para salvar
