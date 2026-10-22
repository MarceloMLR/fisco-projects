## MODIFIED Requirements

### Requirement: Tela Meu perfil

A tela Meu perfil MUST mostrar o avatar, o nome, o e-mail e o papel no sistema da pessoa na sessão; o cartão Preferências com o tema em três opções, claro, escuro e automático, cada uma com uma miniatura do tema; e a Conta, com o papel no sistema, a forma de entrada e o último acesso, além do botão Sair. Ela MUST abrir pelo item Meu perfil do menu da conta e, no celular, do menu. No celular e em janelas estreitas, a Conta MUST ficar abaixo das Preferências e dos Avisos por e-mail, cada parte com a altura do próprio conteúdo, sem uma cobrir a outra, e a página MUST rolar inteira.

#### Scenario: Dados da conta
- **WHEN** a pessoa abre Meu perfil
- **THEN** a tela mostra o nome, o e-mail, o papel no sistema, a forma de entrada e o último acesso dela

#### Scenario: Tema na tela Meu perfil
- **WHEN** a pessoa escolhe outra opção de tema em Meu perfil
- **THEN** a opção fica marcada, a aplicação muda de tema na hora e o menu da conta mostra a mesma escolha

#### Scenario: Meu perfil pelo menu da conta
- **WHEN** a pessoa escolhe Meu perfil no menu da conta
- **THEN** a tela Meu perfil abre, com Meu perfil como título e na trilha

#### Scenario: Meu perfil no celular
- **WHEN** a pessoa abre Meu perfil no celular
- **THEN** as Preferências e os Avisos por e-mail aparecem inteiros, a Conta e o Sair logo abaixo deles, sem cobri-los, e a página rola até o fim
