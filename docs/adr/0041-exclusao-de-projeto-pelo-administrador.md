---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0041: Exclusão definitiva de projeto, com todo o histórico, só pelo administrador

Substitui o item "Excluir projeto" do ADR-0016, que deixava a exclusão fora do sistema.

## Contexto e problema

O ADR-0016 decidiu que nenhum projeto é excluído, só cancelado, para preservar o histórico. Na revisão de papéis (ADR-0040), Marcelo decidiu que o administrador pode excluir um projeto e todo o seu histórico, e mais ninguém. Como a exclusão funciona?

## Fatores de decisão

- Permitir limpar projetos criados por engano, de teste ou que a empresa não quer manter.
- Evitar exclusões acidentais.
- Não apagar arquivos do SharePoint que não foram criados pelo FiscoProjects (ADR-0030).
- Manter o rastro de quem excluiu (ADR-0039).

## Opções consideradas

- A. Manter o ADR-0016: só cancelar, sem exclusão
- B. Exclusão definitiva só pelo administrador
- C. Exclusão também pelo gerente do projeto

## Resultado da decisão

Opção escolhida: **B. Exclusão definitiva só pelo administrador**, por decisão de Marcelo.

Regras:

- Só o administrador exclui projetos; o gerente continua podendo cancelar (ADR-0016).
- A exclusão apaga o projeto e tudo o que pertence a ele no FiscoProjects: cronograma e linha de base, chamados (com mensagens, linha do tempo e chamados de fornecedor), documentos, links, acessos, termos de aceite, membros e notificações ligadas a ele.
- No SharePoint, apaga só as pastas e os arquivos criados pelo FiscoProjects (registrados no banco); arquivos que já existiam na pasta do projeto não são tocados.
- Antes de excluir, o sistema mostra o que será apagado (quantidade de chamados, documentos e demais itens) e exige digitar o código do projeto para confirmar.
- A exclusão é irreversível.
- A auditoria registra quem excluiu, quando e o resumo do que foi apagado. Os registros de auditoria anteriores são mantidos (ADR-0039); o prazo de retenção será decidido no bloco de governança.
- Números de chamados excluídos não são reutilizados (ADR-0029).

### Consequências

- Bom, porque projetos de teste ou criados por engano podem ser removidos.
- Bom, porque só um papel pode excluir, com confirmação reforçada e registro na auditoria.
- Ruim, porque a exclusão é definitiva: um erro do administrador apaga tudo do projeto.
- Neutro, porque a auditoria ainda guarda registros do projeto excluído até o prazo de retenção.
- Neutro, porque apagar arquivos no SharePoint depende de o papel de escrita da aplicação permitir exclusão (a confirmar na implementação).

### Confirmação

Testes cobrindo: só o administrador vê e executa a exclusão; a exclusão exige digitar o código do projeto; todos os dados do projeto são apagados; arquivos do SharePoint não registrados pelo sistema permanecem; a auditoria registra a exclusão; o número de um chamado excluído não é reutilizado.

## Prós e contras das opções

### A. Só cancelar

- Bom, porque nada se perde.
- Ruim, porque projetos de teste ou criados por engano ficam para sempre.

### B. Exclusão só pelo administrador

- Bom, porque permite limpar o sistema com controle.
- Ruim, porque a perda é irreversível.

### C. Exclusão também pelo gerente

- Bom, porque não depende do administrador.
- Ruim, porque mais pessoas podem apagar histórico por engano.
