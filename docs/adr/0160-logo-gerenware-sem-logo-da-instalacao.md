---
status: aceita
date: 2026-11-17
decision-makers: Marcelo Ribeiro
---

# ADR-0160: Logo GerenWare onde a instalação não tem logo

Complementa o ADR-0159 (a marca). Substitui, no ADR-0108, o selo da barra lateral como ícone da aba sem logo; e o selo com o ícone do cronograma na cor principal (ADR-0066, ADR-0100, ADR-0146) na barra lateral, na página de entrada e na prévia das cores.

## Contexto e problema

Sem logo da instalação, a barra lateral, o painel da entrada e a aba do navegador mostram um selo: o ícone do cronograma sobre a cor principal, ao lado do nome. O produto agora tem logo (ADR-0159). Onde ela aparece, e o que acontece com o nome e as cores de uma instalação que não enviou logo?

## Fatores de decisão

- A logo vem dos arquivos da marca, sem redesenho e sem trocar de cor (ADR-0159).
- A instalação que enviou o próprio logo não muda.
- A instalação que só trocou o nome continua vendo o nome dela.
- O ícone da aba aparece desde o primeiro instante, antes de a marca da instalação chegar.

## Opções consideradas

- A. A logo GerenWare inteira quando a instalação mantém o nome do produto; o símbolo ao lado do nome quando ela tem outro nome
- B. Sempre o símbolo ao lado do nome escrito em IBM Plex Sans
- C. Manter o selo na cor principal e usar a logo só na página de entrada

## Resultado da decisão

Opção escolhida: **A**, porque mostra a logo como o manual a desenha onde o nome é o do produto, e não esconde o nome de quem escolheu outro.

Regras:

| Item | Definição |
| --- | --- |
| Com logo da instalação | Nada muda: o logo dela na barra lateral (o do tema escuro na barra escura), no painel da entrada e na aba |
| Sem logo, com o nome GerenWare | A logo horizontal no lugar do selo e do nome: a de fundo claro na barra lateral clara, a de fundo escuro na barra escura e no painel da entrada. O nome fica no texto alternativo da imagem |
| Sem logo, com outro nome | O símbolo (o G) no lugar do selo, ao lado do nome escrito |
| Tamanho | 28 px de altura na barra lateral, 32 px no painel da entrada e 24 px na prévia das cores; nunca abaixo dos mínimos do manual |
| Cores | A logo e o símbolo não mudam com as cores da instalação |
| Aba do navegador | Sem logo, o `favicon.svg` da marca, que a página já traz no HTML; com logo, o da instalação, depois de a marca chegar |
| Celular e atalhos | `apple-touch-icon.png`, os ícones do Android (comuns e "maskable") e o `site.webmanifest` da marca, servidos na raiz do site |
| Arquivos | Cópias exatas de `docs/marca/`, em `web/src/assets/brand/` e `web/public/`; um teste falha se uma cópia fica diferente |
| Página de entrada | A frase e o apoio do manual: "Implantação e suporte dos seus clientes em um só lugar." e "Cronogramas, chamados e aceites, do primeiro dia do projeto ao suporte depois da entrada em produção." |

O ícone do aplicativo do Teams fica para a change do nome nas telas (Marca-4), com o pacote do Teams.

### Consequências

- Bom, porque a marca aparece igual ao manual, sem desenho novo.
- Bom, porque a aba tem ícone antes de a marca da instalação chegar, sem piscar.
- Ruim, porque a instalação sem logo deixa de ter um selo na cor principal: o símbolo tem as cores da marca GerenWare. Quem quer a própria identidade envia o logo.
- Neutro, porque o `site.webmanifest` é um arquivo fixo, com o nome GerenWare: o atalho na tela do celular leva o nome do produto, não o da instalação.

### Confirmação

Testes cobrindo: as cópias iguais às de `docs/marca/`; a logo inteira com o nome do produto e o símbolo com outro nome, na barra lateral e na entrada; o logo da instalação no lugar dos dois; a aba com o símbolo sem logo e com o logo da instalação; os ícones e o manifesto no HTML.

## Prós e contras das opções

### A. Logo inteira ou símbolo com o nome

- Bom, porque respeita a logo e o nome da instalação.
- Ruim, porque são dois desenhos conforme o nome.

### B. Sempre o símbolo com o nome escrito

- Bom, porque é um desenho só.
- Ruim, porque o nome GerenWare apareceria numa fonte que não é a da logo.

### C. Manter o selo

- Bom, porque acompanha a cor principal.
- Ruim, porque o produto continuaria sem a logo nas telas.
