# Tasks

## 1. Tema

- [x] 1.1 Copiar o `tokens.json` do design system, gerar as variáveis CSS e o tema do MUI (claro, escuro, automático) e instalar MUI, Emotion e as fontes `@fontsource` (OFL-1.1 na lista de licenças); testar "Cores dos tokens nos dois temas", "Cor escrita direto no código", "Fonte servida pela aplicação", "Contraste mínimo" e "Tema segue o sistema"; verificar com `npm test`, `npm run lint`, `npm run typecheck` e `npm run build`

## 2. Estrutura das telas

- [x] 2.1 Criar as rotas com React Router e as páginas Início, Projetos e Chamados com o título; testar "Endereço de uma página" e "Endereço desconhecido"; verificar com `npm test`
- [x] 2.2 Criar a estrutura no computador (barra lateral, cabeçalho com a trilha e conteúdo na altura da janela); testar "Navegação principal", "Item da página atual", "Trilha da página" e "Janela sem rolagem"; verificar com `npm test`
- [x] 2.3 Criar a estrutura no celular (barra superior e menu); testar "Menu no celular"; verificar com `npm test`

## 3. Verificação final

- [ ] 3.1 Conferir as telas no Chromium, no computador e no celular, nos temas claro e escuro, contra o canvas aprovado, e registrar as capturas para o resumo
- [ ] 3.2 Rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Fundação-5 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: ADR-0077 como aceito, `/opsx:archive`, roadmap como "Concluída" e merge.
