## 1. API e telas

- [x] 1.1 Tirar a rota `GET /api/client-settings` (`ClientSettings.cs` e o mapeamento no `ApiSetup.cs`) e o `ClientSettingsTests`; teste "Instalação que não guarda a chave" na API (a rota responde 404); `dotnet build -warnaserror` e os testes da API passando
- [x] 1.2 `npm run api:update` (contrato e tipos sem a rota), sem `clientSettingsQuery` nem o simulador da rota em `server.ts`; o teste de tela "Instalação sem chave de licença" passando sem simular a rota (qualquer chamada a ela reprova o teste)

## 2. Infraestrutura e Codespaces

- [x] 2.1 Tirar `syncfusion_license_in_key_vault`, a variável `syncfusion_in_vault` do módulo `app` e a configuração `Syncfusion__LicenseKey`; tirar a linha do `dev.tfvars`; `bash scripts/ci/infra.sh` limpo
- [x] 2.2 `InfrastructureTests` com o teste "Instalação que não guarda a chave" (nada do Terraform nem do guia cita o Syncfusion) e `DevContainerTests` sem chave no `.devcontainer`, passando

## 3. Documentação

- [x] 3.1 `docs/instalacao.md` sem a chave do Syncfusion, com o passo de "Atualizar para uma versão nova" para a instalação que ainda a tem; `docs/demonstracao.md` e `docs/restauracao.md` sem ela; conferido pelo `InfrastructureTests`
- [x] 3.2 ADR-0051 e ADR-0087 com o status "substituída pelo ADR-0134" no arquivo e no índice; `discovery.md`, `glossario.md`, `openspec/config.yaml`, `CLAUDE.md` e o comentário do `frontend.sh` sem o Syncfusion como parte do sistema
- [x] 3.3 Roadmap com a v2 a partir do grupo 13 e a v1 até a Assinatura e os refinamentos do design

## 4. Verificação

- [ ] 4.1 `scripts/ci/build.sh`, `test.sh`, `format.sh`, `frontend.sh`, `infra.sh` e `specs.sh` passando

## Workflow follow-up

- Depois do merge, Marcelo aplica o Terraform no `dev` (o `plan` mostra a configuração `Syncfusion__LicenseKey` saindo) e apaga o segredo `Syncfusion--LicenseKey` do Key Vault, pelo guia.
- Opcional, de Marcelo: apagar o segredo `Syncfusion__LicenseKey` do Codespaces e o `Syncfusion:LicenseKey` dos segredos do usuário.
