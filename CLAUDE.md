# FiscoProjects

Regras deste projeto para o agente de código. As preferências gerais de Marcelo (CLAUDE.md global) continuam valendo; aqui ficam só as do FiscoProjects. Responda sempre em português do Brasil.

## 1. O que é

O FiscoProjects reúne projetos, cronogramas e chamados da equipe em um só lugar, apoiado no Microsoft 365 da empresa. Cada empresa tem a própria instalação (single-tenant, ADR-0003). É um projeto pessoal de Marcelo Ribeiro, que pode ser vendido a empresas (ADR-0002).

Na v1, o sistema faz: entrada pelo Entra ID, Meu trabalho, projetos e clientes, cronograma, chamados (inclusive os abertos em fornecedores), documentos, links, acessos a portais, termos de aceite, membros e permissões, avisos por e-mail e no sino, auditoria e administração.

Fronteiras que não se cruzam sem um ADR novo:

- Todo chamado pertence a um projeto; não existe service desk geral (ADR-0022).
- Só pessoas da empresa entram na v1; convidados são recusados (ADR-0007).
- Só os times Funcional e Desenvolvimento recebem chamados; não há filas (ADR-0068).
- Todo aceite é por PDF assinado fora do sistema (ADR-0035).
- Sem horas trabalhadas, orçamento, custos ou campos personalizados (ADR-0016, ADR-0020).

O escopo completo, com o que fica para depois, está em `docs/discovery.md`.

## 2. Arquitetura e regra de dependência

Monolito modular com Clean Architecture dentro de cada módulo, num único deploy (ADR-0047).

- Dentro do módulo: `Domain` não depende de nada externo (nem EF Core, Graph ou ASP.NET Core); `Application` depende só de `Domain`; `Infrastructure` e `Endpoints` dependem das camadas internas.
- Entre módulos: só o projeto `Contracts` do outro módulo (interfaces, dados e eventos de integração) ou eventos internos. Nunca referenciar o interior de outro módulo.
- Dados: cada módulo é dono das próprias tabelas, num schema próprio. Nenhum módulo lê ou altera tabelas de outro; consultas que cruzam módulos passam pelos contratos.
- Microsoft 365 sempre atrás de interfaces (`IEmailSender`, `IFileStorage`), com implementações locais para desenvolvimento e testes (ADR-0004).
- O servidor é a fonte da verdade das datas do cronograma (dias úteis, feriados, dependências, caminho crítico); o Gantt só exibe e envia alterações (ADR-0051).
- Testes de arquitetura no CI quebram quando essas regras são violadas.

## 3. Mapa do repositório

```
src/
  FiscoProjects.Api/                  host: API, autenticação BFF, serve o build do React
  FiscoProjects.SharedKernel/         o mínimo comum (tipos base, eventos)
  Modules/
    <Modulo>/
      FiscoProjects.<Modulo>/         Domain/ Application/ Infrastructure/ Endpoints/
      FiscoProjects.<Modulo>.Contracts/
web/                                  React + TypeScript
tests/                                testes do backend (unitários, integração, arquitetura, permissões)
infra/terraform/                      módulos e configuração por instalação e ambiente
infra/scripts/                        o que o Terraform não cobre (Exchange, SharePoint)
docs/adr/                             decisões (MADR), leitura obrigatória da área antes de mudar
docs/                                 discovery, apresentação, instalação, custos, design
openspec/                             config, specs e changes do OpenSpec
.claude/                              comandos /opsx e skills do OpenSpec, gerados pelo CLI
```

Os arquivos de `.claude/commands/opsx/` e `.claude/skills/openspec-*/` são gerados pelo `openspec init` e atualizados com `openspec update`; não edite à mão.

Ainda não há módulo de negócio; o primeiro nasce na Acesso-1, seguindo o guia `src/Modules/README.md`. Os módulos previstos (ADR-0047, com os nomes do ADR-0074) são Tickets, Projects, Documents, Credentials, Notifications, Audit e Administration; a lista final é decidida na change que cria cada um.

## 4. Convenções e não-negociáveis

- Idioma: código, nomes e comentários em inglês, inclusive módulos, termos do negócio, tabelas e rotas; a tradução de cada termo fica em `docs/glossario.md` (ADR-0074). Mensagens de commit e títulos e descrições de PR em português do Brasil (regra deste projeto, que substitui a do CLAUDE.md global). Specs, propostas, ADRs e docs em português do Brasil. Textos da interface e dos e-mails só em arquivos de tradução, com chaves em inglês (ADR-0013).
- C#: nullable habilitado, avisos tratados como erro, `.editorconfig` com `dotnet format` (ADR-0061).
- TypeScript em modo estrito, com ESLint e Prettier (ADR-0049, ADR-0061).
- API REST em JSON sob `/api`, contrato OpenAPI gerado pelo ASP.NET Core, tipos do frontend gerados do contrato, erros em Problem Details (RFC 9457), identificadores GUID v7; o número do chamado é sequencial (ADR-0029, ADR-0061).
- Datas e horas em UTC no banco, exibidas no fuso da instalação (ADR-0013).
- Versões fixadas: .NET 10; Node 24 até 28/10/2026 e Node 26 depois; números em `global.json`, `.nvmrc` e `engines` (ADR-0060). OpenSpec CLI 1.14.3 (`npm i -g @fission-ai/openspec@1.14.3`), com a telemetria desligada (`openspec config set telemetry.enabled false`).
- Licenças: só MUI gratuito; nada de FluentAssertions 8 ou superior (use AwesomeAssertions), MediatR ou AutoMapper; nenhuma dependência com licença comercial ou copyleft forte sem ADR (ADR-0050, ADR-0056, ADR-0061).
- Pacotes `@syncfusion/*` só dentro do componente `ScheduleGantt` (ADR-0051).
- Visual: siga o design system FiscoProjects e as telas aprovadas; nunca invente cor, espaço ou canto fora dos tokens. Status sempre por ícone, cor e texto, nunca só cor (ADR-0062 a 0067).
- IBM Plex Sans servida pela própria aplicação, sem Google Fonts (ADR-0064).

## 5. Diretrizes de desenvolvimento

- `async`/`await` de ponta a ponta, com `CancellationToken` nos casos de uso e no acesso a dados.
- Injeção de dependência nativa do ASP.NET Core.
- Um caso de uso por classe simples, com seus testes; sem MediatR. Mapeamento escrito à mão ou com Mapperly.
- SOLID e nomes claros; prefira código legível a código esperto.
- Sem duplicação: regras de calendário, prazos e permissões ficam num lugar só e são reaproveitadas.
- Frontend: tema central do MUI (variáveis CSS, temas claro e escuro) com os tokens do design system; wrappers próprios para componentes repetidos (por exemplo, a tabela padrão em pt-BR); React Hook Form com Zod nos formulários; Tiptap no editor dos chamados; react-i18next nos textos.
- Telas de trabalho na altura da janela: a página não rola, as áreas rolam por dentro (ADR-0066).
- Antes de usar a API de uma biblioteca, consulte a documentação atual (Context7).

## 6. Como rodar, buildar e testar

Pré-requisitos: SDK do .NET 10 (o `global.json` aceita qualquer 10.0 a partir do 10.0.100) e o OpenSpec CLI.

Backend, na raiz do repositório:

```
dotnet build FiscoProjects.slnx -warnaserror
dotnet test FiscoProjects.slnx
dotnet format FiscoProjects.slnx --verify-no-changes
dotnet run --project src/FiscoProjects.Api
```

- `dotnet format FiscoProjects.slnx` (sem `--verify-no-changes`) corrige a formatação.
- Com a API rodando no ambiente de desenvolvimento, a saúde fica em `/health` e o contrato em `/openapi/v1.json`.
- Ortografia: o `cspell.json` configura o Code Spell Checker (código em inglês; documentação em português e inglês). As extensões recomendadas do VS Code, inclusive o dicionário de português, estão em `.vscode/extensions.json`. Palavras do projeto entram na lista `words` do `cspell.json`.

Specs:

```
openspec validate --all --strict
```

Previstos para as próximas changes, e esta seção é atualizada quando existirem:

```
cd web && npm ci && npm run lint && npm run typecheck && npm test
terraform -chdir=infra/terraform fmt -check && terraform -chdir=infra/terraform validate
```

Desenvolvimento local com Docker (SQL Server), implementações locais de e-mail e arquivos e um tenant Microsoft pessoal, nunca o da empresa (ADR-0002, ADR-0004, ADR-0048).

## 7. Padrões recorrentes (reuse, não reinvente)

- Fila de e-mails no banco (outbox), enviada em segundo plano com novas tentativas; a ação do usuário não depende do envio (ADR-0036).
- Canais de aviso atrás de uma interface (e-mail, sino e, depois, Teams); as regras de quem recebe não conhecem o canal (ADR-0038).
- Calendário único da instalação para o cronograma e os prazos dos chamados (ADR-0018, ADR-0025).
- Fluxo de status dos chamados lido de tabela e validado (ADR-0024).
- Auditoria automática num ponto único da camada de dados, ao salvar (ADR-0039).
- Autorização pela relação da pessoa com o item e pelas matrizes dos ADR-0043 a 0046; testes de permissão gerados de uma tabela (ADR-0056).
- Processos em segundo plano dentro da aplicação (ADR-0047).
- Tudo o que varia por empresa é configuração ou cadastro (ADR-0003).

## 8. Fontes de verdade e documentação de referência

- Regras de negócio: ADRs em `docs/adr/` e specs em `openspec/specs/`. O que não está num ADR nem numa spec não é inventado: pergunte ou proponha.
- Pessoas: Entra ID. Papéis: banco do FiscoProjects. Arquivos: SharePoint. Feriados nacionais: calculados pelo sistema.
- Visual: design system FiscoProjects (https://claude.ai/artifact/CymvW4j1dqVrX3KVN13Jt7) e canvas de telas (https://claude.ai/artifact/WoMTFZKZXEpXzmPCCU4xfV), aprovados em 17/10/2026.
- Nunca use dados reais. Dados de demonstração e de testes são fictícios, ficam só no seed e nos testes, e nenhum nome de empresa, cliente ou fornecedor fica fixo no código (ADR-0002, ADR-0003).
- Antes de implementar uma área, leia os ADRs dela (tabela "Documentação de referência" em `docs/discovery.md`). Em infraestrutura e setup, leia também `docs/instalacao.md`.

## 9. Fluxo de trabalho

- OpenSpec: toda mudança, inclusive correções pequenas, começa por uma proposta (`/opsx:propose`).
  - Changes do roadmap (`docs/roadmap.md`): o agente propõe, avalia a proposta contra os ADRs e o design técnico e visual e, se estiver dentro, implementa (`/opsx:apply`) sem esperar aprovação da proposta. A aprovação de Marcelo é no PR.
  - Mudanças fora do roadmap: a proposta é aprovada por Marcelo antes de implementar.
- Quando parar e perguntar: só quando a decisão afeta o design técnico ou visual aprovado, envolve LGPD ou é exclusiva de Marcelo (produto, custo, licença, conta). Nos demais casos, decida, siga e liste as decisões tomadas no resumo final da change.
- Roadmap: as changes têm um ID por grupo (`Fundação-1`, `Acesso-2`...) e são feitas na ordem. Correção de uma change já arquivada ganha um subnúmero (`Fundação-2.1`), só quando necessário. O status de cada change é atualizado em `docs/roadmap.md` a cada etapa.
- Changes pequenas, com um objetivo funcional claro, em fatias verticais finas. Cada fatia termina com build sem avisos, testes passando e lint limpo.
- Decisão com alternativas reais vira ADR em `docs/adr/` antes de implementar. ADR aceito não muda de conteúdo: um ADR novo o substitui (ADR-0001).
- Testes: crie ou atualize os testes de toda mudança e rode-os antes de concluir. Cada cenário de spec tem um teste rastreável pelo nome; as matrizes de permissão são 100% cobertas (ADR-0056).
- Git, para cada change (esta ordem substitui, neste projeto, a do CLAUDE.md global):
  1. Sempre uma branch nova a partir da `main` atualizada: `git switch main`, `git pull`, `git switch -c tipo/nome-da-change`. Nunca escreva na `main` (a regra "Protect main" do GitHub também impede).
  2. Proposta e implementação na branch, com testes; commits pequenos em Conventional Commits.
  3. Push e PR com `gh pr create`; roadmap com "PR aberto". Avise Marcelo com o resumo.
  4. Depois da aprovação de Marcelo no chat (o GitHub não deixa o autor aprovar o próprio PR), rode o archive do OpenSpec (`/opsx:archive`) na mesma branch, atualize o roadmap para "Concluída" e faça push, para o PR levar código e specs juntos.
  5. Faça o merge do PR; se o merge não puder ser feito pela sessão, peça a Marcelo para fazê-lo no GitHub. Depois volte para a `main` (`git switch main`, `git pull`), apague a branch local com `git branch -d <branch>` (nunca `-D`) e a remota com `git push origin --delete <branch>`.
  6. Só então comece a próxima change, de novo pelo passo 1.
  - Nunca use `push --force` nem reescreva histórico publicado.
- Commits em Conventional Commits: `tipo(escopo): descrição`, no imperativo, em minúsculas, sem ponto final, até 72 caracteres. Tipos: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert. Quebra de compatibilidade com `!` e rodapé `BREAKING CHANGE:`. Títulos de PR no mesmo formato.
- Commits e PRs em português do Brasil: a descrição, o corpo do commit e o título e a descrição do PR. Tipo, `BREAKING CHANGE` e nomes de código continuam em inglês. Exemplo: `feat(chamados): adiciona troca do responsável em lote`.
- Sem linhas de atribuição: commits sem `Co-Authored-By` nem outros trailers de autoria do Claude, e PRs sem o rodapé "Generated with Claude Code" (preferência de Marcelo).

## 10. O que não fazer

- Não versionar segredos: nada de chaves, certificados, connection strings, chave do Syncfusion ou arquivos de estado do Terraform no repositório. Segredos ficam no Key Vault, nunca no Terraform (ADR-0052, ADR-0053).
- Não registrar senhas dos acessos, tokens ou dados pessoais em logs, URLs ou na auditoria; a auditoria registra só que a senha mudou (ADR-0033, ADR-0039, ADR-0057).
- Não mandar senha por e-mail (ADR-0033).
- Não expor tokens ao navegador: nada em `localStorage`, `sessionStorage` ou cookie legível por JavaScript (ADR-0054).
- Não conceder Mail.Send no Entra ID nem chamar APIs de permissão do SharePoint (ADR-0030, ADR-0036).
- Não usar App Roles do Entra para autorização (ADR-0006).
- Não acessar o Microsoft 365 real nos testes (ADR-0056).
- Não usar dados reais nem fixar nomes de empresa, cliente, fornecedor, pasta, modelo ou prazo no código (ADR-0002, ADR-0003).
- Não usar recursos, contas ou tenant do empregador (ADR-0002).
- Não pular a revisão: publicação em produção só pelo workflow manual, depois de revisar o plano (ADR-0055).
- Não trocar versões principais, inclusive a do OpenSpec CLI, sem pedir (ADR-0060).
