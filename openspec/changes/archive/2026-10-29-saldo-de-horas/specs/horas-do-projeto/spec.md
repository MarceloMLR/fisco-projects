## ADDED Requirements

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
