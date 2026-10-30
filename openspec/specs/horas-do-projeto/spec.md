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

Apontar horas MUST ser do administrador, em qualquer projeto, e das pessoas do projeto, menos o leitor. Ver, corrigir e apagar as horas de outra pessoa MUST ser só do administrador. Quem não está no projeto MUST receber 404, o leitor 403, e o projeto concluído ou cancelado MUST responder 409 (ADR-0016, ADR-0040, ADR-0128).

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
