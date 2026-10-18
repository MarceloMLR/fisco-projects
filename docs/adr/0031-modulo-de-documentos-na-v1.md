---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0031: Módulo de documentos com pastas padrão configuráveis e status manual

## Contexto e problema

O MVP tem pastas padrão, status do documento, versão automática, vínculo com o cronograma, busca e visualização de PDF, imagem, CSV e texto. Os arquivos ficam no SharePoint, na pasta de cada projeto (ADR-0030). Pelo ADR-0003, os nomes das pastas padrão não podem ficar fixos no código. O que o módulo de documentos tem na v1, e como um documento chega a "Aprovado"?

## Fatores de decisão

- Organização dos documentos por projeto, com versões.
- Nada específico de uma empresa fixo no código.
- Aceite formal já coberto pelos termos de aceite.
- Escopo enxuto na v1.

## Opções consideradas

- A. Status manual, como no MVP, com registro de quem mudou e quando
- B. Fluxo de aprovação com aprovador definido
- C. Sem status (só pastas e versões)

## Resultado da decisão

Opção escolhida: **A. Status manual**, porque é simples, já foi validado no MVP e o aceite formal fica com os termos de aceite.

Escopo:

| Item | Classificação |
| --- | --- |
| Pastas padrão em todo projeto novo, configuráveis na Administração (padrão do MVP: 01 Gestão do projeto, 02 Contratos e compras, 03 Requisitos e especificações, 04 Atas de reunião, 05 Entregas e manuais, 06 Termos de aceite) | v1 |
| Enviar arquivos arrastando e soltando, com limite de tamanho configurável | v1 |
| Arquivo com o mesmo nome na mesma pasta vira nova versão, com o histórico nos detalhes | v1 |
| Status Rascunho, Em revisão, Aprovado e Obsoleto, com registro de quem mudou e quando; obsoletos somem da lista padrão sem serem apagados | v1 |
| Vincular um documento a uma fase, tarefa ou marco do cronograma | v1 |
| Busca por nome no projeto | v1 |
| Visualizar PDF, imagens, CSV e texto no navegador; baixar qualquer arquivo | v1 |
| Excluir documento só pelo gestor; termo assinado nunca pode ser excluído | v1 |
| Criar subpastas dentro das pastas padrão | depois |
| Abrir arquivos do Office para editar no navegador | depois |
| Fluxo de aprovação com aprovador (opção B) | depois |
| Busca pelo conteúdo dos arquivos | depois |

### Consequências

- Bom, porque cada empresa usa os próprios nomes de pastas.
- Bom, porque versões e status mantêm o histórico sem apagar nada.
- Ruim, porque "Aprovado" depende de quem tem permissão, sem aprovador formal.
- Neutro, porque quem envia, muda status e exclui será confirmado na matriz de permissões.

### Confirmação

Testes cobrindo: projeto novo recebe as pastas padrão configuradas; enviar arquivo com o mesmo nome na mesma pasta cria nova versão; mudança de status registra quem e quando; obsoletos saem da lista padrão e aparecem pelo filtro; termo assinado não pode ser excluído; arquivo acima do limite é recusado.

## Prós e contras das opções

### A. Status manual

- Bom, porque é simples e já foi validado.
- Ruim, porque não há aprovação formal.

### B. Fluxo de aprovação

- Bom, porque dá rastreabilidade formal.
- Ruim, porque é mais trabalho e se sobrepõe aos termos de aceite.

### C. Sem status

- Bom, porque é o mais simples.
- Ruim, porque mistura documentos aprovados e obsoletos.
