## 1. Decisão

- [x] 1.1 ADR-0130 com as decisões da integração com a Autentique, no índice dos ADRs

## 2. Provedor da Autentique

- [x] 2.1 Opções `Signature:Autentique:*` e `Signature:LocalDomains`, com a validação ao subir (sem a chave, sandbox e domínios da demonstração em produção)
- [x] 2.2 `AutentiqueSignatureProvider`: mandar com o envio do arquivo, situação, lembrete, cancelamento pelo prazo, PDF assinado e exclusão, com as falhas sem dados pessoais e no log
- [x] 2.3 `FakeAutentique` no `FiscoProjects.Testing`, com a API, os arquivos assinados, a chave, o limite de chamadas e a Autentique fora do ar
- [x] 2.4 Testes do contrato do provedor e os próprios da Autentique (sandbox, assinaturas de fora do pedido, falhas, chave só nos endereços da Autentique)

## 3. Signatários da demonstração

- [x] 3.1 `SignatureProviders` com o provedor da demonstração: o pedido aos domínios da demonstração no local, o `Of(term)`, a conferência, a exclusão do projeto e a página local pelos dois
- [x] 3.2 Testes da API do pedido aos signatários da demonstração e do pedido com signatário de fora

## 4. Aviso do provedor

- [x] 4.1 Contrato `ISignatureNotices` e a leitura do aviso da Autentique (HMAC do corpo cru, documento em `event.data` ou `event.data.object`)
- [x] 4.2 Fila dos avisos lida pela `SignatureCheck` e a rota `POST /signature-notices` fora de `/api`
- [x] 4.3 Testes do aviso: de assinatura, forjado, de documento de fora e sem segredo configurado

## 5. Instalação

- [x] 5.1 Terraform: variável `autentique`, `vault_settings` no módulo `app` e o `dev` com o sandbox, o aviso e os domínios da demonstração
- [x] 5.2 `InfrastructureTests` da Autentique pelo Terraform, do sandbox no `dev` e do guia
- [x] 5.3 `docs/instalacao.md`: a conta, a chave, o aviso e os segredos da Autentique, e a ordem no `dev`
- [x] 5.4 Testes de subida da aplicação (Autentique sem a chave, sandbox e domínios da demonstração em produção)

## 6. Documentação e verificação

- [x] 6.1 `CLAUDE.md`, glossário e roadmap
- [ ] 6.2 Build sem avisos, testes, formatação, lint, specs e `infra.sh`
