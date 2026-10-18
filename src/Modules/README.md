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

- `<Modulo>` é uma palavra só, em inglês, em PascalCase e sem pontos (por exemplo, `Tickets`). Os nomes de código ficam em inglês, com a tradução dos termos em `docs/glossario.md` (ADR-0074). Os testes só reconhecem como módulo os assemblies `FiscoProjects.<Modulo>` e `FiscoProjects.<Modulo>.Contracts`; `Api` e `SharedKernel` não são módulos.
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

## Referências entre projetos

- `FiscoProjects.<Modulo>` referencia o `FiscoProjects.SharedKernel`, o próprio `Contracts` e, se precisar, o `Contracts` de outros módulos. Nunca o projeto principal de outro módulo.
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

O banco de cada módulo (schema próprio) entra com a Fundação-2.
