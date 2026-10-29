---
status: aceita
date: 2026-10-29
decision-makers: Marcelo Ribeiro
---

# ADR-0128: Horas do projeto: apontamento de cada pessoa e saldo contra as horas contratadas, só em horas

Substitui o item "Apontamento de horas trabalhadas" do ADR-0020 e completa o ADR-0016, cujo item "Orçamento e custos" continua fora.

## Contexto e problema

A v1 deixou fora as horas trabalhadas, o orçamento e os custos (ADR-0016, ADR-0020). Com a v1 concluída, em 29/10/2026, Marcelo pediu as horas do projeto: o projeto tem as horas contratadas e ganha horas adicionais quando o cliente pede, no meio do projeto, algo fora do escopo; cada pessoa aponta as horas que trabalhou (o gerente 20 h, o desenvolvedor 10 h); e o administrador vê, numa visão só dele, se o projeto gastou mais horas do que as contratadas. Como registrar as horas e mostrar o saldo de cada projeto?

## Fatores de decisão

- Saber, por projeto, se as horas apontadas passaram das contratadas com as adicionais.
- Apontar rápido, no computador e no celular (ADR-0012).
- As horas de cada pessoa são dado pessoal e de gestão: só a própria pessoa e o administrador as veem (ADR-0058).
- Escopo enxuto: horas, não dinheiro.

## Opções consideradas

Medida do saldo:

- A1. Só em horas: as contratadas e as adicionais contra as apontadas.
- A2. Em reais: o valor vendido do projeto contra o custo por hora de cada pessoa.

Onde a pessoa aponta:

- B1. No projeto, com a tarefa do cronograma ou o chamado do projeto opcional.
- B2. Só no projeto.
- B3. Sempre numa tarefa ou num chamado.

Quem vê e cadastra:

- C1. Cada pessoa aponta e vê só as próprias horas; o administrador cadastra as contratadas e as adicionais e é o único que vê a visão gerencial.
- C2. Como C1, e o gerente cadastra as horas adicionais do projeto dele, sem ver o consumo.
- C3. Como C2, e o gerente vê o consumo do projeto dele.

## Resultado da decisão

Opções escolhidas: **A1**, **B1** e **C1**. A1 e C1 por decisão de Marcelo em 29/10/2026: o saldo em horas responde se o projeto deu lucro ou prejuízo contra o contratado sem guardar o custo de cada pessoa, e a visão fica só com o administrador, como ele descreveu. B1 deixa o apontamento rápido (o projeto basta) e, quando a pessoa quer, mostra em que tarefa ou chamado as horas foram gastas.

Regras:

- **Apontamento.** O dia (até hoje, no fuso da instalação, ADR-0013), as horas (de 0,25 a 24, em quartos de hora), a descrição do que foi feito, o projeto e, opcionalmente, uma tarefa do cronograma dele ou um chamado dele que a pessoa vê. A soma da pessoa num dia não passa de 24 horas.
- **Quem aponta.** As pessoas do projeto, menos o leitor, que acompanha e não trabalha no projeto (ADR-0040), e o administrador, que age em todos os projetos.
- **Quem vê e corrige.** Cada pessoa vê, corrige e apaga só as próprias horas. O administrador vê, corrige e apaga as de qualquer pessoa. O gerente e o líder técnico não veem as horas das outras pessoas. Toda mudança vai para a auditoria (ADR-0039).
- **Projeto encerrado.** Concluído ou cancelado, o projeto não recebe apontamentos nem correções, como os demais módulos (ADR-0016).
- **Horas contratadas e adicionais.** Só o administrador cadastra as horas contratadas do projeto (sem elas, o projeto não tem saldo) e as horas adicionais, cada uma com o dia, as horas e o motivo (o pedido fora do escopo). As adicionais se corrigem e se apagam, com a auditoria.
- **Visão gerencial.** Só do administrador, na Administração: por projeto, as contratadas, as adicionais, o total, as apontadas, o saldo (o total menos as apontadas) e a parte consumida, com o estouro destacado por ícone, cor e texto (ADR-0065); por pessoa, as horas de cada uma no projeto. Baixa em planilha, como a auditoria.
- **Pessoa que sai.** As horas de quem saiu do projeto ou foi desativado continuam no projeto, com o nome atual (o anonimizado aparece pelo identificador, ADR-0110).
- **Exclusão do projeto.** As horas saem com ele (ADR-0111).

Fora:

| Item | Classificação |
| --- | --- |
| Valores em reais: valor do projeto, custo ou preço por hora, faturamento | fora (ADR-0016) |
| Aprovação das horas pelo gerente e fechamento do mês | depois |
| Horas planejadas por tarefa e cronômetro | depois |
| Horas que o gerente vê ou cadastra | fora |

### Consequências

- Bom, porque o administrador vê o saldo de cada projeto sem que o custo de cada pessoa entre no sistema.
- Bom, porque o apontamento pede só o projeto, e a tarefa e o chamado ficam para quem quer o detalhe.
- Ruim, porque o saldo em horas não mostra o lucro em reais: uma hora de gerente e uma de desenvolvedor contam igual.
- Ruim, porque, sem aprovação, uma hora apontada errada só é corrigida pela pessoa ou pelo administrador.

### Confirmação

Testes cobrindo: o apontamento só no projeto em que a pessoa está, menos o leitor, e em qualquer projeto para o administrador; o dia depois de hoje, as horas fora dos quartos de hora e a soma do dia acima de 24 recusados; a tarefa e o chamado só do mesmo projeto, e o chamado só se a pessoa o vê; cada pessoa vendo e corrigindo só as próprias horas, e o administrador as de todos; o projeto encerrado sem apontamentos; as horas contratadas e adicionais só do administrador; o saldo e o estouro na visão gerencial; as horas saindo com o projeto excluído. A matriz de permissões das horas é coberta por inteiro (ADR-0056).

## Prós e contras das opções

### A1. Só em horas

- Bom, porque não guarda o custo de cada pessoa, que é dado sensível.
- Ruim, porque não mostra o lucro em reais.

### A2. Em reais

- Bom, porque mostra o lucro de fato.
- Ruim, porque guarda salário ou custo de cada pessoa e pede orçamento e preços, que o ADR-0016 deixou fora.

### B1. Projeto, com tarefa ou chamado opcional

- Bom, porque é rápido e permite o detalhe.
- Ruim, porque o detalhe depende da disciplina de cada pessoa.

### B2. Só no projeto

- Bom, porque é o mais simples.
- Ruim, porque não diz onde as horas foram gastas.

### B3. Sempre numa tarefa ou num chamado

- Bom, porque todo apontamento tem destino.
- Ruim, porque reuniões e gestão não cabem numa tarefa, e o apontamento fica lento.

### C1. Só a pessoa e o administrador

- Bom, porque segue o pedido de Marcelo e protege as horas de cada pessoa.
- Ruim, porque o gerente não acompanha o consumo do próprio projeto.

### C2 e C3. O gerente cadastra as adicionais ou vê o consumo

- Bom, porque o gerente sabe dos pedidos fora do escopo.
- Ruim, porque espalha o saldo do projeto, que Marcelo quer só com o administrador.
