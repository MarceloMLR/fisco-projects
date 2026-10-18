# Módulos do backend

Cada assunto do FiscoProjects é um módulo, com Clean Architecture por dentro (ADR-0047). Esta convenção é verificada pelos testes de arquitetura em `tests/FiscoProjects.Architecture.Tests`, que encontram os módulos pelo nome do assembly: um módulo novo passa a ser verificado sem mudar os testes.

## Estrutura

```
src/Modules/<Modulo>/
  FiscoProjects.<Modulo>/              o módulo
    Domain/                            regras de negócio; entidades herdam de FiscoProjects.SharedKernel.Entity
    Application/                       casos de uso (um por classe) e as interfaces que a infraestrutura implementa
    Infrastructure/                    EF Core, Microsoft Graph, e-mail e outras tecnologias
    Endpoints/                         rotas (Minimal APIs) e o registro do módulo no host
  FiscoProjects.<Modulo>.Contracts/    o que outros módulos podem usar: interfaces, dados e eventos de integração
tests/FiscoProjects.<Modulo>.Tests/    testes do módulo
```

- `<Modulo>` é uma palavra só, em inglês, em PascalCase e sem pontos (por exemplo, `Tickets`). Os nomes de código ficam em inglês, com a tradução dos termos em `docs/glossario.md` (ADR-0074). Os testes só reconhecem como módulo os assemblies `FiscoProjects.<Modulo>` e `FiscoProjects.<Modulo>.Contracts`; `Api`, `SharedKernel`, `Persistence` e `Testing` não são módulos.
- O namespace acompanha a pasta: `FiscoProjects.<Modulo>.Domain`, `FiscoProjects.<Modulo>.Application`, e assim por diante. Todo tipo do módulo fica numa das quatro camadas.

## Regras verificadas pelos testes

| Regra | Teste |
| --- | --- |
| `Domain` não usa nenhuma outra camada | Domínio usa uma camada externa |
| `Application` usa só `Domain` | Aplicação usa infraestrutura |
| `Endpoints` não usa `Infrastructure`, e o contrário também não | Endpoints usam infraestrutura direto |
| `Domain` e `Application` não usam EF Core, ASP.NET Core, SqlClient, Microsoft Graph, Microsoft.Identity nem Azure | Domínio usa biblioteca de banco ou web |
| Todo tipo do módulo fica numa das quatro camadas | Tipo fora das camadas |
| Um módulo usa de outro só o projeto `Contracts` | Módulo usa o interior de outro módulo |
| `Contracts` não usa o interior de nenhum módulo | Contrato usa o interior de um módulo |
| `SharedKernel` não usa módulos, ASP.NET Core nem EF Core | Núcleo compartilhado usa um módulo |
| `Domain` e `Application` não usam `FiscoProjects.Persistence` | Domínio usa a persistência compartilhada |
| `FiscoProjects.Persistence` não usa módulos | Persistência compartilhada usa um módulo |

## Referências entre projetos

- `FiscoProjects.<Modulo>` referencia o `FiscoProjects.SharedKernel`, o próprio `Contracts` e, se precisar, o `Contracts` de outros módulos. Nunca o projeto principal de outro módulo.
- Se o módulo guarda dados, referencia também o `FiscoProjects.Persistence` (usado só pela `Infrastructure`) e o pacote `Microsoft.EntityFrameworkCore.Design` com `PrivateAssets="all"`, para o `dotnet ef`.
- `FiscoProjects.<Modulo>.Contracts` referencia no máximo o `FiscoProjects.SharedKernel`.
- `FiscoProjects.Api` referencia o projeto principal de cada módulo, para registrá-lo.
- Se `Endpoints` ou `Infrastructure` precisarem do ASP.NET Core, o projeto do módulo declara `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- Framework, nullable, avisos como erro e versões de pacotes já vêm de `Directory.Build.props` e `Directory.Packages.props`; o `.csproj` do módulo não repete essas configurações.

## Registro no host

Na camada `Endpoints`, o módulo expõe dois métodos de extensão:

```csharp
namespace FiscoProjects.<Modulo>.Endpoints;

public static class <Modulo>Module
{
    public static IServiceCollection Add<Modulo>Module(this IServiceCollection services, IConfiguration configuration)
    {
        // casos de uso, repositórios e demais serviços do módulo
        return services;
    }

    public static RouteGroupBuilder Map<Modulo>Endpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/<recurso>");
        // group.MapGet(...), group.MapPost(...)
        return api;
    }
}
```

O host chama os dois em `src/FiscoProjects.Api/ApiSetup.cs`: `Add<Modulo>Module` em `AddFiscoProjectsApi` e `Map<Modulo>Endpoints` no grupo `/api` de `UseFiscoProjectsApi`. As rotas do módulo ficam então em `/api/<recurso>`.

## Passo a passo para criar um módulo

1. Criar os projetos, sem o `Class1.cs` gerado e sem as propriedades que já vêm do `Directory.Build.props`:
   ```
   dotnet new classlib -n FiscoProjects.<Modulo> -o src/Modules/<Modulo>/FiscoProjects.<Modulo>
   dotnet new classlib -n FiscoProjects.<Modulo>.Contracts -o src/Modules/<Modulo>/FiscoProjects.<Modulo>.Contracts
   ```
2. Criar as pastas `Domain`, `Application`, `Infrastructure` e `Endpoints` no projeto do módulo.
3. Criar `tests/FiscoProjects.<Modulo>.Tests`, no mesmo formato dos outros projetos de teste (xUnit v3, `OutputType` `Exe`).
4. Adicionar os projetos à solução com `dotnet sln FiscoProjects.slnx add`.
5. Referenciar o módulo no `FiscoProjects.Api` e registrá-lo em `ApiSetup.cs`.
6. Rodar `dotnet build FiscoProjects.slnx -warnaserror` e `dotnet test FiscoProjects.slnx`: os testes de arquitetura passam a verificar o módulo.

## Banco do módulo

Todos os módulos usam o mesmo banco (`ConnectionStrings:FiscoProjects`), cada um no próprio schema, com o próprio histórico de migrações (ADR-0047, ADR-0075). Um módulo nunca lê nem altera tabelas de outro.

O contexto fica em `Infrastructure/Persistence`, herda de `ModuleDbContext` e declara o schema: o nome do módulo em minúsculas.

```csharp
namespace FiscoProjects.<Modulo>.Infrastructure.Persistence;

internal sealed class <Modulo>DbContext(DbContextOptions<<Modulo>DbContext> options)
    : ModuleDbContext<<Modulo>DbContext>(options), IModuleSchema
{
    public static string Schema => "<modulo>";

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(<Modulo>DbContext).Assembly);
}

// Para o dotnet ef criar o contexto sem subir a API.
internal sealed class <Modulo>DbContextFactory : ModuleDesignTimeDbContextFactory<<Modulo>DbContext>
{
    protected override <Modulo>DbContext CreateContext(DbContextOptions<<Modulo>DbContext> options) => new(options);
}
```

- O registro em `Add<Modulo>Module` é `services.AddModuleDbContext<<Modulo>DbContext>(configuration);`. Ele já configura o schema do histórico, o assembly das migrações e as tentativas de novo.
- O contexto base já trata `DateTime` como UTC e marca o `Id` das entidades como gerado pela aplicação (GUID v7); a configuração das entidades não repete isso.
- Com tentativas de novo ligadas, uma transação aberta pelo código precisa da estratégia de execução:
  ```csharp
  var strategy = db.Database.CreateExecutionStrategy();
  await strategy.ExecuteAsync(async ct =>
  {
      await using var transaction = await db.Database.BeginTransactionAsync(ct);
      // alterações e SaveChangesAsync(ct)
      await transaction.CommitAsync(ct);
  }, cancellationToken);
  ```

### Migrações

Na raiz do repositório, depois de `dotnet tool restore`:

```
dotnet ef migrations add <Nome> --project src/Modules/<Modulo>/FiscoProjects.<Modulo> --output-dir Infrastructure/Persistence/Migrations
```

- Criar uma migração não precisa de banco. Os comandos que acessam o banco (`dotnet ef database update`, `dotnet ef migrations list`) leem a connection string da variável `ConnectionStrings__FiscoProjects`.
- No desenvolvimento, a API cria o banco e aplica as migrações de todos os módulos ao subir; para desligar, `Database:MigrateOnStartup=false`. Fora do desenvolvimento, as migrações vêm da publicação (ADR-0055).
- As migrações são código gerado: o `.editorconfig` não aplica regras de estilo nelas, e elas não são editadas à mão depois de publicadas.

### Testes com banco

Os testes usam um SQL Server de verdade em contêiner (Testcontainers, a mesma imagem do `compose.yaml`), por isso precisam do Docker rodando. O projeto de teste do módulo referencia `tests/FiscoProjects.Testing`, declara `[assembly: AssemblyFixture(typeof(SqlServerFixture))]` e recebe o `SqlServerFixture` no construtor; cada teste pede um banco próprio com `NewDatabase()`. Exemplos em `tests/FiscoProjects.Persistence.Tests`.
