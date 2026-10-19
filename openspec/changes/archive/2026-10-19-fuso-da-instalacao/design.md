# Design: fuso da instalação (Acesso-7)

## Context

- **Configurações:** a Acesso-6 criou o módulo `Administration`, com `InstallationSettings` numa linha só, `PUT /api/installation` (nome) e a tela Configurações gerais com React Hook Form e Zod.
- **Datas no frontend:** ficam em `web/src/format/dates.ts`, com formatadores `Intl` no fuso do navegador:
  - `formatLastAccess`, `formatDayMonth`, `formatTime` e `formatDateTime`;
  - os dias do filtro de período: `toDayValue`, `startOfDayValue`, `nextDayValue` e `lastDays`.
- **Quem usa:** a Auditoria (lista, detalhes e período), os Usuários e Meu perfil.
- **Testes:** rodam com `TZ=America/Sao_Paulo` (`vite.config.ts`).
- **Canvas:** a seção Regras gerais tem um `Select` "Fuso horário" com o valor "Brasília (America/Sao_Paulo)".

## Goals / Non-Goals

**Goals:**

- Um lugar só para o fuso: o servidor guarda e confere, e o frontend formata tudo por um mesmo conjunto de funções no fuso da instalação.
- Nenhuma tela com sessão mostra uma hora no fuso errado, nem por um instante.

**Non-Goals:**

- Uma biblioteca de datas; o `Intl` do navegador basta para formatar e achar o deslocamento de um fuso.

## Decisions

### 1. Fuso IANA nas configurações, conferido pelo servidor

- `InstallationSettings.TimeZone`, texto de até 64 caracteres, com "America/Sao_Paulo" como padrão na classe e na migração `InstallationTimeZone`, que preenche a linha existente.
- `ChangeTimeZone` aceita só um fuso que o servidor conhece e que é IANA: `TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) && zone.HasIanaId`.
  - Um id do Windows ("E. South America Standard Time") é recusado, para o frontend, que usa o `Intl`, sempre entender o valor.
- O fuso entra na auditoria como texto, com o rótulo "Fuso horário" na tela.

Alternativa: guardar o deslocamento (UTC−03:00). Foi descartada, porque o deslocamento muda com o horário de verão de muitos fusos, e o ADR-0013 fala em fuso.

### 2. Um caso de uso para o formulário inteiro

- `RenameInstallation` vira `UpdateInstallation`, que recebe `InstallationChanges(Name, TimeZone)` e devolve os campos inválidos.
  - Sem nenhum campo inválido, aplica tudo e salva uma vez, num registro de auditoria só.
  - Com algum inválido, não muda nada.
  - A Acesso-8 acrescenta a cor principal no mesmo lugar.
- `PUT /api/installation` recebe `{ name, timeZone }` e responde 400 com o motivo em cada campo inválido.

### 3. O fuso só com sessão

- **`GET /api/installation/rules`** (qualquer pessoa com sessão) devolve `{ timeZone }`. A marca (`GET /api/installation`) continua só com o nome e o logo, porque a página de entrada não mostra datas, e o requisito "Marca visível sem sessão" não expõe outras configurações.
- **Contrato:** `IInstallationSettings` passa a devolver `InstallationInfo(Name, TimeZone)`, para os prazos e o resumo diário que vêm depois.

### 4. Frontend: datas num fuso e a estrutura esperando por ele

- **`format/dates.ts`** passa a ter `datesIn(timeZone)`, que devolve as mesmas funções de hoje nesse fuso:
  - formatadores `Intl.DateTimeFormat` com `timeZone`;
  - os dias (`DayValue`, "2026-10-16") calculados com as partes da data no fuso (`formatToParts`);
  - a meia-noite de um dia achada pelo deslocamento do fuso naquele instante, conferido duas vezes por causa do horário de verão.
- **Cache por fuso:** o resultado de `datesIn` fica guardado por fuso, então as funções mantêm a identidade e as colunas do `DataTable` não são redesenhadas à toa (ADR-0080).
- **Consulta e gancho:** `generalRulesQuery()` (chave `['installation', 'rules']`) e o gancho `useDates()`, que devolve `datesIn` do fuso da instalação ou do padrão America/Sao_Paulo, se a consulta falhar.
- **Estrutura esperando o fuso:** a `AppShell` mostra o mesmo "carregando" da sessão até a primeira resposta da consulta, ou a primeira falha. É uma resposta pequena, logo depois da sessão, e assim nenhuma hora aparece em outro fuso. Uma leitura seguinte nunca tira a tela, senão uma falha faria a estrutura e as telas aparecerem e sumirem sem parar.
- **Telas:** Auditoria, detalhes, período, Usuários e Meu perfil usam `useDates()` no lugar das funções soltas. As colunas memoizadas passam a depender das funções do fuso.
- **Depois de salvar:** as Configurações gerais invalidam a chave `['installation']`, que inclui `['installation', 'rules']`, e as telas passam ao novo fuso.

### 5. Campo Fuso horário

- **Busca:** o canvas mostra um `Select`, mas há mais de 400 fusos. O campo usa o mesmo controle com busca do filtro de pessoa da Auditoria (`Autocomplete` com a aparência do `Select`), com o rótulo "Fuso horário".
- **Opções:** vêm de `Intl.supportedValuesOf('timeZone')`, mais o fuso salvo se o navegador não o tiver.
  - Cada uma mostra o nome em português e o identificador, como "Horário de Brasília (America/Sao_Paulo)", com o nome que o navegador dá ao fuso (`Intl.DateTimeFormat('pt-BR', { timeZoneName: 'longGeneric' })`).
  - Ficam em ordem alfabética, e a busca acha pelo nome ou pelo identificador.
- **Formulário:** o fuso entra no formulário com Zod (obrigatório). Descartar o volta ao salvo, e um fuso recusado pela API aparece no campo.

## Risks / Trade-offs

- **[Nomes de fuso diferentes entre navegadores]** O nome por extenso vem do navegador e pode variar um pouco. → O identificador IANA vai sempre junto, e é ele que é salvo.
- **[Fuso que o navegador conhece e o servidor não]** Pode acontecer com bases de fusos de idades diferentes. → O servidor recusa com 400, e o campo mostra o motivo.
- **[Um instante a mais na abertura]** A estrutura espera a consulta do fuso. → É uma resposta pequena, e só na abertura; depois fica guardada na sessão (ADR-0080).

## Migration Plan

- A migração `InstallationTimeZone` acrescenta a coluna com o padrão America/Sao_Paulo; a linha existente fica com ele.
- Para voltar atrás, reverta o PR antes de publicar.
