## 1. Decisão e roadmap

- [x] 1.1 ADR-0149, os ADR-0037 e 0038 apontando para ele, o índice dos ADRs, a Avisos-4 em andamento no roadmap e o Teams no `openspec/config.yaml`; `openspec validate --all --strict` sem erros

## 2. Canal de cada pessoa

- [x] 2.1 O vínculo com o Entra no `IUserDirectory`; a escolha do canal em `notifications.notice_preferences`, fora da auditoria, com a migração; `GET` e `PUT /api/profile/immediate-notices` com `channel` e `teamsAvailable`; testes da API "Escolher o Teams" e "Canal de quem nunca escolheu"
- [x] 2.2 `NoticeRouting` com a regra do canal num lugar só, a disponibilidade do Teams para a pessoa e o `EmailChannel` sem o e-mail de quem fica só com o Teams; testes das regras "Só por e-mail", "Só pelo Teams", "Pelos dois", "Pessoa sem o Entra ID" e "Teams desligado na instalação"

## 3. Fila e envio para o Teams

- [x] 3.1 A fila `notifications.teams_notices` com a migração, o `TeamsChannel` e os textos dos avisos do Teams nos arquivos de tradução; testes "Chamado atribuído no Teams", "Quem fez a ação não recebe no Teams", "Aviso do resumo fora do Teams", "Sino com qualquer canal" e "Ação sem esperar o Teams"
- [x] 3.2 O `TeamsDispatcher`, o `ITeamsSender` com o `GraphTeamsSender` e o `LocalTeamsSender`, a volta ao e-mail e a limpeza; o `FakeGraph` com as instalações e os avisos do Teams, e o `SessionApp` guardando os enviados; testes "Texto do aviso", "Link do aviso", "Aplicativo não instalado para a pessoa", "Teams recusa o envio", "Recusa com os dois canais", "Teams fora do ar", "Fila limpa com o tempo" e "Aviso gravado sem envio"
- [x] 3.3 O `TeamsOptionsValidation`; testes "Implementação local do Teams em produção" e "Teams sem a entrada pelo Entra"

## 4. Aplicativo do Teams na instalação

- [x] 4.1 O `TeamsApp` ("Avisar pelo Teams", o id do manifesto e a versão do pacote) no `notifications`, com a migração e a auditoria; `GET` e `PUT /api/admin/teams-app`; testes "Ligar os avisos pelo Teams", "Ligar sem o Teams na instalação" e "Opção de outro papel"
- [x] 4.2 O `TeamsAppManifest` e `POST /api/admin/teams-app/package`; testes "Conteúdo do pacote", "Pacote de outro papel", "Versão nova do pacote" e "Pacote sem o Teams na instalação"

## 5. Dados pessoais e exclusão do projeto

- [x] 5.1 A fila do Teams no `NotificationsPersonalData` e no `NotificationProjectDeletion`; testes "Aviso do Teams esperando envio", "Nome em avisos de outras pessoas" e "Projeto excluído"

## 6. Telas

- [x] 6.1 `npm run api:update`; o cartão "Avisos" de Meu perfil com o canal dos avisos na hora; testes de tela "Escolher o Teams", "Avisos na hora sempre ligados" e "Desligar o resumo"
- [ ] 6.2 "Avisos pelo Teams" na seção Microsoft 365 das Configurações gerais, com "Avisar pelo Teams", a versão e "Baixar pacote do Teams", que monta os ícones e o `.zip` na tela; testes de tela "Pacote do administrador" e "Ligar os avisos pelo Teams"
- [ ] 6.3 A página `/teams`, fora da estrutura e sem sessão, com o `safeItemPath`; testes "Abrir o item no navegador", "Página sem item" e "Endereço de fora recusado"
- [ ] 6.4 Conferência no navegador do cartão Avisos, da seção Microsoft 365 e da página `/teams`, no computador e no celular, nos temas claro e escuro

## 7. Instalação e documentação

- [ ] 7.1 `Teams__Provider` no `entra_settings` do Terraform, com o `InfrastructureTests`; o passo do aplicativo do Teams e a referência em `docs/instalacao.md`; testes "Teams pelo Terraform" e "Passo do Teams no guia"; `bash scripts/ci/infra.sh` limpo
- [ ] 7.2 `CLAUDE.md`, `docs/discovery.md` e `docs/glossario.md`

## Workflow follow-up

- Conferência por Marcelo no `dev`, com o Teams da avaliação do Microsoft 365: subir o pacote, instalar para ele, ligar "Avisar pelo Teams" e receber o aviso de um chamado atribuído.
- PR, aprovação de Marcelo, `/opsx:archive` na mesma branch, roadmap "Concluída" e merge por Marcelo.
