# Proposta: cronograma (Cronograma-1)

## Why

O cronograma é o centro do projeto: fases, tarefas e marcos, com datas em dias úteis e dependências que mantêm as datas coerentes quando algo atrasa (ADR-0018, ADR-0019). Com os projetos e as pessoas prontos, falta o próprio cronograma. A Linha do tempo, o progresso pelo responsável, a linha de base e os modelos vêm nas changes seguintes e dependem dele.

## What Changes

- **Cronograma do projeto, no módulo `Projects`** (ADR-0019):
  - dois níveis: fases e, dentro delas, tarefas e marcos;
  - a tarefa tem início e duração em dias úteis, e o marco é uma data, sem duração (ADR-0018);
  - responsável opcional: uma pessoa do projeto que não é leitora (ADR-0043);
  - dependências do tipo termina → começa. Atrasar ou mover uma tarefa para depois empurra as seguintes, pulando fins de semana e feriados; antecipar não as puxa; dependência circular é recusada.
- **Progresso e atraso** (ADR-0020):
  - o percentual de cada tarefa, informado por quem edita o cronograma nesta change, por escolha de Marcelo;
  - a data real de conclusão é gravada ao chegar a 100%;
  - o progresso da fase e do projeto é ponderado pela duração das tarefas;
  - a tarefa vencida abaixo de 100% fica atrasada.
- **Quem faz o quê** (ADR-0043): todas as pessoas do projeto e o administrador veem o cronograma; só o administrador e o gerente o editam. O projeto encerrado é somente leitura (ADR-0016).
- **Quem deixa de poder ser responsável:** quem sai do projeto ou passa a leitor deixa as tarefas sem responsável (ADR-0042, ADR-0043).
- **Telas:**
  - aba Cronograma da página do projeto, só com a Lista, desenhada nesta change por escolha de Marcelo: as fases como linhas de grupo, com datas e progresso, e as tarefas e marcos com início, fim, duração, responsável, progresso e atraso;
  - "Nova fase" e "Nova tarefa", e a edição na gaveta (ADR-0081);
  - na lista de Projetos, a faixa de fases (ScheduleStrip) com o percentual e a fase atual, e o prazo final com as tarefas atrasadas (ADR-0071);
  - no cabeçalho do projeto, as datas no lugar de "Sem cronograma".
- **Auditoria** (ADR-0039, ADR-0084): fases, tarefas e dependências entram na auditoria, inclusive as tarefas empurradas, com o projeto, a fase e o responsável pelo nome.

## Non-goals

Cada item abaixo entra com a change indicada:

- Linha do tempo (o Gantt), escala, "Hoje", Expandir e arrastar: Cronograma-3. Até lá, a aba mostra só a Lista, por escolha de Marcelo.
- "Iniciar" e "Concluir", o responsável atualizando o progresso das próprias tarefas e o marco atingido: Cronograma-2.
- Linha de base, o desvio do prazo final contra ela e o caminho crítico: Cronograma-4.
- Desfazer: Cronograma-5. Exportar: Cronograma-6.
- A seção Cronograma do Novo projeto, os modelos e a prévia: Cronograma-7, por escolha de Marcelo.
- Na aba Membros, a coluna Tarefas, as tarefas da pessoa no detalhe e "Passar pendências": ficam para quando chamados e tarefas forem mostrados juntos.
- Fases e marcos na Visão geral: Síntese-2. Faixa de fases em Meu trabalho: Síntese-1.
- Avisos de datas e tarefas: grupo Avisos.
- O aviso ao concluir um projeto com tarefas abaixo de 100% (ADR-0043): Chamados-7, com as outras regras de encerramento.
- Recalcular as datas guardadas quando o calendário da instalação muda: fica para depois. Cada mudança no cronograma já usa o calendário do momento.

## Capabilities

### New Capabilities

- `cronograma`: fases, tarefas e marcos, datas em dias úteis, dependências que empurram, progresso ponderado, atraso, quem vê e quem edita, a aba Cronograma e a auditoria dessas mudanças.

### Modified Capabilities

- `projetos`: a lista de Projetos mostra a faixa de fases e o prazo final, e o cabeçalho mostra as datas e a aba Cronograma.
- `membros-do-projeto`: quem sai do projeto ou passa a leitor deixa as tarefas sem responsável.

## Impact

- **Backend:**
  - `Projects`: o cronograma (`SchedulePhase`, `ScheduleTask`, `ScheduleDependency`), o cálculo de datas pelo `IWorkCalendar` do `Calendar`, o "hoje" no fuso da instalação pelo `IInstallationSettings` do `Administration`, a permissão `CanEditSchedule`, as rotas `/api/projects/{id}/schedule` e o resumo do cronograma na lista e no projeto;
  - migração `Schedule`;
  - testes do domínio, dos casos de uso e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - rota `/projects/:projectId/schedule`, a aba no cabeçalho e o formulário da tarefa;
  - `ScheduleStrip` e a barra de progresso (fp-strip e fp-progress do design system);
  - textos da auditoria, traduções e testes.
- **Documentação:** glossário, `CLAUDE.md` e roadmap.
- **ADRs:** implementa 0018, 0019 e 0020, a linha do cronograma do 0043 e a faixa de fases do 0071, com 0016, 0039, 0042, 0051 (datas no servidor) e 0081.
