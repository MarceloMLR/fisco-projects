# horas-do-projeto Specification

## Purpose

As horas do projeto, só em horas: cada pessoa aponta as próprias horas num dia de um projeto em que está, com a descrição e, opcional, a tarefa ou o chamado, vê e corrige só as dela, e o administrador corrige as de qualquer pessoa; com as regras de quem aponta, a auditoria, as opções para apontar e a tela Minhas horas no computador e no celular (ADR-0128).

## Requirements

### Requirement: Apontamento de horas

Quem aponta horas num projeto MUST incluir o apontamento com o dia, as horas, a descrição (até 500 caracteres) e o projeto. O dia MUST ser no máximo hoje, no fuso da instalação (ADR-0013). As horas MUST ir de 0,25 a 24, em quartos de hora, e a soma dos apontamentos da pessoa num dia MUST não passar de 24 horas. O apontamento MUST guardar quem apontou e quando. Os erros MUST voltar no campo, em Problem Details (ADR-0128).

#### Scenario: Apontar horas no projeto
- **WHEN** a colaboradora aponta 2,5 horas de ontem no projeto ERP-FIN, com a descrição "Reunião de levantamento"
- **THEN** a API grava o apontamento com ela como autora e o devolve na lista dela, com o projeto, o dia, as horas e a descrição

#### Scenario: Dia depois de hoje
- **WHEN** alguém aponta horas para amanhã
- **THEN** a API recusa com 400 no campo do dia, e nada é gravado

#### Scenario: Horas fora dos quartos de hora
- **WHEN** alguém aponta 1,1 hora, 0 hora ou 25 horas
- **THEN** a API recusa com 400 no campo das horas

#### Scenario: Mais de 24 horas no dia
- **WHEN** a pessoa já tem 20 horas apontadas num dia e aponta mais 5 nesse dia, em qualquer projeto
- **THEN** a API recusa com 400 no campo das horas, e o apontamento não é gravado

#### Scenario: Apontamento sem descrição
- **WHEN** alguém aponta horas com a descrição em branco
- **THEN** a API recusa com 400 no campo da descrição

### Requirement: Tarefa ou chamado do apontamento

O apontamento MAY indicar uma tarefa do cronograma do projeto ou um chamado do projeto, nunca os dois. A tarefa MUST ser uma tarefa do cronograma daquele projeto, e não uma fase ou um marco; o chamado MUST ser daquele projeto. Uma tarefa excluída depois MUST deixar o apontamento no projeto, mostrado como tarefa excluída (ADR-0128).

#### Scenario: Apontar numa tarefa
- **WHEN** o desenvolvedor aponta 3 horas na tarefa "Configurar o ambiente" do ERP-FIN
- **THEN** a lista dele mostra o apontamento com o nome da tarefa

#### Scenario: Apontar num chamado
- **WHEN** a colaboradora aponta 1 hora no chamado #104 do ERP-FIN
- **THEN** a lista dela mostra o apontamento com o número e o título do chamado

#### Scenario: Tarefa de outro projeto
- **WHEN** alguém aponta horas no ERP-FIN indicando uma tarefa do TMS-DELTA, um marco ou uma fase
- **THEN** a API recusa com 400 no campo da tarefa

#### Scenario: Chamado de outro projeto
- **WHEN** alguém aponta horas no ERP-FIN indicando um chamado do TMS-DELTA
- **THEN** a API recusa com 400 no campo do chamado

#### Scenario: Tarefa e chamado juntos
- **WHEN** alguém indica uma tarefa e um chamado no mesmo apontamento
- **THEN** a API recusa com 400

#### Scenario: Tarefa excluída depois
- **WHEN** a tarefa de um apontamento é excluída do cronograma
- **THEN** o apontamento continua no projeto, com as mesmas horas, e a lista o mostra como tarefa excluída

### Requirement: Horas de cada pessoa

Cada pessoa MUST ver, corrigir e apagar só as próprias horas, por período de até 93 dias, com o total do período, e cada apontamento da lista MUST dizer se quem pede pode corrigi-lo e apagá-lo. Corrigir MUST seguir as regras do apontamento, também no projeto de destino quando o projeto muda (ADR-0128).

#### Scenario: Só as próprias horas
- **WHEN** a colaboradora pede as horas da semana
- **THEN** a API devolve só os apontamentos dela, de todos os projetos, e o total da semana

#### Scenario: Horas de outra pessoa
- **WHEN** a colaboradora tenta ver, corrigir ou apagar um apontamento do desenvolvedor, ou pede a lista de outra pessoa
- **THEN** a API responde 404 ao apontamento e 403 à lista, e nada muda

#### Scenario: Período longo demais
- **WHEN** alguém pede as horas de um período de mais de 93 dias
- **THEN** a API recusa com 400

### Requirement: Horas de outra pessoa pelo administrador

O administrador MUST ver, corrigir e apagar as horas de qualquer pessoa, com as mesmas regras do apontamento (ADR-0046, ADR-0128).

#### Scenario: Administrador corrige as horas de outra pessoa
- **WHEN** o administrador pede as horas do desenvolvedor e corrige um apontamento dele de 8 para 6 horas
- **THEN** a lista do desenvolvedor passa a mostrar 6 horas, e a auditoria registra a correção pelo administrador

### Requirement: Horas de quem saiu do projeto

Quem saiu do projeto ou passou a leitor MUST não apontar nem corrigir horas nele, e as horas que apontou MUST continuar no projeto (ADR-0128).

#### Scenario: Quem saiu do projeto
- **WHEN** a colaboradora sai do ERP-FIN e tenta apontar ou corrigir horas nele
- **THEN** a API recusa, e os apontamentos que ela fez continuam no projeto e na lista dela, sem as ações de corrigir e apagar

### Requirement: Horas na auditoria

Toda inclusão, correção e exclusão de um apontamento MUST ir para a auditoria, com o projeto, a pessoa, a tarefa e o chamado pelos nomes atuais (ADR-0039, ADR-0084).

#### Scenario: Horas na auditoria
- **WHEN** alguém inclui, corrige e apaga um apontamento
- **THEN** a auditoria tem os três registros, com o projeto e a pessoa pelos nomes atuais

### Requirement: Permissões nas horas

Apontar horas MUST ser do administrador, em qualquer projeto, e das pessoas do projeto, menos o leitor. Corrigir e apagar as horas de outra pessoa MUST ser só do administrador, e ver as horas de outra pessoa também, salvo os apontamentos de uma tarefa, que o gerente do projeto vê nos projetos que gerencia (ADR-0144). Quem não está no projeto MUST receber 404, o leitor 403, e o projeto concluído ou cancelado MUST responder 409 (ADR-0016, ADR-0040, ADR-0128).

#### Scenario: Permissões nas horas por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam apontar horas no projeto, corrigir e apagar o próprio apontamento e ver, corrigir e apagar o apontamento de outra pessoa
- **THEN** o administrador faz tudo; o gerente, o líder técnico, o colaborador e o desenvolvedor apontam, corrigem e apagam os próprios e recebem 404 no de outra pessoa; o leitor recebe 403 ao apontar e 404 no de outra pessoa

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem papel no projeto, aponta horas nele
- **THEN** a API grava o apontamento

#### Scenario: Horas de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador aponta horas nele
- **THEN** a API responde 404

#### Scenario: Horas de projeto encerrado
- **WHEN** alguém aponta, corrige ou apaga horas de um projeto concluído
- **THEN** a API responde 409 e nada muda

### Requirement: Opções para apontar

A API MUST dizer à pessoa em que projetos ela aponta horas, os em aberto em que tem papel, menos o de leitor, e, para o administrador, todos os em aberto, com o código e o nome. Para um desses projetos, MUST dizer as tarefas do cronograma, na ordem do cronograma, e os chamados, primeiro os não fechados, com o número e o título (ADR-0128).

#### Scenario: Projetos para apontar
- **WHEN** a colaboradora, que está no ERP-FIN, no TMS-DELTA como leitora e num projeto concluído, pede os projetos para apontar
- **THEN** a API devolve só o ERP-FIN

#### Scenario: Tarefas e chamados do projeto
- **WHEN** a colaboradora pede as opções do ERP-FIN
- **THEN** a API devolve as tarefas do cronograma, sem as fases e os marcos, e os chamados do projeto

### Requirement: Minhas horas pela tela

A aplicação MUST ter a tela Minhas horas, com a semana de segunda a domingo: os apontamentos da pessoa agrupados por dia, com o total de cada dia e da semana, cada um com o projeto, a tarefa ou o chamado, a descrição, as horas e o "⋮" com corrigir e apagar; a semana anterior, a seguinte e "Esta semana". Os dias seguem o fuso da instalação, e no celular cada apontamento mostra o projeto, a descrição e as horas (ADR-0013, ADR-0065, ADR-0066).

#### Scenario: Semana pela tela
- **WHEN** a pessoa abre Minhas horas
- **THEN** a tela mostra a semana atual, com os apontamentos dela agrupados por dia, o total de cada dia e o total da semana

#### Scenario: Corrigir e apagar pela tela
- **WHEN** a pessoa corrige as horas de um apontamento pelo "⋮" e depois apaga outro, confirmando
- **THEN** a lista e os totais mudam a cada vez

#### Scenario: Trocar a semana
- **WHEN** a pessoa vai para a semana anterior e depois escolhe "Esta semana"
- **THEN** a tela mostra os apontamentos da semana anterior e depois volta à semana atual

#### Scenario: Semana sem horas
- **WHEN** a pessoa abre uma semana sem apontamentos
- **THEN** a tela diz que não há horas na semana e oferece "Apontar horas"

#### Scenario: Minhas horas no celular
- **WHEN** a pessoa abre Minhas horas numa tela menor que 900px
- **THEN** cada apontamento mostra o projeto, a descrição e as horas, sem rolagem horizontal da página

### Requirement: Apontar pela tela

"Apontar horas" MUST abrir o diálogo com o dia (hoje, de início), o projeto, a tarefa ou o chamado opcional, as horas, aceitando "1,5" e "1:30", e a descrição; o erro da API MUST voltar ao campo (ADR-0128).

#### Scenario: Apontar pela tela
- **WHEN** a pessoa escolhe "Apontar horas", escolhe o projeto e uma tarefa, digita "1:30" e a descrição e confirma
- **THEN** o apontamento de 1,5 hora aparece no dia escolhido, os totais mudam e a tela confirma

#### Scenario: Erro no campo
- **WHEN** a pessoa aponta horas que passam de 24 no dia
- **THEN** o diálogo continua aberto, com o erro no campo das horas

### Requirement: Horas contratadas

Só o administrador MUST cadastrar, trocar e tirar as horas contratadas de um projeto, de qualquer situação. As horas contratadas MUST ser maiores que zero e ir até 99.999, em quartos de hora. Sem horas contratadas, o projeto MUST ficar sem saldo (ADR-0128).

#### Scenario: Cadastrar as horas contratadas
- **WHEN** o administrador cadastra 400 horas contratadas num projeto e depois troca para 420
- **THEN** o detalhe do projeto mostra 420 horas contratadas

#### Scenario: Tirar as horas contratadas
- **WHEN** o administrador tira as horas contratadas de um projeto
- **THEN** o projeto passa a aparecer sem horas contratadas, sem saldo nem consumo

#### Scenario: Horas contratadas inválidas
- **WHEN** o administrador informa 0 ou 10,1 horas contratadas
- **THEN** a API recusa com 400 no campo das horas, e nada muda

### Requirement: Horas adicionais

Só o administrador MUST incluir, corrigir e apagar as horas adicionais de um projeto, de qualquer situação, cada uma com o dia (até hoje, no fuso da instalação), as horas (de 0,25 a 9.999, em quartos de hora) e o motivo (até 500 caracteres). A adicional MUST guardar quem a incluiu (ADR-0128).

#### Scenario: Incluir horas adicionais
- **WHEN** o administrador inclui 16 horas adicionais com o motivo "Integração com um segundo banco"
- **THEN** o detalhe do projeto mostra a adicional com o dia, as horas, o motivo e quem a incluiu, e o total das adicionais soma as 16 horas

#### Scenario: Corrigir e apagar horas adicionais
- **WHEN** o administrador corrige uma adicional de 16 para 12 horas e depois a apaga
- **THEN** o total das adicionais passa a contar 12 horas e, depois, nada dela

#### Scenario: Horas adicionais sem motivo
- **WHEN** o administrador inclui horas adicionais sem motivo, com 0 horas ou com o dia de amanhã
- **THEN** a API recusa com 400 no campo de cada erro, e nada é incluído

### Requirement: Saldo do projeto

O saldo MUST ser as horas contratadas mais as adicionais menos as apontadas, e o consumo, as apontadas sobre as contratadas mais as adicionais, em porcentagem arredondada. O estado do saldo MUST ser: acima, quando as apontadas passam do total; perto do limite, quando o consumo chega a 90% num projeto em aberto; dentro, nos outros casos; e sem horas contratadas, quando o projeto não as tem (ADR-0065, ADR-0128).

#### Scenario: Projeto acima das horas contratadas
- **WHEN** um projeto tem 400 horas contratadas, 40 adicionais e 468 apontadas
- **THEN** o saldo é de 28 horas acima, com o consumo de 106%

#### Scenario: Projeto perto do limite
- **WHEN** um projeto em andamento tem 160 horas contratadas e 146 apontadas
- **THEN** o saldo está perto do limite, restam 14 horas, e o consumo é de 91%

#### Scenario: Projeto encerrado perto do limite
- **WHEN** um projeto concluído tem 160 horas contratadas e 150 apontadas
- **THEN** o saldo está dentro, porque o projeto não recebe mais horas

#### Scenario: Projeto sem horas contratadas
- **WHEN** um projeto tem só horas adicionais e apontadas, sem horas contratadas
- **THEN** o saldo fica sem horas contratadas, sem número de saldo nem consumo

### Requirement: Horas dos projetos

O administrador MUST ver, por projeto, o código, o nome, o cliente, a situação, as horas contratadas, as adicionais, as apontadas, o saldo, o consumo e o estado, com as visões Em aberto (padrão), Encerrados e Todos e a busca por código ou nome sem diferenciar maiúsculas e acentos. A lista MUST vir do maior consumo para o menor, com os sem horas contratadas por último, pelo código (ADR-0128).

#### Scenario: Lista das horas dos projetos
- **WHEN** o administrador pede as horas dos projetos em aberto
- **THEN** a API responde com cada projeto em aberto, os números do saldo e o estado, o mais consumido primeiro e o sem horas contratadas por último

#### Scenario: Busca nas horas dos projetos
- **WHEN** o administrador busca "implantacao" na visão Todos
- **THEN** a lista traz só os projetos com "Implantação" no nome, em aberto e encerrados

### Requirement: Detalhe das horas do projeto

O detalhe de um projeto MUST trazer os números do saldo, o primeiro dia com horas apontadas, as horas adicionais do dia mais recente para o mais antigo, com quem as incluiu, e as horas de cada pessoa que apontou, com o papel atual no projeto e a parte dela nas apontadas. Quem saiu do projeto MUST continuar, sem papel (ADR-0110, ADR-0128).

#### Scenario: Detalhe do projeto
- **WHEN** o administrador abre o detalhe de um projeto com horas da gerente e de dois desenvolvedores
- **THEN** a API responde com as três pessoas, do maior número de horas para o menor, cada uma com o papel e a parte

#### Scenario: Pessoa que saiu no detalhe
- **WHEN** uma pessoa que apontou horas sai do projeto
- **THEN** o detalhe continua com as horas dela, sem papel

### Requirement: Apontamentos de um projeto pelo administrador

O administrador MUST ver os apontamentos de todas as pessoas de um projeto num período de até 93 dias, cada um com a pessoa que apontou, e corrigi-los e apagá-los com as regras do apontamento. Quem não é administrador MUST receber 403 ao pedir os apontamentos de um projeto (ADR-0128).

#### Scenario: Apontamentos do projeto
- **WHEN** o administrador pede os apontamentos de outubro de um projeto
- **THEN** a API responde com os apontamentos de todas as pessoas no projeto em outubro, cada um com a pessoa, e o total

#### Scenario: Apontamentos do projeto sem ser administrador
- **WHEN** a gerente do projeto pede os apontamentos do projeto
- **THEN** a API recusa com 403

### Requirement: Permissões no saldo das horas

Ver as horas dos projetos e o detalhe, cadastrar as horas contratadas e incluir, corrigir e apagar horas adicionais MUST ser permitido ao administrador e negado, com 403, ao gerente de projeto, ao líder técnico e ao usuário, mesmo no projeto deles (ADR-0046, ADR-0128).

#### Scenario: Permissões no saldo por papel
- **WHEN** cada papel no sistema tenta ver as horas dos projetos, ver o detalhe, cadastrar as horas contratadas e incluir, corrigir e apagar horas adicionais
- **THEN** o administrador consegue tudo, e o gerente de projeto, o líder técnico e o usuário recebem 403 em cada ação, sem mudar nada

### Requirement: Saldo na auditoria

As mudanças das horas contratadas e das horas adicionais MUST ir para a auditoria, com o projeto como contexto (ADR-0039).

#### Scenario: Horas contratadas e adicionais na auditoria
- **WHEN** o administrador cadastra as horas contratadas e inclui horas adicionais num projeto
- **THEN** a auditoria tem as duas inclusões, por ele, no projeto

### Requirement: Horas dos projetos pela tela

A Administração MUST ter a seção Acompanhamento, a primeira da coluna, com Horas dos projetos: o título, quantos projetos a visão mostra e quantos estão acima, "Baixar planilha", a busca, as visões e a lista com o projeto (nome, código e cliente), a situação, as contratadas, as adicionais, as apontadas, o saldo (ícone, cor e texto) e o consumo (barra e porcentagem), como o canvas "Horas do projeto".

#### Scenario: Horas dos projetos pela tela
- **WHEN** o administrador escolhe Horas dos projetos na coluna da Administração
- **THEN** a tela mostra os projetos em aberto, o acima com "28 h acima" em vermelho e o sem horas contratadas com "Sem horas contratadas" e sem consumo

#### Scenario: Baixar a planilha das horas
- **WHEN** o administrador escolhe "Baixar planilha"
- **THEN** a tela baixa um `.xlsx` com uma linha por projeto da visão e da busca, com os números do saldo

### Requirement: Detalhe das horas pela tela

Escolher um projeto em Horas dos projetos MUST abrir o detalhe numa gaveta (ADR-0081): os quatro números com "Alterar" nas contratadas, a barra do consumo, as três horas adicionais mais recentes, com "Incluir horas adicionais", o "⋮" de corrigir e apagar e "Ver todas", que abre todas num diálogo, dez por página, e "Por pessoa" com "Ver apontamentos", que abre os apontamentos do projeto por mês, com o "⋮" de corrigir e apagar.

#### Scenario: Incluir horas adicionais pela tela
- **WHEN** o administrador abre o detalhe, escolhe "Incluir horas adicionais", preenche o dia, 12 horas e o motivo e confirma
- **THEN** a adicional aparece na lista do detalhe, os números do projeto mudam, e a tela confirma a inclusão

#### Scenario: Muitas horas adicionais pela tela
- **WHEN** o administrador abre o detalhe de um projeto com 23 horas adicionais e escolhe "Ver todas"
- **THEN** a gaveta mostra só as três mais recentes, e o diálogo mostra as 23, dez por página, com o "⋮" de corrigir e apagar

#### Scenario: Alterar as horas contratadas pela tela
- **WHEN** o administrador escolhe "Alterar", informa 420 e salva
- **THEN** o detalhe e a lista mostram 420 horas contratadas e o novo saldo

#### Scenario: Ver apontamentos pela tela
- **WHEN** o administrador escolhe "Ver apontamentos" no detalhe
- **THEN** abre a lista dos apontamentos do projeto no mês atual, por dia, com a pessoa de cada um, e a troca de mês

### Requirement: Horas previstas da tarefa

Só o administrador MUST pôr, trocar e tirar as horas previstas de uma tarefa do cronograma de um projeto, de qualquer situação. A tarefa MAY ser uma tarefa-resumo ou uma subtarefa; o marco e a fase MUST NOT receber horas previstas, nem a tarefa de outro projeto. As horas previstas MUST ir de 0,25 a 9.999, em quartos de hora, e tirá-las MUST deixar a tarefa sem horas previstas (ADR-0128, ADR-0138).

#### Scenario: Pôr as horas previstas
- **WHEN** o administrador põe 40 horas previstas na tarefa "Parametrizar regras de ICMS" e depois troca para 48
- **THEN** as horas das tarefas do projeto mostram a tarefa com 48 horas previstas

#### Scenario: Tirar as horas previstas
- **WHEN** o administrador tira as horas previstas de uma tarefa sem horas apontadas
- **THEN** a tarefa sai das horas das tarefas do projeto

#### Scenario: Horas previstas inválidas
- **WHEN** o administrador informa 0, 10,1 ou 10.000 horas previstas
- **THEN** a API recusa com 400 no campo das horas, e nada muda

#### Scenario: Horas previstas fora de uma tarefa
- **WHEN** o administrador põe horas previstas num marco, numa fase ou numa tarefa de outro projeto
- **THEN** a API recusa com 404, e nada muda

### Requirement: Horas previstas fora do cronograma

As horas previstas MUST NOT mudar as datas, o progresso, o desfazer, a linha de base, os modelos nem o XML do MS Project do cronograma, e MUST sair com o projeto excluído (ADR-0091, ADR-0111, ADR-0138).

#### Scenario: Cronograma igual com as previstas
- **WHEN** o administrador põe horas previstas numa tarefa
- **THEN** o cronograma continua com as mesmas datas e o mesmo progresso, e o desfazer não oferece a mudança

#### Scenario: Horas previstas na exclusão do projeto
- **WHEN** o administrador exclui um projeto com horas previstas nas tarefas
- **THEN** as horas previstas saem com o projeto

### Requirement: Horas das tarefas

O administrador, em qualquer projeto, e o gerente do projeto, nos projetos que gerencia, MUST ver as tarefas com horas previstas ou apontadas, na ordem do cronograma, cada uma com o nome atual, as previstas, as apontadas nela por qualquer pessoa, o saldo, o consumo e o estado, e os totais do projeto: as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas do cronograma, sem tarefa ou numa tarefa excluída. A resposta MUST dizer se quem pede pode mudar as previstas (ADR-0128, ADR-0138, ADR-0144).

#### Scenario: Horas fora das tarefas
- **WHEN** um projeto tem horas apontadas sem tarefa e numa tarefa que foi excluída do cronograma
- **THEN** essas horas contam como apontadas fora das tarefas, e a tarefa excluída não aparece

#### Scenario: Tarefa sem horas previstas
- **WHEN** uma tarefa tem só horas apontadas
- **THEN** a tarefa aparece sem horas previstas, sem saldo nem consumo

#### Scenario: Horas das tarefas pela gerente
- **WHEN** a gerente do projeto pede as horas das tarefas de um projeto que gerencia, também depois de concluído
- **THEN** a API devolve as tarefas com as previstas e as apontadas, e diz que ela não pode mudar as previstas

### Requirement: Estado das horas da tarefa

O estado das horas de uma tarefa MUST seguir a regra do saldo do projeto, com a tarefa concluída no lugar do projeto encerrado: acima das previstas, perto das previstas a partir de 90% numa tarefa abaixo de 100%, dentro e sem horas previstas (ADR-0065, ADR-0138).

#### Scenario: Tarefa acima das previstas
- **WHEN** uma tarefa tem 40 horas previstas e 52 apontadas por duas pessoas
- **THEN** a tarefa está acima das previstas, com o saldo de 12 horas acima e o consumo de 130%

#### Scenario: Tarefa perto das previstas
- **WHEN** uma tarefa em andamento tem 20 horas previstas e 18 apontadas
- **THEN** a tarefa está perto das previstas, restam 2 horas, e o consumo é de 90%

#### Scenario: Tarefa concluída perto das previstas
- **WHEN** uma tarefa a 100% tem 20 horas previstas e 19 apontadas
- **THEN** a tarefa está dentro das previstas, porque não recebe mais trabalho

### Requirement: Permissões nas horas previstas

Ver as horas das tarefas e os apontamentos de uma tarefa MUST ser permitido ao administrador e ao gerente do projeto e negado, com 403, ao gerente de outro projeto, ao líder técnico, ao colaborador, ao desenvolvedor e ao leitor do projeto; quem não está no projeto e não é administrador MUST receber 404. Pôr, trocar e tirar as horas previstas MUST ser só do administrador, com 403 para todos os outros, mesmo no projeto deles (ADR-0046, ADR-0128, ADR-0138, ADR-0144).

#### Scenario: Permissões nas horas previstas por papel
- **WHEN** cada papel tenta ver as horas das tarefas de um projeto, ver os apontamentos de uma tarefa dele e pôr as horas previstas nessa tarefa
- **THEN** o administrador consegue as três; o gerente do projeto vê as horas e os apontamentos e recebe 403 ao pôr as previstas; o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403 em cada uma; quem está fora recebe 404 nas leituras e 403 ao pôr; e nada muda

#### Scenario: Gerente de outro projeto
- **WHEN** a gerente pede as horas das tarefas de um projeto em que é colaboradora
- **THEN** a API responde 403

### Requirement: Horas previstas na auditoria

Toda mudança das horas previstas MUST ir para a auditoria, com a tarefa pelo nome atual e o projeto como contexto (ADR-0039, ADR-0084).

#### Scenario: Horas previstas na auditoria
- **WHEN** o administrador põe e depois troca as horas previstas de uma tarefa
- **THEN** a auditoria tem a inclusão e a alteração, por ele, com o nome da tarefa, no projeto

### Requirement: Horas na Lista do cronograma pela tela

Para o administrador e para o gerente do projeto, a Lista do cronograma MUST ter a coluna "Horas", com as apontadas e as previstas de cada tarefa ("12 de 16 h"), o estado por ícone, cor e texto quando a tarefa está acima ou perto das previstas, e só as apontadas quando a tarefa não tem previstas. Para as outras pessoas, a Lista MUST NOT mostrar horas nem pedi-las à API (ADR-0065, ADR-0138, ADR-0144).

#### Scenario: Horas na Lista do administrador
- **WHEN** o administrador abre a Lista de um cronograma com uma tarefa de 52 horas apontadas e 40 previstas
- **THEN** a coluna "Horas" mostra "52 de 40 h" com o ícone e a cor de acima das previstas

#### Scenario: Horas na Lista da gerente
- **WHEN** a gerente do projeto abre a Lista do cronograma
- **THEN** a coluna "Horas" mostra as horas de cada tarefa, como para o administrador

#### Scenario: Cronograma sem horas para a gestão
- **WHEN** o líder técnico do projeto abre a Lista e o detalhe de uma tarefa
- **THEN** a tela não tem a coluna "Horas" nem a seção "Horas", e as horas não são pedidas

### Requirement: Horas no detalhe da tarefa pela tela

Para o administrador e para o gerente do projeto, o detalhe de uma tarefa MUST ter a seção "Horas", com as previstas, as apontadas, o estado e a barra do consumo. "Alterar", que abre o diálogo das previstas, com o campo vazio para tirar, MUST aparecer só para quem pode mudá-las, o administrador. Com horas apontadas na tarefa, "Ver apontamentos" MUST aparecer ao lado das apontadas (ADR-0138, ADR-0144).

#### Scenario: Alterar as previstas pelo detalhe da tarefa
- **WHEN** o administrador abre o detalhe de uma tarefa, escolhe "Alterar" na seção "Horas", informa 48 e salva
- **THEN** a seção e a coluna mostram as 48 horas previstas, e a tela confirma

#### Scenario: Horas no detalhe para a gerente
- **WHEN** a gerente do projeto abre o detalhe de uma tarefa com horas previstas e apontadas
- **THEN** a seção "Horas" mostra as previstas, as apontadas, o saldo e o consumo e "Ver apontamentos", sem "Alterar"

### Requirement: Horas por tarefa no detalhe das horas pela tela

O detalhe de um projeto em Horas dos projetos MUST ter a seção "Por tarefa", com as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas, a lista das tarefas com previstas ou apontadas, cada uma com as previstas, as apontadas e o saldo (ícone, cor e texto) e "Alterar", que abre o diálogo das previstas, e "Abrir o cronograma", que leva à Lista do cronograma do projeto (ADR-0081, ADR-0138).

#### Scenario: Por tarefa no detalhe das horas
- **WHEN** o administrador abre o detalhe de um projeto com previstas em duas tarefas e horas apontadas sem tarefa
- **THEN** a seção "Por tarefa" mostra as duas tarefas com o saldo de cada uma e as horas apontadas fora das tarefas

#### Scenario: Alterar as previstas pelo detalhe das horas
- **WHEN** o administrador escolhe "Alterar" numa tarefa da seção "Por tarefa", informa 24 e salva
- **THEN** a tarefa mostra 24 horas previstas e o novo saldo, e a tela confirma

### Requirement: Carga da equipe

O sistema MUST dar ao administrador a carga de cada pessoa em oito semanas, de segunda a domingo, a partir da semana pedida (a atual, sem pedido), entre os projetos não concluídos nem cancelados (ADR-0142). A carga da semana MUST somar, em cada dia útil dela, as horas previstas de cada tarefa abaixo de 100% da pessoa divididas pelos dias úteis da tarefa, e trazer as tarefas que pesam nela.

#### Scenario: Tarefa espalhada pelas semanas
- **WHEN** a Carla é responsável por uma tarefa de 40 horas previstas de quinta, 22/10, a quarta, 28/10, em 5 dias úteis
- **THEN** a semana de 19/10 tem 16 horas dela e a de 26/10, 24 horas, cada uma com a tarefa e as horas na semana

#### Scenario: Carga sobre um feriado
- **WHEN** uma tarefa de 24 horas previstas vai de 30/10 a 04/11, com o feriado de 02/11 no meio
- **THEN** cada um dos 3 dias úteis pesa 8 horas, e o feriado não pesa

#### Scenario: Carga entre projetos
- **WHEN** o Diego tem tarefas com horas previstas em dois projetos na mesma semana
- **THEN** a carga da semana dele soma as duas, cada uma com o projeto

#### Scenario: O que não pesa
- **WHEN** a pessoa tem uma tarefa a 100%, uma tarefa sem horas previstas e uma tarefa de um projeto concluído, e o cliente é responsável por outra
- **THEN** nenhuma delas pesa na carga

### Requirement: Capacidade da semana

A capacidade de uma pessoa numa semana MUST ser os dias úteis da semana menos os dias úteis em que a pessoa está ausente, vezes as horas por dia de trabalho da instalação (ADR-0141, ADR-0142). A ausência MUST NOT tirar a carga da semana.

#### Scenario: Capacidade com férias
- **WHEN** a Carla está ausente de quarta a sexta numa semana de 5 dias úteis, com 8 horas por dia de trabalho
- **THEN** a capacidade dela na semana é 16 horas, com 3 dias de ausência, e a carga das tarefas dela continua contando

#### Scenario: Capacidade com feriado
- **WHEN** a semana tem o feriado de 02/11
- **THEN** a capacidade de quem não está ausente é 32 horas

### Requirement: Estado da carga

Cada semana de cada pessoa MUST ter um estado, num lugar só: acima da capacidade (a carga passa da capacidade, também com capacidade zero), perto do limite (a carga chega a 90% da capacidade), dentro e livre (sem carga). A pessoa MUST dizer se está acima da capacidade em alguma das semanas (ADR-0142).

#### Scenario: Estados da carga
- **WHEN** a capacidade é 40 horas e a carga é 44, 38, 20 ou 0 horas
- **THEN** o estado é acima da capacidade, perto do limite, dentro ou livre

#### Scenario: Carga na semana toda de férias
- **WHEN** a pessoa está ausente a semana inteira e tem uma tarefa com horas previstas nela
- **THEN** a capacidade é zero e o estado é acima da capacidade

### Requirement: Pessoas da carga

A carga MUST trazer as pessoas ativas dos projetos não concluídos nem cancelados, em qualquer papel, mesmo sem carga, e quem tem carga sem estar num deles, pelo nome.

#### Scenario: Pessoa livre na carga
- **WHEN** o Lucas é leitor de um projeto em andamento e não tem tarefas
- **THEN** ele aparece com as oito semanas livres

#### Scenario: Pessoa desativada fora da carga
- **WHEN** uma pessoa foi desativada
- **THEN** ela não aparece na carga

### Requirement: Permissões na carga

`GET /api/admin/team-load` MUST ser só do administrador; as outras pessoas MUST receber 403 (ADR-0138, ADR-0142).

#### Scenario: Permissões na carga por papel
- **WHEN** cada papel pede a carga da equipe
- **THEN** o administrador recebe a carga, e o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403

### Requirement: Carga da equipe pela tela

A Administração MUST ter "Carga da equipe" na seção Acompanhamento, depois de Horas dos projetos, com uma linha por pessoa e uma coluna por semana, cada célula com a carga e a capacidade em horas e o estado por ícone, cor e texto, a marca da ausência na semana, a semana anterior, a seguinte e "Esta semana", a busca pelo nome e as visões "Todas" e "Com sobrecarga"; o subtítulo MUST dizer quantas pessoas estão acima da capacidade no período (ADR-0142).

#### Scenario: Carga da equipe na tela
- **WHEN** o administrador abre Carga da equipe
- **THEN** a tela mostra as oito semanas a partir da atual, a carga e a capacidade de cada pessoa em cada uma, com o estado, e quantas pessoas estão acima da capacidade

#### Scenario: Com sobrecarga pela tela
- **WHEN** o administrador escolhe "Com sobrecarga"
- **THEN** só as pessoas acima da capacidade em alguma semana ficam na lista

#### Scenario: Próximas semanas pela tela
- **WHEN** o administrador avança uma semana
- **THEN** a tela pede a carga a partir da semana seguinte, e "Esta semana" volta à atual

### Requirement: Detalhe da carga pela tela

Escolher uma pessoa na Carga da equipe MUST abrir o detalhe na gaveta, pelo endereço (`?person=`), com cada semana: a carga, a capacidade, o estado, as ausências e as tarefas que pesam nela, com o projeto, a tarefa e as horas na semana, cada tarefa levando ao cronograma do projeto com o detalhe dela aberto (ADR-0081, ADR-0142).

#### Scenario: Detalhe da carga
- **WHEN** o administrador escolhe o Diego
- **THEN** a gaveta mostra, em cada semana, as tarefas do Diego com o projeto e as horas, e as ausências dele

### Requirement: Apontamentos da tarefa

O administrador e o gerente do projeto MUST ver os apontamentos de uma tarefa do cronograma, também a resumo e a subtarefa, de todas as pessoas: a pessoa pelo nome atual, o dia, as horas, a descrição e se quem pede pode corrigi-lo e apagá-lo, pelas regras do apontamento, do dia mais recente para o mais antigo, com o total. O marco, a fase e a tarefa de outro projeto MUST responder 404 (ADR-0128, ADR-0144).

#### Scenario: Apontamentos de uma tarefa
- **WHEN** a colaboradora aponta 8 horas e o desenvolvedor 5 horas numa tarefa, em dias diferentes, e a gerente do projeto pede os apontamentos dela
- **THEN** a API devolve os dois apontamentos, do mais recente para o mais antigo, com a pessoa, o dia, as horas e a descrição de cada um e o total de 13 horas, sem os apontamentos de outras tarefas

#### Scenario: Correção pelos apontamentos da tarefa
- **WHEN** a gerente do projeto e o administrador pedem os apontamentos de uma tarefa com um apontamento da gerente e um da colaboradora
- **THEN** para a gerente, só o apontamento dela pode ser corrigido; para o administrador, os dois

#### Scenario: Apontamentos fora de uma tarefa
- **WHEN** o administrador pede os apontamentos de um marco, de uma fase ou de uma tarefa de outro projeto
- **THEN** a API responde 404

### Requirement: Apontamentos da tarefa pela tela

Na seção "Horas" do detalhe de uma tarefa, "Ver apontamentos" MUST abrir os apontamentos da tarefa num diálogo com o nome da tarefa, agrupados por dia, do mais recente para o mais antigo, cada um com a pessoa, a descrição e as horas, o total da tarefa no rodapé e o "⋮" de corrigir e apagar só nos que a pessoa pode corrigir; corrigir ou apagar MUST atualizar as horas da tarefa (ADR-0081, ADR-0128, ADR-0144).

#### Scenario: Ver apontamentos pelo detalhe da tarefa
- **WHEN** a gerente do projeto escolhe "Ver apontamentos" na seção "Horas" de uma tarefa com apontamentos de duas pessoas
- **THEN** o diálogo mostra cada apontamento com a pessoa, o dia, as horas e a descrição e o total da tarefa, com o "⋮" só no apontamento dela
