---
status: aceita
date: 2026-11-17
decision-makers: Marcelo Ribeiro
---

# ADR-0159: Marca GerenWare, com o caminho em G, grafite e esmeralda

Substitui:

- no discovery ("Definições sem ADR"), o nome do produto, FiscoProjects;
- no ADR-0063, o azul corporativo como cor de ação, o verde e o azul das cores de status e os valores dos neutros;
- no ADR-0065, a linha "Ícones" (o nome do conjunto) e o canto das etiquetas de status;
- no ADR-0067, o marinho da página de entrada, o verde do concluído e o canto das etiquetas de status;
- no ADR-0069, a cor do líder técnico na conversa do chamado.

Complementa o ADR-0062 (a direção sóbria continua) e o ADR-0064 (a IBM Plex Sans continua a fonte das telas). As cores da instalação (ADR-0146 e ADR-0147) continuam valendo sobre os valores padrão.

## Contexto e problema

O produto nasceu como FiscoProjects, com o azul corporativo (ADR-0063) e sem logo: a identidade vinha da tipografia e da página de entrada (ADR-0067). Para apresentar e vender o produto (ADR-0002), Marcelo escolheu um nome e uma marca próprios, GerenWare, depois de uma pesquisa de nomes, e aprovou em 16/11/2026 o manual de identidade visual, com o símbolo, as cores, as versões da logo e as regras de uso. Em 17/11/2026, pediu que o produto inteiro passe a usar a marca (grupo 17 do roadmap). O que da identidade atual muda, e o que fica?

## Fatores de decisão

- Um nome e um símbolo próprios, que se possa registrar e mostrar a clientes.
- Telas calmas para uso o dia inteiro (ADR-0062) e contraste AA nos dois temas.
- Status inconfundível com a cor de ação, sem depender só da cor (ADR-0065).
- Cada instalação continua podendo usar as próprias cores e o próprio logo (ADR-0146).
- O histórico do projeto continua legível como foi escrito (ADR-0001).

## Opções consideradas

- A. Manter o nome FiscoProjects e a identidade atual
- B. Trocar só o nome, com o visual azul atual
- C. A marca GerenWare inteira: o nome, o símbolo e a paleta grafite e esmeralda do manual

## Resultado da decisão

Opção escolhida: **C**, aprovada por Marcelo com o manual, em 16/11/2026, e com o rebranding, em 17/11/2026.

A referência visual é o manual de identidade visual da GerenWare (https://claude.ai/artifact/PQ5uaEs2VTiMBz9X2B2s5M). Os arquivos da marca e o resumo dos valores ficam em `docs/marca/`.

### Nome

- O produto se chama **GerenWare**, escrito junto, com G e W maiúsculos. Vem de "gerenciar" e "software", e se fala "JÊ-ren-uér".
- Nunca "Gerenware", "Geren Ware", "GERENWARE" nem abreviado.
- O nome antigo continua nos ADRs aceitos, nas changes arquivadas e nos registros antigos do roadmap.

### Símbolo e logo

- O símbolo é o **caminho em G**: um quadrado grafite de cantos arredondados, com um G branco de traço redondo que começa numa bolinha azul-céu (o início) e termina numa bolinha esmeralda (a entrega).
- O nome da logo usa a **Figtree SemiBold**, só ali e já convertida em curvas nos arquivos: "Geren" em grafite e "Ware" em esmeralda no fundo claro, "Geren" em branco e "Ware" em esmeralda clara no fundo escuro. Nenhum arquivo da Figtree é servido pela aplicação.
- Versões: horizontal (a principal), vertical e ícone, cada uma em fundo claro, fundo escuro, uma cor (carvão) e branca.
- A logo vem sempre dos arquivos de `docs/marca/`. Não se redesenha, não se troca a cor, não se estica, não se gira e não ganha sombra nem contorno.
- Área de proteção: 1/4 da altura do ícone. Tamanhos mínimos: ícone com 16 px ou 5 mm, horizontal com 24 px ou 6 mm de altura, vertical com 64 px ou 12 mm de altura.

### Cores

| Item | Definição |
| --- | --- |
| Ação | Esmeralda: `#047857`, com `#065F46` ao apontar e `#064E3B` ao pressionar, em botão principal, link e item ativo, com texto branco sobre ela. No escuro, o texto e os links de ação usam a esmeralda clara `#6EE7B7` |
| Ação × concluído | As duas são da mesma família esmeralda e se separam pela forma: esmeralda sólida com texto branco é sempre ação; concluído é sempre etiqueta suave com ícone e texto, ou a parte cheia de uma barra. Os tokens ficam separados (`action-*` e `status-success-*`): quando a instalação troca a cor principal, o concluído continua esmeralda |
| Azul-céu | `#38BDF8` tem um só significado, "em andamento", além do símbolo. Não entra em botão, link, item ativo, área grande nem aviso informativo, que fica em grafite |
| Onde cada tom pode | `#10B981`, `#38BDF8`, `#6EE7B7` e `#C9CDD3` só em fundo escuro e no símbolo. No claro, os gráficos usam `#059669` e `#0284C7`, e o texto de "em andamento" usa `#0369A1` |
| Neutros | Escala grafite de 15 tons, de 0 a 950, a partir dos tons da marca (`#2B3038`, `#1C1F24`, `#121417`). Tema claro: conteúdo `#FFFFFF`, barra lateral `#F6F7F9`, borda `#E1E4E8`, borda de campo `#868D97`, texto `#1C1F24` e secundário `#4B525C`. Tema escuro: barra lateral `#121417`, conteúdo `#17191D`, cartões `#1C1F24`, menus `#23272D`, borda `#2B3038`, borda de campo `#6B727D`, texto `#E1E4E8` e secundário `#AEB4BC` |
| Concluído | Claro: texto `#047857`, fundo `#E3F5EC`, gráfico `#059669`. Escuro: texto `#6EE7B7`, fundo `#16332D`, gráfico `#10B981` |
| Em andamento | Claro: texto `#0369A1`, fundo `#E3F2FB`, gráfico `#0284C7`. Escuro: texto `#7DD3FC`, fundo `#1C3340`, gráfico `#38BDF8` |
| Aguardando, atrasado e neutro | Como estão no design system |
| Página de entrada | Grafite no lugar do marinho: fundo `#121417` com brilho `#0F3A2C`, painel em degradê de `#3B3F46` a `#1C1F24`, com a logo, a frase e as formas discretas (anéis, pontos e cartões em camadas); o texto secundário do painel em `#C9CDD3` e o destaque em `#6EE7B7`. Continua o único lugar com degradê |
| Líder técnico na conversa | Azul, o tom que era do responsável (avatar `#1F5FBF` no claro e `#2C69D1` no escuro). O verde-azulado de antes fica quase igual à esmeralda, e o responsável, que segue a cor principal, passa a falar em esmeralda |
| Contraste | AA (WCAG) em textos e controles, nos dois temas, conferido na troca dos tokens |
| Cores da instalação | Continuam como no ADR-0146 e no ADR-0147: a principal, a secundária e a da barra lateral da instalação valem sobre estes valores, que são os de quem não escolheu nenhuma |

### Forma e ícones

- Etiquetas de status com canto de **3 px**, como as etiquetas de papel (ADR-0067). Havia 4 px, 6 px e 3 px em lugares diferentes.
- Os ícones são os **Material Icons no estilo Outlined** (as variantes `…Outlined` do `@mui/icons-material`). O ADR-0065 dizia "Material Symbols", que é outro conjunto.
- O resto da forma continua: bordas em vez de sombras, cantos por tamanho da peça, status por ícone, cor e texto.

### Tipografia

- IBM Plex Sans nas telas, nos e-mails e nas exportações, e IBM Plex Mono em códigos, como no ADR-0064.
- Figtree só no nome da logo, em curvas.

### Onde a decisão entra

Este ADR registra a marca. As changes Marca-2 a Marca-8 do roadmap a aplicam: os tokens e o design system, a logo e a página de entrada, o nome nas telas, o código, a infraestrutura, o repositório e as documentações. Até lá, o produto continua com o nome e o visual anteriores.

### Consequências

- Bom, porque o produto ganha um nome e um símbolo próprios, com regras de uso num lugar só.
- Bom, porque a cor de ação deixa de se confundir com o azul de "em andamento", problema apontado no ADR-0063.
- Ruim, porque a cor de ação e o concluído ficam na mesma família; a forma (sólido para ação, etiqueta suave com ícone para status) passa a carregar a diferença.
- Ruim, porque a troca toca o produto inteiro: telas, código, Azure, GitHub e documentos.
- Neutro, porque as instalações com cores próprias não mudam; só o concluído e o "em andamento" passam aos tons novos.
- Neutro, porque o domínio gerenware.com.br aparece nos exemplos do manual e ainda precisa ser registrado.

### Confirmação

- `docs/marca/` tem os arquivos do pacote da marca e o resumo dos valores, iguais aos do manual.
- Na Marca-2, os testes do tema conferem os valores acima e o contraste AA nos dois temas.
- Na Marca-3 e na Marca-4, os testes conferem a logo, a página de entrada e o nome nas telas.

## Prós e contras das opções

### A. Manter o nome e a identidade atual

- Bom, porque não muda nada.
- Ruim, porque o produto continua sem logo e com um nome que Marcelo não quer levar a clientes.

### B. Trocar só o nome

- Bom, porque é a menor troca.
- Ruim, porque o azul não combina com o símbolo grafite e esmeralda, e a marca ficaria diferente dentro e fora do produto.

### C. A marca GerenWare inteira

- Bom, porque o manual, os materiais e o produto ficam iguais.
- Ruim, porque é a troca maior, feita em oito changes.
