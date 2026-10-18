---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0009: Página de entrada própria com login pelo Entra ID, sessão e saída

## Contexto e problema

O login é pelo Entra ID (ADR-0004) e o acesso é controlado por ele (ADR-0005). Falta decidir como a pessoa entra, quanto tempo dura a sessão e o que o "Sair" faz. Marcelo não quer que a experiência de entrada dependa só da tela da Microsoft e quer manter aberta a possibilidade de, no futuro, atender empresas sem Microsoft 365.

## Fatores de decisão

- Experiência de entrada com a identidade do FiscoProjects.
- Nunca manipular senhas de contas Microsoft no sistema; MFA e acesso condicional continuam valendo.
- Ponto de extensão para outros provedores no futuro.
- Uso corporativo: sair do sistema sem desconectar o Microsoft 365 do navegador.

## Opções consideradas

- A. Página de entrada própria com campo de e-mail; senha e MFA na página da Microsoft
- B. Redirecionamento direto para a página da Microsoft
- C. Senha digitada na página do FiscoProjects (fluxo ROPC)

## Resultado da decisão

Opção escolhida: **A. Página de entrada própria com campo de e-mail**, porque dá ao FiscoProjects a própria tela de entrada, mantém senha e MFA na Microsoft e cria um ponto de extensão para outros provedores (pelo domínio do e-mail).

Escopo:

| Item | Classificação |
| --- | --- |
| Página de entrada própria (nome, logo e tema da instalação) com campo de e-mail, lembrado no navegador, e botão "Entrar"; o e-mail é enviado à Microsoft como dica, e senha e MFA ficam na página da Microsoft | v1 |
| Mensagem clara quando o acesso é recusado: convidado (ADR-0007), desativado (ADR-0008) ou sem atribuição no Entra | v1 |
| Sessão expira após 8 horas sem uso, valor configurável | v1 |
| "Sair" encerra só a sessão do FiscoProjects, sem desconectar a conta Microsoft do navegador | v1 |
| Personalização da tela da Microsoft com a marca da empresa (company branding do Entra) | fora do código; orientação no guia de instalação |
| Senha digitada no FiscoProjects (ROPC) | fora |
| MFA e acesso condicional | fora do sistema (ficam no Entra) |
| "Lembrar de mim" | fora (o SSO resolve) |
| Login simulado do MVP | fora; testes usam autenticação de teste e demonstrações usam um tenant com pessoas fictícias |

Regra de modelo de dados:

- O usuário tem um identificador interno próprio. O vínculo com o provedor de identidade (provedor + identificador externo, como o `oid` do Entra) fica em um registro separado, para que outros provedores, inclusive contas locais, possam ser acrescentados sem migração do usuário.

### Consequências

- Bom, porque a primeira tela é do FiscoProjects e o e-mail lembrado reduz o login a um clique.
- Bom, porque senhas de contas Microsoft nunca passam pelo sistema.
- Bom, porque o campo de e-mail e o vínculo separado com o provedor preparam o caminho para outros provedores.
- Ruim, porque, sem sessão Microsoft ativa no navegador, a pessoa vê duas telas em sequência (a do FiscoProjects e a da Microsoft).
- Neutro, porque a personalização da tela da Microsoft depende de licença Entra ID P1/P2 ou Office 365, e o CSS personalizado está sendo descontinuado.

### Confirmação

Testes cobrindo: o e-mail informado é enviado como dica ao Entra; as mensagens de recusa aparecem para convidado, desativado e sem atribuição; a sessão expira após o tempo configurado sem uso; "Sair" encerra só a sessão local; o usuário é encontrado pelo vínculo com o provedor, não por um campo do Entra na própria tabela de usuários.

## Prós e contras das opções

### A. Página própria com campo de e-mail

- Bom, porque dá identidade própria à entrada e serve de ponto de extensão.
- Ruim, porque pode haver duas telas em sequência.

### B. Redirecionamento direto para a Microsoft

- Bom, porque economiza uma tela.
- Ruim, porque não mostra a marca da instalação nem explica recusas de acesso com clareza.
- Ruim, porque não oferece ponto de extensão para outros provedores.

### C. Senha digitada no FiscoProjects (ROPC)

- Bom, porque a experiência fica inteira na tela do sistema.
- Ruim, porque a Microsoft recomenda não usar: é incompatível com MFA e não funciona com contas federadas.
- Ruim, porque o sistema passaria a manipular senhas, contrariando o ADR-0004.

## Mais informações

- ROPC: https://learn.microsoft.com/entra/identity-platform/v2-oauth-ropc
- Company branding: https://learn.microsoft.com/entra/fundamentals/how-to-customize-branding
- O modo sem Microsoft 365 (contas locais com ASP.NET Core Identity, e-mail por SMTP, arquivos em disco ou blob) fica como pendência para um ADR futuro.
