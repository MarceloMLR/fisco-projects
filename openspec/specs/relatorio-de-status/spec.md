# relatorio-de-status Specification

## Purpose
O relatório de status do projeto, para o cliente: o PowerPoint montado com os números do cronograma e dos chamados, os textos e os riscos que o gerente escolhe e, se ele quiser, as horas do projeto, guardado nos documentos do projeto (ADR-0165, ADR-0166).

## Requirements

### Requirement: Quem gera o relatório

O administrador e o gerente do projeto MUST gerar o relatório de status e ler o rascunho dele, em qualquer situação do projeto, inclusive encerrado. O líder técnico, o colaborador, o desenvolvedor e o leitor do projeto MUST receber 403, e quem não está no projeto, 404 (ADR-0040, ADR-0136, ADR-0165).

#### Scenario: Quem gera o relatório por papel
- **WHEN** cada papel do projeto ERP-FIN pede o rascunho e gera o relatório
- **THEN** o administrador e a gerente conseguem; o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403; e quem está fora do projeto recebe 404, sem nada gravado

#### Scenario: Relatório de projeto encerrado
- **WHEN** a gerente gera o relatório de um projeto concluído
- **THEN** o relatório é gerado e guardado, como num projeto em andamento

### Requirement: Rascunho do relatório

O rascunho MUST trazer os textos do último relatório do projeto, os chamados em aberto (nem resolvidos nem fechados) do projeto que podem entrar como riscos e, entre eles, os sugeridos: os riscos do último relatório que continuam em aberto, com a classificação e o ponto de atenção de então, e os que ameaçam marcos (ADR-0164), com a classificação Média. Sem relatório anterior, os textos vêm vazios (ADR-0165).

#### Scenario: Rascunho sem relatório anterior
- **WHEN** a gerente abre o rascunho do ERP-FIN pela primeira vez, e o chamado da rejeição 539 ameaça o marco Homologação
- **THEN** os quatro textos vêm vazios, e o chamado vem sugerido como risco, com a classificação Média e sem ponto de atenção

#### Scenario: Textos do último relatório
- **WHEN** a gerente gerou um relatório com o resumo "Cadastros validados" e um risco Alta, e depois abre o rascunho de novo
- **THEN** o resumo vem "Cadastros validados", e o risco vem sugerido com a classificação Alta e o ponto de atenção que ela escreveu

#### Scenario: Risco do último relatório já resolvido
- **WHEN** um chamado que entrou como risco no último relatório foi resolvido
- **THEN** o rascunho não o sugere nem o traz entre os chamados que podem entrar

### Requirement: Textos e riscos do relatório

Ao gerar, cada um dos quatro textos (o resumo do período, o foco atual, os próximos passos e o que precisa do cliente) MUST ter até 1.000 caracteres, e o ponto de atenção de cada risco, até 500. Os riscos MUST ser de 0 a 10 chamados em aberto do projeto, sem repetir, cada um com a classificação Alta, Média ou Baixa. Fora disso, MUST responder 400 no campo, sem gerar nada (ADR-0165).

#### Scenario: Chamado fechado como risco
- **WHEN** a gerente gera o relatório com um chamado que foi fechado depois que ela abriu o rascunho
- **THEN** a API responde 400 no campo dos riscos, e nada é guardado

#### Scenario: Riscos demais
- **WHEN** a gerente gera o relatório com 11 riscos
- **THEN** a API responde 400 no campo dos riscos

#### Scenario: Texto longo demais
- **WHEN** a gerente gera o relatório com um resumo do período de 1.001 caracteres
- **THEN** a API responde 400 no campo do resumo

### Requirement: O que vem do sistema

O relatório MUST trazer, pelas mesmas regras da visão geral e do cronograma, no dia de hoje da instalação, os seguintes dados (ADR-0073, ADR-0164, ADR-0165):
- O cliente e o logo dele, o projeto, a situação, o gerente e a data.
- O andamento total e de cada fase, contra o previsto.
- O fim do cronograma com o desvio contra a linha de base.
- Até 5 próximos marcos não atingidos e as fases com os períodos.
- Até 5 tarefas atrasadas, com o que empurram.
- Os chamados em aberto e os vencidos.

#### Scenario: Andamento da fase contra o previsto
- **WHEN** a fase Configuração tem uma tarefa que deveria estar inteira até hoje e outra que ainda não começou, as duas com a mesma duração, e está em 30%
- **THEN** o relatório mostra a fase com 30% de andamento e 50% previsto

#### Scenario: Próximos marcos no relatório
- **WHEN** o projeto tem 7 marcos não atingidos
- **THEN** o relatório mostra os 5 mais próximos pelo dia, cada um com o dia e os dias úteis que faltam ou o atraso

#### Scenario: Tarefas atrasadas além do limite
- **WHEN** o projeto tem 7 tarefas atrasadas
- **THEN** o relatório mostra as 5 mais atrasadas, com o que cada uma empurra, e "e mais 2 tarefas atrasadas"

#### Scenario: Projeto sem cronograma
- **WHEN** a gerente gera o relatório de um projeto sem tarefas nem marcos
- **THEN** o relatório é gerado, e as partes do andamento, das fases e dos marcos dizem "Sem cronograma"

### Requirement: Riscos no relatório

Cada risco MUST ter um slide próprio, na ordem escolhida, com os seguintes dados (ADR-0028, ADR-0164, ADR-0165):
- O número, o título, o status e a prioridade do chamado.
- A classificação e o ponto de atenção.
- O registro no fornecedor, pela regra da lista dos chamados, ou "Sem registro no fornecedor".
- Os marcos não atingidos que dependem dos itens ligados, ou "Nenhum marco depende deste chamado".

Sem riscos, não há slide de risco.

#### Scenario: Risco com chamado no fornecedor
- **WHEN** o risco é o chamado da rejeição 539, registrado no fornecedor como 77812 em análise e ligado a Executar testes, de que depende o marco Homologação
- **THEN** o slide do risco mostra o número, o título, o status, a prioridade, a classificação, o ponto de atenção, "77812 · Em análise" com o fornecedor e o marco Homologação com o dia

#### Scenario: Relatório sem riscos
- **WHEN** a gerente gera o relatório sem escolher nenhum risco
- **THEN** o relatório não tem slide de risco

### Requirement: Horas no relatório

Com "Mostrar horas", o relatório MUST ter o slide das horas do projeto (ADR-0128, ADR-0166):
- As horas contratadas, as adicionais, as apontadas, o saldo e o estado.
- As previstas e as apontadas de até 15 tarefas, na ordem do cronograma, com "e mais N tarefas" e os totais das que ficaram de fora.

As horas de cada pessoa nunca MUST entrar. Sem "Mostrar horas", que vem desmarcado, o relatório MUST não ter o slide das horas.

#### Scenario: Relatório com horas
- **WHEN** a gerente gera o relatório com "Mostrar horas" de um projeto com 200 horas contratadas, 20 adicionais e 180 apontadas
- **THEN** o relatório tem o slide das horas com 200, 20, 180, o saldo de 40 e o estado, e as horas de cada tarefa com horas

#### Scenario: Relatório sem horas
- **WHEN** a gerente gera o relatório sem "Mostrar horas"
- **THEN** o relatório não tem o slide das horas nem nenhum número de horas

#### Scenario: Horas sem as pessoas
- **WHEN** o administrador gera o relatório com "Mostrar horas" de um projeto em que Diego e Carla apontaram horas
- **THEN** o arquivo não traz o nome de nenhuma pessoa nas horas nem nenhum apontamento

#### Scenario: Projeto sem horas contratadas
- **WHEN** a gerente gera o relatório com "Mostrar horas" de um projeto sem horas contratadas
- **THEN** o slide das horas diz "Sem horas contratadas" e mostra só as apontadas

### Requirement: Modelo padrão do relatório

O relatório MUST sair num PowerPoint 16:9 que abre sem reparo, no modelo padrão do GerenWare, nesta ordem: "Status do projeto", "Cronograma", "Próximos passos", um slide por risco e "Horas do projeto", este só com "Mostrar horas". O modelo usa as cores da instalação ou as do GerenWare, o logo dela ou o nome escrito, e o logo do cliente ou o nome dele, sem nenhum nome de empresa no código (ADR-0003, ADR-0146, ADR-0165).

#### Scenario: Relatório abre sem reparo
- **WHEN** a gerente gera o relatório com dois riscos e "Mostrar horas"
- **THEN** o arquivo passa no validador do formato do PowerPoint e tem seis slides, na ordem do modelo

#### Scenario: Cores da instalação no relatório
- **WHEN** a instalação tem a cor principal `#0F766E`
- **THEN** o relatório usa `#0F766E` no lugar da cor de ação do GerenWare

#### Scenario: Instalação e cliente sem logo
- **WHEN** a instalação e o cliente do projeto não têm logo
- **THEN** o relatório mostra o nome da instalação e o nome do cliente escritos, no lugar dos logos

### Requirement: Campos do modelo

O modelo MUST receber os valores por campos entre chaves duplas (como `{{projeto.nome}}`), que mantêm o formato de quem os escreveu, mesmo quando o PowerPoint guardou o campo em pedaços. As listas MUST repetir a linha-modelo da tabela para cada item, e a tabela de uma lista vazia sai do slide. O slide de risco MUST se repetir para cada risco. Os logos e a barra do andamento MUST ser formas pelo nome (ADR-0165).

#### Scenario: Campo quebrado em pedaços
- **WHEN** o modelo tem `{{projeto.nome}}` guardado em três pedaços de texto, o primeiro em negrito
- **THEN** o relatório mostra o nome do projeto inteiro, em negrito

#### Scenario: Lista numa tabela
- **WHEN** o projeto tem quatro fases
- **THEN** a tabela das fases tem o cabeçalho e uma linha para cada fase, no formato da linha-modelo

#### Scenario: Lista vazia
- **WHEN** o projeto não tem tarefas atrasadas
- **THEN** a tabela das tarefas atrasadas sai do slide, e o slide diz "Nenhuma tarefa atrasada"

### Requirement: Relatório guardado nos documentos

O relatório gerado MUST ser guardado como documento do projeto na pasta dos relatórios, com o nome `{código} - Relatório de status - {aaaa-mm-dd}.pptx`. Gerar de novo no mesmo dia MUST guardar uma nova versão do mesmo documento. Os textos e os riscos MUST ficar guardados para o próximo rascunho. O documento entra na auditoria como qualquer documento (ADR-0031, ADR-0039, ADR-0165).

#### Scenario: Relatório guardado na pasta dos relatórios
- **WHEN** a gerente gera o relatório do ERP-FIN em 20/11/2026
- **THEN** a pasta "Relatórios de status" do projeto tem o documento "ERP-FIN - Relatório de status - 2026-11-20.pptx", enviado por ela

#### Scenario: Relatório gerado de novo no mesmo dia
- **WHEN** a gerente gera o relatório do ERP-FIN duas vezes em 20/11/2026
- **THEN** a pasta tem um documento só, na versão 2

### Requirement: Gerar o relatório pela tela

"Gerar relatório de status" MUST aparecer em "Ações do projeto" só para quem gera. Ele MUST abrir o diálogo com os seguintes campos (ADR-0133, ADR-0165, ADR-0166):
- Os quatro textos, preenchidos pelo rascunho.
- Os chamados que entram como riscos, com os sugeridos já escolhidos.
- A classificação e o ponto de atenção de cada risco.
- "Mostrar horas", desmarcado.

Ao gerar, MUST baixar o arquivo e avisar em que pasta ele foi guardado. O erro de um campo volta ao campo.

#### Scenario: Gerar relatório pela tela
- **WHEN** a gerente abre "Gerar relatório de status", escreve o resumo, marca "Mostrar horas" e clica em "Gerar relatório"
- **THEN** a tela manda os textos, os riscos e as horas, baixa o arquivo e avisa "Relatório guardado em Relatórios de status"

#### Scenario: Ação só para quem gera
- **WHEN** o líder técnico abre "Ações do projeto"
- **THEN** a ação "Gerar relatório de status" não aparece

#### Scenario: Riscos sugeridos na tela
- **WHEN** a gerente abre o diálogo, e o rascunho sugere o chamado da rejeição 539
- **THEN** o chamado já vem escolhido, com a classificação Média e o campo do ponto de atenção

#### Scenario: Erro de um campo na tela
- **WHEN** a API responde 400 no campo dos riscos
- **THEN** o diálogo continua aberto, com o erro no campo dos riscos, e nada é baixado
