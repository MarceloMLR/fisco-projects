## Context

O nome que a pessoa vê vem de `GET /api/installation`. Ele nasce de `InstallationSettings.DefaultName`, gravado pela migração inicial do `Administration` (`HasData`). O frontend escreve `app.name` quando a marca não chega. Os e-mails recebem o nome da instalação no topo e no rodapé (`EmailRenderer`). O pacote do Teams tem o manifesto feito no servidor, com o nome da instalação, e os ícones desenhados na tela (`teamsPackage.ts`); a versão sobe quando o manifesto ou a `iconSource` muda.

## Goals / Non-Goals

**Goals:** o nome GerenWare em tudo o que a pessoa lê; nenhum nome escolhido pelo administrador trocado.

**Non-Goals:** identificadores do código e da infraestrutura.

## Decisions

- **Uma constante do produto no `SharedKernel`.** `Product.Name` e `Product.LogoPath` servem ao `Administration` (nome padrão) e ao `Notifications` (topo do e-mail) sem um módulo conhecer o outro. No frontend, `productName` de `brand.ts` é o mesmo nome, e um teste o compara com `app.name`.
- **Migração com condição.** Mudar o `HasData` geraria um `UpdateData` que trocaria o nome de toda instalação. A migração `ProductName` roda um `UPDATE … WHERE [Name] = N'FiscoProjects'`, e o caminho de volta faz o contrário. O instantâneo do modelo fica com o nome novo.
- **Logo no e-mail como imagem do site.** Programas de e-mail não mostram SVG nem imagem embutida de forma confiável. O topo aponta para `{Email:SiteUrl}/gerenware-horizontal-fundo-claro-64px.png`, cópia de `docs/marca/` em `web/public/`, com 28 px de altura e o nome no texto alternativo, que aparece para quem não carrega imagens. Sem `Email:SiteUrl` ou com outro nome, o nome escrito. Alternativa descartada: anexar a imagem em cada e-mail, que aumenta toda mensagem e muda o contrato do `IEmailSender`.
- **Ícones do Teams dos arquivos da marca.** O colorido usa `gerenware-icone-quadrado-cheio.svg`, porque o Teams arredonda o ícone; o de contorno usa `gerenware-icone-branca.svg`, branco sobre transparente, para toda instalação. A `iconSource` passa a levar o logo (ou o símbolo) e a versão do símbolo, e por isso a versão do pacote sobe no próximo download.
- **Specs trocadas direto.** O nome aparece em 56 lugares do texto das specs. Só o que muda de regra vai por delta (nome padrão, ícones do Teams, e-mails). Ficam de fora desta troca os requisitos que as changes anteriores, ainda não arquivadas, substituem inteiros, e os nomes de código da spec de arquitetura (Marca-5).

## Risks / Trade-offs

- Imagem bloqueada pelo programa de e-mail → o texto alternativo mostra o nome.
- O aplicativo já instalado no Teams continua com o ícone antigo até o administrador subir o pacote novo → a versão maior indica a troca; os ambientes são recriados na Marca-6.
