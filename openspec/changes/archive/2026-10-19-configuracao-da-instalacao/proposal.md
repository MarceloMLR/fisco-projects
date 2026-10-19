# Proposta: nome e logo da instalação (Acesso-6)

## Why

Cada empresa tem a própria instalação e troca o nome e o logo exibidos sem mexer no código (ADR-0003). A página de entrada mostra o nome e o logo da instalação (ADR-0009), e a barra lateral os mostra no topo (ADR-0066). Hoje o nome "FiscoProjects" está fixo nos arquivos de tradução, não há logo e a Administração não tem a tela Configurações gerais do canvas.

A Acesso-6 original (nome, logo, cor principal e fuso) foi separada em três a pedido de Marcelo. Esta trata do nome e do logo e cria a tela. A Acesso-7 acrescenta o fuso e a Acesso-8, a cor principal.

## What Changes

- **Módulo `Administration`** (ADR-0047, ADR-0074), com as configurações da instalação no schema `administration`:
  - nome exibido, obrigatório, até 60 caracteres, "FiscoProjects" enquanto ninguém troca;
  - logo opcional, PNG ou SVG de até 1 MB, como no canvas.
- **Logo pelo módulo `Files`** (`IFileStorage`, ADR-0030, ADR-0079), na pasta `FiscoProjects` da raiz configurada. O banco guarda só os ids e o nome do arquivo enviado.
  - O SVG é servido com uma política que impede a execução de scripts, e o arquivo é conferido no envio.
- **Auditoria:** a mudança das configurações entra na auditoria como alteração de um item (ADR-0039). A tela de Auditoria ganha o tipo "Configurações".
- **API:**
  - `GET /api/installation`, sem sessão, porque a página de entrada precisa dele: nome e endereço do logo;
  - `GET /api/installation/logo`, sem sessão;
  - `PUT /api/installation` (nome), `PUT /api/installation/logo` (envio) e `DELETE /api/installation/logo`, só do administrador (ADR-0046).
- **Tela Configurações gerais** (`/admin/settings`), como no canvas, com o que já existe:
  - seção Marca com o nome exibido e o logo (enviar, trocar e remover);
  - Prévia ao lado, com a barra lateral e o painel da entrada;
  - barra de ações com Descartar e Salvar alterações.
- **Coluna da Administração:** nova seção Instalação, com Configurações gerais.
- **Nome e logo da instalação** na barra lateral, na página de entrada e no título da aba do navegador. O logo toma o lugar do selo, com até 28 px de altura na barra lateral.

## Non-goals

- Fuso horário (Acesso-7) e cor principal (Acesso-8).
- Os outros campos de Regras gerais do canvas (expiração da sessão, resumo diário, guarda de notificações e da auditoria, fornecedor sem retorno), que chegam com as changes donas de cada regra.
- A seção Microsoft 365 do canvas (caixa de envio e pasta do SharePoint), do grupo Microsoft 365.
- Logo dos clientes (ADR-0015), que chega com o cadastro de clientes.
- Versão de celular das Configurações gerais; a Administração no celular continua sem a coluna de seções, como hoje.

## Capabilities

### New Capabilities

- `configuracao-da-instalacao`: as configurações da instalação, que só o administrador muda, e onde a aplicação as mostra. Começa com o nome e o logo; o fuso e a cor principal entram nas próximas changes.

### Modified Capabilities

- `estrutura-das-telas`: o requisito "Barra lateral no computador" passa a mostrar o nome e o logo da instalação no topo, no lugar do nome do produto.

O tipo Configurações no filtro da Auditoria fica no requisito de auditoria da capacidade nova, sem mudar o requisito "Tela de Auditoria", que não lista os tipos.

## Impact

- Backend:
  - novo módulo `Administration` (Domain, Application, Infrastructure, Endpoints e Contracts), com a migração `Initial` e testes;
  - registro em `ApiSetup`.
- Contrato OpenAPI e tipos do frontend (`npm run api:update`).
- Frontend:
  - `installationQuery`;
  - nome e logo na `Sidebar`, na `SignInPage` e no título da aba;
  - nova `SettingsPage` em `/admin/settings` e a seção Instalação no `AdminLayout`;
  - tipo Configurações na Auditoria;
  - traduções e testes.
- Documentação:
  - `docs/roadmap.md` (Acesso-6, 7 e 8);
  - `docs/glossario.md` (configurações gerais, nome exibido, logo);
  - `src/Modules/README.md` e `CLAUDE.md`, que passam a citar o módulo `Administration`.
- ADRs: 0003 e 0009 (nome e logo por instalação), 0011 e 0046 (Configurações na Administração, só do administrador), 0030 e 0079 (logo pelo `IFileStorage`), 0039 (mudança de configuração auditada), 0047 e 0074 (módulo `Administration`), 0066 (nome e logo na barra lateral).
