---
status: proposta
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0078: Entrada local por e-mail no desenvolvimento e nas demonstrações, sem tenant

## Contexto e problema

O login é pelo Entra ID (ADR-0004), e o ADR-0009 previa demonstrações num tenant com pessoas fictícias. Marcelo não tem tenant nem assinatura do Azure, e não terá até uma empresa comprar o sistema: desenvolvimento e demonstrações acontecem na máquina dele. Sem tenant, ninguém consegue entrar. Como a pessoa entra no FiscoProjects fora da produção?

## Fatores de decisão

- Demonstrar o sistema inteiro sem tenant, trocando de papel com facilidade.
- Manter a página de entrada aprovada (ADR-0009) e a mesma sessão do BFF (ADR-0054).
- Nenhuma brecha em produção: lá, só o Entra.

## Opções consideradas

- A. Entrada pelo e-mail de uma pessoa fictícia, na própria página de entrada, sem senha
- B. Simulador de OIDC em contêiner, imitando o Entra
- C. Tela própria de demonstração, com a lista de pessoas para escolher

## Resultado da decisão

Opção escolhida: **A. Entrada local por e-mail**, por decisão de Marcelo.

Regras:

- Fora da produção, quando ligada na configuração, a página de entrada aceita o e-mail de uma das pessoas fictícias do diretório de demonstração e entra direto, sem senha. E-mail fora do diretório é recusado.
- O diretório de demonstração é configuração do ambiente (nome e e-mail de cada pessoa fictícia), nunca código, e usa só dados fictícios (ADR-0002).
- A entrada local segue o mesmo caminho do Entra a partir da identidade: usuário criado no primeiro login com o vínculo do provedor "local" (ADR-0005, ADR-0009), a mesma sessão por cookie, com expiração de 8 horas sem uso, e o mesmo "Sair" (ADR-0054).
- Em produção, a entrada local não existe: a aplicação não sobe se ela estiver ligada.
- O login pelo Entra, com a recusa de convidados (ADR-0007), entra quando houver tenant, na change M365-1 do roadmap.
- Substitui, no ADR-0009, o item "Login simulado do MVP: fora; testes usam autenticação de teste e demonstrações usam um tenant com pessoas fictícias".

### Consequências

- Bom, porque desenvolvimento e demonstrações funcionam sem tenant e sem custo.
- Bom, porque a tela, a sessão e a criação do usuário são as mesmas do login pelo Entra.
- Ruim, porque o login pelo Entra só é conferido de verdade quando houver tenant.
- Ruim, porque um ambiente que não é de produção, se exposto na internet com a entrada local ligada, deixaria entrar qualquer um que conheça um e-mail do diretório; por isso ela só é ligada na máquina de desenvolvimento.

### Confirmação

A página de entrada aceita as pessoas do diretório de demonstração e recusa os outros e-mails; um teste garante que a aplicação não sobe em produção com a entrada local ligada.

## Prós e contras das opções

### A. Entrada local por e-mail

- Bom, porque usa a página de entrada aprovada, sem tela nova.
- Ruim, porque não exercita o protocolo OIDC até haver tenant.

### B. Simulador de OIDC

- Bom, porque exercita o mesmo protocolo do Entra.
- Ruim, porque a tela de login é a do simulador, fora do design aprovado, e as demonstrações ficam mais lentas.

### C. Tela de demonstração com a lista de pessoas

- Bom, porque troca de papel com um clique.
- Ruim, porque exige desenhar e aprovar uma tela que só existe fora da produção.
