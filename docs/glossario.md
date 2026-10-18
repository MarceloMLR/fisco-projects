# Glossário português–inglês

Termos do negócio, como aparecem nas specs e na interface, e o nome usado no código (ADR-0074). Um termo novo entra aqui na mesma change que o cria. Os nomes abaixo valem como padrão; a change que cria o código pode refinar um nome, atualizando esta tabela.

## Instalação, pessoas e papéis

| Português | Código | Observação |
| --- | --- | --- |
| Instalação | Installation | Uma cópia do sistema para uma empresa (ADR-0003) |
| Usuário | User | |
| Papel no sistema | SystemRole | |
| Papel no projeto | ProjectRole | |
| Administrador | Administrator | |
| Gerente de projeto | ProjectManager | Papel no sistema e no projeto (ADR-0040) |
| Líder técnico | TechLead | |
| Colaborador | Contributor | Time Funcional |
| Desenvolvedor | Developer | Time de Desenvolvimento |
| Leitor | Viewer | |
| Gestores (gerente e líder técnico) | Managers | |
| Membro do projeto | ProjectMember | |
| Cliente | Client | Empresa para quem o projeto é feito (ADR-0015) |
| Fornecedor | Vendor | |

## Projetos e cronograma

| Português | Código | Observação |
| --- | --- | --- |
| Projeto | Project | |
| Código do projeto | ProjectCode | Ex.: ERP-FIN |
| Status do projeto: Planejamento, Em andamento, Pausado, Concluído, Cancelado | ProjectStatus: Planning, InProgress, OnHold, Completed, Cancelled | |
| Cronograma | Schedule | |
| Linha do tempo (visualização do cronograma) | Timeline | Na interface não aparece a palavra "Gantt" (ADR-0071) |
| Componente do Gantt | ScheduleGantt | Único que usa o Syncfusion (ADR-0051, ADR-0074) |
| Fase | Phase | |
| Tarefa | ScheduleTask | Evita confundir com `System.Threading.Tasks.Task` |
| Marco | Milestone | |
| Dependência | Dependency | |
| Linha de base | Baseline | |
| Caminho crítico | CriticalPath | |
| Progresso | Progress | |
| Modelo de projeto | ProjectTemplate | |
| Calendário da instalação | WorkCalendar | |
| Dia útil | BusinessDay | |
| Feriado | Holiday | |
| Ponto facultativo | OptionalHoliday | |
| Meu trabalho | MyWork | |
| Visão geral do projeto | ProjectOverview | |

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
| Transição do fluxo | StatusTransition | |
| Prioridade: baixa, média, alta, crítica | Priority: Low, Medium, High, Critical | |
| Prazo de primeira resposta | FirstResponseDeadline | |
| Prazo de resolução | ResolutionDeadline | |
| Mensagem | Message | |
| Menção | Mention | |
| Anexo | Attachment | |
| Transferência entre times | Transfer | |
| Chamado em fornecedor | VendorTicket | |

## Documentos, links, acessos e termos

| Português | Código | Observação |
| --- | --- | --- |
| Documento | Document | |
| Versão do documento | DocumentVersion | |
| Status do documento: Rascunho, Em revisão, Aprovado, Obsoleto | DocumentStatus: Draft, InReview, Approved, Obsolete | |
| Pasta padrão | DefaultFolder | |
| Link | Link | |
| Categoria de link | LinkCategory | |
| Acesso (login e senha de um portal) | PortalCredential | Módulo `Credentials` |
| Termo de aceite | AcceptanceRequest | Pedido de aceite de um marco, respondido com PDF assinado (ADR-0035) |
| Termo assinado | SignedAcceptance | |

## Avisos e auditoria

| Português | Código | Observação |
| --- | --- | --- |
| Aviso, notificação | Notification | |
| Resumo diário | DailyDigest | |
| Fila de e-mails | EmailOutbox | |
| Auditoria | Audit | |
| Registro de auditoria | AuditEntry | |
| Anonimização | Anonymization | |
