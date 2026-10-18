---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0033: Senhas dos acessos cifradas com o Data Protection, com proteção das chaves opcional por certificado

## Contexto e problema

O módulo de acessos guarda login e senha de portais por projeto, substituindo o Excel com os acessos de todos os clientes. O ideia.md decidiu cifrar as senhas no banco com o Data Protection do .NET, sem Key Vault. Marcelo considera que esses acessos não são segredos críticos por enquanto. A Microsoft informa que as APIs de Data Protection não foram feitas principalmente para guardar dados sigilosos por tempo indeterminado, embora possam ser usadas para isso; o risco principal é perder ou invalidar as chaves e não conseguir mais abrir as senhas. Como proteger as senhas?

## Fatores de decisão

- Não guardar credenciais de terceiros (portais de clientes) em texto puro.
- Pouco esforço e pouca configuração, já que os acessos não são tratados como segredos críticos.
- Não perder as senhas em deploys ou trocas de chave.
- Funcionar em qualquer hospedagem (ainda não decidida).

## Opções consideradas

- A. Data Protection com proteção leve: chaves no banco e cifragem das chaves por certificado opcional
- B. Data Protection com proteção completa obrigatória (chaves sempre cifradas por certificado ou Key Vault)
- C. Criptografia própria com AES-GCM e chave mestra fora do banco
- D. Cada senha como segredo no Azure Key Vault
- E. Texto puro

## Resultado da decisão

Opção escolhida: **A. Data Protection com proteção leve e configurável**, porque evita o texto puro com quase nenhum código a mais e deixa a proteção mais forte como opção para quem precisar.

Regras:

- As senhas são cifradas com o Data Protection, com um propósito exclusivo para os acessos.
- O conjunto de chaves fica guardado no banco, para não se perder em novos deploys e entrar no mesmo backup dos dados.
- Cifrar o conjunto de chaves com um certificado (ou com o Key Vault, se a hospedagem for Azure) é uma opção da instalação, recomendada em produção pelo guia de instalação.
- As chaves nunca são apagadas, e o sistema consegue abrir senhas cifradas com chaves revogadas.
- A senha nunca é enviada por e-mail.

### Consequências

- Bom, porque as senhas não ficam legíveis em consultas ao banco, exportações ou logs.
- Bom, porque exige pouco código e pouca configuração.
- Ruim, porque, sem o certificado configurado, quem obtiver o banco inteiro leva junto as chaves e consegue abrir as senhas.
- Neutro, porque a opção de certificado permite elevar a proteção sem mudar o código.

### Confirmação

Testes cobrindo: a senha nunca aparece em texto puro no banco; cifrar, trocar a chave e decifrar continua funcionando; senhas cifradas com chave revogada continuam legíveis; com o certificado configurado, o conjunto de chaves fica cifrado; sem o certificado, o sistema funciona normalmente.

## Prós e contras das opções

### A. Proteção leve e configurável

- Bom, porque é simples e evita o texto puro.
- Ruim, porque, sem certificado, não protege contra o roubo do banco inteiro.

### B. Proteção completa obrigatória

- Bom, porque protege até contra o roubo do banco inteiro.
- Ruim, porque exige mais configuração em cada instalação para um dado que não é tratado como crítico.

### C. AES-GCM própria

- Bom, porque é pensada para guardar dados por muito tempo e dá controle total sobre a troca de chaves.
- Ruim, porque exige mais código de criptografia, com alto custo em caso de erro.

### D. Azure Key Vault por senha

- Bom, porque é um cofre gerenciado, com versões e auditoria.
- Ruim, porque são milhares de segredos, uma chamada externa por consulta e dependência do Azure.

### E. Texto puro

- Bom, porque não exige esforço.
- Ruim, porque qualquer pessoa com acesso ao banco, a backups ou a exportações lê as senhas de todos os clientes.
- Ruim, porque criptografar depois exige migrar todas as senhas já cadastradas.

## Mais informações

- ASP.NET Core Data Protection: https://learn.microsoft.com/aspnet/core/security/data-protection
- Abrir dados protegidos por chaves revogadas: https://learn.microsoft.com/aspnet/core/security/data-protection/consumer-apis/dangerous-unprotect
