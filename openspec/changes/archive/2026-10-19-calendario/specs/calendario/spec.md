# Spec Delta

## Purpose

Define o calendário único da instalação, que diz quais dias são úteis para o cronograma e os prazos: os dias de trabalho, os feriados nacionais e pontos facultativos calculados, os feriados locais cadastrados e a tela Calendário e feriados, só do administrador (ADR-0018, ADR-0025, ADR-0046, ADR-0083).

## ADDED Requirements

### Requirement: Calendário só do administrador

Só o administrador MUST poder ler e alterar o calendário da instalação pela API e abrir a seção Calendário e feriados da Administração. O gerente de projeto e o usuário MUST receber a recusa (403), e quem não tem sessão MUST receber 401, sem nada mudar (ADR-0046).

#### Scenario: Administrador altera o calendário
- **WHEN** o administrador lê o calendário de um ano, muda os dias de trabalho, desconsidera um feriado nacional, marca todos e inclui, altera e exclui um feriado local
- **THEN** todas as ações são aceitas

#### Scenario: Gerente de projeto não altera o calendário
- **WHEN** um gerente de projeto pede para ler ou alterar o calendário
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não altera o calendário
- **WHEN** um usuário pede para ler ou alterar o calendário
- **THEN** a API responde 403 e nada muda

#### Scenario: Calendário sem sessão
- **WHEN** alguém sem sessão pede para ler ou alterar o calendário
- **THEN** a API responde 401 e nada muda

### Requirement: Dias de trabalho

A instalação MUST ter os dias de trabalho da semana, de segunda a sexta enquanto ninguém os troca. O administrador MUST poder marcar e desmarcar qualquer dia da semana, mas MUST ficar pelo menos um dia de trabalho marcado (ADR-0018).

#### Scenario: Dias de trabalho padrão
- **WHEN** ninguém trocou os dias de trabalho
- **THEN** segunda, terça, quarta, quinta e sexta são dias de trabalho, e sábado e domingo não

#### Scenario: Trocar os dias de trabalho
- **WHEN** o administrador marca o sábado
- **THEN** o sábado passa a ser dia de trabalho

#### Scenario: Semana sem dia de trabalho
- **WHEN** alguém pede para deixar a semana sem nenhum dia de trabalho
- **THEN** a API recusa com 400, e na tela o último dia marcado não pode ser desmarcado

### Requirement: Feriados nacionais calculados

O sistema MUST calcular os feriados nacionais de qualquer ano, sem cadastro, pela Páscoa do calendário gregoriano: Confraternização Universal, Paixão de Cristo, Tiradentes, Dia Mundial do Trabalho, Independência do Brasil, Nossa Senhora Aparecida, Finados, Proclamação da República, Natal e, a partir de 2024, o Dia Nacional de Zumbi e da Consciência Negra. Eles MUST começar considerados (ADR-0018, ADR-0083).

#### Scenario: Feriados nacionais de 2026
- **WHEN** o administrador abre o calendário de 2026
- **THEN** a lista tem os dez feriados nacionais, com a Paixão de Cristo em 03/04/2026 e todos considerados

#### Scenario: Feriados móveis pela Páscoa
- **WHEN** o calendário de 2024, 2025, 2026 e 2027 é calculado
- **THEN** a Paixão de Cristo cai em 29/03/2024, 18/04/2025, 03/04/2026 e 26/03/2027

#### Scenario: Consciência Negra a partir de 2024
- **WHEN** o calendário de 2023 e o de 2024 são calculados
- **THEN** o 20/11 é feriado nacional em 2024 e não em 2023

### Requirement: Pontos facultativos calculados

O sistema MUST calcular os pontos facultativos que a portaria federal repete todo ano: segunda e terça-feira de Carnaval, Quarta-feira de Cinzas, Corpus Christi, Dia do Servidor Público (28/10), véspera de Natal e véspera de Ano-Novo. Eles MUST começar desconsiderados, como dias úteis. Um ponto facultativo considerado MUST deixar de ser dia útil inteiro, mesmo quando a portaria fala em meio período (ADR-0083).

#### Scenario: Pontos facultativos de 2026
- **WHEN** o administrador abre o calendário de 2026
- **THEN** a lista tem os sete pontos facultativos, com o Carnaval em 16/02 e 17/02, a Quarta-feira de Cinzas em 18/02 e o Corpus Christi em 04/06

#### Scenario: Ponto facultativo desconsiderado por padrão
- **WHEN** ninguém escolheu considerar a Quarta-feira de Cinzas
- **THEN** 18/02/2026 é dia útil

#### Scenario: Ponto facultativo considerado
- **WHEN** o administrador considera a Quarta-feira de Cinzas
- **THEN** 18/02/2026 deixa de ser dia útil, o dia inteiro

### Requirement: Escolha dos feriados calculados

O administrador MUST poder considerar ou desconsiderar cada feriado nacional e cada ponto facultativo calculado. A escolha MUST valer em todos os anos. Um feriado desconsiderado MUST continuar na lista, como desconsiderado (ADR-0018, ADR-0083).

#### Scenario: Desconsiderar um feriado nacional
- **WHEN** o administrador desconsidera Tiradentes
- **THEN** 21/04 passa a ser dia útil, e a lista mostra Tiradentes desconsiderado

#### Scenario: Escolha em todos os anos
- **WHEN** o administrador considera a terça-feira de Carnaval no calendário de 2026
- **THEN** a terça-feira de Carnaval também aparece considerada em 2027, em 09/02/2027

#### Scenario: Feriado calculado desconhecido
- **WHEN** alguém pede para considerar um feriado que o sistema não calcula
- **THEN** a API responde 404 e nada muda

### Requirement: Marcar todos

O administrador MUST poder considerar de uma vez todas as datas da lista mostrada, no ano e no tipo escolhidos, para depois desmarcar as que não quer. Com todas as datas da lista já consideradas, o mesmo botão MUST desconsiderar todas. A mudança MUST ser gravada de uma vez: todas as datas ou nenhuma.

#### Scenario: Marcar todos
- **WHEN** o administrador escolhe "Marcar todos" na lista de 2026
- **THEN** todas as datas da lista passam a consideradas, e ele pode desmarcar uma a uma as que não quer

#### Scenario: Marcar todos de um tipo
- **WHEN** o filtro está em Pontos facultativos e o administrador escolhe "Marcar todos"
- **THEN** só os pontos facultativos passam a considerados

#### Scenario: Desmarcar todos
- **WHEN** todas as datas da lista já estão consideradas
- **THEN** o botão diz "Desmarcar todos" e desconsidera todas as datas da lista

#### Scenario: Marcar todos com um feriado que não existe
- **WHEN** o pedido traz um feriado calculado ou local que não existe
- **THEN** a API responde 404 e nada muda

### Requirement: Feriados locais

O administrador MUST poder incluir feriados locais (da cidade, do estado ou da empresa), com nome de até 80 caracteres sem os espaços das pontas, data e a opção "Repete todo ano". O feriado local MUST começar considerado. Nome vazio ou longo e data ausente MUST ser recusados, com o motivo em cada campo (ADR-0018, ADR-0083).

#### Scenario: Incluir feriado local
- **WHEN** o administrador inclui "Recesso da empresa" em 30/12/2026, sem repetir
- **THEN** o feriado aparece considerado no calendário de 2026, e 30/12/2026 deixa de ser dia útil

#### Scenario: Feriado local inválido
- **WHEN** o administrador tenta incluir um feriado sem nome, com nome de mais de 80 caracteres ou sem data
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada é incluído

### Requirement: Repetição dos feriados locais

Um feriado local que repete todo ano MUST valer no mesmo dia e mês em todos os anos a partir do ano da data, e um feriado em 29/02 só nos anos bissextos. Um feriado que não repete MUST valer só na data (ADR-0083).

#### Scenario: Feriado que repete todo ano
- **WHEN** o administrador inclui "Aniversário da cidade" em 25/01/2026, repetindo todo ano
- **THEN** o feriado aparece em 25/01/2027 e em 25/01/2030, e não aparece em 25/01/2025

#### Scenario: Feriado de um ano só
- **WHEN** o administrador inclui "Emenda de Tiradentes" em 20/04/2026, sem repetir
- **THEN** o feriado aparece em 2026 e não aparece em 2027

#### Scenario: Feriado em 29 de fevereiro
- **WHEN** um feriado em 29/02/2028 repete todo ano
- **THEN** ele aparece em 2032 e não aparece em 2029

### Requirement: Alterar e excluir feriados locais

O administrador MUST poder alterar o nome, a data e a repetição de um feriado local, considerá-lo ou desconsiderá-lo e excluí-lo. A exclusão MUST pedir confirmação na tela. Um feriado local que não existe MUST ser respondido com 404.

#### Scenario: Alterar feriado local
- **WHEN** o administrador troca a data de "Emenda de Tiradentes" para 22/04/2026
- **THEN** o feriado passa a valer em 22/04/2026 e não em 20/04/2026

#### Scenario: Desconsiderar feriado local
- **WHEN** o administrador desconsidera "Data magna do estado"
- **THEN** o feriado continua na lista, desconsiderado, e o dia volta a ser útil

#### Scenario: Excluir feriado local
- **WHEN** o administrador exclui "Emenda de Tiradentes" e confirma
- **THEN** o feriado sai do calendário e 20/04/2026 volta a ser dia útil

### Requirement: Dias úteis

Um dia MUST ser útil quando é dia de trabalho e não é feriado nacional, ponto facultativo ou feriado local considerado. O calendário MUST dizer se um dia é útil, somar dias úteis a uma data, pulando os que não são, e contar os dias úteis de um período, para o cronograma e os prazos dos chamados (ADR-0018, ADR-0025).

#### Scenario: Somar dias úteis
- **WHEN** o calendário soma 1 dia útil a sexta-feira, 09/10/2026, com o feriado de 12/10 considerado
- **THEN** o resultado é terça-feira, 13/10/2026

#### Scenario: Feriado desconsiderado não afeta as datas
- **WHEN** o administrador desconsidera Nossa Senhora Aparecida e o calendário soma 1 dia útil a 09/10/2026
- **THEN** o resultado é segunda-feira, 12/10/2026

#### Scenario: Dias de trabalho mudam o cálculo
- **WHEN** o sábado é dia de trabalho e o calendário soma 1 dia útil a sexta-feira, 16/10/2026
- **THEN** o resultado é sábado, 17/10/2026

#### Scenario: Contar dias úteis
- **WHEN** o calendário conta os dias úteis de 01/11/2026 a 30/11/2026, com os feriados nacionais considerados
- **THEN** o resultado é 19

### Requirement: Tela Calendário e feriados

A seção Instalação da coluna da Administração MUST ter Calendário e feriados, com "Dias de trabalho" no topo e a lista das datas de um ano, que começa no ano atual. Cada data mostra o dia da semana, o nome, o tipo, a repetição e "Considerado". Marcar um dia de trabalho ou "Considerado" MUST gravar na hora (canvas, 19/10/2026).

#### Scenario: Calendário e feriados na Administração
- **WHEN** o administrador escolhe Calendário e feriados na coluna da Administração
- **THEN** a tela mostra os dias de trabalho marcados e as datas do ano atual em ordem, com o total de datas e de consideradas no rodapé

#### Scenario: Trocar o ano
- **WHEN** o administrador escolhe o próximo ano
- **THEN** a lista mostra as datas desse ano, com os feriados móveis recalculados

#### Scenario: Filtrar por tipo
- **WHEN** o administrador escolhe Pontos facultativos
- **THEN** a lista mostra só os pontos facultativos do ano

#### Scenario: Marcar considerado na lista
- **WHEN** o administrador marca "Considerado" na segunda-feira de Carnaval
- **THEN** a escolha é gravada sem outro passo, e a tela avisa que o calendário foi salvo

#### Scenario: Novo feriado pela tela
- **WHEN** o administrador escolhe "Novo feriado", preenche nome e data e escolhe "Incluir feriado"
- **THEN** o diálogo fecha e o feriado aparece na lista do ano da data

#### Scenario: Excluir com confirmação
- **WHEN** o administrador escolhe Excluir no menu de um feriado local
- **THEN** a tela pede confirmação, dizendo que não dá para desfazer, e só exclui depois de "Excluir feriado"

#### Scenario: Calendário no celular
- **WHEN** o administrador abre Calendário e feriados no celular
- **THEN** os dias de trabalho ficam em duas linhas, o tipo é escolhido numa lista, as datas aparecem em lista com o nome, a data, o tipo e "Considerado", com "Marcar todos" no topo da lista e "Novo feriado" embaixo

### Requirement: Mudanças do calendário na auditoria

Cada mudança no calendário MUST entrar na auditoria, com quem fez, quando e os campos antes e depois: "Alterou os dias de trabalho", "Considerou um feriado", "Desconsiderou um feriado", "Incluiu um feriado", "Alterou um feriado" e "Excluiu um feriado". A tela de Auditoria MUST ter os tipos Dias de trabalho, Feriados calculados e Feriados locais no filtro (ADR-0039).

#### Scenario: Dias de trabalho na auditoria
- **WHEN** o administrador marca o sábado como dia de trabalho
- **THEN** a auditoria ganha um registro "Alterou os dias de trabalho" com os dias antes e depois

#### Scenario: Escolha de feriado na auditoria
- **WHEN** o administrador considera a segunda-feira de Carnaval
- **THEN** a auditoria ganha um registro "Considerou um feriado" com o nome do feriado

#### Scenario: Feriado local na auditoria
- **WHEN** o administrador inclui, altera e exclui um feriado local
- **THEN** a auditoria ganha os registros "Incluiu um feriado", "Alterou um feriado" e "Excluiu um feriado", com o nome do feriado
