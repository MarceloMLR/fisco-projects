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

O módulo expõe dois métodos de extensão, um em cada camada externa, porque `Endpoints` e `Infrastructure` não se usam:

```csharp
namespace FiscoProjects.<Modulo>.Infrastructure;

public static class <Modulo>Module
{
    public static IServiceCollection Add<Modulo>Module(this IServiceCollection services, IConfiguration configuration)
    {
        // contexto do banco, implementações das interfaces da Application e casos de uso
        return services;
    }
}
```

```csharp
namespace FiscoProjects.<Modulo>.Endpoints;

public static class <Modulo>Endpoints
{
    public static RouteGroupBuilder Map<Modulo>Endpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/<recurso>");
        // group.MapGet(...), group.MapPost(...)
        return api;
    }
}
```

O host chama os dois em `src/FiscoProjects.Api/ApiSetup.cs`: `Add<Modulo>Module` em `AddFiscoProjectsApi` e `Map<Modulo>Endpoints` no grupo `/api` de `UseFiscoProjectsApi`. As rotas do módulo ficam então em `/api/<recurso>`. As rotas recebem os casos de uso da `Application` por injeção de dependência. O relógio é o `TimeProvider` registrado no host, que os testes trocam por um `FakeTimeProvider`.

## Passo a passo para criar um módulo

1. Criar os projetos, sem o `Class1.cs` gerado e sem as propriedades que já vêm do `Directory.Build.props`:
   ```
   dotnet new classlib -n FiscoProjects.<Modulo> -o src/Modules/<Modulo>/FiscoProjects.<Modulo>
   dotnet new classlib -n FiscoProjects.<Modulo>.Contracts -o src/Modules/<Modulo>/FiscoProjects.<Modulo>.Contracts
   ```
2. Criar as pastas `Domain`, `Application`, `Infrastructure` e `Endpoints` no projeto do módulo.
3. Criar `tests/FiscoProjects.<Modulo>.Tests`, no mesmo formato dos outros projetos de teste (xUnit v3, `OutputType` `Exe`).
4. Adicionar os projetos à solução com `dotnet sln FiscoProjects.slnx add`.
5. Referenciar o módulo no `FiscoProjects.Api` e registrá-lo em `ApiSetup.cs`. O `FiscoProjects.Users` é o exemplo de um módulo completo.
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

### Auditoria

Toda gravação pelo `SaveChangesAsync` do contexto do módulo é auditada pelo contexto base, na mesma transação da alteração (ADR-0039, ADR-0075): criação, alteração e exclusão, com quem fez, quando e os campos com o valor antes e depois. O módulo não escreve código de auditoria; só marca, na configuração das entidades, o que não é o padrão:

```csharp
user.HasAuditLabel(u => u.Name);                       // o que identifica o item na tela de Auditoria
user.Property(u => u.LastSignInAt).ExcludeFromAudit(); // campo técnico: fica de fora
credential.Property(c => c.Password).IsAuditSecret();  // registra só que mudou, nunca o valor (ADR-0033)
link.ExcludeFromAudit();                                // item técnico inteiro de fora
```

- Dados de negócio mudam só pelo `SaveChangesAsync`: `ExecuteUpdate`, `ExecuteDelete` e SQL direto não passam pela auditoria. O `SaveChanges` síncrono é recusado.
- Uma transação aberta pelo módulo (dentro da estratégia de execução) leva junto os registros da auditoria.
- Eventos de segurança que não são alteração de um item (a entrada, a entrada recusada) vão pelo contrato `ISecurityEvents` do módulo `Audit`.
- Nos testes de um módulo isolado, sem o módulo `Audit` registrado, o contexto salva sem auditoria.

### Arquivos

Arquivos (anexos, documentos, termos, logos) vão sempre pelo contrato `IFileStorage` do módulo `Files` (ADR-0079), nunca direto para o disco ou o Microsoft Graph:

- O módulo guarda no próprio banco os ids devolvidos (pasta e arquivo) e o que importa para ele (quem enviou, a que item pertence, status); o `Files` não tem tabelas.
- `EnsureFolderAsync` cria as pastas que faltam no caminho; `UploadAsync` escolhe, para o mesmo nome, entre `NewVersion` (documentos) e `KeepBoth` (anexos).
- Limite de tamanho e tipos recusados são regras de cada uso e ficam no módulo que recebe o arquivo.
- Para excluir, `DeleteFileAsync` remove o arquivo com as versões, e `DeleteFolderIfEmptyAsync` só remove pasta vazia, para nunca apagar o que o FiscoProjects não criou (ADR-0041).
- Exemplo: o logo da instalação no módulo `Administration` (`ReplaceInstallationLogo`), conferido pelo conteúdo, gravado antes do registro e excluído se o registro falhar.

### Testes com banco

Os testes usam um SQL Server de verdade em contêiner (Testcontainers, a mesma imagem do `compose.yaml`), por isso precisam do Docker rodando. O projeto de teste do módulo referencia `tests/FiscoProjects.Testing`, declara `[assembly: AssemblyFixture(typeof(SqlServerFixture))]` e recebe o `SqlServerFixture` no construtor; cada teste pede um banco próprio com `NewDatabase()`. O `TestDatabase` registra o módulo como o host faz, aplica as migrações e dá contextos e escopos novos. Exemplos em `tests/FiscoProjects.Users.Tests` e `tests/FiscoProjects.Persistence.Tests`.
