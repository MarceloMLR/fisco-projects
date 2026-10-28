## Context

O `Users` guarda os vínculos de cada usuário com os provedores (`identity_links`, o `oid` do Entra ID em `entra`) e já lê o tenant pelo Graph com o token do aplicativo (`IGraphTokens`, `MicrosoftGraph.ClientName`, ADR-0119). O avatar das telas (`Avatar`) mostra só as iniciais; o `CardAvatar` recebe a pessoa com o id. Os logos já saem por endereços que o navegador guarda, com o `KeepSession`.

## Goals / Non-Goals

**Goals:**
- A foto do Microsoft 365 em todo avatar de uma pessoa, sem cópia no banco e sem muitas chamadas ao Graph.

**Non-Goals:**
- O contorno na cor do papel em volta da foto (Marcelo valida depois).
- Fotos na entrada local e na demonstração.

## Decisions

- **Caso de uso.** `GetPersonPhoto` lê os vínculos do usuário (`IUserStore.IdentitiesAsync`) e pede a foto ao `IPersonPhotos`. Sem vínculo, nada é pedido: o anonimizado, que perde os vínculos, fica sem foto na hora.
- **Graph.** O `GraphPersonPhotos` (Infrastructure), ligado com a entrada pelo Entra, pede `users/{oid}/photos/96x96/$value` e, com 404, `users/{oid}/photo/$value`; aceita só `image/jpeg` e `image/png` até 1 MB. A falha do Graph (`HttpRequestException`, `AuthenticationFailedException`, tempo esgotado) vira aviso no log, sem o `oid`, e "sem foto".
- **Memória.** O `PersonPhotoCache` (singleton, com o `TimeProvider`) guarda a resposta pelo `oid`: a foto ou a falta dela por uma hora, a falha por um minuto. Um dicionário simples basta: uma entrada por pessoa da empresa, de poucos KB.
- **Endpoint.** `GET /api/people/{id}/photo` (`GetPersonPhoto`) responde a imagem ou 404, os dois com `private, max-age=3600`, `nosniff` e a política de conteúdo dos logos, e com o `KeepSession`, para a sessão renovada não tirar o cache.
- **Tela.** O `Avatar` ganha `photo` (o endereço): as iniciais ficam embaixo, e a imagem, invisível até carregar, cobre o círculo depois do `load`; com `error`, a imagem sai. O endereço vem do `photoOf(id)` (`web/src/components/personPhoto.ts`). Recebem a foto o `CardAvatar` e o avatar do cartão, o menu da conta, Meu perfil, o detalhe em Membros e a menção.
- **Testes.** O `FakeGraph` ganha `Photos` (por id, com o tipo e se existe no tamanho de 96x96) e conta os pedidos, para os testes da API conferirem a memória.

## Risks / Trade-offs

- [Avatar com foto perde a cor do papel nos chamados] → o selo e a cor da mensagem dizem o papel; Marcelo valida o contorno.
- [Foto trocada no Microsoft 365 demora a aparecer] → até uma hora no servidor e uma no navegador.
- [Graph lento numa tela cheia de avatares] → as iniciais aparecem na hora, e a foto entra quando chega.
