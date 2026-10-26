## Context

O `Avatar` de `web/src/components` é decorativo (`aria-hidden`), com o nome escrito ao lado. As respostas da API trazem o id e o nome das pessoas, e só a busca de pessoas, a tela Usuários, Membros e as pessoas do chamado trazem o e-mail. As tabelas escolhem a linha pelo clique, menos quando ele vem de um link ou botão (ADR-0105).

## Goals / Non-Goals

**Goals:**
- Um cartão igual em todas as telas, aberto pelo avatar de outra pessoa, com o e-mail lido só ao abrir.

**Non-Goals:**
- Fotos do Microsoft 365, presença ou o cartão pelas menções no texto das mensagens.
- O cartão pelo avatar da pessoa na sessão (menu da conta, Meu perfil) e pelo cabeçalho do detalhe da pessoa em Membros.

## Decisions

- **API**: `GET /api/people/{id}` (`GetPerson`, no `Users`), com o `IUserStore.FindAsync`; o e-mail vazio do anonimizado sai como `null`. Qualquer pessoa com sessão, como a busca de pessoas. Não entra no aquecimento: não é uma tela principal.
- **Componente**: `CardAvatar` (`web/src/components/PersonCard.tsx`) recebe a pessoa (`id`, `name`), o papel em texto, o tamanho e o tom; desenha o `Avatar` dentro de um botão e abre o cartão num `Popover` do MUI, com `role="dialog"` e o nome como rótulo. O e-mail vem do `personQuery` (TanStack Query, `queryKeys.people`), pedido só com o cartão aberto. "Copiar e-mail" usa a área de transferência e confirma pelo `Snackbar`, como os links.
- **Tabelas**: o clique no avatar já não escolhe a linha (é um botão). O cartão fica num portal, mas os eventos do React sobem até a linha: o cartão para o clique, o `mousedown` e as teclas antes de chegarem a ela.
- **Papel em cada tela**: o papel no chamado na conversa e no painel, o papel no projeto em Membros, na Visão geral e no gerente de Projetos, o papel no sistema em Usuários; sem papel na lista de chamados, no cronograma e na Auditoria.

## Risks / Trade-offs

- [Avatar pequeno como alvo de toque] → o botão tem o tamanho do avatar, ao lado do nome, como hoje; no celular, o cartão cabe na largura da tela.
