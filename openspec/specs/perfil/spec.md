# perfil Specification

## Purpose

Define o perfil da pessoa na sessão: o tema escolhido e salvo no perfil, que vale em qualquer navegador, e a tela Meu perfil, com as preferências e os dados da conta (ADR-0014, ADR-0066).

## Requirements

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

### Requirement: Meu perfil no celular

No celular e em janelas estreitas, a Conta de Meu perfil MUST ficar abaixo das Preferências e dos Avisos por e-mail, cada parte com a altura do próprio conteúdo, sem uma cobrir a outra, e a página MUST rolar inteira (ADR-0066).

#### Scenario: Meu perfil no celular
- **WHEN** a pessoa abre Meu perfil no celular
- **THEN** as Preferências e os Avisos por e-mail aparecem inteiros, a Conta e o Sair logo abaixo deles, sem cobri-los, e a página rola até o fim

### Requirement: Canais dos avisos no perfil

O cartão "Avisos" de Meu perfil MUST mostrar três caixas, cada uma gravada na hora (ADR-0037, ADR-0151): "Por e-mail", marcada de início; "Pelo Teams", desmarcada de início e só com o Teams disponível para a pessoa; e o resumo diário, marcado de início. Sem nenhum canal marcado, o cartão MUST dizer que os avisos continuam no sino e no Meu trabalho. Uma gravação que falha MUST voltar a caixa ao que estava.

#### Scenario: Desligar o resumo
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a escolha fica gravada, e ela deixa de receber o resumo

#### Scenario: Desligar o e-mail
- **WHEN** a pessoa sem o Teams disponível desmarca "Por e-mail" no cartão Avisos
- **THEN** a escolha fica gravada, o cartão diz que os avisos continuam no sino e no Meu trabalho, e os próximos avisos na hora não chegam por e-mail

#### Scenario: Escolher o Teams
- **WHEN** a pessoa com o Teams disponível marca "Pelo Teams" e desmarca "Por e-mail"
- **THEN** as escolhas ficam gravadas, e os próximos avisos na hora chegam só pelo Teams

#### Scenario: Canal de quem nunca escolheu
- **WHEN** a pessoa com o Teams disponível abre o cartão Avisos pela primeira vez
- **THEN** "Por e-mail" aparece marcado e "Pelo Teams" desmarcado

#### Scenario: Ajuda do aplicativo do Teams
- **WHEN** a pessoa com o Teams disponível abre o cartão Avisos
- **THEN** "Pelo Teams" diz que os avisos só chegam com o aplicativo do FiscoProjects no Teams dela

#### Scenario: Teams fora do cartão
- **WHEN** a pessoa abre o cartão Avisos com os avisos pelo Teams desligados na instalação
- **THEN** o cartão mostra só "Por e-mail" e o resumo diário

#### Scenario: Falha ao gravar
- **WHEN** a gravação de "Por e-mail" falha
- **THEN** a caixa volta ao que estava, e um aviso diz que não foi possível salvar

### Requirement: Escolhas dos avisos na auditoria

Ligar e desligar os avisos por e-mail, pelo Teams e o resumo diário MUST entrar na auditoria, com a pessoa da escolha, quem mudou, quando e o valor antes e depois (ADR-0039, ADR-0151). Uma gravação que não muda nada MUST NOT entrar.

#### Scenario: E-mail desligado na auditoria
- **WHEN** a pessoa desmarca "Por e-mail" em Meu perfil
- **THEN** a Auditoria mostra que ela desligou os avisos por e-mail, com o nome dela, a data e a hora

#### Scenario: Teams ligado na auditoria
- **WHEN** a pessoa marca "Pelo Teams" em Meu perfil
- **THEN** a Auditoria mostra que ela ligou os avisos pelo Teams

#### Scenario: Resumo diário na auditoria
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a Auditoria mostra que ela desligou o resumo diário

#### Scenario: Escolha sem mudança fora da auditoria
- **WHEN** a pessoa grava de novo as mesmas escolhas
- **THEN** nenhum registro novo entra na Auditoria
