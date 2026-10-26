## Context

O `IPeopleDirectory` do `Users` é hoje só o `LocalPeopleDirectory` (as pessoas da entrada local). A busca de pessoas (`ListPeople`) junta os usuários e o diretório, sem repetir quem já tem vínculo, e a inclusão num projeto cria o usuário pelo `UserFromDirectory`, com o vínculo do provedor do diretório. A entrada pelo Entra vincula o usuário por `entra` e o `oid` em minúsculas, e prova o aplicativo pelo `IEntraClientAssertion` (identidade gerenciada).

## Goals / Non-Goals

**Goals:**
- Buscar e incluir pessoas do tenant pelo Graph, com o mesmo vínculo da entrada.

**Non-Goals:**
- Fotos, cargos ou grupos do Entra; filtrar só quem está atribuído ao aplicativo; sincronizar nome e e-mail de quem já é usuário.

## Decisions

- **Token**: `IGraphTokens` (`Infrastructure`), com o `ClientAssertionGraphTokens`: um `ClientAssertionCredential` do Azure.Identity com o tenant, o cliente e o endereço da entrada (`EntraSignInOptions`) e a prova do `IEntraClientAssertion`, para o escopo `https://graph.microsoft.com/.default`; o Azure SDK guarda o token até perto de vencer. Nos testes, um token fixo.
- **Chamadas**: `GraphPeopleDirectory`, pelo `HttpClient` nomeado `graph` (`IHttpClientFactory`). A busca: `GET /v1.0/users` com `$search` em `displayName`, `mail` e `userPrincipalName` (o texto entre aspas, com `"` e `\` escapados), `$filter=userType eq 'Member' and accountEnabled eq true`, `$select=id,displayName,mail,userPrincipalName`, `$orderby=displayName`, `$top` e `$count=true`, com `ConsistencyLevel: eventual`; sem texto, a mesma consulta sem o `$search`. A pessoa: `GET /v1.0/users/{id}` com o `$select` mais `userType` e `accountEnabled`, e nula se não for membro ativo. O id vai em minúsculas, como o `oid` da entrada.
- **Diretório da empresa**: `CompanyDirectory` é o `IPeopleDirectory` registrado. Busca nos diretórios ligados (o de demonstração com a entrada local, o da Microsoft com a entrada pelo Entra), junta e ordena pelo nome e pelo e-mail, e encontra a pessoa pelo provedor do id.
- **Falhas**: no `CompanyDirectory`, uma falha de um diretório (`HttpRequestException`, resposta de erro, `AuthenticationFailedException`) vira um aviso no log com o provedor e o status, sem o texto buscado, e a busca segue com o resto; achar a pessoa para incluir devolve nula, e a inclusão responde como para uma pessoa que não existe.
- **Testes**: o `FakeGraph` (`FiscoProjects.Testing`) é um `HttpMessageHandler` que responde às duas consultas com pessoas fictícias, confere o token e o cabeçalho e guarda as consultas; o `FakeEntra` dos testes da API liga o Graph simulado junto com a entrada.

## Risks / Trade-offs

- [O Graph decide o que casa com o texto: começo das palavras do nome e começo do e-mail; os acentos seguem as regras dele] → a busca dos usuários continua sem acento, e o diretório só traz quem ainda não entrou.
- [Até o `apply` do Terraform, o aplicativo não tem a permissão] → o Graph responde 403, o aviso vai para o log e a busca segue com os usuários.
