---
status: proposta
date: 2026-11-05
decision-makers: Marcelo Ribeiro
---

# ADR-0154: Cópia do banco fora da assinatura da instalação

Complementa o ADR-0059, que mantém os backups automáticos do Azure SQL na própria assinatura e descartou backups próprios. Proposta aberta para a decisão de Marcelo, porque pede uma segunda assinatura (conta e custo).

## Contexto e problema

Os backups do ADR-0059 (restauração a qualquer ponto, backups mensais) ficam na mesma assinatura do banco. Isso cobre o erro de quem usa o sistema, o banco corrompido e, com a trava contra exclusão da Operação-4.1, o recurso apagado por engano. Não cobre a perda da própria assinatura:

- a assinatura suspensa ou cancelada por falta de pagamento: a Microsoft desativa os recursos, e a restauração dos backups mensais só funciona com a assinatura ativa;
- a conta de quem opera invadida, com permissão para tirar a trava e apagar o banco e os backups.

Quando a instalação fica na nuvem da empresa (ADR-0127), esse risco é da empresa, como o de qualquer outro sistema dela. Quando o fornecedor hospedar as instalações na própria nuvem, os bancos de todos os clientes dependem de uma assinatura só, e esse risco passa a ser do fornecedor. Como guardar uma cópia que sobreviva à perda da assinatura?

## Fatores de decisão

- Sobreviver à perda da assinatura e à conta invadida.
- Manter os dados no Brasil (ADR-0052).
- Nada pessoal nos logs públicos do GitHub Actions (ADR-0116).
- Sem senha de banco guardada em lugar nenhum (ADR-0052).
- Custo baixo e pouca rotina.

## Opções consideradas

- A. Exportação semanal do banco para um armazenamento imutável numa segunda assinatura
- B. Só os backups da própria assinatura, com a trava, o alerta de orçamento e a conta protegida
- C. Backups geo-redundantes

## Resultado da decisão

A decidir por Marcelo. Recomendação: **A**, só para as instalações hospedadas na nuvem do fornecedor; na nuvem da empresa, **B**, como hoje, e a empresa escolhe.

Como seria a opção A:

- Uma segunda assinatura, de preferência noutro tenant, com uma conta de armazenamento na Brazil South com retenção imutável por tempo (os arquivos não podem ser alterados nem apagados até vencer, nem por quem administra).
- Uma vez por semana, um processo agendado copia o banco (cópia consistente, `az sql db copy`) e exporta a cópia como arquivo `.bacpac` para essa conta, apagando a cópia depois. Guarda 8 semanas.
- O processo roda com uma identidade sem senha, só com a leitura do banco e a escrita nessa conta; quem opera a instalação não tem permissão de apagar ali.
- O teste trimestral do roteiro de restauração passa a incluir importar o último `.bacpac` num banco de teste.

A confirmar antes de aceitar: a exportação com a identidade gerenciada no lugar da senha do SQL, e onde o processo agendado roda (no Azure da segunda assinatura, e não no GitHub Actions, cujos logs são públicos).

### Consequências

- Bom, porque uma cópia de até uma semana sobrevive à perda da assinatura principal e à conta de quem opera invadida.
- Bom, porque os dados continuam no Brasil.
- Ruim, porque pede uma segunda assinatura e uma rotina a mais no teste trimestral.
- Ruim, porque a cópia semanal perde até 7 dias quando é a única que sobra; os backups da assinatura principal continuam sendo o caminho normal.
- Neutro, porque o custo é de armazenamento, pequeno nesse volume, mais o banco temporário da cópia por alguns minutos por semana.

### Confirmação

Se aceita: o processo agendado, a conta com retenção imutável e o roteiro de restauração com a importação do `.bacpac`, testados no ambiente `dev`.

## Prós e contras das opções

### A. Exportação semanal para armazenamento imutável noutra assinatura

- Bom, porque é a única opção que sobrevive à perda da assinatura.
- Ruim, porque é infraestrutura e rotina a mais, e a recuperação a partir dela é mais lenta (importar o arquivo).

### B. Só os backups da própria assinatura

- Bom, porque não custa nada a mais.
- Ruim, porque, perdida a assinatura, perdem-se o banco e os backups juntos.

### C. Backups geo-redundantes

- Bom, porque é só um parâmetro (ADR-0059).
- Ruim, porque a cópia fica na mesma assinatura (não resolve a suspensão nem a conta invadida) e nos Estados Unidos, fora do Brasil.
