# Marca GerenWare

Os arquivos da marca e o resumo dos valores. A decisão está no ADR-0159, e o manual de identidade visual completo, com 35 páginas, em https://claude.ai/artifact/PQ5uaEs2VTiMBz9X2B2s5M.

Use sempre estes arquivos para mostrar a logo. Não redesenhe.

## Pastas

| Pasta | O que tem |
| --- | --- |
| `svg/` | Vetor. Prefira sempre que der (telas, site, impressão) |
| `png/` | Fundo transparente. A logo horizontal com 64, 128 e 256 px de altura; a vertical com 128, 256 e 512 px; o ícone de 64 a 1024 px |
| `favicon/` | `favicon.ico` (16, 32 e 48 px), `favicon.svg`, `favicon-32.png`, `apple-touch-icon.png` (180 px), `android-chrome` (comum e "maskable") e `site.webmanifest` |

Regra de nomes: `gerenware-<versão>-<variante>-<tamanho>px.<extensão>`, tudo em minúsculas, sem acento e com hífen.

- Versão: `horizontal`, `vertical` ou `icone`.
- Variante: `fundo-claro`, `fundo-escuro`, `uma-cor-carvao` ou `branca`. O ícone colorido não tem variante.
- Tamanho: a altura em px, só nos PNG.

## Versões

| Versão | Quando usar |
| --- | --- |
| `horizontal-fundo-claro` | A principal. Fundos brancos e claros |
| `horizontal-fundo-escuro` | Fundos grafite e escuros |
| `horizontal-uma-cor-carvao` | Uma cor só: impressão em uma cor, carimbo, gravação |
| `horizontal-branca` | Fundos coloridos, fotos e escuros em uma cor |
| `vertical-*` | Espaços quadrados ou em pé (verso de cartão, capas) |
| `icone` | Avatar, favicon, ícone de aplicativo |
| `icone-quadrado-cheio` | Só onde o sistema arredonda o ícone sozinho (iPhone, Android, lojas) |

## Símbolo e nome

| Parte | Cor |
| --- | --- |
| Fundo do quadrado | Carvão `#2B3038` |
| Traço do G | Branco `#FFFFFF` |
| Bolinha de início | Azul-céu `#38BDF8` |
| Bolinha de entrega | Esmeralda `#10B981` |
| Nome em fundo claro | "Geren" `#1C1F24` e "Ware" `#047857` |
| Nome em fundo escuro | "Geren" `#FFFFFF` e "Ware" `#6EE7B7` |

O nome usa a Figtree SemiBold (600), já em curvas nos SVG. Na logo horizontal, com o ícone valendo 100: espaço de 29 entre o ícone e o nome, largura total de 508,6 e proporção de 5,09 para 1.

## Cuidados

- Respiro em volta da logo: 1/4 da altura do ícone.
- Tamanho mínimo na tela: ícone com 16 px; horizontal com 24 px de altura; vertical com 64 px de altura.
- Tamanho mínimo no papel: ícone com 5 mm; horizontal com 6 mm de altura; vertical com 12 mm de altura.
- Não troque as cores, não estique, não gire, não ponha sombra nem contorno.

## Cores

| Papel | Claro | Escuro |
| --- | --- | --- |
| Ação | `#047857` | `#047857`, com texto e links em `#6EE7B7` |
| Ação ao apontar | `#065F46` | `#065F46` |
| Ação ao pressionar | `#064E3B` | `#064E3B` |
| Conteúdo | `#FFFFFF` | `#17191D` |
| Barra lateral | `#F6F7F9` | `#121417` |
| Cartões | `#FFFFFF` | `#1C1F24` |
| Menus | `#FFFFFF` | `#23272D` |
| Borda | `#E1E4E8` | `#2B3038` |
| Borda de campo | `#868D97` | `#6B727D` |
| Texto | `#1C1F24` | `#E1E4E8` |
| Texto secundário | `#4B525C` | `#AEB4BC` |

Escala grafite, de 0 a 950: `#FFFFFF`, `#F6F7F9`, `#EFF1F4`, `#E1E4E8`, `#C9CDD3`, `#AEB4BC`, `#868D97`, `#6B727D`, `#4B525C`, `#3B3F46`, `#2B3038`, `#23272D`, `#1C1F24`, `#17191D` e `#121417` (0, 50, 100, 200, 300, 400, 500, 600, 700, 750, 800, 850, 900, 925 e 950).

| Status | Claro (texto, fundo, gráfico) | Escuro (texto, fundo, gráfico) |
| --- | --- | --- |
| Concluído | `#047857`, `#E3F5EC`, `#059669` | `#6EE7B7`, `#16332D`, `#10B981` |
| Em andamento | `#0369A1`, `#E3F2FB`, `#0284C7` | `#7DD3FC`, `#1C3340`, `#38BDF8` |

Aguardando, atrasado e neutro continuam com os valores do design system.

Página de entrada: fundo `#121417` com brilho `#0F3A2C`; painel em degradê de `#3B3F46` a `#1C1F24`; texto secundário do painel `#C9CDD3`; destaque `#6EE7B7`.

Regras:

- Esmeralda sólida com texto branco é sempre ação. Concluído é sempre etiqueta suave com ícone e texto, ou a parte cheia de uma barra.
- O azul-céu só quer dizer "em andamento". Não entra em botão, link, item ativo nem aviso.
- `#10B981`, `#38BDF8`, `#6EE7B7` e `#C9CDD3` só em fundo escuro e no símbolo.

## Tipografia

- IBM Plex Sans nas telas, nos e-mails e nas exportações. IBM Plex Mono em códigos.
- Figtree só no nome da logo.

## Ícones do navegador e do celular

O `site.webmanifest` usa o grafite `#121417` como cor do tema. O `apple-touch-icon.png` e os `android-chrome-maskable` têm o quadrado cheio, porque o sistema arredonda o ícone.
