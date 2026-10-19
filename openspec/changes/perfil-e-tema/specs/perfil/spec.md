## ADDED Requirements

### Requirement: Tema no perfil

Cada pessoa MUST poder escolher o tema claro, o escuro ou o automático, que segue o sistema operacional, no menu da conta ou na tela Meu perfil. A escolha MUST ser salva no perfil e valer na hora e, depois da entrada, em qualquer navegador. O padrão MUST ser o automático. A página de entrada MUST seguir sempre o sistema operacional. A troca de tema MUST NOT entrar na auditoria (ADR-0014).

#### Scenario: Tema segue o sistema
- **WHEN** a pessoa não escolheu um tema e o sistema operacional está no tema escuro, ou no claro
- **THEN** a aplicação abre no mesmo tema do sistema operacional

#### Scenario: Escolher o tema
- **WHEN** a pessoa escolhe o tema escuro no menu da conta
- **THEN** a aplicação passa na hora para o tema escuro e o perfil guarda a escolha

#### Scenario: Tema em outro navegador
- **WHEN** a pessoa escolheu o tema escuro e entra em outro navegador, com o sistema operacional no tema claro
- **THEN** depois da entrada a aplicação usa o tema escuro

#### Scenario: Página de entrada segue o sistema
- **WHEN** a pessoa com o tema escuro no perfil sai, e o sistema operacional está no tema claro
- **THEN** a página de entrada aparece no tema claro

#### Scenario: Tema fora da auditoria
- **WHEN** a pessoa troca o tema
- **THEN** a auditoria não ganha nenhum registro

#### Scenario: Troca de tema sem sessão
- **WHEN** alguém pede para trocar o tema sem uma sessão aberta
- **THEN** a API responde 401 e nada muda

### Requirement: Tela Meu perfil

A tela Meu perfil MUST mostrar o avatar, o nome, o e-mail e o papel no sistema da pessoa na sessão; o cartão Preferências com o tema em três opções, claro, escuro e automático, cada uma com uma miniatura do tema; e a Conta, com o papel no sistema, a forma de entrada e o último acesso, além do botão Sair. Ela MUST abrir pelo item Meu perfil do menu da conta e, no celular, do menu.

#### Scenario: Dados da conta
- **WHEN** a pessoa abre Meu perfil
- **THEN** a tela mostra o nome, o e-mail, o papel no sistema, a forma de entrada e o último acesso dela

#### Scenario: Tema na tela Meu perfil
- **WHEN** a pessoa escolhe outra opção de tema em Meu perfil
- **THEN** a opção fica marcada, a aplicação muda de tema na hora e o menu da conta mostra a mesma escolha

#### Scenario: Meu perfil pelo menu da conta
- **WHEN** a pessoa escolhe Meu perfil no menu da conta
- **THEN** a tela Meu perfil abre, com Meu perfil como título e na trilha
