## 1. Decisão

- [x] 1.1 ADR-0117 (entrada pelo Entra e credencial pela identidade gerenciada), com o certificado do ADR-0052 substituído

## 2. Entrada pelo Entra na API

- [ ] 2.1 Opções `EntraSignIn`, conferidas ao subir, e `entraSignIn` nas opções da página
- [ ] 2.2 Caso de uso `SignInWithEntra`: convidado, desativado e o vínculo `entra` pelo `oid`, com a recusa de convidado na auditoria
- [ ] 2.3 Handler OpenID Connect com a prova da identidade gerenciada, a volta à tela pedida e as recusas na página de entrada
- [ ] 2.4 `POST /api/session/entra`, com a dica do e-mail só no endereço da Microsoft
- [ ] 2.5 Entrada local junto com a do Entra, sem registrar recusa
- [ ] 2.6 Testes da API com o Entra simulado no processo e testes do caso de uso

## 3. Página de entrada

- [ ] 3.1 Seguir para a Microsoft, as mensagens de convidado e sem atribuição e os textos das duas entradas juntas
- [ ] 3.2 Contrato e tipos da API atualizados e testes da página

## 4. Terraform

- [ ] 4.1 Aplicativo do Entra: retorno, declarações, consentimento, atribuição e confiança na identidade gerenciada
- [ ] 4.2 Configurações da aplicação, primeiro administrador do Entra e `ASPNETCORE_FORWARDEDHEADERS_ENABLED`
- [ ] 4.3 Testes de infraestrutura

## 5. Documentação e conferência

- [ ] 5.1 `docs/instalacao.md` (pessoas de teste, atribuição e primeiro administrador), `CLAUDE.md`, glossário e roadmap
- [ ] 5.2 Entrada pelo Entra conferida no ambiente `dev`
