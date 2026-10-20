# FiscoProjects

Regras deste projeto para o agente de código. As preferências gerais de Marcelo (CLAUDE.md global) continuam valendo; aqui ficam só as do FiscoProjects. Responda sempre em português do Brasil.

## 1. O que é

O FiscoProjects reúne projetos, cronogramas e chamados da equipe em um só lugar, apoiado no Microsoft 365 da empresa. Cada empresa tem a própria instalação (single-tenant, ADR-0003). É um projeto pessoal de Marcelo Ribeiro, que pode ser vendido a empresas (ADR-0002).

Na v1, o sistema faz: entrada pelo Entra ID, Meu trabalho, projetos e clientes, cronograma, chamados (inclusive os abertos em fornecedores), documentos, links, acessos a portais, termos de aceite, membros e permissões, avisos por e-mail e no sino, auditoria e administração.

Fronteiras que não se cruzam sem um ADR novo:

- Todo chamado pertence a um projeto; não existe service desk geral (ADR-0022).
- Só pessoas da empresa entram na v1; convidados são recusados (ADR-0007).
- Só os times Funcional e Desenvolvimento recebem chamados; não há filas (ADR-0068).
- Todo aceite é por PDF assinado fora do sistema (ADR-0035).
- Sem horas trabalhadas, orçamento, custos ou campos personalizados (ADR-0016, ADR-0020).

O escopo completo, com o que fica para depois, está em `docs/discovery.md`.

## 2. Arquitetura e regra de dependência

Monolito modular com Clean Architecture dentro de cada módulo, num único deploy (ADR-0047).

- Dentro do módulo: `Domain` não depende de nada externo (nem EF Core, Graph ou ASP.NET Core); `Application` depende só de `Domain`; `Infrastructure` e `Endpoints` dependem das camadas internas.
- Entre módulos: só o projeto `Contracts` do outro módulo (interfaces, dados e eventos de integração) ou eventos internos. Nunca referenciar o interior de outro módulo.
- Dados: cada módulo é dono das próprias tabelas, num schema próprio. Nenhum módulo lê ou altera tabelas de outro; consultas que cruzam módulos passam pelos contratos.
- Microsoft 365 sempre atrás de interfaces (`IEmailSender`, `IFileStorage`), com implementações locais para desenvolvimento e testes (ADR-0004).
- O servidor é a fonte da verdade das datas do cronograma (dias úteis, feriados, dependências, caminho crítico); o Gantt só exibe e envia alterações (ADR-0051).
- Testes de arquitetura no CI quebram quando essas regras são violadas.

## 3. Mapa do repositório

```
src/
  FiscoProjects.Api/                  host: API, autenticação BFF, serve o build do React
  FiscoProjects.SharedKernel/         o mínimo comum (tipos base, eventos)
  FiscoProjects.Persistence/          contexto base do EF Core, migrações e saúde do banco (ADR-0075)
  Modules/
    <Modulo>/
      FiscoProjects.<Modulo>/         Domain/ Application/ Infrastructure/ Endpoints/
      FiscoProjects.<Modulo>.Contracts/
web/                                  React + TypeScript
tests/                                testes do backend (unitários, integração, arquitetura, permissões)
infra/terraform/                      módulos e configuração por instalação e ambiente
infra/scripts/                        o que o Terraform não cobre (Exchange, SharePoint)
scripts/ci/                           verificações do PR, chamadas pelo GitHub Actions (.github/workflows)
docs/adr/                             decisões (MADR), leitura obrigatória da área antes de mudar
docs/                                 discovery, apresentação, instalação, custos, design
openspec/                             config, specs e changes do OpenSpec
.claude/                              comandos /opsx e skills do OpenSpec, gerados pelo CLI
```

Os arquivos de `.claude/commands/opsx/` e `.claude/skills/openspec-*/` são gerados pelo `openspec init` e atualizados com `openspec update`; não edite à mão.

O primeiro módulo, `Users` (usuários, vínculos com o provedor de identidade e sessão), nasceu na Acesso-1 e serve de exemplo do guia `src/Modules/README.md`; a busca de pessoas inclui o diretório da empresa (`IPeopleDirectory`, hoje o de demonstração da entrada local), e quem é incluído num projeto antes de entrar vira usuário pelo contrato `IUserProvisioning` (ADR-0005). O `Files` (Acesso-4) guarda os arquivos de todos os módulos pelo contrato `IFileStorage`, sem tabelas: o registro de cada arquivo fica no módulo que o usa. O `Audit` (Acesso-3) grava a auditoria de todos os módulos: o contexto base registra cada `SaveChangesAsync` na mesma transação, e cada módulo só marca o rótulo do item, os campos técnicos ou secretos, as referências a itens de outros módulos, que aparecem pelo nome atual por uma fonte `IAuditReferenceNames` do módulo dono (ADR-0084), e o contexto do item (`IsAuditContext`, como o projeto de uma pessoa), gravado em toda mudança (seção "Auditoria" do guia). O `Administration` (Acesso-6) guarda as configurações da instalação numa linha só, criada pela migração; a marca (nome e logo) é lida sem sessão em `GET /api/installation`, e o logo vai pelo `IFileStorage`. O `Calendar` (Projetos-1) é dono do calendário da instalação: dias de trabalho, feriados nacionais e pontos facultativos calculados (`INationalCalendar`) e feriados locais; os outros módulos contam dias úteis pelo contrato `IWorkCalendar`, sem repetir a regra. O `Projects` (Projetos-2 a 4) é dono dos clientes (nome, CNPJ, observação, situação e logo) e dos projetos, com as pessoas de cada projeto e o papel delas, incluídas, trocadas e removidas numa transação serializável que nunca deixa o projeto sem gerente (ADR-0042, ADR-0085); listar e incluir clientes e criar projetos usam a política `SystemPolicies.ProjectCreators` (administrador ou gerente de projeto), o que cada pessoa faz num projeto sai de `ProjectPermissions` (ADR-0043) e quem não está no projeto recebe 404. Na Cronograma-1, o `Projects` ganhou o cronograma: um agregado `Schedule` por projeto (fases, tarefas e marcos, dependências), com as datas calculadas no domínio pelo `IBusinessDays` do `Calendar` (ADR-0051), o dia de hoje no fuso da instalação pelo `InstallationClock` e os números de cada cronograma num lugar só (`ScheduleFigures`); toda mudança roda na mesma transação serializável das pessoas, e só o administrador e o gerente editam (`CanEditSchedule`). Na Cronograma-2, o progresso virou uma ação própria (`PUT .../progress`), que o responsável pela tarefa também faz (`MayUpdateProgress`, com `canUpdateProgress` em cada item), e o marco ganhou o estado atingido (`ReachedOn`) e o atraso. Na Cronograma-3, a aba ganhou a Linha do tempo: as novas datas de um arrasto ou da borda de uma barra vão por `PUT .../dates` (`Schedule.Reschedule`, que conta os dias úteis no domínio), o `GET /schedule` traz os dias não úteis (`nonWorkingDays`) para o sombreado e a chave do Syncfusion chega ao navegador por `GET /api/client-settings` (ADR-0087). Na Cronograma-4, a tarefa direto na fase passou a ter subtarefas num nível só (`ParentId`): a tarefa-resumo tem datas, duração e progresso calculados delas a cada mudança (`Summarize`), as dependências ficam só entre quem tem datas próprias e a primeira subtarefa herda as da tarefa (ADR-0088). Na Cronograma-4.1, cada tarefa e marco do `GET /schedule` passou a dizer há quantos dias úteis está atrasado (`lateDays`, de `ScheduleFigures.LateDaysOf`, com o calendário da instalação), e a tarefa-resumo, o da subtarefa mais atrasada. Na Cronograma-5, o agregado ganhou a linha de base (`ScheduleBaseline`, uma por projeto, com as datas de cada item em `ScheduleBaselineItem`, fora da auditoria), salva e atualizada pelo administrador e pelo gerente (`PUT .../schedule/baseline`), e o `GET /schedule` passou a trazer o caminho crítico (`critical`, de `ScheduleCriticalPath`, nas datas atuais e em dias úteis) e a diferença de cada item e do projeto contra a linha de base (`baselineDeviation` e `baseline.deviation`, em dias úteis com sinal). Na Cronograma-6, toda mudança do cronograma feita por quem o edita passa a ser guardada para desfazer e refazer (ADR-0091): o `ScheduleEditing.RunAsync` compara as fotos do agregado antes e depois (`Schedule.Snapshot`, `ScheduleDiff`) e grava a diferença, com o rótulo que cada caso de uso devolve (`ScheduleResult.Labeled`), em `ScheduleChange` (as últimas 20 de cada pessoa por projeto, fora da auditoria); `POST .../schedule/undo` e `.../redo` aplicam a diferença (`Schedule.ApplyChange`) só se os registros estão como ela os deixou, e o `GET /schedule` diz o que o desfazer e o refazer fariam (`undo` e `redo`). Caso de uso novo do cronograma devolve o rótulo, para entrar no desfazer. Na Cronograma-7, o cronograma passou a sair no XML do MS Project (`GET .../schedule/ms-project`, montado à mão pelo `MsProjectExport` com `System.Xml.Linq`, com o calendário da instalação, a linha de base e a restrição "Não iniciar antes de" para o MS Project manter as datas), para todos que veem o cronograma. Na Cronograma-8, o `Projects` ganhou os modelos de projeto (ADR-0017): `ProjectTemplate`, com fases, itens (a distância em dias úteis ao início, a duração, o tipo e a tarefa em que está) e dependências, sem datas nem responsáveis, salvo do cronograma por quem o edita (`POST .../schedule/template`, também com o projeto encerrado) e administrado só pelo administrador (`/api/admin/project-templates`); o `CreateProject` recebe `templateId` e `scheduleStart` e gera o cronograma na mesma transação pelo `Schedule.ApplyTemplate`. Na Chamados-1, nasceu o módulo `Tickets`, dono dos chamados (schema `tickets`): o número sai da sequência `tickets.ticket_numbers`, pedida antes de salvar para a auditoria já ter o número (ADR-0029); quem abre, quem vê, os times que atendem e os "meus times" saem de `TicketAccess` (ADR-0044, ADR-0068); e os projetos que cada pessoa vê, o papel dela e as pessoas de cada projeto vêm do contrato `IProjectAccess` do `Projects.Contracts`, sem ler as tabelas do `Projects`. Na Chamados-2, o chamado ganhou a conversa (ADR-0026, ADR-0069): mensagens (`TicketMessage`), anexos (`TicketAttachment`, pelo `IFileStorage`, na pasta `{código} - {nome}/Chamados/#{número}` até a área de documentos ligar o projeto a uma pasta) e quem acompanha (`TicketFollower`), fora da auditoria; o texto formatado vai e volta como o JSON do Tiptap e é validado no domínio pelo `RichText` (só parágrafos, listas, negrito, links http, https e mailto e menções); mandar mensagem, mencionar e o papel de cada pessoa na conversa saem de `TicketAccess`; o limite dos anexos é `Tickets:Attachments:MaxMegabytes` (15). Na Chamados-3, o chamado ganhou o fluxo de status (ADR-0024): as transições ficam na tabela `tickets.ticket_transitions`, criada pela migração com o fluxo do MVP e conferida pelo `TicketFlow.From` (nada sai de Fechado, todo status tem saída, nenhuma ação se repete no mesmo status); quem faz cada ação sai de `TicketAccess.ActorsOf` (quem atende é o administrador, os gestores e quem atende o time); `POST /api/tickets/{number}/transitions` muda o status (403 de quem não pode, 409 fora do status, 400 sem a mensagem pedida); a primeira mensagem de quem atende o time num chamado sem responsável o torna responsável e leva o Aberto a Em andamento, e a resposta em Aguardando solicitante o retoma (`SendTicketMessage`); os eventos (`TicketEvent`, fora da auditoria) entram na conversa; o `TicketAutoClose`, em segundo plano, fecha a cada hora os Resolvidos parados há `Tickets:AutoCloseBusinessDays` (5) dias úteis do calendário da instalação; e chamado Fechado não recebe mensagens (409). Na Chamados-4, o chamado ganhou os prazos (ADR-0025): os dias de primeira resposta e de resolução de cada prioridade ficam na tabela `tickets.ticket_deadlines`, que o administrador muda em `/api/admin/ticket-deadlines` (de 1 a 365, para os chamados novos); o chamado guarda os dias e as datas de vencimento da abertura, e todo cálculo de prazo, inclusive a pausa em Aguardando solicitante e a retomada estendida pelos dias úteis pausados, fica no `TicketDeadlines` (Application), por onde passa toda mudança de status (`TakeAsync`); a primeira resposta é cumprida pela primeira mensagem de quem atende o time; `PUT /api/tickets/{number}/priority` muda a prioridade (quem atende, com o chamado em aberto), recalcula os prazos a partir da abertura e registra o evento `PriorityChanged`; a lista e o chamado trazem o estado de cada prazo (`TicketDueState`), calculado contra o dia da instalação; o chamado tem um token de concorrência (`rowversion`), e uma gravação que encontra o chamado mudado vira `TicketChangedException` e 409. Na Chamados-5, o chamado ganhou as trocas (ADR-0027, ADR-0068): `PUT /api/tickets/assignee` e `PUT /api/tickets/requester` trocam o responsável e o solicitante de um ou de vários chamados de uma vez (tudo ou nada, pelo `TicketBatch`), com as opções de pessoa em `GET /api/tickets/assignee-choices` e `requester-choices`; `PUT /api/tickets/{number}/team` transfere para o outro time, sem responsável; quem pode e quem pode ser escolhido saem de `TicketAccess` (`MayAssign`, `CanBeAssignee`, `MayChangeRequester`, `CanBeRequester`, `MayTransfer`); e quem sai do projeto ou muda de papel deixa sem responsável os chamados que não pode mais atender, pelo contrato `IProjectMemberChanges` do `Projects.Contracts`, que o `Projects` chama depois de gravar (`MemberChangeNotices`) e o `Tickets` implementa (`TicketMemberChanges`). Os módulos previstos (ADR-0047, com os nomes do ADR-0074) são Tickets, Projects, Documents, Credentials, Notifications, Audit e Administration; a lista final é decidida na change que cria cada um.

## 4. Convenções e não-negociáveis

- Idioma: código, nomes e comentários em inglês, inclusive módulos, termos do negócio, tabelas e rotas; a tradução de cada termo fica em `docs/glossario.md` (ADR-0074). Mensagens de commit e títulos e descrições de PR em português do Brasil (regra deste projeto, que substitui a do CLAUDE.md global). Specs, propostas, ADRs e docs em português do Brasil. Textos da interface e dos e-mails só em arquivos de tradução, com chaves em inglês (ADR-0013).
- C#: nullable habilitado, avisos tratados como erro, `.editorconfig` com `dotnet format` (ADR-0061).
- TypeScript em modo estrito, com ESLint e Prettier (ADR-0049, ADR-0061).
- API REST em JSON sob `/api`, contrato OpenAPI gerado pelo ASP.NET Core, tipos do frontend gerados do contrato, erros em Problem Details (RFC 9457), identificadores GUID v7; o número do chamado é sequencial (ADR-0029, ADR-0061).
- Datas e horas em UTC no banco, exibidas no fuso da instalação (ADR-0013).
- Versões fixadas: .NET 10; Node 24 até 28/10/2026 e Node 26 depois; números em `global.json`, `.nvmrc` e `engines` (ADR-0060). OpenSpec CLI 1.14.3 (`npm i -g @fission-ai/openspec@1.14.3`), com a telemetria desligada (`openspec config set telemetry.enabled false`).
- Licenças: só MUI gratuito; nada de FluentAssertions 8 ou superior (use AwesomeAssertions), MediatR ou AutoMapper; nenhuma dependência com licença comercial ou copyleft forte sem ADR (ADR-0050, ADR-0056, ADR-0061).
- Pacotes `@syncfusion/*` só dentro do componente `ScheduleGantt` (ADR-0051).
- Visual: siga o design system FiscoProjects e as telas aprovadas; nunca invente cor, espaço ou canto fora dos tokens. Status sempre por ícone, cor e texto, nunca só cor (ADR-0062 a 0067).
- IBM Plex Sans servida pela própria aplicação, sem Google Fonts (ADR-0064).

## 5. Diretrizes de desenvolvimento

- `async`/`await` de ponta a ponta, com `CancellationToken` nos casos de uso e no acesso a dados.
- Injeção de dependência nativa do ASP.NET Core.
- Um caso de uso por classe simples, com seus testes; sem MediatR. Mapeamento escrito à mão ou com Mapperly.
- SOLID e nomes claros; prefira código legível a código esperto.
- Sem duplicação: regras de calendário, prazos e permissões ficam num lugar só e são reaproveitadas.
- Frontend: tema central do MUI (variáveis CSS, temas claro e escuro) com os tokens do design system; wrappers próprios para componentes repetidos (por exemplo, a tabela padrão em pt-BR); React Hook Form com Zod nos formulários; Tiptap no editor dos chamados; react-i18next nos textos.
- Telas de trabalho na altura da janela: a página não rola, as áreas rolam por dentro (ADR-0066).
- Antes de usar a API de uma biblioteca, consulte a documentação atual (Context7).

## 6. Como rodar, buildar e testar

Pré-requisitos: SDK do .NET 10 (o `global.json` aceita qualquer 10.0 a partir do 10.0.100), Node da versão do `.nvmrc`, Docker em execução (banco local e testes) e o OpenSpec CLI.

Backend, na raiz do repositório:

```
dotnet build FiscoProjects.slnx -warnaserror
dotnet test FiscoProjects.slnx
dotnet format FiscoProjects.slnx --verify-no-changes
dotnet run --project src/FiscoProjects.Api
```

- `dotnet format FiscoProjects.slnx` (sem `--verify-no-changes`) corrige a formatação.
- Os testes sobem o próprio SQL Server em contêiner (Testcontainers); sem o Docker rodando, os testes de banco e da API falham.
- Com a API rodando no ambiente de desenvolvimento, a saúde fica em `/health` e o contrato em `/openapi/v1.json`.

Banco local (uma vez por máquina; depois, só o `docker compose up -d`):

```
cp .env.example .env                  # e troque a senha
docker compose up -d --wait
dotnet user-secrets set "ConnectionStrings:FiscoProjects" "Server=localhost,1433;Database=FiscoProjects;User Id=sa;Password=<senha do .env>;TrustServerCertificate=True" --project src/FiscoProjects.Api
dotnet tool restore
```

- No desenvolvimento, a API cria o banco e aplica as migrações ao subir. Migrações novas com `dotnet ef`, conforme `src/Modules/README.md`.
- `docker compose down` para o banco; os dados ficam no volume `sqlserver-data` (`docker compose down -v` apaga).
- Dados de demonstração (ADR-0089): com a API no ar, `npm run demo:data` em `web/` carrega pela própria API, entrando como as pessoas do diretório de demonstração, os clientes, os projetos, os membros e os cronogramas fictícios, com datas relativas ao dia em que roda. Não repete se o ERP-FIN já existe e não roda sem a entrada local. O plano fica em `web/scripts/demo/plan.ts`: toda change que cria telas ou dados acrescenta os dela ali, com os testes de `web/scripts/demo`. Depois da carga, abra as telas em `http://localhost:5173` (`npm run dev`): a 5175 é a API e serve o último `npm run build`, que pode estar velho.
- Entrada local (ADR-0078): sem tenant, a página de entrada aceita, sem senha, o e-mail de uma pessoa do diretório de demonstração (`LocalSignIn:People` no `appsettings.Development.json`), como `marina.costa@empresa.example`. O mesmo diretório é o da busca de pessoas: Lucas Prado e Sofia Martins estão nele para mostrar quem ainda não entrou. Ela só vale fora da produção: a aplicação não sobe em produção com `LocalSignIn:Enabled` ligado.
- Arquivos (ADR-0079): no desenvolvimento ficam em `src/FiscoProjects.Api/App_Data/files` (`FileStorage:Provider` = `Local`), fora do Git. Os testes de um armazenamento herdam `FileStorageContract` de `tests/FiscoProjects.Files.Tests`.
- Papéis (ADR-0006, ADR-0040): quem entra pela primeira vez é Usuário; o primeiro administrador vem de `Users:FirstAdministrator` (no desenvolvimento, `paulo.nogueira@empresa.example`). As rotas só do administrador usam `RequireAuthorization(SystemPolicies.Administrator)`, que lê o papel do banco a cada requisição.
- Sessão por cookie `HttpOnly` (ADR-0054), que expira depois de `Session:IdleTimeout` sem uso (8 horas). As rotas sob `/api` exigem sessão, salvo as marcadas com `AllowAnonymous`, e toda alteração (POST, PUT, PATCH, DELETE) exige o cabeçalho `X-CSRF: 1`. Arquivo que o navegador guarda pelo endereço com `?v=` (um logo) leva o marcador `KeepSession` do `Users.Contracts`: a sessão é conferida, mas não renovada, porque o cookie renovado faria a resposta sair com `no-store`.

Frontend, em `web/`:

```
npm ci
npm run dev          # http://localhost:5173, repassa /api, /health e /openapi para a API em 5175
npm run lint
npm run typecheck
npm test
npm run build        # gera src/FiscoProjects.Api/wwwroot, que a API serve
```

- Textos visíveis só em `web/src/locales/pt-BR.json`, com chaves em inglês: texto escrito direto num componente reprova o lint, e chave inexistente reprova a checagem de tipos.
- Quando a API muda, `npm run api:update` atualiza o contrato (`web/src/api/openapi.json`) e os tipos (`web/src/api/schema.ts`). Os dois são gerados, nunca editados à mão, e vão no commit; os testes reprovam se ficarem desatualizados.
- `npm run format` corrige a formatação (Prettier).
- Visual: cores, medidas e fontes vêm de `web/src/theme/tokens.json`, cópia exata do `tokens.json` do design system (para atualizar, copie o arquivo de novo). Componentes próprios usam as variáveis pelo nome do design system (`var(--sidebar-bg)`); cor escrita direto no código reprova os testes.
- Cor principal (ADR-0082): os tokens da marca vêm da cor da instalação por `web/src/theme/brandColors.ts`, aplicados pelo `BrandColors` na rota raiz; com a cor padrão, valem os do `tokens.json`. Nos testes, uma cor digitada pelo administrador vai sem `#` (`'0F766E'`), porque a regra de cores reprova `#RRGGBB` no código.
- Tema (ADR-0014): a escolha da pessoa fica no perfil (`ThemePreference`, `PUT /api/profile/theme`) e o `SessionProvider` a aplica com o `useColorScheme` do MUI; sem sessão, segue o sistema. Nada de guardar o tema no navegador.
- Datas na tela só pelas funções de `useDates()` (`web/src/format/useDates.ts`), no fuso da instalação (ADR-0013), nunca no fuso do navegador; dias de filtro como `DayValue`, com a meia-noite do fuso. Dias sem fuso, como os do cronograma e o `today` que a API devolve com eles, por `formatDayText` e `formatPeriodText` (`web/src/format/dates.ts`).
- Tela nova: uma rota em `web/src/routes.tsx`, com o título no `handle`, e o componente `Page` de `web/src/shell`. As abas de um projeto são rotas filhas do `ProjectLayout` (cabeçalho, "Ações do projeto" e mensagens), que entrega o projeto às abas por `useProjectOutlet`. As telas dentro da estrutura só abrem com sessão. Telas da Administração ficam sob `/admin`, dentro de `RequireAdministrator` e do `AdminLayout`.
- Listas em tabela pelo `DataTable` de `web/src/components` (MUI X Data Grid gratuito em pt-BR, com a paginação no servidor). Linhas de grupo (fp-group-row), como os times da aba Membros, pelo `isGroupRow`, com a primeira coluna ocupando a linha (`colSpan`); com `groupRowsChoosable`, elas se escolhem, como as fases do cronograma, com as próprias células. O `overrides` do `web/package.json` fixa o `@mui/x-internals` na versão do Data Grid; ao atualizar o Data Grid, atualize os dois juntos.
- Caixas de seleção pelo `Checkbox` e diálogos pelo `Dialog` de `web/src/components` (fp-check e fp-dialog do design system), não pelos do MUI direto. Um diálogo com `onSubmit` é um formulário. Texto de várias linhas pelo `TextArea`, o logo de uma empresa (ou as iniciais) pelo `LogoTile`, um campo com rótulo, ajuda e erro em volta de outro controle pelo `FormField`, a escolha de uma pessoa (o gerente, o líder técnico) pelo `PersonField`, que busca em `/api/people` enquanto se digita, e a de várias opções (o "Depende de" de uma tarefa) pelo `MultiSelect`. A faixa de fases pelo `ScheduleStrip`, o progresso pelo `ProgressBar` e um prazo ou atraso (ícone, cor e texto) pelo `DueDate`; as tarefas e os marcos atrasados pelo `LateCounts` e o estado de um marco pelo `MilestoneStatus` (`web/src/projects/ScheduleStatus.tsx`). O atraso de uma tarefa ou de um marco ("2 dias úteis de atraso") sai do `lateText` e do `showsDelay` (`web/src/projects/schedule.ts`), com os dias que a API conta; as subtarefas de uma tarefa, no detalhe, pelo `SubtaskSection`.
- Linha do tempo pelo `ScheduleGantt` de `web/src/schedule-gantt` (ADR-0051), a única pasta que importa `@syncfusion/*` (o lint reprova o import fora dela), carregada sob demanda pelo `ScheduleTimeline`. Ele só desenha as datas da API e transforma arrasto, borda e ligação em eventos (`onMove`, `onResize`, `onLink`); a conversão fica em `ganttRows.ts` e a escala em `ganttLayout.ts`, testadas sem o Syncfusion. As cores do tema Material 3 vêm dos tokens pelo `syncfusionPalette.ts` e o visual pelo `ScheduleGantt.css`; a telemetria do Syncfusion fica desligada (`Telemetry.disable()`) e o `vite.config.ts` tira a fonte do Google do CSS dele (o `frontend.sh` reprova o build que a carregue). Só uma Linha do tempo montada por vez: o Syncfusion procura partes dele na página inteira. Nos testes de tela, o `ScheduleGantt` é simulado com `vi.mock` (`ProjectScheduleTimeline.test.tsx`); o real é testado no jsdom com a largura simulada (`simulateLayout` e `drawWith` de `ganttTesting.tsx`). Escolher linhas e trocar o que o Gantt exibe ficam em arquivos de teste próprios (`ScheduleGanttChoice.test.tsx` e `ScheduleGanttCritical.test.tsx`): um Gantt desenhado antes no mesmo arquivo deixa o Syncfusion sem escolher pelo clique e, com a máquina ocupada, sem desenhar de novo.
- "Exibir" (a linha de base e o caminho crítico, ligados de início e iguais no Expandir), o "⋮" com salvar e atualizar a linha de base, o resumo contra a linha de base e a seção Linha de base do detalhe ficam em `web/src/projects/ScheduleBaseline.tsx`; o `ScheduleGantt` desenha a linha de base pelo próprio Syncfusion (`renderBaseline`, só sob as tarefas com datas próprias) e o caminho crítico pela classe `fp-gantt-critical`, com `showBaseline` e `showCritical`. Uma opção marcável dentro de um menu (`menuitemcheckbox`) usa o `CheckboxMark` do `Checkbox`. O "Desfazer" da barra e do Expandir é o `UndoButton` (`ScheduleSteps.tsx`), o "Refazer" fica no "⋮" (`ScheduleMoreMenu`), os dois pelo `useScheduleStep`, e o texto de cada mudança ("a inclusão da tarefa X") sai do `stepText` de `schedule.ts`. O "⋮" aparece para todos, com as exportações; o PDF sai da página de impressão (`/projects/:id/schedule/print`, fora da estrutura das telas, sempre clara e em A4 paisagem), aberta pelo `ExportPdfDialog` com o período e a escala, e desenhada pelo `SchedulePrintPage` em SVG com os tokens do Gantt do design system, uma faixa por linha, sem o Syncfusion; as contas da escala ficam em `schedulePrint.ts`. "Salvar como modelo" fica no "⋮" (`SaveTemplateDialog`); o "Novo projeto" escolhe "Em branco" ou "A partir de um modelo" pelo `ScheduleOriginChoice` e mostra a prévia do modelo pelo `TemplatePreview` (`TemplateChoice.tsx`); a Administração tem "Modelos de projeto" (`TemplatesPage`, desenhada como a de Clientes).
- Chamados em `web/src/tickets`: o status pelo `TicketStatusBadge`, a prioridade pelo `PriorityTag` e a pessoa (ou "Sem responsável") pelo `TicketPersonTag` (`TicketTags.tsx`); a lista pelo `TicketTable`, na página Chamados (as visões numa coluna ao lado, como a Administração) e na aba Chamados do projeto (`ProjectTicketsPage`, com o `SegmentedControl` e a contagem de cada opção em `count`); o "Abrir chamado" pelo `OpenTicketPage`, em `/tickets/new` e em `/projects/:id/tickets/new`; e a página do chamado no número (`/tickets/104`, `TicketPage`), com a conversa (`TicketConversation`, as cores e os selos de cada papel pelos tokens `msg-*`, `role-*` e `avatar-*`, os eventos numa linha discreta e o filtro "Tudo / Mensagens / Histórico" pelo `conversationItems` de `conversation.ts`), a resposta (`TicketReply`), as ações do fluxo no cabeçalho (`TicketActions`: a primeira que a pessoa pode é o botão, as outras ficam no "⋮", e a que pede mensagem abre um diálogo com o editor) e o painel. O "Fluxo de chamados" (`/tickets/flow`, `TicketFlowPage`) mostra a tabela das transições. Um prazo de chamado pelo `TicketDue` (`web/src/tickets/TicketDue.tsx`, sobre o `DueDate`), no cabeçalho, na seção Prazos do painel e embaixo do status na lista ("Status e prazo"); "Mudar prioridade" fica no "⋮" (`TicketActions`); a Administração tem "Prazos dos chamados" (`TicketDeadlinesPage`, desenhada como Configurações gerais). A lista marca os chamados que a pessoa pode trocar, com a barra "N selecionados" do `DataTable` (`selectionActions`) e o link "Atribuir" embaixo de "Sem responsável"; a troca de pessoa, de um ou de vários chamados, pelo `PeopleChangeDialog`, e a transferência pelo `TransferDialog` (`TicketPeopleDialogs.tsx`, com as regras de erro em `ticketChanges.ts`); o detalhe da pessoa na aba Membros mostra os chamados em aberto dela e "Passar pendências" (`MemberTickets`). Texto formatado pelo `RichTextEditor` de `web/src/components` (Tiptap, carregado sob demanda com `lazy`), mostrado pelo `RichText`, que monta o JSON nó por nó, sem HTML; nos testes, digita-se nele pelo `typeInEditor` de `web/src/test/editor.ts`.
- Diálogo em tela quase inteira, como o Expandir, pelo `Dialog` com `size="full"`, `subtitle` e `headerTools`.
- Detalhe de um item escolhido numa lista pelo `DetailsDrawer` de `web/src/components` (ADR-0081): gaveta à direita, por cima da tela, com o fundo escurecido, aberta só ao escolher o item. A lista não ganha coluna fixa de detalhe.
- Chamadas à API pelo cliente `web/src/api/client.ts` (openapi-fetch com os tipos do contrato), que já envia o `X-CSRF`. Nos testes de tela, a API é simulada com o MSW em `web/src/test/server.ts`; chamada não simulada reprova o teste.
- Leituras da API pelo TanStack Query (ADR-0080), com as consultas em `web/src/api/queries.ts`. Os dados ficam só na memória e são descartados quando a sessão termina, menos a marca da instalação, que é pública. Listas usam `placeholderData: keepPreviousData` e passam ao `DataTable` `loading={isFetching}`; o indicador só aparece se a espera passar de 300 ms, inclusive na atualização de uma lista já vista. Depois de uma alteração, invalide as chaves dos recursos afetados (`queryKeys`). Propriedades passadas ao `DataTable` devem ser estáveis (funções fora do componente ou memoizadas), para a tabela não redesenhar as células.

Specs:

```
openspec validate --all --strict
```

Verificação do PR: em todo PR para a `main`, o GitHub Actions (`.github/workflows/pull-request.yml`) roda os scripts de `scripts/ci` em jobs paralelos (ADR-0090): `backend` (`build.sh` e `test.sh`), `format` (`format.sh`), `frontend` (`frontend.sh`) e `specs` (`specs.sh`), com os pacotes do NuGet e do npm em cache. O `changes.sh` compara o commit com o último do PR que passou na verificação: se só mudou documentação (`docs/`, `openspec/`, `.claude/` e `.md` fora de `web/`), só as specs rodam, como no push do archive. O job `verify` decide pelo `verdict.sh`. O workflow só prepara .NET e Node e chama os scripts (ADR-0055); a lógica nova entra nos scripts, não no workflow.

- Para rodar o mesmo na máquina: `bash scripts/ci/<script>.sh`, de qualquer pasta (no Windows, pelo Git Bash). `frontend.sh` e `specs.sh` usam o Node da versão do `.nvmrc`; o `specs.sh` instala o OpenSpec CLI fixado em `scripts/ci/package.json`.
- A regra "Protect main" exige a verificação `verify` aprovada. Renomear o job bloqueia o merge até a regra ser atualizada.
- Os testes de `tests/FiscoProjects.Pipeline.Tests` leem o workflow como texto (um job por `  <nome>:` sob `jobs:`, scripts por `run: bash scripts/ci/<script>.sh`) e rodam o `changes.sh` e o `verdict.sh` com o `bash`; no Windows, os que precisam do `bash` são pulados.
- Pacote do NuGet com vulnerabilidade conhecida, direto ou transitivo, quebra o build. No frontend, `frontend.sh` reprova pacote vulnerável que vai para o navegador e dependência com licença fora da lista do script `licenses` do `web/package.json`.

Previsto para a Fundação-6, que entra nos scripts de `scripts/ci`:

```
terraform -chdir=infra/terraform fmt -check && terraform -chdir=infra/terraform validate
```

Desenvolvimento local com Docker (SQL Server), implementações locais de e-mail e arquivos e um tenant Microsoft pessoal, nunca o da empresa (ADR-0002, ADR-0004, ADR-0048).

## 7. Padrões recorrentes (reuse, não reinvente)

- Fila de e-mails no banco (outbox), enviada em segundo plano com novas tentativas; a ação do usuário não depende do envio (ADR-0036).
- Canais de aviso atrás de uma interface (e-mail, sino e, depois, Teams); as regras de quem recebe não conhecem o canal (ADR-0038).
- Calendário único da instalação para o cronograma e os prazos dos chamados (ADR-0018, ADR-0025).
- Fluxo de status dos chamados lido de tabela e validado (ADR-0024).
- Auditoria automática num ponto único da camada de dados, ao salvar (ADR-0039).
- Autorização pela relação da pessoa com o item e pelas matrizes dos ADR-0043 a 0046; testes de permissão gerados de uma tabela (ADR-0056).
- Processos em segundo plano dentro da aplicação (ADR-0047).
- Tudo o que varia por empresa é configuração ou cadastro (ADR-0003).

## 8. Fontes de verdade e documentação de referência

- Regras de negócio: ADRs em `docs/adr/` e specs em `openspec/specs/`. O que não está num ADR nem numa spec não é inventado: pergunte ou proponha.
- Pessoas: Entra ID. Papéis: banco do FiscoProjects. Arquivos: SharePoint. Feriados nacionais: calculados pelo sistema.
- Visual: design system FiscoProjects (https://claude.ai/artifact/CymvW4j1dqVrX3KVN13Jt7) e canvas de telas (https://claude.ai/artifact/WoMTFZKZXEpXzmPCCU4xfV), aprovados em 17/10/2026.
- Nunca use dados reais. Dados de demonstração e de testes são fictícios, ficam só no seed e nos testes, e nenhum nome de empresa, cliente ou fornecedor fica fixo no código (ADR-0002, ADR-0003).
- Antes de implementar uma área, leia os ADRs dela (tabela "Documentação de referência" em `docs/discovery.md`). Em infraestrutura e setup, leia também `docs/instalacao.md`.

## 9. Fluxo de trabalho

- OpenSpec: toda mudança, inclusive correções pequenas, começa por uma proposta (`/opsx:propose`).
  - Changes do roadmap (`docs/roadmap.md`): o agente propõe, avalia a proposta contra os ADRs e o design técnico e visual e, se estiver dentro, implementa (`/opsx:apply`) sem esperar aprovação da proposta. A aprovação de Marcelo é no PR.
  - Mudanças fora do roadmap: a proposta é aprovada por Marcelo antes de implementar.
- Quando parar e perguntar: só quando a decisão afeta o design técnico ou visual aprovado, envolve LGPD ou é exclusiva de Marcelo (produto, custo, licença, conta). Nos demais casos, decida, siga e liste as decisões tomadas no resumo final da change.
- Roadmap: as changes têm um ID por grupo (`Fundação-1`, `Acesso-2`...) e são feitas na ordem. Correção de uma change já arquivada ganha um subnúmero (`Fundação-2.1`), só quando necessário. O status de cada change é atualizado em `docs/roadmap.md` a cada etapa.
- Changes pequenas, com um objetivo funcional claro, em fatias verticais finas. Cada fatia termina com build sem avisos, testes passando e lint limpo.
- Decisão com alternativas reais vira ADR em `docs/adr/` antes de implementar. ADR aceito não muda de conteúdo: um ADR novo o substitui (ADR-0001).
- Testes: crie ou atualize os testes de toda mudança e rode-os antes de concluir. Cada cenário de spec tem um teste rastreável pelo nome; as matrizes de permissão são 100% cobertas (ADR-0056).
- Git, para cada change (esta ordem substitui, neste projeto, a do CLAUDE.md global):
  1. Sempre uma branch nova a partir da `main` atualizada: `git switch main`, `git pull`, `git switch -c tipo/nome-da-change`. Nunca escreva na `main` (a regra "Protect main" do GitHub também impede).
  2. Proposta e implementação na branch, com testes; commits pequenos em Conventional Commits.
  3. Push e PR com `gh pr create`; roadmap com "PR aberto". Avise Marcelo com o resumo.
  4. Depois da aprovação de Marcelo no chat (o GitHub não deixa o autor aprovar o próprio PR), rode o archive do OpenSpec (`/opsx:archive`) na mesma branch, atualize o roadmap para "Concluída" e faça push, para o PR levar código e specs juntos.
  5. Faça o merge do PR; se o merge não puder ser feito pela sessão, peça a Marcelo para fazê-lo no GitHub. Depois volte para a `main` (`git switch main`, `git pull`), apague a branch local com `git branch -d <branch>` (nunca `-D`) e a remota com `git push origin --delete <branch>`.
  6. Só então comece a próxima change, de novo pelo passo 1.
  - Nunca use `push --force` nem reescreva histórico publicado.
- Commits em Conventional Commits: `tipo(escopo): descrição`, no imperativo, em minúsculas, sem ponto final, até 72 caracteres. Tipos: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert. Quebra de compatibilidade com `!` e rodapé `BREAKING CHANGE:`. Títulos de PR no mesmo formato.
- Commits e PRs em português do Brasil: a descrição, o corpo do commit e o título e a descrição do PR. Tipo, `BREAKING CHANGE` e nomes de código continuam em inglês. Exemplo: `feat(chamados): adiciona troca do responsável em lote`.
- Sem linhas de atribuição: commits sem `Co-Authored-By` nem outros trailers de autoria do Claude, e PRs sem o rodapé "Generated with Claude Code" (preferência de Marcelo).
- Mensagens curtas e naturais, como uma pessoa escreveria: na maioria dos commits, só o título (por exemplo, `feat(api): health check do banco`). Corpo só quando o motivo não é óbvio, em uma ou duas frases. Nada de listar tarefas, IDs de change ou arquivos alterados.
- Descrição de PR curta: o que muda, o que conferir e como testar, sem relatório. Commits sem assinatura automática do ambiente (`git config commit.gpgsign false` no clone), para não aparecer "Unverified".

## 10. O que não fazer

- Não versionar segredos: nada de chaves, certificados, connection strings, chave do Syncfusion ou arquivos de estado do Terraform no repositório. Segredos ficam no Key Vault, nunca no Terraform (ADR-0052, ADR-0053).
- Não registrar senhas dos acessos, tokens ou dados pessoais em logs, URLs ou na auditoria; a auditoria registra só que a senha mudou (ADR-0033, ADR-0039, ADR-0057).
- Não mandar senha por e-mail (ADR-0033).
- Não expor tokens ao navegador: nada em `localStorage`, `sessionStorage` ou cookie legível por JavaScript (ADR-0054).
- Não conceder Mail.Send no Entra ID nem chamar APIs de permissão do SharePoint (ADR-0030, ADR-0036).
- Não usar App Roles do Entra para autorização (ADR-0006).
- Não acessar o Microsoft 365 real nos testes (ADR-0056).
- Não usar dados reais nem fixar nomes de empresa, cliente, fornecedor, pasta, modelo ou prazo no código (ADR-0002, ADR-0003).
- Não usar recursos, contas ou tenant do empregador (ADR-0002).
- Não pular a revisão: publicação em produção só pelo workflow manual, depois de revisar o plano (ADR-0055).
- Não trocar versões principais, inclusive a do OpenSpec CLI, sem pedir (ADR-0060).
