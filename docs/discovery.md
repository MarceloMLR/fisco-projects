# FiscoProjects: discovery

Referência textual do discovery, feito de 13 a 17/10/2026 por Marcelo Ribeiro com o Claude. Cada decisão tem um ADR em [`docs/adr/`](adr/README.md); este documento resume e aponta para eles. Em caso de divergência, vale o ADR mais recente.

Outros documentos do discovery:

- [Apresentação para o time](apresentacao.html), não técnica.
- [Roadmap da v1](roadmap.md), com as changes do OpenSpec e o status de cada uma.
- [Requisitos de instalação](instalacao.md).
- [Custos estimados no Azure](custos-estimados.md).
- [Justificativa do design](design-justificativa.md).

## Pitch

Projetos, cronogramas e chamados da equipe em um só lugar. O FiscoProjects reúne, para cada projeto, o cronograma, os chamados, os documentos, os links, os acessos a portais e os termos de aceite, apoiado no Microsoft 365 que a empresa já usa.

## Problema

Hoje, numa equipe de implantação e projetos, cada parte do trabalho vive num lugar diferente:

- o cronograma fica no MS Project ou em planilhas, e clientes e equipes precisam recebê-lo nesse formato (ADR-0021);
- os acessos a portais de clientes ficam num Excel com os acessos de todos os clientes (ADR-0033, ADR-0034);
- os documentos ficam soltos no SharePoint (ADR-0030);
- os chamados aos times e aos fornecedores precisam de prazo, responsável e histórico dentro do contexto do projeto (ADR-0022, ADR-0028).

Sem um lugar único, fica difícil saber o que está atrasado, quem precisa responder o quê e se o projeto está no prazo.

Um MVP de demonstração serviu de referência para os fluxos (cronograma com Gantt, chamados com fluxo de status, documentos, acessos e termos de aceite). A v1 nasce neste repositório, com as decisões deste discovery, como projeto pessoal que pode ser vendido a empresas (ADR-0002).

## Escopo

### O que faz (v1)

| Capacidade | O que cobre | Organização no código | ADRs |
| --- | --- | --- | --- |
| Entrada e acesso | Página de entrada própria, login pelo Entra ID, criação do usuário no primeiro login, recusa de convidados e desativados, sessão de 8 h, "Sair" só do FiscoProjects | Módulo de identidade e host da API (BFF com cookie) | 0004, 0005, 0007, 0009, 0054 |
| Meu trabalho | Fila única por urgência (chamados, tarefas, aceites), saúde dos projetos e próximos marcos | Tela do frontend que consome os contratos dos módulos | 0010, 0070 |
| Projetos e clientes | Código, nome, cliente opcional, status, datas e progresso calculados, gerente e líder técnico na criação, exclusão só pelo administrador | Módulo Projetos | 0015, 0016, 0041, 0072, 0073 |
| Cronograma | Dois níveis (fases, tarefas e marcos), dias úteis e feriados, dependências que empurram, linha de base, caminho crítico, desfazer, exportação em PDF e XML do MS Project, modelos de projeto | Módulo Projetos (cálculo no servidor) e componente `CronogramaGantt` no frontend | 0017, 0018, 0019, 0020, 0021, 0051, 0071 |
| Chamados | Sempre ligados a um projeto, para os times Funcional e Desenvolvimento, fluxo de status em tabela, prazos em dias úteis, conversa com anexos e menções, troca de responsável e solicitante, chamados em fornecedores | Módulo Chamados | 0022 a 0029, 0068, 0069 |
| Documentos | Pastas padrão configuráveis, versões, status manual, vínculo com o cronograma, visualização no navegador | Módulo Documentos, arquivos no SharePoint atrás de `IFileStorage` | 0030, 0031 |
| Links | Links do projeto por categoria | Módulo Documentos ou Projetos (definir na change) | 0032 |
| Acessos | Login e senha de portais por projeto, senha cifrada e mascarada, importação de planilha | Módulo Acessos | 0033, 0034 |
| Termos de aceite | Pedido de aceite de um marco, resposta com PDF assinado fora do sistema, recusa com motivo | Módulo Documentos ou Projetos (definir na change) | 0035 |
| Membros e permissões | Papéis no sistema e no projeto, membros geridos pelo gerente e pelo líder técnico, matrizes de permissão | Módulo Projetos e regra de autorização compartilhada | 0006, 0040, 0042 a 0046 |
| Avisos | E-mails imediatos para o que pede ação, resumo diário, sino de notificações | Módulo Notificações, e-mail atrás de `IEmailSender` com fila no banco | 0036, 0037, 0038 |
| Auditoria e dados pessoais | Registro automático de todas as alterações e eventos de segurança, anonimização a pedido, retenção | Módulo Auditoria, ponto único na camada de dados | 0039, 0058 |
| Administração | Usuários, auditoria, configurações e cadastros (clientes, fornecedores, feriados, prazos, pastas, categorias, modelos) | Módulo Administração | 0008, 0011, 0046 |
| Perfil e aparência | Tema claro, escuro e automático salvo no perfil, preferências de e-mail | Módulo de identidade | 0014, 0037 |

A organização exata em módulos é fechada na change de setup do backend, seguindo o ADR-0047: cada módulo tem `Domain`, `Application`, `Infrastructure` e `Endpoints`, e expõe aos outros só o projeto `Contracts`.

### O que não faz

Fora do produto:

- Service desk geral: catálogo de serviços, base de conhecimento, chamado sem projeto (ADR-0022).
- Integração automática com o sistema de chamados de fornecedores (ADR-0028).
- Aceite eletrônico por clique; todo aceite é por PDF assinado (ADR-0035).
- Apontamento de horas, orçamento, custos e campos personalizados (ADR-0016, ADR-0020).
- Dependências entre projetos diferentes (ADR-0019).
- Gerar arquivo `.mpp` nativo (ADR-0021).
- Senha da conta Microsoft digitada no FiscoProjects (ADR-0009).
- Barra lateral recolhível, personalização do painel, gerador de senhas, exportação de acessos (ADR-0011, ADR-0010, ADR-0034).
- Aplicativo instalável e notificações push no celular (ADR-0012).

Fora da v1, previstos para depois:

- Pessoas de fora da empresa (clientes e fornecedores) no sistema (ADR-0007).
- Filas compartilhadas, como a TI da empresa, e atendentes de fora do projeto (ADR-0068).
- Importar arquivos do MS Project, subtarefas, outros tipos de dependência e intervalo entre tarefas (ADR-0019, ADR-0021).
- Avisos pelo Teams e atualização em tempo real (ADR-0038).
- Indicadores gerenciais e gráficos de evolução (ADR-0010, ADR-0073).
- Tradução para inglês (ADR-0013).
- Busca global (ADR-0011, ADR-0066).
- Modo sem Microsoft 365 (contas locais, SMTP, arquivos em disco ou blob).
- Gantt próprio no lugar do Syncfusion (ADR-0051).

## Critérios de sucesso

Definidos nos ADRs:

| Critério | Meta | ADR |
| --- | --- | --- |
| Custo de operação para 5 a 6 pessoas | cerca de R$ 170 por mês (R$ 107 na configuração econômica), preços de 15/10/2026 | 0048, 0052 |
| Perda máxima de dados | alguns minutos | 0059 |
| Tempo para voltar ao ar | até 4 horas | 0059 |
| Teste de restauração | a cada 3 meses | 0059 |
| Permissões testadas | 100% das células das matrizes, permitido e negado | 0056 |
| Cobertura de `Domain` e `Application` | pelo menos 80%, como indicador | 0056 |
| Cenários das specs | cada cenário com ao menos um teste rastreável pelo nome | 0056 |
| Acessibilidade | contraste AA nos dois temas | 0063 |
| Licenças | nenhuma dependência com licença comercial ou copyleft forte sem ADR | 0061 |

Metas de negócio, adotadas como padrão inicial em 17/10/2026 e revistas depois de 3 meses de uso real:

| Meta | Alvo | Como medir |
| --- | --- | --- |
| Projetos ativos com cronograma no FiscoProjects | 100% em até 3 meses de uso | Projetos em andamento com cronograma, sobre o total em andamento |
| Planilha de acessos substituída | 100% dos acessos dos projetos ativos no sistema em até 1 mês | Acessos importados (ADR-0034) e a planilha deixada de lado |
| Uso semanal | Pelo menos 80% das pessoas ativas entram ao menos uma vez por semana | Eventos de entrada na auditoria (ADR-0039) |
| Primeira resposta dos chamados no prazo | Pelo menos 90% | Prazo de primeira resposta da prioridade (ADR-0025) |
| Chamados resolvidos no prazo | Pelo menos 85% | Prazo de resolução da prioridade, descontadas as pausas (ADR-0025) |
| Marcos no prazo | Pelo menos 80% atingidos até a data da linha de base | Marcos atingidos e linha de base (ADR-0021) |

Os alvos de chamados seguem a faixa usual de mercado para cumprimento de prazo, de 75% a 90% dos chamados resolvidos no prazo. Na v1 não há painel de indicadores (ADR-0010): os números saem de consultas ao banco e à auditoria, e o painel vem depois.

## Stakeholders e aprovações

| Papel | Quem | Aprova |
| --- | --- | --- |
| Dono do produto e desenvolvedor | Marcelo Ribeiro | Todos os ADRs, o design system e as telas, cada proposta do OpenSpec antes de implementar, os PRs e as publicações em produção |
| Administrador do sistema (por instalação) | Definido em cada empresa (o primeiro pelo `oid` na configuração) | Usuários e papéis no sistema, cadastros e configurações, anonimização, exclusão de projetos |
| Gerente de projeto | Por projeto | Cronograma, status do projeto, termos de aceite, aprovação de documentos, equipe |
| Líder técnico | Por projeto | Responsável dos chamados, equipe técnica, aprovação de documentos |
| Administrador do Microsoft 365 da empresa | TI de cada empresa | Quem entra (atribuição no Entra), acesso direto ao SharePoint, caixa de envio no Exchange |
| Advogado | A contratar | Contrato de trabalho antes de oferecer o código, validade das assinaturas dos termos, prazos de retenção da LGPD |

Hoje há um único desenvolvedor, que revisa e aprova o próprio código. A separação entre quem escreve e quem aprova fica **A DEFINIR** quando houver uma segunda pessoa no projeto.

## Personas

Papéis no sistema (ADR-0040):

- **Administrador**: configura a instalação, cuida de usuários e cadastros e vê todos os projetos.
- **Gerente de projeto (no sistema)**: pode criar projetos.
- **Usuário**: papel padrão no primeiro login.

Papéis em cada projeto (ADR-0040):

- **Gerente de projeto**: cuida do cronograma, dos termos de aceite e da equipe, e vê e atua em todos os chamados do projeto.
- **Líder técnico**: o mesmo que o gerente, menos alterar o cronograma e os termos; faz parte do time de Desenvolvimento.
- **Colaborador**: time Funcional; atende chamados funcionais, abre chamados, atualiza as próprias tarefas e envia documentos.
- **Desenvolvedor**: time de Desenvolvimento; o mesmo que o colaborador, nos chamados técnicos.
- **Leitor**: acompanha tudo no projeto sem alterar nada; não abre nem atende chamados.

Uso esperado: 5 a 6 pessoas por instalação, não simultâneas, em horário comercial, principalmente no computador e no celular para responder chamados, dar aceites e consultar documentos (ADR-0012).

## Glossário

| Termo | Definição |
| --- | --- |
| Instalação | Uma cópia do FiscoProjects para uma única empresa, com banco, configuração e marca próprios (ADR-0003) |
| Projeto | Unidade de trabalho com código curto (ex.: ERP-FIN), cliente opcional, cronograma, chamados e documentos (ADR-0016) |
| Cronograma | Fases, tarefas e marcos do projeto, em dias úteis; datas e progresso do projeto vêm dele (ADR-0016, ADR-0019) |
| Linha do tempo | Nome, na tela, da visualização gráfica do cronograma; a palavra "Gantt" não aparece para o usuário (ADR-0071) |
| Marco | Ponto no tempo, sem duração, atingido ou não; pode exigir termo de aceite (ADR-0019, ADR-0035) |
| Linha de base | Cópia das datas aprovadas, para medir quanto o projeto escorregou (ADR-0021) |
| Caminho crítico | Tarefas sem folga, cujo atraso atrasa o projeto (ADR-0021) |
| Dia útil | Dia de trabalho da instalação que não é feriado nem ponto facultativo considerado (ADR-0018) |
| Chamado | Pedido dentro de um projeto, com número único na instalação (#103), prioridade, prazo e fluxo de status (ADR-0022, ADR-0029) |
| Time Funcional / Desenvolvimento | Destinos do chamado: colaboradores, ou líder técnico e desenvolvedores do projeto (ADR-0068) |
| Solicitante | Quem abriu o chamado, ou quem ficou no lugar dele (ADR-0027) |
| Responsável | Quem atende o chamado; definido pela primeira resposta de alguém do time, se ainda não houver (ADR-0068) |
| Gestores | Gerente e líder técnico do projeto (ADR-0044) |
| Chamado de fornecedor | Registro, dentro do nosso chamado, do chamado aberto num fornecedor (ADR-0028) |
| Termo de aceite | Pedido de aceite de um marco, respondido com PDF assinado fora do sistema (ADR-0035) |
| Acesso | Login e senha de um portal, guardados por projeto com a senha cifrada (ADR-0034) |
| Modelo de projeto | Estrutura de cronograma salva a partir de um projeto, para criar outros (ADR-0017) |
| Meu trabalho | Tela inicial: o que pede ação da pessoa, por urgência (ADR-0070) |

## Stack e arquitetura

| Camada | Escolha | ADR |
| --- | --- | --- |
| Backend | ASP.NET Core no .NET 10 (LTS), EF Core com SQL Server | 0047, 0060 |
| Arquitetura | Monolito modular, com Clean Architecture dentro de cada módulo | 0047 |
| Frontend | React com TypeScript estrito, MUI (só pacotes gratuitos), React Hook Form com Zod, Tiptap, react-i18next | 0049, 0050 |
| Cronograma na tela | Syncfusion React Gantt (licença comunitária), isolado no componente `CronogramaGantt` | 0051 |
| Autenticação | BFF com cookie de sessão; tokens da Microsoft nunca chegam ao navegador | 0054 |
| API | REST com OpenAPI, Problem Details, GUID v7; tipos do frontend gerados do contrato | 0061 |
| Banco | Azure SQL: provisionado em produção, oferta gratuita no desenvolvimento | 0048 |
| Hospedagem | Azure App Service Linux em Brazil South, Key Vault e identidade gerenciada | 0052 |
| Infraestrutura | Terraform (`azurerm` e `azuread`), compatível com OpenTofu | 0053 |
| CI/CD | GitHub Actions chamando scripts do repositório, pronto para migrar ao Azure DevOps | 0055 |
| Monitoramento | OpenTelemetry exportando para o Application Insights | 0057 |
| Testes | xUnit, AwesomeAssertions, NSubstitute, Testcontainers, NetArchTest ou ArchUnitNET, Vitest, Testing Library, MSW, Playwright | 0056 |
| Visual | Direção sóbria, azul corporativo, IBM Plex Sans servida pela aplicação, status por ícone, cor e texto | 0062 a 0067 |

Regra de dependência:

- Dentro do módulo, as dependências apontam para o centro: `Domain` não depende de nada externo; `Application` depende só de `Domain`; `Infrastructure` e `Endpoints` dependem das camadas internas.
- Entre módulos, só pelo projeto `Contracts` (interfaces, dados e eventos de integração) ou por eventos internos. Cada módulo é dono das próprias tabelas, num schema próprio.
- Microsoft 365 sempre atrás de interfaces (`IEmailSender`, `IFileStorage`), com implementações locais para desenvolvimento e testes (ADR-0004).
- O servidor é a fonte da verdade das datas do cronograma; o Gantt só exibe e envia alterações (ADR-0051).
- Testes de arquitetura no CI garantem essas regras.

## Fluxo de alto nível

A pessoa da empresa entra com a conta Microsoft e cai em Meu trabalho, que mostra o que pede ação dela. Dentro de cada projeto, o gerente mantém o cronograma; marcos pedem termos de aceite, e o termo assinado marca o marco como atingido. Qualquer membro abre chamados para o time Funcional ou de Desenvolvimento; a primeira resposta do time define o responsável, que resolve, e o solicitante confirma. Documentos, links e acessos ficam no projeto. Tudo o que pede ação gera aviso por e-mail e no sino.

## Governança e compliance

### Regulatório e legal

- LGPD: dados pessoais com anonimização a pedido e prazos de retenção configuráveis (ADR-0058). Os prazos padrão precisam ser validados por cada empresa com a assessoria jurídica.
- Lei do Software (Lei nº 9.609/1998): o FiscoProjects é projeto pessoal, sem recursos do empregador (ADR-0002). Antes de oferecer o código a qualquer empresa, um advogado analisa o contrato de trabalho.
- Validade jurídica das assinaturas dos termos de aceite: a confirmar com advogado (ADR-0035).

### Dados sensíveis e retenção

| Dado | Tratamento | ADR |
| --- | --- | --- |
| Nome, e-mail e vínculo com o Entra | Vêm do Entra a cada login; anonimizados a pedido em todo o sistema, inclusive na auditoria | 0005, 0008, 0058 |
| Senhas dos acessos a portais | Cifradas com o Data Protection, mascaradas na tela, nunca por e-mail, nunca na auditoria nem em logs | 0033, 0034, 0039, 0057 |
| Auditoria | 5 anos (configurável), limpeza automática mensal | 0058 |
| Conteúdo dos e-mails enviados | Apagado da fila depois de 30 dias | 0058 |
| Notificações do sino | 90 dias (configurável) | 0038 |
| Termos de aceite | Visíveis só ao gerente, aos administradores e a quem assinou; termo assinado nunca é excluído | 0031, 0045 |
| Dados no Brasil | Aplicação, banco e backups em Brazil South | 0052, 0059 |

### Auditoria e rastreabilidade

- Registro automático, num ponto único da camada de dados, de toda criação, alteração e exclusão, com quem, quando e antes e depois (ADR-0039).
- Eventos de segurança: entrada, recusa de acesso, troca de papel, desativação e reativação, mudança de configuração (ADR-0039).
- Registros somente leitura; tela de Auditoria só para administradores (ADR-0039).
- Linha do tempo de cada chamado com mudanças de status, trocas e transferências (ADR-0024, ADR-0027).
- Exclusão de projeto registra quem excluiu e o resumo do que foi apagado (ADR-0041).

### Segregação de funções

- Só o administrador acessa a Administração, exclui projetos e anonimiza pessoas (ADR-0041, ADR-0046, ADR-0058).
- O sistema impede remover o último administrador ativo e o último gerente de um projeto (ADR-0006, ADR-0042).
- O líder técnico não altera o cronograma nem os termos de aceite; só gestores trocam o responsável de um chamado (ADR-0040, ADR-0068).
- Publicação em produção só por workflow manual, depois de revisar o plano do Terraform (ADR-0055).
- `main` recebe código só por PR com as verificações aprovadas (ADR-0055). Desde 17/10/2026 o repositório é público e a regra "Protect main" do GitHub está ativa, sem exceções, nem para administradores: só entra código por PR, e a `main` não pode ser apagada nem reescrita. Quando o CI existir, as verificações passam a ser exigidas na mesma regra.

### Gestão de segredos

- Nenhum segredo no repositório, no Terraform ou no arquivo de estado (ADR-0053).
- Segredos no Key Vault: certificado do aplicativo no Entra e chave de licença do Syncfusion (ADR-0051, ADR-0052).
- Aplicação acessa banco e Key Vault pela identidade gerenciada, sem senha (ADR-0052).
- GitHub Actions acessa o Azure por identidade federada (OIDC), sem segredo guardado (ADR-0055).
- Mail.Send não é concedido no Entra ID; o envio é restrito à caixa compartilhada pelo RBAC do Exchange Online (ADR-0036).
- Cifrar as chaves do Data Protection com certificado é recomendado em produção (ADR-0033).

## Fontes oficiais de dados

| Dado | Fonte da verdade |
| --- | --- |
| Quem pode entrar, nome e e-mail das pessoas | Entra ID da empresa (ADR-0005, ADR-0008) |
| Papéis e permissões | Banco do FiscoProjects, nunca App Roles do Entra (ADR-0006) |
| Arquivos | SharePoint da empresa, no local configurado; o banco registra o que o sistema enviou (ADR-0030) |
| Feriados nacionais | Calculados pelo próprio sistema (datas fixas e móveis pela Páscoa); feriados locais cadastrados (ADR-0018) |
| Datas do cronograma | Calculadas no servidor (ADR-0051) |
| Regras de negócio | ADRs em `docs/adr/` e, a partir da primeira change, as specs em `openspec/specs/` |
| Valores visuais (cores, tipografia, espaço, cantos, sombras) | [Design system FiscoProjects no Claude Design](https://claude.ai/artifact/CymvW4j1dqVrX3KVN13Jt7), aprovado em 17/10/2026 |
| Telas | [Canvas de telas no Claude Design](https://claude.ai/artifact/WoMTFZKZXEpXzmPCCU4xfV), com 51 telas aprovadas em 17/10/2026 |

Regras:

- Nunca usar dados reais: nada de nomes de clientes, da empresa, chamados ou acessos reais (ADR-0002).
- Dados de demonstração e de testes são fictícios, existem só no seed e nos testes, e nunca ficam fixos no código (ADR-0002, ADR-0003).
- Nenhum nome de empresa, cliente, fornecedor, pasta padrão, modelo ou prazo fixo no código: tudo é configuração ou dado cadastrado (ADR-0003).
- Regra de negócio que não está num ADR nem numa spec não é inventada: vira pergunta ou proposta.

Números para conferência:

- 73 ADRs (0001 a 0073).
- Status do chamado: 6 (Aberto, Em triagem, Em andamento, Aguardando solicitante, Resolvido, Fechado), com 10 transições no fluxo padrão (ADR-0024).
- Prioridades: 4, com prazos de resolução padrão de 10, 5, 2 e 1 dias úteis e primeira resposta em 1 dia útil (ADR-0025).
- Times de destino: 2, Funcional e Desenvolvimento (ADR-0068).
- Papéis: 3 no sistema e 5 no projeto (ADR-0040).
- Status do projeto: 5; status do documento: 4; status do chamado no fornecedor: 5 (ADR-0016, ADR-0031, ADR-0028).
- Pastas padrão: 6; categorias de links: 5 (ADR-0031, ADR-0032).
- Sessão: 8 horas sem uso; anexos: até 15 MB; fechamento automático: 5 dias úteis; fornecedor parado: 5 dias úteis (ADR-0009, ADR-0026, ADR-0024, ADR-0028).

## Documentação de referência

Ler antes de implementar a área:

| Área | Ler |
| --- | --- |
| Qualquer mudança | `CLAUDE.md`, `openspec/config.yaml`, ADR-0001 a 0004, ADR-0047, ADR-0061 |
| Entrada, sessão e usuários | ADR-0005 a 0009, 0054 |
| Telas, navegação e visual | ADR-0011 a 0014, 0062 a 0067, design system e canvas de telas |
| Projetos e clientes | ADR-0015, 0016, 0041, 0072, 0073 |
| Cronograma | ADR-0017 a 0021, 0051, 0071 |
| Chamados | ADR-0022 a 0029, 0044, 0068, 0069 |
| Documentos, links, acessos e termos | ADR-0030 a 0035, 0045 |
| Avisos | ADR-0036 a 0038 |
| Papéis e permissões | ADR-0040, 0042 a 0046 |
| Auditoria e LGPD | ADR-0039, 0058 |
| Infraestrutura e operação | ADR-0048, 0052, 0053, 0055, 0057, 0059, 0060, [requisitos de instalação](instalacao.md), [custos](custos-estimados.md) |
| Testes | ADR-0056 |

## Integrações externas

| Integração | Uso | ADR |
| --- | --- | --- |
| Microsoft Entra ID | Login (OpenID Connect), atribuição de quem entra | 0004, 0005, 0009, 0054 |
| Microsoft Graph: usuários | Buscar pessoas no diretório para incluir em projetos | 0005 |
| Microsoft Graph: SharePoint | Guardar documentos, anexos e logos (Sites.Selected, só escrita) | 0030 |
| Microsoft Graph: e-mail | Envio a partir de uma caixa compartilhada, com fila no banco | 0036 |
| Exchange Online | RBAC para aplicações, restringindo o envio à caixa | 0036 |
| Azure | App Service, Azure SQL, Key Vault, Application Insights, armazenamento do estado do Terraform | 0048, 0052, 0053, 0057 |
| GitHub Actions | Build, testes e deploy, com OIDC no Azure | 0055 |
| Syncfusion | Componente de Gantt, com chave de licença fora do repositório | 0051 |
| MS Project | Exportação do cronograma no formato XML (arquivo, sem integração) | 0021 |
| Microsoft Teams | Avisos, depois da v1 | 0038 |

## Modelo de deploy e promoção

| Ambiente | Como chega | Quem executa |
| --- | --- | --- |
| Local | Docker com SQL Server; e-mail e arquivos nas implementações locais; tenant Microsoft pessoal, nunca o da empresa | Marcelo |
| Pull request | Build, testes (unitários, arquitetura, integração com SQL Server em contêiner), lint, formatação, tipos, dependências vulneráveis, validação do OpenSpec, `terraform fmt`, `validate` e `plan` | GitHub Actions |
| Desenvolvimento e demonstração | Automático no merge na `main`: `terraform apply`, migrações e publicação; depois, testes de ponta a ponta | GitHub Actions |
| Produção | Workflow manual por versão: revisão do plano, `terraform apply`, migrações e publicação | Marcelo |

Variação por cliente: uma instalação por empresa (single-tenant). O que muda entre empresas é **configuração e dados** (nome, logo, cor principal, fuso, local dos arquivos, caixa de envio, cadastros), nunca código (ADR-0003).

## Definições sem ADR

| Item | Definição |
| --- | --- |
| Nome do produto | FiscoProjects (substitui o provisório "Marco") |
| Repositório e pasta | `fisco-projects`, em `C:\workspaces\projects\fisco-projects` |
| Namespaces .NET | `FiscoProjects.*` |
| O que vira ADR | Decisões com alternativas. Números de versão e convenções de estilo ficam nos arquivos do projeto (`global.json`, `.nvmrc`, `.editorconfig`, `CLAUDE.md`); valores exatos do visual ficam no design system |

## Pendências

- Telas ainda não desenhadas: cadastros da Administração (Fornecedores, Calendário e feriados, Prazos dos chamados, Pastas padrão, Categorias de links e Modelos de projeto). Seguem o padrão das telas de Usuários e Clientes, ou são desenhadas antes da change correspondente.
- Confirmar na documentação do Syncfusion o ajuste da linha do tempo ao espaço disponível antes de implementar o cronograma (ADR-0071).
- Divergência a corrigir: o ADR-0065 cita "Material Symbols", mas o design system usa Material Icons no estilo Outlined (variantes `…Outlined` do `@mui/icons-material`).
- Levar os tokens do design system para o repositório na change do tema do MUI.
- Exigir as verificações do CI na regra "Protect main" quando os workflows existirem (ADR-0055).
- Reconsultar os preços do Azure antes de apresentar os custos.
- Em caso de venda: migrar repositório e pipelines para o Azure DevOps da compradora (ADR-0055).
