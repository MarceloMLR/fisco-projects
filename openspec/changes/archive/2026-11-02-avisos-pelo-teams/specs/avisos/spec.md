## ADDED Requirements

### Requirement: Avisos na hora pelo Teams

Com os avisos pelo Teams ligados na instalação, cada aviso que sai por e-mail na hora MUST ir também para a aba Atividade do Teams de cada pessoa que o recebe e tem o Teams no canal dos avisos na hora. Valem as mesmas regras de quem recebe (ADR-0038, ADR-0149). Os avisos que por e-mail vão para o resumo diário MUST NOT ir para o Teams.

#### Scenario: Chamado atribuído no Teams
- **WHEN** o gerente passa um chamado para uma pessoa que tem o Teams no canal
- **THEN** a pessoa recebe na aba Atividade o aviso do chamado atribuído

#### Scenario: Quem fez a ação não recebe no Teams
- **WHEN** quem atende responde um chamado que ele mesmo acompanha
- **THEN** o aviso da mensagem vai para o Teams do solicitante, e não para o de quem respondeu

#### Scenario: Aviso do resumo fora do Teams
- **WHEN** muda o status de um chamado que a pessoa acompanha
- **THEN** a pessoa não recebe nada no Teams, e o aviso fica no sino e no resumo diário

#### Scenario: Teams desligado na instalação
- **WHEN** um chamado é atribuído com os avisos pelo Teams desligados
- **THEN** a pessoa recebe só o e-mail, e nada vai para o Teams

### Requirement: Canal dos avisos na hora

Cada pessoa MUST receber os avisos na hora pelo canal que escolheu: E-mail, Teams ou Os dois. Quem nunca escolheu MUST ficar com Os dois. O Teams MUST valer só com os avisos pelo Teams ligados e para quem pode recebê-los: com o envio pelo Graph, quem entra pelo Entra ID; com a implementação local, qualquer pessoa ativa. Para os outros, os avisos na hora MUST ir por e-mail, qualquer que seja a escolha (ADR-0037, ADR-0149).

#### Scenario: Só por e-mail
- **WHEN** um chamado é atribuído a uma pessoa que escolheu E-mail
- **THEN** a pessoa recebe o e-mail, e nada vai para o Teams

#### Scenario: Só pelo Teams
- **WHEN** um chamado é atribuído a uma pessoa que escolheu Teams
- **THEN** a pessoa recebe o aviso no Teams e não recebe o e-mail

#### Scenario: Pelos dois
- **WHEN** um chamado é atribuído a uma pessoa que nunca escolheu o canal
- **THEN** a pessoa recebe o e-mail e o aviso no Teams

#### Scenario: Pessoa sem o Entra ID
- **WHEN** um chamado é atribuído, com o envio pelo Graph, a uma pessoa que escolheu Teams e entra sem o Entra ID
- **THEN** a pessoa recebe o e-mail, e nada vai para o Teams

#### Scenario: Sino com qualquer canal
- **WHEN** um chamado é atribuído a uma pessoa que escolheu Teams
- **THEN** a notificação também aparece no sino do FiscoProjects

### Requirement: Volta ao e-mail

Quem escolheu só o Teams MUST receber o aviso por e-mail quando o aplicativo do Teams não está instalado para ela, quando o Teams recusa o envio de vez ou quando as tentativas acabam. O aviso MUST NOT ficar sem chegar por nenhum dos dois (ADR-0149).

#### Scenario: Aplicativo não instalado para a pessoa
- **WHEN** um chamado é atribuído a uma pessoa que escolheu Teams e não tem o aplicativo do Teams instalado
- **THEN** o aviso não vai para o Teams, e a pessoa recebe o e-mail

#### Scenario: Teams recusa o envio
- **WHEN** o Graph recusa de vez o aviso para uma pessoa que escolheu Teams
- **THEN** o aviso fica como Falhou, e a pessoa recebe o e-mail

#### Scenario: Recusa com os dois canais
- **WHEN** o Graph recusa de vez o aviso para uma pessoa que escolheu Os dois
- **THEN** o aviso fica como Falhou, e a pessoa recebe só o e-mail que já tinha saído

### Requirement: Fila dos avisos do Teams

Os avisos do Teams MUST entrar numa fila no banco, gravada junto com a ação que os gerou, e sair em segundo plano. Um envio limitado ou com o Teams fora do ar MUST ser tentado de novo depois de 1 min, 5 min, 15 min, 1 h e 4 h e ficar como Falhou na sexta falha. A ação MUST NOT depender do envio. O que a fila guarda MUST ser apagado `Email:KeepDays` dias depois de enviado ou de falhar, e com o projeto excluído (ADR-0036, ADR-0058, ADR-0149).

#### Scenario: Ação sem esperar o Teams
- **WHEN** um chamado é atribuído com o Teams fora do ar
- **THEN** a atribuição é gravada na hora, e o aviso fica na fila

#### Scenario: Teams fora do ar
- **WHEN** o Graph responde 503 a um aviso do Teams
- **THEN** o aviso continua na fila para a próxima tentativa

#### Scenario: Fila limpa com o tempo
- **WHEN** um aviso do Teams foi enviado há mais de `Email:KeepDays` dias
- **THEN** a limpeza o apaga da fila

#### Scenario: Projeto excluído
- **WHEN** o administrador exclui um projeto com avisos do Teams na fila
- **THEN** os avisos do Teams do projeto saem com ele

### Requirement: Aviso na aba Atividade

O aviso no Teams MUST dizer o que aconteceu e quem fez, pelo nome atual. Embaixo, MUST vir o código do projeto e o título do item, com os textos dos arquivos de tradução. O clique MUST abrir a página do FiscoProjects no Teams com o caminho do item. O aviso MUST NOT levar senhas nem tokens (ADR-0013, ADR-0149).

#### Scenario: Texto do aviso
- **WHEN** Marina Costa atribui o chamado #104 do ERP-FIN a uma pessoa que tem o Teams no canal
- **THEN** o Graph recebe o aviso do tipo do chamado atribuído, com o nome de Marina Costa, o número do chamado e o título com o código do projeto

#### Scenario: Link do aviso
- **WHEN** sai o aviso de um chamado pelo Teams
- **THEN** o clique leva à página do FiscoProjects no Teams com o caminho da página do chamado

### Requirement: Página do FiscoProjects no Teams

A página do FiscoProjects no Teams MUST abrir sem sessão e mostrar só a marca da instalação e o botão "Abrir no FiscoProjects". O botão MUST levar ao item no navegador. Sem item, MUST levar ao Início. A página MUST aceitar só caminhos do próprio FiscoProjects (ADR-0149).

#### Scenario: Abrir o item no navegador
- **WHEN** a pessoa clica no aviso do chamado #104 no Teams e escolhe "Abrir no FiscoProjects"
- **THEN** a página do chamado #104 abre no navegador, no endereço da instalação

#### Scenario: Página sem item
- **WHEN** a pessoa abre o FiscoProjects pela barra do Teams
- **THEN** "Abrir no FiscoProjects" leva ao Início

#### Scenario: Endereço de fora recusado
- **WHEN** a página do Teams recebe o endereço de outro site no lugar do caminho do item
- **THEN** "Abrir no FiscoProjects" leva ao Início, e não ao outro site

### Requirement: Implementação local do Teams

Fora da produção, os avisos do Teams MUST poder ficar gravados como arquivos, sem sair para ninguém, com quem recebe, o tipo, os textos e o link. A aplicação MUST NOT subir em produção com a implementação local (ADR-0004, ADR-0149).

#### Scenario: Aviso gravado sem envio
- **WHEN** a fila envia um aviso do Teams com a implementação local
- **THEN** um arquivo com quem recebe, o tipo, os textos e o link fica na pasta configurada

#### Scenario: Implementação local do Teams em produção
- **WHEN** a aplicação sobe em produção com `Teams:Provider` = `Local`
- **THEN** a aplicação não sobe e diz por quê

### Requirement: Configuração do Teams conferida

A aplicação MUST NOT subir com o envio pelo Teams pelo Graph sem a entrada pelo Entra ligada (ADR-0117, ADR-0149).

#### Scenario: Teams sem a entrada pelo Entra
- **WHEN** a aplicação sobe com `Teams:Provider` = `Graph` e a entrada pelo Entra desligada
- **THEN** a aplicação não sobe e diz que o Teams precisa da entrada pelo Entra
