# Design: calendário da instalação (Projetos-1)

## Context

- **Módulos existentes:** `Users`, `Files`, `Audit` e `Administration`. O guia `src/Modules/README.md` diz como criar um módulo, o contexto do banco (`ModuleDbContext`, um schema por módulo) e a auditoria automática ao salvar (`HasAuditLabel`, `ExcludeFromAudit`).
- **Configurações da instalação:** o `Administration` guarda uma linha só, criada pela migração com `HasData` e um id fixo. A tela Configurações gerais usa React Hook Form, Zod e o TanStack Query.
- **Administração no frontend:** `AdminLayout` com a coluna de seções (Pessoas e segurança, Instalação), rotas sob `/admin` dentro de `RequireAdministrator`. No celular, a coluna some e a seção é aberta pelo endereço.
- **Listas:** `DataTable` (MUI X Data Grid gratuito em pt-BR, paginação no servidor) e `isRowMuted` para linhas apagadas.
- **Datas:** `useDates()` formata no fuso da instalação. Aqui as datas são dias (`DateOnly`), sem hora e sem fuso.
- **Canvas:** telas AdminCalendario, AdminCalendarioNovoFeriado e CelularCalendario, nos dois temas, aprovadas por Marcelo em 19/10/2026 com o pedido do "Marcar todos".
- **Regras:** ADR-0018 e ADR-0083 (proposto).

## Goals / Non-Goals

**Goals:**

- Uma só implementação da regra de dia útil, no domínio do módulo, que o cronograma e os prazos usam pelo contrato.
- A lista de um ano calculada na hora, sem guardar datas de feriados nacionais no banco.

**Non-Goals:**

- Cache do calendário entre requisições: são poucas linhas, lidas uma vez por requisição.
- Seletor de datas em calendário: o campo de data aceita "dd/mm/aaaa" digitado (decisão 7).

## Decisions

### 1. Módulo próprio `Calendar`, com o contrato `IWorkCalendar`

- Projetos `FiscoProjects.Calendar` (Domain, Application, Infrastructure, Endpoints) e `FiscoProjects.Calendar.Contracts`, schema `calendar`.
- O contrato expõe só o que o cronograma e os prazos precisam:
  - `IWorkCalendar.GetAsync(ct)` devolve um `IBusinessDays`;
  - `IBusinessDays` tem `IsBusinessDay(date)`, `AddBusinessDays(date, days)` e `CountBusinessDays(first, last)`, síncronos sobre um retrato do calendário.

  Uma recalculação de cronograma faz centenas de contas sem voltar ao banco.

Alternativas:

- Dentro do `Administration`: ele virou o módulo das configurações gerais, e o calendário tem regras próprias (Páscoa, soma de dias úteis) usadas por Projects e Tickets. Juntar faria todo módulo depender das configurações gerais para contar dias.
- Dentro do `Projects`: os prazos dos chamados (Tickets) dependeriam do módulo de projetos só para o calendário.

### 2. Domínio

- **`WorkCalendar`**, uma linha só com `WorkingDays`:
  - enum `[Flags]` de segunda a domingo, guardado como inteiro;
  - criado pela migração com id fixo e de segunda a sexta;
  - `ChangeWorkingDays` recusa o valor vazio.
- **`CalculatedHoliday`**, uma linha por feriado calculado:
  - `Key`, texto como `CarnivalMonday`, e `IsConsidered`;
  - criadas pela migração com ids fixos e o padrão do ADR-0083, para a escolha ser sempre uma alteração, auditada como tal.

  Alternativa: criar a linha só na primeira escolha. Foi descartada, porque a primeira escolha apareceria na auditoria como inclusão.
- **`LocalHoliday`:**
  - `Name` (até 80), `Date` (`DateOnly`), `RepeatsEveryYear` e `IsConsidered`, que começa verdadeiro;
  - `OccursIn(year)` diz a data no ano, ou nenhuma: antes do ano da data, sem repetir em outro ano, ou 29/02 fora dos bissextos.
- **Interface do cálculo:** `INationalCalendar` (domínio) lista as definições (chave, tipo e se começa considerado) e as datas de um ano. Fica atrás dessa interface por pedido do ADR-0018.
  - A implementação é `BrazilianNationalCalendar`.
  - A Páscoa usa o algoritmo de Meeus, Jones e Butcher, do calendário gregoriano.
  - A Consciência Negra entra a partir de 2024.
- **Tipos de data:** `NationalHoliday`, `OptionalHoliday` e `LocalHoliday`.
- **`BusinessDays`** (domínio) implementa `IBusinessDays` a partir de um retrato: dias de trabalho, escolhas, feriados locais e `INationalCalendar`.
  - Guarda, por ano, o conjunto das datas que não são úteis, calculado na primeira consulta daquele ano.
  - `AddBusinessDays(date, n)` anda um dia por vez para a frente (n > 0) ou para trás (n < 0) até contar n dias úteis, sem contar a própria data. Com n = 0, devolve a data.
  - `CountBusinessDays(first, last)` conta os dias úteis do período, com as pontas.
  - Uma busca que passa de 10 anos sem achar dia útil para com erro, porque só um calendário quebrado chega lá.
- **Ano válido:** de 1900 a 2199, nas datas dos feriados locais e no ano pedido. Fora disso, o pedido é recusado.

### 3. Aplicação

- **Casos de uso**, um por classe:
  - `GetCalendarYear(year)`: dias de trabalho e a lista do ano em ordem de data, com chave ou id, nome, tipo, repetição e se é considerado;
  - `ChangeWorkingDays`;
  - `ChooseCalculatedHoliday(key, isConsidered)`;
  - `ConsiderHolidays(keys, ids, isConsidered)`, o "Marcar todos": confere todos os feriados antes de mudar e salva uma vez, numa transação, com um registro de auditoria por feriado;
  - `AddLocalHoliday`, `ChangeLocalHoliday` e `RemoveLocalHoliday`.

  Os de feriado local devolvem os campos inválidos, como o `UpdateInstallation`.
- **Leitura pelo contrato:** `WorkCalendarReader` implementa `IWorkCalendar`, carregando o retrato pela loja `ICalendarStore`.

### 4. API

Rotas em `/api/calendar`, todas com `RequireAuthorization(SystemPolicies.Administrator)`:

| Rota | Corpo | Respostas |
| --- | --- | --- |
| `GET /{year}` | — | 200 `{ year, workingDays: DayOfWeek[], days: [{ date, kind, key, id, name, repeatsEveryYear, isConsidered }] }`; 400 com ano fora do intervalo |
| `PUT /working-days` | `{ workingDays: DayOfWeek[] }` | 204; 400 sem nenhum dia |
| `PUT /calculated-holidays/{key}` | `{ isConsidered }` | 204; 404 com chave desconhecida |
| `PUT /considered` | `{ isConsidered, calculatedHolidays: string[], localHolidays: Guid[] }` | 204; 404 com um feriado que não existe, sem nada mudar |
| `POST /local-holidays` | `{ name, date, repeatsEveryYear }` | 201 `{ id }`; 400 com os campos |
| `PUT /local-holidays/{id}` | `{ name, date, repeatsEveryYear, isConsidered }` | 204; 400; 404 |
| `DELETE /local-holidays/{id}` | — | 204; 404 |

- A data vai em `yyyy-MM-dd`.
- Os dias da semana vão pelo nome do `DayOfWeek`, como os outros enums da API.
- Os erros de campo usam as chaves `year`, `workingDays`, `name` e `date`.

### 5. Auditoria

O contexto base audita sozinho. O módulo só marca os rótulos:

| Item | Rótulo | Campos |
| --- | --- | --- |
| `WorkCalendar` | nenhum: a tela mostra "Calendário e feriados" | `WorkingDays`, como "Monday, Tuesday, …", traduzido na tela |
| `CalculatedHoliday` | `Key`, traduzido na tela pelo nome do feriado | `IsConsidered` |
| `LocalHoliday` | `Name` | `Name`, `Date`, `RepeatsEveryYear` e `IsConsidered` |

- No `CalculatedHoliday`, a chave nunca muda e fica fora da auditoria.
- `auditText.ts` ganha os títulos:
  - "Alterou os dias de trabalho";
  - "Considerou um feriado" e "Desconsiderou um feriado", quando só `IsConsidered` muda, também num feriado local;
  - "Incluiu um feriado", "Alterou um feriado" e "Excluiu um feriado".

  Os valores aparecem traduzidos: dias da semana, "Sim" e "Não", e datas em dd/mm/aaaa.
- O filtro da Auditoria ganha os tipos Dias de trabalho, Feriados calculados e Feriados locais.

### 6. Frontend

- **Rota:** `/admin/calendar` (`CalendarPage`), com o item "Calendário e feriados" (`CalendarMonthOutlined`) na seção Instalação.
- **Leitura:** `calendarYearQuery(year)`, chave `['calendar', year]`, com `placeholderData: keepPreviousData`. O ano começa no atual, no fuso da instalação (`useDates`).
- **Gravação na hora:** dias de trabalho e "Considerado" usam uma mutação com atualização otimista do cache.
  - Em erro, o cache volta e um aviso diz que não deu para salvar.
  - Em sucesso, um aviso curto diz "Calendário salvo".
  - Depois, invalida `['calendar']` e `['audit']`.
  - O último dia de trabalho marcado fica desabilitado.
- **Marcar todos:** um botão na barra da lista (`done_all`), que manda numa chamada só as datas da lista mostrada, no ano e no tipo escolhidos. Com todas já consideradas, o botão vira "Desmarcar todos". A atualização é otimista, como a das caixas. No celular, o botão fica no topo da lista, o tipo vai para a linha do ano e "Novo feriado" fica embaixo, na largura toda.
- **Lista:** o `DataTable`, com todas as datas do ano numa página (são menos de 50) e o rodapé "21 datas em 2026 · 19 consideradas".
  - Linhas desconsideradas apagadas (`isRowMuted`).
  - O filtro por tipo é feito na tela, sobre a lista do ano.
- **Diálogos:** "Novo feriado" e "Editar feriado", com React Hook Form e Zod (nome obrigatório até 80, data em dd/mm/aaaa válida, "Repete todo ano"), e a confirmação de exclusão.
- **Celular:** abaixo de `md`, os dias de trabalho em quatro colunas, o tipo num `Select` e a lista em linhas, como no canvas.
- **Nomes:** os nomes dos feriados calculados vêm das traduções, pela chave (`calendar.holidays.CarnivalMonday`). Os locais vêm do cadastro.

### 7. Data digitada, sem seletor em calendário

O campo de data aceita "dd/mm/aaaa" digitado.

- O MUI X Date Pickers está no ADR-0050, mas pede uma biblioteca de datas (date-fns, Day.js ou Luxon), uma escolha com alternativas que merece um ADR próprio. Ela fica para o cronograma, que tem muitos campos de data.
- O `input type="date"` do navegador mostraria a data no idioma do navegador, não no da aplicação.

### 8. Ids fixos das linhas criadas pela migração

- `WorkCalendar.SingleId` e os ids de cada `CalculatedHoliday` são GUIDs fixos no código da configuração, como o `InstallationSettings.SingleId`.
- Um feriado calculado novo, criado por lei, chega numa versão nova com a sua migração (ADR-0018, consequência "Ruim").

## Risks / Trade-offs

- [O governo muda um ponto facultativo de dia] → o administrador desconsidera a data calculada e cadastra a nova como feriado local (ADR-0083).
- [Gravar na hora deixa uma escolha errada salva sem confirmação] → cada clique entra na auditoria e se desfaz com outro clique. Os dias de trabalho não deixam a semana vazia.
- [A conta de dias úteis anda dia a dia] → os prazos e as durações são de dias e semanas, e o conjunto por ano evita recalcular feriados.
- [Os nomes dos feriados calculados ficam nas traduções, e não no banco] → o rótulo da auditoria guarda a chave, e a tela traduz. Uma chave sem tradução reprova a checagem de tipos.

## Migration Plan

- Migração `Initial` do módulo `Calendar`:
  - cria o schema `calendar` e as tabelas `work_calendar`, `calculated_holidays` e `local_holidays`;
  - insere a linha dos dias de trabalho (segunda a sexta) e as 17 escolhas com o padrão do ADR-0083.
- Sem dados a migrar. Voltar atrás é remover o módulo e o schema.
