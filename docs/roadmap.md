# Roadmap da v1

As 50 changes do OpenSpec que constroem a v1, em 10 grupos, na ordem em que são feitas. Aprovado por Marcelo em 17/10/2026. Não há dependência circular entre grupos: cada grupo usa só o que os anteriores entregaram.

## Como funciona

- Cada change tem um ID (`Fundação-1`, `Acesso-2`...) e um nome no OpenSpec (`estrutura-do-backend`...).
- Fluxo de cada change: branch nova a partir da `main` atualizada → proposta e avaliação contra os ADRs e o design técnico e visual → implementação com testes → PR → aprovação de Marcelo → archive na mesma branch → merge → volta para a `main` e apaga a branch. Detalhes no `CLAUDE.md`, seção 9.
- Correção de uma change já arquivada ganha um subnúmero (`Fundação-2.1`), só quando necessário.
- Toda change com tela entrega as versões de computador e celular, nos temas claro e escuro. Os dados fictícios de demonstração crescem a cada change: cada uma acrescenta os dela ao plano de `web/scripts/demo/plan.ts`, carregado por `npm run demo:data` (ADR-0089).
- Changes marcadas com * usam telas de cadastro da Administração ainda não desenhadas; a tela é desenhada antes da change.
- Status: Planejada, Em andamento, PR aberto, Concluída, Adiada (com o motivo).
- Em 18/10/2026, Marcelo adiou a Fundação-6 e a Fundação-7 até existir a assinatura do Azure, e o login pelo Entra foi para o grupo 9 (M365-1): sem tenant, desenvolvimento e demonstrações usam a entrada local (ADR-0078).
- Em 19/10/2026, Marcelo pediu a Fundação-3.2 (`verificacao-mais-rapida`), para a verificação do PR ficar mais rápida sem deixar de existir (ADR-0090).
- Em 19/10/2026, Marcelo trouxe as subtarefas para a v1 (ADR-0088): a Cronograma-4 (`subtarefas`) entrou logo depois da Linha do tempo, para a linha de base, o caminho crítico, o desfazer e a exportação já nascerem com elas, e as changes seguintes do grupo foram renumeradas (as propostas arquivadas citam a numeração antiga).
- Em 20/10/2026, depois da Síntese, Marcelo pediu dez ajustes, feitos em sequência antes do grupo 10: mais dados de demonstração, Sair voltando ao Início, Líder técnico como papel no sistema, "Iniciar atendimento" assumindo o chamado, baixar anexos, chamado no fornecedor num diálogo, relatório da auditoria em Excel, personalização da marca, cores das mensagens e o cliente como responsável de tarefas.
- Em 21/10/2026, Marcelo pediu mais ajustes, feitos em sequência: só a cor principal na marca, o solicitante em rosa na conversa, o espaço do ícone nos botões, o Voltar do navegador e a primeira carga mais rápida; e depois a menção ao digitar "@", a descrição do chamado em rosa, o logo na aba do navegador e os avisos no topo.
- Em 21/10/2026, depois da Operação-4, Marcelo viu Meu perfil quebrado no celular, e a correção entrou como Acesso-5.1 (`perfil-no-celular`).
- Em 22/10/2026, com o computador desligado, Marcelo pediu um jeito fácil de demonstrar de outro computador: a Cronograma-3.4 (`demonstracao-no-codespaces`) põe a demonstração no GitHub Codespaces (ADR-0113).
- Em 25/10/2026, Marcelo criou a assinatura do Azure (avaliação gratuita) para testar o sistema de ponta a ponta: a Fundação-6 e a Fundação-7 voltam para a fila, seguidas do grupo M365, com a avaliação do Microsoft 365 só para o SharePoint e o e-mail (ADR-0114).

## Marcos

| Grupo | Changes | Concluídas |
| --- | --- | --- |
| 1. Fundação | 13 | 13 |
| 2. Acesso e administração | 18 | 18 |
| 3. Projetos | 4 | 4 |
| 4. Cronograma | 14 | 14 |
| 5. Chamados | 13 | 13 |
| 6. Avisos | 3 | 3 |
| 7. Documentos, links, acessos e termos | 5 | 5 |
| 8. Telas de síntese | 2 | 2 |
| 9. Microsoft 365 real | 4 | 0 |
| 10. Governança e operação | 6 | 4 |
| **Total** | **82** | **76** |

## 1. Fundação

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Fundação-1 | `estrutura-do-backend` | Solução .NET 10, host da API, estrutura de módulos, testes de arquitetura, `/health`, OpenAPI e Problem Details | 0047, 0060, 0061, 0074 | Concluída em 17/10/2026 ([#3](https://github.com/MarceloMLR/fisco-projects/pull/3), [#4](https://github.com/MarceloMLR/fisco-projects/pull/4)) |
| Fundação-2 | `banco-de-dados` | EF Core com SQL Server, um schema por módulo, migrações, SQL Server no Docker local e nos testes | 0048, 0056, 0075 | Concluída em 18/10/2026 ([#5](https://github.com/MarceloMLR/fisco-projects/pull/5)) |
| Fundação-3 | `pipeline-de-pr` | GitHub Actions com build, testes, formatação e `openspec validate`, exigido na regra "Protect main" | 0055 | Concluída em 18/10/2026 ([#6](https://github.com/MarceloMLR/fisco-projects/pull/6)) |
| Fundação-3.1 | `sem-verificacao-ortografica` | Retira a verificação de ortografia (cspell) do PR e a extensão do VS Code, a pedido de Marcelo | 0055 | Concluída em 18/10/2026 ([#12](https://github.com/MarceloMLR/fisco-projects/pull/12)) |
| Fundação-3.2 | `verificacao-mais-rapida` | Verificação do PR em jobs paralelos, com cache dos pacotes e só as specs no commit que muda apenas documentação, a pedido de Marcelo | 0055, 0090 | Concluída em 19/10/2026 ([#31](https://github.com/MarceloMLR/fisco-projects/pull/31)) |
| Fundação-4 | `estrutura-do-frontend` | React com TypeScript estrito, lint, testes, tipos gerados da API, textos em arquivos de tradução, servido pelo host | 0013, 0049, 0061, 0076 | Concluída em 18/10/2026 ([#7](https://github.com/MarceloMLR/fisco-projects/pull/7)) |
| Fundação-5 | `tema-e-estrutura-das-telas` | Tokens do design system no tema do MUI, IBM Plex, barra lateral, cabeçalho invisível, celular | 0012, 0050, 0062 a 0067, 0077 | Concluída em 18/10/2026 ([#8](https://github.com/MarceloMLR/fisco-projects/pull/8)) |
| Fundação-5.1 | `espaco-do-icone-nos-botoes` | Ícone dos botões a 8px do texto, como no design system, a pedido de Marcelo | 0062, 0065 | Concluída em 21/10/2026 ([#67](https://github.com/MarceloMLR/fisco-projects/pull/67)) |
| Fundação-5.2 | `voltar-do-navegador` | Um Voltar do navegador volta uma página: links nas tabelas, gavetas e formulários sem entradas a mais, a pedido de Marcelo | 0066, 0081, 0105 | Concluída em 21/10/2026 ([#68](https://github.com/MarceloMLR/fisco-projects/pull/68)) |
| Fundação-5.3 | `avisos-no-topo` | As confirmações curtas no alto, no meio da tela, a pedido de Marcelo | 0066, 0109 | Concluída em 21/10/2026 ([#73](https://github.com/MarceloMLR/fisco-projects/pull/73)) |
| Fundação-6 | `infraestrutura-azure` | Terraform com App Service, Azure SQL, Key Vault, Application Insights (com o teste de disponibilidade e os alertas do ADR-0112), backups do Azure SQL (ADR-0059) e registro do aplicativo no Entra | 0052, 0053, 0114 | Concluída em 25/10/2026 ([#83](https://github.com/MarceloMLR/fisco-projects/pull/83)) |
| Fundação-7 | `deploy-desenvolvimento` | Publicação automática no ambiente de desenvolvimento a cada merge; verificação de licenças dos pacotes do NuGet no PR | 0055, 0061, 0115, 0116 | Concluída em 25/10/2026 ([#85](https://github.com/MarceloMLR/fisco-projects/pull/85)) |
| Fundação-8 | `primeira-carga-mais-rapida` | Primeira abertura das telas mais rápida: aquecimento da API ao subir, leituras antes na tela e o código da Linha do tempo carregado antes, a pedido de Marcelo | 0047, 0080, 0106 | Concluída em 21/10/2026 ([#69](https://github.com/MarceloMLR/fisco-projects/pull/69)) |

## 2. Acesso e administração

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Acesso-1 | `entrada-e-sessao` | Página de entrada, entrada local por e-mail (desenvolvimento e demonstração), usuário no primeiro login, sessão de 8 h, Sair | 0005, 0009, 0054, 0078 | Concluída em 18/10/2026 ([#9](https://github.com/MarceloMLR/fisco-projects/pull/9), [#10](https://github.com/MarceloMLR/fisco-projects/pull/10)) |
| Acesso-1.1 | `sair-volta-ao-inicio` | Depois de Sair, a próxima entrada vai para o Início; a sessão expirada volta à página só para a mesma pessoa, a pedido de Marcelo | 0054, 0078 | Concluída em 20/10/2026 ([#56](https://github.com/MarceloMLR/fisco-projects/pull/56)) |
| Acesso-2 | `usuarios-e-papeis` | Papéis no sistema, primeiro e último administrador, desativar e reativar, tela Usuários | 0006, 0008, 0040 | Concluída em 18/10/2026 ([#11](https://github.com/MarceloMLR/fisco-projects/pull/11)) |
| Acesso-2.1 | `listas-mais-rapidas` | Listas sem espera aparente: leituras guardadas na sessão (TanStack Query), indicador só depois de 300 ms, menus que fecham na hora e tabela sem redesenho inútil, a pedido de Marcelo | 0049, 0050, 0080 | Concluída em 18/10/2026 ([#15](https://github.com/MarceloMLR/fisco-projects/pull/15)) |
| Acesso-2.2 | `lider-tecnico-no-sistema` | Líder técnico como papel no sistema: só quem o tem lidera tecnicamente um projeto, e é o sugerido nas telas, a pedido de Marcelo | 0040, 0072 | Concluída em 20/10/2026 ([#57](https://github.com/MarceloMLR/fisco-projects/pull/57)) |
| Acesso-3 | `auditoria` | Registro automático das alterações e dos eventos de segurança, tela de Auditoria | 0039 | Concluída em 18/10/2026 ([#13](https://github.com/MarceloMLR/fisco-projects/pull/13)) |
| Acesso-3.1 | `detalhes-em-gaveta` | Detalhes da Auditoria numa gaveta sobreposta com o fundo escurecido, padrão de todas as telas de lista com detalhe, e filtro de pessoa sem cortar o nome, a pedido de Marcelo | 0066, 0081 | Concluída em 18/10/2026 ([#16](https://github.com/MarceloMLR/fisco-projects/pull/16)) |
| Acesso-3.2 | `relatorio-da-auditoria` | Relatório da auditoria em Excel com os filtros da tela, só do administrador e registrado na própria auditoria, a pedido de Marcelo | 0039, 0046, 0058 | Concluída em 21/10/2026 ([#61](https://github.com/MarceloMLR/fisco-projects/pull/61)) |
| Acesso-4 | `armazenamento-de-arquivos` | Interface de arquivos com a implementação local | 0004 | Concluída em 18/10/2026 ([#14](https://github.com/MarceloMLR/fisco-projects/pull/14)) |
| Acesso-5 | `perfil-e-tema` | Tema no perfil e tela Meu perfil (separada da configuração da instalação a pedido de Marcelo) | 0014, 0066 | Concluída em 18/10/2026 ([#17](https://github.com/MarceloMLR/fisco-projects/pull/17)) |
| Acesso-5.1 | `perfil-no-celular` | Meu perfil no celular sem a Conta por cima das Preferências e dos Avisos por e-mail, a pedido de Marcelo | 0066 | Concluída em 22/10/2026 ([#79](https://github.com/MarceloMLR/fisco-projects/pull/79)) |
| Acesso-6 | `configuracao-da-instalacao` | Tela Configurações gerais com o nome e o logo da instalação, exibidos na barra lateral, na entrada e na aba (separada do fuso e da cor a pedido de Marcelo) | 0003, 0009, 0030, 0046, 0066 | Concluída em 19/10/2026 ([#18](https://github.com/MarceloMLR/fisco-projects/pull/18)) |
| Acesso-7 | `fuso-da-instalacao` | Fuso horário nas Configurações gerais; datas e filtros de período no fuso da instalação | 0013 | Concluída em 19/10/2026 ([#19](https://github.com/MarceloMLR/fisco-projects/pull/19)) |
| Acesso-8 | `cor-da-instalacao` | Cor principal nas Configurações gerais, com os tokens da marca derivados nos dois temas e a checagem de contraste | 0003, 0050, 0063, 0067, 0082 | Concluída em 19/10/2026 ([#20](https://github.com/MarceloMLR/fisco-projects/pull/20)) |
| Acesso-8.1 | `personalizacao-da-marca` | Seletor de cor, logo do tema escuro e cor secundária na barra lateral, nos destaques e no painel da entrada, a pedido de Marcelo | 0063, 0082 | Concluída em 21/10/2026 ([#62](https://github.com/MarceloMLR/fisco-projects/pull/62)) |
| Acesso-8.2 | `cores-das-mensagens` | Cor de cada papel na conversa dos chamados configurável, a pedido de Marcelo | 0069, 0082 | Concluída em 21/10/2026 ([#63](https://github.com/MarceloMLR/fisco-projects/pull/63)) |
| Acesso-8.3 | `so-cor-principal` | Só a cor principal na marca, sem cor secundária nem cores das mensagens, mantendo o logo escuro e o seletor de cores, a pedido de Marcelo | 0100, 0101, 0103 | Concluída em 21/10/2026 ([#65](https://github.com/MarceloMLR/fisco-projects/pull/65)) |
| Acesso-8.4 | `logo-na-aba` | O logo da instalação na aba do navegador, e o selo na cor principal sem logo, a pedido de Marcelo | 0003, 0082, 0108 | Concluída em 21/10/2026 ([#72](https://github.com/MarceloMLR/fisco-projects/pull/72)) |

## 3. Projetos

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Projetos-1 | `calendario`* | Dias úteis, feriados nacionais calculados, pontos facultativos, feriados locais | 0018, 0083 | Concluída em 19/10/2026 ([#21](https://github.com/MarceloMLR/fisco-projects/pull/21)) |
| Projetos-2 | `clientes` | Cadastro de clientes, inclusive o cliente novo criado pelo gerente na criação do projeto | 0015, 0046 | Concluída em 19/10/2026 ([#22](https://github.com/MarceloMLR/fisco-projects/pull/22)) |
| Projetos-3 | `projetos` | Criação com gerente e líder técnico, lista, status, somente leitura quando encerrado | 0016, 0043, 0072 | Concluída em 19/10/2026 ([#23](https://github.com/MarceloMLR/fisco-projects/pull/23)) |
| Projetos-4 | `membros-do-projeto` | Incluir pessoas pelo diretório (versão simulada), papéis no projeto, último gerente protegido | 0040, 0042 | Concluída em 19/10/2026 ([#24](https://github.com/MarceloMLR/fisco-projects/pull/24)) |

## 4. Cronograma

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Cronograma-1 | `cronograma` | Fases, tarefas e marcos em dias úteis, dependências que empurram, progresso ponderado, atraso, vista em lista | 0019, 0020 | Concluída em 19/10/2026 ([#25](https://github.com/MarceloMLR/fisco-projects/pull/25)) |
| Cronograma-2 | `progresso-das-tarefas` | Iniciar e Concluir, quem atualiza o quê, marco atingido | 0020, 0043 | Concluída em 19/10/2026 ([#26](https://github.com/MarceloMLR/fisco-projects/pull/26)) |
| Cronograma-2.1 | `cliente-como-responsavel` | O cliente do projeto como responsável de tarefas, a pedido de Marcelo | 0007, 0019, 0020 | Concluída em 21/10/2026 ([#64](https://github.com/MarceloMLR/fisco-projects/pull/64)) |
| Cronograma-3 | `linha-do-tempo` | Gantt do Syncfusion isolado em `ScheduleGantt`, arrastar, zoom, ajuste ao espaço, Expandir | 0051, 0071 | Concluída em 19/10/2026 ([#27](https://github.com/MarceloMLR/fisco-projects/pull/27)) |
| Cronograma-3.1 | `dados-de-demonstracao` | Comando que carrega os dados fictícios de demonstração pela API, com datas relativas ao dia, a pedido de Marcelo | 0078, 0089 | Concluída em 19/10/2026 ([#28](https://github.com/MarceloMLR/fisco-projects/pull/28)) |
| Cronograma-3.2 | `mais-dados-de-demonstracao` | Chamados em todos os projetos e status, com conversas longas, anexos, prioridade, quem acompanha e fornecedores, a pedido de Marcelo | 0089 | Concluída em 20/10/2026 ([#55](https://github.com/MarceloMLR/fisco-projects/pull/55)) |
| Cronograma-3.3 | `demonstracao-mais-rica` | Mais clientes e projetos na demonstração, em todos os status, de uma segunda gerente e a partir de um modelo, com chamados e os demais itens, e a carga só do que falta, a pedido de Marcelo | 0017, 0043, 0089 | Concluída em 21/10/2026 ([#74](https://github.com/MarceloMLR/fisco-projects/pull/74)) |
| Cronograma-3.4 | `demonstracao-no-codespaces` | Demonstração no GitHub Codespaces, preparada e carregada sozinha, com o link para compartilhar, a pedido de Marcelo | 0089, 0113 | Concluída em 25/10/2026 ([#81](https://github.com/MarceloMLR/fisco-projects/pull/81)) |
| Cronograma-4 | `subtarefas` | Subtarefas em um nível, tarefa-resumo calculada, dependências entre quem faz o trabalho | 0019, 0088 | Concluída em 19/10/2026 ([#29](https://github.com/MarceloMLR/fisco-projects/pull/29)) |
| Cronograma-4.1 | `ajustes-das-telas` | Ajustes pedidos por Marcelo: barra de rolagem extra nas abas do projeto, dias úteis de atraso de tarefas e marcos, reabrir a mesma linha da Linha do tempo, motivo exato do CNPJ inválido, subtarefas mais claras na Lista e no detalhe e o endereço das telas no fim da carga de demonstração | 0015, 0020, 0065, 0071, 0086, 0088, 0089 | Concluída em 19/10/2026 ([#30](https://github.com/MarceloMLR/fisco-projects/pull/30)) |
| Cronograma-5 | `linha-de-base-e-caminho-critico` | Linha de base e caminho crítico | 0021, 0043, 0067, 0071 | Concluída em 20/10/2026 ([#32](https://github.com/MarceloMLR/fisco-projects/pull/32)) |
| Cronograma-6 | `desfazer-no-cronograma` | Desfazer as próprias ações, inclusive em cadeia | 0021, 0071, 0091 | Concluída em 20/10/2026 ([#33](https://github.com/MarceloMLR/fisco-projects/pull/33)) |
| Cronograma-7 | `exportar-cronograma` | PDF e XML do MS Project | 0021, 0043, 0051, 0071 | Concluída em 20/10/2026 ([#34](https://github.com/MarceloMLR/fisco-projects/pull/34)) |
| Cronograma-8 | `modelos-de-projeto`* | "Salvar como modelo" e criar projeto a partir de um modelo | 0017, 0043, 0046 | Concluída em 20/10/2026 ([#35](https://github.com/MarceloMLR/fisco-projects/pull/35)) |

## 5. Chamados

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Chamados-1 | `abertura-e-lista-de-chamados` | Número único, os dois times, filtros, quem vê o quê | 0022, 0029, 0044, 0068 | Concluída em 20/10/2026 ([#36](https://github.com/MarceloMLR/fisco-projects/pull/36)) |
| Chamados-2 | `conversa-do-chamado` | Mensagens, anexos, menções, cores por papel | 0026, 0069 | Concluída em 20/10/2026 ([#37](https://github.com/MarceloMLR/fisco-projects/pull/37)) |
| Chamados-2.1 | `baixar-anexos` | Baixar cada anexo e todos de uma vez, a pedido de Marcelo | 0026 | Concluída em 20/10/2026 ([#59](https://github.com/MarceloMLR/fisco-projects/pull/59)) |
| Chamados-2.2 | `solicitante-em-rosa` | Solicitante em rosa na conversa do chamado, e quem não tem papel no cinza neutro, a pedido de Marcelo | 0069, 0104 | Concluída em 21/10/2026 ([#66](https://github.com/MarceloMLR/fisco-projects/pull/66)) |
| Chamados-2.3 | `mencionar-ao-digitar` | Mencionar ao digitar "@" e parte do nome no editor dos chamados, a pedido de Marcelo | 0026, 0107 | Concluída em 21/10/2026 ([#70](https://github.com/MarceloMLR/fisco-projects/pull/70)) |
| Chamados-2.4 | `descricao-em-rosa` | A descrição do chamado, a primeira mensagem do solicitante, na cor dele, a pedido de Marcelo | 0104 | Concluída em 21/10/2026 ([#71](https://github.com/MarceloMLR/fisco-projects/pull/71)) |
| Chamados-3 | `fluxo-de-status` | Fluxo em tabela, responsável pela primeira resposta, fechamento automático | 0024, 0068 | Concluída em 20/10/2026 ([#38](https://github.com/MarceloMLR/fisco-projects/pull/38)) |
| Chamados-3.1 | `iniciar-atendimento-assume` | "Iniciar atendimento" torna responsável quem atende o chamado sem responsável, a pedido de Marcelo | 0024, 0068 | Concluída em 20/10/2026 ([#58](https://github.com/MarceloMLR/fisco-projects/pull/58)) |
| Chamados-4 | `prazos-dos-chamados`* | Dias úteis por prioridade, pausa, primeira resposta | 0025 | Concluída em 20/10/2026 ([#39](https://github.com/MarceloMLR/fisco-projects/pull/39)) |
| Chamados-5 | `trocas-e-transferencias` | Troca de responsável e solicitante, em lote, transferência entre times, pendências de quem sai | 0027, 0042 | Concluída em 20/10/2026 ([#40](https://github.com/MarceloMLR/fisco-projects/pull/40)) |
| Chamados-6 | `chamados-em-fornecedores`* | Cadastro de fornecedores e registros no chamado | 0028 | Concluída em 20/10/2026 ([#41](https://github.com/MarceloMLR/fisco-projects/pull/41)) |
| Chamados-6.1 | `fornecedor-em-dialogo` | Chamado no fornecedor resumido no painel e com os detalhes e o histórico num diálogo, a pedido de Marcelo | 0028 | Concluída em 21/10/2026 ([#60](https://github.com/MarceloMLR/fisco-projects/pull/60)) |
| Chamados-7 | `encerramento-do-projeto` | Concluir só com chamados fechados; cancelar fecha os abertos | 0043 | Concluída em 20/10/2026 ([#43](https://github.com/MarceloMLR/fisco-projects/pull/43)) |

## 6. Avisos

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Avisos-1 | `fila-de-emails` | Fila no banco e envio em segundo plano, com implementação local | 0036 | Concluída em 20/10/2026 ([#45](https://github.com/MarceloMLR/fisco-projects/pull/45)) |
| Avisos-2 | `avisos-por-email` | E-mails imediatos e resumo diário | 0037 | Concluída em 20/10/2026 ([#46](https://github.com/MarceloMLR/fisco-projects/pull/46)) |
| Avisos-3 | `sino-de-notificacoes` | Sino com contador e painel de notificações | 0038 | Concluída em 20/10/2026 ([#47](https://github.com/MarceloMLR/fisco-projects/pull/47)) |

## 7. Documentos, links, acessos e termos

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Documentos-1 | `documentos`* | Pastas padrão, versões, status, vínculo com o cronograma | 0031, 0045, 0092, 0093 | Concluída em 20/10/2026 ([#48](https://github.com/MarceloMLR/fisco-projects/pull/48)) |
| Documentos-2 | `links`* | Links do projeto por categoria | 0032 | Concluída em 20/10/2026 ([#49](https://github.com/MarceloMLR/fisco-projects/pull/49)) |
| Documentos-3 | `acessos` | Senha cifrada e mascarada | 0033, 0034 | Concluída em 20/10/2026 ([#50](https://github.com/MarceloMLR/fisco-projects/pull/50)) |
| Documentos-4 | `importacao-de-acessos` | Importação da planilha | 0034, 0094 | Concluída em 20/10/2026 ([#51](https://github.com/MarceloMLR/fisco-projects/pull/51)) |
| Documentos-5 | `termos-de-aceite` | Pedido, resposta com PDF assinado, recusa | 0035, 0045, 0095 | Concluída em 20/10/2026 ([#52](https://github.com/MarceloMLR/fisco-projects/pull/52)) |

## 8. Telas de síntese

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Síntese-1 | `meu-trabalho` | Fila por urgência, meus projetos e próximos marcos | 0070, 0096 | Concluída em 20/10/2026 ([#53](https://github.com/MarceloMLR/fisco-projects/pull/53)) |
| Síntese-2 | `visao-geral-do-projeto` | "Está no prazo?" | 0073 | Concluída em 20/10/2026 ([#54](https://github.com/MarceloMLR/fisco-projects/pull/54)) |

## 9. Microsoft 365 real

Até aqui, tudo roda com as implementações locais, sem depender de tenant licenciado.

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| M365-1 | `entrada-pelo-entra` | Login pelo Entra ID, recusa de convidados e recusa sem atribuição no Entra | 0004, 0007, 0009, 0117 | PR aberto ([#88](https://github.com/MarceloMLR/fisco-projects/pull/88)) |
| M365-2 | `diretorio-microsoft` | Busca de pessoas pelo Graph | 0005 | Planejada |
| M365-3 | `arquivos-no-sharepoint` | Arquivos no SharePoint, na pasta de cada projeto | 0030 | Planejada |
| M365-4 | `emails-pelo-graph` | Envio pela caixa compartilhada | 0036 | Planejada |

## 10. Governança e operação

| ID | Change | Objetivo | ADRs | Status |
| --- | --- | --- | --- | --- |
| Operação-1 | `lgpd-anonimizacao-e-retencao` | Anonimização a pedido e limpezas por prazo | 0058, 0110 | Concluída em 21/10/2026 ([#75](https://github.com/MarceloMLR/fisco-projects/pull/75)) |
| Operação-2 | `exclusao-de-projeto` | Exclusão definitiva pelo administrador (no fim, porque apaga dados de todos os módulos) | 0041, 0111 | Concluída em 21/10/2026 ([#76](https://github.com/MarceloMLR/fisco-projects/pull/76)) |
| Operação-3 | `monitoramento-e-alertas` | OpenTelemetry, Application Insights, saúde e as métricas dos alertas; o recurso, o teste de disponibilidade e os alertas ficam na Fundação-6 | 0057, 0112 | Concluída em 21/10/2026 ([#77](https://github.com/MarceloMLR/fisco-projects/pull/77)) |
| Operação-4 | `backup-e-restauracao` | Roteiro de restauração e teste trimestral; as definições dos backups ficam na Fundação-6 | 0059 | Concluída em 21/10/2026 ([#78](https://github.com/MarceloMLR/fisco-projects/pull/78)) |
| Operação-5 | `deploy-de-producao` | Workflow manual de produção | 0055 | Planejada |
| Operação-6 | `guia-de-instalacao` | `docs/instalacao.md` completo, com os scripts do Exchange e do SharePoint | 0030, 0036, 0053 | Planejada |
