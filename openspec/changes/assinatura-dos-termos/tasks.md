## 1. Decisão e documentação

- [x] 1.1 ADR-0129 aceito; ADR-0035 substituído e ADR-0045 e ADR-0095 substituídos em parte, com o status de cada um e o índice dos ADRs
- [x] 1.2 A fronteira dos aceites no `CLAUDE.md`, `docs/discovery.md` e `openspec/config.yaml`

## 2. Domínio e dados

- [x] 2.1 `AcceptanceSigner` e o termo com os signatários, o provedor, o documento no provedor e o token de concorrência; o e-mail apagado ao fechar; com os testes "Signatários inválidos", "E-mail apagado ao fechar o pedido" e "Recusa de um signatário"
- [x] 2.2 `AcceptanceAccess` e `DocumentAccess` só da gestão, sem a área `OwnTerms`, com a matriz "Permissões nos termos por papel"
- [x] 2.3 Configuração de `acceptance_signers` com a auditoria (rótulo, referência, contexto e o e-mail como segredo), a migração e o `RecipientId` opcional

## 3. Provedor de assinatura

- [x] 3.1 `ISignatureProvider`, `SignatureProviderException` e os testes do contrato (`SignatureProviderContract`)
- [x] 3.2 `LocalSignatureProvider` (pasta local, códigos por signatário, o PDF como assinado), a escolha por `Signature:Provider` e a recusa de subir em produção ("Provedor local em produção")
- [x] 3.3 Rotas sem sessão do provedor local (`/api/local-signatures/{token}`), só com o provedor local, com os testes "Assinar pela página local" e "Link local de outro termo ou já usado"

## 4. Casos de uso e API

- [x] 4.1 `RequestAcceptance` com os signatários e o provedor (409 sem provedor, 502 com o provedor recusando), e o termo anexado sem pessoa
- [x] 4.2 `ApplySignatureStatus` e o `SignatureCheck` em segundo plano, com os testes "Assinatura parcial", "Conferência repetida", "Provedor fora do ar", "Termo assinado na pasta de termos" e "Marco atingido pelo termo"
- [x] 4.3 Lembrar, cancelar e anexar pelo provedor; os pedidos anteriores ("Pedido anterior pendente", "Anexar em nome da pessoa do pedido"); saem as rotas de resposta
- [x] 4.4 Exclusão do projeto com os documentos do provedor ("Excluir projeto com pedido aberto")
- [x] 4.5 Avisos sem o pedido e o lembrete, `AcceptanceWorkSource` só com os pedidos feitos pela pessoa, e sai o `IPendingAcceptances` do resumo diário
- [x] 4.6 Testes da API com o nome de cada cenário da spec, a auditoria ("E-mail fora da auditoria") e o aquecimento

## 5. Tela

- [x] 5.1 `npm run api:update`, as consultas e os simuladores do MSW (`web/src/test/demoAcceptances.ts`)
- [x] 5.2 `AcceptanceDialog` com os signatários (`useFieldArray`, de 1 a 10) e o termo anexado sem pessoa; só "Anexar termo assinado" sem provedor
- [x] 5.3 A lista com os signatários, a aba só da gestão e a gaveta com a seção Signatários e o link do provedor local; sai a resposta da pessoa
- [x] 5.4 `LocalSigningPage` (`/sign/:token`), fora da estrutura das telas
- [x] 5.5 Testes de tela: "Pedir aceite pela tela", "Aba só da gestão", "Signatários na gaveta", "Lembrar e cancelar pela tela" e a página local

## 6. Demonstração

- [x] 6.1 Signatários fictícios no plano e a carga pelo provedor local, com o teste "Termos na demonstração"

## 7. Documentação e conferência

- [x] 7.1 `CLAUDE.md` (seções 3 e 6), glossário e roadmap
- [ ] 7.2 Capturas do pedido, da lista, da gaveta e da página local, claro e escuro, para o PR
- [ ] 7.3 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`
