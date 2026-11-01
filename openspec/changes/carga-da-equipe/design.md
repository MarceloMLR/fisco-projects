## Context

A carga cruza dados de três módulos: as horas previstas são do `TimeTracking` (ADR-0138), as tarefas, os responsáveis, as datas e as ausências são do `Projects` (ADR-0141), e os dias úteis, do `Calendar`. Só o administrador vê as horas previstas, e a carga vem delas (ADR-0142).

## Goals / Non-Goals

**Goals:** a carga por pessoa e semana numa regra só, no domínio das horas; uma rota de leitura, sem tabela nova além da configuração; a tela na Administração.

**Non-Goals:** capacidade por pessoa, avisos, nivelamento.

## Decisions

- **Onde mora a regra.** No `TimeTracking`, dono das horas previstas: `TeamLoad` (Domain) com o espalhamento pelos dias úteis (`HoursIn`), a capacidade (`CapacityOf`) e o estado (`StateOf`, com o `HoursBalance.NearLimitShare` de 90%); o `GetTeamLoad` (Application) junta os dados. O domínio recebe o `IBusinessDays` do `Calendar.Contracts`, como o cronograma do `Projects`.
- **O que vem do `Projects`.** Um contrato novo, `ITeamWork` (`Projects.Contracts`): `TasksAsync(first, last)` (as tarefas abaixo de 100%, com uma pessoa como responsável, que cruzam o período, de projetos não concluídos nem cancelados, com o projeto, o nome e as datas), `AbsencesAsync(first, last)` (as ausências que cruzam o período) e `PeopleAsync()` (as pessoas dos projetos em aberto). Lido das tabelas do `Projects`, sem regra de quem vê: a rota é só do administrador.
- **Horas previstas.** `IProjectHoursStore.PlannedOfItemsAsync(ids)`, das tarefas que vieram do `ITeamWork`.
- **Pessoas.** Pelo `IUserDirectory` (nome, e-mail, ativo); as inativas ficam de fora; ordenadas pelo nome em pt-BR.
- **Uma rota só.** `GET /api/admin/team-load?week=` devolve as semanas (início, fim e dias úteis), as horas por dia, hoje e cada pessoa com cada semana (carga, capacidade, dias de ausência, estado, as tarefas e as ausências da semana). O detalhe da gaveta usa a mesma resposta, sem outra rota. O dia pedido vira a segunda da semana; fora de 2000 a 2099, 400 em `week`.
- **Arredondamento.** As horas de cada tarefa na semana e as somas em centésimos (`Math.Round(…, 2)`); a tela mostra uma casa.
- **Horas por dia de trabalho.** `InstallationSettings.WorkHoursPerDay` (`decimal(4,2)`, padrão 8, de 1 a 12 em quartos), no `InstallationInfo` e no `PUT /api/installation` (`workHoursPerDay`), com o rótulo da auditoria.
- **Tela.** `TeamLoadPage` (`web/src/admin`, `/admin/team-load`), o `DataTable` com a coluna da pessoa e uma por semana (`TeamLoadCell`: "32 de 40 h", o ícone e o tom do estado, a marca da ausência), o `SegmentedControl` das visões, a busca, as setas e "Esta semana"; a gaveta (`TeamLoadDrawer`, `?person=`) com uma seção por semana. As regras de tela em `teamLoad.ts`.

## Risks / Trade-offs

- [A carga é tão boa quanto as previstas] → a tela diz que só as tarefas com horas previstas pesam.
- [Muitas pessoas e projetos numa consulta] → três leituras do `Projects`, uma das previstas e uma do calendário por pedido; oito semanas no máximo.
