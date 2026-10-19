# Design: clientes (Projetos-2)

## Context

- **Módulos:** `Users`, `Files`, `Audit`, `Administration` e `Calendar`. Ainda não existe o módulo dos projetos.
- **Discovery:** "Projetos e clientes" ficam no módulo Projetos (tabela "O que faz (v1)").
- **Logo da instalação:** o `Administration` já tem o fluxo do logo da instalação, que serve de modelo:
  - conferência pelo conteúdo;
  - envio pelo `IFileStorage` na pasta "FiscoProjects", depois o registro;
  - endereço com `?v=` e cache imutável;
  - cabeçalhos que impedem o navegador de trocar o tipo.
- **Autorização:** só existe a política `SystemPolicies.Administrator`. O ADR-0043 dá a criação de projetos ao administrador e ao gerente de projeto.
- **Lista de Usuários:** busca sem diferenciar acentos (`Latin1_General_100_CI_AI`), filtros e paginação no servidor. A tela usa o `DataTable` com o menu por linha.
- **Componentes:** a Projetos-1 criou o `Checkbox` e o `Dialog` no frontend.
- **Canvas:** a tela AdminClientes mostra a lista com logo ou iniciais (`fp-logo-tile`), CNPJ, projetos ativos, situação e observação, a busca, o filtro de situação e "Novo cliente". O diálogo do cliente não está desenhado e segue o "Novo feriado".

## Goals / Non-Goals

**Goals:**

- Os clientes já no módulo que terá os projetos, para a Projetos-3 ligar projeto e cliente e recusar a exclusão sem atravessar módulos.
- Uma só regra de CNPJ no servidor, espelhada na tela só para avisar antes de salvar.

**Non-Goals:**

- Contrato para outros módulos: nenhum outro módulo lê clientes nesta change.

## Decisions

### 1. Módulo `Projects`, com os clientes

- Projetos `FiscoProjects.Projects` e `FiscoProjects.Projects.Contracts`, schema `projects`, tabela `clients`.
- A Projetos-3 acrescenta os projetos no mesmo módulo. A regra "cliente com projetos não é excluído" vira uma consulta do próprio módulo.

Alternativas:

- Um módulo `Clients`: a exclusão dependeria de um contrato dos projetos, e o cliente do projeto, de um contrato dos clientes, com dependência nos dois sentidos.
- O `Administration`: ele guarda as configurações gerais, e o cliente é dado do negócio que os projetos usam.

### 2. Política "quem cria projetos"

- `SystemPolicies.ProjectCreators` (administrador ou gerente de projeto), registrada no `Users` como a do administrador, com o papel lido do banco a cada requisição.
- Ela vale para listar e incluir clientes. As outras rotas de alteração usam `SystemPolicies.Administrator`. Abrir o logo pede só sessão.
- O gerente inclui o cliente sem logo. O logo é do administrador, como o resto da edição (ADR-0046).

### 3. Domínio

- **`Client`:**
  - `Name` (até 150), `Cnpj` (14 caracteres ou vazio), `Observation` (até 500) e `IsActive`;
  - os campos do logo: `LogoFileId`, `LogoFileName`, `LogoContentType` e `LogoChangedAt`;
  - os métodos `Change`, `Deactivate`, `Reactivate`, `ReplaceLogo` e `RemoveLogo`.
- **`Cnpj`** (estático):
  - `TryNormalize` tira tudo que não é letra nem algarismo, passa para maiúsculas e confere:
    - 12 caracteres de `[0-9A-Z]` e 2 algarismos;
    - os dígitos verificadores pelo módulo 11, com o valor de cada caractere igual ao código ASCII menos 48 (IN RFB nº 2.229/2024; vale também para o CNPJ só numérico);
    - nem todos os caracteres iguais;
  - `Format` devolve `XX.XXX.XXX/XXXX-XX`.
- **`ClientLogo.Check`:**
  - PNG pela assinatura de 8 bytes e JPG pelo começo `FF D8 FF`, até 1 MB;
  - recusa, com o motivo, o formato e o tamanho.

  É a mesma ideia do `LogoFile` da instalação, sem o SVG.

### 4. Aplicação

- **Casos de uso:**
  - `ListClients` (busca, situação e página);
  - `AddClient`, `ChangeClient`, `DeactivateClient`, `ReactivateClient` e `RemoveClient`;
  - `GetClientLogo`, `ReplaceClientLogo` e `RemoveClientLogo`.
- **CNPJ repetido:** `AddClient` e `ChangeClient` conferem o CNPJ antes de salvar e devolvem o cliente que já o usa. O índice único filtrado (`Cnpj IS NOT NULL`) garante a regra numa corrida, e a violação vira a mesma resposta.
- **Arquivo do logo:**
  - pasta "FiscoProjects", nome `client-{id}.png` ou `.jpg`, com `SameNameAction.NewVersion`;
  - enviar primeiro e registrar depois; se o registro falhar, o arquivo novo é apagado;
  - trocar o formato apaga o arquivo antigo.
- **Excluir:** primeiro apaga o registro e depois o arquivo do logo. Uma falha ao apagar o arquivo não desfaz a exclusão.
- **Busca:**
  - o nome é comparado sem diferenciar maiúsculas e acentos;
  - o CNPJ é comparado com o texto buscado normalizado (só letras e algarismos, em maiúsculas), quando sobra algo.

### 5. API

Rotas em `/api/clients`:

| Rota | Quem | Respostas |
| --- | --- | --- |
| `GET /?search=&status=&page=&pageSize=` | ProjectCreators | 200 `{ items: [{ id, name, cnpj, logoUrl, observation, status }], total }` |
| `POST /` `{ name, cnpj, observation }` | ProjectCreators | 201 `{ id }`; 400 com os campos; 409 `client-cnpj-in-use` com `clientName` |
| `PUT /{id}` `{ name, cnpj, observation }` | Administrator | 204; 400; 404; 409 |
| `POST /{id}/deactivate`, `POST /{id}/reactivate` | Administrator | 204; 404 |
| `DELETE /{id}` | Administrator | 204; 404 |
| `PUT /{id}/logo` (multipart, campo `file`) | Administrator | 204; 400 `client-logo-format` ou `client-logo-too-large`; 404 |
| `DELETE /{id}/logo` | Administrator | 204; 404 |
| `GET /{id}/logo?v=` | sessão | o arquivo, com `nosniff`, CSP `default-src 'none'; sandbox` e cache imutável quando `v` confere; 404 sem logo |

- O `cnpj` vai sem pontuação; a tela formata.
- `status` é `Active` ou `Inactive`, como nos usuários.
- **O logo não renova a sessão:** a sessão é renovada a cada chamada, e o cookie renovado faz o ASP.NET Core responder `no-store`, o que anula o cache imutável. As rotas do logo (do cliente e, pelo mesmo motivo, da instalação) levam o marcador `KeepSession` do `Users.Contracts`: a sessão é conferida, mas não renovada. A chamada da lista que mostrou o logo já a renovou.

### 6. Auditoria

| Item | Rótulo | Campos | Fora da auditoria |
| --- | --- | --- | --- |
| `Client` | `Name` | `Name`, `Cnpj`, `Observation`, `IsActive` e `LogoFileName` | `LogoFileId`, `LogoContentType` e `LogoChangedAt` |

`auditText.ts` ganha:

- os títulos "Incluiu um cliente", "Alterou um cliente", "Inativou um cliente", "Reativou um cliente" e "Excluiu um cliente";
- os valores: CNPJ formatado e situação em palavras;
- o tipo Clientes no filtro.

### 7. Frontend

- **Coluna da Administração:** nova seção "Cadastros", com Clientes (`BusinessOutlined`) e o total, como Usuários. Rota `/admin/clients`.
- **Lista (`ClientsPage`):** o `DataTable` com:
  - busca (300 ms) e `SegmentedControl` de situação (Todos, Ativos, Inativos);
  - colunas Cliente (logo ou iniciais em `fp-logo-tile`, e o nome), CNPJ, Situação (`StatusBadge` como Ativo e Inativo dos usuários), Observação e o menu;
  - rodapé "10 clientes · 9 ativos", com a contagem de ativos de uma consulta de uma linha.
- **Menu:** Editar, Inativar ou Reativar, divisor e Excluir, que pede confirmação no `Dialog` de alerta.
- **Diálogo `ClientDialog`** (reaproveitado na Projetos-3):
  - Nome, CNPJ com máscara (aceita letras e as põe em maiúsculas) e Observação em várias linhas;
  - a seção Logo só para o administrador, com "Enviar logo", "Trocar logo" e "Remover logo", e a dica "PNG ou JPG, até 1 MB";
  - ao salvar: o `POST` ou o `PUT` do cliente, e depois o `PUT` ou o `DELETE` do logo;
  - o CNPJ repetido aparece no campo, com o nome do outro cliente.
- **Depois de salvar:** invalida `['clients']` e `['audit']`.
- **Celular:** a lista segue a tela de Usuários (`DataTable` com rolagem), e a coluna das seções some.

## Risks / Trade-offs

- [A Projetos-3 precisa mudar a exclusão] → o requisito "Situação e exclusão do cliente" passa a recusar o cliente com projetos naquela change, com teste.
- [CNPJ alfanumérico pouco conhecido] → testes com o exemplo da Receita e com o numérico tradicional.
- [Logo apagado depois da exclusão pode falhar] → o arquivo fica órfão na pasta do FiscoProjects, sem efeito na aplicação.

## Migration Plan

Migração `Initial` do módulo `Projects`: o schema `projects`, a tabela `clients` e o índice único filtrado do CNPJ. Sem dados.
