# Tasks

## 1. Gaveta de detalhes

- [x] 1.1 Criar o `DetailsDrawer` em `web/src/components`:
  - gaveta à direita, modal, com o fundo `overlay`, o papel em `bg-elevated` e `shadow-dialog`, e até 640 px de largura (a largura toda no celular);
  - botão Fechar e conteúdo que rola por dentro;
  - testar "Detalhes abertos por cima da lista", "Fechar os detalhes" (botão, Esc e clique fora, com o foco de volta) e "Detalhes no celular".

  Verificar com `npm test` e `npm run lint`.

## 2. Auditoria

- [x] 2.1 Na `AuditPage`:
  - tirar a coluna fixa e o registro escolhido de início, e usar o `DetailsDrawer` ao escolher uma linha;
  - a linha fica marcada só com a gaveta aberta;
  - no `AuditDetails`, quebrar só textos maiores que a coluna;
  - atualizar os testes "Lista da auditoria" e "Detalhes de um registro" e o do celular.

  Verificar com `npm test`.
- [x] 2.2 No `PersonFilter`, 240 px e reticências no nome que não cabe. Atualizar o teste "Filtro por pessoa". Verificar com `npm test`.

## 3. Verificação final

- [ ] 3.1 Conferir no navegador, com o build de produção, a Auditoria nos temas claro e escuro, no computador e no celular:
  - a gaveta com o fundo escurecido;
  - a tabela de mudanças sem palavras quebradas;
  - o filtro de pessoa com reticências.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - atualizar o glossário (gaveta de detalhes), o `CLAUDE.md` (§6, telas com detalhe usam o `DetailsDrawer`) e o roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-3.1 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, ADR-0081 como aceita, roadmap como "Concluída" e merge.
