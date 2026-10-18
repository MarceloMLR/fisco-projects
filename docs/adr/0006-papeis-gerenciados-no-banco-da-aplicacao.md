---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0006: Papéis globais e por projeto gerenciados no banco da aplicação

## Contexto e problema

O Entra ID controla quem entra (ADR-0005). É preciso decidir onde ficam os papéis que definem o que cada pessoa pode fazer. O ideia.md previa papéis globais como App Roles do Entra e papéis por projeto no banco; o MVP tinha uma tela para o administrador trocar o papel global. Este ADR decide **onde** os papéis são gerenciados; quais papéis existem e o que cada um pode fazer serão decididos depois (ver "Mais informações").

## Fatores de decisão

- Autonomia do administrador do FiscoProjects, que pode não ser administrador do Entra.
- Troca de papel com efeito imediato.
- Um único modelo de autorização, fácil de testar.
- Não depender de licença Entra ID P1/P2.

## Opções consideradas

- A. Todos os papéis (globais e por projeto) no banco do FiscoProjects
- B. Papéis globais no Entra (App Roles) e papéis por projeto no banco
- C. Só o papel de administrador no Entra; os demais no banco

## Resultado da decisão

Opção escolhida: **A. Todos os papéis no banco do FiscoProjects**, porque dá autonomia ao administrador do sistema, a troca vale na hora, mantém um só modelo de autorização e não depende de licença do Entra.

Regras:

- O Entra ID só autentica; nenhuma regra de autorização depende de App Roles.
- O primeiro administrador é definido na configuração da instalação pelo identificador imutável do usuário no Entra (`oid`), não pelo e-mail.
- Todo usuário criado no primeiro login recebe o papel global de menor privilégio.
- O sistema impede remover ou desativar o último administrador ativo.
- Trocas de papel são registradas na auditoria.

### Consequências

- Bom, porque o administrador do FiscoProjects gerencia papéis sem depender da TI.
- Bom, porque a troca de papel vale sem novo login.
- Bom, porque a autorização fica em um só lugar e é testável sem o Entra.
- Ruim, porque a TI da empresa não vê nem revisa os papéis pelo Entra.
- Ruim, porque o sistema precisa tratar a configuração do primeiro administrador e a proteção do último.

### Confirmação

Testes cobrindo: novo usuário recebe o papel de menor privilégio; o `oid` configurado vira administrador; não é possível remover ou desativar o último administrador; troca de papel vale na requisição seguinte e gera registro de auditoria.

## Prós e contras das opções

### A. Todos os papéis no banco

- Bom, porque dá autonomia ao administrador do sistema.
- Bom, porque a troca vale na hora e fica na auditoria do sistema.
- Ruim, porque a governança não aparece no Entra.

### B. Globais no Entra, por projeto no banco

- Bom, porque a TI governa os papéis globais no Entra e o papel chega no token.
- Ruim, porque cada troca depende do administrador do Entra e exige novo login.
- Ruim, porque a atribuição por grupo exige Entra ID P1 ou P2.
- Ruim, porque divide a autorização em dois lugares.

### C. Só administrador no Entra

- Bom, porque a TI controla quem é administrador.
- Ruim, porque mantém dois modelos de autorização para resolver um único papel.

## Mais informações

- Os papéis listados no ideia.md (globais: Administrador, Gestor de projetos, Equipe de TI, Colaborador; por projeto: gestor, membro, leitor) são provisórios. O catálogo de papéis e a matriz de permissões serão revistos e decididos depois de definidos os módulos, em ADR próprio.
