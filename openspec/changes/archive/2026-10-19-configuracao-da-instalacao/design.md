# Design: nome e logo da instalação (Acesso-6)

## Context

- **Nome:** o nome "FiscoProjects" vem da chave `app.name` dos arquivos de tradução, na `Sidebar` e na `SignInPage`, e o título da aba é fixo no `index.html`.
- **Administração:** já tem a coluna de seções (`AdminLayout`) com Pessoas e segurança (Usuários e Auditoria), só para o administrador (`RequireAdministrator` no frontend, `SystemPolicies.Administrator` na API).
- **Arquivos:** o módulo `Files` oferece o `IFileStorage` (pastas e arquivos por ids opacos, implementação local no desenvolvimento) e não tem tabelas nem rotas. Cada módulo guarda os ids e serve os próprios arquivos (ADR-0079).
- **Auditoria:** toda gravação pelo `SaveChangesAsync` de um `ModuleDbContext` é auditada, com o tipo do item pelo nome da classe.

O canvas `AdminConfiguracoes` define a tela:

- `fp-split--left` com a coluna de seções e, à direita, um `fp-pane` com o título e `fp-cols` (formulário e uma Prévia de 320 px);
- as seções do formulário em `fp-form-section`, com `fp-form-row` de duas colunas;
- o logo num `fp-logo-tile` com o botão secundário "Enviar logo" e a dica "PNG ou SVG, até 1 MB";
- a `fp-actionbar` fixa embaixo, com Descartar e Salvar alterações.

No design system, o logo toma o lugar do selo na barra lateral (`fp-sidebar__logo`: até 28 px de altura e 160 px de largura).

## Goals / Non-Goals

**Goals:**

- Um módulo dono das configurações da instalação, pronto para receber o fuso (Acesso-7) e a cor principal (Acesso-8) sem outra tabela.
- Um lugar só no frontend para a marca: uma consulta que a barra lateral, a página de entrada, o título da aba e a Prévia usam.

**Non-Goals:**

- Guardar a marca no navegador para a página de entrada abrir sem esperar a API (ADR-0080: dados só na memória).

## Decisions

### 1. Módulo `Administration`, com uma linha de configurações

- **Módulo:** novo `FiscoProjects.Administration` e `FiscoProjects.Administration.Contracts`, schema `administration`, no formato do guia `src/Modules/README.md`. É o módulo previsto no ADR-0047 para configurações e cadastros, e os cadastros entram nele depois.
- **Entidade `InstallationSettings`:**
  - `Name`, até 60 caracteres;
  - o logo: `LogoFileId`, `LogoFileName` (o nome do arquivo enviado), `LogoContentType` e `LogoChangedAt`;
  - os métodos `Rename`, `ReplaceLogo` e `RemoveLogo`, que validam o nome e mantêm os campos do logo juntos.
- **Uma linha só:** criada pela migração `Initial` com um id fixo e o nome "FiscoProjects" (`HasData`). Assim a leitura nunca precisa criar a linha, e dois administradores salvando juntos não criam duas.
- **Auditoria:**
  - `Name` e `LogoFileName` entram, para a auditoria mostrar o nome e o arquivo antes e depois;
  - `LogoFileId`, `LogoContentType` e `LogoChangedAt` ficam de fora (`ExcludeFromAudit`), por serem técnicos;
  - sem `HasAuditLabel`, porque o item é sempre o mesmo e a tela de Auditoria o chama de "Configurações gerais".

Alternativa: guardar o nome e o logo no `appsettings` de cada instalação. Foi descartada, porque o ADR-0011 e o canvas põem as configurações numa tela da Administração, e a mudança precisa entrar na auditoria.

### 2. Logo pelo `IFileStorage`, conferido pelo conteúdo

- **Onde fica:** na pasta `FiscoProjects` da raiz configurada (ADR-0030: os arquivos do sistema ficam numa pasta do FiscoProjects), com o nome `installation-logo.png` ou `.svg` e `SameNameAction.NewVersion`.
  - Trocar PNG por SVG, ou o contrário, exclui o arquivo anterior depois de salvar.
  - Remover o logo limpa os campos e exclui o arquivo.
- **Ordem do envio:** o arquivo é gravado primeiro e o banco depois. Se o banco falhar, o arquivo novo é excluído, para não sobrar arquivo sem registro.
- **Conferência no caso de uso (`LogoFile`, na Application),** pelo conteúdo e nunca pela extensão ou pelo tipo informado pelo navegador:
  - até 1 MB (1.048.576 bytes);
  - PNG: a assinatura de 8 bytes do formato;
  - SVG: XML bem formado com raiz `svg`, sem DTD (fecha a porta para entidades externas), sem os elementos `script` e `foreignObject`, sem atributos `on*` e sem `href` com `javascript:`.
- **Recusas:** 400 em Problem Details, com um tipo por motivo, para a tela dizer qual foi:
  - `urn:fiscoprojects:problem:logo-format`;
  - `urn:fiscoprojects:problem:logo-too-large`;
  - `urn:fiscoprojects:problem:logo-unsafe`.
- **Rota de envio:** `PUT /api/installation/logo` recebe `multipart/form-data` com o campo `file`:
  - o limite do corpo é de 2 MB, para recusar cedo um envio grande;
  - sem o antiforgery do ASP.NET Core, porque a proteção da aplicação é o cabeçalho `X-CSRF` (ADR-0054).

Alternativa: limpar o SVG (tirar scripts e atributos perigosos) em vez de recusar. Foi descartada, porque a limpeza é fácil de errar e mudaria o arquivo da empresa sem avisar. Recusar com o motivo é mais simples, e a política da resposta (decisão 3) protege mesmo se algo passar.

### 3. Marca sem sessão, e o logo servido sem scripts

- **`GET /api/installation`** (`AllowAnonymous`) devolve `{ name, logoUrl }`, com `logoUrl` `null` sem logo ou `/api/installation/logo?v=<LogoChangedAt em ticks>`.
  - A mesma resposta serve à tela Configurações gerais. O fuso (Acesso-7) não entra nela, por não ser da marca.
- **`GET /api/installation/logo`** (`AllowAnonymous`) devolve o arquivo com:
  - `Content-Type` guardado e `X-Content-Type-Options: nosniff`;
  - `Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; sandbox`, para um SVG aberto direto no navegador não executar nada;
  - cache: com o `v` igual à versão atual, `Cache-Control: public, max-age=31536000, immutable`, já que um logo novo muda o endereço; sem ele, `no-cache`;
  - 404 quando não há logo.
- **`PUT /api/installation`** recebe `{ name }` e responde 204, ou 400 com o motivo no campo.
  - Ele leva sempre o formulário inteiro: na Acesso-7 ganha `timeZone` e na Acesso-8, `mainColor`.
- **Autorização:** as alterações (`PUT /api/installation`, `PUT` e `DELETE /api/installation/logo`) exigem `SystemPolicies.Administrator`.

### 4. Frontend: uma consulta para a marca

- **Consulta:** `installationQuery()` em `queries.ts`, com a chave `['installation']`, e o gancho `useInstallation()`. Enquanto a resposta não chega, ele devolve o nome dos arquivos de tradução (`app.name`) e nenhum logo.
- **Barra lateral:** com logo, `<img alt="">` no lugar do selo, com até 28 × 160 px, porque o nome vem logo depois.
- **Página de entrada:** o logo no lugar do selo de 32 px, sobre um fundo claro (`brand-panel-text`) com cantos `radius-md`. O painel é sempre escuro, e um logo escuro sumiria nele.
- **Título da aba:** o nome da instalação, atualizado por uma rota raiz (`InstallationRoot`) em volta de todas as telas, com ou sem sessão.
- **Saída:** a sessão descarta o que leu, menos a marca, que é pública. Removida a consulta, a rota raiz ficaria presa a uma consulta que não existe mais e a aba não mudaria de nome na página de entrada.
- **Depois de salvar:** a tela invalida `['installation']` e `['audit']`.

### 5. Tela Configurações gerais

- **Rota e coluna:**
  - rota `/admin/settings` dentro do `AdminLayout`, com o título "Configurações gerais" e a trilha Administração / Configurações gerais;
  - na coluna, a seção Instalação com o item Configurações gerais (ícone `SettingsOutlined`).
- **Formulário:** React Hook Form com Zod. O nome é obrigatório, sem os espaços das pontas e com até 60 caracteres.
- **Logo:**
  - O estado é `salvo`, `novo` (o arquivo escolhido e uma prévia local por `URL.createObjectURL`) ou `removido`.
  - Na escolha do arquivo, o formato e o tamanho são conferidos na tela, com a mesma mensagem da API; o SVG com script só a API recusa.
  - O botão mostra "Enviar logo" sem logo e "Trocar logo" com logo, ao lado de "Remover logo".
- **Salvar alterações:**
  1. envia o logo novo ou remove o logo, se mudou;
  2. depois envia o nome, se mudou.

  Uma recusa para ali e mostra o motivo no campo, sem perder o que foi digitado.
- **Descartar:** volta aos valores salvos. Os dois botões ficam desabilitados enquanto nada mudou ou enquanto salva.
- **Prévia (320 px),** com os tokens:
  - a miniatura da aplicação com a barra lateral, o nome e o logo;
  - a linha da marca da barra lateral em tamanho real;
  - o painel da entrada.
- **Auditoria:** `auditItemTypes` ganha `InstallationSettings`, com o rótulo "Configurações" no filtro, o título "Alterou as configurações" e os campos "Nome exibido" e "Logo".

## Risks / Trade-offs

- **[Página de entrada pisca o nome padrão]** Até `GET /api/installation` responder, a entrada mostra "FiscoProjects". → É uma resposta pequena e só na abertura. Guardar no navegador ficou fora (Non-Goals).
- **[Logo grande em pixels]** Um PNG de 1 MB pode ter muitos pixels. → A tela o mostra reduzido (`max-height` e `max-width`), e o limite de 1 MB segura o tamanho do download.
- **[Envio em dois pedidos]** O logo pode ser salvo e o nome recusado. → O nome é conferido na tela antes do envio, e a recusa aparece no campo, com o logo já salvo e o formulário mantido para corrigir.
- **[SVG malicioso]** A conferência pode deixar passar um truque. → O SVG só aparece em `<img>`, que não executa scripts, e quem abrir o endereço direto recebe a política `sandbox`.

## Migration Plan

- A migração `Initial` do módulo cria o schema `administration` e a tabela com a linha padrão.
- No desenvolvimento, a API a aplica ao subir; em produção, segue o processo de publicação (ADR-0055).
- Para voltar atrás, reverta o PR antes de publicar. O logo enviado no desenvolvimento fica em `App_Data/files/FiscoProjects`, fora do Git.
