# avisos Specification

## Purpose

Avisos do sistema: a fila de e-mails no banco, enviada em segundo plano com novas tentativas e uma implementação local fora da produção, a retenção do conteúdo dos e-mails, o teste de envio pela Administração, as regras de quem recebe os avisos, os e-mails na hora do que pede ação, os alertas de prazo dos chamados, o resumo diário com o restante e o sino de notificações, com a guarda configurável (ADR-0004, ADR-0036, ADR-0037, ADR-0038, ADR-0058).

## Requirements

### Requirement: Fila de e-mails

Todo e-mail MUST ser gravado numa fila no banco e enviado em segundo plano, de modo que a ação que o gera não dependa do envio e não falhe quando o envio está indisponível (ADR-0036).

#### Scenario: E-mail na fila com o envio fora do ar
- **WHEN** o administrador pede o teste de envio e o envio está indisponível
- **THEN** a API responde 202, e o e-mail fica na fila, Na fila

### Requirement: Envio em segundo plano

Um processo da aplicação MUST enviar os e-mails da fila, marcando cada um como Enviado. Uma falha MUST ser tentada de novo depois de 1 min, 5 min, 15 min, 1 h e 4 h, e a sexta falha MUST deixar o e-mail como Falhou, com o erro registrado. Um e-mail MUST ser enviado por um processo só de cada vez (ADR-0036).

#### Scenario: Envio e marcação
- **WHEN** o processo encontra um e-mail na fila e o envio funciona
- **THEN** o e-mail sai e fica Enviado, com a hora do envio

#### Scenario: Falha e nova tentativa
- **WHEN** o envio de um e-mail falha
- **THEN** o e-mail continua na fila, com a próxima tentativa 1 minuto depois e o erro registrado

#### Scenario: Falha definitiva
- **WHEN** o envio de um e-mail falha pela sexta vez
- **THEN** o e-mail fica Falhou e não é tentado de novo

### Requirement: Implementação local

Fora da produção, os e-mails MUST ficar gravados como arquivos `.eml`, sem sair para ninguém, e a aplicação MUST não subir em produção com a implementação local (ADR-0004, ADR-0036).

#### Scenario: E-mail gravado sem envio
- **WHEN** o processo envia um e-mail com a implementação local
- **THEN** um arquivo `.eml` com o destinatário, o assunto e os textos fica na pasta configurada

#### Scenario: Implementação local em produção
- **WHEN** a aplicação sobe em produção com `Email:Provider` = `Local`
- **THEN** a aplicação não sobe e diz por quê

### Requirement: Retenção do conteúdo dos e-mails

O conteúdo de um e-mail enviado ou que falhou há mais de `Email:KeepDays` dias (30) MUST ser apagado da fila, ficando só a data e o tipo (ADR-0058).

#### Scenario: Conteúdo apagado depois de 30 dias
- **WHEN** passam 31 dias do envio de um e-mail
- **THEN** a fila guarda só a data e o tipo dele, sem destinatário, assunto nem textos

### Requirement: Teste de envio

O administrador MUST ver nas Configurações gerais a caixa de envio e o resultado do último teste, e MUST poder enviar um e-mail de teste para o próprio endereço. As outras pessoas MUST receber 403 (ADR-0036, ADR-0046).

#### Scenario: Enviar teste pela tela
- **WHEN** o administrador escolhe "Enviar teste" na seção Microsoft 365
- **THEN** a tela avisa que o e-mail está na fila para o endereço dele, e o resultado do teste aparece depois do envio

#### Scenario: Teste só do administrador
- **WHEN** um gerente de projeto pede o teste de envio
- **THEN** a API responde 403

### Requirement: Quem recebe os avisos

Os módulos MUST avisar pelo contrato dos avisos, sem conhecer os canais (ADR-0038). Um aviso MUST NOT chegar a quem fez a ação, a quem está desativado nem a quem não vê mais o projeto do item. Cada pessoa MUST receber no máximo um aviso por ação, e todo e-mail MUST levar ao item no FiscoProjects (ADR-0037).

#### Scenario: Quem fez a ação não recebe
- **WHEN** o solicitante manda uma mensagem num chamado que ele acompanha
- **THEN** o responsável recebe o e-mail da mensagem, e o solicitante não

#### Scenario: Pessoa desativada não recebe
- **WHEN** um chamado é aberto para um time em que uma das pessoas está desativada
- **THEN** só as pessoas ativas do time recebem o e-mail

#### Scenario: Quem saiu do projeto não recebe
- **WHEN** muda o status de um chamado acompanhado por alguém que saiu do projeto
- **THEN** essa pessoa não recebe o aviso

#### Scenario: Link para o item
- **WHEN** um aviso de chamado sai por e-mail
- **THEN** o botão do e-mail leva à página do chamado

### Requirement: Avisos dos chamados na hora

Os eventos que pedem ação MUST gerar e-mail na hora (ADR-0037): o chamado aberto ou transferido para quem atende o time dele; a mensagem para quem acompanha o chamado; a menção para quem foi mencionado; o pedido de informação e a resolução para o solicitante; a troca de responsável para o novo responsável; e a troca de solicitante para o novo solicitante. Quem recebe um aviso mais específico da mesma ação (a menção, o pedido de informação, a resolução) MUST NOT receber também o da mensagem.

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

### Requirement: Alertas de prazo dos chamados

A cada hora, um processo da aplicação MUST avisar na hora quando um prazo de um chamado em aberto (a primeira resposta ou a resolução) passa a vencer, hoje ou no próximo dia útil, e quando vence. O aviso MUST ir para o responsável e, sem responsável, para quem atende o time. Cada prazo MUST avisar uma vez por estado; se o prazo mudar, como numa nova prioridade, os alertas recomeçam. Prazo pausado ou cumprido MUST NOT avisar.

#### Scenario: Prazo vencendo
- **WHEN** a resolução de um chamado com responsável passa a vencer no próximo dia útil
- **THEN** o responsável recebe o e-mail do prazo vencendo, uma vez só

#### Scenario: Prazo vencido sem responsável
- **WHEN** a primeira resposta de um chamado sem responsável vence
- **THEN** quem atende o time do chamado recebe o e-mail do prazo vencido

#### Scenario: Prazo pausado não avisa
- **WHEN** um chamado aguarda o solicitante
- **THEN** a resolução dele não gera alerta

### Requirement: Resumo dos chamados

As mudanças de status (inclusive o fechamento automático e o fechamento pelo cancelamento do projeto) e os registros e atualizações em fornecedores MUST entrar no resumo diário de quem acompanha o chamado, salvo de quem recebeu um aviso na hora da mesma ação (ADR-0037).

#### Scenario: Mudança de status no resumo
- **WHEN** quem atende muda o status, sem mensagem, de um chamado que o gerente acompanha
- **THEN** a mudança entra no próximo resumo diário do gerente, sem e-mail na hora

#### Scenario: Fornecedor no resumo
- **WHEN** quem atende atualiza o status de um chamado no fornecedor
- **THEN** a atualização entra no próximo resumo diário de quem acompanha o chamado

### Requirement: Resumo dos projetos

A inclusão num projeto e a troca de papel MUST entrar no resumo diário da pessoa; a mudança de status do projeto, no resumo das pessoas do projeto; e as datas alteradas no cronograma, inclusive as empurradas em cadeia, no resumo do responsável de cada tarefa, com um item só por tarefa e as datas mais recentes (ADR-0037).

#### Scenario: Inclusão no projeto
- **WHEN** o gerente inclui uma pessoa no projeto
- **THEN** a inclusão, com o papel, entra no próximo resumo diário dela

#### Scenario: Status do projeto
- **WHEN** o gerente pausa o projeto
- **THEN** a mudança entra no próximo resumo diário das outras pessoas do projeto

#### Scenario: Datas empurradas em cadeia
- **WHEN** o gerente adia uma tarefa e isso empurra a tarefa seguinte, de outra pessoa, duas vezes no mesmo dia
- **THEN** o resumo dessa pessoa traz a tarefa uma vez, com as datas novas

### Requirement: Resumo diário

No horário da instalação, um processo da aplicação MUST mandar a cada pessoa ativa que não desligou o resumo um e-mail só com as novidades desde o último resumo e as tarefas dela, ainda não concluídas, que vencem de hoje até o segundo dia útil seguinte. Resumo sem novidades MUST NOT ser enviado. O resumo MUST sair uma vez por dia, mesmo com a aplicação reiniciada ou em mais de uma instância; com a aplicação parada no horário, MUST sair quando ela voltar, no mesmo dia (ADR-0037).

#### Scenario: Resumo no horário
- **WHEN** chega o horário do resumo e a pessoa tem novidades
- **THEN** ela recebe um e-mail só, com as novidades agrupadas e o link de cada item

#### Scenario: Tarefas que vencem
- **WHEN** chega o horário do resumo e a pessoa tem uma tarefa que vence amanhã
- **THEN** o resumo traz a tarefa com a data de término

#### Scenario: Sem novidades
- **WHEN** chega o horário do resumo e a pessoa não tem novidades nem tarefas vencendo
- **THEN** nenhum e-mail sai para ela

#### Scenario: Resumo desligado
- **WHEN** a pessoa desligou o resumo diário
- **THEN** ela não recebe o resumo, e as novidades dela não se acumulam para depois

#### Scenario: Um resumo por dia
- **WHEN** a aplicação reinicia depois do horário do resumo
- **THEN** o resumo do dia não sai de novo

### Requirement: Notificações no sino

Cada aviso MUST virar, na hora, uma notificação no sino para cada pessoa que o recebe, pelas mesmas regras de quem recebe, inclusive os avisos que por e-mail vão para o resumo diário (ADR-0038). A notificação MUST dizer o que aconteceu, quem fez (pelo nome atual), quando, o projeto, e levar ao item. Cada pessoa MUST ver só as próprias notificações.

#### Scenario: Notificação do mesmo aviso do e-mail
- **WHEN** uma mensagem num chamado avisa o solicitante por e-mail
- **THEN** o solicitante também tem a notificação no sino, com o link para o chamado

#### Scenario: Notificação do resumo na hora
- **WHEN** muda o status de um chamado que a pessoa acompanha
- **THEN** a notificação aparece no sino na hora, mesmo que o e-mail fique para o resumo

#### Scenario: Só as próprias notificações
- **WHEN** uma pessoa pede para marcar como lida a notificação de outra
- **THEN** a API responde 404 e nada muda

### Requirement: Sino e painel

O sino MUST ficar no canto superior direito, com o número de notificações não lidas em vermelho, e no celular na barra do topo (ADR-0066). O número MUST ser atualizado ao navegar e a cada minuto, sem tempo real. O painel MUST mostrar as notificações agrupadas por dia, as não lidas destacadas, com "Não lidas" e "Todas", "Marcar todas como lidas" e "Ver todas"; escolher uma notificação MUST marcá-la como lida e abrir o item (ADR-0038).

#### Scenario: Contador de não lidas
- **WHEN** a pessoa tem 3 notificações não lidas
- **THEN** o sino mostra 3 e se anuncia como "Notificações, 3 não lidas"

#### Scenario: Abrir uma notificação
- **WHEN** a pessoa escolhe uma notificação não lida no painel
- **THEN** ela fica lida, o contador diminui e a página do item abre

#### Scenario: Marcar todas como lidas
- **WHEN** a pessoa escolhe "Marcar todas como lidas"
- **THEN** o contador some e as notificações deixam de estar destacadas

#### Scenario: Atualização a cada minuto
- **WHEN** chega uma notificação com a tela aberta
- **THEN** o contador mostra a nova notificação em até um minuto, sem recarregar a página

### Requirement: Guarda das notificações

As notificações mais antigas que o prazo da instalação MUST ser apagadas, lidas ou não; o prazo MUST ser escolhido pelo administrador em "Guardar notificações por", nas Regras gerais das Configurações gerais, de 1 a 365 dias, com o padrão de 90 dias (ADR-0038).

#### Scenario: Notificação antiga apagada
- **WHEN** uma notificação passa de 90 dias
- **THEN** ela some do sino

#### Scenario: Trocar a guarda
- **WHEN** o administrador escolhe 30 dias e salva
- **THEN** as notificações com mais de 30 dias são apagadas na rodada seguinte

### Requirement: Avisos dos termos de aceite

O pedido de aceite e o lembrete MUST gerar e-mail na hora para a pessoa do pedido (ADR-0037). O termo assinado, inclusive o anexado pela gestão, e a recusa MUST avisar quem pediu e o gerente do projeto pelo sino e pelo resumo diário. O resumo diário MUST trazer os pedidos de aceite pendentes da pessoa nos projetos em aberto, com o projeto e o dia do pedido. Nenhum aviso MUST levar o termo nem o motivo da recusa por e-mail; o aviso leva ao termo no FiscoProjects (ADR-0035, ADR-0037, ADR-0038).

#### Scenario: Pedido de aceite por e-mail
- **WHEN** a gerente pede o aceite a Carla
- **THEN** Carla recebe o e-mail na hora, com o botão que abre o termo

#### Scenario: Lembrete do pedido
- **WHEN** a gerente lembra Carla de um pedido pendente
- **THEN** Carla recebe o e-mail do pedido de novo, na hora

#### Scenario: Termo assinado avisa a gestão
- **WHEN** Carla envia o termo assinado
- **THEN** quem pediu e o gerente têm a notificação no sino, e o aviso entra no próximo resumo diário deles

#### Scenario: Recusa avisa a gestão
- **WHEN** Carla recusa o pedido
- **THEN** quem pediu e o gerente têm a notificação da recusa no sino e no resumo

#### Scenario: Aceites pendentes no resumo
- **WHEN** chega o horário do resumo e Carla tem um pedido de aceite pendente
- **THEN** o resumo dela traz o pedido, com o projeto e o dia do pedido

### Requirement: Envio pelo Microsoft Graph

Com `Email:Provider` = `Graph`, os e-mails da fila MUST sair pelo `sendMail` do Microsoft Graph a partir da caixa compartilhada de `Email:Mailbox`, com o token do aplicativo, o corpo em HTML e o envio guardado nos itens enviados da caixa. Um envio recusado ou limitado pelo Graph MUST ser falha da tentativa, com o código do Graph no erro e sem o destinatário (ADR-0036, ADR-0123).

#### Scenario: E-mail pela caixa compartilhada
- **WHEN** o administrador pede o teste de envio com o envio pelo Graph
- **THEN** o Graph recebe o e-mail da caixa de envio para o endereço do administrador, com o assunto e o HTML, e o teste fica Enviado

#### Scenario: Caixa sem permissão de envio
- **WHEN** o Graph recusa o envio pela caixa com 403
- **THEN** o e-mail continua na fila para a próxima tentativa, com o erro do Graph sem o endereço do destinatário

#### Scenario: Graph fora do ar
- **WHEN** o Graph responde 503 a um envio
- **THEN** o e-mail continua na fila para a próxima tentativa

### Requirement: Configuração do envio conferida

A aplicação MUST não subir com o envio pelo Graph sem uma caixa de envio válida, nem com domínios locais em produção (ADR-0036, ADR-0123).

#### Scenario: Graph sem caixa de envio
- **WHEN** a aplicação sobe com `Email:Provider` = `Graph` e sem `Email:Mailbox`
- **THEN** a aplicação não sobe e diz que falta a caixa de envio

#### Scenario: Domínios locais em produção
- **WHEN** a aplicação sobe em produção com `Email:Graph:LocalDomains`
- **THEN** a aplicação não sobe e diz por quê

### Requirement: E-mails da demonstração na pasta local

Fora da produção, com o envio pelo Graph, os e-mails para os domínios de `Email:Graph:LocalDomains` MUST ficar como `.eml` na pasta local, sem passar pelo Graph, e os outros MUST sair pelo Graph (ADR-0123).

#### Scenario: Endereço da demonstração na pasta
- **WHEN** a fila envia um e-mail para `marina.costa@empresa.example` com `empresa.example` nos domínios locais
- **THEN** o e-mail fica na pasta local, e o Graph não recebe nada

#### Scenario: Endereço do tenant pelo Graph
- **WHEN** a fila envia um e-mail para uma pessoa de outro domínio com domínios locais configurados
- **THEN** o e-mail sai pelo Graph
