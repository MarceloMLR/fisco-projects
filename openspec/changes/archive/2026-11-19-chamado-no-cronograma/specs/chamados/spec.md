## ADDED Requirements

### Requirement: Chamado no cronograma

Quem atende o chamado (o administrador, o gerente, o líder técnico e quem atende o time do chamado) MUST poder ligá-lo a até 10 tarefas, também a tarefa-resumo e a subtarefa, ou marcos do cronograma do projeto dele, trocando a lista inteira de uma vez, inclusive por nenhuma. A ligação MUST não mudar as datas do cronograma nem os prazos do chamado (ADR-0025, ADR-0044, ADR-0051, ADR-0164).

#### Scenario: Ligar ao cronograma
- **WHEN** o desenvolvedor responsável pelo chamado #104, do time Desenvolvimento, o liga à tarefa Integrar o emissor de notas
- **THEN** o chamado mostra a tarefa em "No cronograma", e as datas do cronograma e os prazos do chamado não mudam

#### Scenario: Gestão liga ao cronograma
- **WHEN** a gerente, e depois o administrador, ligam um chamado do time Funcional a uma tarefa e a um marco
- **THEN** o chamado mostra os dois itens

#### Scenario: Desligar do cronograma
- **WHEN** quem atende troca os itens ligados por nenhum
- **THEN** o chamado não tem mais itens ligados

### Requirement: Quem liga o chamado ao cronograma

Quem não atende o chamado (o Leitor, o solicitante que não atende o time e quem atende o outro time) MUST receber 403 ao ligar; o chamado fechado MUST receber 409; e quem está fora do projeto, 404 (ADR-0024, ADR-0044, ADR-0164).

#### Scenario: Quem não atende não liga
- **WHEN** o Leitor do projeto, ou uma colaboradora num chamado do time Desenvolvimento, tenta ligar o chamado ao cronograma
- **THEN** a API responde 403, e nada é gravado

#### Scenario: Chamado fechado não liga
- **WHEN** quem atende tenta ligar um chamado fechado
- **THEN** a API responde 409, e nada é gravado

#### Scenario: Fora do projeto não liga
- **WHEN** alguém que não está no projeto tenta ligar o chamado
- **THEN** a API responde 404

### Requirement: Itens que podem ser ligados

Só tarefas e marcos do cronograma do projeto do chamado, até 10 e sem repetir, MUST poder ser ligados; uma fase, um item de outro projeto, um item que não existe ou mais de 10 itens MUST receber 400, sem gravar nada (ADR-0088, ADR-0164).

#### Scenario: Item que não pode ser ligado
- **WHEN** quem atende tenta ligar uma fase, uma tarefa de outro projeto ou 11 itens
- **THEN** a API responde 400, e nada é gravado

### Requirement: O que depende do chamado

Para um chamado com itens ligados, a API MUST trazer os itens que ainda existem, com o tipo, as datas, o progresso e o atraso; os marcos ainda não atingidos que vêm depois deles pelas dependências, de qualquer tipo, com o dia de cada um; e se algum item ligado, ou uma subtarefa da tarefa-resumo ligada, está no caminho crítico. Tudo MUST ser calculado com as datas atuais do cronograma, a cada leitura (ADR-0051, ADR-0088, ADR-0137, ADR-0164).

#### Scenario: Marcos ameaçados pelas dependências
- **WHEN** o chamado está ligado a Testes integrados, o marco Homologação concluída depende de Testes integrados e o marco Go-live depende de Homologação concluída
- **THEN** o chamado mostra os dois marcos como ameaçados, cada um com o dia

#### Scenario: Marco atingido não é ameaçado
- **WHEN** um dos marcos que vêm depois do item ligado já foi atingido
- **THEN** o chamado não o mostra entre os ameaçados

#### Scenario: Item no caminho crítico
- **WHEN** o item ligado ao chamado está no caminho crítico
- **THEN** o chamado avisa que um atraso nele atrasa o fim do projeto

#### Scenario: Tarefa-resumo ligada
- **WHEN** o chamado está ligado a uma tarefa-resumo cuja subtarefa leva ao marco Go-live
- **THEN** o chamado mostra Go-live como ameaçado

#### Scenario: Item ligado excluído
- **WHEN** a gerente exclui a tarefa ligada ao chamado e depois desfaz a exclusão
- **THEN** o chamado deixa de mostrar a tarefa enquanto ela não existe e volta a mostrá-la depois do desfazer

### Requirement: Ligação na conversa e na auditoria

Ligar e desligar o chamado do cronograma MUST entrar na conversa como evento, com quem fez e os itens ligados e desligados, e na auditoria, com o chamado e os itens pelo nome atual, no contexto do projeto. As ligações MUST sair com o projeto excluído (ADR-0039, ADR-0069, ADR-0084, ADR-0111, ADR-0164).

#### Scenario: Ligação na conversa
- **WHEN** quem atende liga o chamado a Testes integrados
- **THEN** a conversa mostra o evento com quem ligou e o item, também no filtro "Histórico"

#### Scenario: Ligação na auditoria pelo nome atual
- **WHEN** quem atende liga o chamado a uma tarefa e a gerente depois renomeia a tarefa
- **THEN** a auditoria mostra a ligação com o chamado e a tarefa pelo nome novo

#### Scenario: Projeto excluído com ligações
- **WHEN** o administrador exclui um projeto com chamados ligados ao cronograma
- **THEN** as ligações saem com ele

### Requirement: Seção No cronograma

O painel do chamado MUST ter a seção "No cronograma", depois de "No fornecedor", com os itens ligados (o tipo, o nome, as datas e o atraso, por ícone, cor e texto, levando ao detalhe no cronograma), "Ameaça", com os marcos e os dias, e o aviso do caminho crítico. Sem itens, a seção MUST dizer "Sem ligação com o cronograma" (ADR-0065, ADR-0164).

#### Scenario: Seção para quem não atende
- **WHEN** o Leitor abre um chamado ligado ao cronograma, e depois um sem ligação
- **THEN** o primeiro mostra os itens e os marcos sem "Alterar", e o segundo, "Sem ligação com o cronograma", sem "Ligar ao cronograma"

### Requirement: Ligar pela tela

Para quem atende, a seção "No cronograma" MUST ter "Ligar ao cronograma", sem itens, ou "Alterar", com itens, que abrem a escolha das tarefas e dos marcos na ordem do cronograma, com a fase de cada um, até 10 (ADR-0164).

#### Scenario: Ligar pela tela
- **WHEN** quem atende escolhe "Ligar ao cronograma", marca Testes integrados e confirma
- **THEN** a seção mostra a tarefa, os marcos ameaçados e "Alterar", e a conversa mostra o evento
