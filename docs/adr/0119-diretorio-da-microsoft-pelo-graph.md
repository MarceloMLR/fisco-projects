---
status: aceita
date: 2026-10-26
decision-makers: Marcelo Ribeiro
---

# ADR-0119: Diretório da empresa pelo Microsoft Graph

Complementa o ADR-0005 (busca no diretório para incluir quem ainda não entrou) e o ADR-0117 (a identidade gerenciada como prova do aplicativo).

## Contexto e problema

A busca de pessoas traz os usuários do FiscoProjects e as pessoas do diretório da empresa que ainda não entraram (ADR-0005). Até aqui, o diretório é o de demonstração da entrada local (ADR-0078). Com a entrada pelo Entra ID (ADR-0117), o diretório passa a ser o tenant da empresa, lido pelo Microsoft Graph. O ADR-0005 deixou a permissão exata para a implementação. Que permissão pedir, como chamar o Graph e o que buscar?

## Fatores de decisão

- Nenhum segredo nem token guardado: o aplicativo já prova quem é pela identidade gerenciada do App Service (ADR-0117).
- O menor acesso que funcione, e só os dados que a busca usa (ADR-0058).
- A mesma pessoa no diretório e na entrada, para que a primeira entrada encontre o usuário criado ao incluí-la num projeto.
- O Graph fora do ar ou sem permissão não pode derrubar a busca de pessoas.
- Testes sem o Microsoft 365 real (ADR-0056).

## Opções consideradas

Permissão:

1. A permissão de aplicativo `User.Read.All`, a menor que lista usuários sem uma pessoa conectada, com o consentimento do administrador.
2. A permissão delegada `User.ReadBasic.All`, só com o perfil básico, pelo token de quem busca.

Chamada:

1. `HttpClient` com as consultas escritas à mão e o token do `ClientAssertionCredential` do Azure.Identity.
2. O SDK do Microsoft Graph (`Microsoft.Graph`).

## Resultado da decisão

Permissão 1, escolhida por Marcelo. A delegada exigiria guardar os tokens da Microsoft no servidor, o que o ADR-0117 evita, e só valeria para quem entrou pelo Entra. A `User.Read.All` alcança o perfil completo das pessoas, mas o FiscoProjects pede só o identificador, o nome, o e-mail e o nome de entrada (`$select`).

Chamada 1. O aplicativo pede ao Entra um token do Graph com a prova da identidade gerenciada (`ClientAssertionCredential`, o mesmo cliente e tenant da entrada) e chama `GET /v1.0/users` com `HttpClient`. O SDK traria um pacote grande para duas consultas.

Como funciona:

- A busca usa o `$search` do Graph no nome (começo das palavras) e no e-mail e no nome de entrada (começo do texto), com `ConsistencyLevel: eventual`, só membros ativos (`userType eq 'Member' and accountEnabled eq true`), em ordem de nome. Convidados e contas desativadas ficam fora, como na entrada (ADR-0007). As maiúsculas não contam; os acentos seguem as regras do Graph.
- A busca traz o diretório inteiro, não só quem está atribuído ao aplicativo: quem não está atribuído pode ser incluído num projeto, mas só entra depois de atribuído no Entra (ADR-0005).
- Cada pessoa vira `entra:{id do objeto}`, o mesmo vínculo da entrada (`oid`), e o e-mail é o `mail` ou, sem caixa de correio, o nome de entrada, como na entrada.
- Com as duas entradas ligadas, a busca junta o diretório de demonstração e o da Microsoft.
- Uma falha do Graph vai para o log, sem o texto buscado nem nomes, e a busca segue com os usuários e o diretório de demonstração.
- Nos testes, um Graph simulado no próprio processo responde às consultas.

### Consequências

- Boa, porque nenhum segredo nem token de pessoa é guardado.
- Boa, porque incluir alguém do tenant num projeto antes da primeira entrada passa a funcionar na instalação real.
- Ruim, porque a permissão de aplicativo alcança mais dados do que a busca usa; o código pede só o necessário.
- Neutra, porque o consentimento do administrador entra no Terraform, aplicado por quem opera a instalação.
