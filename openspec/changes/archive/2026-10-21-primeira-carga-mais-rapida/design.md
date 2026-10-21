## Context

Medição com os dados de demonstração (Playwright, navegando dentro da aplicação, API em Release): a primeira chamada de cada consulta paga a compilação da consulta do EF e do código (visão geral 0,82 s contra 0,10 s; contagem de chamados 0,50 s contra 0,08 s); a página do projeto só pede a aba depois que o projeto chega e pede cinco contagens para o cabeçalho; o Cronograma carrega 6,6 MB do Syncfusion na primeira abertura.

## Goals / Non-Goals

**Goals:**
- A primeira visita depois de a API subir como a segunda.
- Nenhuma regra de módulo nova e nenhuma leitura a mais com os dados já no cache.

**Non-Goals:**
- Configuração da produção (ReadyToRun, "Always On", HTTP/2, compressão): fica para o deploy (Fundação-7).
- Modelos e consultas compilados do EF.

## Decisions

- **`IWarmUp` no SharedKernel**: cada módulo tem o seu, na `Infrastructure`, porque escolhe a amostra no próprio `DbContext` e chama os casos de uso da `Application`, sem cruzar fronteiras. Users (sessão e perfil), Administration (marca e regras), Projects (listas, projeto, visão geral, cronograma, membros, próximos marcos e o Meu trabalho do cronograma), Tickets (listas, contagens, escolhas, chamado e o Meu trabalho dos chamados), Documents (pastas, documentos, links, termos e o Meu trabalho dos termos, só num projeto que já tem pastas), Credentials (lista, sem as senhas) e Notifications (sino). Calendar, Audit e MyWork ficam de fora: o calendário entra pelas leituras dos outros, a Auditoria é de uma tela só do administrador, e o Meu trabalho junta o que os módulos já aqueceram.
- **`StartupWarmUp`** (`BackgroundService` da API): espera o `ApplicationStarted`, roda cada aquecimento num escopo próprio, um depois do outro, mede e registra no log; uma falha vai para o log como aviso e o próximo roda. Sem banco configurado ou com `WarmUp:Enabled=false`, não faz nada. Os testes da API o desligam e rodam os aquecimentos só nos testes dele.
- **`prefetchProject`**: o projeto e a leitura da aba (visão geral, cronograma ou membros), com `queryClient.query({ ...opções, staleTime: 'static' })`, que só pede o que o cache não tem. As contagens do cabeçalho ficam com o cabeçalho: pedidas antes, chegavam antes de ele aparecer e eram pedidas de novo, e disputavam com a aba as poucas conexões que o navegador abre por vez. O `ProjectLayout` chama ao abrir e ao trocar de aba; o `DataTable` ganha `onRowHover` (o `onMouseEnter` da linha que se escolhe), usado pela lista de projetos e pela `TicketTable` (`prefetchTicket`), e a barra lateral chama no mouse e no foco de "Meus projetos".
- **`preloadTimeline`**: o mesmo `import('../schedule-gantt/ScheduleGantt')` do `ScheduleTimeline`, chamado pelo `ProjectLayout` no computador. Nos testes de tela, é simulado no `setup.ts`, para não carregar o Syncfusion.

## Risks / Trade-offs

- [Um caso de uso novo de tela fica sem aquecimento] → só a primeira visita depois de subir fica mais lenta; a nota no `CLAUDE.md` lembra de incluí-lo.
- [Aquecer escreve algo] → só casos de uso de leitura; o teste confere que a auditoria, as pastas e as notificações não lidas não mudam.
