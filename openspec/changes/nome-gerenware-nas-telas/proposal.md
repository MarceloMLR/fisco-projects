## Why

As telas já têm as cores e a logo da marca (Marca-2 e Marca-3), mas o produto ainda se apresenta como FiscoProjects: no nome padrão da instalação, no título da aba, em textos das telas, nos e-mails e no pacote do Teams. Esta change troca o nome que as pessoas veem.

## What Changes

- O nome padrão da instalação passa a ser "GerenWare". Uma migração troca o nome só da instalação que ainda mostrava "FiscoProjects"; o nome que o administrador escolheu continua.
- Textos das telas com o nome novo: o nome usado quando a marca não chega, a ajuda dos avisos pelo Teams, o subtítulo de Visual e o título inicial da aba.
- E-mails: o topo mostra a logo GerenWare quando a instalação tem o nome do produto e o site tem endereço; senão, o nome escrito, como hoje. O rodapé ganha onde mudar os avisos.
- Pacote do Teams: sem logo da instalação, o ícone colorido é o símbolo da GerenWare; o ícone de contorno passa a ser o símbolo em branco. Os dois saem dos arquivos da marca, e a versão do pacote sobe sozinha.
- Specs: o nome do produto no texto das specs e nos títulos de um requisito e de três cenários, trocado direto nos arquivos, com os testes que citam esses títulos. É uma troca de texto, sem mudança de regra; o OpenSpec não renomeia cenário por delta.
- Nas exportações (Excel, PDF e XML do MS Project) não havia o nome do produto: elas levam o nome da instalação, e não mudam.

## Capabilities

### New Capabilities

### Modified Capabilities

- `configuracao-da-instalacao`: o nome padrão e os ícones do pacote do Teams.
- `avisos`: a marca nos e-mails.

## Non-goals

- Nomes no código: namespaces, tipos de erro (`urn:fiscoprojects:…`), cookie, métricas, pasta do sistema e chave da conexão (Marca-5).
- Infraestrutura, repositório e documentações (Marca-6 a Marca-8).
- Trocar o nome de uma instalação que o administrador renomeou.

## Impact

- Backend: `Product` no `SharedKernel`, `InstallationSettings.DefaultName`, a migração `ProductName` do `Administration`, `EmailRenderer` e o texto do rodapé.
- Frontend: `pt-BR.json`, `index.html`, `teamsPackage.ts`, `brand.ts`, `web/public/` (a logo dos e-mails) e os testes que esperavam o nome antigo.
- Specs em `openspec/specs/` com o nome novo no texto.
