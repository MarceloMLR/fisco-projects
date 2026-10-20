## ADDED Requirements

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
