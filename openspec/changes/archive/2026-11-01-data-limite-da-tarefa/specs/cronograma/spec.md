## ADDED Requirements

### Requirement: Data-limite de tarefas e marcos

Quem edita o cronograma MUST poder pôr, mudar e tirar a data-limite de uma tarefa ou de um marco, sem mudar as datas do item nem das dependências. O cronograma MUST dizer, em dias úteis da instalação, quanto cada item passou dela (pelo fim ou pelo dia do marco; concluído, pelo dia em que terminou; atrasado, por hoje) e quantos itens abertos passaram. Ela MUST entrar no desfazer, no XML do MS Project e no Excel (ADR-0018, ADR-0019, ADR-0020, ADR-0043, ADR-0091, ADR-0135).

#### Scenario: Data-limite cumprida
- **WHEN** a gerente põe a data-limite 30/10/2026 em Testes integrados, que termina em 29/10/2026
- **THEN** as datas do item e das tarefas que dependem dele não mudam, e ele não passou da data-limite

#### Scenario: Data-limite estourada
- **WHEN** a data-limite de Testes integrados é 30/10/2026 e uma mudança empurra o fim dele para 03/11/2026, com 02/11/2026 feriado
- **THEN** o item passou 1 dia útil da data-limite, e o cronograma conta 1 data-limite estourada

#### Scenario: Data-limite do marco
- **WHEN** um marco do dia 04/11/2026, com a data-limite 04/11/2026, ainda não foi atingido em 05/11/2026
- **THEN** ele passou 1 dia útil da data-limite e conta como estourado; atingido em 05/11/2026, continua 1 dia útil depois, sem contar como estourado

#### Scenario: Data-limite só de quem edita
- **WHEN** um colaborador, um desenvolvedor, o líder técnico ou um leitor do projeto manda a data-limite de um item
- **THEN** a API responde 403, e para o administrador e o gerente a data-limite é gravada

#### Scenario: Desfazer a data-limite
- **WHEN** a gerente põe a data-limite num item e escolhe "Desfazer"
- **THEN** o item volta sem data-limite

#### Scenario: Data-limite no MS Project
- **WHEN** a gerente exporta o XML de um cronograma com uma data-limite e cria um projeto com esse arquivo, com o início 5 dias úteis depois
- **THEN** o XML traz o `Deadline` da tarefa, e o projeto novo tem a data-limite 5 dias úteis depois da original

#### Scenario: Data-limite no Excel
- **WHEN** uma pessoa do projeto exporta para o Excel um cronograma com um item que passou 1 dia útil da data-limite
- **THEN** a planilha traz a data-limite do item e o 1 dia útil na coluna do estouro

### Requirement: Data-limite pela tela

O detalhe da tarefa e do marco MUST ter o campo "Data-limite" para quem edita e dizer a quem vê quanto falta ou quanto passou, com ícone, cor e texto. A Linha do tempo e o PDF MUST marcar o dia da data-limite na linha do item, em vermelho quando passou, com a legenda. A Lista e o Expandir MUST ter a coluna "Data-limite", e o resumo da aba MUST contar as datas-limite estouradas (ADR-0065, ADR-0071, ADR-0134).

#### Scenario: Data-limite no detalhe
- **WHEN** a gerente põe a data-limite 30/10/2026 em Testes integrados pelo detalhe e, depois, o fim do item passa para 03/11/2026
- **THEN** a API recebe a data-limite, e o detalhe diz "Passou 1 dia útil da data-limite" em vermelho, com o ícone

#### Scenario: Marcador da data-limite
- **WHEN** a Linha do tempo mostra um item com a data-limite no período
- **THEN** a linha dele tem o marcador no dia da data-limite, vermelho se passou, e a legenda mostra "Data-limite"

#### Scenario: Data-limite na Lista e no resumo
- **WHEN** a Lista mostra um item que passou da data-limite
- **THEN** a coluna "Data-limite" mostra a data com o estouro destacado, e o resumo da aba mostra "1 data-limite estourada"
