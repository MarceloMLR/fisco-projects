## Why

O aviso de um chamado no Teams abre a página do FiscoProjects no Teams, que só tem a marca e "Abrir no FiscoProjects": a pessoa não vê nem a mensagem que a mencionou sem ir ao navegador. Em 03/11/2026, ao testar os avisos pelo Teams, Marcelo pediu para mostrar mais do chamado no próprio Teams e, depois de ver o canvas "Chamado no Teams", pediu esta proposta. A página não sabe quem é a pessoa porque o cookie da sessão não vai dentro do quadro do Teams; a entrada única do Teams (SSO) resolve isso sem nova entrada (ADR-0152). Avisos-6, fora do roadmap aprovado: a proposta é aprovada por Marcelo antes da implementação.

## What Changes

- **Página do Teams, aviso de chamado:** com o caminho `/tickets/{número}`, a página mostra o chamado só para ler, como no canvas.
  - O projeto, o número, o título, o status, a prioridade e o prazo de resolução.
  - As últimas 5 interações da conversa (a descrição como a primeira, as mensagens e os eventos), com as menções destacadas e quantas vieram antes.
  - As pessoas (solicitante, responsável e gerente), os prazos, o time, a abertura e quantos anexos há; os anexos só pelo nome.
  - "Responder no FiscoProjects", que abre o chamado no navegador.
  - No Teams estreito (celular ou janela pequena), tudo numa coluna, com o botão no pé.
- **Chamado que a pessoa não vê:** "Você não vê este chamado", com "Abrir no FiscoProjects".
- **Sem a entrada única:** fora do Teams, sem a entrada pelo Entra, com o token recusado, com falha ou com aviso de outro item, a página continua como hoje.
- **Entrada pelo Teams:** a página pede ao Teams o token da pessoa para o aplicativo da instalação (`@microsoft/teams-js`), guardado só na memória da página. A API confere o token e acha a pessoa pelo vínculo `entra`; recusa o convidado, quem não tem o vínculo `entra` e o desativado, sem criar usuário nem sessão.
- **Rota de leitura:** `GET /api/teams/tickets/{número}`, só com o token do Teams, nunca com o cookie, com as regras de quem vê o chamado (ADR-0044), sem e-mails e com `no-store`; 404 para quem não vê. O token do Teams não vale em nenhuma outra rota.
- **Aplicativo do Entra (Terraform):** o identificador `api://{endereço do site}/{cliente}`, o escopo `access_as_user`, os tokens v2, os clientes do Teams pré-autorizados e a declaração de conta convidada no token de acesso. Nenhuma permissão do Graph muda; o manifesto do Teams não muda.
- **Dependências novas:** `@microsoft/teams-js` (MIT), carregada só na página do Teams, e `Microsoft.AspNetCore.Authentication.JwtBearer` (MIT).
- **Documentação:** o ADR-0152, os ADR-0054 e 0149 apontando para ele, o guia de instalação, o `CLAUDE.md` e o roadmap.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos`: a página do FiscoProjects no Teams mostra o chamado do aviso e continua como hoje sem a entrada única.
- `chamados`: a leitura do chamado para o Teams, com as regras de quem vê e só o que a página mostra.
- `entrada-e-sessao`: a entrada pelo token do Teams, só nas rotas de leitura do Teams, e a sessão por cookie nas outras.
- `infraestrutura`: o aplicativo do Entra pronto para a entrada única do Teams.

## Non-goals

- Responder, mudar o status, assumir, acompanhar ou baixar anexos dentro do Teams.
- Mostrar no Teams outros itens dos avisos (tarefas, termos de aceite, documentos).
- Uma lista de chamados ou o Meu trabalho na página do Teams aberta sem aviso.
- Bot ou cartões no chat do Teams.
- Avisos na aba Atividade do Teams do celular, que a Microsoft não mostra para aplicativos próprios.
- O FiscoProjects no aplicativo do Microsoft 365 ou no Outlook.
- Abrir a sessão das telas pelo token do Teams.
- Mudar quais avisos vão ao Teams ou quem os recebe.
- Dados de demonstração novos: a página do Teams só existe numa instalação com o Entra.

## Impact

- **Backend (`Users`):** o esquema de autenticação do token do Teams, ligado só com a entrada pelo Entra, e o caso de uso que acha a pessoa pelo vínculo, com a regra do convidado num lugar só.
- **Backend (`Tickets`):** a rota de leitura do Teams, sobre o `GetTicket`, com a resposta própria.
- **Host (`FiscoProjects.Api`):** o grupo `/api/teams`, com a política do Teams no lugar da sessão.
- **Frontend:** a página do Teams com o chamado, o chamado que a pessoa não vê e a volta à página de hoje; a biblioteca do Teams carregada só ali; os textos e os testes de tela.
- **Testes:** o `FakeEntra` passa a emitir o token do Teams; testes da rota, das recusas, do token fora das outras rotas e dos registros.
- **Contrato:** `web/src/api/openapi.json` e `schema.ts` gerados de novo.
- **Infraestrutura:** o módulo `entra` do Terraform e o `InfrastructureTests`; `terraform apply` no `dev` só com a aprovação de Marcelo.
- **Instalações:** aplicar o Terraform da versão nova, como em toda versão; nenhum pacote novo do Teams.
- **Docs:** ADR-0152, os ADR-0054 e 0149 apontando para ele, `docs/instalacao.md`, `CLAUDE.md` e o roadmap.
