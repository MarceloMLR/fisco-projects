## Why

Os avisos na hora saem só por e-mail (ADR-0037), e quem passa o dia no Teams só os vê ao abrir a caixa de entrada. Em 02/11/2026, Marcelo viu um esquema da aba Atividade do Teams com os avisos do FiscoProjects e aprovou o aviso no Teams. Escolheu a aba Atividade e a escolha do canal por cada pessoa em Meu perfil (ADR-0149). Avisos-4, fora do roadmap aprovado: a proposta é aprovada por Marcelo antes da implementação.

## What Changes

- **Aviso no Teams:** os avisos que hoje saem por e-mail na hora também chegam na aba Atividade do Teams, para as mesmas pessoas e pelas mesmas regras.
  - O aviso diz o que aconteceu e quem fez, com o projeto e o título do item embaixo.
  - O clique abre uma página do FiscoProjects dentro do Teams, com "Abrir no FiscoProjects", que leva ao item no navegador.
  - O que vai para o resumo diário continua só no resumo e no sino.
- **Envio em segundo plano:** os avisos do Teams entram numa fila no banco, como os e-mails, com novas tentativas.
  - O envio é feito pelo Microsoft Graph, com a identidade do aplicativo e as permissões que o próprio aplicativo do Teams declara, sem permissão nova no Entra ID.
  - Uma implementação local grava os avisos em arquivos fora da produção, para o desenvolvimento e a demonstração.
- **Canal escolhido pela pessoa:** em Meu perfil, o cartão "Avisos por e-mail" vira "Avisos", e a pessoa escolhe por onde chegam os avisos na hora: E-mail, Teams ou Os dois (padrão).
  - Só aparece com os avisos pelo Teams ligados e para quem entra pelo Entra ID. Para os outros, os avisos na hora continuam por e-mail.
  - Quem escolheu só o Teams recebe o aviso por e-mail quando o aplicativo não está instalado para ela ou quando o Teams recusa o envio de vez.
- **Aplicativo do Teams:** na seção Microsoft 365 das Configurações gerais, o administrador:
  - baixa o pacote do aplicativo da instalação, com o nome, o ícone, o endereço e a identificação dela;
  - liga "Avisar pelo Teams" depois que o administrador do Teams da empresa instala o aplicativo.
- **API:**
  - `GET /api/admin/teams-app` traz o pacote do aplicativo, sem os ícones, que a tela monta;
  - `PUT /api/installation` recebe `teamsNotices`;
  - `PUT /api/profile/immediate-notices` grava o canal da pessoa;
  - o perfil traz o canal e se o Teams está disponível para a pessoa.
- **Banco:**
  - a fila dos avisos do Teams no schema `notifications`;
  - o canal de cada pessoa no `users`;
  - "Avisar pelo Teams" e a identificação do aplicativo nas configurações da instalação.
- **Instalação:**
  - o Terraform liga o envio pelo Graph com a entrada pelo Entra;
  - o guia ensina a subir o pacote no centro de administração do Teams e a instalar por política;
  - a aplicação não sobe com o envio pelo Graph sem a entrada pelo Entra, nem com a implementação local em produção.
- **Dados pessoais:** a anonimização e a exclusão do projeto apagam o que a fila do Teams guarda.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos`: o canal do Teams, a fila com novas tentativas, a escolha do canal, a volta ao e-mail, a página do FiscoProjects no Teams, a implementação local e a configuração conferida.
- `perfil`: o cartão "Avisos" com o canal dos avisos na hora.
- `configuracao-da-instalacao`: o aplicativo do Teams e "Avisar pelo Teams" nas Configurações gerais.
- `dados-pessoais`: a fila do Teams na anonimização.
- `infraestrutura`: o envio pelo Teams ligado pelo Terraform e o passo do Teams no guia de instalação.

## Non-goals

- O aviso no celular e a mensagem de um bot no chat (opção B do ADR-0149).
- Posts nos canais das equipes pelo Workflows.
- O FiscoProjects inteiro dentro do Teams, com a entrada pelo Teams, e os dados do item na página do Teams.
- O resumo diário pelo Teams.
- Publicar o aplicativo na loja do Teams.
- Instalar o aplicativo pelo próprio FiscoProjects ou por script: o administrador do Teams sobe o pacote.
- Escolher o canal por tipo de aviso.
- Dados de demonstração novos: as pessoas fictícias entram sem o Entra ID e recebem por e-mail.

## Impact

- `src/Modules/Notifications`:
  - o canal do Teams, ao lado do e-mail e do sino, e a regra do canal de cada pessoa num lugar só;
  - a fila, o envio em segundo plano, o envio pelo Graph e a implementação local;
  - o pacote do aplicativo;
  - a anonimização e a exclusão do projeto;
  - os textos dos avisos do Teams e a migração.
- `src/Modules/Users`: o canal dos avisos na hora no perfil, o vínculo com o Entra no contrato das pessoas e a migração.
- `src/Modules/Administration`: "Avisar pelo Teams" e a identificação do aplicativo nas configurações, com a migração.
- `tests/`:
  - as regras do canal;
  - a fila e o envio com o Graph simulado;
  - o pacote;
  - a API do perfil e das configurações;
  - a anonimização;
  - a validação das configurações.
- `web/src`:
  - o cartão "Avisos" de Meu perfil;
  - "Avisos pelo Teams" na seção Microsoft 365, com o pacote montado na tela (os ícones e o `.zip`);
  - a página `/teams`;
  - os textos, os tipos gerados, o simulador e os testes.
- `infra/terraform`: `Teams__Provider` com a entrada pelo Entra.
- Documentação: o ADR-0149, os ADR-0037 e 0038 apontando para ele, `docs/instalacao.md`, `CLAUDE.md`, `docs/discovery.md`, `docs/glossario.md`, `openspec/config.yaml` e o roadmap.
