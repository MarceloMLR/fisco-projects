---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0066: Estrutura das telas, com barra lateral de altura total, cabeçalho invisível e conteúdo na altura da janela

Substitui o item "Rodapé da barra: nome da pessoa, troca de tema e Sair" do ADR-0011 e o item "Sino na barra lateral com o número de notificações não lidas" do ADR-0038, que colocava o sino na barra lateral.

## Contexto e problema

O ADR-0011 fixou a barra lateral com o rodapé da conta, e o ADR-0038 pôs o sino na barra lateral. Na revisão das telas, Marcelo apontou três problemas: o conteúdo ficava encolhido no canto superior esquerdo com o resto da tela em branco, a barra lateral carregava informação demais no rodapé e as notificações não estavam onde as pessoas procuram. Uma segunda versão, com moldura marinho em L e o conteúdo numa "folha" recuada, também foi recusada: a barra lateral parecia cortada em cima e o cabeçalho pesava. Como organizar a estrutura das telas?

## Fatores de decisão

- O conteúdo deve usar a tela inteira, também em monitores grandes.
- Conta e notificações no lugar esperado (canto superior direito).
- Navegação previsível e sempre visível (ADR-0011).
- Telas com listas longas e conversas de até 50 interações sem rolar a página inteira.

## Opções consideradas

- A. Barra lateral com rodapé da conta e sino (ADR-0011 e ADR-0038 originais)
- B. Moldura marinho em L, com o conteúdo numa folha recuada
- C. Barra lateral clara de altura total com o nome da instalação no topo, cabeçalho invisível e conteúdo na altura da janela

## Resultado da decisão

Opção escolhida: **C. Barra lateral de altura total, cabeçalho invisível e conteúdo na altura da janela**, aprovada por Marcelo na revisão do design.

Escopo:

| Item | Classificação |
| --- | --- |
| Barra lateral fixa, da altura da janela, com o nome (ou o logo) da instalação no topo, Início, Projetos, Chamados, Administração (só administradores) e "Meus projetos" (nome e código de cada projeto da pessoa) | v1 |
| Cabeçalho invisível sobre o conteúdo (mesma cor, sem linha), com a trilha de navegação, o sino com contador vermelho e o avatar, que abre Meu perfil, Tema e Sair | v1 |
| Painel de notificações aberto pelo sino, agrupado por dia, com filtro "Não lidas" e "Todas" (regras de conteúdo no ADR-0038) | v1 |
| Telas de trabalho na altura da janela: a página não rola; tabelas, conversa do chamado, linha do tempo e painéis rolam por dentro, com a paginação colada embaixo | v1 |
| Detalhe do item selecionado num painel lateral (documentos, termos, membros, auditoria, chamado) | v1 |
| Celular: barra superior com menu, título e sino; menu em gaveta com a navegação e Meus projetos (ADR-0012) | v1 |
| Barra lateral recolhível | fora (mantém a decisão do ADR-0011) |
| Busca global | depois (mantém o ADR-0011) |

### Consequências

- Bom, porque o conteúdo ocupa a tela inteira em qualquer tamanho de monitor.
- Bom, porque conta e notificações ficam no canto superior direito, onde as pessoas procuram.
- Bom, porque listas e conversas longas rolam por dentro, mantendo cabeçalho, filtros e caixa de resposta à vista.
- Ruim, porque a rolagem interna exige cuidado com teclado, foco e impressão.
- Neutro, porque telas com pouco conteúdo (como Meu perfil) ainda mostram espaço livre em monitores grandes.

### Confirmação

Testes de interface (ADR-0056) em 1440×900 e 1920×1080 cobrindo: a página não rola e as regiões longas rolam por dentro; o item Administração só aparece para administradores; o contador do sino mostra só as não lidas; o menu do avatar oferece Meu perfil, Tema e Sair. As telas de referência estão no canvas do Claude Design.

## Prós e contras das opções

### A. Rodapé da conta e sino na barra lateral

- Bom, porque concentra tudo num lugar.
- Ruim, porque pesa a barra lateral e esconde as notificações.

### B. Moldura marinho com folha

- Bom, porque dá identidade forte.
- Ruim, porque a barra lateral parece cortada e a moldura pesa no uso diário.

### C. Barra lateral de altura total e cabeçalho invisível

- Bom, porque é limpa e aproveita a tela.
- Ruim, porque a identidade depende de outros detalhes (ADR-0067).
