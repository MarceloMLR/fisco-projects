# Registro de decisões (ADR)

Decisões do FiscoProjects no formato MADR (ADR-0001), em português do Brasil. Leia o ADR antes de implementar ou alterar a área correspondente.

Regras:

- Um ADR por decisão, numerado com quatro dígitos: `NNNN-titulo-em-kebab-case.md`.
- ADR aceito não muda de conteúdo. Mudar de ideia gera um ADR novo que diz o que substitui; o ADR antigo ganha a indicação no status e uma nota no item substituído.
- Quando um ADR cita `claude/...` (por exemplo, `claude/custos-estimados.md`), o arquivo correspondente está em `docs/` (por exemplo, `docs/custos-estimados.md`). As citações ficam como foram aprovadas.
- Os ADRs 0001 a 0073 foram decididos no discovery, de 13 a 17/10/2026, e movidos do projeto do Claude para este repositório no setup (ADR-0001). Os seguintes nascem durante a construção da v1.

## Fundamentos, acesso e entrada

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0001](0001-registrar-decisoes-em-adr.md) | Registrar as decisões do projeto em ADR (formato MADR) | aceita | 2026-10-13 |
| [0002](0002-projeto-pessoal-sem-recursos-do-empregador.md) | Tratar o FiscoProjects como projeto pessoal, sem recursos do empregador | aceita | 2026-10-13 |
| [0003](0003-uma-instalacao-por-empresa.md) | Uma instalação por empresa (single-tenant), sem nada da empresa fixo no código | aceita | 2026-10-13 |
| [0004](0004-microsoft-365-nativo-atras-de-interfaces.md) | Microsoft 365 nativo, com e-mail e arquivos atrás de interfaces | aceita | 2026-10-13 |
| [0005](0005-acesso-controlado-pelo-entra-com-criacao-no-primeiro-login.md) | Acesso controlado pelo Entra ID, com criação do usuário no primeiro login | aceita | 2026-10-13 |
| [0006](0006-papeis-gerenciados-no-banco-da-aplicacao.md) | Papéis globais e por projeto gerenciados no banco da aplicação | aceita | 2026-10-13 |
| [0007](0007-somente-usuarios-da-empresa-na-v1.md) | Somente usuários da empresa na v1, com contas de convidado recusadas | aceita | 2026-10-13 |
| [0008](0008-administracao-de-usuarios-na-v1.md) | Escopo da administração de usuários na v1, com desativação no sistema | aceita | 2026-10-13 |
| [0009](0009-pagina-de-entrada-propria-sessao-e-saida.md) | Página de entrada própria com login pelo Entra ID, sessão e saída | aceita (login simulado substituído pelo ADR-0078) | 2026-10-13 |

## Telas gerais, clientes e projetos

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0010](0010-tela-inicial-painel-meu-trabalho.md) | Tela inicial como painel pessoal "Meu trabalho" | aceita (seções, cards de projetos e atalhos substituídos pelo ADR-0070) | 2026-10-13 |
| [0011](0011-navegacao-hibrida.md) | Navegação híbrida, com barra lateral fixa e abas dentro do projeto | aceita (rodapé da barra lateral substituído pelo ADR-0066) | 2026-10-13 |
| [0012](0012-desktop-primeiro-utilizavel-no-celular.md) | Desktop em primeiro lugar, com uso no celular para as tarefas do dia a dia | aceita | 2026-10-13 |
| [0013](0013-idioma-portugues-com-arquivos-de-traducao.md) | Interface em português, com textos em arquivos de tradução desde a v1 | aceita | 2026-10-13 |
| [0014](0014-tema-claro-escuro-automatico-no-perfil.md) | Tema claro, escuro e automático, com a preferência salva no perfil | aceita | 2026-10-13 |
| [0015](0015-cadastro-de-clientes-com-cliente-opcional-no-projeto.md) | Cadastro de clientes, com cliente opcional no projeto | aceita | 2026-10-13 |
| [0016](0016-dados-e-status-do-projeto.md) | Dados e status do projeto, com datas e progresso calculados pelo cronograma | aceita (item "Excluir projeto" substituído pelo ADR-0041) | 2026-10-13 |
| [0017](0017-modelos-de-projeto-salvar-como-modelo.md) | Modelos de projeto criados a partir de um cronograma ("Salvar como modelo") | aceita | 2026-10-13 |

## Cronograma

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0018](0018-cronograma-em-dias-uteis-com-feriados.md) | Cronograma em dias úteis, com feriados nacionais calculados e feriados locais cadastrados | aceita | 2026-10-13 |
| [0019](0019-estrutura-do-cronograma-e-dependencias.md) | Cronograma em dois níveis, com dependências que empurram as tarefas seguintes | aceita (item "Subtarefas" substituído pelo ADR-0088) | 2026-10-13 |
| [0020](0020-progresso-e-atraso-das-tarefas.md) | Progresso das tarefas em percentual com atalhos, ponderado pela duração | aceita | 2026-10-13 |
| [0021](0021-recursos-do-gantt-na-v1.md) | Recursos do Gantt na v1, com linha de base, caminho crítico, desfazer e exportação | aceita (apresentação na tela definida pelo ADR-0071) | 2026-10-13 |

## Chamados

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0022](0022-chamados-sempre-ligados-a-um-projeto.md) | Chamados sempre ligados a um projeto, sem service desk geral | aceita | 2026-10-13 |
| [0023](0023-destinos-dos-chamados-times-do-projeto-e-filas.md) | Chamados para o time funcional ou técnico do projeto, ou para filas compartilhadas, com responsável opcional | aceita (filas, responsável na abertura e "Assumir chamado" substituídos pelo ADR-0068) | 2026-10-13 |
| [0024](0024-fluxo-de-status-dos-chamados-em-tabela.md) | Fluxo de status dos chamados em tabela, com o fluxo do MVP como padrão | aceita | 2026-10-13 |
| [0025](0025-prazos-dos-chamados-em-dias-uteis.md) | Prazos dos chamados em dias úteis, com pausa ao aguardar o solicitante e prazo de primeira resposta | aceita | 2026-10-14 |
| [0026](0026-mensagens-anexos-e-mencoes-nos-chamados.md) | Mensagens dos chamados visíveis a todos que veem o chamado, com anexos e menções | aceita (apresentação da conversa no ADR-0069, que substitui o "arrastando e soltando" da abertura) | 2026-10-14 |
| [0027](0027-trocar-responsavel-e-solicitante-do-chamado.md) | Troca do responsável e do solicitante dos chamados, um a um ou em lote | aceita (quem troca o responsável substituído pelo ADR-0068) | 2026-10-14 |
| [0028](0028-chamados-em-fornecedores.md) | Chamados em fornecedores registrados manualmente, com destaque para o que está parado | aceita | 2026-10-14 |
| [0029](0029-numero-abertura-e-lista-de-chamados.md) | Número único na instalação, abertura e lista de chamados | aceita (abertura sem responsável e sem fila, atalhos e visões substituídos pelo ADR-0068) | 2026-10-14 |

## Documentos, links, acessos e termos de aceite

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0030](0030-arquivos-no-sharepoint-na-pasta-de-cada-projeto.md) | Arquivos no SharePoint da empresa, na pasta de cada projeto, com as permissões já existentes | aceita | 2026-10-14 |
| [0031](0031-modulo-de-documentos-na-v1.md) | Módulo de documentos com pastas padrão configuráveis e status manual | aceita | 2026-10-14 |
| [0032](0032-links-do-projeto.md) | Links do projeto com categorias de uma lista configurável | aceita | 2026-10-14 |
| [0033](0033-senhas-dos-acessos-cifradas-com-data-protection.md) | Senhas dos acessos cifradas com o Data Protection, com proteção das chaves opcional por certificado | aceita | 2026-10-14 |
| [0034](0034-modulo-de-acessos-na-v1.md) | Módulo de acessos com senha mascarada, visibilidade pelo projeto e importação de planilha | aceita | 2026-10-14 |
| [0035](0035-termos-de-aceite-com-pdf-assinado.md) | Termos de aceite sempre por PDF assinado eletronicamente fora do sistema | aceita | 2026-10-14 |

## Avisos e auditoria

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0036](0036-envio-de-email-pelo-graph-com-caixa-compartilhada.md) | E-mails enviados pelo Microsoft Graph a partir de uma caixa compartilhada, com fila no banco | aceita | 2026-10-14 |
| [0037](0037-avisos-por-email-imediatos-e-resumo-diario.md) | Avisos por e-mail imediatos só para o que pede ação, com resumo diário para o restante | aceita | 2026-10-14 |
| [0038](0038-sino-de-notificacoes-e-teams-depois.md) | Sino de notificações no FiscoProjects, com avisos pelo Teams depois | aceita (posição do sino substituída pelo ADR-0066) | 2026-10-15 |
| [0039](0039-auditoria-automatica-de-alteracoes.md) | Auditoria automática de todas as alterações e dos eventos de segurança | aceita | 2026-10-15 |

## Papéis e permissões

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0040](0040-papeis-no-sistema-e-no-projeto.md) | Papéis no sistema e papéis em cada projeto | aceita (atendentes de fila substituídos pelo ADR-0068; gerente na criação ajustado pelo ADR-0072) | 2026-10-15 |
| [0041](0041-exclusao-de-projeto-pelo-administrador.md) | Exclusão definitiva de projeto, com todo o histórico, só pelo administrador | aceita | 2026-10-15 |
| [0042](0042-membros-do-projeto.md) | Membros do projeto geridos pelo gerente e pelo líder técnico, com ao menos um gerente | aceita | 2026-10-15 |
| [0043](0043-permissoes-no-projeto-e-no-cronograma.md) | Permissões no projeto e no cronograma | aceita | 2026-10-15 |
| [0044](0044-permissoes-nos-chamados.md) | Permissões nos chamados, com todos os membros do projeto vendo todos os chamados | aceita (filas, "Assumir" e troca do responsável por quem atende substituídos pelo ADR-0068) | 2026-10-15 |
| [0045](0045-permissoes-em-documentos-links-acessos-e-termos.md) | Permissões em documentos, links, acessos e termos de aceite | aceita | 2026-10-15 |
| [0046](0046-administracao-so-do-administrador.md) | Administração só do administrador, com cadastro de cliente na criação do projeto | aceita | 2026-10-15 |

## Arquitetura, infraestrutura e qualidade

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0047](0047-monolito-modular-com-clean-architecture.md) | Monolito modular com Clean Architecture em cada módulo | aceita (nomes dos módulos em português substituídos pelo ADR-0074) | 2026-10-15 |
| [0048](0048-azure-sql-provisionado-em-producao.md) | Azure SQL como banco, provisionado em produção e gratuito no desenvolvimento | aceita | 2026-10-15 |
| [0049](0049-frontend-react-typescript.md) | Frontend em React com TypeScript | aceita | 2026-10-15 |
| [0050](0050-mui-como-biblioteca-de-componentes.md) | Material UI (MUI) como biblioteca de componentes, só com pacotes gratuitos | aceita | 2026-10-15 |
| [0051](0051-gantt-syncfusion-isolado-com-gantt-proprio-depois.md) | Gantt com Syncfusion na v1, isolado num componente próprio, com Gantt próprio como evolução | aceita (nome do componente substituído pelo ADR-0074: `ScheduleGantt`) | 2026-10-15 |
| [0052](0052-hospedagem-no-app-service-linux.md) | Hospedagem no Azure App Service Linux, com Key Vault e identidade gerenciada | aceita | 2026-10-15 |
| [0053](0053-infraestrutura-como-codigo-com-terraform.md) | Infraestrutura como código com Terraform | aceita | 2026-10-15 |
| [0054](0054-autenticacao-bff-com-cookie.md) | Autenticação do React na API pelo padrão BFF, com cookie de sessão | aceita | 2026-10-15 |
| [0055](0055-ci-cd-com-github-actions.md) | CI/CD com GitHub Actions, preparado para migrar ao Azure DevOps | aceita (linha "Em todo pull request" substituída pelo ADR-0090) | 2026-10-15 |
| [0056](0056-estrategia-de-testes.md) | Pirâmide de testes ligada às specs, com matrizes de permissão 100% cobertas | aceita | 2026-10-15 |
| [0057](0057-monitoramento-com-application-insights.md) | Monitoramento com Application Insights e instrumentação OpenTelemetry | aceita | 2026-10-15 |
| [0058](0058-dados-pessoais-anonimizacao-e-retencao.md) | Dados pessoais com anonimização a pedido e prazos de retenção configuráveis (LGPD) | aceita | 2026-10-15 |
| [0059](0059-backup-e-recuperacao.md) | Backups automáticos do Azure SQL ajustados, mantidos no Brasil e com teste de restauração | aceita | 2026-10-15 |
| [0060](0060-versoes-lts-fixadas.md) | Versões LTS fixadas (.NET 10 e Node LTS), atualizadas só quando necessário | aceita | 2026-10-16 |
| [0061](0061-convencoes-da-api-e-do-codigo.md) | Convenções da API e do código: REST com OpenAPI, Problem Details, GUID v7 e sem dependências de licença comercial | aceita | 2026-10-16 |

## Identidade visual

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0062](0062-direcao-visual-profissional-e-sobria.md) | Direção visual profissional e sóbria | aceita | 2026-10-16 |
| [0063](0063-cores-azul-corporativo-com-marinho.md) | Cores: azul corporativo de ação, neutros frios e barra lateral marinho | aceita (barra lateral marinho substituída pelo ADR-0067) | 2026-10-16 |
| [0064](0064-tipografia-ibm-plex-sans.md) | Tipografia com IBM Plex Sans, servida pela própria aplicação | aceita | 2026-10-16 |
| [0065](0065-forma-sobria-com-status-por-icone-cor-e-texto.md) | Forma sóbria e confortável, com status por ícone, cor e texto | aceita (cantos substituídos pelo ADR-0067) | 2026-10-16 |
| [0066](0066-estrutura-das-telas.md) | Estrutura das telas, com barra lateral de altura total, cabeçalho invisível e conteúdo na altura da janela | aceita | 2026-10-17 |
| [0067](0067-identidade-visual-revisada.md) | Identidade visual revisada, com estrutura clara, marinho só na entrada, verde no concluído e cantos por tamanho | aceita | 2026-10-17 |

## Revisão do design (17/10/2026)

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0068](0068-times-dos-chamados-e-responsavel.md) | Chamados só para os times Funcional e Desenvolvimento, sem filas, com o responsável definido pela primeira resposta e trocado só pelos gestores | aceita | 2026-10-17 |
| [0069](0069-conversa-do-chamado.md) | Conversa do chamado com cores por papel, rolagem própria e interações antigas recolhidas | aceita (cor do solicitante substituída pelo ADR-0104) | 2026-10-17 |
| [0070](0070-meu-trabalho-fila-por-urgencia.md) | Meu trabalho como fila única por urgência, com a saúde dos projetos e os próximos marcos | aceita | 2026-10-17 |
| [0071](0071-cronograma-na-tela.md) | Cronograma na tela, com Linha do tempo que se ajusta ao espaço, Expandir e faixa de fases nos projetos | aceita | 2026-10-17 |
| [0072](0072-gerente-e-lider-tecnico-na-criacao.md) | Gerente e líder técnico definidos na criação do projeto e mostrados no cabeçalho | aceita | 2026-10-17 |
| [0073](0073-visao-geral-do-projeto.md) | Visão geral do projeto responde "está no prazo?" | aceita | 2026-10-17 |

## Construção da v1

| ADR | Título | Status | Data |
| --- | --- | --- | --- |
| [0074](0074-nomes-de-codigo-em-ingles-com-glossario.md) | Nomes de código em inglês, inclusive módulos e termos do negócio, com glossário português–inglês | aceita | 2026-10-17 |
| [0075](0075-projeto-compartilhado-de-persistencia.md) | Projeto compartilhado de persistência para os módulos | aceita | 2026-10-17 |
| [0076](0076-vite-e-tipos-gerados-com-openapi-typescript.md) | Frontend com Vite e tipos da API gerados pelo openapi-typescript | aceita | 2026-10-18 |
| [0077](0077-rotas-do-frontend-com-react-router.md) | Rotas do frontend com React Router | aceita | 2026-10-18 |
| [0078](0078-entrada-local-sem-tenant.md) | Entrada local por e-mail no desenvolvimento e nas demonstrações, sem tenant | aceita | 2026-10-18 |
| [0079](0079-modulo-de-arquivos-com-implementacao-local.md) | Arquivos num módulo próprio, com contrato único e implementação local em disco | aceita | 2026-10-18 |
| [0080](0080-leituras-da-api-com-tanstack-query.md) | Leituras da API no frontend com TanStack Query, guardadas só na memória da sessão | aceita | 2026-10-18 |
| [0081](0081-detalhes-em-gaveta-sobreposta.md) | Detalhes de um item da lista numa gaveta sobreposta, com o fundo escurecido | aceita | 2026-10-18 |
| [0082](0082-tokens-da-marca-derivados-da-cor-principal.md) | Tokens da marca derivados da cor principal em OKLCH, com a cor recusada sem contraste | aceita | 2026-10-18 |
| [0083](0083-pontos-facultativos-calculados-e-feriados-desconsiderados.md) | Pontos facultativos calculados pela portaria federal, contados como dia inteiro e escolhidos para todos os anos | aceita | 2026-10-19 |
| [0084](0084-referencias-na-auditoria-pelo-nome-atual.md) | Referências a outros itens na auditoria, guardadas pelo identificador e mostradas pelo nome atual | aceita | 2026-10-19 |
| [0085](0085-gerente-do-projeto-com-papel-no-sistema.md) | Gerente do projeto sempre com o papel no sistema Gerente de projeto ou Administrador | aceita | 2026-10-19 |
| [0086](0086-iniciar-pede-o-percentual-e-marco-atrasado.md) | "Iniciar" pede o percentual, e o marco não atingido depois da data fica atrasado | aceita | 2026-10-19 |
| [0087](0087-chave-do-syncfusion-pela-api.md) | Chave de licença do Syncfusion entregue pela API, a partir da configuração da instalação | aceita | 2026-10-19 |
| [0088](0088-subtarefas-na-v1.md) | Subtarefas na v1, em um nível, com a tarefa-resumo calculada e dependências só entre quem faz o trabalho | aceita | 2026-10-19 |
| [0089](0089-dados-de-demonstracao-pela-api.md) | Dados de demonstração carregados por um comando que usa a própria API | aceita | 2026-10-19 |
| [0090](0090-verificacao-do-pr-em-paralelo.md) | Verificação do PR em partes paralelas, com só as specs quando o commit muda apenas a documentação | aceita | 2026-10-19 |
| [0091](0091-desfazer-e-refazer-pelo-servidor.md) | Desfazer e refazer no cronograma pelo servidor, com o histórico de cada pessoa | aceita | 2026-10-19 |
| [0092](0092-modulo-documents-com-links-e-termos.md) | Documentos, links e termos de aceite no módulo `Documents`, e os acessos no `Credentials` | aceita | 2026-10-20 |
| [0093](0093-nova-versao-de-aprovado-e-pasta-de-termos.md) | Nova versão de um documento aprovado volta para revisão, e a pasta de termos recebe só os termos assinados | aceita | 2026-10-20 |
| [0094](0094-leitura-de-planilhas-com-open-xml-sdk.md) | Leitura das planilhas de acessos com o Open XML SDK e o CSV por um leitor próprio | aceita | 2026-10-20 |
| [0095](0095-pedidos-de-aceite.md) | Pedido de aceite cancelado sai da lista, recusa encerra o pedido e termo anexado sempre de uma pessoa do projeto | aceita | 2026-10-20 |
| [0096](0096-meu-trabalho-montado-no-servidor.md) | Meu trabalho montado no servidor por um módulo próprio, com a janela de 7 dias e o atraso do item mais atrasado | aceita | 2026-10-20 |
| [0097](0097-lider-tecnico-como-papel-no-sistema.md) | Líder técnico também como papel no sistema, exigido para liderar tecnicamente um projeto | aceita | 2026-10-20 |
| [0098](0098-iniciar-atendimento-assume-o-chamado.md) | "Iniciar atendimento" torna responsável quem atende o chamado sem responsável | aceita | 2026-10-20 |
| [0099](0099-relatorio-da-auditoria-em-excel.md) | Relatório da auditoria em Excel, montado na tela e registrado na auditoria | aceita | 2026-10-21 |
| [0100](0100-cor-secundaria-e-logo-para-o-tema-escuro.md) | Cor secundária da instalação na barra lateral, nos destaques de apoio e no painel da entrada, com logo para o tema escuro | aceita (cor secundária substituída pelo ADR-0103) | 2026-10-21 |
| [0101](0101-cores-das-mensagens-dos-chamados.md) | Cores das mensagens dos chamados configuráveis por papel, com os tons calculados | substituída pelo ADR-0103 | 2026-10-21 |
| [0102](0102-cliente-como-responsavel-de-tarefas.md) | O cliente do projeto como responsável de tarefas do cronograma | aceita | 2026-10-21 |
| [0103](0103-so-a-cor-principal-na-marca.md) | Só a cor principal na marca da instalação, sem cor secundária nem cores das mensagens | aceita | 2026-10-21 |
| [0104](0104-solicitante-em-rosa-na-conversa.md) | Solicitante em rosa na conversa do chamado | aceita | 2026-10-21 |
| [0105](0105-voltar-do-navegador.md) | O que entra no histórico do navegador | aceita | 2026-10-21 |
| [0106](0106-primeira-carga-mais-rapida.md) | Primeira abertura das telas mais rápida: aquecimento da API, leituras antes e o código da Linha do tempo carregado antes | aceita | 2026-10-21 |
| [0107](0107-mencionar-ao-digitar.md) | Mencionar ao digitar "@" no editor dos chamados | aceita | 2026-10-21 |
| [0108](0108-logo-na-aba-do-navegador.md) | Logo da instalação na aba do navegador | aceita | 2026-10-21 |
| [0109](0109-confirmacoes-no-alto-da-tela.md) | Confirmações no alto da tela, no meio | aceita | 2026-10-21 |
| [0110](0110-anonimizacao-na-pratica.md) | Como a anonimização e o prazo da auditoria funcionam | aceita | 2026-10-21 |
| [0111](0111-exclusao-de-projeto-na-pratica.md) | Como a exclusão de um projeto funciona | aceita | 2026-10-21 |
| [0112](0112-monitoramento-na-pratica.md) | Como a aplicação envia o monitoramento | aceita | 2026-10-21 |
| [0113](0113-demonstracao-no-codespaces.md) | Demonstração no GitHub Codespaces | aceita | 2026-10-22 |
