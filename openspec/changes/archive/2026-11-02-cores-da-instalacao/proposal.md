## Why

Hoje a instalação troca só a cor principal, e só uma cor com 4,5:1 de contraste com o branco (ADR-0082, ADR-0103). Marcas amarelas, verde-limão ou claras são recusadas, e o seletor do navegador muda de um sistema para outro. Em 01/11/2026, Marcelo pediu que o cliente personalize as cores pela marca da empresa, sem esse limite, num seletor melhor e em vários formatos, e aprovou o protótipo da seção Cores (ADR-0146). Acesso-8.5.

## What Changes

- **Três cores opcionais:** a principal, a secundária e a da barra lateral, em `#RRGGBB`. Sem uma delas, vale o design system.
  - **Principal:** botões, links, abas, controles marcados, anel de foco, avatar do responsável, selo e item ativo da barra lateral.
  - **Secundária:** linha selecionada, notificação não lida, menção e painel da entrada. Sem ela, o painel segue a principal, como hoje.
  - **Barra lateral:** o fundo dela; os textos, o item ativo e as divisórias acompanham.
- **Sem limite de contraste:** a cor sem 4,5:1 com o branco passa a ser aceita (**BREAKING** para a regra do ADR-0082, não para a API).
  - O texto sobre a cor fica branco ou escuro, o que tiver contraste.
  - Os links, as abas, os contornos e os textos da barra lateral escurecem só até o contraste mínimo.
  - Cada ajuste aparece como aviso informativo embaixo do campo, nunca como erro.
- **Seletor próprio:** área de saturação e brilho, barra de matiz, a cor atual e a nova, e o valor em HEX, RGB ou HSL, com "Cancelar" e "Usar esta cor". Funciona pelo ponteiro e pelo teclado, como popover no computador e folha no celular. O campo aceita a cor digitada ou colada em qualquer desses formatos.
- **Seção Cores** em Administração → Instalação (`/admin/colors`), depois de Configurações gerais. Tem os três campos, a prévia grande com Claro e Escuro, e "Descartar" e "Salvar alterações".
- **API:**
  - nova rota `PUT /api/installation/colors` (`ChangeInstallationColors`), só do administrador, com `mainColor`, `secondaryColor` e `sidebarColor` (nulo volta ao padrão). Cor fora do formato dá 400 no campo;
  - `GET /api/installation` traz as três cores;
  - `PUT /api/installation` deixa de receber `mainColor` (**BREAKING** no contrato, que só as telas usam).
- **Banco:** a migração inclui `SecondaryColor` e `SidebarColor` nas configurações da instalação; as três cores entram na auditoria.
- **Configurações gerais:** fica com o nome e os logos na seção Marca, com a prévia pequena, e perde o campo da cor.
- **Logo escuro:** a barra lateral escura, inclusive no tema claro com uma cor escura na barra, mostra o logo para o tema escuro (ADR-0100).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `configuracao-da-instalacao`:
  - a cor principal sem recusa por contraste;
  - a cor secundária e a da barra lateral;
  - o contraste calculado sobre as cores;
  - o seletor próprio;
  - a seção Cores;
  - a marca sem sessão com as três cores;
  - a auditoria das cores;
  - Configurações gerais sem a cor;
  - o logo escuro na barra lateral escura do tema claro;
  - o selo da aba com o texto calculado.

## Non-goals

- **Tirar as cores do logo** e o conta-gotas: não foram escolhidos.
- **Cores por papel na conversa dos chamados:** continuam as do design system (ADR-0069, ADR-0103).
- **E-mails:** continuam com as cores do `tokens.json` no tema claro (ADR-0036).
- **Cores de status, superfícies, textos neutros e o fundo atrás do painel da entrada:** não mudam (ADR-0065).
- **Formatos na API:** ela recebe só `#RRGGBB`; a leitura dos outros formatos é da tela.
- **Mudar o design system publicado:** a regra "nunca cor de marca" na barra lateral passa a valer só sem a cor da barra lateral, como no ADR-0100.

## Impact

- `src/Modules/Administration`:
  - `InstallationSettings` com as três cores, só pelo formato;
  - o caso de uso `ChangeInstallationColors` e a rota `PUT /api/installation/colors`;
  - `UpdateInstallation` e `PUT /api/installation` sem a cor;
  - `GET /api/installation` com as três cores;
  - a migração das duas colunas.
- `tests/`: os testes das configurações e da API que mandam `mainColor` no `PUT /api/installation`.
- `web/src`:
  - `theme/brandColors.ts`: a derivação das três cores, o texto sobre a cor e a barra lateral escura pela cor;
  - `BrandColors`, `Sidebar` (logo escuro), `favicon.ts` e a página de entrada;
  - o seletor e o campo de cor novos (no lugar do `MainColorField`) e a leitura dos formatos;
  - a página `ColorsPage` com a prévia grande, a rota, a coluna da Administração e o `SettingsPage` sem a cor;
  - a descrição na Auditoria (`auditText.ts`), os textos, os tipos gerados e os testes.
- Documentação: o ADR-0146, o README dos ADRs, `CLAUDE.md`, `docs/glossario.md` e o roadmap (ADR-0003, ADR-0046, ADR-0065, ADR-0082, ADR-0100, ADR-0103, ADR-0108).
