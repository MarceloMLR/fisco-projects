## MODIFIED Requirements

### Requirement: Avisos dos chamados na hora

Os eventos que pedem ação MUST gerar o aviso na hora (ADR-0037): o chamado aberto ou transferido para quem atende o time dele; a mensagem para quem acompanha o chamado; a menção para quem foi mencionado; o pedido de informação e a resolução para o solicitante; a troca de responsável para o novo responsável; e a troca de solicitante para o novo solicitante. Quem recebe um aviso mais específico da mesma ação (a menção, o pedido de informação, a resolução) MUST NOT receber também o da mensagem.

#### Scenario: Chamado aberto para o time
- **WHEN** uma pessoa abre um chamado para o time Desenvolvimento
- **THEN** o líder técnico e os desenvolvedores do projeto recebem o e-mail na hora

#### Scenario: Resposta no chamado
- **WHEN** quem atende responde um chamado
- **THEN** o solicitante e quem acompanha o chamado recebem o e-mail da mensagem na hora

#### Scenario: Menção
- **WHEN** uma mensagem menciona o gerente do projeto
- **THEN** o gerente recebe um e-mail só, o da menção

#### Scenario: Pedido de informação
- **WHEN** quem atende pede informações ao solicitante
- **THEN** o solicitante recebe o e-mail do pedido, e não o da mensagem

#### Scenario: Chamado resolvido
- **WHEN** quem atende resolve o chamado
- **THEN** o solicitante recebe o e-mail pedindo a confirmação

#### Scenario: Chamado atribuído
- **WHEN** o gerente passa o chamado para uma pessoa
- **THEN** essa pessoa recebe o e-mail na hora

#### Scenario: Novo solicitante
- **WHEN** o solicitante é trocado
- **THEN** o novo solicitante recebe o e-mail na hora

#### Scenario: Chamado transferido
- **WHEN** um chamado é transferido para o time Funcional
- **THEN** os colaboradores do projeto recebem o e-mail na hora

### Requirement: Avisos na hora pelo Teams

Com os avisos pelo Teams ligados na instalação, cada aviso que sai na hora MUST ir para a aba Atividade do Teams de cada pessoa que o recebe e tem o Teams ligado e disponível. Valem as mesmas regras de quem recebe (ADR-0038, ADR-0149, ADR-0151). Os avisos que por e-mail vão para o resumo diário MUST NOT ir para o Teams.

#### Scenario: Chamado atribuído no Teams
- **WHEN** o gerente passa um chamado para uma pessoa que tem o Teams ligado
- **THEN** a pessoa recebe na aba Atividade o aviso do chamado atribuído

#### Scenario: Quem fez a ação não recebe no Teams
- **WHEN** quem atende responde um chamado que ele mesmo acompanha
- **THEN** o aviso da mensagem vai para o Teams do solicitante, e não para o de quem respondeu

#### Scenario: Aviso do resumo fora do Teams
- **WHEN** muda o status de um chamado que a pessoa acompanha
- **THEN** a pessoa não recebe nada no Teams, e o aviso fica no sino e no resumo diário

#### Scenario: Teams desligado na instalação
- **WHEN** um chamado é atribuído, com os avisos pelo Teams desligados, a uma pessoa com os dois canais ligados
- **THEN** a pessoa recebe só o e-mail, e nada vai para o Teams

### Requirement: Canal dos avisos na hora

Cada pessoa MUST receber os avisos na hora pelos canais que ligou, o e-mail e o Teams; quem nunca escolheu MUST ficar com o e-mail ligado e o Teams desligado. O Teams MUST valer só com os avisos pelo Teams ligados e para quem pode recebê-los (com o Graph, quem entra pelo Entra ID; com a implementação local, qualquer pessoa ativa); fora disso, a escolha MUST ficar guardada, sem efeito. O sino MUST receber todo aviso, e com os dois canais desligados o aviso MUST ficar só nele (ADR-0151).

#### Scenario: Só por e-mail
- **WHEN** um chamado é atribuído a uma pessoa com o e-mail ligado e o Teams desligado
- **THEN** a pessoa recebe o e-mail, e nada vai para o Teams

#### Scenario: Só pelo Teams
- **WHEN** um chamado é atribuído a uma pessoa com o Teams ligado e o e-mail desligado
- **THEN** a pessoa recebe o aviso no Teams e não recebe o e-mail

#### Scenario: Pelos dois
- **WHEN** um chamado é atribuído a uma pessoa com os dois canais ligados
- **THEN** a pessoa recebe o e-mail e o aviso no Teams

#### Scenario: Pessoa sem o Entra ID
- **WHEN** um chamado é atribuído, com o envio pelo Graph, a uma pessoa com os dois canais ligados que entra sem o Entra ID
- **THEN** a pessoa recebe só o e-mail, e nada vai para o Teams

#### Scenario: Sino com qualquer canal
- **WHEN** um chamado é atribuído a uma pessoa com o Teams ligado e o e-mail desligado
- **THEN** a notificação também aparece no sino do FiscoProjects

#### Scenario: Canais de quem nunca escolheu
- **WHEN** um chamado é atribuído, com os avisos pelo Teams ligados, a uma pessoa que nunca escolheu os canais
- **THEN** a pessoa recebe o e-mail, e nada vai para o Teams

#### Scenario: Os dois canais desligados
- **WHEN** um chamado é atribuído a uma pessoa com o e-mail e o Teams desligados
- **THEN** nada vai por e-mail nem pelo Teams, e a notificação aparece no sino

#### Scenario: Teams ligado sem o Teams disponível
- **WHEN** um chamado é atribuído, com o envio pelo Graph, a uma pessoa que entra sem o Entra ID e tem só o Teams ligado
- **THEN** nada vai por e-mail nem pelo Teams, e a notificação aparece no sino

#### Scenario: Teams sem volta ao e-mail
- **WHEN** o Graph recusa de vez o aviso para uma pessoa com o Teams ligado e o e-mail desligado
- **THEN** o aviso do Teams fica como Falhou, e a pessoa não recebe o e-mail

## REMOVED Requirements

### Requirement: Volta ao e-mail

**Reason**: Com o e-mail e o Teams ligados e desligados por cada pessoa, quem deixou o e-mail ligado já recebe o e-mail, e quem o desligou escolheu não recebê-lo; o aviso continua no sino (ADR-0151).

**Migration**: Quem escolheu só o Teams e quer o e-mail quando o Teams falha deixa os dois ligados em Meu perfil.
