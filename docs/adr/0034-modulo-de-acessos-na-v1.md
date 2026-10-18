---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0034: Módulo de acessos com senha mascarada, visibilidade pelo projeto e importação de planilha

## Contexto e problema

No MVP, cada acesso tem portal, ambiente, endereço, login, senha e observação; a senha é mascarada, com mostrar e copiar registrados na auditoria; a visibilidade é "equipe do projeto" ou "pessoas escolhidas"; e há importação da planilha atual, com mapeamento de colunas e filtro por cliente. As senhas são cifradas no banco (ADR-0033). Marcelo considera que esses acessos não são segredos. Quanto desse controle entra na v1?

## Fatores de decisão

- Evitar exposição acidental da senha (por exemplo, em compartilhamento de tela).
- Pouca regra para um dado que não é tratado como segredo.
- Migrar com facilidade os acessos que hoje estão em Excel.

## Opções consideradas

- A. Igual ao MVP: máscara, mostrar e copiar com auditoria, visibilidade por equipe ou pessoas escolhidas, importação
- B. Mais leve: máscara, visibilidade pelo acesso ao projeto, importação
- C. Mínimo: senha visível, sem auditoria e sem importação

## Resultado da decisão

Opção escolhida: **B. Mais leve**, sem registro na auditoria de quem mostrou ou copiou a senha (decisão de Marcelo), porque mantém a proteção contra exposição acidental e a migração do Excel com o mínimo de regras.

Escopo:

| Item | Classificação |
| --- | --- |
| Acessos por projeto: portal, ambiente, endereço, login, senha e observação | v1 |
| Senha mascarada, com botões "Mostrar" e "Copiar" | v1 |
| Visibilidade: quem tem acesso ao projeto (papéis exatos na matriz de permissões) | v1 |
| Importação de planilha (.xlsx ou .csv): mapear colunas, filtrar linhas (por exemplo, pelo cliente) e importar só as marcadas | v1 |
| A senha nunca vai por e-mail (ADR-0033) | v1 |
| Registro na auditoria de quem mostrou ou copiou a senha | fora |
| Restringir um acesso a pessoas escolhidas | depois |
| Lembrete de troca de senha numa data definida | depois |
| Exportar os acessos | fora |
| Gerador de senhas | fora |

Criar, alterar e excluir acessos continuam sujeitos à auditoria geral do sistema, a ser decidida no bloco de auditoria.

### Consequências

- Bom, porque a máscara evita que a senha apareça em compartilhamentos de tela.
- Bom, porque a importação facilita sair do Excel.
- Ruim, porque não dá para esconder um acesso de parte da equipe do projeto na v1.
- Ruim, porque não há registro de quem viu ou copiou cada senha.

### Confirmação

Testes cobrindo: a senha aparece mascarada por padrão e é revelada só ao clicar em "Mostrar"; quem não tem acesso ao projeto não vê os acessos; a importação respeita o mapeamento de colunas e importa só as linhas marcadas, já cifrando as senhas; nenhum e-mail do sistema contém senha.

## Prós e contras das opções

### A. Igual ao MVP

- Bom, porque é o mais completo e já foi validado.
- Ruim, porque cria regras de acesso por item para um dado não crítico.

### B. Mais leve

- Bom, porque mantém a proteção contra exposição acidental com poucas regras.
- Ruim, porque não permite restringir um acesso a parte da equipe.

### C. Mínimo

- Bom, porque é o mais rápido de construir.
- Ruim, porque a senha aparece em qualquer compartilhamento de tela e a migração do Excel vira trabalho manual.
