# Glossário português–inglês

Termos do negócio, como aparecem nas specs e na interface, e o nome usado no código (ADR-0074). Um termo novo entra aqui na mesma change que o cria. Os nomes abaixo valem como padrão; a change que cria o código pode refinar um nome, atualizando esta tabela.

## Instalação, pessoas e papéis

| Português | Código | Observação |
| --- | --- | --- |
| Instalação | Installation | Uma cópia do sistema para uma empresa (ADR-0003) |
| Configurações gerais | InstallationSettings | As configurações da instalação na Administração (`/admin/settings`), numa linha só do módulo `Administration` |
| Nome exibido | InstallationSettings.Name | Nome da instalação na barra lateral, na entrada e na aba; padrão "FiscoProjects" |
| Logo da instalação | Logo (`LogoFile`) | PNG ou SVG de até 1 MB, no lugar do selo |
| Cor principal | MainColor | Cor da marca da instalação em `#RRGGBB`, vazia para a do design system; os tokens da marca são derivados dela (ADR-0082) |
| Logo para o tema escuro | DarkLogo | Logo opcional mostrado na barra lateral escura; sem ele, vale o logo (ADR-0100) |
| Marca da instalação | Installation | Nome e logo, lidos sem sessão (`GET /api/installation`) |
| Regras gerais | GeneralRules | Configurações da instalação que as telas usam com sessão (`GET /api/installation/rules`); hoje, o fuso |
| Fuso horário | TimeZone | Identificador IANA da instalação, padrão America/Sao_Paulo; as datas das telas aparecem nele (ADR-0013) |
| Usuário | User | Módulo `Users`, dono das pessoas, dos vínculos e da sessão; também o papel de menor privilégio (`SystemRole.User`) |
| Vínculo com o provedor de identidade | IdentityLink | Provedor e identificador externo, separado do usuário (ADR-0009) |
| Entrada (login) | SignIn | |
| Entrada local | LocalSignIn | Por e-mail, sem senha, só fora da produção (ADR-0078) |
| Pessoas da demonstração | LocalSignIn.People | Diretório fictício da entrada local |
| Entrada pelo Entra ID | EntraSignIn, SignInWithEntra | Pela página da Microsoft, com o e-mail como dica, só numa instalação no Azure (ADR-0117) |
| Conta convidada | Guest (`acct`, `idp`) | Recusada na entrada, sem criar usuário (ADR-0007) |
| Atribuição no Entra | `entra_assignment_required` | Só as pessoas atribuídas ao aplicativo empresarial entram (ADR-0005) |
| Prova do aplicativo | IEntraClientAssertion | Token da identidade gerenciada que o aplicativo do Entra aceita, sem segredo nem certificado (ADR-0117) |
| Sessão | Session | |
| Sair | SignOut | |
| Papel no sistema | SystemRole | Administrador, Gerente de projeto, Líder técnico ou Usuário (ADR-0040, ADR-0097) |
| Primeiro administrador | FirstAdministrator | Configuração `Users:FirstAdministrator` (ADR-0006) |
| Último administrador ativo | LastAdministrator | Recusa da troca ou da desativação que deixaria a instalação sem administrador |
| Desativar acesso, Reativar acesso | Deactivate, Reactivate | |
| Situação: Ativo, Inativo | UserStatus: Active, Inactive | `IsActive` no domínio |
| Último acesso | LastSignInAt | |
| Meu perfil | Profile | Tela `/profile` da pessoa na sessão: preferências e conta |
| Tema: Claro, Escuro, Automático | ThemePreference: Light, Dark, Automatic | Preferência salva no perfil; Automático segue o sistema (ADR-0014) |
| Administração | Administration | Área só do administrador, rotas `/admin` (ADR-0046), e o módulo das configurações e dos cadastros |
| Papel no projeto | ProjectRole | Gerente de projeto, Líder técnico, Colaborador, Desenvolvedor ou Leitor, um por pessoa no projeto (ADR-0040) |
| Administrador | Administrator | |
| Gerente de projeto | ProjectManager | Papel no sistema e no projeto (ADR-0040) |
| Líder técnico | TechLead | Papel no projeto e, pelo ADR-0097, também no sistema: só quem o tem no sistema (ou o administrador) lidera tecnicamente um projeto (`SystemRoles.LeadsTechnically`, `canBeTechLead`) |
| Colaborador | Contributor | Time Funcional |
| Desenvolvedor | Developer | Time de Desenvolvimento |
| Leitor | Viewer | |
| Gestores (gerente e líder técnico) | Managers | |
| Pessoa do projeto (membro) | ProjectMember | A pessoa e o papel dela num projeto; na Auditoria, "Pessoas dos projetos" |
| Membros (aba do projeto) | `/projects/{id}/members` | As pessoas do projeto por time ou por nome, com "Incluir pessoa" e o detalhe na gaveta |
| Times na aba Membros: Gestão, Funcional, Desenvolvimento, Leitores | MemberTeam: management, functional, development, viewers | O líder técnico aparece em Gestão e em Desenvolvimento (ADR-0040, ADR-0068) |
| Incluir pessoa, trocar o papel, remover do projeto | AddProjectMember, ChangeProjectMemberRole, RemoveProjectMember | Administrador e gerente com todos; líder técnico com todos menos os gerentes (ADR-0042, ADR-0043) |
| Quem gerencia as pessoas | `CanManagePeople`, `CanManageManagers`, `MayManage` | A regra num lugar só, em `ProjectPermissions` |
| Último gerente | LastManager | O projeto sempre tem um gerente: remover ou rebaixar o último é recusado (ADR-0042) |
| Cliente | Client | Empresa para quem o projeto é feito (ADR-0015); módulo `Projects` (schema `projects`) e tela `/admin/clients` |
| CNPJ | Cnpj | Numérico ou alfanumérico (IN RFB nº 2.229/2024), guardado sem pontuação e único entre os clientes; opcional |
| Logo do cliente | Client logo (`ClientLogo`) | PNG ou JPG de até 1 MB, no lugar das iniciais; enviado só pelo administrador |
| Situação do cliente: Ativo, Inativo | ClientStatus: Active, Inactive | `IsActive` no domínio; o inativo continua na lista, apagado |
| Observação | Observation | Texto livre do cliente, até 500 caracteres |
| Quem cria projetos | SystemPolicies.ProjectCreators | Administrador ou gerente de projeto (ADR-0043): cria projetos e lista e inclui clientes |
| Clientes para escolher | ClientChoice (`GET /api/clients/choices`) | Nome, logo e situação de todos os clientes, para qualquer pessoa com sessão, sem o CNPJ |
| Pessoas para escolher | Person (`GET /api/people`) | Usuários ativos e pessoas do diretório que nunca entraram, buscados pelo nome ou e-mail por qualquer pessoa com sessão; com `managers`, só administradores e gerentes de projeto |
| Diretório da empresa | IPeopleDirectory | Onde as pessoas são achadas antes de entrar (ADR-0005): hoje o de demonstração da entrada local (`LocalSignIn:People`); na M365-2, o Microsoft Graph |
| Pessoa do diretório (que nunca entrou) | DirectoryPerson, `directoryId` | Identificada na API por `"{provider}:{externalId}"` até virar usuário |
| Usuário criado pelo diretório | `User.FromDirectory`, IUserProvisioning | Criado ao ser incluído num projeto, com o papel Usuário e sem último acesso; na primeira entrada, usa o mesmo usuário |
| Nunca entrou | `LastSignInAt` nulo | O último acesso de quem foi incluído pelo diretório e ainda não entrou |
| Fornecedor | Vendor | |

## Projetos e cronograma

| Português | Código | Observação |
| --- | --- | --- |
| Projeto | Project | Módulo `Projects` (schema `projects`), telas `/projects`, `/projects/new` e `/projects/{id}`; quem não está no projeto não o vê, menos o administrador (ADR-0043) |
| Projeto interno | Project sem `ClientId` | Projeto sem cliente |
| Código do projeto | ProjectCode | De 2 a 15 letras, algarismos e hífens, começando por letra, em maiúsculas e único na instalação; ex.: ERP-FIN |
| Status do projeto: Planejamento, Em andamento, Pausado, Concluído, Cancelado | ProjectStatus: Planning, InProgress, Paused, Completed, Cancelled | |
| Projeto encerrado (concluído ou cancelado) | IsClosed | Somente leitura até ser reaberto, voltando a Em andamento (ADR-0016) |
| Ações do projeto | ProjectPermissions | Editar dados e mudar o status, conforme o papel da pessoa (ADR-0043) |
| Situação na lista: Ativos, Concluídos e cancelados, Todos | ProjectSituation: Active, Closed, All | Filtro da tela Projetos |
| Meus projetos | `mine=true` | Projetos ativos em que a pessoa está, na barra lateral |
| Cronograma | Schedule | Um por projeto, com o mesmo id; rota `/projects/{id}/schedule` e aba Cronograma (ADR-0019) |
| Lista (visualização do cronograma) | ProjectSchedulePage | As fases em linhas de grupo e, abaixo, as tarefas e os marcos (ADR-0071) |
| Linha do tempo (visualização do cronograma) | Timeline | `ScheduleTimeline`, aberta no computador; `?view=list` abre a Lista. Na interface não aparece a palavra "Gantt" (ADR-0071) |
| Componente do Gantt | ScheduleGantt | Único que usa o Syncfusion, em `web/src/schedule-gantt` (ADR-0051, ADR-0074) |
| Escala: Dias, Semanas, Meses | GanttScale: days, weeks, months | `?scale=` no endereço; o período ocupa a largura disponível (ADR-0071) |
| Expandir, Recolher | Expand, Collapse | A Linha do tempo em tela quase inteira, com a tabela completa (ADR-0071) |
| Novas datas (arrastar ou puxar a borda) | Reschedule | `PUT /schedule/items/{itemId}/dates`, com o início e, numa tarefa, o fim |
| Dias não úteis | NonWorkingDays | Do calendário da instalação, no `GET /schedule`, só para o sombreado da Linha do tempo |
| Configurações do navegador | ClientSettings | `GET /api/client-settings`, com a chave do Syncfusion (`timelineLicense`, ADR-0087) |
| Fase | SchedulePhase | Nome e, dentro dela, tarefas e marcos; as datas e o progresso vêm deles |
| Tarefa ou marco (item do cronograma) | ScheduleItem | Rota `/schedule/items`; o tipo não muda depois da criação |
| Tipo: tarefa, marco | ScheduleItemKind: Task, Milestone | O marco é uma data, sem duração, responsável nem progresso (ADR-0018, ADR-0020) |
| Subtarefa | Subtask (`ScheduleItem` com `ParentId`) | Tarefa ou marco dentro de uma tarefa da mesma fase, num nível só (ADR-0088) |
| Dentro de | ParentId | A tarefa em que o item está; vazio quando está direto na fase |
| Tarefa-resumo | Summary (`hasSubtasks`) | Tarefa com subtarefas: datas, duração, progresso e atraso vêm delas, e ela não tem dependências (ADR-0088) |
| Início, fim | Start, End | Dias sem fuso; o fim é calculado em dias úteis pelo servidor |
| Duração em dias úteis | Duration | De 1 a 999 numa tarefa, contando o início; zero no marco |
| Responsável | OwnerId | Uma pessoa do projeto que não é leitora; quem sai ou passa a leitor deixa as tarefas sem responsável (ADR-0042, ADR-0043) |
| Dependência; anterior e seguinte | ScheduleDependency: PredecessorId, SuccessorId | Termina → começa; dependência circular é recusada (ADR-0019) |
| Empurrar as seguintes | Push, `pushed` | Atrasar uma tarefa move as seguintes só o necessário; antecipar não as puxa (ADR-0019) |
| Tarefa atrasada | IsLate, `late` | Fim antes de hoje, no fuso da instalação, abaixo de 100% (ADR-0020) |
| Marco atrasado | IsLate, `late`, LateMilestones | Data antes de hoje, no fuso da instalação, sem ter sido atingido (ADR-0086) |
| Data de conclusão | CompletedOn | Gravada quando a tarefa chega a 100%, apagada abaixo disso |
| Marco atingido, pendente; atingido em | `reached`, ReachedOn | Marcado e desmarcado pelo administrador e pelo gerente em `PUT /schedule/items/{itemId}/reached`; guarda o dia (ADR-0086) |
| Atualizar o progresso; Iniciar, Atualizar progresso, Concluir | UpdateProgress, `PUT /schedule/items/{itemId}/progress` | Ação própria, fora do formulário da tarefa; "Iniciar" pede o percentual e "Concluir" põe 100% (ADR-0086) |
| Pode atualizar o próprio progresso | CanUpdateOwnProgress, MayUpdateProgress, `canUpdateProgress` | Administrador e gerente em qualquer tarefa; líder técnico, colaborador e desenvolvedor nas de que são responsáveis (ADR-0043) |
| Números do cronograma | ScheduleFigures | Datas, progresso ponderado pela duração, atraso e fase atual, num lugar só |
| Fase atual | CurrentPhase | A primeira fase atrasada ou, sem atraso, a última que já começou |
| Hoje da instalação | InstallationClock | O dia de hoje no fuso da instalação, para o atraso e a conclusão |
| Linha de base | Baseline | |
| Caminho crítico | CriticalPath | |
| Progresso | Progress | De 0 a 100% numa tarefa; o da fase e o do projeto são ponderados pela duração (ADR-0020) |
| Modelo de projeto | ProjectTemplate | |
| Calendário e feriados | Calendar | Módulo `Calendar` (schema `calendar`) e tela `/admin/calendar`; os outros módulos contam dias úteis pelo contrato `IWorkCalendar` (ADR-0018, ADR-0083) |
| Calendário da instalação, dias de trabalho | WorkCalendar, WorkingDays | Os dias da semana em que a instalação trabalha, numa linha só; padrão de segunda a sexta, nunca nenhum |
| Dia útil | BusinessDay | Dia de trabalho que não é feriado nem ponto facultativo considerado (`IBusinessDays`) |
| Feriado | Holiday | |
| Feriado nacional | NationalHoliday | Calculado pelo sistema (`INationalCalendar`), considerado até o administrador desconsiderar |
| Ponto facultativo | OptionalHoliday | Calculado pelo sistema, desconsiderado até a empresa considerar; meio período conta como dia inteiro (ADR-0083) |
| Feriado calculado | CalculatedHoliday | Feriado nacional ou ponto facultativo, com a escolha de considerar, que vale em todos os anos |
| Feriado local | LocalHoliday | Da cidade, do estado ou da empresa: nome, data e "Repete todo ano", a partir do ano da data |
| Considerado, desconsiderado | IsConsidered | Considerado, o feriado deixa de ser dia útil; desconsiderado, continua na lista como dia útil |
| Marcar todos, Desmarcar todos | ConsiderHolidays | Considera (ou desconsidera) de uma vez as datas da lista mostrada |
| Meu trabalho | MyWork | Módulo `MyWork`, sem tabelas, e a página inicial: a fila por urgência dos itens da pessoa (ADR-0070, ADR-0096) |
| Fila por urgência | MyWorkQueue | A regra dos grupos, da ação de cada linha e da ordem, num lugar só |
| Visões: Para mim, Abertos por mim, Que acompanho | MyWorkView: ForMe, OpenedByMe, Following | |
| Grupos: Atrasado, Aguardando você, Hoje e o próximo dia útil, Próximos 7 dias | MyWorkGroup: Late, Waiting, Soon, Week | Próximos 7 dias corridos; o que vence depois fica fora da fila (ADR-0096) |
| Itens de cada módulo para o Meu trabalho | IMyWorkSource | Contrato do `MyWork.Contracts`, que o `Tickets`, o `Projects` e o `Documents` implementam |
| Próximos marcos | UpcomingMilestones | Os 10 próximos marcos não atingidos dos projetos abertos da pessoa (`GET /api/projects/upcoming-milestones`) |
| Atraso do cronograma | LateDays | Os dias úteis de atraso do item mais atrasado do projeto (ADR-0096) |
| Visão geral do projeto | ProjectOverview | A aba Visão geral, que responde "está no prazo?", montada no servidor (`GET /api/projects/{id}/overview`, ADR-0073) |
| Andamento previsto | SchedulePlan.PlannedProgress | O que já deveria estar feito até o fim de hoje, em dias úteis, pela linha de base quando existe |
| Desvio do prazo final | Deviation | Os dias úteis entre o fim do cronograma e o da linha de base, com sinal |
| Precisa de atenção | Attention | As tarefas atrasadas, os chamados vencidos, por vencer ou sem responsável, os aceites pendentes e os chamados parados nos fornecedores do projeto |
| Chamados do projeto na visão geral | IProjectTickets | Contrato do `Projects.Contracts`, que o `Tickets` implementa (`TicketProjectOverview`) |
| Prazo que conta agora | TicketDueNow | O prazo de um chamado que vale hoje (primeira resposta ou resolução) e o estado dele, num lugar só |

## Chamados

| Português | Código | Observação |
| --- | --- | --- |
| Chamado | Ticket | |
| Número do chamado | TicketNumber | Sequencial na instalação (ADR-0029) |
| Time de destino: Funcional, Desenvolvimento | Team: Functional, Development | |
| Solicitante | Requester | |
| Responsável | Assignee | |
| Acompanhar | Follow | |
| Status: Aberto, Em triagem, Em andamento, Aguardando solicitante, Resolvido, Fechado | TicketStatus: Open, Triage, InProgress, WaitingForRequester, Resolved, Closed | |
| Transição do fluxo | TicketTransition | Lida da tabela `ticket_transitions` e conferida pelo `TicketFlow` (ADR-0024) |
| Prioridade: baixa, média, alta, crítica | Priority: Low, Medium, High, Critical | |
| Prazo de primeira resposta | FirstResponseDays, FirstResponseDueOn | Os dias da prioridade e o dia em que vence, guardados no chamado (ADR-0025) |
| Prazo de resolução | ResolutionDays, ResolutionDueOn | Sem vencimento enquanto Aguardando solicitante; estendido pelos dias pausados (ADR-0025) |
| Prazos dos chamados (Administração) | TicketDeadlineSetting | Uma linha por prioridade, na tabela `ticket_deadlines` |
| Estado do prazo: em dia, vencendo, vencido, pausado, cumprido | TicketDueState: OnTrack, Soon, Overdue, Paused, Met | Vencendo é hoje ou no próximo dia útil |
| Mensagem | Message | |
| Menção | Mention | |
| Anexo | Attachment | |
| Transferência entre times | Transfer | |
| Chamado em fornecedor | VendorTicket | Registrado no nosso chamado por quem atende, na auditoria (ADR-0028) |
| Fornecedor | Vendor | Cadastrado pelo administrador em Administração, Fornecedores; nunca é excluído, só inativado |
| Status no fornecedor: Aberto no fornecedor, Em análise, Aguardando nosso retorno, Resolvido, Fechado | VendorTicketStatus: Open, InAnalysis, WaitingForUs, Resolved, Closed | Qualquer status vai para qualquer outro |
| Sem retorno do fornecedor (parado) | VendorTickets.StaleDaysOf | Em aberto no fornecedor e sem atualização há `Tickets:VendorStaleBusinessDays` (5) dias úteis |
| Com fornecedor (visão) | TicketView.WithVendor | Os chamados com registro em fornecedor |
| Visões: Atribuídos a mim, Sem responsável nos meus times, Abertos por mim, Todos os abertos | TicketView: AssignedToMe, UnassignedInMyTeams, OpenedByMe, All | "Sem responsável" da aba do projeto é `Unassigned` (ADR-0068) |
| Situação na lista: Em aberto, Encerrados, Todos | TicketSituation: Open, Finished, All | Encerrados são os resolvidos e os fechados |
| Acesso aos projetos (contrato) | IProjectAccess | Os projetos que cada pessoa vê e o papel dela, para os outros módulos |
| Que acompanho (visão) | TicketView.Following | Os chamados que a pessoa acompanha |
| Papel na conversa: Solicitante, Responsável, Gerente do projeto, Líder técnico, Desenvolvimento, Funcional | TicketParticipantRole: Requester, Owner, Manager, TechLead, Development, Functional | Dá a cor e o selo de cada mensagem (ADR-0069) |
| Texto formatado | RichText | O JSON do editor (Tiptap), validado no servidor |
| Ações do fluxo: Iniciar triagem, Primeira resposta, Iniciar atendimento, Pedir informações, Enviar resposta, Marcar como resolvido, Confirmar e fechar, Fechamento automático, Reabrir, Cancelar chamado | TicketAction: StartTriage, FirstResponse, StartWork, AskForInformation, Reply, Resolve, ConfirmAndClose, AutoClose, Reopen, Cancel | A primeira resposta, a resposta em Aguardando solicitante e o fechamento automático são do sistema, nunca botões |
| Quem pode no fluxo: quem atende, solicitante, gestor, administrador, sistema | TicketActors: Attendant, Requester, Manager, Administrator, System | Quem atende inclui o administrador e os gestores (ADR-0044) |
| Evento da conversa: mudança de status, responsável definido, prioridade, responsável tirado, solicitante trocado, time trocado, registro no fornecedor, atualização no fornecedor | TicketEvent: StatusChanged, AssigneeSet, PriorityChanged, AssigneeCleared, RequesterChanged, TeamChanged, VendorTicketRegistered, VendorStatusChanged | Fora da auditoria, como as mensagens |
| Trocar o responsável, em lote | ChangeTicketAssignees | Só o administrador, o gerente e o líder técnico (ADR-0068) |
| Trocar o solicitante | ChangeTicketRequesters | O próprio solicitante troca um a um; em lote, só os gestores |
| Transferir de time | TransferTicket | Deixa o chamado sem responsável |
| Passar pendências | (tela) | Os chamados de quem sai, passados a outra pessoa |
| Mudanças das pessoas de um projeto (contrato) | IProjectMemberChanges | O Projects avisa os outros módulos depois de gravar (ADR-0042) |
| Fechamento automático | TicketAutoClose | Processo em segundo plano; `Tickets:AutoCloseBusinessDays` (5) |
| Fechado pelo cancelamento do projeto | TicketEventKind.ClosedWithProject | Evento "projeto cancelado" na conversa, de qualquer status (ADR-0043) |
| Encerramento do projeto (contrato) | IProjectClosing | O `Projects` pergunta os chamados não fechados e pede o fechamento ao cancelar |
| Resumo do encerramento | ProjectClosing | Chamados não fechados e tarefas abaixo de 100%, antes de concluir ou cancelar |

## Documentos, links, acessos e termos

| Português | Código | Observação |
| --- | --- | --- |
| Arquivos (módulo) | Files | Contrato `IFileStorage`; sem tabelas (ADR-0079) |
| Armazenamento de arquivos | FileStorage | SharePoint em produção; pasta local no desenvolvimento |
| Pasta, arquivo guardado | StoredFolder, StoredFile | Identificados por ids opacos, guardados por quem usa |
| Mesmo nome: nova versão, manter os dois | SameNameAction: NewVersion, KeepBoth | |
| Regras de tipo de arquivo | FileTypes, FileRefusal | No `Files.Contracts`: programa ou script recusado e o tipo para o navegador, usadas por anexos e documentos |
| Documentos (módulo) | Documents | Documentos, links e termos de aceite (ADR-0092) |
| Documento | Document | |
| Versão do documento | DocumentVersion | |
| Status do documento: Rascunho, Em revisão, Aprovado, Obsoleto | DocumentStatus: Draft, InReview, Approved, Obsolete | A nova versão de um aprovado volta para Em revisão (ADR-0093) |
| Pasta padrão | DefaultFolder | Lista da Administração, copiada para cada projeto novo |
| Pasta do projeto | DocumentFolder | Cópia de uma pasta padrão, com a pasta no armazenamento |
| Pasta de termos de aceite | `IsAcceptance` | Só leitura na área de documentos, só para a gestão (ADR-0093) |
| Enviar arquivos | UploadDocument | Um arquivo por vez; o mesmo nome na mesma pasta vira nova versão |
| Vincular ao cronograma | LinkDocument | A uma fase, tarefa ou marco do mesmo projeto |
| Projeto criado (contrato) | IProjectSetup | O `Projects` avisa o projeto novo; o `Documents` cria as pastas |
| Itens do cronograma (contrato) | IScheduleItems | Fases, tarefas e marcos de um projeto, com o nome atual |
| Link do projeto | ProjectLink | Título, endereço http ou https, categoria e para que serve (ADR-0032) |
| Categoria de link | LinkCategory | Lista da Administração; a inativa fica fora dos links novos |
| Cadastrar links | `DocumentAccess.MayEditLinks` | Todos do projeto, menos o leitor (ADR-0045) |
| Endereço da web | WebAddress | No `SharedKernel`: http ou https, até um tamanho, usado por links e fornecedores |
| Busca sem acento | SearchText | No `SharedKernel`: busca numa lista em memória sem caixa e sem acento |
| Acessos (módulo) | Credentials | Acessos aos portais, com as senhas cifradas (ADR-0092) |
| Acesso (login e senha de um portal) | PortalCredential | Portal, ambiente, endereço, usuário, senha e observação (ADR-0034) |
| Ambiente do acesso | `Environment` | Texto, como Produção; os do projeto viram sugestão |
| Senha cifrada | `ProtectedPassword`, IPasswordProtector | Data Protection só dos acessos, com as chaves no banco (ADR-0033) |
| Mostrar e copiar a senha | RevealPassword | Só quando pedida, sem guardar no navegador e sem registro de quem pediu |
| Certificado das chaves | `Credentials:KeyCertificate` | Opcional; cifra o conjunto de chaves |
| Importar planilha | PreviewCredentialImport, ImportCredentials | Lê o `.xlsx` ou `.csv` sem guardar e inclui as linhas marcadas, todas ou nenhuma (ADR-0094) |
| Leitor de planilhas | SpreadsheetReader, CsvText | Open XML SDK para o `.xlsx`; leitor próprio para o `.csv` |
| Mapeamento das colunas | ImportMapping | De que coluna vem cada campo; o ambiente também "Mesmo para todas" |
| Termo de aceite | AcceptanceTerm (`documents.acceptance_terms`) | Pedido de aceite de um marco, respondido com PDF assinado, ou termo assinado anexado pela gestão (ADR-0035, ADR-0095) |
| Situação do termo: pendente, assinado, recusado | AcceptanceStatus: Pending, Signed, Refused | |
| Pedir aceite | RequestAcceptance | O administrador e o gerente, de um marco, para uma pessoa do projeto, com o termo em PDF |
| Termo para assinar | `FileId`, `FileName` | O PDF do pedido, em `{código} - {nome}/Pedidos de aceite` |
| Enviar o termo assinado | SignAcceptance | A pessoa do pedido, em qualquer papel; vira documento Aprovado na pasta de termos e o marco fica atingido |
| Anexar termo assinado | SignAcceptance (em nome), AttachAcceptance (sem pedido) | Termo assinado recebido por fora, em nome de uma pessoa do projeto |
| Recusar o termo, motivo da recusa | RefuseAcceptance, `RefusalReason` | Encerra o pedido; o marco continua em aberto |
| Lembrar | RemindAcceptance | Manda de novo o e-mail do pedido e entra no histórico |
| Cancelar pedido | CancelAcceptance | O pedido pendente sai da lista e fica só na auditoria |
| Histórico do termo | AcceptanceEvent (`documents.acceptance_events`) | Pedido, lembrete, assinado, anexado e recusado, fora da auditoria |
| Marco atingido por outro módulo (contrato) | IScheduleMilestones | O `Projects` marca o marco no dia da instalação, fora do desfazer |
| Aceites pendentes (contratos) | IProjectAcceptances, IPendingAcceptances | No resumo do encerramento e no resumo diário |

## Avisos e auditoria

| Português | Código | Observação |
| --- | --- | --- |
| Aviso, notificação | Notice (`INoticeBoard`, `NoticeKind`) | O que um módulo conta que aconteceu; o `Notifications` decide quem recebe e por qual canal (ADR-0038) |
| Canal de aviso | INoticeChannel (`EmailChannel`, `BellChannel`) | E-mail e sino; Teams depois |
| Sino, notificação do sino | NotificationBell, Notification (`notifications.notifications`) | Os mesmos avisos do e-mail, na hora; lida ou não lida (ADR-0038) |
| Marcar como lida, marcar todas como lidas | ReadNotification, ReadAllNotifications | |
| Guardar notificações por | InstallationSettings.NotificationRetentionDays | Padrão 90 dias, de 1 a 365; as mais antigas são apagadas a cada hora |
| Aviso na hora | `EmailChannel.Immediate` | O que pede ação da pessoa, por e-mail logo (ADR-0037); não se desliga |
| Resumo diário | DailySummary (`SendDailySummaries`, `DigestItem`) | Um e-mail por dia com o restante e as tarefas que vencem; a pessoa desliga em Meu perfil (`ReceivesDailySummary`) |
| Horário do resumo diário | InstallationSettings.DailySummaryAt | Padrão 07:30, no fuso da instalação |
| Alerta de prazo | TicketDeadlineAlert (`AlertTicketDeadlines`) | Prazo de chamado vencendo (hoje ou no próximo dia útil) ou vencido, uma vez por estado |
| Tarefas que vencem | UpcomingTask (`IUpcomingTasks`) | Tarefas abaixo de 100% com responsável que terminam até o segundo dia útil |
| Fila de e-mails | OutboxEmail (`notifications.outbox_emails`) | Módulo `Notifications`; todo e-mail entra na fila e sai em segundo plano (ADR-0036) |
| Na fila, Enviado, Falhou | OutboxEmailStatus: Pending, Sent, Failed | Falhou depois da sexta tentativa |
| Envio em segundo plano | EmailDispatcher (`DispatchEmails`) | A cada 30 segundos, com a reserva de cada e-mail e as novas tentativas |
| Caixa de envio | `Email:Mailbox` | Caixa compartilhada de onde os e-mails saem; só leitura nas Configurações gerais |
| Endereço das telas | `Email:SiteUrl` | Base dos links dos e-mails |
| Envio local | LocalEmailSender (`Email:Provider` = `Local`) | Grava `.eml` numa pasta, sem enviar; recusado em produção |
| Teste de envio | SendTestEmail (`POST /api/admin/email/test`) | E-mail para o próprio administrador, na seção Microsoft 365 |
| Auditoria | Audit | Módulo `Audit`, dono das tabelas da auditoria (schema `audit`) |
| Registro de auditoria | AuditEntry | Só acrescentado; a anonimização apaga dele o nome e o e-mail da pessoa, e a limpeza mensal apaga os mais antigos que o prazo (ADR-0039, ADR-0110) |
| Ação: criou, alterou, excluiu, entrou, recusou uma entrada, exportou, limpou | AuditAction: Created, Updated, Deleted, SignedIn, SignInRefused, Exported, Cleaned | |
| Exportação da auditoria (relatório em Excel) | AuditExport | Só do administrador, registrada na própria auditoria com o período, o tipo, a pessoa e os registros (ADR-0099) |
| Campo alterado, antes e depois | AuditField: Field, Before, After | Valores em texto invariável; a tela traduz |
| Autor; Sistema quando não há sessão | ActorId (vazio para Sistema) | |
| Tipo de item | ItemType | Nome da entidade (`User`) ou `SignIn` para as entradas |
| Rótulo do item | ItemLabel, `HasAuditLabel` | O que identifica o item na tela, como o nome da pessoa |
| Campo fora da auditoria | `ExcludeFromAudit` | Campos técnicos, como o último acesso |
| Campo secreto | `IsAuditSecret` | Registra só que mudou, sem o valor (ADR-0033) |
| Referência a outro item | `IsAuditReference`, `HasAuditLabelReference`, `AuditField.Reference` | Guarda o identificador e o tipo; a lista mostra o nome atual por `IAuditReferenceNames` (ADR-0084) |
| Contexto do item | `IsAuditContext` | Campo gravado em toda mudança, mesmo sem mudar, para dizer onde o item está, como o projeto de uma pessoa; a tela o usa no texto, fora da tabela de mudanças |
| Evento de segurança | ISecurityEvents | Entrada e entrada recusada; troca de papel e situação entram como alteração do usuário |
| Motivo da recusa: fora do diretório, conta desativada | SignInRefusal: NotInDirectory, Inactive | |
| Anonimização | Anonymization, AnonymizeUser (`POST /api/users/{id}/anonymization`) | Só de pessoa desativada, definitiva; o nome vira o identificador e o e-mail fica vazio (ADR-0058, ADR-0110) |
| Pessoa anonimizada | UserStatus.Anonymized, `User.AnonymizedAt` | Situação "Anonimizado" em Usuários, sem reativar nem trocar o papel |
| Identificador da anonimização | `Users:AnonymousName`, `User.AnonymousNumber` | "Pessoa removida nº {0}", com o número em sequência na instalação |
| Apagador de dados pessoais | IPersonalDataEraser, PersonErasure | Cada módulo apaga as cópias do nome e do e-mail, na transação da anonimização |
| Prazo da auditoria | AuditRetentionYears | "Guardar auditoria por", de 1 a 10 anos (padrão 5), nas Configurações gerais |
| Limpeza mensal da auditoria | CleanUpAudit, AuditCleanup, AuditCleanupRun (`audit.cleanups`) | Uma vez por mês da instalação; registrada na auditoria, por Sistema, quando apaga algum |
| Exclusão de projeto | DeleteProject (`POST /api/projects/{id}/deletion`), `ProjectPermissions.CanDelete` | Só o administrador, com o código digitado; definitiva (ADR-0041, ADR-0111) |
| Resumo da exclusão | GetProjectDeletion (`GET /api/projects/{id}/deletion`), ProjectItemKind | Chamados, documentos, links, acessos, termos de aceite, tarefas e marcos e pessoas |
| Apagar o que é do projeto | IProjectDeletion, ProjectItemCount | Cada módulo: conta, apaga os arquivos e apaga as linhas na transação da exclusão |
| Remoção do que foi guardado | StoredFilesRemoval, `IFileStorage.FolderOfAsync` | Os arquivos registrados e as pastas que ficaram vazias, até a raiz |
| Monitoramento | Monitoring (`AddFiscoProjectsMonitoring`), Application Insights | OpenTelemetry pela distribuição do Azure Monitor, só com `APPLICATIONINSIGHTS_CONNECTION_STRING` (ADR-0057, ADR-0112) |
| Código da instalação | installation (Terraform) | De 2 a 6 letras ou números, parte do nome de todo recurso do Azure, como `app-fiscoprojects-mlr-dev` (ADR-0114) |
| Ambiente | environment (Terraform) | `prod` ou `dev` (desenvolvimento e demonstração); parâmetros em `infra/terraform/environments/` (ADR-0052, ADR-0114) |
| Estado do Terraform | Terraform state | Guardado numa conta de armazenamento por instalação, criada pelo `Prepare-TerraformState.ps1` (ADR-0053) |
| Administradores do banco | grupo "FiscoProjects <instalação> <ambiente> - administradores do banco" | Grupo do Entra ID que administra o Azure SQL, que aceita só o Entra ID (ADR-0114) |
| Identidade gerenciada | managed identity (App Service) | Como a aplicação entra no banco e no Key Vault, sem senha (ADR-0052) |
| Identidade do pipeline | módulo `ci` (Terraform), environment do GitHub | Aplicativo do Entra ID que o GitHub Actions alcança por federação para publicar e conferir um ambiente (ADR-0115) |
| Publicação | `deploy-dev` (workflow), `scripts/cd` | Pacote, publicação no App Service e conferência da infraestrutura a cada merge (ADR-0115) |
| Métricas da fila de e-mails | EmailQueueMetrics, EmailQueueState | `fiscoprojects.emails.waiting`, `.oldest_waiting`, `.sent` e `.failed`, para o alerta de fila parada |

## Telas

| Português | Código | Observação |
| --- | --- | --- |
| Controle segmentado | SegmentedControl | Poucas opções lado a lado, uma escolhida (o tema no menu da conta) |
| Caixa de seleção | Checkbox | Componente `Checkbox` (fp-check), com o rótulo visível ou só para leitores de tela |
| Diálogo | Dialog | Componente `Dialog` (fp-dialog): título, fechar, conteúdo e ações; formulário ou confirmação |
| Campo de várias linhas | TextArea | Componente `TextArea` (TextField multiline, fp-field__control--multiline) |
| Logo ou iniciais | LogoTile | Componente `LogoTile` (fp-logo-tile): o logo da empresa ao lado do nome, ou as iniciais |
| Gaveta de detalhes | DetailsDrawer | Detalhe de um item escolhido numa lista, à direita, por cima da tela, com o fundo escurecido (ADR-0081) |
| Faixa de fases | ScheduleStrip | Componente `ScheduleStrip` (fp-strip): fases, parte concluída, fases atrasadas, marcos e hoje (ADR-0071) |
| Barra de progresso | ProgressBar | Componente `ProgressBar` (fp-progress), com o percentual ao lado |
| Prazo | DueDate | Componente `DueDate` (fp-due): ícone, cor e texto do estado, como "Atrasada" |
| Escolha de várias opções | MultiSelect | Componente `MultiSelect`, no visual do campo, com as escolhidas em fichas, como "Depende de" |
