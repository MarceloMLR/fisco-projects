---
status: aceita
date: 2026-11-03
decision-makers: Marcelo Ribeiro
---

# ADR-0152: Chamado no Teams pela entrada única do Teams, só para ler

Complementa o ADR-0149: a página do FiscoProjects no Teams, que hoje só tem a marca e "Abrir no FiscoProjects", passa a mostrar o chamado do aviso, só para ler. Para isso, cria a autenticação por token que o ADR-0054 deixou para "outros clientes, com ADR próprio": o token que o Teams entrega ao aplicativo da instalação, aceito só nas rotas de leitura do Teams. A sessão por cookie do ADR-0054 continua como está em todas as outras rotas.

## Contexto e problema

Os avisos que pedem ação chegam pela aba Atividade do Teams (ADR-0149, ADR-0151). Escolher um aviso abre a página do FiscoProjects no Teams, que não sabe quem é a pessoa e por isso só mostra a marca e "Abrir no FiscoProjects", que leva ao item no navegador.

Em 03/11/2026, ao testar os avisos pelo Teams com uma menção num chamado, Marcelo pediu que a página mostre mais do chamado, ao menos a mensagem, para a pessoa entender o aviso sem sair do Teams. Depois de ver o canvas "Chamado no Teams" (o chamado no Teams do computador, no do celular e o chamado que a pessoa não vê), pediu esta proposta.

A página no Teams é um quadro de outro site (o Teams) com o endereço da instalação. Nele:

- o cookie da sessão (`SameSite=Lax`, ADR-0054) não vai nas requisições, porque o quadro é de outro site;
- a página da Microsoft não abre num quadro, então a entrada pelo Entra (ADR-0117) teria de abrir uma janela à parte;
- o Teams oferece a entrada única (SSO): pela biblioteca do Teams, a página pede um token da pessoa já conectada ao Teams para o aplicativo da instalação no Entra ID, sem pedir senha.

Como a página do Teams mostra o chamado do aviso a quem pode vê-lo, sem enfraquecer a sessão do FiscoProjects?

## Fatores de decisão

- A pessoa entende o aviso sem sair do Teams e sem entrar de novo.
- As regras de quem vê cada chamado continuam num lugar só (ADR-0044).
- A sessão do FiscoProjects continua só no cookie `HttpOnly`, sem token ao alcance do JavaScript nas telas (ADR-0054).
- Nada de segredo novo, permissão nova do Graph ou consentimento novo no Entra ID (ADR-0117).
- A instalação do cliente muda o mínimo: o aplicativo do Entra pelo Terraform, que já roda em cada versão (ADR-0127), sem pacote novo do Teams.
- Sem a entrada única (fora do Teams, sem o Entra, com falha), a página continua como hoje.

## Opções consideradas

- A. Entrada única do Teams, com o token só na memória da página e aceito só por rotas de leitura do Teams
- B. Sessão por cookie dentro do quadro, com a entrada pela janela da Microsoft
- C. Cartão no chat do Teams, por um bot
- D. Deixar como está, só com "Abrir no FiscoProjects"

## Resultado da decisão

Opção escolhida: **A**, pedida por Marcelo em 03/11/2026, depois de ver o canvas.

- **O que mostra:** com um aviso de chamado (o caminho `/tickets/{número}`), a página mostra o chamado só para ler, como no canvas: o projeto, o número e o título, o status, a prioridade e o prazo de resolução; as últimas 5 interações da conversa (mensagens, com a descrição como a primeira, e eventos), da mais antiga para a mais nova, com as menções destacadas e quantas vieram antes; as pessoas (solicitante, responsável e gerente do projeto), os prazos de primeira resposta e de resolução, o time, a abertura e quantos anexos o chamado tem. Os anexos aparecem pelo nome, sem abrir nem baixar.
- **Ação:** "Responder no FiscoProjects" abre o chamado no navegador, como hoje. Responder, mudar o status, assumir e baixar anexos continuam só no FiscoProjects.
- **Quem vê:** quem vê o chamado no FiscoProjects (ADR-0044). Para os outros, e para um chamado que não existe, a página diz "Você não vê este chamado", com "Abrir no FiscoProjects".
- **Entrada única:** a página pede o token ao Teams pela biblioteca oficial `@microsoft/teams-js` (MIT), carregada só na página do Teams. O token é do aplicativo da instalação no Entra ID (audiência própria, escopo `access_as_user`, tokens v2), emitido só para o Teams do computador, do celular e da web (clientes pré-autorizados), e não abre o Microsoft Graph.
- **Token só na memória:** o token fica numa variável da página só enquanto ela está aberta, vai no cabeçalho `Authorization` das leituras do Teams e nunca é guardado (`localStorage`, `sessionStorage`, cookie ou cache das consultas), nem posto em endereço ou registro. É a exceção do ADR-0054 para o cliente Teams, limitada à página do Teams.
- **Rotas do Teams:** só leituras (`GET`), sob `/api/teams`, que aceitam só o token do Teams e nunca o cookie; o token não vale em nenhuma outra rota. A primeira é `GET /api/teams/tickets/{número}`, com a resposta só com o que a página mostra, sem e-mails, e `no-store`.
- **Quem é a pessoa:** a aplicação confere a assinatura, o emissor do tenant da instalação, a audiência, a validade, o escopo e o cliente do Teams, e acha o usuário pelo vínculo `entra` do identificador do objeto (ADR-0117). O convidado, quem não tem o vínculo `entra` (nunca entrou nem foi incluído num projeto pelo diretório) e o desativado são recusados (401), sem criar usuário nem sessão (ADR-0005, ADR-0007, ADR-0008). Quem não está atribuído ao aplicativo não recebe token do Entra ID.
- **Sem a entrada única:** fora do Teams, sem a entrada pelo Entra na instalação, com o token recusado, com a falha da leitura ou com um aviso de outro item (tarefa, termo, documento), a página mostra a marca e "Abrir no FiscoProjects", como no ADR-0149.
- **Aplicativo do Entra:** o Terraform expõe a API do aplicativo da instalação: o identificador `api://{endereço do site}/{cliente}`, que o manifesto do Teams já declara no `webApplicationInfo`; o escopo `access_as_user`, com o consentimento do administrador; os tokens v2; os clientes do Teams pré-autorizados; e a declaração de conta convidada também no token de acesso. Nenhuma permissão do Graph muda.
- **Instalações:** quem opera aplica o Terraform da versão nova, como em toda versão. O manifesto do Teams não muda, então o Administrador do Teams não sobe pacote novo.

### Consequências

- Bom, porque a pessoa lê o chamado do aviso, com a mensagem que a mencionou, sem sair do Teams nem entrar de novo.
- Bom, porque quem vê o chamado continua decidido pelas regras do FiscoProjects, e o chamado que a pessoa não vê não aparece.
- Bom, porque a sessão das telas continua só no cookie, e o token do Teams vale só para ler, só nas rotas do Teams e só por pouco tempo.
- Bom, porque a instalação do cliente só aplica o Terraform da versão, sem segredo, permissão ou pacote do Teams novos.
- Ruim, porque um ataque de script na página do Teams poderia ler o token enquanto a página está aberta; com ele, só se leem os chamados que a pessoa vê, pelo tempo de vida do token. O mesmo ataque, numa tela com o cookie, já faria requisições como a pessoa; a página do Teams mostra só texto montado nó por nó, sem HTML.
- Ruim, porque a API passa a ter um segundo jeito de reconhecer a pessoa, que precisa dos próprios testes.
- Neutro, porque no Teams do celular os avisos de aplicativos próprios não aparecem na aba Atividade (limitação da Microsoft); a página do celular serve a quem abre o chamado por um link do Teams.

### Confirmação

Testes cobrindo:

- o chamado na página do Teams, com as últimas interações, a quantidade das anteriores e as menções;
- "Você não vê este chamado" para quem não está no projeto e para o chamado que não existe;
- a página como hoje fora do Teams, com a entrada única recusada e com o aviso de outro item;
- o token recusado com outra audiência, outro emissor, outro escopo, outro cliente, assinatura errada ou vencido, e do convidado, de quem não tem o vínculo `entra` e do desativado, sem criar usuário nem sessão;
- o token do Teams recusado nas outras rotas, e o cookie recusado nas rotas do Teams;
- o token fora do armazenamento do navegador, dos endereços e dos registros;
- o aplicativo do Entra com o identificador, o escopo, os tokens v2 e os clientes do Teams no Terraform.

## Prós e contras das opções

### A. Entrada única do Teams, só para ler

- Bom, porque é o caminho que a Microsoft indica para páginas no Teams e não pede entrada.
- Bom, porque o token serve só ao aplicativo da instalação e só às leituras do Teams.
- Ruim, porque abre a exceção do ADR-0054 para o cliente Teams e acrescenta a biblioteca do Teams e a validação do token.

### B. Cookie dentro do quadro

- Bom, porque reaproveitaria a sessão e as rotas de hoje.
- Ruim, porque o cookie teria de ser `SameSite=None` e particionado, mais fraco contra requisições forjadas, e navegadores que bloqueiam cookies de terceiros o recusam.
- Ruim, porque a entrada abriria uma janela da Microsoft a cada sessão vencida.

### C. Cartão no chat, por um bot

- Bom, porque o cartão aparece no próprio chat.
- Ruim, porque pede o Azure Bot Service, um recurso e um custo novos, e foi descartado no ADR-0149 em favor da aba Atividade.
- Ruim, porque o cartão leva o conteúdo do chamado para o histórico do Teams, fora das regras de quem vê.

### D. Deixar como está

- Bom, porque não muda nada.
- Ruim, porque a pessoa não entende o aviso sem abrir o navegador, que é o que Marcelo pediu para mudar.
