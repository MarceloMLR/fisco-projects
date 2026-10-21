# projetos Specification

## Purpose

Define os projetos da instalação: os dados e o status de cada projeto, quem cria, vê e altera, a criação com gerente e líder técnico, o projeto encerrado somente leitura, as telas Projetos, Novo projeto e a página do projeto, e a auditoria dessas mudanças (ADR-0015, ADR-0016, ADR-0040, ADR-0043, ADR-0072).

## Requirements

### Requirement: Quem cria e vê projetos

O administrador e o gerente de projeto (papel no sistema) MUST poder criar projetos; o usuário MUST receber 403. O administrador MUST ver todos os projetos, e as outras pessoas só os projetos em que estão; para elas, os outros projetos não existem (404). Quem não tem sessão MUST receber 401 (ADR-0040, ADR-0043).

#### Scenario: Administrador cria e vê projetos
- **WHEN** o administrador cria um projeto e lista os projetos
- **THEN** a criação é aceita e a lista mostra todos os projetos, inclusive aqueles em que ele não está

#### Scenario: Gerente de projeto cria um projeto
- **WHEN** um gerente de projeto cria um projeto
- **THEN** a criação é aceita e ele vê o projeto

#### Scenario: Usuário não cria projetos
- **WHEN** um usuário pede para criar um projeto
- **THEN** a API responde 403 e nada é criado

#### Scenario: Projeto de outras pessoas
- **WHEN** uma pessoa que não está num projeto lista os projetos ou pede o projeto
- **THEN** o projeto não aparece na lista e o pedido recebe 404

#### Scenario: Projetos sem sessão
- **WHEN** alguém sem sessão lista, cria, abre ou altera projetos
- **THEN** a API responde 401 e nada muda

### Requirement: Quem altera o projeto

O administrador, o gerente e o líder técnico do projeto MUST poder alterar o código, o nome, a descrição e o cliente. Só o administrador e o gerente do projeto MUST poder mudar o status, inclusive reabrir. O colaborador, o desenvolvedor e o leitor MUST receber 403 nas duas ações, e o líder técnico MUST receber 403 ao mudar o status (ADR-0043).

#### Scenario: Gerente altera o projeto
- **WHEN** o gerente do projeto altera os dados e muda o status
- **THEN** as duas ações são aceitas

#### Scenario: Líder técnico altera os dados
- **WHEN** o líder técnico altera os dados do projeto e depois tenta mudar o status
- **THEN** a alteração dos dados é aceita e a mudança de status recebe 403

#### Scenario: Colaborador, desenvolvedor e leitor não alteram o projeto
- **WHEN** um colaborador, um desenvolvedor ou um leitor do projeto pede para alterar os dados ou mudar o status
- **THEN** a API responde 403 e nada muda

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem estar no projeto, altera os dados e muda o status
- **THEN** as duas ações são aceitas

### Requirement: Dados do projeto

Um projeto MUST ter código e nome e pode ter descrição e cliente. O nome MUST ter até 150 caracteres e a descrição, até 4.000, sem os espaços das pontas. O cliente MUST ser um cliente ativo; um cliente que ficou inativo continua no projeto enquanto não for trocado (ADR-0015, ADR-0016).

#### Scenario: Criar projeto
- **WHEN** o gerente de projeto cria o projeto "erp-fin", "Implantação do ERP financeiro", com cliente e descrição
- **THEN** o projeto é criado com o código "ERP-FIN", em Planejamento

#### Scenario: Projeto inválido
- **WHEN** alguém cria um projeto sem nome, com descrição longa ou com um cliente inativo ou que não existe
- **THEN** a API responde 400, com o motivo em cada campo

#### Scenario: Projeto interno
- **WHEN** o gerente de projeto cria um projeto sem cliente
- **THEN** o projeto é criado sem cliente e a lista o mostra como projeto interno

### Requirement: Código do projeto

O código MUST ter de 2 a 15 caracteres, só letras, algarismos e hífens, começar por letra, sem hífen no fim nem dois seguidos. Ele é guardado em maiúsculas e MUST ser único entre todos os projetos, inclusive os encerrados (ADR-0016).

#### Scenario: Código inválido
- **WHEN** alguém cria um projeto com o código "1-ERP", "ERP-", "ERP--FIN" ou "E"
- **THEN** a API responde 400 no campo do código

#### Scenario: Código repetido
- **WHEN** alguém cria ou altera um projeto com o código de outro projeto, mesmo concluído ou cancelado, em maiúsculas ou minúsculas
- **THEN** a API responde 409, com o nome do projeto que já usa o código

### Requirement: Gerente e líder técnico na criação

A criação MUST definir o gerente do projeto: quem cria, por padrão, ou outra pessoa ativa com o papel no sistema Gerente de projeto ou Administrador. Com outro gerente, quem cria MUST NOT entrar no projeto. O líder técnico é opcional, MUST ser uma pessoa ativa diferente do gerente, com o papel no sistema Líder técnico ou Administrador, e entra com o papel Líder técnico (ADR-0072, ADR-0097).

#### Scenario: Quem cria é o gerente
- **WHEN** um gerente de projeto cria um projeto sem trocar o gerente
- **THEN** ele entra no projeto como gerente

#### Scenario: Projeto para outro gerente
- **WHEN** o administrador cria um projeto com Marina Costa como gerente
- **THEN** Marina entra como gerente, e o administrador não entra no projeto

#### Scenario: Gerente sem o papel no sistema
- **WHEN** alguém cria um projeto com um gerente que tem o papel Usuário, está inativo ou não existe
- **THEN** a API responde 400 no campo do gerente

#### Scenario: Líder técnico na criação
- **WHEN** o gerente de projeto cria um projeto com Rafael Souza, que tem o papel no sistema Líder técnico, como líder técnico
- **THEN** Rafael entra no projeto como líder técnico e passa a ver o projeto

#### Scenario: Líder técnico do diretório na criação
- **WHEN** o gerente de projeto cria um projeto com um líder técnico do diretório que nunca entrou
- **THEN** a API responde 400 no campo do líder técnico, porque quem nunca entrou tem o papel Usuário, e nem o projeto nem o usuário são criados

#### Scenario: Líder técnico sem o papel no sistema
- **WHEN** o gerente de projeto cria um projeto com um líder técnico que tem o papel Usuário
- **THEN** a API responde 400 no campo do líder técnico e o projeto não é criado

#### Scenario: Líder técnico inválido
- **WHEN** alguém escolhe como líder técnico o próprio gerente, uma pessoa inativa ou alguém que não existe
- **THEN** a API responde 400 no campo do líder técnico

### Requirement: Status do projeto

O projeto MUST começar em Planejamento e pode passar a Em andamento, Pausado, Concluído ou Cancelado, em qualquer ordem. Concluído ou cancelado, o projeto MUST ficar somente leitura: alterar os dados MUST receber 409, até o gerente ou o administrador reabrir o projeto, mudando o status para Planejamento, Em andamento ou Pausado. Nenhum projeto é excluído (ADR-0016, ADR-0043).

#### Scenario: Pausar e retomar
- **WHEN** o gerente pausa um projeto em andamento e depois o retoma
- **THEN** o status passa a Pausado e depois a Em andamento

#### Scenario: Projeto encerrado somente leitura
- **WHEN** alguém altera os dados de um projeto concluído ou cancelado
- **THEN** a API responde 409 e nada muda

#### Scenario: Reabrir projeto
- **WHEN** o gerente reabre um projeto concluído
- **THEN** o projeto volta a Em andamento e aceita alterações

### Requirement: Lista de projetos

A lista MUST mostrar os projetos que a pessoa vê, em ordem alfabética de nome, com o nome, o código, o cliente (ou "Projeto interno"), o status, o cronograma, o prazo final e o gerente. Por padrão, MUST mostrar só os projetos ativos; o filtro de situação MUST oferecer Ativos, Concluídos e cancelados e Todos. A lista MUST ter busca por código ou nome, sem diferenciar maiúsculas e acentos, e filtro de cliente (ADR-0015, ADR-0016).

#### Scenario: Projetos na lista
- **WHEN** a pessoa abre Projetos
- **THEN** a lista mostra os projetos ativos que ela vê, em ordem alfabética, com o rodapé "N projetos"

#### Scenario: Projetos encerrados pelo filtro
- **WHEN** a pessoa escolhe "Concluídos e cancelados"
- **THEN** a lista mostra só os projetos concluídos e cancelados

#### Scenario: Buscar e filtrar projetos
- **WHEN** a pessoa busca "erp" ou escolhe um cliente
- **THEN** a lista mostra só os projetos que combinam

### Requirement: Tela Novo projeto

O botão "Novo projeto" MUST aparecer só para o administrador e o gerente de projeto. A tela MUST pedir o código, o nome, o cliente, o gerente, o líder técnico e a descrição, como a seção "Dados do projeto" do canvas NovoProjeto. "Criar projeto" MUST abrir a página do projeto criado (ADR-0072).

#### Scenario: Novo projeto pela tela
- **WHEN** o gerente de projeto preenche o Novo projeto, escolhe um líder técnico e escolhe "Criar projeto"
- **THEN** a página do projeto abre, com ele como gerente e o líder técnico no cabeçalho

#### Scenario: Usuário sem Novo projeto
- **WHEN** um usuário abre Projetos
- **THEN** a tela não mostra "Novo projeto"

### Requirement: Gerente e cliente no Novo projeto

O gerente MUST vir com quem cria, marcado "(você)", e trocar o gerente MUST mostrar que quem cria não entra no projeto. "Cadastrar cliente" MUST abrir o diálogo do cliente, sem o logo para o gerente de projeto, e escolher o cliente incluído (ADR-0015, ADR-0046, ADR-0072).

#### Scenario: Cadastrar cliente na criação
- **WHEN** o gerente de projeto escolhe "Cadastrar cliente" e inclui um cliente novo
- **THEN** o cliente novo fica escolhido no campo Cliente

#### Scenario: Trocar o gerente na criação
- **WHEN** o administrador escolhe outro gerente
- **THEN** a tela avisa que ele não entra no projeto

### Requirement: Cabeçalho do projeto

A página do projeto MUST ter o cabeçalho do canvas ProjetoCabecalho: o logo do cliente ou as iniciais, o nome, o status, o código, o cliente, o gerente, o líder técnico e as datas do cronograma, ou "Sem cronograma" sem tarefas nem marcos, e as abas Visão geral, Cronograma e Membros, esta com o número de pessoas. A aba Visão geral MUST mostrar o resumo do projeto da visão geral, como o canvas ProjetoVisaoGeral (ADR-0016, ADR-0071, ADR-0072, ADR-0073).

#### Scenario: Cabeçalho do projeto
- **WHEN** uma pessoa do projeto abre a página do projeto
- **THEN** o cabeçalho mostra o nome, o status, o código, o cliente, o gerente e o líder técnico, e a trilha mostra Projetos e o código

#### Scenario: Abas do projeto
- **WHEN** uma pessoa do projeto escolhe a aba Membros no cabeçalho
- **THEN** a aba Membros fica marcada, mostra o número de pessoas e abre as pessoas do projeto, e Visão geral volta ao resumo do projeto

#### Scenario: Datas do cronograma no cabeçalho
- **WHEN** o cronograma do projeto vai de 05/10/2026 a 13/11/2026
- **THEN** o cabeçalho mostra "05/10 – 13/11/2026" no lugar de "Sem cronograma"

### Requirement: Abas do projeto sem rolagem vertical

A linha das abas do projeto MUST NOT rolar na vertical nem mostrar barra de rolagem; ela só rola para o lado quando as abas não cabem na largura, como no celular (ADR-0066, ADR-0071).

#### Scenario: Abas sem barra de rolagem
- **WHEN** uma pessoa abre a página de um projeto no computador
- **THEN** a linha das abas não rola na vertical e não mostra barra de rolagem, e o traço da aba escolhida fica sobre a linha do cabeçalho

### Requirement: Ações do projeto

"Ações do projeto" MUST oferecer "Editar dados" a quem altera os dados e as mudanças de status a quem muda o status; concluir e cancelar MUST pedir confirmação. Um projeto encerrado MUST mostrar que está somente leitura, com "Reabrir projeto" para quem muda o status (ADR-0016, ADR-0043).

#### Scenario: Editar dados pela tela
- **WHEN** o líder técnico escolhe "Editar dados", troca o nome e salva
- **THEN** o cabeçalho mostra o nome novo, e as mudanças de status não aparecem para ele

#### Scenario: Concluir projeto pela tela
- **WHEN** o gerente escolhe "Concluir projeto" e confirma
- **THEN** o status passa a Concluído, a página avisa que o projeto está somente leitura e oferece "Reabrir projeto"

### Requirement: Meus projetos

A barra lateral MUST listar em Meus projetos os projetos ativos em que a pessoa está, com o nome e o código, e marcar o projeto aberto (ADR-0066).

#### Scenario: Meus projetos na barra lateral
- **WHEN** a pessoa está em projetos ativos
- **THEN** a barra lateral lista esses projetos, com o nome e o código, e marca o projeto aberto

### Requirement: Mudanças dos projetos na auditoria

Criar, alterar e mudar o status de um projeto MUST entrar na auditoria, com quem fez e os campos antes e depois; o cliente aparece pelo nome. A entrada de cada pessoa no projeto MUST entrar na auditoria, com a pessoa, o papel e o projeto pelo nome. A tela de Auditoria MUST ter os tipos Projetos e Pessoas dos projetos no filtro (ADR-0039, ADR-0072, ADR-0084).

#### Scenario: Projeto criado na auditoria
- **WHEN** o administrador cria um projeto com Marina Costa como gerente
- **THEN** a auditoria tem "Criou um projeto", com o administrador como autor, e "Incluiu uma pessoa num projeto", com Marina Costa, Gerente de projeto e o nome do projeto

#### Scenario: Status na auditoria
- **WHEN** o gerente conclui e depois reabre um projeto
- **THEN** a auditoria tem "Concluiu um projeto" e "Reabriu um projeto", com o status antes e depois

### Requirement: Cronograma na lista de projetos

Na lista, a coluna Cronograma MUST mostrar a faixa de fases, com a parte concluída, as fases atrasadas, os marcos e o dia de hoje, e embaixo o progresso com a fase atual: a primeira fase atrasada ou, sem atraso, a última que já começou; antes do início, "Começa em" e a data; com todas as tarefas a 100%, a data da conclusão. O Prazo final MUST mostrar o fim do cronograma e as tarefas e os marcos atrasados, com ícone e texto. Sem itens, "Sem cronograma" (ADR-0065, ADR-0071, ADR-0086).

#### Scenario: Cronograma na lista
- **WHEN** hoje é 16/10/2026 e a pessoa abre Projetos com um projeto de três fases, a segunda em andamento, que termina em 13/11/2026 com duas tarefas atrasadas na segunda fase
- **THEN** a linha do projeto mostra a faixa com as três fases, a segunda atrasada, o progresso com o nome da segunda fase, "13/11/2026" e "2 tarefas atrasadas"

#### Scenario: Projeto que ainda não começou
- **WHEN** hoje é 16/10/2026 e o cronograma de um projeto começa em 26/10/2026
- **THEN** a linha do projeto mostra a faixa e "Começa em 26/10"

#### Scenario: Projeto sem cronograma na lista
- **WHEN** um projeto não tem tarefas nem marcos
- **THEN** a linha do projeto mostra "Sem cronograma" e fica sem prazo final

#### Scenario: Marco atrasado na lista
- **WHEN** hoje é 16/10/2026 e um projeto tem uma tarefa e um marco atrasados
- **THEN** o prazo final do projeto diz "1 tarefa atrasada" e "1 marco atrasado"

### Requirement: Conclusão com chamados

O projeto MUST ser concluído só com todos os seus chamados fechados: qualquer chamado em outro status, inclusive Resolvido, MUST impedir a conclusão com 409 e o número de chamados não fechados. Concluir com tarefas abaixo de 100% MUST ser permitido (ADR-0043).

#### Scenario: Concluir com chamado não fechado
- **WHEN** o gerente conclui um projeto com um chamado Resolvido
- **THEN** a API responde 409 com o número de chamados não fechados, e o projeto continua no mesmo status

#### Scenario: Concluir com os chamados fechados
- **WHEN** o gerente conclui um projeto cujos chamados estão todos fechados
- **THEN** o projeto fica concluído

### Requirement: Cancelamento com chamados

Ao cancelar o projeto, os chamados que não estão fechados MUST ser fechados, cada um com o motivo "projeto cancelado" na conversa e quem cancelou (ADR-0043).

#### Scenario: Cancelar com chamados abertos
- **WHEN** o gerente cancela um projeto com dois chamados abertos
- **THEN** o projeto fica cancelado, e os dois chamados ficam Fechados, cada um com o evento "projeto cancelado" na conversa

### Requirement: Resumo do encerramento

Quem muda o status do projeto MUST poder ver, antes de concluir ou cancelar, quantos chamados não estão fechados, quantas tarefas estão abaixo de 100% e quantos pedidos de aceite estão pendentes; as outras pessoas do projeto MUST receber 403 (ADR-0043).

#### Scenario: Resumo do encerramento
- **WHEN** o gerente pede o resumo do encerramento de um projeto com um chamado aberto, uma tarefa em 40% e um pedido de aceite pendente
- **THEN** a API responde com um chamado não fechado, uma tarefa abaixo de 100% e um aceite pendente

#### Scenario: Resumo só de quem muda o status
- **WHEN** um colaborador do projeto pede o resumo do encerramento
- **THEN** a API responde 403

### Requirement: Encerramento pela tela

"Concluir projeto" MUST mostrar, antes de confirmar, os chamados não fechados, que impedem a conclusão, com o link para a aba Chamados do projeto e sem deixar confirmar, e as tarefas abaixo de 100% e os pedidos de aceite pendentes como aviso, que deixa confirmar. "Cancelar projeto" MUST dizer quantos chamados serão fechados (ADR-0043, ADR-0065).

#### Scenario: Conclusão impedida pela tela
- **WHEN** o gerente escolhe "Concluir projeto" num projeto com chamados não fechados
- **THEN** o diálogo diz quantos são, mostra o link para os chamados e não deixa concluir

#### Scenario: Aviso de tarefas incompletas
- **WHEN** o gerente escolhe "Concluir projeto" num projeto sem chamados abertos e com tarefas abaixo de 100%
- **THEN** o diálogo avisa quantas são e deixa concluir

#### Scenario: Aviso de aceites pendentes
- **WHEN** o gerente escolhe "Concluir projeto" num projeto sem chamados abertos e com um pedido de aceite pendente
- **THEN** o diálogo avisa o aceite pendente e deixa concluir

#### Scenario: Cancelamento pela tela
- **WHEN** o gerente escolhe "Cancelar projeto" num projeto com chamados abertos e confirma
- **THEN** o diálogo diz quantos chamados serão fechados, e o projeto fica cancelado

### Requirement: Atraso do cronograma na lista

A lista de projetos MUST trazer, no cronograma de cada projeto, o atraso em dias úteis do item mais atrasado: a tarefa ou o marco com mais dias úteis de atraso no calendário da instalação; zero sem atraso (ADR-0088, ADR-0096).

#### Scenario: Atraso pelo item mais atrasado
- **WHEN** hoje é 19/10/2026 e um projeto tem uma tarefa com 2 dias úteis de atraso e um marco com 1
- **THEN** a lista traz o cronograma do projeto com 2 dias úteis de atraso

#### Scenario: Cronograma sem atraso
- **WHEN** nenhuma tarefa nem marco do projeto está atrasado
- **THEN** a lista traz o cronograma do projeto com zero dias de atraso

### Requirement: Próximos marcos da pessoa

`GET /api/projects/upcoming-milestones` MUST responder os 10 primeiros marcos não atingidos, de hoje em diante, dos projetos não concluídos nem cancelados de que a pessoa participa, pelo dia e pelo código do projeto, cada um com o projeto, o dia, os dias úteis que faltam e se há pedido de aceite pendente para ele (ADR-0070, ADR-0096).

#### Scenario: Próximos marcos pela API
- **WHEN** hoje é 19/10/2026 e o projeto da pessoa tem os marcos Homologação, em 03/11/2026, com um pedido de aceite pendente, e Entrada em produção, em 13/11/2026
- **THEN** a API responde os dois marcos nessa ordem, com 10 e 18 dias úteis, e o aceite pedido só no primeiro

#### Scenario: Marco atingido ou passado fica fora
- **WHEN** hoje é 19/10/2026, um marco de 21/10 já foi atingido e outro, de 16/10, não foi
- **THEN** nenhum dos dois aparece nos próximos marcos

#### Scenario: Só projetos da pessoa
- **WHEN** o administrador, fora de um projeto, pede os próximos marcos
- **THEN** os marcos desse projeto não aparecem

### Requirement: Excluir projeto

Só o administrador MUST poder excluir um projeto, de qualquer status, digitando o código do projeto para confirmar. A exclusão MUST apagar o projeto e tudo o que é dele em todos os módulos, sem que nada do projeto fique em nenhuma tabela. O cliente, os modelos de projeto e os fornecedores MUST ficar, e os números dos chamados apagados MUST não ser reaproveitados (ADR-0029, ADR-0041, ADR-0111).

#### Scenario: Excluir projeto
- **WHEN** o administrador exclui um projeto com cronograma, chamados com anexos, documentos, links, acessos e termos de aceite, digitando o código
- **THEN** o projeto deixa de existir, nenhuma tabela guarda mais nada dele, os arquivos que o sistema criou saem do armazenamento, e o cliente e os modelos de projeto continuam

#### Scenario: Código errado
- **WHEN** o administrador pede a exclusão com um código diferente do código do projeto
- **THEN** a API recusa com 400 no campo do código, e nada é apagado

#### Scenario: Só o administrador exclui
- **WHEN** o gerente ou outra pessoa do projeto pede o resumo ou a exclusão
- **THEN** a API recusa com 403, e quem não está no projeto recebe 404

#### Scenario: Número de chamado não reaproveitado
- **WHEN** um chamado é aberto depois da exclusão de um projeto com chamados
- **THEN** ele recebe o número seguinte ao maior já usado, nunca um dos apagados

### Requirement: Arquivos do projeto excluído

Na exclusão, MUST sair do armazenamento só o que o sistema registrou para o projeto (anexos, documentos com as versões e termos) e as pastas que ficarem vazias; o resto MUST ficar (ADR-0041, ADR-0111).

#### Scenario: Arquivo de fora fica
- **WHEN** a pasta do projeto no armazenamento tem um arquivo que o sistema não criou
- **THEN** a exclusão apaga os arquivos do sistema e mantém esse arquivo e as pastas acima dele

### Requirement: Resumo da exclusão

Antes de excluir, o administrador MUST poder ver quantos chamados, documentos, links, acessos, termos de aceite, tarefas e marcos e pessoas o projeto tem (ADR-0041).

#### Scenario: Resumo da exclusão
- **WHEN** o administrador pede o resumo de um projeto com dois chamados, um documento e três tarefas
- **THEN** a API responde com o código e o nome do projeto e essas quantidades

### Requirement: Exclusão na auditoria

A exclusão MUST ficar na auditoria num registro só, de quem excluiu, com o código, o nome, o cliente e o status do projeto e quantos itens de cada tipo foram apagados, sem registros das linhas apagadas. Os registros anteriores do projeto MUST continuar (ADR-0039, ADR-0084, ADR-0111).

#### Scenario: Exclusão na auditoria
- **WHEN** o administrador exclui um projeto
- **THEN** a auditoria tem um registro "Excluiu um projeto" com o nome, o código e o resumo do que foi apagado, e os registros anteriores continuam lá

### Requirement: Exclusão pela tela

"Ações do projeto" MUST oferecer "Excluir projeto" só ao administrador. O diálogo MUST mostrar o resumo do que será apagado, avisar que não dá para desfazer e só deixar excluir com o código do projeto digitado; depois da exclusão, a tela MUST voltar para a lista de projetos com a confirmação (ADR-0041, ADR-0065).

#### Scenario: Excluir pela tela
- **WHEN** o administrador escolhe "Excluir projeto", digita o código e confirma
- **THEN** o diálogo mostrou o resumo, o botão só ficou disponível com o código certo, e a tela volta para Projetos com a confirmação

#### Scenario: Excluir só para o administrador
- **WHEN** o gerente abre "Ações do projeto"
- **THEN** "Excluir projeto" não aparece
