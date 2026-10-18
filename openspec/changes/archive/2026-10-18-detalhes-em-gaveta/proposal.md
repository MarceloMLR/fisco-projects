# Proposta: detalhes em gaveta sobreposta (Acesso-3.1)

## Why

Na tela de Auditoria, os detalhes do registro ficam numa coluna fixa de 340 px à direita (ADR-0066 e canvas). Ela tira espaço da tabela e é estreita demais para a tabela de mudanças: "Gerente de projeto" e "Administrador" quebram no meio da palavra. No filtro de pessoa, de 180 px, o nome escolhido aparece cortado ("Ana Almei"). Marcelo pediu que os detalhes abram só quando ele clica numa linha, numa gaveta à direita por cima da tela, com o fundo escurecido e mais larga, e que esse seja o padrão de todos os detalhes do sistema.

## What Changes

- **Padrão novo para detalhes** (ADR-0081, que substitui o item "Detalhe do item selecionado num painel lateral" do ADR-0066):
  - o detalhe de um item escolhido numa lista abre numa gaveta à direita, por cima da tela, com o fundo escurecido (token `overlay`), com até 640 px de largura e a largura toda no celular;
  - a gaveta fecha pelo botão Fechar, pela tecla Esc ou por um clique fora dela, e o foco volta para a lista;
  - a lista ocupa toda a largura.
- **Componente `DetailsDrawer`** em `web/src/components`, reaproveitado por documentos, termos e membros quando chegarem.
- **Tela de Auditoria:**
  - sem a coluna fixa e sem registro escolhido de início; os detalhes abrem ao escolher uma linha (clique ou Enter), e a linha fica marcada enquanto a gaveta está aberta;
  - a tabela de mudanças não quebra palavras no meio, só textos maiores que a coluna, como e-mails longos;
  - o filtro de pessoa fica mais largo e, quando o nome não cabe, termina em reticências.

## Non-goals

- A página do chamado, que é uma página própria com painel lateral (ADR-0069) e não o detalhe de uma linha de lista.
- O menu do celular (gaveta da navegação) e o painel de notificações do sino, que continuam como no ADR-0066.
- Mudar o conteúdo dos detalhes da Auditoria (quem, o quê, quando, item e mudanças).
- Gaveta redimensionável ou que guarde a largura escolhida.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `estrutura-das-telas`: novo requisito "Detalhes em gaveta sobreposta", comum a todas as telas com lista e detalhe.
- `auditoria`: o requisito "Tela de Auditoria" passa a abrir os detalhes na gaveta, só quando o administrador escolhe um registro, e a mostrar o nome escolhido no filtro de pessoa sem cortar.

## Impact

- Frontend:
  - novo `web/src/components/DetailsDrawer.tsx`, com testes;
  - `web/src/admin/AuditPage.tsx`, `AuditDetails.tsx` e `PersonFilter.tsx`;
  - testes da Auditoria e as chaves de tradução, se precisar de texto novo.
- Documentação:
  - ADR-0081 novo e a linha no `docs/adr/README.md`;
  - `docs/glossario.md` (gaveta de detalhes);
  - `docs/roadmap.md` (Acesso-3.1).
- ADRs tocados:
  - 0066 (estrutura das telas): o item do painel lateral é substituído pelo ADR-0081;
  - 0063 e 0067: fundo escurecido pelo token `overlay` do design system, já usado no menu do celular;
  - 0039 e 0069 continuam como estão.
- Sem mudança na API nem no banco.
