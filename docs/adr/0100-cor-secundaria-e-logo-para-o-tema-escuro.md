---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0100: Cor secundária da instalação na barra lateral, nos destaques de apoio e no painel da entrada, com logo para o tema escuro

Complementa o ADR-0082 (cor principal da instalação) e o ADR-0067 (identidade visual revisada).

## Contexto e problema

Pelo ADR-0082, a instalação troca só a cor principal, e dela saem o botão principal, os links, os controles marcados, o item ativo da barra lateral e o painel da entrada. Marcelo pediu mais personalização: uma cor secundária, um seletor de cores ao clicar na cor e um segundo logo, para o tema escuro. O design system diz que a barra lateral é "um cinza levíssimo, nunca cor de marca", e o logo único some sobre um fundo escuro quando é escuro também. Onde a cor secundária entra, como os textos sobre ela ficam legíveis, e qual logo aparece onde?

## Fatores de decisão

- A empresa reconhece a própria marca nas telas.
- Sem a cor secundária, nada muda: as telas continuam exatamente como o design system.
- Todo texto sobre a cor escolhida tem o contraste do WCAG AA, em qualquer cor e nos dois temas.
- As cores de status não mudam (ADR-0065).

## Opções consideradas

Onde a cor secundária entra (escolha de Marcelo entre barra lateral, destaques de apoio e painel da entrada):

- A. Na barra lateral, nos destaques de apoio e no painel da entrada
- B. Só no painel da entrada
- C. Só como um tom suave da barra lateral

Contraste sobre a cor secundária:

- D. Qualquer cor, com os textos calculados para ter contraste
- E. Só cores com contraste com o branco, como a cor principal

## Resultado da decisão

Opções escolhidas: **A**, por escolha de Marcelo, e **D**, porque a barra lateral pode ser clara ou escura e os textos acompanham.

Regras:

- A cor secundária é opcional, em hexadecimal; sem ela, valem os tokens do design system. A regra "nunca cor de marca" na barra lateral passa a valer só sem a cor secundária, por escolha de Marcelo.
- Barra lateral: no tema claro, o fundo é a própria cor; no escuro, um tom escuro do mesmo matiz. Os textos, os contadores, o item sob o ponteiro, o item ativo, as divisórias e o nome da instalação saem do fundo, com contraste de 4,5:1 (3:1 para o filete do item ativo, que é a cor principal quando ela aparece sobre o fundo). O nome da instalação usa a variável `--sidebar-title`, que só existe com a cor secundária; sem ela, vale o `--text` do design system.
- Destaques de apoio: a linha selecionada, a notificação não lida e a menção (`bg-selected`) mantêm a claridade do design system e ganham o matiz da cor secundária.
- Painel da entrada: o degradê sai da cor secundária no lugar da principal, escurecido até o texto branco ter 4,5:1.
- A cor principal continua no botão principal, nos links, nos controles e no anel de foco.
- Seletor de cores: ao clicar na amostra da cor principal ou da secundária, abre o seletor de cores do navegador; a cor escolhida vai para o campo em hexadecimal, com as mesmas regras de quem digita.
- Logo para o tema escuro: opcional, com as regras do logo (PNG ou SVG, até 1 MB, sem script), guardado ao lado dele (`installation-logo-dark`). A barra lateral mostra o logo escuro quando ela é escura (o tema escuro, ou uma cor secundária escura no tema claro); sem ele, mostra o logo. O selo claro do painel da entrada continua com o logo.
- A auditoria registra a cor secundária e o nome do arquivo do logo escuro, como a cor principal e o logo.

### Consequências

- Bom, porque a marca da empresa aparece na barra lateral e na entrada, com o contraste garantido pelo cálculo.
- Bom, porque sem a cor secundária as telas continuam as do design system.
- Ruim, porque uma cor muito saturada na barra lateral pesa mais que o cinza do design system; a Prévia mostra antes de salvar.
- Ruim, porque o design system ganha uma variável fora dele (`--sidebar-title`), só para o nome na barra lateral.

### Confirmação

Testes cobrindo: sem cor secundária, nenhum token muda; com ela, a barra lateral, a linha selecionada e o painel mudam nos dois temas; contraste de 4,5:1 dos textos da barra lateral e do painel para cores claras, escuras, branco e preto; cor fora do formato recusada pela API; logo escuro enviado e removido sem mexer no logo; a barra lateral escura mostra o logo escuro; o seletor de cores preenche o campo.

## Prós e contras das opções

### A. Barra lateral, destaques e painel

- Bom, porque a marca aparece onde a pessoa olha sempre.
- Ruim, porque contraria o design system quando ligada.

### B. Só o painel da entrada

- Bom, porque não mexe nas telas de trabalho.
- Ruim, porque quase ninguém veria a cor.

### C. Só um tom suave na barra lateral

- Bom, porque fica perto do design system.
- Ruim, porque a cor escolhida quase não aparece.

### D. Qualquer cor, com textos calculados

- Bom, porque aceita a cor da empresa como ela é.
- Ruim, porque os textos da barra lateral podem ficar claros sobre uma cor escura.

### E. Só cores com contraste com o branco

- Bom, porque repete a regra da cor principal.
- Ruim, porque recusaria as cores claras, que combinam com uma barra lateral clara.
