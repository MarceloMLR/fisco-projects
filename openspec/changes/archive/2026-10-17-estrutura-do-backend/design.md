# Design: estrutura do backend (Fundação-1)

## Context

O repositório só tem documentação (ver proposal.md, "Why"). A estrutura alvo está no ADR-0047; as convenções de API e código, no ADR-0061; a política de versões, no ADR-0060; as ferramentas de teste, no ADR-0056. Ambiente verificado: SDK do .NET 10.0.401 na máquina de Marcelo e 10.0.402 no ambiente de nuvem.

## Goals / Non-Goals

**Goals:**

- Deixar pronto o esqueleto em que o primeiro módulo (Acesso-1) só precisa ser criado, sem decidir estrutura de novo.
- Fazer as regras de dependência do ADR-0047 quebrarem o teste no momento da violação, inclusive para módulos que ainda não existem.
- Dar à API o comportamento comum (saúde, erros, contrato, prefixo) num único lugar do host.

**Non-Goals:**

- Mecanismo genérico de descoberta de módulos por reflexão: o host chama cada módulo explicitamente.
- Validação de entrada das rotas e autorização: entram com as primeiras rotas de negócio.

## Decisions

### 1. Camadas como pastas e namespaces dentro do projeto do módulo

Cada módulo é um projeto `FiscoProjects.<Modulo>` com as pastas `Domain/`, `Application/`, `Infrastructure/` e `Endpoints/` (namespaces `FiscoProjects.<Modulo>.Domain` etc.), mais o projeto `FiscoProjects.<Modulo>.Contracts`, exatamente como no ADR-0047.

- O compilador garante a fronteira entre módulos: outros módulos só referenciam o projeto `Contracts`.
- Os testes de arquitetura garantem a fronteira entre camadas, por namespace.
- Alternativa: um projeto por camada (`.Domain`, `.Application`...). O compilador garantiria também as camadas, mas seriam 5 projetos por módulo, cerca de 50 na v1, sem ganho que os testes não cubram.

### 2. Minimal APIs para as rotas

As rotas ficam em `Endpoints/`, como Minimal APIs agrupadas por módulo.

- Combinam com "um caso de uso por classe" (ADR-0061): cada rota chama um caso de uso.
- O OpenAPI nativo do ASP.NET Core 10 descreve Minimal APIs sem pacote extra.
- Alternativa: controllers do MVC. Trazem filtros e convenções que não usaremos, e mais cerimônia por rota.

### 3. Registro explícito dos módulos no host

Cada módulo expõe, na camada `Endpoints`, dois métodos de extensão públicos: `Add<Modulo>Module(IServiceCollection, IConfiguration)` e `Map<Modulo>Endpoints(RouteGroupBuilder)`. O host chama os dois para cada módulo e passa o grupo `/api`.

- Simples de ler e de depurar; nada acontece por reflexão.
- Alternativa: descoberta automática de módulos por reflexão. Esconde a ordem de registro e não traz ganho com cerca de 10 módulos.
- O guia `src/Modules/README.md` descreve a convenção para a Acesso-1 seguir.

### 4. Host com composição reaproveitável nos testes

O `Program.cs` só chama `builder.AddFiscoProjectsApi()` e `app.UseFiscoProjectsApi()`, definidos no host. Os testes de integração usam o `WebApplicationFactory` para o host real e, para os cenários que precisam de uma rota de teste (erro inesperado e grupo `/api`), montam um `WebApplication` com as mesmas duas chamadas mais a rota de teste.

- Testa a configuração real do pipeline, sem colocar rotas de teste no código de produção.
- Alternativa: rota de diagnóstico ligada por configuração. Seria código de teste em produção.

### 5. Problem Details em todos os ambientes

`AddProblemDetails()`, `UseExceptionHandler()` e `UseStatusCodePages()` valem em qualquer ambiente, inclusive desenvolvimento. O manipulador de exceções roda antes da página de exceções de desenvolvimento, então a resposta é a mesma em todos os ambientes e nunca traz a exceção.

- Alternativa: página de exceção detalhada no desenvolvimento. Faz o comportamento depender do ambiente e pode vazar detalhes se o ambiente for configurado errado.

### 6. OpenAPI só em tempo de execução, só no desenvolvimento

`AddOpenApi()` e `MapOpenApi()` com o documento `v1` em OpenAPI 3.1 (padrão do .NET 10), mapeado só quando o ambiente é `Development`, como recomenda a documentação da Microsoft.

- A geração no build (`Microsoft.Extensions.ApiDescription.Server`) entra na Fundação-4, quando o frontend precisar gerar os tipos.

### 7. ArchUnitNET para os testes de arquitetura

O ADR-0056 permite NetArchTest ou ArchUnitNET. Escolhido o ArchUnitNET (Apache 2.0): versão 0.13.4, mantido ativamente e com adaptador para xUnit v3. O NetArchTest.Rules não tem versão desde 2021.

As regras ficam em funções reaproveitáveis que recebem o namespace raiz de um módulo e são aplicadas de duas formas:

- a todo módulo real, descoberto pelo nome do assembly (`FiscoProjects.<Modulo>` e `FiscoProjects.<Modulo>.Contracts`, excluindo `Api`, `SharedKernel` e testes). A lista de assemblies vem do manifesto de dependências do projeto de testes (`Microsoft.Extensions.DependencyModel`), que inclui o host e tudo o que ele referencia, e não da pasta `bin`, que pode ter arquivos antigos de módulos removidos;
- a exemplos de módulos válidos e inválidos, escritos no próprio projeto de testes, que provam que cada regra aceita o certo e pega a violação. Isso também cobre o período em que ainda não há módulo de negócio.

As bibliotecas proibidas em `Domain` e `Application` são identificadas por namespace: `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `Microsoft.Graph`, `Azure` e `Microsoft.Data.SqlClient`.

### 8. Ferramentas de teste

xUnit v3, AwesomeAssertions e NSubstitute (ADR-0056), com `Microsoft.AspNetCore.Mvc.Testing` para os testes de integração. Os testes rodam no Microsoft Testing Platform, ligado no `global.json`: o xUnit v3 atual não roda mais no `dotnet test` antigo (VSTest) com o SDK do .NET 10. O NSubstitute entra no primeiro teste que precisar de dublê. O nome do método de teste fica em inglês; o `DisplayName` do teste é o nome exato do cenário da spec, para rastrear spec e teste (ADR-0056).

### 9. Configuração de build na raiz

- `global.json`: SDK `10.0.100` com `rollForward: latestFeature`. Aceita qualquer SDK 10.0 a partir do 10.0.100 e nunca o 11, cumprindo "fixar o .NET 10" (ADR-0060) sem travar correções.
- `Directory.Build.props`: `net10.0`, nullable, usings implícitos, avisos como erro, `EnforceCodeStyleInBuild` e `AnalysisLevel` mais recente.
- `Directory.Packages.props`: versões de pacotes centralizadas, para cada pacote ter uma versão só na solução.
- `.editorconfig` com as convenções de C# e `dotnet format --verify-no-changes` limpo.
- Solução no formato `.slnx`, o padrão do SDK do .NET 10.

### 10. Entidade base no SharedKernel

`Entity` é uma classe abstrata com `Id` do tipo `Guid`, preenchido com `Guid.CreateVersion7()` na criação e só atribuível na inicialização. Igualdade por identificador e eventos de domínio entram quando um módulo precisar.

## Risks / Trade-offs

- [Nenhum módulo real ainda; as regras rodam só sobre os exemplos] → Os exemplos cobrem cada regra nos dois sentidos, e a descoberta por nome aplica as regras ao primeiro módulo sem mudar os testes.
- [ArchUnitNET ainda não chegou à versão 1.0; a API pode mudar] → Versão fixada no `Directory.Packages.props`; atualização só como change própria (ADR-0060).
- [Avisos como erro e regras de estilo no build podem travar o desenvolvimento por detalhes] → `.editorconfig` com as convenções padrão do .NET e `dotnet format` para corrigir automaticamente.
- [A regra de camadas por namespace depende de os arquivos estarem nas pastas certas] → A convenção está no guia de módulos, e um tipo fora das quatro camadas é apontado pelos testes de arquitetura.

## Migration Plan

Não há migração: é o primeiro código do repositório. Para desfazer, basta reverter o PR.

## Open Questions

Nenhuma. O idioma dos nomes dos módulos foi decidido por Marcelo em 17/10/2026, durante a revisão desta change: tudo em inglês, com glossário português–inglês (ADR-0074).
