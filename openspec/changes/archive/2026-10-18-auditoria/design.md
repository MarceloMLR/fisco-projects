# Design: auditoria (Acesso-3)

## Context

Motivação em proposal.md. Cada módulo tem o próprio contexto do EF Core, derivado de `ModuleDbContext` (`FiscoProjects.Persistence`, ADR-0075), com schema próprio e tentativas de novo em falhas passageiras (`EnableRetryOnFailure`). O `Persistence` não tem tabelas e não depende de módulos; os testes de arquitetura garantem. Só existe o módulo `Users`: o usuário (nome, e-mail, papel, situação, criação, último acesso) e o vínculo com o provedor. A troca de papel e a desativação já rodam numa transação `Serializable` dentro da estratégia de novas tentativas. A entrada local (`SignInLocally` → `SignInWithIdentity`) cria o usuário na primeira entrada, sem sessão. O frontend tem a área de Administração com a seção Usuários e a tabela padrão (`DataTable`).

## Goals / Non-Goals

**Goals:**

- Um módulo novo fica auditado sem escrever código de auditoria: no máximo marca, na configuração de banco, o rótulo do item e os campos técnicos ou secretos.
- Alteração e registro gravados juntos, também quando a alteração já roda numa transação do módulo.
- `Persistence` continua sem tabelas e sem depender de módulos; só o módulo `Audit` grava e lê as tabelas da auditoria.

**Non-Goals:**

- Registrar leituras ou alterações feitas fora do EF Core (por exemplo, `ExecuteUpdate`). Hoje só os testes usam isso; o guia de módulos passa a dizer que alterações de negócio vão pelo `SaveChangesAsync`.

## Decisions

### 1. Captura no contexto base, gravação na mesma transação

`ModuleDbContext.SaveChangesAsync` passa a:

1. ler o `ChangeTracker` antes de salvar e montar a lista de alterações (`AuditedChange`: módulo, tipo e id do item, ação, rótulo e campos com antes e depois);
2. se não houver alteração auditada, salvar como antes;
3. se o contexto já estiver numa transação (como a `Serializable` dos papéis), salvar e gravar o registro nela;
4. senão, abrir uma transação dentro da estratégia de novas tentativas (`CreateExecutionStrategy`, como no `ResilientTransaction` da documentação do EF Core), salvar sem aceitar as mudanças, gravar o registro, confirmar e só então aceitar as mudanças. Numa falha passageira, tudo é repetido.

A gravação fica atrás da interface `IChangeAuditor`, definida no `Persistence` e implementada pelo módulo `Audit`. O contexto base a obtém dos serviços da aplicação, pelas opções do contexto (`CoreOptionsExtension.ApplicationServiceProvider`). Sem implementação registrada, como nos testes de um módulo isolado, o contexto salva sem auditoria. O `SaveChanges` síncrono passa a recusar a chamada (async de ponta a ponta), para não haver caminho sem auditoria.

- Alternativa: mapear a tabela da auditoria em todo contexto (com `ExcludeFromMigrations`) e acrescentar os registros no mesmo `SaveChanges`. Mais simples no EF, mas faz todo módulo gravar numa tabela do módulo `Audit` e põe a forma dessa tabela no `Persistence`, contra o ADR-0047 e o ADR-0075.
- Alternativa: interceptor de `SaveChanges`. Não consegue abrir a transação em volta do salvamento quando há estratégia de novas tentativas.
- Alternativa: gravar depois, por evento. Não é atômico: uma alteração salva poderia ficar sem registro.

### 2. Marcações de auditoria na configuração do módulo

Métodos de extensão do `Persistence`, usados no `Infrastructure` de cada módulo, gravam anotações no modelo do EF:

- `HasAuditLabel(x => x.Name)`: o campo que identifica o item na tela (o nome da pessoa, o título da tarefa);
- `ExcludeFromAudit()`, no tipo ou no campo: fica de fora (o vínculo com o provedor, o último acesso, a data de criação);
- `IsAuditSecret()`: o registro diz só que o campo mudou, sem antes nem depois (a senha de um acesso, ADR-0033).

A chave (`Id`) não entra nos campos, porque já é a identificação do item. Os valores são gravados como texto, em formato invariável (datas em ISO 8601 UTC, enums pelo nome, booleanos `true`/`false`), e a tela traduz.

No `Users`: o usuário tem o nome como rótulo e deixa de fora `LastSignInAt` e `CreatedAt`; o vínculo com o provedor fica todo de fora. Assim a entrada de todo dia não gera "alterou um usuário"; ela já é o evento de entrada.

### 3. Autor

O módulo `Audit` lê o autor da requisição atual (`IHttpContextAccessor` e `UserSession.UserIdOf`, do contrato do `Users`). Sem sessão, o autor fica vazio e a tela mostra Sistema. O registro guarda só o id do autor; o nome vem do contrato novo `IUserDirectory` do `Users` na hora da consulta, o que também vale para a futura anonimização (ADR-0058).

### 4. Eventos de segurança

- Contrato `ISecurityEvents` em `FiscoProjects.Audit.Contracts`, com a entrada (`SignedInAsync`: pessoa e provedor) e a entrada recusada (`SignInRefusedAsync`: identidade usada e motivo, `NotInDirectory` ou `Inactive`).
- `SignInWithIdentity` registra a entrada depois de salvar o usuário, e a recusa de conta desativada; `SignInLocally` registra a recusa de quem não está no diretório. A identidade é guardada como foi digitada, sem espaços nas pontas e com até 256 caracteres.
- Troca de papel, desativação e reativação são alterações do usuário (`Role` e `IsActive`) e entram pelo registro automático, na mesma transação `Serializable`, sem código no `Users`.
- O evento de entrada é gravado numa transação própria, logo depois da entrada. É um fato à parte da alteração do usuário.

### 5. Módulo `Audit`

- Domínio: `AuditEntry` imutável, com data e hora, autor (opcional), ação (`Created`, `Updated`, `Deleted`, `SignedIn`, `SignInRefused`), tipo do item (`User`, `SignIn`...), id e rótulo do item, detalhe (a forma de entrada ou o motivo da recusa), módulo de origem e campos (`Field`, `Before`, `After`, `Secret`).
- Banco: schema `audit`, tabela `entries`, com os campos alterados numa coluna JSON (coleção complexa do EF Core 10) e índices por data, por autor e data e por tipo e data.
- O contexto da auditoria não é auditado e recusa gravar registro alterado ou excluído: só acrescenta.
- Caso de uso `ListAuditEntries` com filtros por autor, tipo e intervalo (início inclusivo, fim exclusivo), do mais novo para o mais antigo, com paginação.

### 6. API

| Rota | Quem | Resposta |
| --- | --- | --- |
| `GET /api/audit?actorId=&itemType=&from=&to=&page=&pageSize=` | administrador | 200 com `items` e `total`; 400 para página inválida ou período invertido |

- `from` e `to` são instantes com fuso (`DateTimeOffset`): a tela converte o primeiro dia e o dia seguinte ao último no fuso do navegador, até a Acesso-5.
- Cada item: id, data e hora, autor (`{ id, name }` ou nulo), ação, tipo, id e rótulo do item, detalhe e campos. O detalhe vem na própria lista, sem rota de detalhe.
- Nenhuma rota de alteração ou exclusão sob `/api/audit`.

### 7. Frontend

- Seção Auditoria (ícone `HistoryOutlined`) em "Pessoas e segurança", rota `/admin/audit`, trilha "Administração / Auditoria".
- Layout do canvas: a lista e, a partir de 1200px, o painel lateral de 340px com o registro escolhido (o primeiro da página, até a pessoa escolher outro). Abaixo de 1200px, o painel abre numa gaveta ao tocar no registro.
- Colunas Quando (dia e hora), Quem (avatar, ou o avatar tracejado de Sistema, como o `Person` vazio do design system) e O quê (ação em destaque e o resumo do item). Rodapé "1–25 de 312 no período".
- Filtros:
  - pessoa: busca na lista de usuários enquanto se digita;
  - tipo de item: Entradas e Usuários por enquanto;
  - período: campo com ícone de calendário que abre dois campos de data (De e Até), começando nos últimos 30 dias.
- O texto de cada registro é montado num lugar só (`describeAuditEntry`), com as chaves de tradução por ação e tipo e os casos especiais do usuário: papel ("Trocou um papel · Beatriz Lima · Usuário → Gerente de projeto"), situação ("Desativou um usuário") e entrada recusada ("joao.silva@... · fora do diretório").
- `DataTable` ganha a linha selecionada e o texto do rodapé configurável.

### 8. Testes

- `Persistence`: com SQL Server, criar, alterar e excluir geram as alterações certas; campo técnico e item fora da auditoria não geram; campo secreto vai sem valor; falha ao gravar o registro desfaz a alteração; falha na alteração não grava registro; transação do módulo é reaproveitada.
- `Audit`: gravação só acrescenta; consulta com filtros, ordem e paginação; eventos de segurança.
- `Users`: eventos de entrada e de recusa.
- API: matriz de permissões da consulta gerada de uma tabela (ADR-0056); troca de papel, desativação e reativação com o administrador como autor; usuário criado na primeira entrada com Sistema; entrada e recusa; sem rota de alteração ou exclusão.
- Frontend, com MSW: seção só para o administrador, lista, filtros, período, detalhes e textos dos registros.

## Risks / Trade-offs

- [O volume cresce com o uso, e cada entrada vira um registro] → Índices por data, paginação no servidor e o período padrão de 30 dias. A limpeza pelo prazo de retenção vem com a governança (ADR-0058).
- [Antes e depois guardam dados pessoais, como nome e e-mail] → Só o administrador consulta. A retenção e a anonimização ficam no grupo de governança, como decidido no ADR-0058.
- [Alterações feitas com `ExecuteUpdate`/`ExecuteDelete` ou SQL direto não são auditadas] → O guia de módulos proíbe esse caminho para dados de negócio.
- [O evento de entrada fica fora da transação do usuário] → Se ele falhar, a entrada falha e pode ser repetida. Uma entrada sem evento não gera estado errado.
- [A busca de pessoas usa a lista de usuários, só do administrador] → É a mesma pessoa que vê a auditoria.

## Migration Plan

Migração `Initial` do módulo `Audit` (schema `audit`), aplicada pelo `ModuleMigrator` como as outras. Não há dados anteriores a migrar: a auditoria começa a contar a partir da implantação. Para desfazer, basta reverter o PR; as tabelas do schema `audit` podem ficar.
